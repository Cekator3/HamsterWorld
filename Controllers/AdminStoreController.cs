using System.Diagnostics;
using NetTopologySuite.Geometries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HamsterWorld.Models;
using Microsoft.EntityFrameworkCore;
//Для работы с сервисов Dadata
using Dadata;
using Dadata.Model;

namespace HamsterWorld.Controllers;

[Authorize(Policy = Role.AdminRoleName)]
public class AdminStoreController : Controller
{
    ApplicationContext _context;
    IConfiguration _config;

    public AdminStoreController(ApplicationContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<IActionResult> ManageStores()
    {
        List<Store> stores = await _context.Stores.AsNoTracking().OrderBy(e => e.Id).ToListAsync();
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
        else if( !(await TryAddNewStoreToDatabase(model)) )
        {
            ModelState.AddModelError(nameof(model.Address), "Не удалось распознать полный адрес магазина, либо адрес такого магазина уже существует");
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
        Store? store = await _context.Stores.FirstOrDefaultAsync(e => e.Id == id);
        if(store == null)
        {
            return NotFound("Такого филиала не существует");
        }

        _context.Stores.Remove(store);
        await _context.SaveChangesAsync();

        return RedirectToAction("ManageStores");
    }

    public async Task<IActionResult> ChangeStoreInfo(int id)
    {
        //Find store by id
        Store? store = await _context.Stores.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
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
        else if(!(await TryApplyChangesToStoreInfo(model)))
        {
            ModelState.AddModelError(nameof(model.Address), "Не удалось распознать полный адрес магазина, либо новый адрес магазина соответствует адресу другого магазина");
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
            return BadRequest("Неверно указан Id магазина");
        }
        //Obtaining Id list of all administrators of this store
        var storeInfo = _context.Stores.AsNoTracking()
                                    .Include(e => e.Administrators)
                                    .Select(e => new { e.Id, e.Administrators })
                                    .FirstOrDefault(e => e.Id == storeId);
        if(storeInfo == null)
        {
            return BadRequest("Магазина не существует");
        }
        List<int> adminsIdentificators = storeInfo.Administrators!.Select(e => e.Id).ToList();

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

        //Explicitly loads to context administrator's info of this store
        await _context.Entry(store) 
                      .Collection(e => e.Administrators!)
                      .Query()
                      .FirstOrDefaultAsync(e => e.Id == adminId);

        //Adding or removing admin from administrating this store
        bool IsAlreadyAdminOfThisStore = store.Administrators![0].Id == adminId;

        if(!IsAlreadyAdminOfThisStore & (bool)isBecomingAdmin)
        {
            store.Administrators!.Add(admin);
        }
        if(IsAlreadyAdminOfThisStore && (bool)!isBecomingAdmin)
        {
            store.Administrators!.Remove(admin);
        }
        await _context.SaveChangesAsync();

        return Ok();
    }

    bool IsWorkingScheduleTooShort(TimeOnly openingTime, TimeOnly closingTime)
    {
        long minTime = new TimeOnly(hour: 4, minute: 0).Ticks;

        //Такое возможно, когда, например, открывается в 22:00, а закрывается в 02:00 ночи
        if(openingTime > closingTime)
        {
            int timeUntilMidnight = 24 - openingTime.Hour;
            openingTime = openingTime.AddHours(timeUntilMidnight);
            closingTime = closingTime.AddHours(timeUntilMidnight);
        }
        return Math.Abs(openingTime.Ticks - closingTime.Ticks) < minTime;
    }

    async Task<bool> TryAddNewStoreToDatabase(AddStoreBindingModel model)
    {
        Store? store = await TryConvertInputedStoreInfoToActualStoreInfo(model);

        //Такое может произойти только, когда пользователь ввёл плохой адрес
        if (store == null)
        {
            return false;
        }
        //Проверяем, существует ли магазин с такими же координатами
        if(await IsStoreWithThatAddressAlreadyExists(store))
        {
            return false;
        }

        await _context.AddAsync(store);
        await _context.SaveChangesAsync();

        return true;
    }

    async Task<Store?> TryConvertInputedStoreInfoToActualStoreInfo(AddStoreBindingModel model)
    {
        //Obtaining coordinates of store by dadata api
        Address storeInfo = await ObtainInfoOfStoreFromDadataApiByEnteredAddress(model.Address);

        //Если магазин не был найден
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

    async Task<Address> ObtainInfoOfStoreFromDadataApiByEnteredAddress(string address)
    {
        string token = _config["Dadata:ApiKey"]!;
        string secret = _config["Dadata:Secret"]!;

        //Creating API client instance
        var api = new CleanClientAsync(token, secret);

        return await api.Clean<Address>(address);
    }

    async Task<bool> IsStoreWithThatAddressAlreadyExists(Store store)
    {
        return await _context.Stores.AsNoTracking() 
                             .FirstOrDefaultAsync(e => e.Coordinates == store.Coordinates) != null;
    }


    async Task<bool> TryApplyChangesToStoreInfo(ChangeStoreInfoBindingModel model)
    {
        Store? newStoreInfo = await TryConvertInputedStoreInfoToActualStoreInfo(model);
        Store? oldStoreInfo = await _context.Stores.FindAsync(model.Id);

        //If ID of old store or inputed address are invalid
        if (oldStoreInfo == null || newStoreInfo == null)
        {
            return false;
        }
        //Проверяем, если новый адрес магазина соответствует адресам других магазинов
        if(oldStoreInfo.Address != newStoreInfo.Address)
        {
            if (_context.Stores.AsNoTracking().FirstOrDefaultAsync(e => e.Address == model.Address) != null)
            {
                return false;
            }
        }

        oldStoreInfo.Name = newStoreInfo.Name;
        oldStoreInfo.OpeningTime = newStoreInfo.OpeningTime;
        oldStoreInfo.ClosingTime = newStoreInfo.ClosingTime;
        oldStoreInfo.Address = newStoreInfo.Address;
        await _context.SaveChangesAsync();
        //TODO добавить возможность изменения администраторов магазина
        return true;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
