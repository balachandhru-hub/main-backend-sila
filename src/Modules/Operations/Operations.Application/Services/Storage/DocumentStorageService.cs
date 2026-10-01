using Microsoft.Extensions.Configuration;
using Operations.Domain.Common;

namespace Operations.Application.Services.Storage
{
    public interface IDocumentStorageService
    {
        /// <summary>Stores the content and returns the storage reference "{organizationId}/{file}".</summary>
        Task<string> StoreAsync(Guid organizationId, byte[] content, string originalFilename, CancellationToken cancellationToken);

        /// <summary>Reads a stored document. Throws <see cref="FileNotFoundException"/> when it is missing.</summary>
        Task<byte[]> ReadAsync(string reference, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Local-disk document storage. The root comes from <c>FolderPath:BasePath</c> and every
    /// organization gets its own folder: {BasePath}/operations/documents/{organizationId}/{file}.
    /// </summary>
    public class LocalDocumentStorageService : IDocumentStorageService
    {
        private readonly string _root;

        public LocalDocumentStorageService(IConfiguration configuration)
        {
            string? basePath = configuration[Common.BASE_FOLDER_PATH];
            _root = Path.GetFullPath(Path.Combine(
                string.IsNullOrWhiteSpace(basePath) ? AppContext.BaseDirectory : basePath,
                Common.DOCUMENT_SUBFOLDER));
        }

        public async Task<string> StoreAsync(Guid organizationId, byte[] content, string originalFilename, CancellationToken cancellationToken)
        {
            string folder = organizationId.ToString("N");
            Directory.CreateDirectory(Path.Combine(_root, folder));
            string fileName = $"{Guid.NewGuid():N}{Path.GetExtension(originalFilename).ToLowerInvariant()}";
            await File.WriteAllBytesAsync(Path.Combine(_root, folder, fileName), content, cancellationToken);
            return $"{folder}/{fileName}";
        }

        public async Task<byte[]> ReadAsync(string reference, CancellationToken cancellationToken)
        {
            // A reference is "{organizationId}/{file}". Each segment is reduced to a bare file
            // name, so a stored reference can never point outside the storage root.
            string[] segments = reference.Split('/', '\\')
                .Select(segment => Path.GetFileName(segment))
                .Where(segment => !string.IsNullOrWhiteSpace(segment) && segment != "." && segment != "..")
                .ToArray();
            string path = segments.Length == 2 ? Path.Combine(_root, segments[0], segments[1]) : string.Empty;
            if (path.Length == 0 || !File.Exists(path))
            {
                throw new FileNotFoundException("The stored document could not be found.", reference);
            }

            return await File.ReadAllBytesAsync(path, cancellationToken);
        }
    }
}
