using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly OrderDbContext _dbContext;

    public UserRepository(OrderDbContext dbContext) => _dbContext = dbContext;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _dbContext.Users.FindAsync([id], ct);

    public async Task<User?> GetByEmailAsync(Domain.ValueObjects.Email email, CancellationToken ct = default) =>
        await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<bool> IsExistByEmailAsync(Domain.ValueObjects.Email email, CancellationToken ct = default) =>
        await _dbContext.Users.AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _dbContext.Users.AddAsync(user, ct);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        return await _dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken), ct);
    }
}
