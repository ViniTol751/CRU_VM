using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDO.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCamposSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Usuarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Usuarios",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Relatorios",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Relatorios",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RelatorioEquipamentos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "RelatorioEquipamentos",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RelatorioAcompanhantes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "RelatorioAcompanhantes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Ocorrencias",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Ocorrencias",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Obras",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Obras",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MembrosObra",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MembrosObra",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Materiais",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Materiais",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Funcionarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Funcionarios",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Fotos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Fotos",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EquipamentosCadastrados",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "EquipamentosCadastrados",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Climas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Climas",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Atividades",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Atividades",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Assinaturas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Assinaturas",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Acompanhantes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Acompanhantes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Relatorios");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Relatorios");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RelatorioEquipamentos");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "RelatorioEquipamentos");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RelatorioAcompanhantes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "RelatorioAcompanhantes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Ocorrencias");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Ocorrencias");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Obras");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Obras");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MembrosObra");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MembrosObra");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Materiais");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Materiais");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Funcionarios");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Funcionarios");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Fotos");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Fotos");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EquipamentosCadastrados");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "EquipamentosCadastrados");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Climas");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Climas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Atividades");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Atividades");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Acompanhantes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Acompanhantes");
        }
    }
}
