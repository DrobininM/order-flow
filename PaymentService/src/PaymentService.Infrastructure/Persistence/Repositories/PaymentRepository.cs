using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Repositories;

namespace PaymentService.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _dbContext;

    public PaymentRepository(PaymentDbContext dbContext) => _dbContext = dbContext;

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _dbContext.Payments.FindAsync([id], ct);

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default) =>
        await _dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

    public async Task AddAsync(Payment payment, CancellationToken ct = default) =>
        await _dbContext.Payments.AddAsync(payment, ct);

    public void Update(Payment payment) => _dbContext.Payments.Update(payment);
}
