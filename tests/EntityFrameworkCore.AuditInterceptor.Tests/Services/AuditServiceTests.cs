using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Services
{
    public class AuditServiceTests
    {
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<AuditService>> _loggerMock;
        private readonly AuditService _auditService;

        public AuditServiceTests()
        {
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<AuditService>>();
            _auditService = new AuditService(_currentUserServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task SavingChangesAsync_WithAuditableEntities_SetsAuditProperties()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            var testEntity = new TestAuditableEntity { Id = 1, Name = "Test Entity" };
            var currentUserId = "test-user-123";
            var currentDateTime = DateTime.UtcNow;

            _currentUserServiceMock.Setup(x => x.UserId).Returns(currentUserId);

            using var context = new TestDbContext(options);
            context.TestEntities.Add(testEntity);
            
            var eventData = new Mock<SaveChangesEventData>(
                Mock.Of<DbContextEventId>(),
                context,
                new CancellationToken());

            // Act
            await _auditService.SavingChangesAsync(eventData.Object, default);

            // Assert
            Assert.Equal(currentUserId, testEntity.CreatedBy);
            Assert.NotNull(testEntity.CreatedOn);
            Assert.Equal(currentUserId, testEntity.LastModifiedBy);
            Assert.NotNull(testEntity.LastModifiedOn);
        }

        [Fact]
        public async Task SavingChangesAsync_WithModifiedAuditableEntities_UpdatesModifiedProperties()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            var createdBy = "original-user-456";
            var createdOn = DateTime.UtcNow.AddDays(-10);
            var currentUserId = "test-user-123";

            var testEntity = new TestAuditableEntity 
            { 
                Id = 1, 
                Name = "Test Entity",
                CreatedBy = createdBy,
                CreatedOn = createdOn
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(currentUserId);

            using var context = new TestDbContext(options);
            
            // Add and save to simulate existing entity
            context.TestEntities.Add(testEntity);
            await context.SaveChangesAsync();
            
            // Modify entity
            context.Entry(testEntity).State = EntityState.Modified;
            testEntity.Name = "Updated Name";
            
            var eventData = new Mock<SaveChangesEventData>(
                Mock.Of<DbContextEventId>(),
                context,
                new CancellationToken());

            // Act
            await _auditService.SavingChangesAsync(eventData.Object, default);

            // Assert
            Assert.Equal(createdBy, testEntity.CreatedBy);
            Assert.Equal(createdOn, testEntity.CreatedOn);
            Assert.Equal(currentUserId, testEntity.LastModifiedBy);
            Assert.NotNull(testEntity.LastModifiedOn);
        }

        [Fact]
        public async Task SavingChangesAsync_WithNonAuditableEntities_DoesNotSetAuditProperties()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            var testEntity = new TestNonAuditableEntity { Id = 1, Description = "Non-auditable entity" };
            var currentUserId = "test-user-123";

            _currentUserServiceMock.Setup(x => x.UserId).Returns(currentUserId);

            using var context = new TestDbContext(options);
            context.NonAuditableEntities.Add(testEntity);
            
            var eventData = new Mock<SaveChangesEventData>(
                Mock.Of<DbContextEventId>(),
                context,
                new CancellationToken());

            // Act
            await _auditService.SavingChangesAsync(eventData.Object, default);

            // Assert - No exception should be thrown
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task SavingChangesAsync_WithNullUser_SetsSystemUserForAuditProperties()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            var testEntity = new TestAuditableEntity { Id = 1, Name = "Test Entity" };
            
            // Setup null user ID
            _currentUserServiceMock.Setup(x => x.UserId).Returns((string)null);

            using var context = new TestDbContext(options);
            context.TestEntities.Add(testEntity);
            
            var eventData = new Mock<SaveChangesEventData>(
                Mock.Of<DbContextEventId>(),
                context,
                new CancellationToken());

            // Act
            await _auditService.SavingChangesAsync(eventData.Object, default);

            // Assert
            Assert.Equal("System", testEntity.CreatedBy);
            Assert.NotNull(testEntity.CreatedOn);
            Assert.Equal("System", testEntity.LastModifiedBy);
            Assert.NotNull(testEntity.LastModifiedOn);
        }

        // Helper classes for testing
        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

            public DbSet<TestAuditableEntity> TestEntities { get; set; }
            public DbSet<TestNonAuditableEntity> NonAuditableEntities { get; set; }
        }

        public class TestAuditableEntity : IAuditable
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string CreatedBy { get; set; }
            public DateTime? CreatedOn { get; set; }
            public string LastModifiedBy { get; set; }
            public DateTime? LastModifiedOn { get; set; }
        }

        public class TestNonAuditableEntity
        {
            public int Id { get; set; }
            public string Description { get; set; }
        }
    }
}
