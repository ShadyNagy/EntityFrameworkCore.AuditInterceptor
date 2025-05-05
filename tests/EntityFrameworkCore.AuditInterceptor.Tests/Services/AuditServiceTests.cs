using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Services
{
    public class AuditServiceTests
    {
        private readonly TestDbContext _dbContext;
        private readonly AuditService _auditService;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private const string TestUserId = "test-user-123";

        public AuditServiceTests()
        {
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _currentUserServiceMock.Setup(x => x.GetUserId()).Returns(TestUserId);

            _auditService = new AuditService(_currentUserServiceMock.Object);

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .AddInterceptors(_auditService)
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _dbContext = new TestDbContext(options);
        }

        [Fact]
        public async Task SavingChanges_WhenAddingEntity_ShouldSetAuditFields()
        {
            // Arrange
            var entity = new TestEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Entity"
            };

            // Act
            _dbContext.TestEntities.Add(entity);
            await _dbContext.SaveChangesAsync();

            // Assert
            var savedEntity = await _dbContext.TestEntities.FindAsync(entity.Id);
            Assert.NotNull(savedEntity);
            Assert.Equal(TestUserId, savedEntity!.CreatedBy);
            Assert.Equal(TestUserId, savedEntity.UpdatedBy);
            Assert.True(DateTime.UtcNow.Subtract(savedEntity.CreatedAt).TotalSeconds < 5);
            Assert.True(DateTime.UtcNow.Subtract(savedEntity.UpdatedAt).TotalSeconds < 5);
        }

        [Fact]
        public async Task SavingChanges_WhenModifyingEntity_ShouldUpdateAuditFields()
        {
            // Arrange
            var entity = new TestEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Entity"
            };

            _dbContext.TestEntities.Add(entity);
            await _dbContext.SaveChangesAsync();

            var originalCreatedBy = entity.CreatedBy;
            var originalCreatedAt = entity.CreatedAt;
            var originalUpdatedAt = entity.UpdatedAt;

            // Wait briefly to ensure timestamps are different
            await Task.Delay(10);

            // Change the user
            _currentUserServiceMock.Setup(x => x.GetUserId()).Returns("modified-user-456");

            // Act
            entity.Name = "Modified Entity";
            _dbContext.TestEntities.Update(entity);
            await _dbContext.SaveChangesAsync();

            // Assert
            var modifiedEntity = await _dbContext.TestEntities.FindAsync(entity.Id);
            Assert.NotNull(modifiedEntity);
            Assert.Equal("Modified Entity", modifiedEntity!.Name);
            Assert.Equal(originalCreatedBy, modifiedEntity.CreatedBy);
            Assert.Equal(originalCreatedAt, modifiedEntity.CreatedAt);
            Assert.Equal("modified-user-456", modifiedEntity.UpdatedBy);
            Assert.True(modifiedEntity.UpdatedAt > originalUpdatedAt);
        }

        [Fact]
        public async Task SavingChanges_WhenUserIdIsNull_ShouldUseSystemAsDefault()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.GetUserId()).Returns((string?)null);

            var entity = new TestEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Entity with Null User"
            };

            // Act
            _dbContext.TestEntities.Add(entity);
            await _dbContext.SaveChangesAsync();

            // Assert
            var savedEntity = await _dbContext.TestEntities.FindAsync(entity.Id);
            Assert.NotNull(savedEntity);
            Assert.Equal("System", savedEntity!.CreatedBy);
            Assert.Equal("System", savedEntity.UpdatedBy);
        }

        [Fact]
        public async Task SavingChanges_WithMultipleEntities_ShouldAuditAllEntities()
        {
            // Arrange
            var entities = Enumerable.Range(1, 5).Select(i => new TestEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Test Entity {i}"
            }).ToList();

            // Act
            _dbContext.TestEntities.AddRange(entities);
            await _dbContext.SaveChangesAsync();

            // Assert
            var savedEntities = await _dbContext.TestEntities.ToListAsync();
            Assert.Equal(entities.Count, savedEntities.Count);
            
            foreach (var entity in savedEntities)
            {
                Assert.Equal(TestUserId, entity.CreatedBy);
                Assert.Equal(TestUserId, entity.UpdatedBy);
                Assert.True(DateTime.UtcNow.Subtract(entity.CreatedAt).TotalSeconds < 5);
                Assert.True(DateTime.UtcNow.Subtract(entity.UpdatedAt).TotalSeconds < 5);
            }
        }
    }

    // Test DbContext and Entity for unit tests
    public class TestDbContext : DbContext
    {
        public DbSet<TestEntity> TestEntities { get; set; } = null!;

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }
    }

    public class TestEntity : IAuditable
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}
