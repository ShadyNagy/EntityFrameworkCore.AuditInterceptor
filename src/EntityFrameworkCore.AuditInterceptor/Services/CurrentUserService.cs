using System.Security.Claims;
using EntityFrameworkCore.AuditInterceptor.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EntityFrameworkCore.AuditInterceptor.Services;
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
  private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

  public string? GetUserId()
  {
    return _httpContextAccessor.HttpContext?.User?
      .FindFirst(ClaimTypes.NameIdentifier)?.Value;
  }
}
