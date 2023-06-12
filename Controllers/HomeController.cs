using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HamsterWorld.Models;
using Microsoft.EntityFrameworkCore;
using YandexStaticMap;
using NetTopologySuite.Geometries;

namespace HamsterWorld.Controllers;

public class HomeController : Controller
{
    ApplicationContext _context;
    IConfiguration _config;

    public HomeController(ApplicationContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HamsterWorldInfoBindingModel()
        {
            StoresAmount = await _context.Stores.CountAsync(),
            EmployeesAmount = await _context.Users.CountAsync(user => user.RoleId == Role.STORE_ADMIN)
        };

        return View(model);
    }

    public async Task<IActionResult> Catalog(CatalogFilter filter)
    {
        List<CatalogItem> catalogItems = new List<CatalogItem>();

        //Load products and convert to binding model
        switch(filter.filterType)
        {
            case Product.Categorys.CPU:
                CatalogCpuFilter cpuFilter = (filter as CatalogCpuFilter)!;

                DbSet<CPU> CpuDbSet = _context.CPUs;
                List<CPU> CPUs = await LoadProducts(CpuDbSet, cpuFilter);
                catalogItems = ConvertProductsToCatalogItems(CPUs);
                break;

            case Product.Categorys.GPU:
                CatalogGpuFilter gpuFilter = (filter as CatalogGpuFilter)!;

                DbSet<GPU> GpuDbSet = _context.GPUs;
                List<GPU> GPUs = await LoadProducts(GpuDbSet, gpuFilter);
                catalogItems = ConvertProductsToCatalogItems(GPUs);
                break;


            case Product.Categorys.RAM:
                CatalogRamFilter ramFilter = (filter as CatalogRamFilter)!;

                DbSet<RAM> RamDbSet = _context.RAMs;
                List<RAM> RAMs = await LoadProducts(RamDbSet, ramFilter);
                catalogItems = ConvertProductsToCatalogItems(RAMs);
                break;

            default:
                return NotFound("Такой категории не существует");
        }

        int? userId = GetUserIdFromCookies(HttpContext);

        List<int> usersShoppingList = new List<int>();
        if(userId != null)
        {
            usersShoppingList = await GetUsersBuyingsIdList((int)userId);
        }

        CatalogBindingModel bindingModel = new CatalogBindingModel()
        {
            Filter = filter,
            CatalogItems = catalogItems,
            ProductsFromUsersShoppingList = usersShoppingList
        };

        return View(bindingModel);
    }

    public async Task<IActionResult> ProductInStores(int productId)
    {
        //Get stores where the amount of product in them is more than zero
        List<ProductInStoresBindingModel> bindModel = (await _context.Assortments.AsNoTracking()
                                                            .Where(e => e.ProductId == productId && e.Amount > 0)
                                                            .Include(e => e.store)
                                                            .Select(e => new ProductInStoresBindingModel()
                                                            {
                                                                Store = e.store!,
                                                                AmountOfProduct = e.Amount
                                                            })
                                                            .ToListAsync())!;

        return View(bindModel);
    }

    public IActionResult GetYandexMapQuery(double coordinatesX, double coordinatesY)
    {
        Point coordinates = new Point(coordinatesX, coordinatesY);

        string imgSrc = YandexStaticMapTools.GenerateSrcAttributeForMap(coordinates, _config);

        return Content(imgSrc);
    }

