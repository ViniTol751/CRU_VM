using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDO.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A coluna Type já foi adicionada por AddPhotoTypeForSQLite (20260414193159).
            // Apenas corrige registros com valor vazio gerado pelo default "" daquela migration.
            migrationBuilder.Sql(
                "UPDATE \"Photo\" SET \"Type\" = 'photo' WHERE \"Type\" = '' OR \"Type\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Type", table: "Photo");
        }
    }
}
