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

            List<Store> stores = await GetStoresAdministratedByUser(userId);

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
            searchFilter = searchFilter.Trim(' ').ToLower();

            //First query to database to check if store exist and User have all needed rights
            Store? store = await _context.Stores.FindAsync(storeId);
            if(store == null)
            {
                return NotFound("Такого магазина не существует");
            }

            if( !(await IsUserAdminOfStore(store)))
            {
                return Unauthorized("Вы не являетесь администратором этого магазина");
            }


            //preparing viewModel (loading all CPUs/GPUs/... of store and their amount at this store)
            List<ProductAmountBindingModel> viewModel = null!;

            switch(category)
            {
                case (byte)Product.Categorys.CPU:
                    await LoadCPUsDetailsToStoreEntity(searchFilter, store);
                    viewModel = ConvertStoreToCPUBindingModelList(store);
                    break;

                case (byte)Product.Categorys.GPU:
                    await LoadGPUsDetailsToStoreEntity(searchFilter, store);
                    viewModel = ConvertStoreToGPUBindingModelList(store);
                    break;

                case (byte)Product.Categorys.RAM:
                    await LoadRAMsDetailsToStoreEntity(searchFilter, store);
                    viewModel = ConvertStoreToRAMBindingModelList(store);
                    break;
                default:
                    return NotFound("Такой категории не существует");
            }

            ViewBag.StoreId = storeId;
            ViewBag.Category = category;
            return View("ManageProducts", viewModel);
        }

        public async Task<IActionResult> UpdateProduct(int productId, byte? category, short storeId)
        {
            if(!IsCategoryExist(category))
            {
                return NotFound("Такой категории не существует");
            }

            Product? product = await _context.Products.AsNoTracking()
                                                    .Include(e => e.Pictures)
                                                    .FirstOrDefaultAsync(e => e.Id == productId);
            if(product == null)
            {
                return NotFound("Товар с таким id не был обнаружен");
            }

            ProductDetailsBindingModel model = GetProductDetailsBindingModelFromProduct(product, storeId, (byte)category!);

            return View("ManageProduct", model);
        }

        public IActionResult AddNewProduct(byte? category, short storeId)
        {
            if(!IsCategoryExist(category))
            {
                return NotFound("Такой категории не существует");
            }

            ProductDetailsBindingModel model = new ProductDetailsBindingModel();

            switch((byte)category!)
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
                default:
                    throw new NotImplementedException();
            }

            model.StoreId = storeId;
            model.Category = (byte)category;

            return View("ManageProduct", model);
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

            bool isProductExist = productDetails.Id != null && await IsProductExist((int)productDetails.Id);
            if(isProductExist)
            {
                if(await IsProductModelNameHasDuplicates(productDetails.Model, (int)productDetails.Id!))
                {
                    ModelState.AddModelError(nameof(productDetails.Model), "Другая техника с таким же названием модели уже существует");
                }
            }

            if(!ModelState.IsValid)
            {
                return View(productDetails);
            }


            List<string> newPhotosFilesNames = await SaveNewPhotosToImageDirectory(productDetails);

            if(isProductExist)
            {
                await SaveChangesToExistingProduct(productDetails, newPhotosFilesNames);
            } 
            else
            {
                await SaveNewProduct(productDetails, newPhotosFilesNames);
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
            if(amount < 0)
            {
                return BadRequest("Количество товара не может быть отрицательным");
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

            Assortment? assortment = await _context.Assortments.FindAsync(storeId, productId);
            if(assortment == null)
            {
                return NotFound("Товар с таким Id не найден");
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
            List<ProductPicture> existingPictures = await _context.ProductsPictures
                                                                .Where(img => picturesIds.Contains(img.Id))
                                                                .ToListAsync();

            if(existingPictures.Count == 0)
            {
                return BadRequest("Ни один из предоставленных Id не соответствует ни одной сущности из базы данных");
            }

            foreach(ProductPicture picture in existingPictures)
            {
                picture.OrderNumber = picturesOrderInfo.First(img => img.Id == picture.Id).OrderNumber;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateConcurrencyException ex)
            {
                return Conflict("Некоторые фото были удалены до того, как их порядок был изменён");
            }

            return Ok();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        async Task<List<Store>> GetStoresAdministratedByUser(int userId)
        {
            return (await _context.Users.AsNoTracking()
                                        .Include(e => e.AdministratingStores)
                                        .Select(e => new {e.Id, e.AdministratingStores})
                                        .FirstAsync(e => e.Id == userId))
                                        .AdministratingStores!;
        }

        bool IsCategoryExist(byte? category)
        {
            return category != null && Enum.IsDefined(typeof(Product.Categorys), category);
        }

        async Task LoadCPUsDetailsToStoreEntity(string searchFilter, Store store)
        {
            //Чтобы подгрузить товары (CPUs), ef core автоматически подгружает промежуточную таблицу assortments,
            //Поэтому её подгружать отдельно нет смысла
            await _context.Entry(store)
                        .Collection(s => s.CPUs!)
                        .Query()
                        .Where(cpu => cpu.Model.ToLower().Contains(searchFilter))
                        .LoadAsync();

            foreach (CPU cpu in store.CPUs!)
            {
                await _context.Entry(cpu)
                                .Collection(e => e.Pictures!)
                                .Query()
                                .OrderBy(pic => pic.OrderNumber)
                                .Take(1)
                                .LoadAsync();
            }
        }
        async Task LoadGPUsDetailsToStoreEntity(string searchFilter, Store store)
        {
            await _context.Entry(store)
                        .Collection(s => s.GPUs!)
                        .Query()
                        .Where(gpu => gpu.Model.ToLower().Contains(searchFilter))
                        .LoadAsync();

            foreach (GPU gpu in store.GPUs!)
            {
                await _context.Entry(gpu)
                                .Collection(e => e.Pictures!)
                                .Query()
                                .OrderBy(pic => pic.OrderNumber)
                                .Take(1)
                                .LoadAsync();
            }
        }
        async Task LoadRAMsDetailsToStoreEntity(string searchFilter, Store store)
        {
            await _context.Entry(store)
                        .Collection(s => s.RAMs!)
                        .Query()
                        .Where(ram => ram.Model.ToLower().Contains(searchFilter))
                        .LoadAsync();

            foreach (RAM ram in store.RAMs!)
            {
                await _context.Entry(ram)
                                .Collection(e => e.Pictures!)
                                .Query()
                                .OrderBy(pic => pic.OrderNumber)
                                .Take(1)
                                .LoadAsync();
            }
        }

        List<ProductAmountBindingModel> ConvertStoreToCPUBindingModelList(Store store)
        {
            List<CPUAmountBindingModel> models = store.CPUs!
                                                    .Select(cpu => new CPUAmountBindingModel(cpu))
                                                    .ToList();

            return models.Cast<ProductAmountBindingModel>().ToList();
        }

        List<ProductAmountBindingModel> ConvertStoreToGPUBindingModelList(Store store)
        {
            //Почти полное повторение ConvertStoreToCPUBindingModelList
            List<GPUAmountBindingModel> models = store.GPUs!
                                                    .Select(gpu => new GPUAmountBindingModel(gpu))
                                                    .ToList();

            return models.Cast<ProductAmountBindingModel>().ToList();
        }

        List<ProductAmountBindingModel> ConvertStoreToRAMBindingModelList(Store store)
        {
            //Почти полное повторение ConvertStoreToCPUBindingModelList
            List<RAMAmountBindingModel> models = store.RAMs!
                                                    .Select(ram => new RAMAmountBindingModel(ram))
                                                    .ToList();

            return models.Cast<ProductAmountBindingModel>().ToList();
        }

        async Task<bool> IsUserAdminOfStore(Store store)
        {
            int userId = GetCurrentUserIdFromAuthCookie();

            //Loads to context current user's info if he is admin of this store
            bool isAdmin = await _context.Entry(store)
                                            .Collection(e => e.Administrators!)
                                            .Query()
                                            .AnyAsync(user => user.Id == userId);

            return isAdmin;
        }

        int GetCurrentUserIdFromAuthCookie()
        {
            string userId = User.FindFirst(e => e.Type == ClaimTypes.NameIdentifier)!.Value;
            return int.Parse(userId);
        }

        ProductDetailsBindingModel GetProductDetailsBindingModelFromProduct(Product product, int storeId, byte category)
        {
            ProductDetailsBindingModel model = null!;

            if(product is CPU cpu)
            {
                model = new ProductDetailsBindingModel(cpu);
            }
            else if(product is GPU gpu)
            {
                model = new ProductDetailsBindingModel(gpu);
            }
            else if(product is RAM ram)
            {
                model = new ProductDetailsBindingModel(ram);
            }
            else
            {
                throw new NotImplementedException();
            }

            model.StoreId = storeId;
            model.Category = category;

            return model;
        }

        async Task<bool> IsCountryExist(string code)
        {
            return (await _context.Countries.FindAsync(code)) != null;
        }

        (bool, string) IsUploadedFilesNotBreakingRules(IFormFileCollection files)
        {
            string[] permittedExtensions = { ".jpg", ".jpeg", ".png", ".webp"};
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
                                .AnyAsync(prod => (prod.Id != productId) && (prod.Model == modelName));
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

        async Task SaveChangesToExistingProduct(ProductDetailsBindingModel productDetails, List<string> newPhotosFilesNames)
        {
            Product oldProductInfo = (await _context.Products
                                                    .Include(product => product.Pictures)
                                                    .FirstOrDefaultAsync(product => product.Id == productDetails.Id))!;
            
            ApplyNewCharacteristicInfoToProduct(oldProductInfo, productDetails);

            PutNewImagesIntoProductInfo(oldProductInfo, newPhotosFilesNames);

            await _context.SaveChangesAsync();
        }

        async Task SaveNewProduct(ProductDetailsBindingModel productInfo, List<string> newPhotosFilesNames)
        {
            Product product = null!;

            if(productInfo.CpuDetails != null)
            {
                product = new CPU();
            }
            else if(productInfo.GpuDetails != null)
            {
                product = new GPU();
            }
            else if(productInfo.RamDetails != null)
            {
                product = new RAM();
            }
            else
            {
                throw new NotImplementedException();
            }

            await SaveNewProduct(product, productInfo, newPhotosFilesNames);
        }

        async Task SaveNewProduct(Product product, ProductDetailsBindingModel productInfo, List<string> newPhotosFilesNames)
        {
            ApplyNewCharacteristicInfoToProduct(product, productInfo);

            product.Pictures = new List<ProductPicture>();
            PutNewImagesIntoProductInfo(product, newPhotosFilesNames);

            await DbUsageTools.TryAddNewProductToDatabase(_context, product);
        }

        void PutNewImagesIntoProductInfo(Product product, List<string> newPhotosFilesNames)
        {
            ushort lastPictureOrderNumber = 1;
            if(product.Pictures!.Count != 0)
            {
                lastPictureOrderNumber = (ushort)(product.Pictures!.Max(e => e.OrderNumber) + 1);
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

        Product ApplyNewCharacteristicInfoToProduct(Product oldProductInfo, ProductDetailsBindingModel newProductInfo)
        {
            oldProductInfo.Country = newProductInfo.Country;
            oldProductInfo.Model = newProductInfo.Model;
            oldProductInfo.Description = newProductInfo.Description;
            oldProductInfo.Price = newProductInfo.Price;

            if(oldProductInfo is CPU cpu && newProductInfo.CpuDetails != null)
            {
                cpu.ClockRate = newProductInfo.CpuDetails.ClockRate;
                cpu.NumberOfCores = newProductInfo.CpuDetails.NumberOfCores;
                cpu.Socket = newProductInfo.CpuDetails.Socket;
            }
            else if(oldProductInfo is GPU gpu && newProductInfo.GpuDetails != null)
            {
                gpu.VRAM = newProductInfo.GpuDetails.VRAM;
                gpu.MemoryType = newProductInfo.GpuDetails.MemoryType;
            }
            else if(oldProductInfo is RAM ram && newProductInfo.RamDetails != null)
            {
                ram.AmountOfMemory = newProductInfo.RamDetails.AmountOfMemory;
                ram.MemoryType = newProductInfo.RamDetails.MemoryType;
            }
            else
            {
                throw new NotImplementedException();
            }

            return oldProductInfo;
        }

        void DeleteProductPicturesFromFileSystem(List<string> fileNames)
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