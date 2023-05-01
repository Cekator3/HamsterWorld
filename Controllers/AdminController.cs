using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HamsterWorld.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HamsterWorld.Controllers;

public class AdminController : Controller
{
    ApplicationContext _context;

    public AdminController(ApplicationContext context)
    {
        _context = context;
    }

    [Authorize(Policy = Role.AdminRoleName)]
    public IActionResult ManageUsers()
    {
        return View();
    }

    //Используется для ajax-запросов для подгрузки строк таблицы
    public async Task<IActionResult> GetUsersRows(int startPosition = 1)
    {
        int amount = 15;
        IQueryable<UserBindingModel> query = _context.Users
                                                    .AsNoTracking()
                                                    .Include(c => c.Role)
                                                    .Where(u => u.Id >= startPosition)
                                                    .OrderBy(u => u.Id)
                                                    .Select(u => new UserBindingModel(u.Login, u.Role.Name))
                                                    .Take(amount);

        ViewBag.startPosition = startPosition;
        List<UserBindingModel> users = await query.ToListAsync();
        return PartialView("_GetUsersRows", users);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
