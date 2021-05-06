using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace SSIMediator.Authentication
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthenticationAttribute : Attribute, IAsyncActionFilter
    {
        public const string header = "X-Auth-Token";


        ///<inheritdoc/>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(header, out Microsoft.Extensions.Primitives.StringValues givenApiKey))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            string apiKey = Environment.GetEnvironmentVariable(header.ToUpper());

            if (apiKey != givenApiKey)
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            await next();
        }
    }
}