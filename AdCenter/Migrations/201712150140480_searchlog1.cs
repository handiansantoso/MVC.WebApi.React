namespace AdCenter.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class searchlog1 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.ImpressionLogs", "BidPrice", c => c.Decimal(nullable: false, precision: 9, scale: 4));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.ImpressionLogs", "BidPrice", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
    }
}
