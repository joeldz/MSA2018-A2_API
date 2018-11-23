using Microsoft.EntityFrameworkCore.Migrations;

namespace jdezscreenshotservice.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScreenshotItem",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Series = table.Column<string>(nullable: true),
                    Episode = table.Column<string>(nullable: true),
                    Timestamp = table.Column<string>(nullable: true),
                    Subtitle = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    Uploaded = table.Column<string>(nullable: true),
                    Width = table.Column<string>(nullable: true),
                    Height = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScreenshotItem", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScreenshotItem");
        }
    }
}
