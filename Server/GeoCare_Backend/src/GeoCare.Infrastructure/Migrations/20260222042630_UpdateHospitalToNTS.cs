using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace GeoCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHospitalToNTS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Hospitales");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Hospitales");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Hospitales",
                type: "geography",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Hospitales");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Hospitales",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Hospitales",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
