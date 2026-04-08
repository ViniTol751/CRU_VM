using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDO.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarHorariosAssinatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HoraEntrada",
                table: "Assinaturas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HoraIntervalo",
                table: "Assinaturas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HoraSaida",
                table: "Assinaturas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HoraEntrada",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "HoraIntervalo",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "HoraSaida",
                table: "Assinaturas");
        }
    }
}
