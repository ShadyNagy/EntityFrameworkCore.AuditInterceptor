using EntityFrameworkCore.AuditInterceptor.Extensions;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using EntityFrameworkCore.AuditInterceptor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityFrameworkCore.AuditInterceptor.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAuditing_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        
        // Act
        services.AddAuditing();
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var currentUserService = serviceProvider.GetService<ICurrentUserService>();
        var auditService = serviceProvider.GetService<AuditService>();
        
        Assert.NotNull(currentUserService);
        Assert.NotNull(auditService);
        Assert.IsType<CurrentUserService>(currentUserService);
    }
    
    [Fact]
    public void AddAuditing_WithOptions_ExecutesConfigureOptionsAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var optionsActionExecuted = false;
        
        // Act
        services.AddAuditing(options => 
        {
            optionsActionExecuted = true;
            Assert.NotNull(options.Services);
        });
        
        // Assert
        Assert.True(optionsActionExecuted);
    }
    
    [Fact]
    public void AddAuditInterceptors_RegistersAuditInterceptor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddAuditing();
        services.AddDbContext<TestDbContext>(options => 
            options.UseInMemoryDatabase("TestDb")
                   .AddAuditInterceptors());
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act
        var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
        
        // Assert
        // The test passes if no exception is thrown when getting the DbContext
        Assert.NotNull(dbContext);
    }
    
    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }
    }
}
