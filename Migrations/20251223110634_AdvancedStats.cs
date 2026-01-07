using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FutbolSitesi.Migrations
{
    /// <inheritdoc />
    public partial class AdvancedStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AwayGoalsMinutes",
                table: "Matches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FoulsAway",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FoulsHome",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HomeGoalsMinutes",
                table: "Matches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PossessionAway",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PossessionHome",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShotsAway",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShotsHome",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShotsOnTargetAway",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShotsOnTargetHome",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayGoalsMinutes",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "FoulsAway",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "FoulsHome",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeGoalsMinutes",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PossessionAway",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PossessionHome",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ShotsAway",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ShotsHome",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ShotsOnTargetAway",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ShotsOnTargetHome",
                table: "Matches");
        }
    }
}
