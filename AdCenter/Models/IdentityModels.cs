using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using AdCenter.Context;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdCenter.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class AdUser : IdentityUser<int,AdLogin,AdUserRole,AdClaim>
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<AdUser,int> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            userIdentity.AddClaim(new Claim("Active", this.Active.ToString()));
            userIdentity.AddClaim(new Claim("Address", this.Address == null ? string.Empty : this.Address.ToString()));
            userIdentity.AddClaim(new Claim("PaymentTerm", this.PaymentTerm == null ? string.Empty : this.PaymentTerm.ToString()));
            userIdentity.AddClaim(new Claim("Balance", this.Balance.ToString()));
            userIdentity.AddClaim(new Claim("CreditLimit", this.CreditLimit.ToString()));
            return userIdentity;
        }
        public bool Active { get; set; }
        public string Address { get; set; }
        public string PaymentTerm { get; set; }
        public decimal Balance { get; set; }
        public decimal CreditLimit { get; set; }
    }

    public class AdClaim : IdentityUserClaim<int>
    {
    }

    public class AdUserRole : IdentityUserRole<int>
    {
    }

    public class AdLogin : IdentityUserLogin<int>
    {
    }

    public class AdRole : IdentityRole<int,AdUserRole>
    {
        public string Description { get; set; }
        public AdRole() { }
        public AdRole(string name) : this()
        {
            this.Name = name;
        }
        public AdRole(string name,string description) : this(name)
        {
            this.Description = description;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<AdUser,AdRole,int,AdLogin,AdUserRole,AdClaim>
    {
        static ApplicationDbContext()
        {
            Database.SetInitializer(new MySqlInitializer());
        }

        public ApplicationDbContext()
            : base("AdCenterConnection")
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AdUser>().ToTable("User");
            modelBuilder.Entity<AdRole>().ToTable("Role");
            modelBuilder.Entity<AdUserRole>().ToTable("UserRole");
            modelBuilder.Entity<AdClaim>().ToTable("UserClaim");
            modelBuilder.Entity<AdLogin>().ToTable("UserLogin");

            modelBuilder.Entity<AdUser>().Property(r => r.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            modelBuilder.Entity<AdRole>().Property(r => r.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            modelBuilder.Entity<AdClaim>().Property(r => r.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        }
    }
}