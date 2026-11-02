namespace Olli.Api.Infrastructure.Persistence;

public sealed class MongoDbOptions
{
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "olli";
    public string AuditCollectionName { get; set; } = "recommendation_audits";
}
