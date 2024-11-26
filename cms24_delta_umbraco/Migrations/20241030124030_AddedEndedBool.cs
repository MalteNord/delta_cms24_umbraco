using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cms24_delta_umbraco.Migrations
{
    /// <inheritdoc />
    public partial class AddedEndedBool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnded",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnded",
                table: "Rooms");
        }
    }
}
