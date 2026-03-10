using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GeoCare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialtiesAndResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Hospitales",
                table: "Hospitales");

            migrationBuilder.DropColumn(
                name: "HasSpecialCare",
                table: "Hospitales");

            migrationBuilder.RenameTable(
                name: "Hospitales",
                newName: "Hospitals");

            migrationBuilder.RenameColumn(
                name: "UpDatedBy",
                table: "Hospitals",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "AvaibleBeds",
                table: "Hospitals",
                newName: "Type");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Hospitals",
                table: "Hospitals",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "MedicalResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HospitalId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalResources_Hospitals_HospitalId",
                        column: x => x.HospitalId,
                        principalTable: "Hospitals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Specialties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HospitalSpecialties",
                columns: table => new
                {
                    HospitalId = table.Column<int>(type: "int", nullable: false),
                    SpecialtyId = table.Column<int>(type: "int", nullable: false),
                    HandlesEmergencies = table.Column<bool>(type: "bit", nullable: false),
                    Schedule = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalSpecialties", x => new { x.HospitalId, x.SpecialtyId });
                    table.ForeignKey(
                        name: "FK_HospitalSpecialties_Hospitals_HospitalId",
                        column: x => x.HospitalId,
                        principalTable: "Hospitals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HospitalSpecialties_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Specialties",
                columns: new[] { "Id", "Category", "Name" },
                values: new object[,]
                {
                    { 1, 0, "Medicina General" },
                    { 2, 0, "Urgencias" },
                    { 3, 0, "Pediatría" },
                    { 4, 0, "Ginecología y Obstetricia" },
                    { 5, 0, "Medicina Interna" },
                    { 6, 0, "Medicina Familiar" },
                    { 7, 1, "Cirugía General" },
                    { 8, 1, "Traumatología y Ortopedia" },
                    { 9, 1, "Neurocirugía" },
                    { 10, 1, "Cirugía Plástica" },
                    { 11, 1, "Cirugía Cardiovascular" },
                    { 12, 1, "Urología" },
                    { 13, 2, "Radiología e Imagen" },
                    { 14, 2, "Patología" },
                    { 15, 2, "Laboratorio Clínico" },
                    { 16, 2, "Medicina Nuclear" },
                    { 17, 2, "Anestesiología" },
                    { 18, 3, "Cardiología" },
                    { 19, 3, "Neurología" },
                    { 20, 3, "Oncología" },
                    { 21, 3, "Dermatología" },
                    { 22, 3, "Oftalmología" },
                    { 23, 3, "Psiquiatría" },
                    { 24, 3, "Endocrinología" },
                    { 25, 3, "Gastroenterología" },
                    { 26, 3, "Neumología" },
                    { 27, 3, "Nefrología" },
                    { 28, 3, "Hematología" },
                    { 29, 3, "Infectología" },
                    { 30, 3, "Rehabilitación" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalSpecialties_SpecialtyId",
                table: "HospitalSpecialties",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalResources_HospitalId",
                table: "MedicalResources",
                column: "HospitalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HospitalSpecialties");

            migrationBuilder.DropTable(
                name: "MedicalResources");

            migrationBuilder.DropTable(
                name: "Specialties");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Hospitals",
                table: "Hospitals");

            migrationBuilder.RenameTable(
                name: "Hospitals",
                newName: "Hospitales");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "Hospitales",
                newName: "UpDatedBy");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Hospitales",
                newName: "AvaibleBeds");

            migrationBuilder.AddColumn<bool>(
                name: "HasSpecialCare",
                table: "Hospitales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Hospitales",
                table: "Hospitales",
                column: "Id");
        }
    }
}
