using System.Diagnostics;
using NetTopologySuite.Geometries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HamsterWorld.Models;
using HamsterWorld.DatabaseUtilities;
using Microsoft.EntityFrameworkCore;
//Для работы с сервисов Dadata
using Dadata;
using Dadata.Model;

namespace HamsterWorld.Controllers;

[Authorize(Policy = Role.AdminRoleName)]
public class AdminStoreController : Controller
{
    readonly ApplicationContext _context;
    readonly IConfiguration _config;

    public AdminStoreController(ApplicationContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<IActionResult> ManageStores()
    {
        //List of stores sorted by Id
        List<Store> stores = await _context.Stores.AsNoTracking()
                                                    .OrderBy(e => e.Id)
                                                    .ToListAsync();
        return View(stores);
    }

    public IActionResult AddStore()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStore(AddStoreBindingModel model)
    {
        if(IsWorkingScheduleTooShort(model.OpeningTime, model.ClosingTime))
        {
            ModelState.AddModelError(nameof(model.OpeningTime), "Рабочее время не может быть меньше четырёх часов");
        }

        (bool IsSuccess, string Message) = await TryAddNewStoreToDatabase(model);
        if( !IsSuccess )
        {
            ModelState.AddModelError(nameof(model.Address), Message);
        }

        if(!ModelState.IsValid)
        {
            return View(model);
        }

        return Redirect("ManageStores");
   }

    public async Task<IActionResult> DeleteStore(int id)
    {
        //Find store by id
        Store? store = await _context.Stores.FindAsync(id);
        if(store == null)
        {
            return NotFound("Такого филиала не существует");
        }

        _context.Stores.Remove(store);
        await _context.SaveChangesAsync();

        return RedirectToAction("ManageStores");
    }

    public async Task<IActionResult> ChangeStoreInfo(short id)
    {
        //Find store by id
        Store? store = await _context.Stores.FindAsync(id);
        if(store == null)
        {
            return NotFound("Такого филиала не существует");
        }

        ChangeStoreInfoBindingModel model = new ChangeStoreInfoBindingModel()
        {
            Id = store.Id,
            Name = store.Name,
            OpeningTime = store.OpeningTime,
            ClosingTime = store.ClosingTime,
            Address = store.Address
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStoreInfo(ChangeStoreInfoBindingModel model)
    {
        //Почти полный копипаст из AddStore
        if(IsWorkingScheduleTooShort(model.OpeningTime, model.ClosingTime))
        {
            ModelState.AddModelError(nameof(model.OpeningTime), "Рабочее время не может быть меньше четырёх часов");
        }

        (bool IsSuccess, string Message) = await TryApplyChangesToStoreInfo(model);
        if( !IsSuccess )
        {
            ModelState.AddModelError(nameof(model.Address), Message);
        }

        if(!ModelState.IsValid)
        {
            return View(model);
        }

        return RedirectToAction("ManageStores");
    }

    public async Task<IActionResult> ChangeStoreAdministrators(short storeId = 0)
    {
        if (storeId <= 0)
        {
            return NotFound("Магазина с таким Id не существует");
        }

        //Obtaining list of all administrators of this store
        List<User>? storeAdministrators = (await _context.Stores.AsNoTracking()
                                                .Include(e => e.Administrators)
                                                .Select(e => new { e.Id, e.Administrators })
                                                .FirstOrDefaultAsync(e => e.Id == storeId))
                                                ?.Administrators;
        if(storeAdministrators == null)
        {
            return NotFound("Магазина с таким Id не существует");
        }

        //id list of store's admins
        List<int> adminsIdentificators = storeAdministrators.Select(e => e.Id).ToList();

        //Creating view model
        List<ChangeStoreAdministratorsBindingModel> allAdmins = await _context.Users.AsNoTracking()
                                    .Where(e => e.RoleId == Role.STORE_ADMIN)
                                    .Select(e => new ChangeStoreAdministratorsBindingModel()
                                    {
                                        AdminId = e.Id,
                                        Login = e.Login,
                                        Email = e.Email,
                                        IsAdminOfThisStore = adminsIdentificators.Contains(e.Id)
                                    })
                                    .ToListAsync();

        ViewBag.StoreId = storeId;
        return View(allAdmins);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStoreAdministrators(bool? isBecomingAdmin, int adminId = 0,  short storeId = 0)
    {
        if (adminId <= 0 || storeId <= 0 || isBecomingAdmin == null)
        {
            return BadRequest();
        }

        //Obtain store info from database
        Store? store = await _context.Stores.FindAsync(storeId);
        if(store == null)
        {
            return BadRequest("Магазина не существует");
        }

        //Obtain admin info from database
        User? admin = await _context.Users.FindAsync(adminId);
        if(admin == null)
        {
            return BadRequest("Пользователя с таким Id не существует");
        }

        //Loads to context only user's info if he is admin of this store
        await _context.Entry(store) 
                      .Collection(e => e.Administrators!)
                      .Query()
                      .FirstOrDefaultAsync(e => e.Id == adminId);

        bool IsAdminOfThisStore = store.Administrators!.Count == 1;

        if(!IsAdminOfThisStore & (bool)isBecomingAdmin)
        {
            store.Administrators!.Add(admin);
        }
        if(IsAdminOfThisStore && (bool)!isBecomingAdmin)
        {
            store.Administrators!.Remove(admin);
        }
        await _context.SaveChangesAsync();

        return Ok();
    }

    bool IsWorkingScheduleTooShort(TimeOnly openingTime, TimeOnly closingTime)
    {
        long minWorkingTime = new TimeOnly(hour: 4, minute: 0).Ticks;

        //For example, 22:00 - 02:00
        if(openingTime > closingTime)
        {
            int timeUntilMidnight = 24 - openingTime.Hour;
            openingTime = openingTime.AddHours(timeUntilMidnight);
            closingTime = closingTime.AddHours(timeUntilMidnight);
        }

        return Math.Abs(openingTime.Ticks - closingTime.Ticks) < minWorkingTime;
    }

    async Task<(bool, string)> TryAddNewStoreToDatabase(AddStoreBindingModel model)
    {
        Store? store = await TryFindStoreInfoWithDadataService(model);

        //Такое может произойти только, когда пользователь ввёл плохой адрес
        if (store == null)
        {
            return (false, "Не удалось распознать адрес магазина");
        }

        await DbUsageTools.TryAddStoreToDatabase(_context, store);
    }

    async Task<Store?> TryFindStoreInfoWithDadataService(AddStoreBindingModel model)
    {
        //Obtaining coordinates of store by dadata api
        Address storeInfo = await ObtainStoreInfoFromDadataApi(model.Address);

        //If store hasn't been found
        if(storeInfo.geo_lon == null)
        {
            return null;
        }

        //Долгота и широта
        double longitude = double.Parse(storeInfo.geo_lon);
        double latitude = double.Parse(storeInfo.geo_lat);
        Point coordinates = new Point(longitude, latitude);

        //Найденый Dadat-ой адрес
        string fullAddress = storeInfo.result;

        Store store = new Store()
        {
            Name = model.Name,
            OpeningTime = model.OpeningTime,
            ClosingTime = model.ClosingTime,
            Coordinates = coordinates,
            Address = fullAddress
        };

        return store;
    }

    async Task<Address> ObtainStoreInfoFromDadataApi(string address)
    {
        string token = _config["Dadata:ApiKey"]!;
        string secret = _config["Dadata:Secret"]!;

        //Creating API client instance
        var api = new CleanClientAsync(token, secret);

        return await api.Clean<Address>(address);
    }


    async Task<(bool, string)> TryApplyChangesToStoreInfo(ChangeStoreInfoBindingModel model)
    {
        Store? oldStoreInfo = await _context.Stores.FindAsync(model.Id);
        if (oldStoreInfo == null)
        {
            return (false, "Магазина с таким Id не существует");
        }

        Store? newStoreInfo = await TryFindStoreInfoWithDadataService(model);
        if (newStoreInfo == null)
        {
            return (false, "Не удалось найти магазин по такому адресу");
        }

        //Проверяем, если новый адрес магазина соответствует адресам других магазинов
        if(oldStoreInfo.Address != newStoreInfo.Address)
        {
            if (_context.Stores.AsNoTracking().FirstOrDefaultAsync(e => e.Address == model.Address) != null)
            {
                return (false, "Другой магазин с таким адресом уже существует");
            }
        }

        oldStoreInfo.Name = newStoreInfo.Name;
        oldStoreInfo.OpeningTime = newStoreInfo.OpeningTime;
        oldStoreInfo.ClosingTime = newStoreInfo.ClosingTime;
        oldStoreInfo.Address = newStoreInfo.Address;
        await _context.SaveChangesAsync();

        return (true, "");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
