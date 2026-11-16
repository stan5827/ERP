using System.Buffers.Binary;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace ErpBackend.Infrastructure.CapaAttachments;

public sealed class CapaAttachmentOptions
{
    public const string SectionName = "CapaAttachments";
    public long MaximumFileBytes { get; set; } = 20 * 1024 * 1024;
    public int MaximumAttachmentsPerCapa { get; set; } = 10;
    public string MinioServiceUrl { get; set; } = string.Empty;
    public string MinioBucket { get; set; } = string.Empty;
    public string MinioAccessKey { get; set; } = string.Empty;
    public string MinioSecretKey { get; set; } = string.Empty;
    public string MinioRegion { get; set; } = "us-east-1";
    public string ClamAvHost { get; set; } = string.Empty;
    public int ClamAvPort { get; set; } = 3310;
    public int ClamAvTimeoutSeconds { get; set; } = 30;
    public bool AllowInsecureLocalMinio { get; set; }
}

public sealed record CapaStoredObjectMetadata(
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Sha256);

public enum CapaMalwareScanResult
{
    Clean,
    Infected
}

public sealed class CapaAttachmentConfigurationException(string message) : Exception(message);

public sealed class CapaAttachmentScannerUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public interface ICapaPrivateObjectStorage
{
    Task PutAsync(
        string storageKey,
        Stream content,
        long contentLength,
        string contentType,
        CancellationToken cancellationToken);

    Task CopyToAsync(string storageKey, Stream destination, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}

public interface ICapaMalwareScanner
{
    Task<CapaMalwareScanResult> ScanAsync(
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken);
}

public sealed class MinioCapaObjectStorage(
    IOptions<CapaAttachmentOptions> options,
    IHostEnvironment environment,
    ILogger<MinioCapaObjectStorage> logger) : ICapaPrivateObjectStorage, IDisposable
{
    private readonly CapaAttachmentOptions _options = options.Value;
    private IAmazonS3? _client;

    public async Task PutAsync(
        string storageKey,
        Stream content,
        long contentLength,
        string contentType,
        CancellationToken cancellationToken)
    {
        var client = GetClient();
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.MinioBucket,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken);
    }

    public async Task CopyToAsync(
        string storageKey,
        Stream destination,
        CancellationToken cancellationToken)
    {
        using var response = await GetClient().GetObjectAsync(
            _options.MinioBucket,
            storageKey,
            cancellationToken);
        await response.ResponseStream.CopyToAsync(destination, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var response = await GetClient().GetObjectAsync(
            _options.MinioBucket,
            storageKey,
            cancellationToken);
        return new S3ResponseStream(response);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        await GetClient().DeleteObjectAsync(
            _options.MinioBucket,
            storageKey,
            cancellationToken);
    }

    private IAmazonS3 GetClient()
    {
        if (_client is not null)
            return _client;

        if (!Uri.TryCreate(_options.MinioServiceUrl, UriKind.Absolute, out var serviceUri)
            || serviceUri.Scheme is not ("http" or "https")
            || string.IsNullOrWhiteSpace(_options.MinioBucket)
            || string.IsNullOrWhiteSpace(_options.MinioAccessKey)
            || string.IsNullOrWhiteSpace(_options.MinioSecretKey))
            throw new CapaAttachmentConfigurationException(
                "CAPA 附件 MinIO endpoint、bucket 及認證設定不完整。");

        if (serviceUri.Scheme != Uri.UriSchemeHttps
            && !(environment.IsDevelopment()
                && _options.AllowInsecureLocalMinio
                && serviceUri.IsLoopback))
            throw new CapaAttachmentConfigurationException(
                "CAPA 附件 MinIO 必須使用 HTTPS；僅 Development loopback 可明確允許 HTTP。");

        var config = new AmazonS3Config
        {
            ServiceURL = serviceUri.ToString().TrimEnd('/'),
            ForcePathStyle = true,
            AuthenticationRegion = string.IsNullOrWhiteSpace(_options.MinioRegion)
                ? "us-east-1"
                : _options.MinioRegion
        };
        _client = new AmazonS3Client(
            new BasicAWSCredentials(_options.MinioAccessKey, _options.MinioSecretKey),
            config);
        logger.LogInformation("CAPA private object storage client configured for {Host}", serviceUri.Host);
        return _client;
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private sealed class S3ResponseStream(GetObjectResponse response) : Stream
    {
        private readonly Stream _inner = response.ResponseStream;

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            _inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException("CAPA attachment streams are read-only.");

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                response.Dispose();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();
            response.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

public sealed class ClamAvCapaMalwareScanner(
    IOptions<CapaAttachmentOptions> options,
    ILogger<ClamAvCapaMalwareScanner> logger) : ICapaMalwareScanner
{
    private readonly CapaAttachmentOptions _options = options.Value;

    public async Task<CapaMalwareScanResult> ScanAsync(
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ClamAvHost)
            || _options.ClamAvPort is < 1 or > 65535
            || _options.ClamAvTimeoutSeconds is < 1 or > 300)
            throw new CapaAttachmentConfigurationException(
                "CAPA 附件 ClamAV host、port 或 timeout 設定不完整。");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.ClamAvTimeoutSeconds));

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_options.ClamAvHost, _options.ClamAvPort, timeout.Token);
            await using var stream = client.GetStream();
            await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), timeout.Token);

            const int chunkSize = 64 * 1024;
            var chunkLength = new byte[sizeof(int)];
            for (var offset = 0; offset < content.Length; offset += chunkSize)
            {
                var length = Math.Min(chunkSize, content.Length - offset);
                BinaryPrimitives.WriteInt32BigEndian(chunkLength, length);
                await stream.WriteAsync(chunkLength, timeout.Token);
                await stream.WriteAsync(content.Slice(offset, length), timeout.Token);
            }

            BinaryPrimitives.WriteInt32BigEndian(chunkLength, 0);
            await stream.WriteAsync(chunkLength, timeout.Token);
            await stream.FlushAsync(timeout.Token);

            var response = await ReadResponseAsync(stream, timeout.Token);
            if (response.EndsWith(" OK", StringComparison.Ordinal))
                return CapaMalwareScanResult.Clean;
            if (response.EndsWith(" FOUND", StringComparison.Ordinal))
                return CapaMalwareScanResult.Infected;

            throw new IOException("ClamAV returned an unrecognized scan result.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CapaAttachmentScannerUnavailableException("CAPA 附件掃描逾時。");
        }
        catch (Exception exception) when (exception is SocketException or IOException)
        {
            logger.LogWarning(exception, "CAPA attachment malware scanner is unavailable");
            throw new CapaAttachmentScannerUnavailableException(
                "CAPA 附件掃描服務目前無法使用。",
                exception);
        }
    }

    private static async Task<string> ReadResponseAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        var response = new List<byte>(128);
        var buffer = new byte[128];
        while (response.Count < 4096)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;
            var terminator = Array.IndexOf(buffer, (byte)0, 0, read);
            var count = terminator < 0 ? read : terminator;
            response.AddRange(buffer.AsSpan(0, count).ToArray());
            if (terminator >= 0)
                break;
        }

        if (response.Count == 0 || response.Count >= 4096)
            throw new IOException("ClamAV returned an empty or oversized result.");
        return Encoding.ASCII.GetString(response.ToArray());
    }
}
