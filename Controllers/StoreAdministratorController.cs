using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using HamsterWorld.Models;
using HamsterWorld.DatabaseUtilities;

namespace HamsterWorld.Controllers
{
    [Authorize(Policy = Role.StoreAdminRoleName)]
    public class StoreAdministratorController : Controller
    {
        readonly ApplicationContext _context;
        readonly IWebHostEnvironment _env;
        readonly long _fileSizeLimit;

        public StoreAdministratorController(ApplicationContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            _fileSizeLimit = config.GetValue<long>("FileSizeLimit");
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

            //preparing viewModel (loading all CPUs/GPUs/... of store and their amount at this store)
            List<ProductAmountBindingModel> viewModel = null!;
            Store fullStoreInfo = null!;

            switch(category)
            {
                case (byte)Product.Categorys.CPU:
                    fullStoreInfo = await GetStoreWithLoadedCPUsDetails(searchFilter, storeId);
                    viewModel = ConvertStoreToCPUBindingModelList(fullStoreInfo).Cast<ProductAmountBindingModel>().ToList();
                    break;

                case (byte)Product.Categorys.GPU:
                    fullStoreInfo = await GetStoreWithLoadedGPUsDetails(searchFilter, storeId);
                    viewModel = ConvertStoreToGPUBindingModelList(fullStoreInfo).Cast<ProductAmountBindingModel>().ToList();
                    break;

                case (byte)Product.Categorys.RAM:
                    fullStoreInfo = await GetStoreWithLoadedRAMsDetails(searchFilter, storeId);
                    viewModel = ConvertStoreToRAMBindingModelList(fullStoreInfo).Cast<ProductAmountBindingModel>().ToList();
                    break;
                default:
                    return NotFound("Такой категории не существует");
            }

            ViewBag.Category = category;
            return View("ManageProducts", viewModel);
        }

