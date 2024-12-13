using FormClick.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using FormClick.Middleware;
using Serilog;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Localization;

namespace FormClick{
    public class Program
    {
        public static void Main(string[] args){
            Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("Logs/app_log.txt", rollingInterval: RollingInterval.Day).CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog();

            builder.Services.AddControllersWithViews().
                AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

            builder.Services.AddLocalization(options =>
            {
                options.ResourcesPath = "Resources";
            });

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("de-DE"),
                    new CultureInfo("es-MX")
                };

                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedUICultures = supportedCultures;
            });

            builder.Services.AddDbContext<AppDBContext>(options =>{
                options.UseSqlServer(builder.Configuration.GetConnectionString("Conn"));
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Access/Login";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(120);
                });

            var app = builder.Build();

            app.UseRequestLocalization();

            app.UseDeveloperExceptionPage();

            // app.UseMiddleware<VerificationMiddleware>();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Access}/{action=Login}/{id?}");

            app.Run();
        }
    }
}
