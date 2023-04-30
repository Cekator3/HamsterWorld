using Microsoft.EntityFrameworkCore; 
using HamsterWorld.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

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
            config.AccessDeniedPath = "/AccessDenied/Index";
            config.LoginPath = "/Auth/Login";
         });
         builder.Services.AddAuthorization(options =>
         {

         });

         builder.Services.AddControllersWithViews();

         //Сессионное хранилище
         builder.Services.AddDistributedMemoryCache();
         builder.Services.AddSession();
      }

      static void Configure(WebApplication app)
      {

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
