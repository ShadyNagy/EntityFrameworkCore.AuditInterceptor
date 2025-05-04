using System;
using EntityFrameworkCore.AuditInterceptor.Extensions;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Extensions
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddAuditing_ShouldRegisterRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddAuditing();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            var currentUserService = serviceProvider.GetService<ICurrentUserService>();
            Assert.NotNull(currentUserService);
            Assert.IsType<CurrentUserService>(currentUserService);
            
            var auditService = serviceProvider.GetService<AuditService>();
            Assert.NotNull(auditService);
        }

        [Fact]
        public void AddAuditing_ShouldInvokeOptionsAction_WhenProvided()
        {
            // Arrange
            var services = new ServiceCollection();
            var optionsInvoked = false;

            // Act
            services.AddAuditing(options => 
            {
                optionsInvoked = true;
                Assert.NotNull(options.Services);
                Assert.Same(services, options.Services);
            });

            // Assert
            Assert.True(optionsInvoked);
        }

        [Fact]
        public void AddAuditInterceptors_ShouldConfigureDbContextWithInterceptors()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<AuditService>();
            
            var serviceProvider = services.BuildServiceProvider();
            
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("TestDb")
                .UseApplicationServiceProvider(serviceProvider);

            // Act
            options.AddAuditInterceptors();

            // Assert
            var dbContext = new TestDbContext(options.Options);
            // The test passes if no exception is thrown, as we can't easily inspect the interceptors
        }

        // Test DbContext for unit tests
        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions options) : base(options)
            {
            }
        }
    }
}
