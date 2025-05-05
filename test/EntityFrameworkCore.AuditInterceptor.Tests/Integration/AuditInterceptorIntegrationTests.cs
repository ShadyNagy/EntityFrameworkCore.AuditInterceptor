using System;
using System.Threading.Tasks;
using EntityFrameworkCore.AuditInterceptor.Extensions;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Integration;

public class AuditInterceptorIntegrationTests
{
    [Fact]
    public async Task AuditInterceptor_TracksAuditFields_WhenEntityIsSaved()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var testUserId = "integration-test-user";
        mockCurrentUserService.Setup(s => s.GetUserId()).Returns(testUserId);
        
        services.AddSingleton(mockCurrentUserService.Object);
        services.AddAuditing();
        services.AddDbContext<TestDbContext>(options => 
            options.UseInMemoryDatabase("IntegrationTestDb")
                   .AddAuditInterceptors());
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            
            var entity = new TestEntity { Id = Guid.NewGuid().ToString(), Name = "Integration Test Entity" };
            dbContext.TestEntities.Add(entity);
            await dbContext.SaveChangesAsync();
            
            // Assert - Check fields after initial save
            Assert.Equal(testUserId, entity.CreatedBy);
            Assert.Equal(testUserId, entity.UpdatedBy);
            Assert.True(DateTime.UtcNow.Subtract(entity.CreatedAt).TotalSeconds < 5);
            Assert.True(DateTime.UtcNow.Subtract(entity.UpdatedAt).TotalSeconds < 5);
            
            // Store original timestamps for comparison
            var originalCreatedAt = entity.CreatedAt;
            var originalUpdatedAt = entity.UpdatedAt;
            
            // Update the entity
            entity.Name = "Updated Integration Test Entity";
            await dbContext.SaveChangesAsync();
            
            // Assert - Check fields after update
            Assert.Equal(testUserId, entity.CreatedBy);
            Assert.Equal(testUserId, entity.UpdatedBy);
            Assert.Equal(originalCreatedAt, entity.CreatedAt);
            Assert.True(entity.UpdatedAt > originalUpdatedAt);
        }
    }

    private class TestDbContext : DbContext
    {
        public DbSet<TestEntity> TestEntities { get; set; } = null!;
        
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }
    }
    
    private class TestEntity : IAuditable
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}
