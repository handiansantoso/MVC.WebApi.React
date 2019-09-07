namespace AdCenter.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class add_default_tier : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Tier", "Default", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Tier", "Default");
        }
    }
}
