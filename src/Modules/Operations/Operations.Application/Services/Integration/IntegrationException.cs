namespace Operations.Application.Services.Integration
{
    /// <summary>
    /// Raised by the integration engines (HTTP executor, spreadsheet engine). Handlers translate it
    /// into the solution's exceptions; <see cref="Status"/> is the HTTP status that fits the failure.
    /// </summary>
    public class IntegrationException : Exception
    {
        public IntegrationException(string code, string message, int status = 400) : base(message)
        {
            Code = code;
            Status = status;
        }

        public string Code { get; }

        public int Status { get; }
    }
}
