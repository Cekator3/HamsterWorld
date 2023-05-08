using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HamsterWorld.Models;

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
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
