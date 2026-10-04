using PaymentService.Domain.Entities;

namespace PaymentService.Domain.Repositories;

/// <summary>
/// Persistence contract for <see cref="Payment"/> aggregates.
/// </summary>
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns the payment registered for an order. The unique index on <c>OrderId</c>
    /// guarantees at most one payment per order.
    /// </summary>
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);

    Task AddAsync(Payment payment, CancellationToken ct = default);

    void Update(Payment payment);
}
