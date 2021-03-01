


using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebApi.Entities;

namespace WebApi.Helpers
{
    public class WebCrawlerDbContext : DbContext
    {
        protected readonly IConfiguration Configuration;

        public WebCrawlerDbContext(DbContextOptions<DbContext> options)
            : base(options)
        { }

        public WebCrawlerDbContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // connect to sql server database
            options.UseSqlServer(Configuration.GetConnectionString("WebApiDatabase"));
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UrlEntries> UrlEntries { get; set; }
    }
}