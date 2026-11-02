using Olli.Api.Application.Services;

namespace Olli.Api.Infrastructure.Persistence;

public sealed class NullRecommendationAuditRepository : IRecommendationAuditRepository
{
    public Task AddAsync(RecommendationAudit audit, CancellationToken cancellationToken) => Task.CompletedTask;
}
