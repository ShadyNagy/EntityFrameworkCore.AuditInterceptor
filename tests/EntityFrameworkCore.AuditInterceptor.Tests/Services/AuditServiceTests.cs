using System;
using System.Linq;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Services;

public class AuditServiceTests
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

    private TestDbContext CreateContext(AuditService auditService)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(auditService)
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public void SavingChanges_WhenAddingEntity_SetsAuditFields()
    {
        // Arrange
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns("TestUser");
        
        var auditService = new AuditService(currentUserServiceMock.Object);
        var context = CreateContext(auditService);
        
        var entity = new TestEntity
        {
            Name = "Test Entity"
        };

        // Act
        context.TestEntities.Add(entity);
        context.SaveChanges();

        // Assert
        Assert.Equal("TestUser", entity.CreatedBy);
        Assert.Equal("TestUser", entity.UpdatedBy);
        Assert.True(DateTime.UtcNow.Subtract(entity.CreatedAt).TotalSeconds < 5);
        Assert.True(DateTime.UtcNow.Subtract(entity.UpdatedAt).TotalSeconds < 5);
    }

    [Fact]
    public void SavingChanges_WhenModifyingEntity_UpdatesAuditFields()
    {
        // Arrange
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns("TestUser");
        
        var auditService = new AuditService(currentUserServiceMock.Object);
        var context = CreateContext(auditService);
        
        // Add entity first
        var entity = new TestEntity
        {
            Name = "Test Entity"
        };
        context.TestEntities.Add(entity);
        context.SaveChanges();
        
        var originalCreatedBy = entity.CreatedBy;
        var originalCreatedAt = entity.CreatedAt;
        var originalUpdatedAt = entity.UpdatedAt;

        // Wait a bit to ensure the timestamps are different
        System.Threading.Thread.Sleep(10);
        
        // Change user and modify entity
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns("DifferentUser");
        
        // Act
        entity.Name = "Modified Test Entity";
        context.SaveChanges();

        // Assert
        Assert.Equal(originalCreatedBy, entity.CreatedBy);
        Assert.Equal(originalCreatedAt, entity.CreatedAt);
        Assert.Equal("DifferentUser", entity.UpdatedBy);
        Assert.True(entity.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void SavingChanges_WhenUserIdIsNull_UsesSystemAsDefault()
    {
        // Arrange
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns((string?)null);
        
        var auditService = new AuditService(currentUserServiceMock.Object);
        var context = CreateContext(auditService);
        
        var entity = new TestEntity
        {
            Name = "Test Entity"
        };

        // Act
        context.TestEntities.Add(entity);
        context.SaveChanges();

        // Assert
        Assert.Equal("System", entity.CreatedBy);
        Assert.Equal("System", entity.UpdatedBy);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenAddingEntity_SetsAuditFields()
    {
        // Arrange
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.GetUserId()).Returns("AsyncTestUser");
        
        var auditService = new AuditService(currentUserServiceMock.Object);
        var context = CreateContext(auditService);
        
        var entity = new TestEntity
        {
            Name = "Async Test Entity"
        };

        // Act
        await context.TestEntities.AddAsync(entity);
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal("AsyncTestUser", entity.CreatedBy);
        Assert.Equal("AsyncTestUser", entity.UpdatedBy);
        Assert.True(DateTime.UtcNow.Subtract(entity.CreatedAt).TotalSeconds < 5);
        Assert.True(DateTime.UtcNow.Subtract(entity.UpdatedAt).TotalSeconds < 5);
    }
}
