using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDO.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportCompanionAliasColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RelatorioId",
                table: "ReportCompanion",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AcompanhanteId",
                table: "ReportCompanion",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Sincroniza os valores alias com as colunas reais
            migrationBuilder.Sql(
                "UPDATE \"ReportCompanion\" SET \"RelatorioId\" = \"ReportId\", \"AcompanhanteId\" = \"CompanionId\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RelatorioId", table: "ReportCompanion");
            migrationBuilder.DropColumn(name: "AcompanhanteId", table: "ReportCompanion");
        }
    }
}
