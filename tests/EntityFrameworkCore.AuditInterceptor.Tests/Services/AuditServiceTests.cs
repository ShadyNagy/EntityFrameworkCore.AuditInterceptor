using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Services
{
    public class AuditServiceTests
    {
        [Fact]
        public void ApplyAuditInformation_ForNewEntity_SetsCreatedProperties()
        {
            // Arrange
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(x => x.UserId).Returns("testuser123");
            
            var auditService = new AuditService(currentUserServiceMock.Object);
            
            var mockEntity = new TestAuditableEntity();
            var mockEntityEntry = CreateMockEntityEntry(mockEntity, EntityState.Added);
            var entries = new List<EntityEntry> { mockEntityEntry };
            
            // Act
            auditService.ApplyAuditInformation(entries);
            
            // Assert
            Assert.Equal("testuser123", mockEntity.CreatedBy);
            Assert.NotEqual(default, mockEntity.CreatedOn);
            Assert.Null(mockEntity.LastModifiedBy);
            Assert.Equal(default, mockEntity.LastModifiedOn);
        }
        
        [Fact]
        public void ApplyAuditInformation_ForModifiedEntity_SetsModifiedProperties()
        {
            // Arrange
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(x => x.UserId).Returns("testuser456");
            
            var auditService = new AuditService(currentUserServiceMock.Object);
            
            var mockEntity = new TestAuditableEntity
            {
                CreatedBy = "originaluser",
                CreatedOn = DateTime.UtcNow.AddDays(-1)
            };
            var mockEntityEntry = CreateMockEntityEntry(mockEntity, EntityState.Modified);
            var entries = new List<EntityEntry> { mockEntityEntry };
            
            // Act
            auditService.ApplyAuditInformation(entries);
            
            // Assert
            Assert.Equal("originaluser", mockEntity.CreatedBy); // Should not change
            Assert.NotEqual(default, mockEntity.CreatedOn);
            Assert.Equal("testuser456", mockEntity.LastModifiedBy);
            Assert.NotEqual(default, mockEntity.LastModifiedOn);
        }
        
        [Fact]
        public void ApplyAuditInformation_ForNonAuditableEntity_DoesNothing()
        {
            // Arrange
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(x => x.UserId).Returns("testuser789");
            
            var auditService = new AuditService(currentUserServiceMock.Object);
            
            var mockEntity = new NonAuditableEntity();
            var mockEntityEntry = CreateMockEntityEntry(mockEntity, EntityState.Added);
            var entries = new List<EntityEntry> { mockEntityEntry };
            
            // Act - This should not throw an exception
            auditService.ApplyAuditInformation(entries);
            
            // Assert - No assertion needed, we're just verifying it doesn't throw
        }
        
        [Fact]
        public void ApplyAuditInformation_WithMultipleEntities_SetsPropertiesCorrectly()
        {
            // Arrange
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(x => x.UserId).Returns("testuser999");
            
            var auditService = new AuditService(currentUserServiceMock.Object);
            
            var newEntity = new TestAuditableEntity();
            var modifiedEntity = new TestAuditableEntity
            {
                CreatedBy = "originaluser",
                CreatedOn = DateTime.UtcNow.AddDays(-1)
            };
            var nonAuditableEntity = new NonAuditableEntity();
            
            var entries = new List<EntityEntry>
            {
                CreateMockEntityEntry(newEntity, EntityState.Added),
                CreateMockEntityEntry(modifiedEntity, EntityState.Modified),
                CreateMockEntityEntry(nonAuditableEntity, EntityState.Added)
            };
            
            // Act
            auditService.ApplyAuditInformation(entries);
            
            // Assert
            Assert.Equal("testuser999", newEntity.CreatedBy);
            Assert.NotEqual(default, newEntity.CreatedOn);
            
            Assert.Equal("originaluser", modifiedEntity.CreatedBy);
            Assert.Equal("testuser999", modifiedEntity.LastModifiedBy);
            Assert.NotEqual(default, modifiedEntity.LastModifiedOn);
        }
        
        [Fact]
        public void ApplyAuditInformation_WithNullUserId_SetsSystemUserForAuditFields()
        {
            // Arrange
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(x => x.UserId).Returns((string)null);
            
            var auditService = new AuditService(currentUserServiceMock.Object);
            
            var newEntity = new TestAuditableEntity();
            var mockEntityEntry = CreateMockEntityEntry(newEntity, EntityState.Added);
            var entries = new List<EntityEntry> { mockEntityEntry };
            
            // Act
            auditService.ApplyAuditInformation(entries);
            
            // Assert
            Assert.Equal("System", newEntity.CreatedBy);
            Assert.NotEqual(default, newEntity.CreatedOn);
        }
        
        private EntityEntry CreateMockEntityEntry<T>(T entity, EntityState state) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            var mockContext = new Mock<DbContext>();
            mockContext.Setup(c => c.Set<T>()).Returns(mockSet.Object);
            
            var entityEntry = mockContext.Object.Entry(entity);
            
            // Use reflection to set the State property since it's read-only
            var entityEntryType = entityEntry.GetType();
            var stateProperty = entityEntryType.GetProperty("State");
            stateProperty?.SetValue(entityEntry, state);
            
            return entityEntry;
        }
        
        private class TestAuditableEntity : IAuditable
        {
            public string CreatedBy { get; set; }
            public DateTime CreatedOn { get; set; }
            public string LastModifiedBy { get; set; }
            public DateTime LastModifiedOn { get; set; }
        }
        
        private class NonAuditableEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
