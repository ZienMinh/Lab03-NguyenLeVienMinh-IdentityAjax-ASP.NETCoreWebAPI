using BusinessObjects.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PRN231.ExploreNow.BusinessObject.Entities;

namespace Repositories.Context
{
	public class ApplicationDbContext : BaseDbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		public virtual DbSet<Category> Categories { get; set; }

		public virtual DbSet<Product> Products { get; set; }

		public DbSet<ApplicationUser> Users { get; set; }

		public DbSet<RefreshToken> RefreshTokens { get; set; }

		protected override void OnModelCreating(ModelBuilder optionsBuilder)
		{
			base.OnModelCreating(optionsBuilder);

			optionsBuilder.Entity<Product>()
				.HasOne(p => p.User)
				.WithMany(u => u.Products) 
				.HasForeignKey(p => p.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			optionsBuilder.Entity<Category>().HasData(
				new Category { CategoryId = 1, CategoryName = "Beverages" },
				new Category { CategoryId = 2, CategoryName = "Condiments" },
				new Category { CategoryId = 3, CategoryName = "Confections" },
				new Category { CategoryId = 4, CategoryName = "Dairy Products" },
				new Category { CategoryId = 5, CategoryName = "Grains/Crereals" },
				new Category { CategoryId = 6, CategoryName = "Meat/Poultry" },
				new Category { CategoryId = 7, CategoryName = "Produce" },
				new Category { CategoryId = 8, CategoryName = "Seafood" }
			);
		}
	}
}
