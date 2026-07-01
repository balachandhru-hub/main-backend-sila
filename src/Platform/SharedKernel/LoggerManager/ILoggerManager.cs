
namespace LoggerService
{
    /// <summary>
    /// Interface <c>ILoggerManager</c>.
    /// Contains the definition of different log levels.
    /// </summary>
    public interface ILoggerManager
    {
        /// <summary>
        /// 
        /// </summary>
        void LogInfo(string message);
        /// <summary>
        /// 
        /// </summary>
        void LogDebug(string message);
        /// <summary>
        /// 
        /// </summary>
        void LogError(string message);
      
    }
}