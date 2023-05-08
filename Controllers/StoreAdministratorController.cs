using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using HamsterWorld.Models;

namespace HamsterWorld.Controllers
{
    [Authorize(Policy = Role.StoreAdminRoleName)]
    public class StoreAdministratorController : Controller
    {
        ApplicationContext _context;

        public StoreAdministratorController(ApplicationContext context)
        {
            _context = context;
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

        public async Task<IActionResult> ChooseCategory(short storeId)
        {
            //Check if store exist
            Store? store = await _context.Stores.FindAsync(storeId);
            if(store == null)
            {
                return BadRequest("Такого магазина не существует");
            }

            int userId = GetCurrentUserIdFromAuthCookie();

            if( !(await IsUserAdminOfStore(userId, store)) )
            {
                return Unauthorized();
            }

            ViewBag.StoreId = storeId;
            return View();
        }
        //TODO Расплодить контроллеры (ManageCPU, ManageGPU...) или сделать один(ManageProduct)
        // public IActionResult Manage()

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        int GetCurrentUserIdFromAuthCookie()
        {
            string userId = User.FindFirst(e => e.Type == ClaimTypes.NameIdentifier)!.Value;
            return int.Parse(userId);
        }

        async Task<bool> IsUserAdminOfStore(int userId, Store store)
        {
            //Loads to context current user's info if he is admin of this store
            await _context.Entry(store)
                            .Collection(e => e.Administrators!)
                            .Query()
                            .FirstOrDefaultAsync(e => e.Id == userId);

            return store.Administrators!.Count == 1;
        }
    }
}

