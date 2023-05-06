using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HamsterWorld.Models;
using Microsoft.EntityFrameworkCore;

namespace HamsterWorld.Controllers;

[Authorize(Policy = Role.AdminRoleName)]
public class AdminUserController : Controller
{
    ApplicationContext _context;
    IConfiguration _config;

    public AdminUserController(ApplicationContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<IActionResult> ManageUsers(FilterBindingModel filters)
    {
        //Aquiring roles from database
        UserInfoBindingModel.AllRoles = await _context.Roles.AsNoTracking()
                                                        .Select(e => e.Name)
                                                        .ToListAsync(); 

        //Generating base query to aquire users from database
        IQueryable<UserInfoBindingModel> query = _context.Users.AsNoTracking()
                                                                .Include(e => e.Role)
                                                                .Select(e => new UserInfoBindingModel()
                                                                {
                                                                    Login = e.Login,
                                                                    Email = e.Email,
                                                                    Role = e.Role.Name
                                                                });
        
        //If filter exist then apply him
        if(filters.FilterValue != null)
        {
            query = ApplyFilterToQuery(query, filters);
        }

        //using query to aquire users from database
        List<UserInfoBindingModel> users = await query.Take(15)
                                            .OrderBy(e => e.Login)
                                            .ToListAsync();
        
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> ManageUsers(string login, string newRole)
    {
        //Getting user from database
        User? user = await _context.Users.FirstOrDefaultAsync(e => e.Login == login);
        //Getting role from database
        Role? role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(e => e.Name == newRole);

        if(user == null)
        {
            return BadRequest("Такого пользователя не существует");
        }
        if(role == null)
        {
            return BadRequest("Такой роли не существует");
        }

        //Saving new user's role to database
        user.Role = role;

        //Adding user to blacklist
        UserWithChangedRole usr = new UserWithChangedRole()
        {
            UserId = user.Id
        };
        await _context.Blacklist.AddAsync(usr);

        await _context.SaveChangesAsync();
        return Ok();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    IQueryable<UserInfoBindingModel> ApplyFilterToQuery(IQueryable<UserInfoBindingModel> query, FilterBindingModel filters)
    {
        switch(filters.FilterType)
        {
            case (byte)UserInfoBindingModel.Filters.Email:
                query = query.Where(e => e.Email.StartsWith(filters.FilterValue!));
                break;
            case (byte)UserInfoBindingModel.Filters.Login:
                query = query.Where(e => e.Login.StartsWith(filters.FilterValue!));
                break;
            case (byte)UserInfoBindingModel.Filters.Role:
                query = query.Where(e => e.Role.StartsWith(filters.FilterValue!));
                break;
        }
        return query;
    }
}
