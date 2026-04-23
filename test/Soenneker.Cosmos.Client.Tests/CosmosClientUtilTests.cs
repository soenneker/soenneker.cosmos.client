using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Azure.Cosmos;
using Soenneker.Cosmos.Client.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Cosmos.Client.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class CosmosClientUtilTests : HostedUnitTest
{
    private readonly ICosmosClientUtil _util;

    public CosmosClientUtilTests(Host host) : base(host)
    {
        _util = Resolve<ICosmosClientUtil>(true);
    }

    [Test]
    public void Default()
    {
    }

    [Test]
    public async Task Get_TwoClients_AreNotNull()
    {
        CosmosClient client1 = await _util.Get("https://localhost:8081", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", System.Threading.CancellationToken.None);
        CosmosClient client2 = await _util.Get("https://localhost:8080", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", System.Threading.CancellationToken.None);

        client1.Should()
               .NotBeNull();
        client2.Should()
               .NotBeNull();
        client1.Should()
               .NotBe(client2);
    }
}
