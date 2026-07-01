using System;
using System.Net;
using System.Runtime.Serialization;

namespace ExceptionHandler
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public sealed class InternalServerCustomException : BaseCustomException
    {
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        public InternalServerCustomException(string message, string description) : base(message, description, (int)HttpStatusCode.InternalServerError)
        {

        }
    }
}