using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDO.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarRelatorioAcompanhante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RelatorioAcompanhantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RelatorioId = table.Column<int>(type: "INTEGER", nullable: false),
                    AcompanhanteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatorioAcompanhantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelatorioAcompanhantes_Acompanhantes_AcompanhanteId",
                        column: x => x.AcompanhanteId,
                        principalTable: "Acompanhantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RelatorioAcompanhantes_Relatorios_RelatorioId",
                        column: x => x.RelatorioId,
                        principalTable: "Relatorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RelatorioAcompanhantes_AcompanhanteId",
                table: "RelatorioAcompanhantes",
                column: "AcompanhanteId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatorioAcompanhantes_RelatorioId",
                table: "RelatorioAcompanhantes",
                column: "RelatorioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RelatorioAcompanhantes");
        }
    }
}
