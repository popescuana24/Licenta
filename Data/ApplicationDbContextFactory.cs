using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ClothingWebApp.Data
{
    
    /// Factory for creating DbContext instances at design time
    /// Used by EF Core migrations and other design-time tools
   
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Build configuration
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            
            // Get connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            // Create DbContextOptions
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            
            // Create DbContext
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}