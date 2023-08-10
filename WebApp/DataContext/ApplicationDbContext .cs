using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Data.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.DataContext
{
    public static class ModelBuilderExtensions
    {
        public static void RegisterAllEntities(this ModelBuilder modelBuilder, params Assembly[] assemblies)
        {
            var types = assemblies.SelectMany(asm => asm.GetExportedTypes())
                                  .Where(cls => cls.IsClass && cls.IsPublic && !cls.IsAbstract && cls.CustomAttributes.Any(attr => attr.AttributeType == typeof(TableAttribute)))
                                  .ToArray();

            RegisterEntities(modelBuilder, types);
        }

        public static void RegisterEntities(this ModelBuilder modelBuilder, Type[] types)
        {
            foreach (var type in types)
                modelBuilder.Entity(type);
        }
    }

    public class ApplicationDbContext : DbContext
    {
        [NotNull]
        private readonly IDbContextConfigurator? _dbContextConfigurator = null;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDbContextConfigurator dbContextConfigurator) : base(options)
        {
            _dbContextConfigurator = dbContextConfigurator;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (_dbContextConfigurator != null)
            {
                _dbContextConfigurator.Configure(modelBuilder);
            }
            else
            {
                base.OnModelCreating(modelBuilder);
                var entitiesAssembly = typeof(Payment).Assembly;
                modelBuilder.RegisterAllEntities(entitiesAssembly);
            }
        }
    }
}
