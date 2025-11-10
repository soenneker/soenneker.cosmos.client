using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Azure.Cosmos;
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
    public void Default()
    {
    }

    [Fact]
    public async Task Get_TwoClients_AreNotNull()
    {
        CosmosClient client1 = await _util.Get("https://localhost:8081", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", CancellationToken);
        CosmosClient client2 = await _util.Get("https://localhost:8080", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", CancellationToken);

        client1.Should()
               .NotBeNull();
        client2.Should()
               .NotBeNull();
        client1.Should()
               .NotBe(client2);
    }
}