        public async Task<IActionResult> ManageProduct(int? id, byte? category)
        {
            if(category == null || !Enum.IsDefined(typeof(Product.Categorys), category))
            {
                return NotFound("Такой категории не существует");
            }

            ProductDetailsBindingModel? model = null;

            //Если добавляется новый товар
            if(id == null)
            {
                model = new ProductDetailsBindingModel();
                model.Id = -1;
                switch((byte)category)
                {
                    case (byte)Product.Categorys.CPU:
                        model.CpuDetails = new CPUDetails();
                        break;
                    
                    case (byte)Product.Categorys.GPU:
                        model.GpuDetails = new GPUDetails();
                        break;
                    
                    case (byte)Product.Categorys.RAM:
                        model.RamDetails = new RAMDetails();
                        break;
                }

                return View(model);
            }

            //Если редактируется существующий
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
            else if((byte)category == (byte)Product.Categorys.RAM)
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
                return NotFound("Товар не найден");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ManageProduct(ProductDetailsBindingModel productDetails)
        {
            //Validating model
            productDetails.Country = productDetails.Country.ToUpperInvariant();
            if(!(await IsCountryExist(productDetails.Country)))
            {
                ModelState.AddModelError(nameof(productDetails.Country), "Такой страны в базе данных не было найдено");
            }
            if(productDetails.Price < 0)
            {
                ModelState.AddModelError(nameof(productDetails.Country), "Цена не может быть отрицательной");
            }
            (bool filesAreFine, string Message) = IsUploadedFilesNotBreakingRules(productDetails.NewPhotos);
            if(!filesAreFine)
            {
                ModelState.AddModelError(nameof(productDetails.Country), Message);
            }
            if(productDetails.Id >= 0)
            {
                if( !(await IsProductExist(productDetails.Id)) )
                {
                    ModelState.AddModelError(nameof(productDetails.Model), "Товара с таким Id не существует");
                }
            }
            if(await IsProductModelNameHasDuplicates(productDetails.Model, productDetails.Id))
            {
                ModelState.AddModelError(nameof(productDetails.Model), "Другая техника с таким же названием модели уже существует");
            }
            if(!ModelState.IsValid)
            {
                return View(productDetails);
            }


            List<string> newPhotosFilesNames = await SaveNewPhotosToImageDirectory(productDetails);


            //Aquire specific info
            if(productDetails.CpuDetails != null)
            {
                //Редактируется существующий продукт
                if(productDetails.Id >= 0)
                {
                    CPU oldCPUInfo = (await _context.CPUs
                                            .Include(cpu => cpu.Pictures)
                                            .FirstOrDefaultAsync(cpu => cpu.Id == productDetails.Id))!;
                    
                    oldCPUInfo.Country = productDetails.Country;
                    oldCPUInfo.Model = productDetails.Model;
                    oldCPUInfo.Description = productDetails.Description;
                    oldCPUInfo.Price = productDetails.Price;
                    oldCPUInfo.ClockRate = productDetails.CpuDetails.ClockRate;
                    oldCPUInfo.NumberOfCores = productDetails.CpuDetails.NumberOfCores;
                    oldCPUInfo.Socket = productDetails.CpuDetails.Socket;

                    PutNewImagesIntoExistingProductEntityOfDatabase(oldCPUInfo, newPhotosFilesNames);

                    await _context.SaveChangesAsync();
                }
                else
                {
                    CPU newCPUInfo = new CPU()
                    {
                        Country = productDetails.Country,
                        Model = productDetails.Model,
                        Description = productDetails.Description,
                        Price = productDetails.Price,
                        ClockRate = productDetails.CpuDetails.ClockRate,
                        NumberOfCores = productDetails.CpuDetails.NumberOfCores,
                        Socket = productDetails.CpuDetails.Socket,
                        Pictures = new List<ProductPicture>()
                    };

                    PutNewImagesIntoExistingProductEntityOfDatabase(newCPUInfo, newPhotosFilesNames);

                    await DbUsageTools.TryAddNewProductToDatabase(_context, newCPUInfo);
                }
            }
            else if(productDetails.GpuDetails != null)
            {

            }
            else if(productDetails.RamDetails != null)
            {

            }
            return RedirectToAction("ChooseStore");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }



        async Task<Store> GetStoreWithLoadedCPUsDetails(string search, short storeId)
        {
            IQueryable<Store> query = _context.Stores.AsNoTracking();

            //Include CPUs that match the search filter
            query = query.Include(store => store.CPUs!.Where(cpu => cpu.Model.Contains(search)));

            //Also load first picture of each CPU
            query = query.Include(store => store.CPUs!.Where(cpu => cpu.Model.Contains(search)))
                            .ThenInclude(store => store.Pictures!.OrderBy(pic => pic.OrderNumber).Take(1));

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
                            .ThenInclude(store => store.Pictures!.OrderBy(pic => pic.OrderNumber).Take(1));

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
                            .ThenInclude(store => store.Pictures!.OrderBy(pic => pic.OrderNumber).Take(1));

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

        async Task<bool> IsCountryExist(string code)
        {
            return (await _context.Countries.FindAsync(code)) != null;
        }

        (bool, string) IsUploadedFilesNotBreakingRules(IFormFileCollection files)
        {
            string[] permittedExtensions = { ".jpg", ".jpeg", ".png"};
            foreach(FormFile file in files!)
            {
                if(file.Length > _fileSizeLimit)
                {
                    return (false, "Файл не может быть более 20 мб");
                }

                string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if(!permittedExtensions.Contains(fileExtension))
                {
                    return (false, "Неприемлимый тип файла");
                }
                if(file.Length == 0)
                {
                    return (false, "Произошла ошибка передачи файлов на сервер");
                }
            }
            return (true, "");
        }

        async Task<bool> IsProductExist(int productId)
        {
            return await _context.Products.FindAsync(productId) != null;
        }

        async Task<bool> IsProductModelNameHasDuplicates(string modelName, int productId)
        {
            return await _context.Products
                                .Where(prod => (prod.Id != productId) && (prod.Model == modelName))
                                .FirstOrDefaultAsync() 
                                != null;
        }
        async Task<bool> IsProductModelNameHasDuplicates(string modelName)
        {
            return await _context.Products
                                .Where(prod => prod.Model == modelName)
                                .FirstOrDefaultAsync() 
                                != null;
        }
        
        async Task<List<string>> SaveNewPhotosToImageDirectory(ProductDetailsBindingModel model)
        {
            string pathToProductsPictures = Path.Combine(_env.ContentRootPath, "wwwroot/Images/Products/");

            List<string> picturesFileNames = new List<string>();

            foreach(FormFile file in model.NewPhotos!)
            {
                string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                string fileName = Path.GetRandomFileName();
                string filePath = Path.Combine(pathToProductsPictures, fileName + extension);

                //check for duplicates
                while(System.IO.File.Exists(filePath))
                {
                    fileName = Path.GetRandomFileName();
                    filePath = Path.Combine(pathToProductsPictures, fileName + extension);
                }

                picturesFileNames.Add(fileName + extension);

                using(FileStream stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);
                }
            }

            return picturesFileNames;
        }

        public void PutNewImagesIntoExistingProductEntityOfDatabase(Product product, List<string> newPhotosFilesNames)
        {
            ushort lastPictureOrderNumber = 1;

            if(product.Pictures!.Count != 0)
            {
                lastPictureOrderNumber = product.Pictures.Max(e => e.OrderNumber);
            }

            foreach(string fileName in newPhotosFilesNames)
            {
                ProductPicture picture = new ProductPicture()
                {
                    FileName = fileName,
                    OrderNumber = lastPictureOrderNumber
                };
                product.Pictures!.Add(picture);

                lastPictureOrderNumber++;
            }
        }
    }
}