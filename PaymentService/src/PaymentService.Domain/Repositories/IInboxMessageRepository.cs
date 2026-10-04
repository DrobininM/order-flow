using PaymentService.Domain.Entities;

namespace PaymentService.Domain.Repositories;

/// <summary>
/// Persistence contract for inbound integration messages (idempotent consumption).
/// </summary>
public interface IInboxMessageRepository
{
    /// <summary>
    /// Returns whether a message with the given identifier was already received.
    /// </summary>
    Task<bool> ExistsAsync(string messageId, CancellationToken ct = default);

    /// <summary>
    /// Adds a newly received message.
    /// </summary>
    Task AddAsync(InboxMessage message, CancellationToken ct = default);
}
