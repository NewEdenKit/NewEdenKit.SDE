using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using NewEdenKit.Sde.Serialization;

namespace NewEdenKit.Sde;

/// <summary>Access to CCP's static data endpoints.</summary>
public interface ISdeClient
{
    /// <summary>Returns the latest published build.</summary>
    Task<SdeBuildInfo> GetLatestBuildAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the changes published with <paramref name="buildNumber"/>, or <see langword="null"/>
    /// when no change set exists for that build.
    /// </summary>
    Task<SdeChangeSet?> GetChangesAsync(long buildNumber, CancellationToken cancellationToken = default);

    /// <summary>The download URL of an archive.</summary>
    Uri GetArchiveUri(long buildNumber, SdeVariant variant = SdeVariant.JsonLines);

    /// <summary>
    /// Downloads an archive to <paramref name="destinationPath"/>. The file only appears at that path
    /// once the download has completed.
    /// </summary>
    Task DownloadArchiveAsync(
        long buildNumber,
        string destinationPath,
        SdeVariant variant = SdeVariant.JsonLines,
        IProgress<SdeDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Progress of an archive download.</summary>
/// <param name="BytesReceived">Bytes downloaded so far.</param>
/// <param name="TotalBytes">Size of the archive, when the server reports it.</param>
public readonly record struct SdeDownloadProgress(long BytesReceived, long? TotalBytes)
{
    /// <summary>Fraction downloaded from 0 to 1, when the size is known.</summary>
    public double? Fraction => TotalBytes is > 0 ? (double)BytesReceived / TotalBytes.Value : null;
}

/// <summary>Settings for <see cref="SdeClient"/>.</summary>
public sealed class SdeClientOptions
{
    /// <summary>Root of the static data endpoints.</summary>
    public Uri BaseAddress { get; set; } = new("https://developers.eveonline.com/static-data/");

    /// <summary>The server whose SDE to use.</summary>
    public string Environment { get; set; } = "tranquility";

    /// <summary>
    /// The User-Agent sent with each request. CCP asks API consumers to identify themselves, so
    /// consider adding your application name and contact details.
    /// </summary>
    public string UserAgent { get; set; } =
        $"NewEdenKit.Sde/{typeof(SdeClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0"}";
}

/// <summary>HTTP client for CCP's static data endpoints at <c>developers.eveonline.com/static-data</c>.</summary>
public sealed class SdeClient : ISdeClient, IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly SdeClientOptions _options;

    /// <summary>Creates a client that uses <paramref name="httpClient"/> (for example one from <c>IHttpClientFactory</c>).</summary>
    public SdeClient(HttpClient httpClient, SdeClientOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _http = httpClient;
        _options = options ?? new SdeClientOptions();
    }

    /// <summary>Creates a client with its own <see cref="HttpClient"/>.</summary>
    public SdeClient(SdeClientOptions? options = null)
        : this(new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }), options)
    {
        _ownsHttpClient = true;
    }

    private Uri EnvironmentUri(string relative) =>
        new(_options.BaseAddress, $"{Uri.EscapeDataString(_options.Environment)}/{relative}");

    /// <inheritdoc />
    public async Task<SdeBuildInfo> GetLatestBuildAsync(CancellationToken cancellationToken = default)
    {
        foreach (var line in await GetLinesAsync(EnvironmentUri("latest.jsonl"), cancellationToken).ConfigureAwait(false) ?? [])
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            using var document = JsonDocument.Parse(line);
            if (document.RootElement.TryGetProperty("_key", out var key) && key.ValueEquals("sde"))
                return document.RootElement.Deserialize<SdeBuildInfo>(SdeJson.Options)!;
        }

        throw new SdeException("The latest build response does not contain an 'sde' record.");
    }

    /// <inheritdoc />
    public async Task<SdeChangeSet?> GetChangesAsync(long buildNumber, CancellationToken cancellationToken = default)
    {
        var lines = await GetLinesAsync(EnvironmentUri($"changes/{buildNumber}.jsonl"), cancellationToken).ConfigureAwait(false);
        return lines is null ? null : SdeChangeSet.Parse(lines);
    }

    /// <inheritdoc />
    public Uri GetArchiveUri(long buildNumber, SdeVariant variant = SdeVariant.JsonLines) =>
        EnvironmentUri(variant.GetArchiveFileName(buildNumber));

    /// <inheritdoc />
    public async Task DownloadArchiveAsync(
        long buildNumber,
        string destinationPath,
        SdeVariant variant = SdeVariant.JsonLines,
        IProgress<SdeDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        destinationPath = Path.GetFullPath(destinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        using var request = CreateRequest(GetArchiveUri(buildNumber, variant));
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var total = response.Content.Headers.ContentLength;
        var partialPath = $"{destinationPath}.{Guid.NewGuid():N}.partial";
        try
        {
            var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (source.ConfigureAwait(false))
            {
                var target = new FileStream(partialPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
                await using (target.ConfigureAwait(false))
                {
                    var buffer = new byte[81920];
                    long received = 0;
                    progress?.Report(new SdeDownloadProgress(0, total));
                    int read;
                    while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                        received += read;
                        progress?.Report(new SdeDownloadProgress(received, total));
                    }
                }
            }

            File.Move(partialPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(partialPath))
                File.Delete(partialPath);
        }
    }

    private async Task<string[]?> GetLinesAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(uri);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
            return null;

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return body.Split('\n');
    }

    private HttpRequestMessage CreateRequest(Uri uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (!string.IsNullOrWhiteSpace(_options.UserAgent))
            request.Headers.UserAgent.ParseAdd(_options.UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (body.Length > 500)
            body = body[..500];
        throw new HttpRequestException(
            $"GET {response.RequestMessage?.RequestUri} failed with {(int)response.StatusCode} {response.ReasonPhrase}: {body}",
            null,
            response.StatusCode);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient)
            _http.Dispose();
    }
}
