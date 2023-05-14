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
        readonly IWebHostEnvironment _env;

        public StoreAdministratorController(ApplicationContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> ChooseStore()
        {
            if(!(await _context.CPUs.AnyAsync()))
            {
                Store? store = await _context.Stores.Include(e => e.CPUs).FirstAsync();
                CPU cpu = new CPU()
                {
                    Socket = "Socket",
                    NumberOfCores = 16,
                    ClockRate = 3200,
                    Country = Country.Russia,
                    Model = "Intel Core i99",
                    Description = "Description",
                    Price = 999.99M
                };

                CPU cpu2 = new CPU()
                {
                    Socket = "Socket2",
                    NumberOfCores = 32,
                    ClockRate = 5400,
                    Country = Country.Japan,
                    Model = "AMD CPU name",
                    Description = "DEsckription",
                    Price = 10.00M
                };

                store.CPUs!.Add(cpu);
                store.CPUs!.Add(cpu2);
                await _context.SaveChangesAsync();
            }
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
            ViewBag.StoreId = storeId;
            return View();
        }

        [HttpGet]
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
            List<ProductAmountBindingModel> viewModel = null!;
            Store fullStoreInfo = null!;

            switch(category)
            {
                case (byte)Product.Categorys.CPU:
                    fullStoreInfo = await GetStoreWithLoadedCPUsDetails(searchFilter, storeId);
                    viewModel = ConvertStoreToCPUBindingModelList(fullStoreInfo).Cast<ProductAmountBindingModel>().ToList();
                    break;

                case (byte)Product.Categorys.GPU:
                    fullStoreInfo = await GetStoreWithLoadedCPUsDetails(searchFilter, storeId);
                    viewModel = ConvertStoreToGPUBindingModelList(fullStoreInfo).Cast<ProductAmountBindingModel>().ToList();
                    break;

                case (byte)Product.Categorys.RAM:
                    fullStoreInfo = await GetStoreWithLoadedCPUsDetails(searchFilter, storeId);
                    viewModel = ConvertStoreToRAMBindingModelList(fullStoreInfo).Cast<ProductAmountBindingModel>().ToList();
                    break;
                default:
                    return NotFound("Такой категории не существует");
            }

            ViewBag.Category = category;
            return View("ManageProducts", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ManageProduct(int? id, byte? category)
        {
            if(category == null || !Enum.IsDefined(typeof(Product.Categorys), category))
            {
                return NotFound("Такой категории не существует");
            }

            ViewBag.Category = Enum.GetName(typeof(Product.Categorys), category);

            //Если добавляется новый товар
            if(id == null)
            {
                return View();
            }

            //Если редактируется существующий
            ProductDetailsBindingModel? model = null;

            if((byte)category == (byte)Product.Categorys.CPU)
            {
                CPU? cpu = await _context.CPUs.AsNoTracking()
                                            .Include(cpu => cpu.Pictures)
                                            .FirstOrDefaultAsync(e => e.Id == id);

                if(cpu != null)
                {
                    model = new ProductDetailsBindingModel(cpu);
                }

            }
            else if((byte)category == (byte)Product.Categorys.GPU)
            {
                GPU? gpu = await _context.GPUs.AsNoTracking()
                                            .Include(gpu => gpu.Pictures)
                                            .FirstOrDefaultAsync(e => e.Id == id);

                if(gpu != null)
                {
                    model = new ProductDetailsBindingModel(gpu);
                }
            }
            else if((byte)category == (byte)Product.Categorys.CPU)
            {
                RAM? ram = await _context.RAMs.AsNoTracking()
                                            .Include(ram => ram.Pictures)
                                            .FirstOrDefaultAsync(e => e.Id == id);

                if(ram != null)
                {
                    model = new ProductDetailsBindingModel(ram);
                }
            }

            if(model == null)
            {
                return NotFound("Товар не был найден");
            }

            return View(model);
        }

        public async Task<IActionResult> ManageCPU(ProductDetailsBindingModel model)
        {
            return View("ManageProduct", model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult CPUDetailsForm(CPUDetails? model)
        {
            return PartialView(model);
        }
        public IActionResult GPUDetailsForm(GPUDetails model)
        {
            return PartialView(model);
        }
        public IActionResult RAMDetailsForm(RAMDetails model)
        {
            return PartialView(model);
        }



        async Task<Store> GetStoreWithLoadedCPUsDetails(string search, short storeId)
        {
            IQueryable<Store> query = _context.Stores.AsNoTracking();

            //Include CPUs that match the search filter
            query = query.Include(store => store.CPUs!.Where(cpu => cpu.Model.Contains(search)));

            //Also load first picture of each CPU
            query = query.Include(store => store.CPUs!.Where(cpu => cpu.Model.Contains(search)))
                            .ThenInclude(store => store.Pictures!.OrderBy(pic => pic.Id).Take(1));

            //Also load amount of CPU that contains in that store
            query = query.Include(store => store.CPUs!.Where(cpu => cpu.Model.Contains(search)))
                            .ThenInclude(cpu => cpu.Assortments);

            query = query.OrderBy(store => store.Id)
                        .Take(15)
                        .AsSplitQuery();

            Store store = (await query.FirstOrDefaultAsync(store => store.Id == storeId))!;

            return store;
        }
        async Task<Store> GetStoreWithLoadedGPUsDetails(string search, short storeId)
        {
            //Почти полное повторение GetStoreWithLoadedCPUDetails
            IQueryable<Store> query = _context.Stores.AsNoTracking();

            //Include GPUs that match the search filter
            query = query.Include(store => store.GPUs!.Where(gpu => gpu.Model.Contains(search)));

            //Also load first picture of each GPU
            query = query.Include(store => store.GPUs!.Where(gpu => gpu.Model.Contains(search)))
                            .ThenInclude(store => store.Pictures!.OrderBy(pic => pic.Id).Take(1));

            //Also load amount of GPU that contains in that store
            query = query.Include(store => store.GPUs!.Where(gpu => gpu.Model.Contains(search)))
                            .ThenInclude(gpu => gpu.Assortments);

            query = query.OrderBy(store => store.Id)
                        .Take(15)
                        .AsSplitQuery();

            Store store = (await query.FirstOrDefaultAsync(store => store.Id == storeId))!;

            return store;
        }

        async Task<Store> GetStoreWithLoadedRAMsDetails(string search, short storeId)
        {
            //Почти полное повторение GetStoreWithLoadedCPUDetails
            IQueryable<Store> query = _context.Stores.AsNoTracking();

            //Include RAMs that match the search filter
            query = query.Include(store => store.RAMs!.Where(ram => ram.Model.Contains(search)));

            //Also load first picture of each RAM
            query = query.Include(store => store.RAMs!.Where(ram => ram.Model.Contains(search)))
                            .ThenInclude(store => store.Pictures!.OrderBy(pic => pic.Id).Take(1));

            //Also load amount of RAM that contains in that store
            query = query.Include(store => store.RAMs!.Where(ram => ram.Model.Contains(search)))
                            .ThenInclude(ram => ram.Assortments);

            query = query.OrderBy(store => store.Id)
                        .Take(15)
                        .AsSplitQuery();

            Store store = (await query.FirstOrDefaultAsync(store => store.Id == storeId))!;

            return store;
        }

        public List<CPUAmountBindingModel> ConvertStoreToCPUBindingModelList(Store store)
        {
            List<CPUAmountBindingModel> models = new List<CPUAmountBindingModel>();

            foreach(CPU cpu in store.CPUs!)
            {
                CPUAmountBindingModel model = new CPUAmountBindingModel(cpu);

                models.Add(model);
            }

            return models;
        }

        public List<GPUAmountBindingModel> ConvertStoreToGPUBindingModelList(Store store)
        {
            //Почти полное повторение ConvertStoreToCPUBindingModelList
            List<GPUAmountBindingModel> models = new List<GPUAmountBindingModel>();

            foreach(GPU gpu in store.GPUs!)
            {
                GPUAmountBindingModel model = new GPUAmountBindingModel(gpu);

                models.Add(model);
            }

            return models;
        }

        public List<RAMAmountBindingModel> ConvertStoreToRAMBindingModelList(Store store)
        {
            //Почти полное повторение ConvertStoreToCPUBindingModelList
            List<RAMAmountBindingModel> models = new List<RAMAmountBindingModel>();

            foreach(RAM ram in store.RAMs!)
            {
                RAMAmountBindingModel model = new RAMAmountBindingModel(ram);

                models.Add(model);
            }

            return models;
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