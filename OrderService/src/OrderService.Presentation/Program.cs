using Microsoft.EntityFrameworkCore;
using OrderService.Application;
using OrderService.Application.Common;
using OrderService.Infrastructure;
using OrderService.Infrastructure.BackgroundServices;
using OrderService.Infrastructure.Persistence;
using OrderService.Presentation;
using OrderService.Presentation.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
        .Enrich.WithProperty("Service", "OrderService")
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddApplication();

builder.Services.Configure<DomainEventRecoveryOptions>(
    builder.Configuration.GetSection(DomainEventRecoveryOptions.SectionName));

builder.Services.Configure<OrderOptions>(
    builder.Configuration.GetSection(OrderOptions.SectionName));

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddSwaggerWithJwt();

builder.Services.AddRateLimiting();

builder.Services.AddControllers();

var app = builder.Build();

app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        dbContext.Database.Migrate();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();