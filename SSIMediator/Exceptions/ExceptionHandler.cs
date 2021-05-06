using SSIMediator.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SSIMediator.Exceptions
{
    public class ExceptionHandler
    {
        private readonly RequestDelegate _requestDelegate;
        private readonly ILogger _logger;

        public ExceptionHandler(RequestDelegate requestDelegate, ILoggerFactory logger)
        {
            _logger = logger.CreateLogger<ExceptionHandler>();
            _requestDelegate = requestDelegate;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _requestDelegate(context);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unhandled exception has been thrown: {ex.Message}");
                _logger.LogError("Exception: {ex}");
                await Handle(context);
            }
        }

        private static Task Handle(HttpContext context)
        {
            context.Response.ContentType = HttpContentType.JSON;
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            return context.Response.WriteAsync(new CustomErrorDetails()
            {
                StatusCode = context.Response.StatusCode,
                Message = "Internal Server Error."
            }.ToString());
        }
    }
}
