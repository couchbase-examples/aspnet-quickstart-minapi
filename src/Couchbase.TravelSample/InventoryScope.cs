using Couchbase.Extensions.DependencyInjection;
using Couchbase.KeyValue;

namespace Couchbase.TravelSample;

/// <summary>
/// Resolves the scope (travel-sample.inventory by default) that every endpoint reads from and writes to.
/// </summary>
public static class InventoryScope
{
    /// <summary>
    /// Opens the configured bucket and returns the configured scope.
    /// Throws <see cref="InvalidOperationException"/> with a setup hint when the configuration,
    /// bucket or scope is missing, so the application can stop at startup instead of serving errors.
    /// </summary>
    public static async Task<IScope> ResolveAsync(IBucketProvider bucketProvider, string? bucketName, string? scopeName)
    {
        if (string.IsNullOrEmpty(bucketName))
        {
            throw new InvalidOperationException("Bucket name is not provided in the configuration.");
        }

        if (string.IsNullOrEmpty(scopeName))
        {
            throw new InvalidOperationException("Scope name is not provided in the configuration.");
        }

        IBucket bucket;
        try
        {
            bucket = await bucketProvider.GetBucketAsync(bucketName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ensure that you have the travel-sample bucket loaded in the cluster.", ex);
        }

        var scopes = await bucket.Collections.GetAllScopesAsync();

        if (!scopes.Any(s => s.Name == scopeName))
        {
            throw new InvalidOperationException("Inventory scope does not exist in the bucket. Ensure that you have the inventory scope in your travel-sample bucket.");
        }

        return await bucket.ScopeAsync(scopeName);
    }
}
