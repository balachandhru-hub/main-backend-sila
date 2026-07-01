using System;
using System.Net;
using System.Runtime.Serialization;

namespace ExceptionHandler
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public sealed class ConflictCustomException : BaseCustomException
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        public ConflictCustomException(string message, string description) : base(message, description, (int)HttpStatusCode.Conflict)
        {
        }
    }
}