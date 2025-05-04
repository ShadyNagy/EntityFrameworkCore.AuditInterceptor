using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Update;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Services
{
    public class AuditServiceTests
    {
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly AuditService _auditService;

        public AuditServiceTests()
        {
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user-id");
            _auditService = new AuditService(_currentUserServiceMock.Object);
        }

        [Fact]
        public void ApplyAuditInformation_WhenEntityIsAuditable_SetsCreatedProperties()
        {
            // Arrange
            var mockEntity = new TestAuditableEntity();
            var mockEntry = CreateMockEntityEntry(mockEntity, EntityState.Added);
            var entries = new List<EntityEntry> { mockEntry.Object };

            // Act
            _auditService.ApplyAuditInformation(entries);

            // Assert
            Assert.Equal("test-user-id", mockEntity.CreatedBy);
            Assert.NotEqual(default, mockEntity.CreatedOn);
            Assert.Null(mockEntity.LastModifiedBy);
            Assert.Equal(default, mockEntity.LastModifiedOn);
        }

        [Fact]
        public void ApplyAuditInformation_WhenEntityIsAuditableAndModified_SetsModifiedProperties()
        {
            // Arrange
            var mockEntity = new TestAuditableEntity
            {
                CreatedBy = "original-creator",
                CreatedOn = DateTime.UtcNow.AddDays(-1)
            };
            var mockEntry = CreateMockEntityEntry(mockEntity, EntityState.Modified);
            var entries = new List<EntityEntry> { mockEntry.Object };

            // Act
            _auditService.ApplyAuditInformation(entries);

            // Assert
            Assert.Equal("original-creator", mockEntity.CreatedBy);
            Assert.NotEqual(default, mockEntity.CreatedOn);
            Assert.Equal("test-user-id", mockEntity.LastModifiedBy);
            Assert.NotEqual(default, mockEntity.LastModifiedOn);
        }

        [Fact]
        public void ApplyAuditInformation_WhenEntityIsNotAuditable_DoesNotSetProperties()
        {
            // Arrange
            var mockEntity = new TestNonAuditableEntity();
            var mockEntry = CreateMockEntityEntry(mockEntity, EntityState.Added);
            var entries = new List<EntityEntry> { mockEntry.Object };

            // Act
            _auditService.ApplyAuditInformation(entries);

            // Assert
            // No exception should be thrown, and no properties modified
        }

        [Fact]
        public void ApplyAuditInformation_WithMultipleEntities_HandlesEachCorrectly()
        {
            // Arrange
            var mockAddedEntity = new TestAuditableEntity();
            var mockModifiedEntity = new TestAuditableEntity
            {
                CreatedBy = "original-creator",
                CreatedOn = DateTime.UtcNow.AddDays(-1)
            };
            var mockNonAuditableEntity = new TestNonAuditableEntity();

            var mockAddedEntry = CreateMockEntityEntry(mockAddedEntity, EntityState.Added);
            var mockModifiedEntry = CreateMockEntityEntry(mockModifiedEntity, EntityState.Modified);
            var mockNonAuditableEntry = CreateMockEntityEntry(mockNonAuditableEntity, EntityState.Added);

            var entries = new List<EntityEntry> 
            { 
                mockAddedEntry.Object, 
                mockModifiedEntry.Object,
                mockNonAuditableEntry.Object
            };

            // Act
            _auditService.ApplyAuditInformation(entries);

            // Assert
            Assert.Equal("test-user-id", mockAddedEntity.CreatedBy);
            Assert.NotEqual(default, mockAddedEntity.CreatedOn);
            Assert.Null(mockAddedEntity.LastModifiedBy);
            Assert.Equal(default, mockAddedEntity.LastModifiedOn);

            Assert.Equal("original-creator", mockModifiedEntity.CreatedBy);
            Assert.Equal("test-user-id", mockModifiedEntity.LastModifiedBy);
            Assert.NotEqual(default, mockModifiedEntity.LastModifiedOn);
        }

        [Fact]
        public void ApplyAuditInformation_WhenUserIdIsNull_UsesSystemUser()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns((string)null);
            var auditService = new AuditService(_currentUserServiceMock.Object);
            
            var mockEntity = new TestAuditableEntity();
            var mockEntry = CreateMockEntityEntry(mockEntity, EntityState.Added);
            var entries = new List<EntityEntry> { mockEntry.Object };

            // Act
            auditService.ApplyAuditInformation(entries);

            // Assert
            Assert.Equal("System", mockEntity.CreatedBy);
            Assert.NotEqual(default, mockEntity.CreatedOn);
        }

        private Mock<EntityEntry> CreateMockEntityEntry<TEntity>(TEntity entity, EntityState state) where TEntity : class
        {
            var mockEntry = new Mock<EntityEntry>(MockBehavior.Default);
            mockEntry.Setup(e => e.Entity).Returns(entity);
            mockEntry.Setup(e => e.State).Returns(state);
            return mockEntry;
        }

        private class TestAuditableEntity : IAuditable
        {
            public string CreatedBy { get; set; }
            public DateTime CreatedOn { get; set; }
            public string LastModifiedBy { get; set; }
            public DateTime LastModifiedOn { get; set; }
        }

        private class TestNonAuditableEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
