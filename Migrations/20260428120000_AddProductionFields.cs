using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Teste.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Project ────────────────────────────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "Crea",
                table: "Projects",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientManager",
                table: "Projects",
                type: "text",
                nullable: false,
                defaultValue: "");

            // ── Report ─────────────────────────────────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "Revisao",
                table: "Reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // ── Companion ──────────────────────────────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Companions",
                type: "integer",
                nullable: true);

            // ── Empresa (nova tabela) ─────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id          = table.Column<int>(type: "integer", nullable: false)
                                       .Annotation("Npgsql:ValueGenerationStrategy",
                                                   NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome        = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    ImagemPath  = table.Column<string>(type: "text", nullable: true),
                    IsActive    = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    UpdatedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted   = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Empresas");

            migrationBuilder.DropColumn(name: "EmpresaId",     table: "Companions");
            migrationBuilder.DropColumn(name: "Revisao",       table: "Reports");
            migrationBuilder.DropColumn(name: "Crea",          table: "Projects");
            migrationBuilder.DropColumn(name: "ClientManager", table: "Projects");
        }
    }
}
