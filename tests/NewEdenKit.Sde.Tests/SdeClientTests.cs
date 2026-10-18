using System.Net;

namespace NewEdenKit.Sde.Tests;

public sealed class SdeClientTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Text(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };

    [Fact]
    public async Task Gets_latest_build()
    {
        var handler = new FakeHandler(_ => Text("""{"_key": "sde", "buildNumber": 3607705, "releaseDate": "2026-10-12T11:08:38Z"}""" + "\n"));
        using var client = new SdeClient(new HttpClient(handler));

        var build = await client.GetLatestBuildAsync();

        Assert.Equal(3607705, build.BuildNumber);
        Assert.Equal("https://developers.eveonline.com/static-data/tranquility/latest.jsonl", handler.Requests[0].RequestUri!.ToString());
        Assert.StartsWith("NewEdenKit.Sde/", handler.Requests[0].Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task Gets_changes()
    {
        var handler = new FakeHandler(_ => Text("""
            {"_key":"_meta","buildNumber":3607705,"lastBuildNumber":3586130,"releaseDate":"2026-10-12T11:08:38Z"}
            {"_key":"icons","added":[27859]}
            {"_key":"types","changed":[95742,97659],"changedLocalization":[97710]}
            {"_key":"characterTitles","removed":["title_a"]}
            """));
        using var client = new SdeClient(new HttpClient(handler));

        var changes = (await client.GetChangesAsync(3607705))!;

        Assert.Equal("https://developers.eveonline.com/static-data/tranquility/changes/3607705.jsonl", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal(3586130, changes.LastBuildNumber);
        Assert.Equal(["27859"], changes.Datasets["icons"].Added);
        Assert.Equal(["95742", "97659"], changes.Datasets["types"].Changed);
        Assert.Equal(["97710"], changes.Datasets["types"].ChangedLocalization);
        Assert.Equal(["title_a"], changes.Datasets["characterTitles"].Removed);
        Assert.Equal("types", changes.Datasets["types"].Dataset);
    }

    [Fact]
    public async Task Returns_null_when_change_set_does_not_exist()
    {
        using var client = new SdeClient(new HttpClient(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))));

        Assert.Null(await client.GetChangesAsync(1));
    }

    [Fact]
    public void Builds_archive_uris()
    {
        using var client = new SdeClient(new HttpClient());

        Assert.Equal(
            "https://developers.eveonline.com/static-data/tranquility/eve-online-static-data-3607705-jsonl.zip",
            client.GetArchiveUri(3607705).ToString());
        Assert.Equal(
            "https://developers.eveonline.com/static-data/tranquility/eve-online-static-data-3607705-yaml.zip",
            client.GetArchiveUri(3607705, SdeVariant.Yaml).ToString());
    }

    [Fact]
    public async Task Downloads_archive_with_progress()
    {
        var payload = new byte[300_000];
        Random.Shared.NextBytes(payload);
        using var client = new SdeClient(new HttpClient(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) })));
        using var temp = new TempDirectory();
        var target = Path.Combine(temp.Path, "nested", "sde.zip");
        var reports = new List<SdeDownloadProgress>();

        await client.DownloadArchiveAsync(1, target, progress: new SynchronousProgress<SdeDownloadProgress>(reports.Add));

        Assert.Equal(payload, await File.ReadAllBytesAsync(target));
        Assert.Equal(payload.Length, reports[^1].BytesReceived);
        Assert.Equal(1.0, reports[^1].Fraction);
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(target)!));
    }

    [Fact]
    public async Task Failed_download_leaves_no_file()
    {
        using var client = new SdeClient(new HttpClient(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError))));
        using var temp = new TempDirectory();
        var target = Path.Combine(temp.Path, "sde.zip");

        await Assert.ThrowsAsync<HttpRequestException>(() => client.DownloadArchiveAsync(1, target));

        Assert.Empty(Directory.GetFiles(temp.Path));
    }
}

internal sealed class SynchronousProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
