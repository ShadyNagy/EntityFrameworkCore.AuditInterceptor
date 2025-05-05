using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.AuditInterceptor.Extensions;


public class AuditOptions(IServiceCollection services)
{
  public IServiceCollection Services { get; } = services;
}
