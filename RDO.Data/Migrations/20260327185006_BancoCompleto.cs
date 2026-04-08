using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDO.Data.Migrations
{
    /// <inheritdoc />
    public partial class BancoCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Acompanhantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Cargo = table.Column<string>(type: "TEXT", nullable: false),
                    Grupo = table.Column<string>(type: "TEXT", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Acompanhantes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipamentosCadastrados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Categoria = table.Column<string>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", nullable: false),
                    NumeroSerie = table.Column<string>(type: "TEXT", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipamentosCadastrados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Funcionarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Funcao = table.Column<string>(type: "TEXT", nullable: false),
                    Empresa = table.Column<string>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Contato = table.Column<string>(type: "TEXT", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funcionarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Obras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Endereco = table.Column<string>(type: "TEXT", nullable: false),
                    ART = table.Column<string>(type: "TEXT", nullable: false),
                    Grupo = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Responsavel = table.Column<string>(type: "TEXT", nullable: false),
                    TipoContrato = table.Column<string>(type: "TEXT", nullable: false),
                    Contratante = table.Column<string>(type: "TEXT", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PrevisaoTermino = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ImagemPath = table.Column<string>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Obras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    SenhaHash = table.Column<string>(type: "TEXT", nullable: false),
                    Perfil = table.Column<string>(type: "TEXT", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MembrosObra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObraId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Papel = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembrosObra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembrosObra_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MembrosObra_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Relatorios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObraId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    AcompanhanteId = table.Column<int>(type: "INTEGER", nullable: true),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HoraEntrada = table.Column<string>(type: "TEXT", nullable: false),
                    HoraSaida = table.Column<string>(type: "TEXT", nullable: false),
                    HoraIntervalo = table.Column<string>(type: "TEXT", nullable: false),
                    ObsGerais = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Sincronizado = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relatorios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relatorios_Acompanhantes_AcompanhanteId",
                        column: x => x.AcompanhanteId,
                        principalTable: "Acompanhantes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Relatorios_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Relatorios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assinaturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RelatorioId = table.Column<int>(type: "INTEGER", nullable: false),
                    NomeAssinante = table.Column<string>(type: "TEXT", nullable: false),
                    Cargo = table.Column<string>(type: "TEXT", nullable: false),
                    CPF = table.Column<string>(type: "TEXT", nullable: false),
                    DataAssinatura = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Assinado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assinaturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assinaturas_Relatorios_RelatorioId",
                        column: x => x.RelatorioId,
                        principalTable: "Relatorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Atividades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RelatorioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", nullable: false),
                    Local = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atividades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Atividades_Relatorios_RelatorioId",
                        column: x => x.RelatorioId,
                        principalTable: "Relatorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Climas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RelatorioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Periodo = table.Column<string>(type: "TEXT", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tempo = table.Column<string>(type: "TEXT", nullable: false),
                    Condicao = table.Column<string>(type: "TEXT", nullable: false),
                    IndicePluviometrico = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Climas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Climas_Relatorios_RelatorioId",
                        column: x => x.RelatorioId,
                        principalTable: "Relatorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RelatorioId = table.Column<int>(type: "INTEGER", nullable: false),
                    CaminhoArquivo = table.Column<string>(type: "TEXT", nullable: false),
                    Legenda = table.Column<string>(type: "TEXT", nullable: false),
                    AtividadeRelacionada = table.Column<string>(type: "TEXT", nullable: false),
                    TiradaEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fotos_Relatorios_RelatorioId",
                        column: x => x.RelatorioId,
                        principalTable: "Relatorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Materiais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RelatorioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Quantidade = table.Column<string>(type: "TEXT", nullable: false),
                    Unidade = table.Column<string>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materiais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Materiais_Relatorios_RelatorioId",
                        column: x => x.RelatorioId,
                        principalTable: "Relatorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ocorrencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RelatorioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    HoraInicio = table.Column<string>(type: "TEXT", nullable: false),
                    HoraFim = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ocorrencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ocorrencias_Relatorios_RelatorioId",
                        column: x => x.RelatorioId,
                        principalTable: "Relatorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_RelatorioId",
                table: "Assinaturas",
                column: "RelatorioId");

            migrationBuilder.CreateIndex(
                name: "IX_Atividades_RelatorioId",
                table: "Atividades",
                column: "RelatorioId");

            migrationBuilder.CreateIndex(
                name: "IX_Climas_RelatorioId",
                table: "Climas",
                column: "RelatorioId");

            migrationBuilder.CreateIndex(
                name: "IX_Fotos_RelatorioId",
                table: "Fotos",
                column: "RelatorioId");

            migrationBuilder.CreateIndex(
                name: "IX_Materiais_RelatorioId",
                table: "Materiais",
                column: "RelatorioId");

            migrationBuilder.CreateIndex(
                name: "IX_MembrosObra_ObraId",
                table: "MembrosObra",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_MembrosObra_UsuarioId",
                table: "MembrosObra",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_RelatorioId",
                table: "Ocorrencias",
                column: "RelatorioId");

            migrationBuilder.CreateIndex(
                name: "IX_Relatorios_AcompanhanteId",
                table: "Relatorios",
                column: "AcompanhanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Relatorios_ObraId",
                table: "Relatorios",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Relatorios_UsuarioId",
                table: "Relatorios",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assinaturas");

            migrationBuilder.DropTable(
                name: "Atividades");

            migrationBuilder.DropTable(
                name: "Climas");

            migrationBuilder.DropTable(
                name: "EquipamentosCadastrados");

            migrationBuilder.DropTable(
                name: "Fotos");

            migrationBuilder.DropTable(
                name: "Funcionarios");

            migrationBuilder.DropTable(
                name: "Materiais");

            migrationBuilder.DropTable(
                name: "MembrosObra");

            migrationBuilder.DropTable(
                name: "Ocorrencias");

            migrationBuilder.DropTable(
                name: "Relatorios");

            migrationBuilder.DropTable(
                name: "Acompanhantes");

            migrationBuilder.DropTable(
                name: "Obras");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
