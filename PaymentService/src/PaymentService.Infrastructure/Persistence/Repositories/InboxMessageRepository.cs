using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Repositories;

namespace PaymentService.Infrastructure.Persistence.Repositories;

public sealed class InboxMessageRepository : IInboxMessageRepository
{
    private readonly PaymentDbContext _dbContext;

    public InboxMessageRepository(PaymentDbContext dbContext) => _dbContext = dbContext;

    public async Task<bool> ExistsAsync(string messageId, CancellationToken ct = default) =>
        await _dbContext.InboxMessages.AnyAsync(m => m.MessageId == messageId, ct);

    public async Task AddAsync(InboxMessage message, CancellationToken ct = default) =>
        await _dbContext.InboxMessages.AddAsync(message, ct);
}
