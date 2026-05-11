using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Teste.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Revisao",
                table: "Reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ClientManager",
                table: "Projects",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Crea",
                table: "Projects",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Companions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    ImagemPath = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherDetails_UpdatedAt",
                table: "WeatherDetails",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UpdatedAt",
                table: "Users",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Signatures_UpdatedAt",
                table: "Signatures",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_UpdatedAt",
                table: "Reports",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReportEquipments_UpdatedAt",
                table: "ReportEquipments",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReportCompanions_UpdatedAt",
                table: "ReportCompanions",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UpdatedAt",
                table: "Projects",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UpdatedAt",
                table: "ProjectMembers",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_UpdatedAt",
                table: "Photos",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Occurrences_UpdatedAt",
                table: "Occurrences",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_UpdatedAt",
                table: "Materials",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_UpdatedAt",
                table: "Equipments",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UpdatedAt",
                table: "Employees",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Companions_UpdatedAt",
                table: "Companions",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_UpdatedAt",
                table: "Activities",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_UpdatedAt",
                table: "Empresas",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropIndex(
                name: "IX_WeatherDetails_UpdatedAt",
                table: "WeatherDetails");

            migrationBuilder.DropIndex(
                name: "IX_Users_UpdatedAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Signatures_UpdatedAt",
                table: "Signatures");

            migrationBuilder.DropIndex(
                name: "IX_Reports_UpdatedAt",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_ReportEquipments_UpdatedAt",
                table: "ReportEquipments");

            migrationBuilder.DropIndex(
                name: "IX_ReportCompanions_UpdatedAt",
                table: "ReportCompanions");

            migrationBuilder.DropIndex(
                name: "IX_Projects_UpdatedAt",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_UpdatedAt",
                table: "ProjectMembers");

            migrationBuilder.DropIndex(
                name: "IX_Photos_UpdatedAt",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Occurrences_UpdatedAt",
                table: "Occurrences");

            migrationBuilder.DropIndex(
                name: "IX_Materials_UpdatedAt",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_UpdatedAt",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Employees_UpdatedAt",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Companions_UpdatedAt",
                table: "Companions");

            migrationBuilder.DropIndex(
                name: "IX_Activities_UpdatedAt",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Revisao",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ClientManager",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Crea",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Companions");
        }
    }
}
