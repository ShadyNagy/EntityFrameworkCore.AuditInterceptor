using System;

namespace EntityFrameworkCore.AuditInterceptor.Interfaces;

public interface IAuditable
{
  string CreatedBy { get; set; }
  DateTime CreatedAt { get; set; }
  string UpdatedBy { get; set; }
  DateTime UpdatedAt { get; set; }
}
