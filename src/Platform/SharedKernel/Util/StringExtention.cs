using System.Text.RegularExpressions;

namespace SharedKernel.Util
{
    /// <summary>
    /// Class <c>StringExtensions</c> an extension class consists of string manipulation
    /// </summary>
    public static partial class StringExtensions
    {
        /// <summary>
        /// This method converts the string to snake case.
        /// </summary>
        /// <param name="input">The string input to be converted as snake case.</param>
        /// <returns>A string converted to snake case.</returns>
        /// <example>
        /// Use this method with any string to be converted as snake case
        /// Usage: string.ConvertToSnakeCase()
        /// For e.g. The given input string is PersonName will be converted to 
        /// person_name.
        /// </example>
        public static string ConvertToSnakeCase(this string input)
        {
            if (System.String.IsNullOrEmpty(input)) { return input; }

            Match startUnderscores = MyRegex().Match(input);
            return startUnderscores + MyRegex1().Replace(input, "$1_$2").ToLower();
        }

        [GeneratedRegex(@"^_+")]
        private static partial Regex MyRegex();
        [GeneratedRegex(@"([a-z0-9])([A-Z])")]
        private static partial Regex MyRegex1();
    }
}