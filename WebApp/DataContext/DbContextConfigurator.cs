using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace WebApp.DataContext
{
	public interface IDbContextConfigurator
	{
		public void Configure(ModelBuilder modelBuilder);
	}

	public class DbContextConfigurator : IDbContextConfigurator
	{
		[NotNull]
		private readonly Type[] _types;

		public DbContextConfigurator(Type[] types)
		{
			_types = types;
		}

		public void Configure(ModelBuilder modelBuilder)
		{
			modelBuilder.RegisterEntities(_types);
		}
	}
}
