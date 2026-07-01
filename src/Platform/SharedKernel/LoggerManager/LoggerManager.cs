using Contracts;
// using Entities.Common;
using NLog;
using System.Web;
using System.Net;

namespace LoggerService
{
    /// <summary>
    /// Class <c>LoggerManager</c>.
    /// Contains the implemenation of different log levels.
    /// </summary>
    public class LoggerManager : ILoggerManager
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 
        /// </summary>
        public LoggerManager()
        {
        }

        /// <summary>
        /// This method used to log the DEBUG logs.
        /// </summary>
        /// <param name="message">A string message to log at Debug level</param>
        public void LogDebug(string message)
        {
            _logger.Debug(WebUtility.HtmlEncode(message));
        }

        /// <summary>
        /// This method used to log the ERROR logs.
        /// </summary>
        /// <param name="message">A string message to log at Error level</param>
        public void LogError(string message)
        {
            _logger.Error(WebUtility.HtmlEncode(message));
        }

        /// <summary>
        /// This method used to log the INFO logs.
        /// </summary>
        /// <param name="message">A string message to log at Info level</param>
        public void LogInfo(string message)
        {
            _logger.Info(WebUtility.HtmlEncode(message));
        }
     
    }
}