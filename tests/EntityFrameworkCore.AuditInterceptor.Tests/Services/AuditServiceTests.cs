using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Services
{
    public class AuditServiceTests
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly AuditService _auditService;

        public AuditServiceTests()
        {
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _auditService = new AuditService(_mockCurrentUserService.Object);
        }

        [Fact]
        public void SavingChanges_ShouldAuditEntities_WhenContextContainsAuditableEntities()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            using var context = new TestDbContext(options);
            var entity = new TestEntity { Id = "1", Name = "Test Entity" };
            context.TestEntities.Add(entity);
            
            var userId = "test-user-id";
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns(userId);

            var eventData = new DbContextEventData(
                new EventDefinitionBase(new Dictionary<string, string>(), typeof(DbContext)),
                context);
            
            var result = InterceptionResult<int>.SuppressWithResult(1);

            // Act
            var interceptResult = _auditService.SavingChanges(eventData, result);

            // Assert
            Assert.Equal(userId, entity.CreatedBy);
            Assert.Equal(userId, entity.UpdatedBy);
            Assert.True(DateTime.UtcNow.Subtract(entity.CreatedAt).TotalSeconds < 5);
            Assert.True(DateTime.UtcNow.Subtract(entity.UpdatedAt).TotalSeconds < 5);
        }

        [Fact]
        public async Task SavingChangesAsync_ShouldAuditEntities_WhenContextContainsAuditableEntities()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            using var context = new TestDbContext(options);
            var entity = new TestEntity { Id = "1", Name = "Test Entity" };
            context.TestEntities.Add(entity);
            
            var userId = "test-user-id";
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns(userId);

            var eventData = new DbContextEventData(
                new EventDefinitionBase(new Dictionary<string, string>(), typeof(DbContext)),
                context);
            
            var result = InterceptionResult<int>.SuppressWithResult(1);

            // Act
            var interceptResult = await _auditService.SavingChangesAsync(eventData, result, CancellationToken.None);

            // Assert
            Assert.Equal(userId, entity.CreatedBy);
            Assert.Equal(userId, entity.UpdatedBy);
            Assert.True(DateTime.UtcNow.Subtract(entity.CreatedAt).TotalSeconds < 5);
            Assert.True(DateTime.UtcNow.Subtract(entity.UpdatedAt).TotalSeconds < 5);
        }

        [Fact]
        public void SavingChanges_ShouldSetSystemAsUser_WhenCurrentUserIsNull()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            using var context = new TestDbContext(options);
            var entity = new TestEntity { Id = "1", Name = "Test Entity" };
            context.TestEntities.Add(entity);
            
            _mockCurrentUserService.Setup(s => s.GetUserId()).Returns((string)null);

            var eventData = new DbContextEventData(
                new EventDefinitionBase(new Dictionary<string, string>(), typeof(DbContext)),
                context);
            
            var result = InterceptionResult<int>.SuppressWithResult(1);

            // Act
            var interceptResult = _auditService.SavingChanges(eventData, result);

            // Assert
            Assert.Equal("System", entity.CreatedBy);
            Assert.Equal("System", entity.UpdatedBy);
        }

        [Fact]
        public void SavingChanges_ShouldUpdateAuditFields_WhenEntityIsModified()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            string initialUser = "initial-user";
            DateTime initialTime = DateTime.UtcNow.AddDays(-1);
            string updatedUser = "updated-user";

            // First, create and save the entity
            using (var context = new TestDbContext(options))
            {
                var entity = new TestEntity 
                { 
                    Id = "1", 
                    Name = "Test Entity",
                    CreatedBy = initialUser,
                    CreatedAt = initialTime,
                    UpdatedBy = initialUser,
                    UpdatedAt = initialTime
                };
                context.TestEntities.Add(entity);
                context.SaveChanges();
            }

            // Now modify the entity
            using (var context = new TestDbContext(options))
            {
                _mockCurrentUserService.Setup(s => s.GetUserId()).Returns(updatedUser);

                var entity = context.TestEntities.Find("1");
                entity.Name = "Updated Name";
                context.Entry(entity).State = EntityState.Modified;

                var eventData = new DbContextEventData(
                    new EventDefinitionBase(new Dictionary<string, string>(), typeof(DbContext)),
                    context);
                
                var result = InterceptionResult<int>.SuppressWithResult(1);

                // Act
                var interceptResult = _auditService.SavingChanges(eventData, result);

                // Assert
                Assert.Equal(initialUser, entity.CreatedBy);
                Assert.Equal(initialTime, entity.CreatedAt);
                Assert.Equal(updatedUser, entity.UpdatedBy);
                Assert.True(DateTime.UtcNow.Subtract(entity.UpdatedAt).TotalSeconds < 5);
            }
        }

        [Fact]
        public void SavingChanges_ShouldDoNothing_WhenContextIsNull()
        {
            // Arrange
            var eventData = new DbContextEventData(
                new EventDefinitionBase(new Dictionary<string, string>(), typeof(DbContext)),
                null);
            
            var result = InterceptionResult<int>.SuppressWithResult(1);

            // Act & Assert (no exception should be thrown)
            var interceptResult = _auditService.SavingChanges(eventData, result);
            
            // Additional verification
            _mockCurrentUserService.Verify(s => s.GetUserId(), Times.Never);
        }

        // Test DbContext for unit tests
        private class TestDbContext : DbContext
        {
            public DbSet<TestEntity> TestEntities { get; set; }

            public TestDbContext(DbContextOptions options) : base(options)
            {
            }
        }

        // Test entity for unit tests
        private class TestEntity : IAuditable
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public string UpdatedBy { get; set; }
            public DateTime UpdatedAt { get; set; }
        }
    }
}
