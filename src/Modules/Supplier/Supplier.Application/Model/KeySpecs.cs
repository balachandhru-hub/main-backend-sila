namespace HashingSystem
{
    /// <summary>
    /// KeySpecs class specifies the hashing key specifications
    /// </summary>
    public class KeySpecs
    {
        /// <summary>
        /// Random salt to safeguard the password
        /// </summary>
        public string Salt { get; set; }
        /// <summary>
        /// Number of iterations of the hashing algorithm
        /// </summary>
        public int WorkFactor { get; set; }

    }
}