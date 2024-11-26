using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cms24_delta_umbraco.Migrations
{
    /// <inheritdoc />
    public partial class AddedScoreToPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "Players");
        }
    }
}