    public async Task<IActionResult> ViewProduct(int productId)
    {
        Product? product = await _context.Products.FindAsync(productId);

        if(product == null)
        {
            return NotFound("Товар с таким Id был не найден");
        }

        await LoadProductDetails(product);

        ViewProductBindingModel bindingModel = await ConvertProductToViewProductBindingModel(product);

        int? userId = GetUserIdFromCookies(HttpContext);
        if(userId != null)
        {
            bindingModel.IsInUsersBuyingsList = await CheckIfProductIsInUsersBuyingsList((int)userId, productId);
        }

        return View(bindingModel);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddProductToUserShoppingList(int productId)
    {
        int? userId = GetUserIdFromCookies(HttpContext);
        if(userId == null)
        {
            return Forbid("В куках отсутствует индентификатор пользователя");
        }

        //get user's current shopping list
        ShoppingList? userShoppingList = await GetUserCurrentShoppingList((int)userId) ??
                                        await CreateUserShoppingList((int)userId);

        if(CheckIfProductIsInUserShoppingList(userShoppingList, productId))
        {
            return BadRequest("Этот товар уже в корзине");
        }
        
        await AddNewProductToUserShoppingList(userShoppingList, productId);

        return Ok();
    }

    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> RemoveProductFromUserShoppingList(int productId)
    {
        int? userId = GetUserIdFromCookies(HttpContext);
        if(userId == null)
        {
            return Forbid("В куках отсутствует индентификатор пользователя");
        }

        //get user's shopping list
        ShoppingList? userShoppingList = await GetUserCurrentShoppingList((int)userId) ??
                                        await CreateUserShoppingList((int)userId);

        await RemoveProductFromUserShoppingList(userShoppingList, productId);

        return Ok();
    }

    [Authorize]
    public async Task<IActionResult> ShoppingCart()
    {
        int userId = (int)GetUserIdFromCookies(HttpContext)!;

        //get user's current shopping list with its items
        ShoppingList? shoppingCart = await GetUserCurrentShoppingListWithProductDetailsLoaded((int)userId);
        if(shoppingCart == null)
        {
            shoppingCart = await CreateUserShoppingList((int)userId);
        }

        ShoppingCartBindingModel model = GenerateShoppingCartBindingModel(shoppingCart);

        return View(model);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ShoppingCart(ShoppingCartBindingModel model)
    {
        bool isShoppingCartEmpty = model.ShoppingItems == null;
        if(isShoppingCartEmpty)
        {
            return BadRequest();
        }

        int? userId = (int)GetUserIdFromCookies(HttpContext)!;

        //Obtain items from db 
        ShoppingList? shoppingCart = await GetUserCurrentShoppingListWithProductDetailsLoaded((int)userId);
        if(shoppingCart == null || shoppingCart.Buyings!.Count == 0)
        {
            return NotFound("У пользователя нет сформированной корзины");
        }

        decimal finalPrice = 0.0m;

        foreach(ItemOfShoppingList itemFromDb in shoppingCart.Buyings!)
        {
            int productAmountLimit = await GetRemainingAmountOfProductInStores(itemFromDb.ProductId);

            ShoppingItem? itemFromUserForm = model.ShoppingItems!.FirstOrDefault(e => e.ProductId == itemFromDb.ProductId);

            if(itemFromUserForm == null)
            {
                return BadRequest("Данные корзины были изменены некорректно");
            }

            if(itemFromUserForm.Amount <= 0)
            {
                ModelState.AddModelError("", "Количество товара для покупки должно быть положительным");
                break;
            }

            if(itemFromUserForm.Amount > productAmountLimit)
            {
                ModelState.AddModelError("", $"Количество оставшихся {itemFromUserForm.ProductName} в магазинах - {productAmountLimit}");
            }

            itemFromDb.Amount = itemFromUserForm.Amount;
            finalPrice += itemFromDb.Product.Price * itemFromDb.Amount;
        }

        if(!ModelState.IsValid)
        {
            model = GenerateShoppingCartBindingModel(shoppingCart);
            return View(model);
        }
        else
        {
            //save all changes
            shoppingCart.FinalPrice = finalPrice;
            await _context.SaveChangesAsync();
        }
        
        return RedirectToAction("Checkout", new {shoppingListId = shoppingCart.Id});
    }

    [Authorize]
    public async Task<IActionResult> Checkout(int shoppingListId)
    {
        //Это действие - Эмуляция покупки
        int userId = (int)GetUserIdFromCookies(HttpContext)!;

        ShoppingList? shoppingCart = await GetUserCurrentShoppingListWithProductDetailsLoaded((int)userId);

        bool isUserShoppingCartNotExist = shoppingCart == null || shoppingCart.Buyings!.Count == 0;
        if(isUserShoppingCartNotExist)
        {
            return RedirectToAction("ShoppingCart");
        }

        List<Assortment> assortments = await GetRemainingAmountOfProductsThatWereFoundInUserShoppingCart(shoppingCart!);
        
        //Есть шанс, что данные устарели и количество продуктов стало меньше, чем было.
        foreach(ItemOfShoppingList buying in shoppingCart!.Buyings!)
        {
            //Amounts of product in different stores
            List<Assortment> assortmentsOfProduct = assortments.Where(e => e.ProductId == buying.ProductId).ToList();

            if(!IsStoresHaveEnoughProductAmountToPerformDeal(assortmentsOfProduct, buying.Amount))
            {
                return RedirectToAction("ShoppingCart");
            }

            SimulatePerfomingDeal(assortmentsOfProduct, buying.Amount);
        }

        shoppingCart.TimeOfSale = DateTimeOffset.UtcNow;

        //Сохраняем изменения
        await _context.SaveChangesAsync();
        return View(shoppingCart.FinalPrice);
    }

    [HttpGet]
    [Authorize(Policy = Role.UserRoleName)]
    public IActionResult AddNewFeedback(int productId)
    {
        AddNewFeedbackBindingModel model = new AddNewFeedbackBindingModel()
        {
            ProductId = productId
        };

        return View(model);
    }

    [Authorize(Policy = Role.UserRoleName)]
    [HttpPost]
    public async Task<IActionResult> AddNewFeedback(AddNewFeedbackBindingModel model)
    {
        if(string.IsNullOrWhiteSpace(model.FeedbackText))
        {
            ModelState.AddModelError("", "Текст отзыва не должен быть пустым");
        }
        
        int userId = (int)GetUserIdFromCookies(HttpContext)!;

        if(!(await IsProductExist(model.ProductId)))
        {
            ModelState.AddModelError("", "Товара с таким Id не существует");
        }

        if(await IsFeedbackToProductWithThisAuthorExist((int)userId, model.ProductId))
        {
            ModelState.AddModelError("", "Вы уже писали отзыв для этого товара");
        }

        if(!ModelState.IsValid)
        {
            return View(model);
        }

        CommentToProduct feedback = new CommentToProduct()
        {
            ProductId = model.ProductId,
            AuthorId = userId,
            Content = model.FeedbackText,
            WritingDate = DateTimeOffset.UtcNow,
            AmountOfStars = model.FeedbackRating
        };

        await _context.CommentsToProducts.AddAsync(feedback);
        await _context.SaveChangesAsync();


        return RedirectToAction("ViewProduct", new { productId = model.ProductId });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


    async Task<List<T>> LoadProducts<T>(DbSet<T> productDbSet, CatalogFilter filter) where T:Product
    {
        IQueryable<T> query = productDbSet.AsNoTracking();

        ApplyBaseFilters(ref query, filter);
        ApplySpecificFilters(ref query, filter);

        //loading preview image
        query = query.Include(product => product.Pictures!.OrderBy(pic => pic.OrderNumber).Take(1));

        byte amountOfProductsToBeLoaded = 15;
        query = query.OrderBy(product => product.Id)
                    .Take(amountOfProductsToBeLoaded);

        return await query.ToListAsync();
    }

    List<CatalogItem> ConvertProductsToCatalogItems(List<CPU> products)
    {
        return products.Select(cpu => new CatalogCpuItem(cpu)).ToList<CatalogItem>();
    }
    List<CatalogItem> ConvertProductsToCatalogItems(List<GPU> products)
    {
        return products.Select(gpu => new CatalogGpuItem(gpu)).ToList<CatalogItem>();
    }
    List<CatalogItem> ConvertProductsToCatalogItems(List<RAM> products)
    {
        return products.Select(ram => new CatalogRamItem(ram)).ToList<CatalogItem>();
    }

    void ApplyBaseFilters<T>(ref IQueryable<T> query, CatalogFilter filter) where T:Product
    {
        if(filter.MinPrice != null)
        {
            query = query.Where(product => product.Price >= filter.MinPrice);
        }

        if(filter.MaxPrice != null)
        {
            query = query.Where(product => product.Price <= filter.MaxPrice);
        }

        if(!String.IsNullOrWhiteSpace(filter.Model))
        {
            string model = filter.Model.ToLower();
            query = query.Where(product => product.Model.ToLower().Contains(model));
        }
    }

    void ApplySpecificFilters<T>(ref IQueryable<T> query, CatalogFilter filter) where T:Product
    {
        if(query is IQueryable<CPU> cpuQuery)
        {
            ApplyCpuFilters(ref cpuQuery, (filter as CatalogCpuFilter)!);

            query = (IQueryable<T>)cpuQuery;
        }
        else if(query is IQueryable<GPU> gpuQuery)
        {
            ApplyGpuFilters(ref gpuQuery, (filter as CatalogGpuFilter)!);

            query = (IQueryable<T>)gpuQuery;
        }
        else if(query is IQueryable<RAM> ramQuery)
        {
            ApplyRamFilters(ref ramQuery, (filter as CatalogRamFilter)!);

            query = (IQueryable<T>)ramQuery;
        }
    }

    void ApplyCpuFilters(ref IQueryable<CPU> query, CatalogCpuFilter filter)
    {
        if(filter.ClockRateMin != null)
        {
            query = query.Where(product => product.ClockRate >= filter.ClockRateMin);
        }

        if(filter.ClockRateMax != null)
        {
            query = query.Where(product => product.ClockRate <= filter.ClockRateMax);
        }

        if(filter.NumberOfCoresMin != null)
        {
            query = query.Where(product => product.NumberOfCores >= filter.NumberOfCoresMin);
        }

        if(filter.NumberOfCoresMax != null)
        {
            query = query.Where(product => product.NumberOfCores <= filter.NumberOfCoresMax);
        }

        if(!String.IsNullOrWhiteSpace(filter.Socket))
        {
            string socket = filter.Socket.ToLower();
            query = query.Where(product => product.Socket.ToLower().Contains(socket));
        }
    }

    void ApplyGpuFilters(ref IQueryable<GPU> query, CatalogGpuFilter filter)
    {
        if(!String.IsNullOrWhiteSpace(filter.MemoryType))
        {
            string MemoryType = filter.MemoryType.ToLower();
            query = query.Where(product => product.MemoryType.ToLower().Contains(MemoryType));
        }

        if(filter.VramMin != null)
        {
            query = query.Where(product => product.VRAM >= filter.VramMin);
        }

        if(filter.VramMax != null)
        {
            query = query.Where(product => product.VRAM < filter.VramMax);
        }
    }

    void ApplyRamFilters(ref IQueryable<RAM> query, CatalogRamFilter filter)
    {
        if(!String.IsNullOrWhiteSpace(filter.MemoryType))
        {
            string MemoryType = filter.MemoryType.ToLower();
            query = query.Where(product => product.MemoryType.ToLower().Contains(MemoryType));
        }

        if(filter.AmountOfMemoryMin != null)
        {
            query = query.Where(product => product.AmountOfMemory >= filter.AmountOfMemoryMin);
        }

        if(filter.AmountOfMemoryMax != null)
        {
            query = query.Where(product => product.AmountOfMemory < filter.AmountOfMemoryMax);
        }
    }

    async Task LoadProductDetails(Product product)
    {
        await _context.Entry(product)
                        .Collection(e => e.Pictures!)
                        .LoadAsync();

        await _context.Entry(product)
                        .Collection(e => e.Comments!)
                        .Query()
                        .Include(e => e.ChildrenComments)
                        .LoadAsync();
    }

    async Task<ViewProductBindingModel> ConvertProductToViewProductBindingModel(Product product)
    {
        ViewProductBindingModel model = null!;
        if(product is CPU cpu)
        {
            model = new ViewProductBindingModel(cpu);
        }
        else if(product is GPU gpu)
        {
            model = new ViewProductBindingModel(gpu);
        }
        else if(product is RAM ram)
        {
            model = new ViewProductBindingModel(ram);
        }
        else
        {
            throw new NotImplementedException();
        }

        model.AverageMark = await GetAverageMarkOfProduct(product.Id);

        return model;
    }

    async Task<double> GetAverageMarkOfProduct(int productId)
    {
        return await _context.CommentsToProducts
                            .Where(e => e.ProductId == productId)
                            .AverageAsync(e => (double?)e.AmountOfStars) ?? 0.0;
    }

    int? GetUserIdFromCookies(HttpContext context)
    {
        string? userId = HttpContext.User.Claims
                                        .FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)
                                        ?.Value;
        
        if(!int.TryParse(userId, out int result))
        {
            return null;
        }

        return result;
    }

    async Task<bool> IsFeedbackToProductWithThisAuthorExist(int authorId, int productId)
    {
        return await _context.CommentsToProducts
                                .AnyAsync(feedback => feedback.AuthorId == authorId && feedback.ProductId == productId );
    }

    async Task<List<int>> GetUsersBuyingsIdList(int userId)
    {
        return (await _context.ShoppingLists.AsNoTracking()
                            .Include(e => e.Buyings)
                            .Select(e => new {e.UserId, e.Buyings, e.TimeOfSale})
                            .FirstOrDefaultAsync(e => e.UserId == userId && e.TimeOfSale == null))
                            ?.Buyings
                            ?.Select(e => e.ProductId)
                            .ToList() 
                            ?? new List<int>();
    }

    async Task<bool> CheckIfProductIsInUsersBuyingsList(int userId, int productId)
    {
        return (await GetUsersBuyingsIdList(userId)).Contains(productId);
    }

    bool CheckIfProductIsInUserShoppingList(ShoppingList userShoppingList, int productId)
    {
        return userShoppingList.Buyings!.Any(e => e.ProductId == productId);
    }

    async Task<ShoppingList?> GetUserCurrentShoppingList(int userId)
    {
        //Если у корзины определён TimeOfSale, то эта корзина старая: пользователь её оплачивал
        return await _context.ShoppingLists
                            .Include(e => e.Buyings)
                            .FirstOrDefaultAsync(e => e.UserId == (int)userId && e.TimeOfSale == null);
    }

    async Task<ShoppingList> CreateUserShoppingList(int userId)
    {
        ShoppingList userShoppingList = new ShoppingList()
        {
            UserId = userId,
            Buyings = new List<ItemOfShoppingList>(),
            FinalPrice = 0
        };

        await _context.ShoppingLists.AddAsync(userShoppingList);
        await _context.SaveChangesAsync();

        return userShoppingList;
    }

    async Task AddNewProductToUserShoppingList(ShoppingList userShoppingList, int productId)
    {
        //add new product to user's shopping list
        ItemOfShoppingList item = new ItemOfShoppingList()
        {
            ShoppingListId = userShoppingList.Id,
            ProductId = productId,
            Amount = 1
        };

        userShoppingList.Buyings!.Add(item);
        await _context.SaveChangesAsync();
    }

    async Task RemoveProductFromUserShoppingList(ShoppingList userShoppingList, int productId)
    {
        ItemOfShoppingList? productToRemove = userShoppingList.Buyings!.FirstOrDefault(e => e.ProductId == productId);

        if(productToRemove != null)
        {
            userShoppingList.Buyings!.Remove(productToRemove);
            await _context.SaveChangesAsync();
        }
    }

    async Task<ShoppingList?> GetUserCurrentShoppingListWithProductDetailsLoaded(int userId)
    {
        //TODO вообще не уверен, что это эффективно. Следует зарефакторить
        //Загружаем все необходимые данные
        ShoppingList? shoppingCart = await _context.ShoppingLists
                                                    .Include(e => e.Buyings)
                                                        !.ThenInclude(e => e.Product)
                                                    .FirstOrDefaultAsync(e => e.TimeOfSale == null && e.UserId == userId);

        if(shoppingCart == null)
        {
            return null;
        }

        List<Product> products = shoppingCart.Buyings!.Select(e => e.Product).ToList();
        foreach(Product product in products)
        {
            await _context.Entry(product)
                        .Collection(e => e.Pictures!)
                        .Query()
                        .OrderBy(e => e.OrderNumber)
                        .Take(1)
                        .LoadAsync();
        }

        return shoppingCart;
    }

    ShoppingCartBindingModel GenerateShoppingCartBindingModel(ShoppingList shoppingCart)
    {
        List<ShoppingItem> items = shoppingCart.Buyings!.Select(e => new ShoppingItem()
        {
            ProductId = e.ProductId,
            ProductName = e.Product.ToString()!,
            ProductPictureSrc = e.Product.Pictures!.First().FileName,
            Amount = e.Amount,
            Price = e.Product.Price
        }).ToList();

        decimal totalPrice = shoppingCart.Buyings!.Sum(e => e.Product.Price);

        ShoppingCartBindingModel model = new ShoppingCartBindingModel()
        {
            ShoppingItems = items,
            TotalPrice = totalPrice
        };

        return model;
    }

    async Task<int> GetRemainingAmountOfProductInStores(int productId)
    {
        return await _context.Assortments
                                .Where(e => e.ProductId == productId)
                                .SumAsync(e => e.Amount);
    }

    bool IsStoresHaveEnoughProductAmountToPerformDeal(List<Assortment> assortmentsOfProduct, int productAmount)
    {
        int productAmountLimit = assortmentsOfProduct.Sum(e => e.Amount);
                                        
        return productAmount <= productAmountLimit;
    }

    void SimulatePerfomingDeal(List<Assortment> assortmentsOfProduct, int productAmount)
    {
        foreach(Assortment assortment in assortmentsOfProduct)
        {
            if(assortment.Amount > productAmount)
            {
                assortment.Amount -= productAmount;
                productAmount = 0;
                break;
            }
            else
            {
                productAmount -= assortment.Amount;
                assortment.Amount = 0;
            }
        }
    }

    async Task<List<Assortment>> GetRemainingAmountOfProductsThatWereFoundInUserShoppingCart(ShoppingList userShoppingCart)
    {
        int[] productIds = userShoppingCart!.Buyings!.Select(e => e.ProductId).ToArray();

        //Get remaining amount of products that were found in the user’s shopping cart
        return await _context.Assortments
                            .Where(e => productIds.Contains(e.ProductId))
                            .ToListAsync();
    }

    async Task<bool> IsProductExist(int productId)
    {
        return await _context.Products.AnyAsync(e => e.Id == productId);
    }
}