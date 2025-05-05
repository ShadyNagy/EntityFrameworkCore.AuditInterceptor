using EntityFrameworkCore.AuditInterceptor.Interfaces;

namespace EntityFrameworkCore.AuditInterceptor.Web;

public class MyEntity : IAuditable
{
  public string Id { get; set; } = string.Empty;
  public string CreatedBy { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public string UpdatedBy { get; set; } = string.Empty;
  public DateTime UpdatedAt { get; set; }

  public string Name { get; set; } = string.Empty;
}
