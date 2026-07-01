using System;
using System.Net;
using System.Runtime.Serialization;

namespace ExceptionHandler
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public sealed class UnAuthorizedCustomException : BaseCustomException
    {
    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        public UnAuthorizedCustomException(string message, string description) : base(message, description, (int)HttpStatusCode.Unauthorized)
        {
        }
    }
}