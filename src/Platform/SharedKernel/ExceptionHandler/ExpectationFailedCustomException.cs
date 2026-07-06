using System;
using System.Net;
using System.Runtime.Serialization;

namespace SharedKernel.ExceptionHandler
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public sealed class ExpectationFailedCustomException : BaseCustomException
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        public ExpectationFailedCustomException(string message, string description) : base(message, description, (int)HttpStatusCode.ExpectationFailed)
        {

        }
    }
}