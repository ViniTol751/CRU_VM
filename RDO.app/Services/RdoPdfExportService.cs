using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RDO.Data.Data;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace RDO.App.Services
{
    public static class RdoPdfExportService
    {
        static RdoPdfExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static Task<string?> ExportAsync(int relatorioId)
            => Task.Run(() => Export(relatorioId));

        private static string? Export(int relatorioId)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());

            var rel = db.Relatorios
                .Include(r => r.Project)
                .Include(r => r.WeatherDetails)
                .Include(r => r.Activities)
                .Include(r => r.Occurrences)
                .Include(r => r.Photos)
                .Include(r => r.Companion)
                .Include(r => r.Signatures)
                .Include(r => r.Equipments).ThenInclude(re => re.Equipment)
                .Include(r => r.ReportCompanions).ThenInclude(ra => ra.Companion!)
                .FirstOrDefault(r => r.Id == relatorioId);

            if (rel == null) return null;

            var pasta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
            Directory.CreateDirectory(pasta);

            var nomeSeguro = Regex.Replace(rel.Obra.Nome, @"[\\/:*?""<>|]", "_").Trim();
            var nomeArquivo = $"{rel.Data:yy-MM-dd}_{nomeSeguro}.pdf";
            var caminho = Path.Combine(pasta, nomeArquivo);

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(9).FontColor("#1a2a3a"));

                    page.Header().Element(c => DesenharCabecalho(c, rel));
                    page.Content().PaddingTop(12).Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Element(c => SecaoIdentificacao(c, rel));
                        col.Item().Element(c => SecaoHorarioTrabalho(c, rel));
                        col.Item().Element(c => SecaoClima(c, rel));
                        // Acompanhantes: prioriza join-table; fallback para campo legado
                        var acomps = rel.RelatorioAcompanhantes.Select(ra => ra.Acompanhante).ToList();
                        if (acomps.Count == 0 && rel.Acompanhante != null)
                            acomps.Add(rel.Acompanhante);
                        if (acomps.Count > 0)
                            col.Item().Element(c => SecaoAcompanhantes(c, acomps));
                        col.Item().Element(c => SecaoEquipe(c, rel));
                        col.Item().Element(c => SecaoEquipamentos(c, rel));
                        col.Item().Element(c => SecaoAtividades(c, rel));
                        col.Item().Element(c => SecaoOcorrencias(c, rel));
                        col.Item().Element(c => SecaoComentarios(c, rel));
                        col.Item().Element(c => SecaoFotos(c, rel));
                        col.Item().Element(c => SecaoVideos(c, rel));
                        col.Item().Element(c => SecaoAnexos(c, rel));
                    });
                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Focus Engenharia Elétrica  |  Página ").FontSize(8).FontColor("#6080a0");
                        t.CurrentPageNumber().FontSize(8).FontColor("#6080a0");
                        t.Span(" de ").FontSize(8).FontColor("#6080a0");
                        t.TotalPages().FontSize(8).FontColor("#6080a0");
                    });
                });
            }).GeneratePdf();

            // Tenta sobrescrever; se o arquivo estiver aberto/bloqueado, salva com sufixo de hora
            try
            {
                File.WriteAllBytes(caminho, pdfBytes);
            }
            catch (IOException)
            {
                var altNome = $"{rel.Data:yy-MM-dd}_{nomeSeguro}_{DateTime.Now:HHmmss}.pdf";
                caminho = Path.Combine(pasta, altNome);
                File.WriteAllBytes(caminho, pdfBytes);
            }

            return caminho;
        }

        // ── CABEÇALHO ─────────────────────────────────────────────────────────
        private static void DesenharCabecalho(IContainer c, Data.Models.Report rel)
        {
            c.BorderBottom(1).BorderColor("#0052cc").PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("FOCUS ENGENHARIA ELÉTRICA")
                        .FontSize(16).Bold().FontColor("#0052cc");
                    col.Item().Text("RELATÓRIO DIÁRIO DE OBRA — RDO")
                        .FontSize(10).FontColor("#5a6f8a");
                    col.Item().Text($"{rel.Data:dddd}, {rel.Data:dd/MM/yyyy}")
                        .FontSize(9).FontColor("#1a2a3a");
                });
                row.ConstantItem(160).AlignRight().Column(col =>
                {
                    col.Item().Text($"Nº {rel.Numero:D3}").FontSize(20).Bold().FontColor("#0052cc").AlignRight();
                    col.Item().Text(rel.Data.ToString("dd/MM/yyyy")).FontSize(11).FontColor("#1a2a3a").AlignRight();
                    if (rel.Status == "Publicado")
                        col.Item().AlignRight().Text(t =>
                            t.Span(" ✓  PUBLICADO ").FontSize(9).Bold()
                             .FontColor("#00dd77").BackgroundColor("#0a3a1a"));
                    else
                        col.Item().Text(rel.Status).FontSize(9).FontColor("#5a6f8a").AlignRight();
                });
            });
        }

        // ── IDENTIFICAÇÃO DA OBRA ─────────────────────────────────────────────
        private static void SecaoIdentificacao(IContainer c, Data.Models.Report rel)
        {
            c.Column(col =>
            {
                col.Item().Element(CabecalhoSecao("IDENTIFICAÇÃO DA OBRA"));
                col.Item().Border(1).BorderColor("#d0dce8").Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(120);
                        cols.RelativeColumn();
                        cols.ConstantColumn(120);
                        cols.RelativeColumn();
                    });

                    void Linha(string l1, string v1, string l2, string v2)
                    {
                        table.Cell().Background("#f0f4f8").Padding(5).Text(l1).Bold().FontColor("#0052cc");
                        table.Cell().Padding(5).Text(v1);
                        table.Cell().Background("#f0f4f8").Padding(5).Text(l2).Bold().FontColor("#0052cc");
                        table.Cell().Padding(5).Text(v2);
                    }

                    Linha("Obra:", rel.Obra.Nome, "Grupo/Cliente:", rel.Obra.Grupo);
                    Linha("Local:", rel.Obra.Endereco, "Contrato:", rel.Obra.TipoContrato);
                    Linha("Contratante:", rel.Obra.Contratante, "Responsável:", rel.Obra.Responsavel);
                    Linha("ART/RRT:", rel.Obra.ART, "Status:", rel.Status);
                });
            });
        }

        // ── HORÁRIO DE TRABALHO ───────────────────────────────────────────────
        private static void SecaoHorarioTrabalho(IContainer c, Data.Models.Report rel)
        {
            var assinaturas = rel.Assinaturas.ToList();
            var primeiraEntrada = assinaturas
                .Select(a => TimeSpan.TryParse(a.HoraEntrada, out var t) ? (TimeSpan?)t : null)
                .Where(t => t.HasValue)
                .Select(t => t!.Value)
                .DefaultIfEmpty()
                .Min();
            var ultimaSaida = assinaturas
                .Select(a => TimeSpan.TryParse(a.HoraSaida, out var t) ? (TimeSpan?)t : null)
                .Where(t => t.HasValue)
                .Select(t => t!.Value)
                .DefaultIfEmpty()
                .Max();
            var mediaIntervalo = assinaturas
                .Select(a => TimeSpan.TryParse(a.HoraIntervalo, out var t) ? (TimeSpan?)t : null)
                .Where(t => t.HasValue)
                .Select(t => t!.Value)
                .DefaultIfEmpty(TimeSpan.FromHours(1))
                .Average(t => t.TotalMinutes);
            var totalHoras = assinaturas.Sum(a =>
            {
                TimeSpan.TryParse(a.HoraEntrada, out var entrada);
                TimeSpan.TryParse(a.HoraSaida, out var saida);
                TimeSpan.TryParse(a.HoraIntervalo, out var intervalo);
                var total = saida - entrada - intervalo;
                return total.TotalMinutes > 0 ? total.TotalHours : 0;
            });

            c.Column(col =>
            {
                col.Item().Element(CabecalhoSecao("HORÁRIO DE TRABALHO"));
                col.Item().Border(1).BorderColor("#d0dce8").Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(130);
                        cols.RelativeColumn();
                        cols.ConstantColumn(130);
                        cols.RelativeColumn();
                    });

                    void Linha(string l1, string v1, string l2, string v2)
                    {
                        table.Cell().Background("#f0f4f8").Padding(5).Text(l1).Bold().FontColor("#0052cc");
                        table.Cell().Padding(5).Text(v1);
                        table.Cell().Background("#f0f4f8").Padding(5).Text(l2).Bold().FontColor("#0052cc");
                        table.Cell().Padding(5).Text(v2);
                    }

                    Linha("Entrada:", primeiraEntrada == default ? "—" : primeiraEntrada.ToString(@"hh\:mm"),
                        "Saída:", ultimaSaida == default ? "—" : ultimaSaida.ToString(@"hh\:mm"));
                    Linha("Intervalo:", TimeSpan.FromMinutes(mediaIntervalo).ToString(@"hh\:mm"),
                        "Total horas:", $"{totalHoras:0.00} h");
                });
            });
        }

        // ── CONDIÇÕES CLIMÁTICAS ──────────────────────────────────────────────
        private static void SecaoClima(IContainer c, Data.Models.Report rel)
        {
            var manha = rel.Climas.FirstOrDefault(x => x.Periodo == "Manhã");
            var tarde = rel.Climas.FirstOrDefault(x => x.Periodo == "Tarde");
            static string IconeTempo(string? tempo) => tempo switch
            {
                "Ensolarado" => "☀",
                "Nublado" => "☁",
                "Chuvoso" => "☂",
                _ => "•"
            };

            c.Column(col =>
            {
                col.Item().Element(CabecalhoSecao("CONDIÇÕES CLIMÁTICAS"));
                col.Item().Border(1).BorderColor("#d0dce8").Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(80);
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    table.Cell().Background("#0052cc").Padding(5).AlignCenter().Text("PERÍODO").FontColor("#FFFFFF").Bold();
                    table.Cell().Background("#0052cc").Padding(5).AlignCenter().Text("TEMPO").FontColor("#FFFFFF").Bold();
                    table.Cell().Background("#0052cc").Padding(5).AlignCenter().Text("CONDIÇÃO").FontColor("#FFFFFF").Bold();

                    void LinhaClima(string periodo, Data.Models.WeatherDetail? clima)
                    {
                        table.Cell().Background("#f0f4f8").Padding(5).AlignCenter().Text(periodo).Bold();
                        table.Cell().Padding(5).AlignCenter().Text($"{IconeTempo(clima?.Tempo)} {clima?.Tempo ?? "—"}").FontSize(10);
                        table.Cell().Padding(5).AlignCenter().Text(clima?.Condicao ?? "—")
                            .FontColor(clima?.Condicao == "Impraticável" ? "#cc0000" : "#008800");
                    }

                    LinhaClima("Manhã", manha);
                    LinhaClima("Tarde", tarde);
                });
            });
        }

        // ── ACOMPANHANTES TÉCNICOS ────────────────────────────────────────────
        private static void SecaoAcompanhantes(IContainer c, System.Collections.Generic.List<Data.Models.Companion> acomps)
        {
            c.Column(col =>
            {
                col.Item().Element(CabecalhoSecao($"ACOMPANHANTES TÉCNICOS ({acomps.Count})"));
                col.Item().Border(1).BorderColor("#d0dce8").Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });
                    table.Cell().Background("#0052cc").Padding(5).Text("NOME").FontColor("#FFFFFF").Bold();
                    table.Cell().Background("#0052cc").Padding(5).Text("CARGO").FontColor("#FFFFFF").Bold();
                    table.Cell().Background("#0052cc").Padding(5).Text("GRUPO / CLIENTE").FontColor("#FFFFFF").Bold();
                    table.Cell().Background("#0052cc").Padding(5).Text("CONTATO").FontColor("#FFFFFF").Bold();
                    for (int i = 0; i < acomps.Count; i++)
                    {
                        var bg = i % 2 == 0 ? "#FFFFFF" : "#f0f4f8";
                        var ac = acomps[i];
                        table.Cell().Background(bg).Padding(5).Text(ac.Nome);
                        table.Cell().Background(bg).Padding(5).Text(ac.Cargo);
                        table.Cell().Background(bg).Padding(5).Text(ac.Grupo);
                        table.Cell().Background(bg).Padding(5).Text(string.IsNullOrEmpty(ac.Contato) ? "—" : ac.Contato);
                    }
                });
            });
        }

        // ── EQUIPAMENTOS UTILIZADOS ───────────────────────────────────────────
        private static void SecaoEquipamentos(IContainer c, Data.Models.Report rel)
        {
            var equipamentos = rel.Equipamentos.ToList();
            c.Column(col =>
            {
                col.Item().Element(CabecalhoSecao($"EQUIPAMENTOS UTILIZADOS ({equipamentos.Count})"));
                col.Item().Border(1).BorderColor("#d0dce8").Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(90);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                    });
                    table.Cell().Background("#0052cc").Padding(5).Text("PATRIMÔNIO").FontColor("#FFFFFF").Bold().FontSize(8);
                    table.Cell().Background("#0052cc").Padding(5).Text("EQUIPAMENTO").FontColor("#FFFFFF").Bold();
                    table.Cell().Background("#0052cc").Padding(5).Text("FABRICANTE / MODELO").FontColor("#FFFFFF").Bold();
                    for (int i = 0; i < equipamentos.Count; i++)
                    {
                        var bg = i % 2 == 0 ? "#FFFFFF" : "#f0f4f8";
                        var eq = equipamentos[i].EquipamentoCadastrado;
                        table.Cell().Background(bg).Padding(5).Text(eq.NumeroSerie).Bold().FontColor("#0052cc");
                        table.Cell().Background(bg).Padding(5).Text(eq.Nome);
                        table.Cell().Background(bg).Padding(5).Text($"{eq.Fabricante} — {eq.Modelo}").FontColor("#5a6f8a");
                    }
                });
            });
        }

        // ── EQUIPE ────────────────────────────────────────────────────────────
        private static void SecaoEquipe(IContainer c, Data.Models.Report rel)
        {
            var assinaturas = rel.Assinaturas.ToList();
            var count = assinaturas.Count;
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var employeeIds = assinaturas.Where(a => a.FuncionarioId.HasValue).Select(a => a.FuncionarioId!.Value).Distinct().ToList();
            var employeeMap = db.Funcionarios.Where(f => employeeIds.Contains(f.Id))
                .ToDictionary(f => f.Id, f => f);
            c.Column(col =>
            {
                col.Item().Element(CabecalhoSecao($"EQUIPE DE CAMPO ({count})"));
                if (count == 0)
                {
                    col.Item().Border(1).BorderColor("#d0dce8").Padding(8)
                        .Text("Nenhum membro registrado.").FontColor("#6080a0").Italic();
                    return;
                }
                col.Item().Border(1).BorderColor("#d0dce8").Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.ConstantColumn(50);
                        cols.ConstantColumn(50);
                        cols.ConstantColumn(55);
                        cols.ConstantColumn(55);
                    });

                    table.Cell().Background("#0052cc").Padding(5).Text("NOME").FontColor("#FFFFFF").Bold();
                    table.Cell().Background("#0052cc").Padding(5).Text("FUNÇÃO / CARGO").FontColor("#FFFFFF").Bold();
                    table.Cell().Background("#0052cc").Padding(5).Text("EMPRESA / TIPO").FontColor("#FFFFFF").Bold();
                    table.Cell().Background("#0052cc").Padding(5).AlignCenter().Text("ENTRADA").FontColor("#FFFFFF").Bold().FontSize(8);
                    table.Cell().Background("#0052cc").Padding(5).AlignCenter().Text("SAÍDA").FontColor("#FFFFFF").Bold().FontSize(8);
                    table.Cell().Background("#0052cc").Padding(5).AlignCenter().Text("INTERV.").FontColor("#FFFFFF").Bold().FontSize(8);
                    table.Cell().Background("#0052cc").Padding(5).AlignCenter().Text("HS TRAB.").FontColor("#FFFFFF").Bold().FontSize(8);

                    for (int i = 0; i < assinaturas.Count; i++)
                    {
                        var bg = i % 2 == 0 ? "#FFFFFF" : "#f0f4f8";
                        var a = assinaturas[i];
                        TimeSpan.TryParse(a.HoraEntrada, out var entrada);
                        TimeSpan.TryParse(a.HoraSaida, out var saida);
                        TimeSpan.TryParse(a.HoraIntervalo, out var intervalo);
                        var trabalhado = saida - entrada - intervalo;
                        var hsTrab = trabalhado.TotalMinutes > 0
                            ? $"{(int)trabalhado.TotalHours:D2}:{trabalhado.Minutes:D2}"
                            : "—";
                        table.Cell().Background(bg).Padding(5).Text(a.NomeAssinante);
                        table.Cell().Background(bg).Padding(5).Text(a.Cargo);
                        var origem = a.FuncionarioId.HasValue && employeeMap.TryGetValue(a.FuncionarioId.Value, out var f)
                            ? $"{f.Empresa} / {f.Tipo}"
                            : "Sem vínculo";
                        table.Cell().Background(bg).Padding(5).Text(origem).FontSize(8).FontColor("#5a6f8a");
                        table.Cell().Background(bg).Padding(5).AlignCenter().Text(a.HoraEntrada).FontSize(8);
                        table.Cell().Background(bg).Padding(5).AlignCenter().Text(a.HoraSaida).FontSize(8);
                        table.Cell().Background(bg).Padding(5).AlignCenter().Text(string.IsNullOrEmpty(a.HoraIntervalo) ? "—" : a.HoraIntervalo).FontSize(8);
                        table.Cell().Background(bg).Padding(5).AlignCenter().Text(hsTrab).FontSize(8).Bold().FontColor("#0052cc");
                    }
                });
                col.Item().PaddingTop(4).Text($"Total equipe: {count}  |  Faltas: 0").FontSize(8).FontColor("#5a6f8a");
            });
        }

        // ── ATIVIDADES ────────────────────────────────────────────────────────
        private static void SecaoAtividades(IContainer c, Data.Models.Report rel)
        {
            c.Column(col =>
            {
                col.Item().Element(CabecalhoSecao($"ATIVIDADES REALIZADAS ({rel.Atividades.Count})"));
                if (!rel.Atividades.Any())
                {
                    col.Item().Border(1).BorderColor("#d0dce8").Padding(8)
                        .Text("Nenhuma atividade registrada.").FontColor("#6080a0").Italic();
                    return;
                }
                col.Item().Border(1).BorderColor("#d0dce8").Column(inner =>
                {
                    var atividades = rel.Atividades.ToList();
                    for (int i = 0; i < atividades.Count; i++)
                    {
                        var bg = i % 2 == 0 ? "#FFFFFF" : "#f0f4f8";
                        inner.Item().Background(bg).Padding(6).Row(row =>
                        {
                            row.ConstantItem(20).Text($"{i + 1}.").Bold().FontColor("#0052cc");
                            row.RelativeItem().Text(atividades[i].Descricao);
                        });
                    }
                });
            });
        }

        // ── OCORRÊNCIAS ───────────────────────────────────────────────────────
        private static void SecaoOcorrencias(IContainer c, Data.Models.Report rel)
        {
            c.Column(col =>
            {
                col.Item().Element(CabecalhoSecao($"OCORRÊNCIAS / OBSERVAÇÕES ({rel.Ocorrencias.Count})"));
                if (!rel.Ocorrencias.Any())
                {
                    col.Item().Border(1).BorderColor("#d0dce8").Padding(8)
                        .Text("Nenhuma ocorrência registrada.").FontColor("#6080a0").Italic();
                    return;
                }
                col.Item().Border(1).BorderColor("#d0dce8").Column(inner =>
                {
                    var ocorrencias = rel.Ocorrencias.ToList();
                    for (int i = 0; i < ocorrencias.Count; i++)
                    {
                        var bg = i % 2 == 0 ? "#FFFFFF" : "#f0f4f8";
                        inner.Item().Background(bg).Padding(6).Row(row =>
                        {
                            row.ConstantItem(20).Text($"{i + 1}.").Bold().FontColor("#0052cc");
                            row.RelativeItem().Column(ocol =>
                            {
                                ocol.Item().Text(ocorrencias[i].Descricao);
                                if (!string.IsNullOrEmpty(ocorrencias[i].HoraInicio))
                                    ocol.Item().Text($"⏱ {ocorrencias[i].HoraInicio} – {ocorrencias[i].HoraFim}")
                                        .FontSize(8).FontColor("#6080a0");
                                if (!string.IsNullOrWhiteSpace(ocorrencias[i].Tags))
                                    ocol.Item().Text($"Tags: {ocorrencias[i].Tags}")
                                        .FontSize(8).FontColor("#0052cc");
                            });
                        });
                    }
                });
            });
        }

        private static void SecaoComentarios(IContainer c, Data.Models.Report rel)
        {
            c.Column(col =>
            {
                col.Item().Element(CabecalhoSecao("COMENTÁRIOS"));
                col.Item().Border(1).BorderColor("#d0dce8").Padding(8).Column(inner =>
                {
                    if (string.IsNullOrWhiteSpace(rel.ObsGerais))
                    {
                        inner.Item().Text("Sem dados").FontColor("#6080a0").Italic();
                        return;
                    }

                    inner.Item().Text($"Autor: Sistema").FontColor("#5a6f8a").FontSize(8);
                    inner.Item().Text($"Data/Hora: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor("#5a6f8a").FontSize(8);
                    inner.Item().PaddingTop(4).Text(rel.ObsGerais);
                });
            });
        }

        // ── FOTOS ─────────────────────────────────────────────────────────────
        private static void SecaoFotos(IContainer c, Data.Models.Report rel)
        {
            var fotos = rel.Fotos.Where(f => File.Exists(f.CaminhoArquivo)).ToList();

            c.Column(col =>
            {
                col.Item().Element(CabecalhoSecao($"REGISTRO FOTOGRÁFICO ({fotos.Count})"));
                if (!fotos.Any())
                {
                    col.Item().Border(1).BorderColor("#d0dce8").Padding(8)
                        .Text("Sem dados").FontColor("#6080a0").Italic();
                    return;
                }
                col.Item().Border(1).BorderColor("#d0dce8").Padding(8).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    foreach (var foto in fotos)
                    {
                        try
                        {
                            var imageBytes = CropCenterToStandard(foto.CaminhoArquivo);
                            table.Cell().Padding(4).Column(inner =>
                            {
                                inner.Item().Height(5, Unit.Centimetre).Image(imageBytes).FitArea();
                                if (!string.IsNullOrEmpty(foto.Legenda))
                                    inner.Item().PaddingTop(2).Text(foto.Legenda)
                                        .FontSize(8).FontColor("#5a6f8a").AlignCenter();
                            });
                        }
                        catch
                        {
                            table.Cell().Padding(4).Height(5, Unit.Centimetre)
                                .Background("#f0f4f8").AlignCenter()
                                .Text("Foto indisponível").FontColor("#aabbcc");
                        }
                    }

                    // Pad to even number of cells
                    if (fotos.Count % 2 != 0)
                        table.Cell().Padding(4);
                });
            });
        }

        private static void SecaoVideos(IContainer c, Data.Models.Report rel)
        {
            c.Column(col =>
            {
                col.Item().Element(CabecalhoSecao("VÍDEOS"));
                col.Item().Border(1).BorderColor("#d0dce8").Padding(8)
                    .Text("Sem dados").FontColor("#6080a0").Italic();
            });
        }

        private static void SecaoAnexos(IContainer c, Data.Models.Report rel)
        {
            c.Column(col =>
            {
                col.Item().Element(CabecalhoSecao("ANEXOS"));
                col.Item().Border(1).BorderColor("#d0dce8").Padding(8)
                    .Text("Sem dados").FontColor("#6080a0").Italic();
            });
        }

        // ── IMAGE PROCESSING ─────────────────────────────────────────────────
        private static byte[] CropCenterToStandard(string imagePath, int targetW = 800, int targetH = 600)
        {
            using var src = System.Drawing.Image.FromFile(imagePath);
            double srcRatio = (double)src.Width / src.Height;
            double tgtRatio = (double)targetW / targetH;

            int cropW, cropH, offsetX, offsetY;
            if (srcRatio > tgtRatio)
            {
                cropH = src.Height;
                cropW = (int)(src.Height * tgtRatio);
                offsetX = (src.Width - cropW) / 2;
                offsetY = 0;
            }
            else
            {
                cropW = src.Width;
                cropH = (int)(src.Width / tgtRatio);
                offsetX = 0;
                offsetY = (src.Height - cropH) / 2;
            }

            using var bmp = new System.Drawing.Bitmap(targetW, targetH);
            using var g = System.Drawing.Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(src,
                new System.Drawing.Rectangle(0, 0, targetW, targetH),
                new System.Drawing.Rectangle(offsetX, offsetY, cropW, cropH),
                System.Drawing.GraphicsUnit.Pixel);

            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return ms.ToArray();
        }

        // ── HELPER: cabeçalho de seção ────────────────────────────────────────
        private static Action<IContainer> CabecalhoSecao(string titulo)
            => c => c.Background("#1a2a4a").Padding(6).Text(titulo)
                     .FontSize(9).Bold().FontColor("#FFFFFF");
    }
}






