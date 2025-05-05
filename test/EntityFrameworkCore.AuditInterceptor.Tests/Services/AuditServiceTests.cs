using System;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Services;

public class AuditServiceTests
{
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly AuditService _auditService;
    private readonly TestDbContext _dbContext;
    private readonly string _testUserId = "test-user-123";

    public AuditServiceTests()
    {
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(s => s.GetUserId()).Returns(_testUserId);
        
        _auditService = new AuditService(_mockCurrentUserService.Object);
        
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new TestDbContext(options);
    }
    
    [Fact]
    public void SavingChanges_WhenEntityAdded_SetsAuditFields()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid().ToString(), Name = "Test Entity" };
        _dbContext.TestEntities.Add(entity);
        
        // Create a mock DbContextEventData
        var mockEventData = new Mock<DbContextEventData>(MockBehavior.Loose, null, _dbContext);
        
        // Act
        _auditService.SavingChanges(mockEventData.Object, InterceptionResult<int>.SuppressWithResult(1));
        
        // Assert
        Assert.Equal(_testUserId, entity.CreatedBy);
        Assert.Equal(_testUserId, entity.UpdatedBy);
        Assert.True(DateTime.UtcNow.Subtract(entity.CreatedAt).TotalSeconds < 5);
        Assert.True(DateTime.UtcNow.Subtract(entity.UpdatedAt).TotalSeconds < 5);
    }
    
    [Fact]
    public async Task SavingChangesAsync_WhenEntityAdded_SetsAuditFields()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid().ToString(), Name = "Test Entity Async" };
        _dbContext.TestEntities.Add(entity);
        
        // Create a mock DbContextEventData
        var mockEventData = new Mock<DbContextEventData>(MockBehavior.Loose, null, _dbContext);
        
        // Act
        await _auditService.SavingChangesAsync(
            mockEventData.Object, 
            InterceptionResult<int>.SuppressWithResult(1),
            CancellationToken.None);
        
        // Assert
        Assert.Equal(_testUserId, entity.CreatedBy);
        Assert.Equal(_testUserId, entity.UpdatedBy);
        Assert.True(DateTime.UtcNow.Subtract(entity.CreatedAt).TotalSeconds < 5);
        Assert.True(DateTime.UtcNow.Subtract(entity.UpdatedAt).TotalSeconds < 5);
    }
    
    [Fact]
    public void SavingChanges_WhenEntityModified_UpdatesUpdateFields()
    {
        // Arrange
        var entity = new TestEntity 
        { 
            Id = Guid.NewGuid().ToString(), 
            Name = "Original Name",
            CreatedBy = "original-creator",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedBy = "original-updater",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        
        _dbContext.TestEntities.Add(entity);
        _dbContext.SaveChanges();
        
        // Detach and modify
        _dbContext.Entry(entity).State = EntityState.Detached;
        
        entity.Name = "Updated Name";
        _dbContext.TestEntities.Update(entity);
        
        // Create a mock DbContextEventData
        var mockEventData = new Mock<DbContextEventData>(MockBehavior.Loose, null, _dbContext);
        
        // Act
        _auditService.SavingChanges(mockEventData.Object, InterceptionResult<int>.SuppressWithResult(1));
        
        // Assert
        Assert.Equal("original-creator", entity.CreatedBy);
        Assert.Equal(_testUserId, entity.UpdatedBy);
        Assert.True(DateTime.UtcNow.Subtract(entity.UpdatedAt).TotalSeconds < 5);
    }
    
    [Fact]
    public void SavingChanges_WhenCurrentUserIsNull_UsesSystemAsDefault()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.GetUserId()).Returns((string?)null);
        
        var entity = new TestEntity { Id = Guid.NewGuid().ToString(), Name = "No User Entity" };
        _dbContext.TestEntities.Add(entity);
        
        // Create a mock DbContextEventData
        var mockEventData = new Mock<DbContextEventData>(MockBehavior.Loose, null, _dbContext);
        
        // Act
        _auditService.SavingChanges(mockEventData.Object, InterceptionResult<int>.SuppressWithResult(1));
        
        // Assert
        Assert.Equal("System", entity.CreatedBy);
        Assert.Equal("System", entity.UpdatedBy);
    }
    
    [Fact]
    public void SavingChanges_WhenContextIsNull_DoesNotThrowException()
    {
        // Arrange
        // Create a mock DbContextEventData with null context
        var mockEventData = new Mock<DbContextEventData>(MockBehavior.Loose, null, null);
        
        // Act & Assert
        var exception = Record.Exception(() => 
            _auditService.SavingChanges(mockEventData.Object, InterceptionResult<int>.SuppressWithResult(1)));
        
        Assert.Null(exception);
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
