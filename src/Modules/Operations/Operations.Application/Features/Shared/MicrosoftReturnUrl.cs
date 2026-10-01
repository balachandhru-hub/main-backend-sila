using Operations.Domain.Common;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// The frontend path the browser returns to after the Microsoft sign-in. Only a relative path
    /// of this application is accepted, so the OAuth callback can never redirect to another site.
    /// </summary>
    internal static class MicrosoftReturnUrl
    {
        public static string Normalize(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return Common.MICROSOFT_DEFAULT_RETURN_URL;
            }

            string value = returnUrl.Trim();
            bool relative = value.StartsWith('/')
                && !value.StartsWith("//", StringComparison.Ordinal)
                && !value.Contains('\\')
                && !value.Contains("://", StringComparison.Ordinal)
                && !value.Any(char.IsControl)
                && value.Length <= 200;
            return relative ? value : Common.MICROSOFT_DEFAULT_RETURN_URL;
        }
    }
}
