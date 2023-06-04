using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HamsterWorld.Models;
using Microsoft.EntityFrameworkCore;

namespace HamsterWorld.Controllers;

public class HomeController : Controller
{
    ApplicationContext _context;

    public HomeController(ApplicationContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var model = new HamsterWorldInfoBindingModel()
        {
            StoresAmount = _context.Stores.Count(),
            EmployeesAmount = _context.Users.Count(user => user.RoleId == Role.STORE_ADMIN)
        };

        return View(model);
    }

    public async Task<IActionResult> Catalog(CatalogFilter filter)
    {
        List<CatalogItem> catalogItems = new List<CatalogItem>();

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

        CatalogBindingModel bindingModel = new CatalogBindingModel()
        {
            Filter = filter,
            CatalogItems = catalogItems
        };

        return View(bindingModel);
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
}
