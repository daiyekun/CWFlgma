# 本地文件存储配置

## 存储目录结构

```
D:\woocodetest\OpenCode\CWFlgma\storage\
├── images\
│   ├── {document-id}\
│   │   ├── {resource-id}.png
│   │   ├── {resource-id}.jpg
│   │   └── {resource-id}.svg
│   └── thumbnails\
│       └── {document-id}.png
├── fonts\
│   ├── Roboto\
│   │   ├── Roboto-Regular.ttf
│   │   ├── Roboto-Bold.ttf
│   │   ├── Roboto-Medium.ttf
│   │   └── Roboto-Light.ttf
│   ├── OpenSans\
│   │   └── ...
│   └── NotoSansSC\
│       └── NotoSansSC-Regular.otf
├── exports\
│   └── {document-id}\
│       └── {timestamp}.{format}
└── temp\
    └── {upload-session-id}\
```

## 配置文件

### appsettings.json

```json
{
  "Storage": {
    "Type": "Local",
    "LocalPath": "./storage",
    "BaseUrl": "https://localhost:5004/resources",
    "MaxFileSize": {
      "Image": 10485760,
      "Font": 5242880
    },
    "AllowedImageTypes": [".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp"],
    "AllowedFontTypes": [".ttf", ".otf", ".woff", ".woff2"],
    "ThumbnailSize": {
      "Width": 200,
      "Height": 200
    }
  }
}
```

## IStorageProvider 接口

```csharp
public interface IStorageProvider
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<string> GetPublicUrlAsync(string fileUrl, CancellationToken cancellationToken = default);
}
```

## LocalStorageProvider 实现

```csharp
public class LocalStorageProvider : IStorageProvider
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalStorageProvider(IOptions<StorageOptions> options)
    {
        _basePath = options.Value.LocalPath;
        _baseUrl = options.Value.BaseUrl;
        
        // 确保目录存在
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var relativePath = Path.Combine("images", DateTime.UtcNow.ToString("yyyy/MM/dd"), uniqueName);
        var fullPath = Path.Combine(_basePath, relativePath);
        
        // 确保目录存在
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        
        // 写入文件
        using var fileStream2 = new FileStream(fullPath, FileMode.Create);
        await fileStream.CopyToAsync(fileStream2, cancellationToken);
        
        return relativePath;
    }

    public Task<Stream> DownloadAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, fileUrl);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found", fileUrl);
        
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, fileUrl);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, fileUrl);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<string> GetPublicUrlAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"{_baseUrl}/{fileUrl}");
    }
}
```

## 预置字体

建议预装以下常用字体：

| 字体名称 | 类型 | 下载地址 |
|---------|------|---------|
| Roboto | Google Fonts | https://fonts.google.com/specimen/Roboto |
| Open Sans | Google Fonts | https://fonts.google.com/specimen/Open+Sans |
| Noto Sans SC | Google Fonts | https://fonts.google.com/noto/specimen/Noto+Sans+SC |
| Inter | Google Fonts | https://fonts.google.com/specimen/Inter |
| Poppins | Google Fonts | https://fonts.google.com/specimen/Poppins |

## 图片处理

使用 `SixLabors.ImageSharp` 库进行图片处理：

- 生成缩略图
- 调整尺寸
- 格式转换
- 元数据提取

## 未来迁移到云存储

接口设计已抽象，未来可轻松迁移到：

- Azure Blob Storage
- AWS S3
- 阿里云 OSS
- 腾讯云 COS

只需实现对应的 `IStorageProvider` 即可。
