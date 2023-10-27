namespace Couchbase.TravelSample.Models;

public record CouchbaseConfig
{
    public string BucketName { get; set; } = "";
    public string ScopeName { get; set; } = "";
    public string RestEndpoint { get; set; } = "";

    public bool IgnoreRemoteCertificateNameMismatch { get; set; }
    public bool HttpIgnoreRemoteCertificateMismatch { get; set; }
    public bool KvIgnoreRemoteCertificateNameMismatch { get; set; }

    public string ConnectionString { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}