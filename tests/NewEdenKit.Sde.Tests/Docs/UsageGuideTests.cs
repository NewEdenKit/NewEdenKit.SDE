using NewEdenKit.Sde.Models;

namespace NewEdenKit.Sde.Tests.Docs;

/// <summary>
/// Keeps the consumer docs (README, <c>docs/usage.md</c>, <c>llms.txt</c>) honest: their code is the compiled code in
/// <see cref="UsageExamples"/>, and the examples behave as the guide says. Run with <c>UPDATE_USAGE_GUIDE=1</c> to
/// copy the regions into the docs.
/// </summary>
public sealed class UsageGuideTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly SdeLoader _loader;

    public UsageGuideTests()
    {
        _loader = new SdeLoader(new SdeLoaderTests.FakeClient(), new SdeLoaderOptions { CacheDirectory = Path.Combine(_temp.Path, "cache") });
    }

    public void Dispose() => _temp.Dispose();

    private static ISdeReader Sample => SdeReader.FromDirectory(TestSde.Directory);

    [Fact]
    public void Docs_show_exactly_the_compiled_examples()
    {
        var examples = Snippets.FromExamples();
        if (Environment.GetEnvironmentVariable("UPDATE_USAGE_GUIDE") == "1")
        {
            foreach (var document in Snippets.Documents)
                Snippets.Update(document, examples);
        }

        var shown = new HashSet<string>(StringComparer.Ordinal);
        foreach (var document in Snippets.Documents)
        {
            foreach (var (name, code) in Snippets.FromDocument(document))
            {
                Assert.True(examples.ContainsKey(name), $"{document} shows snippet '{name}', which UsageExamples.cs doesn't have.");
                Assert.True(code == examples[name], $"{document} snippet '{name}' differs from UsageExamples.cs. Run the tests with UPDATE_USAGE_GUIDE=1.");
                shown.Add(name);
            }
        }

        Assert.Empty(examples.Keys.Except(shown));
    }

    [Fact]
    public async Task Read_archive_lists_market_types()
    {
        var names = await UsageExamples.ReadArchive(_loader, default);

        Assert.Contains("Tritanium", names);
        Assert.Contains("Rifter", names);
    }

    [Fact]
    public async Task File_store_then_in_memory_data()
    {
        var directory = Path.Combine(_temp.Path, "store");

        var first = await UsageExamples.CacheModels(_loader, directory, default);
        var second = await UsageExamples.CacheModels(_loader, directory, default);
        var data = await UsageExamples.SdeData.LoadAsync(new SdeFileStore(directory), default);

        Assert.False(first.UpToDate);
        Assert.True(second.UpToDate);
        Assert.Equal("Material", data.CategoryOf(34));
    }

    [Fact]
    public async Task Progress_reports_every_dataset()
    {
        var result = await UsageExamples.ReportProgress(_loader, new SdeFileStore(Path.Combine(_temp.Path, "store")), default);

        Assert.Equal(SdeDatasets.Items.Count, result.ImportedDatasets.Count);
    }

    [Fact]
    public async Task Custom_store_holds_type_names()
    {
        var names = await UsageExamples.UseCustomStore(_loader, default);

        Assert.Equal("Tritanium", names[34]);
    }

    [Fact]
    public async Task Localized_falls_back_to_english()
    {
        var rifter = (await Sample.ReadDictionaryAsync(SdeDatasets.Types))[587];

        Assert.Equal(rifter.Name.Ja, UsageExamples.Localized(rifter));
        Assert.Equal("Rifter", UsageExamples.Localized(rifter with { Name = new LocalizedString(new Dictionary<string, string> { ["en"] = "Rifter" }) }));
    }

    [Fact]
    public async Task Dogma_lists_every_attribute_of_a_type()
    {
        var typeDogma = Assert.Single(await Sample.ReadListAsync(SdeDatasets.TypeDogma));

        var values = await UsageExamples.Attributes(Sample, typeDogma.Id, default);

        Assert.Equal(typeDogma.DogmaAttributes.Count, values.Count);
        Assert.Empty(await UsageExamples.Attributes(Sample, -1, default));
    }

    [Fact]
    public async Task Blueprints_find_the_rifter_materials()
    {
        var materials = await UsageExamples.Materials(Sample, 587, default);

        Assert.NotEmpty(materials);
        Assert.Empty(await UsageExamples.Materials(Sample, -1, default));
    }

    [Fact]
    public async Task Map_follows_stargates()
    {
        // The sample holds one of Jita's stargates.
        var stargate = (await Sample.ReadDictionaryAsync(SdeDatasets.Stargates))[50001248];

        var neighbours = await UsageExamples.Neighbours(Sample, 30000142, default);

        Assert.Equal([stargate.Destination.SolarSystemId], neighbours);
    }

    [Fact]
    public async Task Raw_reads_untyped_datasets()
    {
        Assert.Equal(2, (await UsageExamples.Raw(Sample, default)).Count);
    }

    [Fact]
    public async Task Custom_model_reads_only_its_fields()
    {
        var summaries = await UsageExamples.CustomModel(Sample, default);

        Assert.Equal("Rifter", summaries[587].Name.En);
        Assert.Equal(25, summaries[587].GroupId);
        Assert.NotNull(summaries[587].AdditionalProperties);
    }

    [Fact]
    public async Task Testing_reads_a_folder_of_records()
    {
        Assert.Equal("Tritanium", await UsageExamples.Testing(TestSde.Directory));
    }

    [Fact]
    public void Client_can_be_created_from_an_http_client()
    {
        using var client = UsageExamples.CreateClient(new HttpClient());

        Assert.EndsWith("-jsonl.zip", client.GetArchiveUri(1).ToString());
    }
}
