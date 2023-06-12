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
        ManageStoreBindingModel model = new ManageStoreBindingModel();
        return View("ManageStore", model);
    }

    public async Task<IActionResult> ChangeStoreInfo(short id)
    {
        //Find store by id
        Store? store = await _context.Stores.FindAsync(id);
        if(store == null)
        {
            return NotFound("Такого филиала не существует");
        }

        ManageStoreBindingModel model = new ManageStoreBindingModel(store);
        return View("ManageStore", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageStore(ManageStoreBindingModel model)
    {
        if(IsWorkingScheduleTooShort(model.OpeningTime, model.ClosingTime))
        {
            ModelState.AddModelError(nameof(model.OpeningTime), "Рабочее время не может быть меньше четырёх часов");
        }
        if(!ModelState.IsValid)
        {
            return View(model);
        }

        bool IsSuccess = false;
        string Message = "";
        if(model.StoreId != null)
        {
            (IsSuccess, Message) = await TryApplyChangesToStoreInfo(model);
        }
        else
        {
            (IsSuccess, Message) = await TryAddNewStoreToDatabase(model);
        }

        if(!IsSuccess)
        {
            ModelState.AddModelError(nameof(model.Address), Message);
        }
        if(!ModelState.IsValid)
        {
            return View(model);
        }

        return Redirect("ManageStores");
    }

    public async Task<IActionResult> DeleteStore(short id)
    {
        await DbUsageTools.RemoveStoreFromDatabase(_context, id);

        return RedirectToAction("ManageStores");
    }

    public async Task<IActionResult> ChangeStoreAdministrators(short storeId)
    {
        if (storeId <= 0)
        {
            return NotFound("Магазина с таким Id не существует");
        }

        List<int>? adminsIdentificators = await GetListOfIdsOfAllAdministratorsOfStore(storeId);
        if(adminsIdentificators == null)
        {
            return NotFound("Магазина с таким Id не существует");
        }

        //Creating view model
        List<ChangeStoreAdministratorsBindingModel> allAdmins = await GetListOfChangeStoreAdministratorsBindingModel(adminsIdentificators);

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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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

    async Task<(bool, string)> TryAddNewStoreToDatabase(ManageStoreBindingModel model)
    {
        Store? store = await TryFindStoreInfoWithDadataService(model);

        //Такое может произойти только, когда пользователь ввёл плохой адрес
        if (store == null)
        {
            return (false, "Не удалось распознать адрес магазина");
        }

        return await DbUsageTools.TryAddStoreToDatabase(_context, store);
    }


    async Task<(bool, string)> TryApplyChangesToStoreInfo(ManageStoreBindingModel model)
    {
        Store? oldStoreInfo = await _context.Stores.FindAsync(model.StoreId);
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
            if (await _context.Stores.AsNoTracking().AnyAsync(e => e.Address == newStoreInfo.Address || e.Coordinates == newStoreInfo.Coordinates))
            {
                return (false, "Другой магазин с такими координатами или адресом уже существует");
            }
        }

        oldStoreInfo.Name = newStoreInfo.Name;
        oldStoreInfo.OpeningTime = newStoreInfo.OpeningTime;
        oldStoreInfo.ClosingTime = newStoreInfo.ClosingTime;
        oldStoreInfo.Address = newStoreInfo.Address;
        oldStoreInfo.Coordinates = newStoreInfo.Coordinates;
        await _context.SaveChangesAsync();

        return (true, "");
    }

    async Task<Store?> TryFindStoreInfoWithDadataService(ManageStoreBindingModel model)
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

    async Task<List<int>?> GetListOfIdsOfAllAdministratorsOfStore(int storeId)
    {
        return (await _context.Stores.AsNoTracking()
                                    .Include(e => e.Administrators)
                                    .Select(e => new { e.Id, e.Administrators })
                                    .FirstOrDefaultAsync(e => e.Id == storeId))
                                    ?.Administrators
                                    ?.Select(e => e.Id)
                                    .ToList();
    }

    async Task<List<ChangeStoreAdministratorsBindingModel>> GetListOfChangeStoreAdministratorsBindingModel(List<int> adminsIdentificators)
    {
        return await _context.Users.AsNoTracking()
                                    .Where(e => e.RoleId == Role.STORE_ADMIN)
                                    .Select(e => new ChangeStoreAdministratorsBindingModel()
                                    {
                                        AdminId = e.Id,
                                        Login = e.Login,
                                        Email = e.Email,
                                        IsAdminOfThisStore = adminsIdentificators.Contains(e.Id)
                                    })
                                    .ToListAsync();
    }
}