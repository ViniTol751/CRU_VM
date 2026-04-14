using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDO.Data.Migrations
{
    /// <inheritdoc />
    public partial class TornarCPFOpcional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Reports_ReportId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeePresences_Reports_ReportId",
                table: "EmployeePresences");

            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Reports_ReportId",
                table: "Materials");

            migrationBuilder.DropForeignKey(
                name: "FK_Occurrences_Reports_ReportId",
                table: "Occurrences");

            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Reports_ReportId",
                table: "Photos");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_Projects_ProjectId",
                table: "ProjectMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_Users_UserId",
                table: "ProjectMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportCompanions_Companions_CompanionId",
                table: "ReportCompanions");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportCompanions_Reports_ReportId",
                table: "ReportCompanions");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportEquipments_Equipments_EquipmentId",
                table: "ReportEquipments");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportEquipments_Reports_ReportId",
                table: "ReportEquipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Companions_CompanionId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Projects_ProjectId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_UserId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Signatures_Reports_ReportId",
                table: "Signatures");

            migrationBuilder.DropForeignKey(
                name: "FK_WeatherDetails_Reports_ReportId",
                table: "WeatherDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeatherDetails",
                table: "WeatherDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Signatures",
                table: "Signatures");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reports",
                table: "Reports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReportEquipments",
                table: "ReportEquipments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReportCompanions",
                table: "ReportCompanions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Projects",
                table: "Projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectMembers",
                table: "ProjectMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Photos",
                table: "Photos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Occurrences",
                table: "Occurrences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Materials",
                table: "Materials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Equipments",
                table: "Equipments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Employees",
                table: "Employees");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmployeePresences",
                table: "EmployeePresences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Companions",
                table: "Companions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Activities",
                table: "Activities");

            migrationBuilder.RenameTable(
                name: "WeatherDetails",
                newName: "WeatherDetail");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "User");

            migrationBuilder.RenameTable(
                name: "Signatures",
                newName: "Signature");

            migrationBuilder.RenameTable(
                name: "Reports",
                newName: "Report");

            migrationBuilder.RenameTable(
                name: "ReportEquipments",
                newName: "ReportEquipment");

            migrationBuilder.RenameTable(
                name: "ReportCompanions",
                newName: "ReportCompanion");

            migrationBuilder.RenameTable(
                name: "Projects",
                newName: "Project");

            migrationBuilder.RenameTable(
                name: "ProjectMembers",
                newName: "ProjectMember");

            migrationBuilder.RenameTable(
                name: "Photos",
                newName: "Photo");

            migrationBuilder.RenameTable(
                name: "Occurrences",
                newName: "Occurrence");

            migrationBuilder.RenameTable(
                name: "Materials",
                newName: "Material");

            migrationBuilder.RenameTable(
                name: "Equipments",
                newName: "Equipment");

            migrationBuilder.RenameTable(
                name: "Employees",
                newName: "Employee");

            migrationBuilder.RenameTable(
                name: "EmployeePresences",
                newName: "EmployeePresence");

            migrationBuilder.RenameTable(
                name: "Companions",
                newName: "Companion");

            migrationBuilder.RenameTable(
                name: "Activities",
                newName: "Activity");

            migrationBuilder.RenameIndex(
                name: "IX_WeatherDetails_ReportId",
                table: "WeatherDetail",
                newName: "IX_WeatherDetail_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_Signatures_ReportId",
                table: "Signature",
                newName: "IX_Signature_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_UserId",
                table: "Report",
                newName: "IX_Report_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_ProjectId",
                table: "Report",
                newName: "IX_Report_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_CompanionId",
                table: "Report",
                newName: "IX_Report_CompanionId");

            migrationBuilder.RenameIndex(
                name: "IX_ReportEquipments_ReportId",
                table: "ReportEquipment",
                newName: "IX_ReportEquipment_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_ReportEquipments_EquipmentId",
                table: "ReportEquipment",
                newName: "IX_ReportEquipment_EquipmentId");

            migrationBuilder.RenameIndex(
                name: "IX_ReportCompanions_ReportId",
                table: "ReportCompanion",
                newName: "IX_ReportCompanion_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_ReportCompanions_CompanionId",
                table: "ReportCompanion",
                newName: "IX_ReportCompanion_CompanionId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectMembers_UserId",
                table: "ProjectMember",
                newName: "IX_ProjectMember_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectMembers_ProjectId",
                table: "ProjectMember",
                newName: "IX_ProjectMember_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Photos_ReportId",
                table: "Photo",
                newName: "IX_Photo_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_Occurrences_ReportId",
                table: "Occurrence",
                newName: "IX_Occurrence_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_Materials_ReportId",
                table: "Material",
                newName: "IX_Material_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_EmployeePresences_ReportId",
                table: "EmployeePresence",
                newName: "IX_EmployeePresence_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_Activities_ReportId",
                table: "Activity",
                newName: "IX_Activity_ReportId");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "WeatherDetail",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Condicao",
                table: "WeatherDetail",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "IndicePluviometrico",
                table: "WeatherDetail",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Periodo",
                table: "WeatherDetail",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RelatorioId",
                table: "WeatherDetail",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tempo",
                table: "WeatherDetail",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "User",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Perfil",
                table: "User",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SenhaHash",
                table: "User",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "CPF",
                table: "Signature",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "Assinado",
                table: "Signature",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Cargo",
                table: "Signature",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataAssinatura",
                table: "Signature",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FuncionarioId",
                table: "Signature",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HoraEntrada",
                table: "Signature",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HoraIntervalo",
                table: "Signature",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HoraSaida",
                table: "Signature",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NomeAssinante",
                table: "Signature",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RelatorioId",
                table: "Signature",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AcompanhanteId",
                table: "Report",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "Report",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "Data",
                table: "Report",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "HoraEntrada",
                table: "Report",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HoraIntervalo",
                table: "Report",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HoraSaida",
                table: "Report",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Numero",
                table: "Report",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ObraId",
                table: "Report",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ObsGerais",
                table: "Report",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Rascunho",
                table: "Report",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Sincronizado",
                table: "Report",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Report",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EquipamentoCadastradoId",
                table: "ReportEquipment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RelatorioId",
                table: "ReportEquipment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AcompanhanteId",
                table: "ReportCompanion",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RelatorioId",
                table: "ReportCompanion",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Project",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Contratante",
                table: "Project",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataInicio",
                table: "Project",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Endereco",
                table: "Project",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Grupo",
                table: "Project",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImagemPath",
                table: "Project",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "Project",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PrevisaoTermino",
                table: "Project",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Responsavel",
                table: "Project",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoContrato",
                table: "Project",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ObraId",
                table: "ProjectMember",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Papel",
                table: "ProjectMember",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "ProjectMember",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AtividadeRelacionada",
                table: "Photo",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CaminhoArquivo",
                table: "Photo",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Legenda",
                table: "Photo",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RelatorioId",
                table: "Photo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TiradaEm",
                table: "Photo",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Occurrence",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HoraFim",
                table: "Occurrence",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HoraInicio",
                table: "Occurrence",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RelatorioId",
                table: "Occurrence",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "Material",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Quantidade",
                table: "Material",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RelatorioId",
                table: "Material",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Material",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Unidade",
                table: "Material",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Equipment",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Fabricante",
                table: "Equipment",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Modelo",
                table: "Equipment",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "Equipment",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroSerie",
                table: "Equipment",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Employee",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Contato",
                table: "Employee",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Empresa",
                table: "Employee",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Funcao",
                table: "Employee",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "Employee",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Employee",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Funcao",
                table: "EmployeePresence",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HorasTrabalhadas",
                table: "EmployeePresence",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NomeFuncionario",
                table: "EmployeePresence",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RelatorioId",
                table: "EmployeePresence",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Companion",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Cargo",
                table: "Companion",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Contato",
                table: "Companion",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Grupo",
                table: "Companion",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "Companion",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Activity",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Local",
                table: "Activity",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RelatorioId",
                table: "Activity",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeatherDetail",
                table: "WeatherDetail",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Signature",
                table: "Signature",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Report",
                table: "Report",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReportEquipment",
                table: "ReportEquipment",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReportCompanion",
                table: "ReportCompanion",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Project",
                table: "Project",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectMember",
                table: "ProjectMember",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Photo",
                table: "Photo",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Occurrence",
                table: "Occurrence",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Material",
                table: "Material",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Equipment",
                table: "Equipment",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Employee",
                table: "Employee",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmployeePresence",
                table: "EmployeePresence",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Companion",
                table: "Companion",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Activity",
                table: "Activity",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Activity_Report_ReportId",
                table: "Activity",
                column: "ReportId",
                principalTable: "Report",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeePresence_Report_ReportId",
                table: "EmployeePresence",
                column: "ReportId",
                principalTable: "Report",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Material_Report_ReportId",
                table: "Material",
                column: "ReportId",
                principalTable: "Report",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Occurrence_Report_ReportId",
                table: "Occurrence",
                column: "ReportId",
                principalTable: "Report",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Photo_Report_ReportId",
                table: "Photo",
                column: "ReportId",
                principalTable: "Report",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMember_Project_ProjectId",
                table: "ProjectMember",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMember_User_UserId",
                table: "ProjectMember",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Report_Companion_CompanionId",
                table: "Report",
                column: "CompanionId",
                principalTable: "Companion",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Report_Project_ProjectId",
                table: "Report",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Report_User_UserId",
                table: "Report",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportCompanion_Companion_CompanionId",
                table: "ReportCompanion",
                column: "CompanionId",
                principalTable: "Companion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportCompanion_Report_ReportId",
                table: "ReportCompanion",
                column: "ReportId",
                principalTable: "Report",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportEquipment_Equipment_EquipmentId",
                table: "ReportEquipment",
                column: "EquipmentId",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportEquipment_Report_ReportId",
                table: "ReportEquipment",
                column: "ReportId",
                principalTable: "Report",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Signature_Report_ReportId",
                table: "Signature",
                column: "ReportId",
                principalTable: "Report",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WeatherDetail_Report_ReportId",
                table: "WeatherDetail",
                column: "ReportId",
                principalTable: "Report",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activity_Report_ReportId",
                table: "Activity");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeePresence_Report_ReportId",
                table: "EmployeePresence");

            migrationBuilder.DropForeignKey(
                name: "FK_Material_Report_ReportId",
                table: "Material");

            migrationBuilder.DropForeignKey(
                name: "FK_Occurrence_Report_ReportId",
                table: "Occurrence");

            migrationBuilder.DropForeignKey(
                name: "FK_Photo_Report_ReportId",
                table: "Photo");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMember_Project_ProjectId",
                table: "ProjectMember");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMember_User_UserId",
                table: "ProjectMember");

            migrationBuilder.DropForeignKey(
                name: "FK_Report_Companion_CompanionId",
                table: "Report");

            migrationBuilder.DropForeignKey(
                name: "FK_Report_Project_ProjectId",
                table: "Report");

            migrationBuilder.DropForeignKey(
                name: "FK_Report_User_UserId",
                table: "Report");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportCompanion_Companion_CompanionId",
                table: "ReportCompanion");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportCompanion_Report_ReportId",
                table: "ReportCompanion");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportEquipment_Equipment_EquipmentId",
                table: "ReportEquipment");

            migrationBuilder.DropForeignKey(
                name: "FK_ReportEquipment_Report_ReportId",
                table: "ReportEquipment");

            migrationBuilder.DropForeignKey(
                name: "FK_Signature_Report_ReportId",
                table: "Signature");

            migrationBuilder.DropForeignKey(
                name: "FK_WeatherDetail_Report_ReportId",
                table: "WeatherDetail");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeatherDetail",
                table: "WeatherDetail");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Signature",
                table: "Signature");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReportEquipment",
                table: "ReportEquipment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReportCompanion",
                table: "ReportCompanion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Report",
                table: "Report");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectMember",
                table: "ProjectMember");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Project",
                table: "Project");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Photo",
                table: "Photo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Occurrence",
                table: "Occurrence");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Material",
                table: "Material");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Equipment",
                table: "Equipment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmployeePresence",
                table: "EmployeePresence");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Employee",
                table: "Employee");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Companion",
                table: "Companion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Activity",
                table: "Activity");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "WeatherDetail");

            migrationBuilder.DropColumn(
                name: "Condicao",
                table: "WeatherDetail");

            migrationBuilder.DropColumn(
                name: "IndicePluviometrico",
                table: "WeatherDetail");

            migrationBuilder.DropColumn(
                name: "Periodo",
                table: "WeatherDetail");

            migrationBuilder.DropColumn(
                name: "RelatorioId",
                table: "WeatherDetail");

            migrationBuilder.DropColumn(
                name: "Tempo",
                table: "WeatherDetail");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Perfil",
                table: "User");

            migrationBuilder.DropColumn(
                name: "SenhaHash",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Assinado",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "Cargo",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "DataAssinatura",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "FuncionarioId",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "HoraEntrada",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "HoraIntervalo",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "HoraSaida",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "NomeAssinante",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "RelatorioId",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "EquipamentoCadastradoId",
                table: "ReportEquipment");

            migrationBuilder.DropColumn(
                name: "RelatorioId",
                table: "ReportEquipment");

            migrationBuilder.DropColumn(
                name: "AcompanhanteId",
                table: "ReportCompanion");

            migrationBuilder.DropColumn(
                name: "RelatorioId",
                table: "ReportCompanion");

            migrationBuilder.DropColumn(
                name: "AcompanhanteId",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "HoraEntrada",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "HoraIntervalo",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "HoraSaida",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "Numero",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "ObraId",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "ObsGerais",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "Rascunho",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "Sincronizado",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "ObraId",
                table: "ProjectMember");

            migrationBuilder.DropColumn(
                name: "Papel",
                table: "ProjectMember");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "ProjectMember");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "Contratante",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "DataInicio",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "Endereco",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "Grupo",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "ImagemPath",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "PrevisaoTermino",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "Responsavel",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "TipoContrato",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "AtividadeRelacionada",
                table: "Photo");

            migrationBuilder.DropColumn(
                name: "CaminhoArquivo",
                table: "Photo");

            migrationBuilder.DropColumn(
                name: "Legenda",
                table: "Photo");

            migrationBuilder.DropColumn(
                name: "RelatorioId",
                table: "Photo");

            migrationBuilder.DropColumn(
                name: "TiradaEm",
                table: "Photo");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Occurrence");

            migrationBuilder.DropColumn(
                name: "HoraFim",
                table: "Occurrence");

            migrationBuilder.DropColumn(
                name: "HoraInicio",
                table: "Occurrence");

            migrationBuilder.DropColumn(
                name: "RelatorioId",
                table: "Occurrence");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "Material");

            migrationBuilder.DropColumn(
                name: "Quantidade",
                table: "Material");

            migrationBuilder.DropColumn(
                name: "RelatorioId",
                table: "Material");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Material");

            migrationBuilder.DropColumn(
                name: "Unidade",
                table: "Material");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Fabricante",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Modelo",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "NumeroSerie",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Funcao",
                table: "EmployeePresence");

            migrationBuilder.DropColumn(
                name: "HorasTrabalhadas",
                table: "EmployeePresence");

            migrationBuilder.DropColumn(
                name: "NomeFuncionario",
                table: "EmployeePresence");

            migrationBuilder.DropColumn(
                name: "RelatorioId",
                table: "EmployeePresence");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Contato",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Empresa",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Funcao",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Companion");

            migrationBuilder.DropColumn(
                name: "Cargo",
                table: "Companion");

            migrationBuilder.DropColumn(
                name: "Contato",
                table: "Companion");

            migrationBuilder.DropColumn(
                name: "Grupo",
                table: "Companion");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "Companion");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Activity");

            migrationBuilder.DropColumn(
                name: "Local",
                table: "Activity");

            migrationBuilder.DropColumn(
                name: "RelatorioId",
                table: "Activity");

            migrationBuilder.RenameTable(
                name: "WeatherDetail",
                newName: "WeatherDetails");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Signature",
                newName: "Signatures");

            migrationBuilder.RenameTable(
                name: "ReportEquipment",
                newName: "ReportEquipments");

            migrationBuilder.RenameTable(
                name: "ReportCompanion",
                newName: "ReportCompanions");

            migrationBuilder.RenameTable(
                name: "Report",
                newName: "Reports");

            migrationBuilder.RenameTable(
                name: "ProjectMember",
                newName: "ProjectMembers");

            migrationBuilder.RenameTable(
                name: "Project",
                newName: "Projects");

            migrationBuilder.RenameTable(
                name: "Photo",
                newName: "Photos");

            migrationBuilder.RenameTable(
                name: "Occurrence",
                newName: "Occurrences");

            migrationBuilder.RenameTable(
                name: "Material",
                newName: "Materials");

            migrationBuilder.RenameTable(
                name: "Equipment",
                newName: "Equipments");

            migrationBuilder.RenameTable(
                name: "EmployeePresence",
                newName: "EmployeePresences");

            migrationBuilder.RenameTable(
                name: "Employee",
                newName: "Employees");

            migrationBuilder.RenameTable(
                name: "Companion",
                newName: "Companions");

            migrationBuilder.RenameTable(
                name: "Activity",
                newName: "Activities");

            migrationBuilder.RenameIndex(
                name: "IX_WeatherDetail_ReportId",
                table: "WeatherDetails",
                newName: "IX_WeatherDetails_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_Signature_ReportId",
                table: "Signatures",
                newName: "IX_Signatures_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_ReportEquipment_ReportId",
                table: "ReportEquipments",
                newName: "IX_ReportEquipments_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_ReportEquipment_EquipmentId",
                table: "ReportEquipments",
                newName: "IX_ReportEquipments_EquipmentId");

            migrationBuilder.RenameIndex(
                name: "IX_ReportCompanion_ReportId",
                table: "ReportCompanions",
                newName: "IX_ReportCompanions_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_ReportCompanion_CompanionId",
                table: "ReportCompanions",
                newName: "IX_ReportCompanions_CompanionId");

            migrationBuilder.RenameIndex(
                name: "IX_Report_UserId",
                table: "Reports",
                newName: "IX_Reports_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Report_ProjectId",
                table: "Reports",
                newName: "IX_Reports_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Report_CompanionId",
                table: "Reports",
                newName: "IX_Reports_CompanionId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectMember_UserId",
                table: "ProjectMembers",
                newName: "IX_ProjectMembers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectMember_ProjectId",
                table: "ProjectMembers",
                newName: "IX_ProjectMembers_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Photo_ReportId",
                table: "Photos",
                newName: "IX_Photos_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_Occurrence_ReportId",
                table: "Occurrences",
                newName: "IX_Occurrences_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_Material_ReportId",
                table: "Materials",
                newName: "IX_Materials_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_EmployeePresence_ReportId",
                table: "EmployeePresences",
                newName: "IX_EmployeePresences_ReportId");

            migrationBuilder.RenameIndex(
                name: "IX_Activity_ReportId",
                table: "Activities",
                newName: "IX_Activities_ReportId");

            migrationBuilder.AlterColumn<string>(
                name: "CPF",
                table: "Signatures",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeatherDetails",
                table: "WeatherDetails",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Signatures",
                table: "Signatures",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReportEquipments",
                table: "ReportEquipments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReportCompanions",
                table: "ReportCompanions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reports",
                table: "Reports",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectMembers",
                table: "ProjectMembers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Projects",
                table: "Projects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Photos",
                table: "Photos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Occurrences",
                table: "Occurrences",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Materials",
                table: "Materials",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Equipments",
                table: "Equipments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmployeePresences",
                table: "EmployeePresences",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Employees",
                table: "Employees",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Companions",
                table: "Companions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Activities",
                table: "Activities",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Reports_ReportId",
                table: "Activities",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeePresences_Reports_ReportId",
                table: "EmployeePresences",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Reports_ReportId",
                table: "Materials",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Occurrences_Reports_ReportId",
                table: "Occurrences",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Reports_ReportId",
                table: "Photos",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_Projects_ProjectId",
                table: "ProjectMembers",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_Users_UserId",
                table: "ProjectMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportCompanions_Companions_CompanionId",
                table: "ReportCompanions",
                column: "CompanionId",
                principalTable: "Companions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportCompanions_Reports_ReportId",
                table: "ReportCompanions",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportEquipments_Equipments_EquipmentId",
                table: "ReportEquipments",
                column: "EquipmentId",
                principalTable: "Equipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportEquipments_Reports_ReportId",
                table: "ReportEquipments",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Companions_CompanionId",
                table: "Reports",
                column: "CompanionId",
                principalTable: "Companions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Projects_ProjectId",
                table: "Reports",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_UserId",
                table: "Reports",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Signatures_Reports_ReportId",
                table: "Signatures",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WeatherDetails_Reports_ReportId",
                table: "WeatherDetails",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
