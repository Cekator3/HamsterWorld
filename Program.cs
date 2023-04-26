using Microsoft.EntityFrameworkCore; 
using HamsterWorld.Models;

namespace HamsterWorld
{
   public class Program
   {
      public static void Main(string[] args) 
      {
         var builder = WebApplication.CreateBuilder(args);

         string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
         builder.Services.AddDbContext<ApplicationContext>(options => options.UseNpgsql(connectionString, x => x.UseNetTopologySuite()));

         // Service == Dependency
         builder.Services.AddControllersWithViews();

         var app = builder.Build();


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
         app.MapControllerRoute(
             name: "default",
             pattern: "{controller=Home}/{action=Index}/{id?}");

         app.Run();
      }
   }
}
