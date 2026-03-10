using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeoCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEstratoToHospital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Estrato",
                table: "Hospitals",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Estrato",
                table: "Hospitals");
        }
    }
}
