using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Teste.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncEventLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "SyncEventLog",
        columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            EventId = table.Column<string>(type: "text", nullable: false),
            EntityName = table.Column<string>(type: "text", nullable: false),
            EntityId = table.Column<int>(type: "integer", nullable: false),
            Operation = table.Column<string>(type: "text", nullable: false),
            DeviceId = table.Column<string>(type: "text", nullable: false),
            ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_SyncEventLog", x => x.Id);
        });

    migrationBuilder.CreateIndex(
        name: "IX_SyncEventLog_EntityName_EntityId",
        table: "SyncEventLog",
        columns: new[] { "EntityName", "EntityId" });

    migrationBuilder.CreateIndex(
        name: "IX_SyncEventLog_EventId",
        table: "SyncEventLog",
        column: "EventId",
        unique: true);

    migrationBuilder.CreateIndex(
        name: "IX_SyncEventLog_ProcessedAt",
        table: "SyncEventLog",
        column: "ProcessedAt");
}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable(name: "SyncEventLog");
}
    }
}
