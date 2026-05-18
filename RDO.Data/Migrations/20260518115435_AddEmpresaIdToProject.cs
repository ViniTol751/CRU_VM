using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RDO.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpresaIdToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Revisao",
                table: "Report",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Project",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Companion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "Activity",
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
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Project_EmpresaId",
                table: "Project",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Project_Empresas_EmpresaId",
                table: "Project",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Project_Empresas_EmpresaId",
                table: "Project");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropIndex(
                name: "IX_Project_EmpresaId",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Companion");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Activity");

            migrationBuilder.AlterColumn<int>(
                name: "Revisao",
                table: "Report",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
