using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutbolSitesi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HomeTeam = table.Column<string>(type: "TEXT", nullable: false),
                    AwayTeam = table.Column<string>(type: "TEXT", nullable: false),
                    Winner = table.Column<string>(type: "TEXT", nullable: false),
                    GoalHome = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalAway = table.Column<int>(type: "INTEGER", nullable: false),
                    CornerHome = table.Column<int>(type: "INTEGER", nullable: false),
                    CornerAway = table.Column<int>(type: "INTEGER", nullable: false),
                    YellowHome = table.Column<int>(type: "INTEGER", nullable: false),
                    YellowAway = table.Column<int>(type: "INTEGER", nullable: false),
                    RedHome = table.Column<int>(type: "INTEGER", nullable: false),
                    RedAway = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Matches");
        }
    }
}
