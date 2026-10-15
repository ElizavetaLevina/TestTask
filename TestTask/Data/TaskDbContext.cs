using Microsoft.EntityFrameworkCore;
using TestTask.Models;

namespace TestTask.Data
{
	public class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
	{
		public DbSet<TaskItem> Tasks => Set<TaskItem>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<TaskItem>(entity =>
			{
				entity.Property(t => t.Title)
					.IsRequired()
					.HasMaxLength(200);

				entity.Property(t => t.Version)
					.IsRowVersion();
			});
		}
	}
}
