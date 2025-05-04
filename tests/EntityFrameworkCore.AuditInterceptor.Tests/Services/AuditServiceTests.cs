using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Services
{
    public class AuditServiceTests
    {
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly AuditService _auditService;
        private readonly TestDbContext _dbContext;

        public AuditServiceTests()
        {
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _auditService = new AuditService(_currentUserServiceMock.Object);
            
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _dbContext = new TestDbContext(options);
        }

        [Fact]
        public void SavingChanges_WithNewEntity_SetsAuditProperties()
        {
            // Arrange
            var testUserId = "test-user-id";
            _currentUserServiceMock.Setup(x => x.GetUserId()).Returns(testUserId);
            
            var entity = new TestEntity { Name = "Test Entity" };
            _dbContext.TestEntities.Add(entity);
            
            // Create event data with context
            var eventData = new DbContextEventData(
                new EventDefinitionBase(new Dictionary<string, string>(), (p, l) => ""),
                new DbContextEventData(
                    new EventDefinitionBase(new Dictionary<string, string>(), (p, l) => ""),
                    _dbContext,
                    null,
                    null,
                    null,
                    null,
                    null).Context);
            
            var result = InterceptionResult<int>.SuppressWithResult(0);
            
            // Act
            var beforeDate = DateTime.UtcNow;
            _auditService.SavingChanges(eventData, result);
            var afterDate = DateTime.UtcNow;
            
            // Assert
            Assert.Equal(testUserId, entity.CreatedBy);
            Assert.Equal(testUserId, entity.UpdatedBy);
            Assert.True(entity.CreatedAt >= beforeDate && entity.CreatedAt <= afterDate);
            Assert.True(entity.UpdatedAt >= beforeDate && entity.UpdatedAt <= afterDate);
        }

        [Fact]
        public async Task SavingChangesAsync_WithNewEntity_SetsAuditProperties()
        {
            // Arrange
            var testUserId = "test-user-id";
            _currentUserServiceMock.Setup(x => x.GetUserId()).Returns(testUserId);
            
            var entity = new TestEntity { Name = "Test Entity" };
            _dbContext.TestEntities.Add(entity);
            
            // Create event data with context
            var eventData = new DbContextEventData(
                new EventDefinitionBase(new Dictionary<string, string>(), (p, l) => ""),
                new DbContextEventData(
                    new EventDefinitionBase(new Dictionary<string, string>(), (p, l) => ""),
                    _dbContext,
                    null,
                    null,
                    null,
                    null,
                    null).Context);
            
            var result = InterceptionResult<int>.SuppressWithResult(0);
            
            // Act
            var beforeDate = DateTime.UtcNow;
            await _auditService.SavingChangesAsync(eventData, result, CancellationToken.None);
            var afterDate = DateTime.UtcNow;
            
            // Assert
            Assert.Equal(testUserId, entity.CreatedBy);
            Assert.Equal(testUserId, entity.UpdatedBy);
            Assert.True(entity.CreatedAt >= beforeDate && entity.CreatedAt <= afterDate);
            Assert.True(entity.UpdatedAt >= beforeDate && entity.UpdatedAt <= afterDate);
        }

        [Fact]
        public void SavingChanges_WithModifiedEntity_SetsOnlyUpdateProperties()
        {
            // Arrange
            var originalUserId = "original-user";
            var newUserId = "new-user";
            
            var originalDate = DateTime.UtcNow.AddDays(-1);
            
            // Create an entity that's already "saved"
            var entity = new TestEntity 
            { 
                Id = "1", 
                Name = "Test Entity", 
                CreatedBy = originalUserId,
                CreatedAt = originalDate,
                UpdatedBy = originalUserId,
                UpdatedAt = originalDate
            };
            
            _dbContext.TestEntities.Add(entity);
            _dbContext.Entry(entity).State = EntityState.Unchanged;
            
            // Now modify it
            _currentUserServiceMock.Setup(x => x.GetUserId()).Returns(newUserId);
            entity.Name = "Modified Test Entity";
            _dbContext.Entry(entity).State = EntityState.Modified;
            
            // Create event data with context
            var eventData = new DbContextEventData(
                new EventDefinitionBase(new Dictionary<string, string>(), (p, l) => ""),
                new DbContextEventData(
                    new EventDefinitionBase(new Dictionary<string, string>(), (p, l) => ""),
                    _dbContext,
                    null,
                    null,
                    null,
                    null,
                    null).Context);
            
            var result = InterceptionResult<int>.SuppressWithResult(0);
            
            // Act
            var beforeDate = DateTime.UtcNow;
            _auditService.SavingChanges(eventData, result);
            var afterDate = DateTime.UtcNow;
            
            // Assert
            Assert.Equal(originalUserId, entity.CreatedBy); // Should not change
            Assert.Equal(originalDate, entity.CreatedAt); // Should not change
            Assert.Equal(newUserId, entity.UpdatedBy); // Should be updated
            Assert.True(entity.UpdatedAt >= beforeDate && entity.UpdatedAt <= afterDate); // Should be updated
        }

        [Fact]
        public void SavingChanges_WithNullContext_DoesNotThrowException()
        {
            // Arrange
            var eventData = new DbContextEventData(
                new EventDefinitionBase(new Dictionary<string, string>(), (p, l) => ""),
                null);
            
            var result = InterceptionResult<int>.SuppressWithResult(0);
            
            // Act & Assert - should not throw
            _auditService.SavingChanges(eventData, result);
        }

        [Fact]
        public void SavingChanges_WithNullUserId_UsesSystemAsDefault()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.GetUserId()).Returns((string)null);
            
            var entity = new TestEntity { Name = "Test Entity" };
            _dbContext.TestEntities.Add(entity);
            
            // Create event data with context
            var eventData = new DbContextEventData(
                new EventDefinitionBase(new Dictionary<string, string>(), (p, l) => ""),
                new DbContextEventData(
                    new EventDefinitionBase(new Dictionary<string, string>(), (p, l) => ""),
                    _dbContext,
                    null,
                    null,
                    null,
                    null,
                    null).Context);
            
            var result = InterceptionResult<int>.SuppressWithResult(0);
            
            // Act
            _auditService.SavingChanges(eventData, result);
            
            // Assert
            Assert.Equal("System", entity.CreatedBy);
            Assert.Equal("System", entity.UpdatedBy);
        }

        // Test DbContext
        private class TestDbContext : DbContext
        {
            public DbSet<TestEntity> TestEntities { get; set; }
            
            public TestDbContext(DbContextOptions options) : base(options)
            {
            }
        }

        // Test Entity
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
}
