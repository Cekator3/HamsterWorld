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

            ViewBag.StoreId = storeId;
            ViewBag.Category = category;
            return View("ManageProducts", viewModel);
        }

        public async Task<IActionResult> ManageProduct(int? id, byte? category, short storeId)
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
                model.StoreId = storeId;
                model.Category = (byte)category;
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

            model.StoreId = storeId;
            model.Category = (byte)category;
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
            (bool filesAreFine, string Message) = IsUploadedFilesNotBreakingRules(productDetails.NewPhotos!);
            if(!filesAreFine)
            {
                ModelState.AddModelError(nameof(productDetails.NewPhotos), Message);
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


            //Edit existing product info
            if(productDetails.Id >= 0)
            {
                if(productDetails.CpuDetails != null)
                {
                    CPU oldCPUInfo = (await _context.CPUs
                                            .Include(cpu => cpu.Pictures)
                                            .FirstOrDefaultAsync(cpu => cpu.Id == productDetails.Id))!;
                    
                    ApplyNewCharacteristicInfoToProduct(oldCPUInfo, productDetails);

                    PutNewImagesIntoProductInfo(oldCPUInfo, newPhotosFilesNames);

                    await _context.SaveChangesAsync();
                }
                else if(productDetails.GpuDetails != null)
                {
                    GPU oldGPUInfo = (await _context.GPUs
                                            .Include(e => e.Pictures)
                                            .FirstOrDefaultAsync(e => e.Id == productDetails.Id))!;
                    
                    ApplyNewCharacteristicInfoToProduct(oldGPUInfo, productDetails);

                    PutNewImagesIntoProductInfo(oldGPUInfo, newPhotosFilesNames);

                    await _context.SaveChangesAsync();
                }
                else if(productDetails.RamDetails != null)
                {
                    RAM oldRAMInfo = (await _context.RAMs
                                            .Include(e => e.Pictures)
                                            .FirstOrDefaultAsync(e => e.Id == productDetails.Id))!;
                    
                    ApplyNewCharacteristicInfoToProduct(oldRAMInfo, productDetails);

                    PutNewImagesIntoProductInfo(oldRAMInfo, newPhotosFilesNames);

                    await _context.SaveChangesAsync();
                }
            } 
            // add new product info
            else
            {
                if(productDetails.CpuDetails != null)
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

                    PutNewImagesIntoProductInfo(newCPUInfo, newPhotosFilesNames);

                    await DbUsageTools.TryAddNewProductToDatabase(_context, newCPUInfo);
                }
                if(productDetails.GpuDetails != null)
                {
                    GPU newGPUInfo = new GPU()
                    {
                        Country = productDetails.Country,
                        Model = productDetails.Model,
                        Description = productDetails.Description,
                        Price = productDetails.Price,
                        VRAM = productDetails.GpuDetails.VRAM,
                        MemoryType = productDetails.GpuDetails.MemoryType,
                        Pictures = new List<ProductPicture>()
                    };

                    PutNewImagesIntoProductInfo(newGPUInfo, newPhotosFilesNames);

                    await DbUsageTools.TryAddNewProductToDatabase(_context, newGPUInfo);
                }
                if(productDetails.RamDetails != null)
                {
                    RAM newRamInfo = new RAM()
                    {
                        Country = productDetails.Country,
                        Model = productDetails.Model,
                        Description = productDetails.Description,
                        Price = productDetails.Price,
                        AmountOfMemory = productDetails.RamDetails.AmountOfMemory,
                        MemoryType = productDetails.RamDetails.MemoryType,
                        Pictures = new List<ProductPicture>()
                    };

                    PutNewImagesIntoProductInfo(newRamInfo, newPhotosFilesNames);

                    await DbUsageTools.TryAddNewProductToDatabase(_context, newRamInfo);
                }
            }
            return RedirectToAction("ManageProducts", new { storeId = productDetails.StoreId, category = productDetails.Category});
        }

		[HttpPost]
        public async Task<IActionResult> ChangeProductAmount(short? storeId, int? productId, int? amount)
        {
            if(storeId == null || productId == null || amount == null)
            {
                return BadRequest("Неверный формат данных");
            }

            Store? store = await _context.Stores.FindAsync(storeId);
            if(store == null)
            {
                return NotFound("Такого магазина не существует");
            }
            if(!(await IsUserAdminOfStore(store)))
            {
                return Unauthorized("Вы не являетесь администратором этого магазина");
            }

            Assortment? assortment=  await _context.Assortments.FindAsync(storeId, productId);
            if(assortment == null)
            {
                return NotFound("Товара с таким Id не было найдено");
            }

            assortment.Amount = (int)amount;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> ManagePictures(int[] picturesToDelete)
        {
            if(picturesToDelete.Length == 0)
            {
                return BadRequest("Не было предоставлено ни одного pictureId");
            }

            //obtaining fileNames from db
            List<string> fileNamesOfPictures = await _context.ProductsPictures
                                                            .AsNoTracking()
                                                            .Where(img => picturesToDelete.Contains(img.Id))
                                                            .Select(img => img.FileName)
                                                            .ToListAsync();

            //deleting pictures from db 
            await _context.ProductsPictures.Where(img => picturesToDelete.Contains(img.Id)).ExecuteDeleteAsync();

            DeleteProductPicturesFromFileSystem(fileNamesOfPictures);

            return Ok();
        }

        [HttpPut]
        public async Task<IActionResult> ManagePictures(List<PicturesOrderInfo> picturesOrderInfo)
        {
            if(picturesOrderInfo.Count == 0)
            {
                return BadRequest("Неверный формат входных данных");
            }
            List<int> picturesIds = picturesOrderInfo.Select(img => img.Id).ToList();

            //Obtaining pictures whose Id is in the request
            List<ProductPicture> pictures = await _context.ProductsPictures
                                                            .Where(img => picturesIds.Contains(img.Id))
                                                            .ToListAsync();

            if(pictures.Count == 0)
            {
                return BadRequest("Ни один из предоставленных Id не соответствует ни одной сущности из базы данных");
            }

            foreach(ProductPicture picture in pictures)
            {
                picture.OrderNumber = picturesOrderInfo.First(img => img.Id == picture.Id).OrderNumber;
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }



        async Task<Store> GetStoreWithLoadedCPUsDetails(string searchFilter, short storeId)
        {
            string search = searchFilter.ToLower();
            IQueryable<Store> query = _context.Stores.AsNoTracking();

            //Include CPUs that match the search filter
            query = query.Include(store => store.CPUs!.Where(cpu => cpu.Model.ToLower().Contains(search)));

            //Also load first picture of each CPU
            query = query.Include(store => store.CPUs!.Where(cpu => cpu.Model.ToLower().Contains(search)))
                            .ThenInclude(store => store.Pictures!.OrderBy(pic => pic.OrderNumber).Take(1));

            //Also load amount of CPU that contains in that store
            query = query.Include(store => store.CPUs!.Where(cpu => cpu.Model.ToLower().Contains(search)))
                            .ThenInclude(cpu => cpu.Assortments!.Where(assortment => assortment.StoreId == storeId));

            query = query.OrderBy(store => store.Id)
                        .Take(15)
                        .AsSplitQuery();

            Store store = (await query.FirstOrDefaultAsync(store => store.Id == storeId))!;

            return store;
        }
        async Task<Store> GetStoreWithLoadedGPUsDetails(string searchFilter, short storeId)
        {
            //Почти полное повторение GetStoreWithLoadedCPUDetails
            string search = searchFilter.ToLower();
            IQueryable<Store> query = _context.Stores.AsNoTracking();

            //Include GPUs that match the search filter
            query = query.Include(store => store.GPUs!.Where(gpu => gpu.Model.ToLower().Contains(search)));

            //Also load first picture of each GPU
            query = query.Include(store => store.GPUs!.Where(gpu => gpu.Model.ToLower().Contains(search)))
                            .ThenInclude(store => store.Pictures!.OrderBy(pic => pic.OrderNumber).Take(1));

            //Also load amount of GPU that contains in that store
            query = query.Include(store => store.GPUs!.Where(gpu => gpu.Model.ToLower().Contains(search)))
                            .ThenInclude(gpu => gpu.Assortments!.Where(assortment => assortment.StoreId == storeId));

            query = query.OrderBy(store => store.Id)
                        .Take(15)
                        .AsSplitQuery();

            Store store = (await query.FirstOrDefaultAsync(store => store.Id == storeId))!;

            return store;
        }

        async Task<Store> GetStoreWithLoadedRAMsDetails(string searchFilter, short storeId)
        {
            //Почти полное повторение GetStoreWithLoadedCPUDetails
            string search = searchFilter.ToLower();
            IQueryable<Store> query = _context.Stores.AsNoTracking();

            //Include RAMs that match the search filter
            query = query.Include(store => store.RAMs!.Where(ram => ram.Model.ToLower().Contains(search)));

            //Also load first picture of each RAM
            query = query.Include(store => store.RAMs!.Where(ram => ram.Model.ToLower().Contains(search)))
                            .ThenInclude(store => store.Pictures!.OrderBy(pic => pic.OrderNumber).Take(1));

            //Also load amount of RAM that contains in that store
            query = query.Include(store => store.RAMs!.Where(ram => ram.Model.ToLower().Contains(search)))
                            .ThenInclude(ram => ram.Assortments!.Where(assortment => assortment.StoreId == storeId));

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

        public void PutNewImagesIntoProductInfo(Product product, List<string> newPhotosFilesNames)
        {
            ushort lastPictureOrderNumber = 1;

            if(product.Pictures!.Count != 0)
            {
                lastPictureOrderNumber = (ushort)(product.Pictures.Max(e => e.OrderNumber) + 1);
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

        public CPU ApplyNewCharacteristicInfoToProduct(CPU oldCpuInfo, ProductDetailsBindingModel newCpuInfo)
        {
            oldCpuInfo.Country = newCpuInfo.Country;
            oldCpuInfo.Model = newCpuInfo.Model;
            oldCpuInfo.Description = newCpuInfo.Description;
            oldCpuInfo.Price = newCpuInfo.Price;

            oldCpuInfo.ClockRate = newCpuInfo.CpuDetails!.ClockRate;
            oldCpuInfo.NumberOfCores = newCpuInfo.CpuDetails.NumberOfCores;
            oldCpuInfo.Socket = newCpuInfo.CpuDetails.Socket;

            return oldCpuInfo;
        }
        public GPU ApplyNewCharacteristicInfoToProduct(GPU oldGpuInfo, ProductDetailsBindingModel newGpuInfo)
        {
            oldGpuInfo.Country = newGpuInfo.Country;
            oldGpuInfo.Model = newGpuInfo.Model;
            oldGpuInfo.Description = newGpuInfo.Description;
            oldGpuInfo.Price = newGpuInfo.Price;

            oldGpuInfo.VRAM = newGpuInfo.GpuDetails!.VRAM;
            oldGpuInfo.MemoryType = newGpuInfo.GpuDetails.MemoryType;

            return oldGpuInfo;
        }
        public RAM ApplyNewCharacteristicInfoToProduct(RAM oldRamInfo, ProductDetailsBindingModel newRamInfo)
        {
            oldRamInfo.Country = newRamInfo.Country;
            oldRamInfo.Model = newRamInfo.Model;
            oldRamInfo.Description = newRamInfo.Description;
            oldRamInfo.Price = newRamInfo.Price;

            oldRamInfo.AmountOfMemory = newRamInfo.RamDetails!.AmountOfMemory;
            oldRamInfo.MemoryType = newRamInfo.RamDetails.MemoryType;

            return oldRamInfo;
        }

        public void DeleteProductPicturesFromFileSystem(List<string> fileNames)
        {
            string pathToProductsPictures = Path.Combine(_env.ContentRootPath, "wwwroot/Images/Products/");
            if( !Directory.Exists(pathToProductsPictures) )
            {
                throw new Exception("Каталога wwwroot/Images/Products/ не существует");
            }
            
            foreach(string fileName in fileNames)
            {
                string imgPath = Path.Combine(pathToProductsPictures, fileName);

                FileInfo picture = new FileInfo(imgPath);
                if(picture.Exists)
                {
                    picture.Delete();
                }
            }
        }
    }
}