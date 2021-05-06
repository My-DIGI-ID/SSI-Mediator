using SSIMediator.Authentication;
using SSIMediator.Model;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;
using System.Threading.Tasks;

namespace SSIMediator.Controllers
{
    /// <inheritdoc />
    [Route("api/[controller]")]
    [ApiController]
    public class LogLevelController : ControllerBase
    {
        /// <summary>
        /// Sets the log level:
        /// Verbose = 0,
        /// Debug = 1,
        /// Information = 2,
        /// Warning = 3,
        /// Error = 4,
        /// Fatal = 5
        /// </summary>
        [Authentication]
        [HttpPut]
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Controller")]
        public async Task<ActionResult<LogLevel>> Put(LogLevel logLevel)
        {
            return await Task.Run(() =>
            {
                Program.LoggingLevelSwitch.MinimumLevel = (LogEventLevel)logLevel;

                return logLevel;
            });
        }
    }
}