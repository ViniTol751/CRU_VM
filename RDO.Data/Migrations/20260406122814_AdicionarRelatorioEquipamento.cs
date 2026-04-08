using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDO.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarRelatorioEquipamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RelatorioEquipamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RelatorioId = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipamentoCadastradoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatorioEquipamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelatorioEquipamentos_EquipamentosCadastrados_EquipamentoCadastradoId",
                        column: x => x.EquipamentoCadastradoId,
                        principalTable: "EquipamentosCadastrados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RelatorioEquipamentos_Relatorios_RelatorioId",
                        column: x => x.RelatorioId,
                        principalTable: "Relatorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RelatorioEquipamentos_EquipamentoCadastradoId",
                table: "RelatorioEquipamentos",
                column: "EquipamentoCadastradoId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatorioEquipamentos_RelatorioId",
                table: "RelatorioEquipamentos",
                column: "RelatorioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RelatorioEquipamentos");
        }
    }
}
