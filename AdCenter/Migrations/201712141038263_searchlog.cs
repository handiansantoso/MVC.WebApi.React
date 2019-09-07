namespace AdCenter.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class searchlog : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ImpressionLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DateCreatedGMT = c.DateTime(nullable: false, precision: 0),
                        AdId = c.Int(nullable: false),
                        AdvertiserId = c.Int(nullable: false),
                        BidPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.ImpressionLogs");
        }
    }
}
