using System;
using System.Net;
using System.Runtime.Serialization;

namespace SharedKernel.ExceptionHandler
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public sealed class FailedDependencyCustomException : BaseCustomException
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        public FailedDependencyCustomException(string message, string description) : base(message, description, (int)HttpStatusCode.FailedDependency)
        {

        }
    }
}