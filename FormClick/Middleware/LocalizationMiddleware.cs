using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using FormClick.Data;
using System.Security.Claims;
using FormClick.Models;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace FormClick.Middleware{
    public class LocalizationMiddleware{
        private readonly RequestDelegate _next;

        public LocalizationMiddleware(RequestDelegate next){
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDBContext dbContext){
            if (context.User.Identity.IsAuthenticated){
                var userClaims = context.User.Identity as ClaimsIdentity;
                var userIdClaim = userClaims?.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                Console.WriteLine($"User ID Claim: {userIdClaim}");

                if (userIdClaim != null){
                    int userId = int.Parse(userIdClaim);
                    var userLanguage = dbContext.Users.Where(u => u.Id == userId).Select(u => u.Language).FirstOrDefault();

                    if (!string.IsNullOrEmpty(userLanguage)){
                        var language = userLanguage switch{
                            "en" => "en-US",
                            "de" => "de-DE",
                            "es" => "es-MX",
                            _ => "es-MX"
                        };

                        var culture = new CultureInfo(language);
                        var requestCulture = new RequestCulture(culture);

                        context.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(requestCulture, null));
                        context.Request.Headers["Accept-Language"] = language;
                        context.Response.Headers["Content-Language"] = language;
                    }
                }
            }

            await _next(context);
        }
    }
}
