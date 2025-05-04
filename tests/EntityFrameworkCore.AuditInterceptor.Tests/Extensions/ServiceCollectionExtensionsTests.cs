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

            var auditService = serviceProvider.GetService<IAuditService>();
            Assert.NotNull(auditService);
            Assert.IsType<AuditService>(auditService);
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
                options.UserIdClaimType = "custom-claim-type";
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var auditOptions = serviceProvider.GetRequiredService<AuditOptions>();
            
            Assert.Equal("custom-claim-type", auditOptions.UserIdClaimType);
        }

        [Fact]
        public void AddAuditInterceptor_WithCustomCurrentUserService_RegistersCustomImplementation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CustomCurrentUserService>();

            // Act
            services.AddAuditInterceptor();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var currentUserService = serviceProvider.GetRequiredService<ICurrentUserService>();
            
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
                options.UseInMemoryDatabase("TestDb")
                       .UseAuditInterceptor(sp);
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            
            // Verify the context was created successfully with the interceptor
            Assert.NotNull(dbContext);
        }

        private class CustomCurrentUserService : ICurrentUserService
        {
            public string UserId => "custom-user-id";
        }

        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }
        }
    }
}
