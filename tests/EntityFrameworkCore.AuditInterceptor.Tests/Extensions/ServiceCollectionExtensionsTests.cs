using EntityFrameworkCore.AuditInterceptor.Extensions;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Extensions
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddAuditInterceptor_RegistersRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            
            // Act
            services.AddAuditInterceptor();
            
            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            var currentUserService = serviceProvider.GetService<ICurrentUserService>();
            Assert.NotNull(currentUserService);
            Assert.IsType<CurrentUserService>(currentUserService);
            
            var auditService = serviceProvider.GetService<AuditService>();
            Assert.NotNull(auditService);
        }
        
        [Fact]
        public void AddAuditInterceptor_WithOptions_ConfiguresOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            
            // Act
            services.AddAuditInterceptor(options =>
            {
                options.DefaultUserId = "CustomDefaultUser";
            });
            
            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            var auditOptions = serviceProvider.GetRequiredService<AuditOptions>();
            Assert.Equal("CustomDefaultUser", auditOptions.DefaultUserId);
        }
        
        [Fact]
        public void AddAuditInterceptor_WithCustomCurrentUserService_RegistersCustomImplementation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ICurrentUserService, CustomCurrentUserService>();
            
            // Act
            services.AddAuditInterceptor();
            
            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            var currentUserService = serviceProvider.GetService<ICurrentUserService>();
            Assert.NotNull(currentUserService);
            Assert.IsType<CustomCurrentUserService>(currentUserService);
        }
        
        [Fact]
        public void UseAuditInterceptor_AddsInterceptorToDbContextOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            services.AddAuditInterceptor();
            
            // Act
            services.AddDbContext<TestDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase("TestDb");
                options.UseAuditInterceptor(sp);
            });
            
            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            // Getting the DbContext should not throw any exceptions
            var dbContext = serviceProvider.GetService<TestDbContext>();
            Assert.NotNull(dbContext);
        }
        
        private class CustomCurrentUserService : ICurrentUserService
        {
            public string UserId => "CustomUser";
        }
        
        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }
            
            public DbSet<TestEntity> TestEntities { get; set; }
        }
        
        private class TestEntity : IAuditable
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedOn { get; set; }
            public string LastModifiedBy { get; set; }
            public DateTime LastModifiedOn { get; set; }
        }
    }
}
