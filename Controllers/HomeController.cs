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

        ApplyBaseFilters(query, filter);
        ApplySpecificFilters(query, filter);

        //loading preview image
        query = query.Include(product => product.Pictures!.OrderBy(pic => pic.OrderNumber).Take(1));

        return await query.Take(15).ToListAsync();
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

    void ApplyBaseFilters(IQueryable<Product> query, CatalogFilter filter)
    {
        if(filter.MaxPrice != null)
        {
            query = query.Where(product => product.Price >= filter.MinPrice && product.Price <= filter.MaxPrice);
        }

        if(filter.Model != null)
        {
            query = query.Where(product => product.Model == filter.Model);
        }
    }

    void ApplySpecificFilters(IQueryable<Product> query, CatalogFilter filter)
    {
        if(query is IQueryable<CPU>)
        {
            ApplyCpuFilters((query as IQueryable<CPU>)!, (filter as CatalogCpuFilter)!);
        }
        else if(query is IQueryable<GPU>)
        {
            ApplyGpuFilters((query as IQueryable<GPU>)!, (filter as CatalogGpuFilter)!);
        }
        else if(query is IQueryable<RAM>)
        {
            ApplyRamFilters((query as IQueryable<RAM>)!, (filter as CatalogRamFilter)!);
        }
    }

    void ApplyCpuFilters(IQueryable<CPU> query, CatalogCpuFilter filter)
    {
        if(filter.ClockRateMax != null)
        {
            query = query.Where(product => product.ClockRate >= filter.ClockRateMin && product.ClockRate <= filter.ClockRateMax);
        }

        if(filter.AllowedNumbersOfCores.Count != 0)
        {
            query = query.Where(product => filter.AllowedNumbersOfCores.Contains(product.NumberOfCores));
        }

        if(filter.AllowedSockets.Count != 0)
        {
            query = query.Where(product => filter.AllowedSockets.Contains(product.Socket));
        }
    }

    void ApplyGpuFilters(IQueryable<GPU> query, CatalogGpuFilter filter)
    {
        if(filter.MemoryType != null)
        {
            query = query.Where(product => product.MemoryType.Contains(filter.MemoryType));
        }

        if(filter.AllowedVRAMs.Count != 0)
        {
            query = query.Where(product => filter.AllowedVRAMs.Contains(product.VRAM));
        }
    }

    void ApplyRamFilters(IQueryable<RAM> query, CatalogRamFilter filter)
    {
        if(filter.MemoryType != null)
        {
            query = query.Where(product => product.MemoryType.Contains(filter.MemoryType));
        }

        if(filter.AllowedAmountsOfMemory.Count() != 0)
        {
            query = query.Where(product => filter.AllowedAmountsOfMemory.Contains(product.AmountOfMemory));
        }
    }

}
