using System;
using System.Runtime.Serialization;

namespace SharedKernel.ExceptionHandler
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class BaseCustomException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public int Code { get; }
        /// <summary>
        /// 
        /// </summary>
        public string Description { get; }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        /// <param name="code"></param>
        public BaseCustomException(string message, string description, int code) : base(message)
        {
            Code = code;
            Description = description;
        }
    }
}