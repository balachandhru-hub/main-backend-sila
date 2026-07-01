using System;
using System.Net;
using System.Runtime.Serialization;

namespace ExceptionHandler
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public sealed class NoContentCustomException : BaseCustomException
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        public NoContentCustomException(string message, string description) : base(message, description, (int)HttpStatusCode.NoContent)
        {

        }
    }
}