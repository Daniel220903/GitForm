using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using FormClick.Middleware;
using Serilog;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using FormClick.Data;

namespace FormClick{
    public class Program{
        public static void Main(string[] args){
            Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("Logs/app_log.txt", rollingInterval: RollingInterval.Day).CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog();

            builder.Services.AddScoped<FileUploadService>();

            builder.Services.AddControllersWithViews().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

            builder.Services.AddLocalization(options =>{
                options.ResourcesPath = "Resources";
            });

            builder.Services.AddDbContext<AppDBContext>(options =>{
                options.UseSqlServer(builder.Configuration.GetConnectionString("Conn"));
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>{
                    options.LoginPath = "/Access/Login";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(120);
                });

            builder.Services.AddHttpContextAccessor();

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[] {
                    new CultureInfo("en-US"),
                    new CultureInfo("de-DE"),
                    new CultureInfo("es-MX")
                };

                options.SupportedUICultures = supportedCultures;
                options.SupportedCultures = supportedCultures;
            });

            var app = builder.Build();

            app.UseAuthentication();

            app.UseMiddleware<LocalizationMiddleware>();

            app.UseRequestLocalization();

            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Access}/{action=Home}/{id?}");

            app.Run();
        }
    }
}
