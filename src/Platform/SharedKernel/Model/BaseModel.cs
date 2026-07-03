using System;

namespace SharedKernel.Models
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseModel
    {

        /// <summary>
        /// Creation time
        /// </summary>
        public DateTime DateCreated { get; set; }
        /// <summary>
        /// Last updated time
        /// </summary>
        public DateTime DateUpdated { get; set; }
        /// <summary>
        /// Created by
        /// </summary>
        public Guid CreatedBy { get; set; }
        /// <summary>
        /// Last updated by
        /// </summary>
        public Guid UpdatedBy { get; set; }
        /// <summary>
        /// Status of the record
        /// </summary>
        public bool IsActive { get; set; }
    }
}