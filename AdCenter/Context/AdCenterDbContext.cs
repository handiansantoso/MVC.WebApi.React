using AdCenter.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace AdCenter.Context
{
    public class AdCenterDbContext : DbContext
    {
        public AdCenterDbContext() : base("AdCenterConnection")
        {
            Database.SetInitializer<AdCenterDbContext>(new AdCenterDbInitializer());
            //this.Configuration.LazyLoadingEnabled = false;
            //this.Configuration.ProxyCreationEnabled = false;
        }

        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<AdGroup> AdGroups { get; set; }
        public DbSet<Ad> Ads { get; set; }
        public DbSet<Keyword> Keywords { get; set; }
        public DbSet<Domain> Domains { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Tier> Tiers { get; set; }
        public DbSet<SearchLog> SearchLog { get; set; }
        public DbSet<ImpressionLog> ImpressionLog { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AdGroup>().Property(p => p.Bid).HasPrecision(9, 4);
            modelBuilder.Entity<Keyword>().Property(p => p.BidPrice).HasPrecision(9, 4);
            modelBuilder.Entity<Domain>().Property(p => p.BidPrice).HasPrecision(9, 4);
            modelBuilder.Entity<ImpressionLog>().Property(p => p.BidPrice).HasPrecision(9, 4);
        }
    }

    public class AdCenterDbInitializer : CreateDatabaseIfNotExists<AdCenterDbContext>
    {
        public override void InitializeDatabase(AdCenterDbContext context)
        {
            base.InitializeDatabase(context);
            
        }
    }
}