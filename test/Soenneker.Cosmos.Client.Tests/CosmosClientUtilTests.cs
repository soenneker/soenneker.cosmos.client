using System.Threading.Tasks;
using Soenneker.Cosmos.Client.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;


namespace Soenneker.Cosmos.Client.Tests;

[Collection("Collection")]
public class CosmosClientUtilTests : FixturedUnitTest
{
    private readonly ICosmosClientUtil _util;

    public CosmosClientUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<ICosmosClientUtil>(true);
    }

    [Fact]
    public async Task Default()
    {
        const string endpoint = "https://test-endpoint.documents.azure.com:443/";
        const string accountKey = "test-account-key";

        var client = await _util.Get(endpoint, accountKey, TestContext.Current.CancellationToken);
        
        Assert.NotNull(client);
    }
}
