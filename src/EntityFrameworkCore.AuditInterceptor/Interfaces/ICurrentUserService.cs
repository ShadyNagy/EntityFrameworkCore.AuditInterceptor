namespace EntityFrameworkCore.AuditInterceptor.Interfaces;

public interface ICurrentUserService
{
  string? GetUserId();
}
