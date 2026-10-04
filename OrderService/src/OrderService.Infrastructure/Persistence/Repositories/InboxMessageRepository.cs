using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Infrastructure.Persistence.Repositories;

public sealed class InboxMessageRepository : IInboxMessageRepository
{
    private readonly OrderDbContext _dbContext;

    public InboxMessageRepository(OrderDbContext dbContext) => _dbContext = dbContext;

    public async Task<bool> ExistsAsync(string messageId, CancellationToken ct = default) =>
        await _dbContext.InboxMessages.AnyAsync(m => m.MessageId == messageId, ct);

    public async Task AddAsync(InboxMessage message, CancellationToken ct = default) =>
        await _dbContext.InboxMessages.AddAsync(message, ct);
}
