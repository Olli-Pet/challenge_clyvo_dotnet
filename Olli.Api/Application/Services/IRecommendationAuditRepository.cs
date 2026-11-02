namespace Olli.Api.Application.Services;

public sealed record RecommendationAudit(
    int PetId,
    int ScoreSaude,
    string NivelPrioridade,
    DateTime CreatedAtUtc);

public interface IRecommendationAuditRepository
{
    Task AddAsync(RecommendationAudit audit, CancellationToken cancellationToken);
}
