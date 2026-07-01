using System;
using System.Net;
using System.Runtime.Serialization;

namespace ExceptionHandler
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public sealed class TooManyRequestsCustomException : BaseCustomException
    {
    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        public TooManyRequestsCustomException(string message, string description) : base(message, description, (int)HttpStatusCode.TooManyRequests)
        {
        }
    }
}