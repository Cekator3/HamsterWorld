using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HamsterWorld.Models;
using Microsoft.EntityFrameworkCore;

namespace HamsterWorld.Controllers;

[Authorize(Policy = Role.AdminRoleName)]
public class AdminUserController : Controller
{
    readonly ApplicationContext _context;

    public AdminUserController(ApplicationContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> ManageUsers(string search = "")
    {
        //Список ролей необходим для отображения их в представлении
        UserInfoBindingModel.AllRoles = await GetListOfRolesNames();

        int amountOfUsersToGet = 15;
        List<UserInfoBindingModel> users = await GetUsersWhoMatchTheSearchFilter(search, amountOfUsersToGet);
        
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> ManageUsers(string login, string newRole)
    {
        //Getting user from database
        User? user = await _context.Users.FirstOrDefaultAsync(e => e.Login == login);
        if(user == null)
        {
            return NotFound("Такого пользователя не существует");
        }

        //Getting role from database
        Role? role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(e => e.Name == newRole);
        if(role == null)
        {
            return NotFound("Такой роли не существует");
        }

        //Saving new user's role to database
        user.Role = role;

        //Adding user to blacklist if he is not already there
        if (!(await _context.Blacklist.AnyAsync(blacklistUser => blacklistUser.UserId == user.Id)))
        {
            UserWithChangedRole usr = new UserWithChangedRole()
            {
                UserId = user.Id
            };
            await _context.Blacklist.AddAsync(usr);
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    async Task<List<string>> GetListOfRolesNames()
    {
        return await _context.Roles.AsNoTracking()
                                    .Select(e => e.Name)
                                    .ToListAsync();
    }

    async Task<List<UserInfoBindingModel>> GetUsersWhoMatchTheSearchFilter(string searchFilter, int amount)
    {
        string searchFilterLowered = searchFilter.Trim().ToLower();

        return await _context.Users.AsNoTracking()
                                    .Where(e => e.Login.ToLower().Contains(searchFilterLowered) || e.Email.ToLower().Contains(searchFilterLowered))
                                    .Select(e => new UserInfoBindingModel()
                                    {
                                        Login = e.Login,
                                        Email = e.Email,
                                        Role = e.Role.Name
                                    })
                                    .Take(amount)
                                    .OrderBy(e => e.Login)
                                    .ToListAsync();

    }
}
