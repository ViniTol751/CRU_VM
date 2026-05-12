using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDO.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSyncQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IF EXISTS evita falha em bancos SQLite onde SyncQueue nunca foi criada
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"SyncQueue\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Não restaura — arquitetura event-driven foi removida intencionalmente
        }
    }
}
