using System.Reflection;
using Couchbase.Extensions.DependencyInjection;
using Couchbase.KeyValue;
using Couchbase.Management.Collections;

namespace Couchbase.TravelSample.Tests;

// Runs without a Couchbase cluster: the SDK interfaces are faked with DispatchProxy.
public class InventoryScopeTests
{
    [Fact]
    public async Task ResolvesConfiguredScope()
    {
        var cluster = new FakeCluster();

        var scope = await InventoryScope.ResolveAsync(cluster.BucketProvider, "travel-sample", "inventory");

        Assert.Same(cluster.Scope, scope);
        Assert.Equal(new[] { "travel-sample" }, cluster.RequestedBuckets);
        Assert.Equal(new[] { "inventory" }, cluster.RequestedScopes);
    }

    [Theory]
    [InlineData(null, "inventory", "Bucket name is not provided")]
    [InlineData("", "inventory", "Bucket name is not provided")]
    [InlineData("travel-sample", null, "Scope name is not provided")]
    public async Task MissingConfigurationIsReportedWithoutContactingTheCluster(string? bucketName, string? scopeName, string expected)
    {
        var cluster = new FakeCluster();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InventoryScope.ResolveAsync(cluster.BucketProvider, bucketName, scopeName));

        Assert.Contains(expected, ex.Message);
        Assert.Empty(cluster.RequestedBuckets);
    }

    [Fact]
    public async Task MissingBucketIsReported()
    {
        var cluster = new FakeCluster { BucketAvailable = false };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InventoryScope.ResolveAsync(cluster.BucketProvider, "travel-sample", "inventory"));

        Assert.Contains("travel-sample bucket", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task MissingScopeIsReported()
    {
        var cluster = new FakeCluster { ScopeNames = new[] { "_default" } };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InventoryScope.ResolveAsync(cluster.BucketProvider, "travel-sample", "inventory"));

        Assert.Contains("Inventory scope does not exist", ex.Message);
        Assert.Empty(cluster.RequestedScopes);
    }

    private sealed class FakeCluster
    {
        public bool BucketAvailable { get; set; } = true;
        public string[] ScopeNames { get; set; } = { "_default", "inventory" };
        public List<string> RequestedBuckets { get; } = new();
        public List<string> RequestedScopes { get; } = new();

        public IScope Scope { get; }
        public IBucketProvider BucketProvider { get; }

        public FakeCluster()
        {
            Scope = Fake<IScope>.Create((method, _) => throw new NotSupportedException(method.Name));

            var collections = Fake<ICouchbaseCollectionManager>.Create((method, _) =>
            {
                if (method.Name != nameof(ICouchbaseCollectionManager.GetAllScopesAsync))
                    throw new NotSupportedException(method.Name);
                return Task.FromResult<IEnumerable<ScopeSpec>>(ScopeNames.Select(name => new ScopeSpec(name)).ToList());
            });

            var bucket = Fake<IBucket>.Create((method, args) =>
            {
                switch (method.Name)
                {
                    case "get_Collections":
                        return collections;
                    case nameof(IBucket.ScopeAsync):
                        RequestedScopes.Add((string)args[0]!);
                        return new ValueTask<IScope>(Scope);
                    default:
                        throw new NotSupportedException(method.Name);
                }
            });

            BucketProvider = Fake<IBucketProvider>.Create((method, args) =>
            {
                if (method.Name != nameof(IBucketProvider.GetBucketAsync))
                    throw new NotSupportedException(method.Name);
                RequestedBuckets.Add((string)args[0]!);
                return BucketAvailable
                    ? new ValueTask<IBucket>(bucket)
                    : ValueTask.FromException<IBucket>(new Exception("bucket not found"));
            });
        }
    }

    public class Fake<T> : DispatchProxy where T : class
    {
        private Func<MethodInfo, object?[], object?> _handler = null!;

        public static T Create(Func<MethodInfo, object?[], object?> handler)
        {
            var proxy = Create<T, Fake<T>>();
            ((Fake<T>)(object)proxy)._handler = handler;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => _handler(targetMethod!, args ?? Array.Empty<object?>());
    }
}
