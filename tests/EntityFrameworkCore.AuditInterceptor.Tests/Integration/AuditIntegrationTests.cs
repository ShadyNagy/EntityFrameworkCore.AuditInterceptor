using System;
using System.Threading.Tasks;
using EntityFrameworkCore.AuditInterceptor.Extensions;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Integration;

public class AuditIntegrationTests
{
  private class TestEntity : IAuditable
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
  }

  private class TestDbContext : DbContext
  {
    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }
  }

  private static string _databaseName = $"AuditIntegrationTests-{Guid.NewGuid()}";

  private IServiceProvider BuildServiceProvider(string userId)
  {
    var services = new ServiceCollection();

    // Mock current user service
    var currentUserServiceMock = new Mock<ICurrentUserService>();
    currentUserServiceMock.Setup(x => x.GetUserId()).Returns(userId);
    services.AddScoped<ICurrentUserService>(_ => currentUserServiceMock.Object);

    // Add audit service
    services.AddScoped<AuditService>();

    // Add DbContext with shared database name
    var serviceProvider = services.BuildServiceProvider();

    services.AddDbContext<TestDbContext>(options =>
    {
      options.UseInMemoryDatabase(_databaseName)  // Use shared database name
        .UseApplicationServiceProvider(serviceProvider)
        .AddAuditInterceptors();
    });

    return services.BuildServiceProvider();
  }

  [Fact]
  public async Task FullIntegration_AuditFieldsAreSetCorrectly()
  {
    // Arrange
    var userId = "integration-test-user";
    var serviceProvider = BuildServiceProvider(userId);

    // Act - Add entity
    TestEntity entity;
    using (var scope = serviceProvider.CreateScope())
    {
      var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

      entity = new TestEntity { Name = "Integration Test Entity" };
      context.TestEntities.Add(entity);
      await context.SaveChangesAsync();
    }

    // Assert - Audit fields are set for creation
    Assert.Equal(userId, entity.CreatedBy);
    Assert.Equal(userId, entity.UpdatedBy);
    Assert.True(DateTime.UtcNow.Subtract(entity.CreatedAt).TotalSeconds < 5);
    Assert.True(DateTime.UtcNow.Subtract(entity.UpdatedAt).TotalSeconds < 5);

    var originalCreatedAt = entity.CreatedAt;
    var originalUpdatedAt = entity.UpdatedAt;

    // Wait to ensure timestamps will be different
    await Task.Delay(10);

    // Act - Update entity with different user
    var newUserId = "different-user";
    var newServiceProvider = BuildServiceProvider(newUserId);

    using (var scope = newServiceProvider.CreateScope())
    {
      var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

      var foundEntity = await context.TestEntities.FindAsync(entity.Id);
      if (foundEntity != null)
      {
        foundEntity.Name = "Updated Integration Test Entity";
        await context.SaveChangesAsync();

        // Update our reference to match the updated entity
        entity = foundEntity;
      }
    }

    // Assert - Only update fields are modified
    Assert.Equal(userId, entity.CreatedBy);
    Assert.Equal(newUserId, entity.UpdatedBy);
    Assert.Equal(originalCreatedAt, entity.CreatedAt);
    Assert.True(entity.UpdatedAt > originalUpdatedAt);
  }
}
