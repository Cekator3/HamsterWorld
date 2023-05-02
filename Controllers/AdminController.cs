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
        IQueryable<UserInfoBindingModel> query = _context.Users
                                                        .AsNoTracking()
                                                        .Include(e => e.Role)
                                                        .Include(e => e.AdministratingStores)
                                                        .Where(u => u.Id >= startPosition)
                                                        .OrderBy(u => u.Id)
                                                        .Select(e => new UserInfoBindingModel
                                                        (
                                                            e.Login,
                                                            e.Role.Name,
                                                            e.AdministratingStores
                                                        ))
                                                        .Take(amount);

        ViewBag.startPosition = startPosition;
        List<UserInfoBindingModel> users = await query.ToListAsync();
        return PartialView("_GetUsersRows", users);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
