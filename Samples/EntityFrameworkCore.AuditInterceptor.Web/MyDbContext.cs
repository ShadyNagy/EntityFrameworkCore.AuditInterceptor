using EntityFrameworkCore.AuditInterceptor.Interfaces;
using Microsoft.AspNetCore.Components;
using System;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.AuditInterceptor.Web;

public class MyDbContext : DbContext
{
  public DbSet<MyEntity> MyEntities { get; set; }

  public MyDbContext(DbContextOptions<MyDbContext> options)
    : base(options)
  {
  }
}
