using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using HamsterWorld.Models;

namespace HamsterWorld.Controllers
{
    [Authorize(Policy = Role.StoreAdminRoleName)]
    public class StoreAdministratorController : Controller
    {
        readonly ApplicationContext _context;

        public StoreAdministratorController(ApplicationContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ChooseStore()
        {
            //getting user's id from auth cookies
            int userId = GetCurrentUserIdFromAuthCookie();

            //getting user from database
            var user = await _context.Users.AsNoTracking()
                                            .Include(e => e.AdministratingStores)
                                            .Select(e => new {e.Id, e.AdministratingStores})
                                            .FirstOrDefaultAsync(e => e.Id == userId);
            if(user == null)
            {
                return BadRequest("У вас испорченные куки");
            }

            //getting all stores that are administrered by this user
            List<Store> stores = user.AdministratingStores!;

            return View(stores);
        }

        public IActionResult ChooseCategory(short storeId)
        {
            // CPU cpu1 = new CPU();
            // cpu1.ClockRate = 2500;
            // cpu1.Country = Country.Russia;
            // cpu1.Id = 1;
            // cpu1.Model = "Intel Core i5";
            // cpu1.NumberOfCores = 6;
            // cpu1.Pictures = new List<ProductPicture>(){ new ProductPicture(){ Id = 1, Path = "~/Images/Intel_Core_i5.jpg", OrderNumber = 1}};
            // cpu1.Price = 15_500;
            // cpu1.Socket = "LGA";
            // cpu1.Description = "adsf";
            // _context.CPUs.Add(cpu1);

            // Store store = _context.Stores.Include(e => e.CPUs).FirstOrDefault(e => e.Id == storeId);
            // store.CPUs.Add(cpu1);
            // CPU cpu2 = new CPU();
            // cpu2.ClockRate = 2600;
            // cpu2.Country = Country.Russia;
            // cpu2.Id = 2;
            // cpu2.Model = "Intel Core i6";
            // cpu2.NumberOfCores = 6;
            // cpu2.Pictures = new List<ProductPicture>(){ new ProductPicture(){ Id = 2, Path = "~/Images/Intel_Core_i6.jpg", OrderNumber = 1}};
            // cpu2.Price = 16_600;
            // cpu2.Socket = "LGA6";
            // cpu2.Description = "adsf";
            // _context.CPUs.Add(cpu2);
            // store.CPUs.Add(cpu2);
            // _context.SaveChanges();
            ViewBag.StoreId = storeId;
            return View();
        }

        public async Task<IActionResult> ManageProducts(short storeId, byte category, string searchFilter = "")
        {
            searchFilter = searchFilter.Trim(' ');

            //First query to database to check if store exist and User have all needed rights
            Store? store = await _context.Stores.FindAsync(storeId);
            if(store == null)
            {
                return NotFound("Такого магазина не существует");
            }
            if( !(await IsUserAdminOfStore(store)))
            {
                return Unauthorized();
            }

            //Loading product's details to context, preparing BindingModel and selecting view
            List<ProductBindingModel> viewModel = null!;
            switch(category)
            {
                case (byte)Product.Categorys.CPU:
                    Store fullStoreInfo = await GetStoreWithLoadedCPUsDetails(searchFilter, storeId);
                    viewModel  = ConvertStoreToCPUBindingModelList(fullStoreInfo).Cast<ProductBindingModel>().ToList();
                    break;

                case (byte)Product.Categorys.GPU:
                    break;

                case (byte)Product.Categorys.RAM:
                    break;

                default:
                    return NotFound("Такой категории не существует");
            }
            return View("ManageProducts", viewModel);
        }

        public IActionResult AddNewCPU()
        {
            return View();
        }

        async Task<Store> GetStoreWithLoadedCPUsDetails(string search, short storeId)
        {
            IQueryable<Store> query = _context.Stores.AsNoTracking();

            //Include CPUs that match the search filter
            query = query.Include(e => e.CPUs!.Where(cpu => cpu.Model.Contains(search)));

            //Also load first picture of each CPU
            query = query.Include(e => e.CPUs!.Where(cpu => cpu.Model.Contains(search)))
                            .ThenInclude(e => e.Pictures!.OrderBy(e => e.Id).Take(1));

            //Also load amount of CPU that contains in that store
            query = query.Include(e => e.CPUs!.Where(cpu => cpu.Model.Contains(search)))
                            .ThenInclude(e => e.Assortments);

            query = query.OrderBy(e => e.Id)
                        .Take(15)
                        .AsSplitQuery();

            Store store = (await query.FirstOrDefaultAsync(e => e.Id == storeId))!;

            return store;
        }

        // public async Task LoadGPUsDetails(string search, ref IQueryable<Store> query)
        // {
            //То же самое, что и CPU, только заменить CPUs на GPUs
        // }

        // public async Task LoadRAMsDetails(Store store, string search)
        // {
            //То же самое, что и CPU, только заменить CPUs на RAMs
        // }

        public List<CPUBindingModel> ConvertStoreToCPUBindingModelList(Store store)
        {
            List<CPUBindingModel> models = new List<CPUBindingModel>();

            foreach(CPU cpu in store.CPUs)
            {
                CPUBindingModel model = new CPUBindingModel(cpu);

                models.Add(model);
            }

            return models;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        async Task<bool> IsUserAdminOfStore(Store store)
        {
            int userId = GetCurrentUserIdFromAuthCookie();

            //Loads to context current user's info if he is admin of this store
            await _context.Entry(store)
                            .Collection(e => e.Administrators!)
                            .Query()
                            .AsSplitQuery()
                            .FirstOrDefaultAsync(e => e.Id == userId);

            return store.Administrators!.Count == 1;
        }

        int GetCurrentUserIdFromAuthCookie()
        {
            string userId = User.FindFirst(e => e.Type == ClaimTypes.NameIdentifier)!.Value;
            return int.Parse(userId);
        }
    }
}