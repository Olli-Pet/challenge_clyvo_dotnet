using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Olli.Api.Application.Services;

namespace Olli.Api.Infrastructure.Persistence;

public sealed class MongoRecommendationAuditRepository : IRecommendationAuditRepository
{
    private readonly IMongoCollection<RecommendationAuditDocument> _collection;

    public MongoRecommendationAuditRepository(IOptions<MongoDbOptions> options)
    {
        var value = options.Value;
        var client = new MongoClient(value.ConnectionString);
        var database = client.GetDatabase(value.DatabaseName);
        _collection = database.GetCollection<RecommendationAuditDocument>(value.AuditCollectionName);
    }

    public Task AddAsync(RecommendationAudit audit, CancellationToken cancellationToken) =>
        _collection.InsertOneAsync(new RecommendationAuditDocument
        {
            PetId = audit.PetId,
            ScoreSaude = audit.ScoreSaude,
            NivelPrioridade = audit.NivelPrioridade,
            CreatedAtUtc = audit.CreatedAtUtc
        }, cancellationToken: cancellationToken);

    private sealed class RecommendationAuditDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public int PetId { get; set; }
        public int ScoreSaude { get; set; }
        public string NivelPrioridade { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
