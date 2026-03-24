using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace CWFlgma.Infrastructure.Storage;

public class StorageOptions
{
    public string Type { get; set; } = "Local";
    public string LocalPath { get; set; } = "./storage";
    public string BaseUrl { get; set; } = "https://localhost:5004/resources";
    public long MaxImageSize { get; set; } = 10 * 1024 * 1024; // 10MB
    public long MaxFontSize { get; set; } = 5 * 1024 * 1024;   // 5MB
    public string[] AllowedImageTypes { get; set; } = { ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp" };
    public string[] AllowedFontTypes { get; set; } = { ".ttf", ".otf", ".woff", ".woff2" };
    public int ThumbnailWidth { get; set; } = 200;
    public int ThumbnailHeight { get; set; } = 200;
}

public interface IStorageProvider
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<string> GetPublicUrlAsync(string fileUrl, CancellationToken cancellationToken = default);
}

public class LocalStorageProvider : IStorageProvider
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalStorageProvider(IOptions<StorageOptions> options)
    {
        _basePath = Path.GetFullPath(options.Value.LocalPath);
        _baseUrl = options.Value.BaseUrl;

        Directory.CreateDirectory(_basePath);
        Directory.CreateDirectory(Path.Combine(_basePath, "images"));
        Directory.CreateDirectory(Path.Combine(_basePath, "fonts"));
        Directory.CreateDirectory(Path.Combine(_basePath, "exports"));
        Directory.CreateDirectory(Path.Combine(_basePath, "temp"));
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var relativePath = Path.Combine("images", datePath, uniqueName);
        var fullPath = Path.Combine(_basePath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var targetStream = new FileStream(fullPath, FileMode.Create);
        await fileStream.CopyToAsync(targetStream, cancellationToken);

        return relativePath.Replace('\\', '/');
    }

    public Task<Stream> DownloadAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, fileUrl.Replace('/', '\\'));
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found", fileUrl);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, fileUrl.Replace('/', '\\'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, fileUrl.Replace('/', '\\'));
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<string> GetPublicUrlAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"{_baseUrl}/{fileUrl}");
    }
}
