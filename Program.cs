using Microsoft.EntityFrameworkCore; 
using HamsterWorld.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.HttpOverrides;

namespace HamsterWorld
{
   public class Program
   {
      public static void Main(string[] args) 
      {
         WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
         ConfigureServices(builder);

         WebApplication app = builder.Build();
         Configure(app);

         app.Run();
      }

      static void ConfigureServices(WebApplicationBuilder builder)
      {
			//ConnectionString расположена в User-Secrets
         string connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"]!;
         builder.Services.AddDbContext<ApplicationContext>(options => options.UseNpgsql(connectionString, x => x.UseNetTopologySuite()));

         builder.Services.AddAuthentication("Cookies").AddCookie(config => 
         {
            config.Cookie.Name = "MyCookie";
            config.AccessDeniedPath = "/Forbidden/";
            config.LoginPath = "/Auth/Login";
            config.EventsType = typeof(CustomCookieAuthenticationEvents);
         });
         builder.Services.AddScoped<CustomCookieAuthenticationEvents>();

         builder.Services.AddAuthorization(options =>
         {
            options.AddPolicy(name: Role.AdminRoleName, policy =>
            {
               policy.RequireRole(Role.ADMIN.ToString());
            });
            options.AddPolicy(name: Role.StoreAdminRoleName, policy =>
            {
               policy.RequireAssertion( x => 
                  x.User.HasClaim(ClaimTypes.Role, Role.STORE_ADMIN.ToString()) ||
                  x.User.HasClaim(ClaimTypes.Role, Role.ADMIN.ToString())
               );
            });
            options.AddPolicy(name: Role.BannedUserRoleName, policy =>
            {
               policy.RequireRole(Role.BANNED.ToString());
            });
         });

         builder.Services.AddControllersWithViews();

         //Сессионное хранилище
         builder.Services.AddDistributedMemoryCache();
         builder.Services.AddSession();
      }

      static void Configure(WebApplication app)
      {
         //Необходим для разворачивания, используя прокси-сервер nginx
         app.UseForwardedHeaders(new ForwardedHeadersOptions
         {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
         });

         //Middleware обработки исключений, возникающих в приложении
         if (app.Environment.IsDevelopment())
         {
             app.UseDeveloperExceptionPage();
         }
         else
         {
            //Заново запускает конвеер, дабы он смог вернуть красивую страницу
            app.UseExceptionHandler("/Error");
         }

         //Middleware обработки ошибок, когда конвеер не может выполнить свою задачу (ресурс не найден, страницы не существует...)
         //Заново запускает конвеер, запрашивая ресурс /StatusCode/{код ошибки}. Приложение должно вернуть красивую страницу
         app.UseStatusCodePagesWithReExecute("/StatusCode/{0}");

         app.UseStaticFiles();
         app.UseRouting();

         app.UseAuthentication();
         app.UseAuthorization();

         app.UseSession();

         app.MapControllerRoute(
             name: "default",
             pattern: "{controller=Home}/{action=Index}/{id?}");

      }
   }
}
