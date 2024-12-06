using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FormClick.Middleware{
    public class VerificationMiddleware{
        private readonly RequestDelegate _next;

        public VerificationMiddleware(RequestDelegate next){
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context){

            if (context.User.Identity?.IsAuthenticated ?? false){
                var isVerified = context.User.FindFirst("Verified")?.Value;

                if (isVerified != "True"){
                    if (!context.Request.Path.StartsWithSegments("/Access/Verify")){
                        context.Response.Redirect("/Access/Verify");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
