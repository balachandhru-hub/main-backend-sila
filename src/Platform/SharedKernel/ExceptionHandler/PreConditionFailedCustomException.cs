using System;
using System.Net;
using System.Runtime.Serialization;

namespace SharedKernel.ExceptionHandler
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public sealed class PreConditionFailedCustomException : BaseCustomException
    {
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        public PreConditionFailedCustomException(string message, string description) : base(message, description, (int)HttpStatusCode.PreconditionFailed)
        {

        }
    }
}