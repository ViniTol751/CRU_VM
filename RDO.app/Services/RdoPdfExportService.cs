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
        // Paleta PROFISSIONAL EM TONS DE CINZA para relatórios de obra
        private const string CorPreta = "#000000";
        private const string CorTexto = "#212529";  // Cinza muito escuro (quase preto)
        private const string CorLabelCinza = "#495057";  // Cinza médio escuro para labels
        private const string CorBorda = "#343a40";  // Cinza escuro para bordas principais
        private const string CorBordaInterna = "#dee2e6";  // Cinza claro para divisórias internas
        private const string CorCabecalhoSec = "#e9ecef";  // Cinza claro para cabeçalhos
        private const string CorLinhaAltern = "#f8f9fa";  // Cinza muito claro para linhas alternadas
        private const string CorBranco = "#FFFFFF";
        private const string CorAprovado = "#28a745";  // Verde profissional
        private const string CorRascunho = "#6c757d";  // Cinza médio
        private const string CorVermelho = "#dc3545";  // Vermelho profissional
        private const string CorAmarelo = "#ffc107";  // Amarelo
        private const string CorDestaque = "#007bff";  // Azul para destaques

        // Mapeamento de responsáveis Focus para CREA
        private static readonly Dictionary<string, string> CreaMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Bruno Pires",          "5063435630" },
            { "Felipe Prado",         "5063687510" },
            { "Juliana Bertoni",      "5063687927" },
            { "Maicon Salomão",       "5070334847" },
            { "Murilo Franco",        "5068975820" },
            { "Wellington Bortolozo", "5070173544" },
            { "Wesley Gregório",      "5070948640" }
        };

        private static string ObterCrea(string? responsavelFocus)
        {
            if (string.IsNullOrWhiteSpace(responsavelFocus))
                return "—";

            return CreaMap.TryGetValue(responsavelFocus.Trim(), out var crea) ? crea : "—";
        }

        static RdoPdfExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static async Task<string?> ExportAsync(int relatorioId)
        {
            // Resolve client logo path before entering Task.Run so WinRT async is available
            string? clientLogoPath = null;
            using (var db = new RdoDbContext(DbContextHelper.GetOptions()))
            {
                var relLight = db.Relatorios
                    .Include(r => r.Project)
                    .FirstOrDefault(r => r.Id == relatorioId);
                var obra = relLight?.Obra;
                if (obra != null)
                {
                    var cfg = LogosConfig.Load();
                    RDO.Data.Models.Empresa? empresa = null;
                    // Lookup direto por EmpresaId; fallback por Grupo para obras antigas
                    if (obra.EmpresaId.HasValue)
                        empresa = db.Empresas.Find(obra.EmpresaId.Value);
                    if (empresa == null && !string.IsNullOrEmpty(obra.Grupo))
                        empresa = db.Empresas.Where(e => e.IsActive).AsEnumerable()
                            .FirstOrDefault(e => LogoService.GetBaseNome(e.Nome)
                                .Equals(obra.Grupo, StringComparison.OrdinalIgnoreCase));
                    if (empresa != null)
                        clientLogoPath = LogoService.ResolveLogoUrl(cfg, empresa.ImagemPath, empresa.Nome);
                }
            }

            // Flatten logos to white via WinRT (guaranteed to work in MSIX — no System.Drawing)
            var focusLogoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "focus_logo.png");
            var flatFocus = await LogoService.FlattenToWhiteAsync(focusLogoPath);
            var flatClient = await LogoService.FlattenToWhiteAsync(clientLogoPath);

            byte[]? focusBytes = flatFocus != null && File.Exists(flatFocus) ? File.ReadAllBytes(flatFocus) : null;
            byte[]? clientBytes = flatClient != null && File.Exists(flatClient) ? File.ReadAllBytes(flatClient) : null;

            return await Task.Run(() => Export(relatorioId, focusBytes, clientBytes));
        }

        private static string? Export(int relatorioId, byte[]? focusBytes, byte[]? clientBytes)
        {
            try
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

                if (rel == null || rel.Obra == null) return null;

                var pasta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");
                Directory.CreateDirectory(pasta);

                var nomeSeguro = Regex.Replace(rel.Obra.Nome, @"[\\/:*?""<>|]", "_").Trim();
                var nomeArquivo = $"{rel.Data:yy-MM-dd}_{nomeSeguro}.pdf";
                var caminho = Path.Combine(pasta, nomeArquivo);

                var fotos = rel.Fotos.Where(f => f.Type != "document").ToList();
                var docs = rel.Fotos.Where(f => f.Type == "document").ToList();
                var pastaAnexos = docs.Any()
                    ? Path.Combine(pasta, Path.GetFileNameWithoutExtension(nomeArquivo) + "_Anexos")
                    : null;

                var pdfBytes = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(t => t
                            .FontFamily("Segoe UI", "Arial", "Helvetica")
                            .FontSize(9)
                            .FontColor(CorTexto)
                            .LineHeight(1.4f));

                        page.Header().Element(c => DesenharCabecalho(c, rel, focusBytes, clientBytes));
                        page.Content().PaddingTop(10).Column(col =>
                        {
                            try
                            {
                                col.Spacing(6);

                                System.Diagnostics.Debug.WriteLine("[PDF] Renderizando Identificação...");
                                col.Item().Element(c => SecaoIdentificacao(c, rel));

                                System.Diagnostics.Debug.WriteLine("[PDF] Renderizando Clima...");
                                col.Item().Element(c => SecaoClima(c, rel));

                                var acomps = rel.RelatorioAcompanhantes
                                    .Select(ra => ra.Companion).Where(a => a != null).Select(a => a!).ToList();
                                if (acomps.Count == 0 && rel.Companion != null)
                                    acomps.Add(rel.Companion);
                                if (acomps.Count > 0)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[PDF] Renderizando Acompanhantes ({acomps.Count})...");
                                    col.Item().Element(c => SecaoAcompanhantes(c, acomps));
                                }

                                System.Diagnostics.Debug.WriteLine("[PDF] Renderizando Equipe...");
                                col.Item().Element(c => SecaoEquipe(c, rel));

                                if (rel.Equipamentos.Any())
                                {
                                    System.Diagnostics.Debug.WriteLine($"[PDF] Renderizando Equipamentos ({rel.Equipamentos.Count})...");
                                    col.Item().Element(c => SecaoEquipamentos(c, rel));
                                }

                                System.Diagnostics.Debug.WriteLine($"[PDF] Renderizando Atividades ({rel.Activities.Count})...");
                                col.Item().Element(c => SecaoAtividades(c, rel));

                                System.Diagnostics.Debug.WriteLine($"[PDF] Renderizando Ocorrências ({rel.Occurrences.Count})...");
                                col.Item().Element(c => SecaoOcorrencias(c, rel));

                                // Limita a 20 fotos para evitar problemas de layout
                                const int MAX_FOTOS = 20;
                                if (fotos.Count > MAX_FOTOS)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[PDF] Limitando fotos de {fotos.Count} para {MAX_FOTOS}");
                                    col.Item().Border(1).BorderColor("#ff9800").Background("#fff3e0")
                                        .Padding(8).Text($"⚠️ Este relatório tem {fotos.Count} fotos. " +
                                        $"Apenas as primeiras {MAX_FOTOS} serão incluídas no PDF para evitar problemas de layout.")
                                        .FontSize(8).FontColor("#e65100");
                                    fotos = fotos.Take(MAX_FOTOS).ToList();
                                }

                                if (fotos.Any())
                                {
                                    System.Diagnostics.Debug.WriteLine($"[PDF] Renderizando Fotos ({fotos.Count})...");
                                    col.Item().Element(c => SecaoFotos(c, fotos));
                                }

                                if (docs.Any())
                                {
                                    System.Diagnostics.Debug.WriteLine($"[PDF] Renderizando Documentos ({docs.Count})...");
                                    col.Item().Element(c => SecaoDocumentos(c, docs, pastaAnexos));
                                }

                                System.Diagnostics.Debug.WriteLine("[PDF] ✓ Todas as seções renderizadas com sucesso");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[PDF] ✗ Erro ao renderizar seção: {ex.Message}");
                                throw;
                            }
                        });

                        page.Footer().BorderTop(1).BorderColor(CorBorda).PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text($"Relatório {rel.Data:dd/MM/yyyy} n° {rel.Numero:D3} · Rev. {rel.Revisao:D2}")
                                .FontSize(8).FontColor(CorLabelCinza);
                            row.ConstantItem(60).AlignRight().Text(t =>
                            {
                                t.CurrentPageNumber().FontSize(8).FontColor(CorLabelCinza);
                                t.Span(" / ").FontSize(8).FontColor(CorLabelCinza);
                                t.TotalPages().FontSize(8).FontColor(CorLabelCinza);
                            });
                        });
                    });
                }).GeneratePdf();

                try
                {
                    File.WriteAllBytes(caminho, pdfBytes);
                }
                catch (IOException)
                {
                    var altNome = $"{rel.Data:yy-MM-dd}_{nomeSeguro}_{DateTime.Now:HHmmss}.pdf";
                    caminho = Path.Combine(pasta, altNome);
                    if (pastaAnexos != null)
                        pastaAnexos = Path.Combine(pasta, Path.GetFileNameWithoutExtension(altNome) + "_Anexos");
                    File.WriteAllBytes(caminho, pdfBytes);
                }

                // Copia documentos para pasta _Anexos ao lado do PDF
                if (pastaAnexos != null && docs.Any())
                {
                    try
                    {
                        Directory.CreateDirectory(pastaAnexos);
                        foreach (var d in docs)
                        {
                            if (File.Exists(d.CaminhoArquivo))
                            {
                                var destino = Path.Combine(pastaAnexos, Path.GetFileName(d.CaminhoArquivo));
                                File.Copy(d.CaminhoArquivo, destino, overwrite: true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PDF] Aviso: erro ao copiar anexos — {ex.Message}");
                    }
                }

                return caminho;
            }
            catch (QuestPDF.Drawing.Exceptions.DocumentLayoutException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PDF] Erro de layout: {ex.Message}");
                // Expõe o detalhe QuestPDF para facilitar diagnóstico
                var detalhe = ex.Message.Length > 400 ? ex.Message[..400] : ex.Message;
                throw new Exception($"Erro de layout PDF:\n{detalhe}", ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PDF] Erro inesperado: {ex.Message}");
                throw new Exception($"Erro ao gerar PDF: {ex.Message}", ex);
            }
        }

        // Achata a transparência de um PNG para fundo branco (evita xadrez no PDF)
        private static byte[]? AchatarLogoParaBranco(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                using var src = System.Drawing.Image.FromFile(path);
                using var bmp = new System.Drawing.Bitmap(src.Width, src.Height,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                using var g = System.Drawing.Graphics.FromImage(bmp);
                g.Clear(System.Drawing.Color.White);
                g.DrawImage(src, 0, 0, src.Width, src.Height);
                using var ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
            catch { return null; }
        }

        // ── CABEÇALHO ─────────────────────────────────────────────────────────
        // Modelo: logo Focus + logo empresa (esquerda) | metadados + badge Aprovado (direita)
        private static void DesenharCabecalho(IContainer c, Data.Models.Report rel, byte[]? focusBytes, byte[]? clientBytes)
        {

            c.Column(outer =>
            {
                outer.Item().Row(row =>
                {
                    // ── Logos à esquerda ──────────────────────────────────────
                    row.RelativeItem().AlignMiddle().Row(logoRow =>
                    {
                        // Logo Focus Engenharia Elétrica
                        if (focusBytes != null)
                            logoRow.RelativeItem().Padding(5).MaxHeight(55)
                                .AlignCenter().AlignMiddle().Image(focusBytes).FitArea();
                        else
                            logoRow.RelativeItem().AlignCenter().AlignMiddle()
                                .Text("focus").FontSize(10).Bold().FontColor(CorPreta);

                        logoRow.ConstantItem(8);

                        // Logo do cliente — menor, centralizado verticalmente
                        if (clientBytes != null)
                            logoRow.RelativeItem().Padding(5).MaxHeight(34)
                                .AlignCenter().AlignMiddle().Image(clientBytes).FitArea();
                        else
                            logoRow.RelativeItem().AlignCenter().AlignMiddle()
                                .Text(rel.Obra?.Grupo ?? "").FontSize(9).FontColor(CorLabelCinza);
                    });

                    // ── Metadados + badge à direita ───────────────────────────
                    row.ConstantItem(230).Column(rightCol =>
                    {
                        rightCol.Item().Row(topRow =>
                        {
                            topRow.RelativeItem();
                            topRow.ConstantItem(85).AlignRight().AlignMiddle()
                                .Border(0)
                                .Background(CorAprovado)
                                .PaddingVertical(4).PaddingHorizontal(6)
                                .Text("PUBLICADO")
                                .FontSize(9).Bold()
                                .FontColor(CorBranco);
                        });

                        rightCol.Item().PaddingTop(5).Border(1).BorderColor(CorBorda).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(3);
                            });

                            void MetaLinha(string label, string valor, bool ultimaLinha = false)
                            {
                                var bordaBottom = ultimaLinha ? 0 : 1;
                                table.Cell()
                                    .BorderBottom(bordaBottom).BorderRight(1).BorderColor(CorBordaInterna)
                                    .Background(CorCabecalhoSec)
                                    .Padding(6).Text(label).Bold().FontSize(8).FontColor(CorLabelCinza);
                                table.Cell()
                                    .BorderBottom(bordaBottom).BorderColor(CorBordaInterna)
                                    .Padding(6).Text(valor).FontSize(9).FontColor(CorTexto);
                            }

                            MetaLinha("Relatório n°", rel.Numero.ToString("D3"));
                            MetaLinha("Data do relatório", rel.Data.ToString("dd/MM/yyyy"));
                            MetaLinha("Dia da semana", rel.Data.DayOfWeek switch
                            {
                                DayOfWeek.Sunday => "Domingo",
                                DayOfWeek.Monday => "Segunda-feira",
                                DayOfWeek.Tuesday => "Terça-feira",
                                DayOfWeek.Wednesday => "Quarta-feira",
                                DayOfWeek.Thursday => "Quinta-feira",
                                DayOfWeek.Friday => "Sexta-feira",
                                _ => "Sábado"
                            });
                            MetaLinha("Revisão", $"Rev. {rel.Revisao:D2}", ultimaLinha: true);
                        });
                    });
                });

                outer.Item().PaddingTop(8).LineHorizontal(1).LineColor(CorBorda);
            });
        }

        // ── IDENTIFICAÇÃO DA OBRA ─────────────────────────────────────────────
        // Sem campo "Contratante" (= Resp. Cliente). CREA do responsável Focus exibido.
        private static void SecaoIdentificacao(IContainer c, Data.Models.Report rel)
        {
            c.Border(1).BorderColor(CorBorda).Column(bloco =>
            {
                // Cabeçalho da seção
                bloco.Item()
                    .Background(CorCabecalhoSec)
                    .BorderBottom(2).BorderColor(CorBorda)
                    .Padding(8)
                    .Text("Relatório Diário de Obra (RDO)")
                    .FontSize(11).Bold().FontColor(CorLabelCinza);

                // Tabela de identificação
                bloco.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(90);
                        cols.RelativeColumn(2);
                        cols.ConstantColumn(90);
                        cols.RelativeColumn();
                    });

                    void Linha(string l1, string v1, string l2, string v2, bool ultima = false)
                    {
                        var borda = ultima ? 0 : 1;
                        table.Cell().BorderBottom(borda).BorderRight(1).BorderColor(CorBordaInterna)
                             .Background(CorCabecalhoSec)
                             .Padding(7).Text(l1).Bold().FontSize(8).FontColor(CorLabelCinza);
                        table.Cell().BorderBottom(borda).BorderRight(1).BorderColor(CorBordaInterna)
                             .Padding(7).Text(v1).FontSize(9).FontColor(CorTexto);
                        table.Cell().BorderBottom(borda).BorderRight(1).BorderColor(CorBordaInterna)
                             .Background(CorCabecalhoSec)
                             .Padding(7).Text(l2).Bold().FontSize(8).FontColor(CorLabelCinza);
                        table.Cell().BorderBottom(borda).BorderColor(CorBordaInterna)
                             .Padding(7).Text(v2).FontSize(9).FontColor(CorTexto);
                    }

                    Linha("Obra", rel.Obra!.Nome,
                          "Número ART", rel.Obra!.ART);
                    Linha("Local", rel.Obra!.Endereco,
                          "Resp. Cliente", string.IsNullOrEmpty(rel.Obra!.ResponsavelCliente)
                                            ? rel.Obra!.Contratante : rel.Obra!.ResponsavelCliente);
                    Linha("Resp. Focus", rel.Obra!.Responsavel,
                          "CREA", ObterCrea(rel.Obra!.Responsavel));
                    Linha("Grupo", rel.Obra!.Grupo,
                          "Data", rel.Data.ToString("dd/MM/yyyy"), ultima: true);
                });
            });
        }

        // ── HORÁRIO DE TRABALHO ───────────────────────────────────────────────
        // Registro fixo conforme CLT: Entrada/Saída, Intervalo, Horas trabalhadas
        private static void SecaoHorarioTrabalho(IContainer c, Data.Models.Report rel)
        {
            TimeSpan.TryParse(rel.HoraEntrada, out var entrada);
            TimeSpan.TryParse(rel.HoraSaida, out var saida);
            TimeSpan.TryParse(rel.HoraIntervalo, out var intervalo);
            var trab = saida - entrada - intervalo;
            var hsTrab = trab.TotalMinutes > 0
                ? $"{(int)trab.TotalHours:D2}:{trab.Minutes:D2}"
                : "—";

            c.Border(1).BorderColor(CorBorda).Column(bloco =>
            {
                bloco.Item()
                    .Background(CorCabecalhoSec)
                    .BorderBottom(1).BorderColor(CorBorda)
                    .Padding(5)
                    .Text("Horário de trabalho")
                    .FontSize(11).Bold().FontColor(CorPreta);

                bloco.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    void Th(string txt) => table.Cell()
                        .Background(CorCabecalhoSec)
                        .BorderBottom(1).BorderColor(CorBorda)
                        .Padding(4).AlignCenter()
                        .Text(txt).Bold().FontSize(8);

                    Th("Entrada / Saída"); Th("Horas trabalhadas"); Th("Intervalo");

                    void Td(string v) => table.Cell()
                        .Padding(5).AlignCenter().Text(v).FontSize(8);

                    var entSaida = (!string.IsNullOrEmpty(rel.HoraEntrada) && !string.IsNullOrEmpty(rel.HoraSaida))
                        ? $"{rel.HoraEntrada} - {rel.HoraSaida}"
                        : rel.HoraEntrada ?? "—";

                    Td(entSaida);
                    Td(hsTrab);
                    Td(string.IsNullOrEmpty(rel.HoraIntervalo) ? "—" : rel.HoraIntervalo);
                });
            });
        }

        // ── CONDIÇÕES CLIMÁTICAS ──────────────────────────────────────────────
        // Colunas: Período | Tempo (ícone + texto) | Condição | Índice pluviométrico
        // "Impraticável" em vermelho
        private static void SecaoClima(IContainer c, Data.Models.Report rel)
        {
            var manha = rel.Climas.FirstOrDefault(x => x.Periodo == "Manhã");
            var tarde = rel.Climas.FirstOrDefault(x => x.Periodo == "Tarde");
            var noite = rel.Climas.FirstOrDefault(x => x.Periodo == "Noite");

            c.Border(1).BorderColor(CorBorda).Column(bloco =>
            {
                bloco.Item()
                    .Background(CorCabecalhoSec)
                    .BorderBottom(2).BorderColor(CorBorda)
                    .Padding(8)
                    .Text("Condição climática")
                    .FontSize(11).Bold().FontColor(CorLabelCinza);

                bloco.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(60);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                    });

                    void Th(string txt, bool ultima = false)
                    {
                        if (!ultima)
                            table.Cell()
                                .Background(CorCabecalhoSec)
                                .BorderBottom(1).BorderRight(1).BorderColor(CorBordaInterna)
                                .Padding(5).AlignCenter()
                                .Text(txt).Bold().FontSize(8).FontColor(CorLabelCinza);
                        else
                            table.Cell()
                                .Background(CorCabecalhoSec)
                                .BorderBottom(1).BorderColor(CorBordaInterna)
                                .Padding(5).AlignCenter()
                                .Text(txt).Bold().FontSize(8).FontColor(CorLabelCinza);
                    }

                    Th("Período"); Th("Tempo"); Th("Condição", ultima: true);

                    void LinhaClima(string periodo, Data.Models.WeatherDetail? clima, bool ultima = false)
                    {
                        var borda = ultima ? 0 : 1;
                        string tempo = clima?.Tempo ?? "—";
                        string condicao = clima?.Condicao ?? "—";
                        bool impraticavel = condicao.ToLowerInvariant().Contains("impratic");

                        table.Cell().BorderBottom(borda).BorderRight(1).BorderColor(CorBordaInterna)
                             .Padding(5).Text(periodo).Bold().FontSize(8).FontColor(CorLabelCinza);

                        table.Cell().BorderBottom(borda).BorderRight(1).BorderColor(CorBordaInterna)
                             .Padding(5).AlignCenter()
                             .Text($"{IconeTempo(tempo)} {tempo}").FontSize(9).FontColor(CorTexto);

                        var condCell = table.Cell().BorderBottom(borda).BorderColor(CorBordaInterna)
                             .Padding(5).AlignCenter();
                        var condTxt = condCell.Text(condicao).FontSize(9)
                             .FontColor(impraticavel ? CorVermelho : CorTexto);
                        if (impraticavel) condTxt.Bold();
                    }

                    LinhaClima("Manhã", manha);
                    LinhaClima("Tarde", tarde);
                    LinhaClima("Noite", noite, ultima: true);
                });
            });
        }

        // ── ACOMPANHANTES TÉCNICOS ────────────────────────────────────────────
        private static void SecaoAcompanhantes(IContainer c, List<Data.Models.Companion> acomps)
        {
            c.Border(1).BorderColor(CorBorda).Column(bloco =>
            {
                bloco.Item()
                    .Background(CorCabecalhoSec)
                    .BorderBottom(2).BorderColor(CorBorda)
                    .Padding(8)
                    .Text($"Acompanhantes técnicos ({acomps.Count})")
                    .FontSize(11).Bold().FontColor(CorLabelCinza);

                bloco.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(1.5f);
                    });

                    void Th(string txt, bool ultima = false)
                    {
                        if (!ultima)
                            table.Cell()
                                .Background(CorCabecalhoSec)
                                .BorderBottom(1).BorderRight(1).BorderColor(CorBordaInterna)
                                .Padding(5)
                                .Text(txt).Bold().FontSize(8).FontColor(CorLabelCinza);
                        else
                            table.Cell()
                                .Background(CorCabecalhoSec)
                                .BorderBottom(1).BorderColor(CorBordaInterna)
                                .Padding(5)
                                .Text(txt).Bold().FontSize(8).FontColor(CorLabelCinza);
                    }

                    Th("Nome"); Th("Grupo / Cliente", ultima: true);

                    for (int i = 0; i < acomps.Count; i++)
                    {
                        var bg = i % 2 == 0 ? CorBranco : CorLinhaAltern;
                        var borda = i < acomps.Count - 1 ? 1 : 0;
                        var ac = acomps[i];
                        void Td(string v, bool ultima = false)
                        {
                            if (!ultima)
                                table.Cell().Background(bg)
                                    .BorderBottom(borda).BorderRight(1).BorderColor(CorBordaInterna)
                                    .Padding(5).Text(v).FontSize(9).FontColor(CorTexto);
                            else
                                table.Cell().Background(bg)
                                    .BorderBottom(borda).BorderColor(CorBordaInterna)
                                    .Padding(5).Text(v).FontSize(9).FontColor(CorTexto);
                        }
                        Td(ac.Nome); Td(ac.Grupo, ultima: true);
                    }
                });
            });
        }

        // ── EQUIPE DE CAMPO (MÃO DE OBRA) ─────────────────────────────────────
        private static void SecaoEquipe(IContainer c, Data.Models.Report rel)
        {
            var assinaturas = rel.Assinaturas.ToList();
            int count = assinaturas.Count;

            c.Border(1).BorderColor(CorBorda).Column(bloco =>
            {
                bloco.Item()
                    .Background(CorCabecalhoSec)
                    .BorderBottom(2).BorderColor(CorBorda)
                    .Padding(8)
                    .Text($"Mão de obra ({count})")
                    .FontSize(11).Bold().FontColor(CorLabelCinza);

                if (count == 0)
                {
                    bloco.Item().Padding(8)
                        .Text("Nenhum membro registrado.").FontColor(CorLabelCinza).Italic().FontSize(8);
                    return;
                }

                bloco.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    void Th(string txt, bool ultima = false)
                    {
                        if (!ultima)
                            table.Cell()
                                .Background(CorCabecalhoSec)
                                .BorderBottom(1).BorderRight(1).BorderColor(CorBordaInterna)
                                .Padding(5)
                                .Text(txt).Bold().FontSize(8).FontColor(CorLabelCinza);
                        else
                            table.Cell()
                                .Background(CorCabecalhoSec)
                                .BorderBottom(1).BorderColor(CorBordaInterna)
                                .Padding(5)
                                .Text(txt).Bold().FontSize(8).FontColor(CorLabelCinza);
                    }

                    Th("Nome"); Th("Entrada / Saída"); Th("Intervalo"); Th("Horas", ultima: true);

                    for (int i = 0; i < assinaturas.Count; i++)
                    {
                        var bg = i % 2 == 0 ? CorBranco : CorLinhaAltern;
                        var borda = i < assinaturas.Count - 1 ? 1 : 0;
                        var a = assinaturas[i];
                        TimeSpan.TryParse(a.HoraEntrada, out var ent);
                        TimeSpan.TryParse(a.HoraSaida, out var sai);
                        TimeSpan.TryParse(a.HoraIntervalo, out var inv);
                        var trab = sai - ent - inv;
                        var hsTrab = trab.TotalMinutes > 0
                            ? $"{(int)trab.TotalHours:D2}:{trab.Minutes:D2}"
                            : "—";
                        string entSaida = (!string.IsNullOrEmpty(a.HoraEntrada) && !string.IsNullOrEmpty(a.HoraSaida))
                            ? $"{a.HoraEntrada} - {a.HoraSaida}"
                            : a.HoraEntrada ?? "—";

                        void Td(string v, bool negrito = false, bool ultima = false)
                        {
                            IContainer cell;
                            if (!ultima)
                                cell = table.Cell().Background(bg)
                                    .BorderBottom(borda).BorderRight(1).BorderColor(CorBordaInterna).Padding(5);
                            else
                                cell = table.Cell().Background(bg)
                                    .BorderBottom(borda).BorderColor(CorBordaInterna).Padding(5);

                            var txt = cell.Text(v).FontSize(9).FontColor(CorTexto);
                            if (negrito) txt.Bold();
                        }

                        Td(a.NomeAssinante); Td(entSaida);
                        Td(string.IsNullOrEmpty(a.HoraIntervalo) ? "—" : a.HoraIntervalo);
                        Td(hsTrab, negrito: true, ultima: true);
                    }
                });
            });
        }

        // ── EQUIPAMENTOS ──────────────────────────────────────────────────────
        // Tabela com Nº Série | Descrição | Modelo (mesma estrutura da Mão de Obra)
        private static void SecaoEquipamentos(IContainer c, Data.Models.Report rel)
        {
            var equipamentos = rel.Equipamentos
                .Where(re => re.Equipment != null)
                .Select(re => re.Equipment!)
                .ToList();

            c.Border(1).BorderColor(CorBorda).Column(bloco =>
            {
                bloco.Item()
                    .Background(CorCabecalhoSec)
                    .BorderBottom(2).BorderColor(CorBorda)
                    .Padding(8)
                    .Text($"Equipamentos ({equipamentos.Count})")
                    .FontSize(11).Bold().FontColor(CorLabelCinza);

                bloco.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(90);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn();
                    });

                    void Th(string txt, bool ultima = false)
                    {
                        if (!ultima)
                            table.Cell()
                                .Background(CorCabecalhoSec)
                                .BorderBottom(1).BorderRight(1).BorderColor(CorBordaInterna)
                                .Padding(5)
                                .Text(txt).Bold().FontSize(8).FontColor(CorLabelCinza);
                        else
                            table.Cell()
                                .Background(CorCabecalhoSec)
                                .BorderBottom(1).BorderColor(CorBordaInterna)
                                .Padding(5)
                                .Text(txt).Bold().FontSize(8).FontColor(CorLabelCinza);
                    }

                    Th("Nº Série"); Th("Descrição"); Th("Modelo", ultima: true);

                    for (int i = 0; i < equipamentos.Count; i++)
                    {
                        var bg = i % 2 == 0 ? CorBranco : CorLinhaAltern;
                        var borda = i < equipamentos.Count - 1 ? 1 : 0;
                        var eq = equipamentos[i];

                        void Td(string v, bool ultima = false)
                        {
                            if (!ultima)
                                table.Cell().Background(bg)
                                    .BorderBottom(borda).BorderRight(1).BorderColor(CorBordaInterna)
                                    .Padding(5).Text(v).FontSize(9).FontColor(CorTexto);
                            else
                                table.Cell().Background(bg)
                                    .BorderBottom(borda).BorderColor(CorBordaInterna)
                                    .Padding(5).Text(v).FontSize(9).FontColor(CorTexto);
                        }

                        Td(string.IsNullOrEmpty(eq.NumeroSerie) ? "—" : eq.NumeroSerie);
                        Td(eq.Nome);
                        Td(string.IsNullOrEmpty(eq.Modelo) ? "—" : eq.Modelo, ultima: true);
                    }
                });
            });
        }

        // ── ATIVIDADES ────────────────────────────────────────────────────────
        private static void SecaoAtividades(IContainer c, Data.Models.Report rel)
        {
            c.Border(1).BorderColor(CorBorda).Column(bloco =>
            {
                bloco.Item()
                    .Background(CorCabecalhoSec)
                    .BorderBottom(2).BorderColor(CorBorda)
                    .Padding(8)
                    .Text($"Atividades ({rel.Atividades.Count(a => a.ParentId == null)})")
                    .FontSize(11).Bold().FontColor(CorLabelCinza);

                if (!rel.Atividades.Any())
                {
                    bloco.Item().Padding(8)
                        .Text("Nenhuma atividade registrada.").FontColor(CorLabelCinza).Italic().FontSize(8);
                    return;
                }

                bloco.Item().Column(inner =>
                {
                    var raizes = rel.Atividades.Where(a => a.ParentId == null).ToList();
                    var subMap = rel.Atividades
                        .Where(a => a.ParentId != null)
                        .GroupBy(a => a.ParentId!.Value)
                        .ToDictionary(g => g.Key, g => g.OrderBy(a => a.Id).ToList());

                    for (int i = 0; i < raizes.Count; i++)
                    {
                        var bg = i % 2 == 0 ? CorBranco : CorLinhaAltern;
                        var at = raizes[i];
                        var subs = subMap.TryGetValue(at.Id, out var s) ? s : null;
                        var temSubs = subs?.Count > 0;

                        inner.Item().Background(bg)
                             .BorderBottom(temSubs ? 0 : 1).BorderColor(CorBorda)
                             .Padding(5).Row(row =>
                             {
                                 row.ConstantItem(18).Text($"{i + 1}.").Bold().FontSize(8)
                                     .FontColor(CorLabelCinza);
                                 row.RelativeItem().Text(at.Descricao).FontSize(8);
                             });

                        if (temSubs)
                        {
                            for (int j = 0; j < subs!.Count; j++)
                            {
                                var sub = subs[j];
                                var subBorda = j < subs.Count - 1 ? 0 : 1;
                                inner.Item().Background(bg)
                                     .BorderBottom(subBorda).BorderColor(CorBorda)
                                     .PaddingLeft(18).PaddingRight(5).PaddingBottom(4)
                                     .Row(row =>
                                     {
                                         row.ConstantItem(24).Text($"{i + 1}.{j + 1}").FontSize(7).Bold()
                                             .FontColor(CorLabelCinza);
                                         row.RelativeItem().Text(sub.Descricao).FontSize(7)
                                             .FontColor("#444444");
                                     });
                            }
                        }
                    }
                });
            });
        }

        // ── OCORRÊNCIAS ───────────────────────────────────────────────────────
        private static void SecaoOcorrencias(IContainer c, Data.Models.Report rel)
        {
            c.Border(1).BorderColor(CorBorda).Column(bloco =>
            {
                bloco.Item()
                    .Background(CorCabecalhoSec)
                    .BorderBottom(2).BorderColor(CorBorda)
                    .Padding(8)
                    .Text($"Ocorrências ({rel.Ocorrencias.Count})")
                    .FontSize(11).Bold().FontColor(CorLabelCinza);

                if (!rel.Ocorrencias.Any())
                {
                    bloco.Item().Padding(8)
                        .Text("Nenhuma ocorrência registrada.").FontColor(CorLabelCinza).Italic().FontSize(8);
                    return;
                }

                bloco.Item().Column(inner =>
                {
                    var lista = rel.Ocorrencias.ToList();
                    for (int i = 0; i < lista.Count; i++)
                    {
                        var bg = i % 2 == 0 ? "#FFFFFF" : "#EEF2F7";
                        var borda = i < lista.Count - 1 ? 1 : 0;
                        var oc = lista[i];
                        inner.Item().Background(bg)
                             .BorderBottom(borda).BorderColor(CorBorda)
                             .Padding(5).Column(ocol =>
                             {
                                 // Número + descrição
                                 ocol.Item().Row(row =>
                                 {
                                     row.ConstantItem(18).Text($"{i + 1}.").Bold().FontSize(8)
                                         .FontColor(CorLabelCinza);
                                     row.RelativeItem().Text(oc.Descricao).FontSize(8);
                                 });

                                 // Tags (ex: "Retrabalho", "Solicitação fora do escopo")
                                 if (!string.IsNullOrEmpty(oc.Tags))
                                 {
                                     ocol.Item().PaddingTop(2).PaddingLeft(18).Row(tagRow =>
                                     {
                                         foreach (var tag in oc.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
                                         {
                                             tagRow.AutoItem().PaddingRight(4)
                                                 .Border(1).BorderColor(CorLabelCinza)
                                                 .Padding(2)
                                                 .Text(tag.Trim()).FontSize(7).FontColor(CorLabelCinza);
                                         }
                                     });
                                 }

                                 // Horário com ícone de relógio
                                 if (!string.IsNullOrEmpty(oc.HoraInicio))
                                     ocol.Item().PaddingTop(2).PaddingLeft(18)
                                         .Text(t =>
                                         {
                                             t.Span("⏱ ").FontSize(8).FontColor(CorLabelCinza);
                                             t.Span($"{oc.HoraInicio} até {oc.HoraFim}")
                                                 .FontSize(7).FontColor(CorLabelCinza).Italic();
                                         });
                             });
                    }
                });
            });
        }

        // ── REGISTRO FOTOGRÁFICO ──────────────────────────────────────────────
        // Grade 2 colunas com legenda centralizada abaixo
        // Renderiza no máximo 2 fotos por página para evitar problemas de layout
        private static void SecaoFotos(IContainer c, List<Data.Models.Photo> fotos)
        {
            var comArquivo = fotos.Where(f => File.Exists(f.CaminhoArquivo)).ToList();
            if (!comArquivo.Any()) return;

            c.Border(1).BorderColor(CorBorda).Column(bloco =>
            {
                bloco.Item()
                    .Background(CorCabecalhoSec)
                    .BorderBottom(2).BorderColor(CorBorda)
                    .Padding(8)
                    .Text($"Fotos ({comArquivo.Count})")
                    .FontSize(11).Bold().FontColor(CorLabelCinza);

                // Renderiza cada par de fotos em uma nova página se necessário
                bloco.Item().Padding(6).Column(col =>
                {
                    for (int i = 0; i < comArquivo.Count; i += 2)
                    {
                        var foto1 = comArquivo[i];
                        var foto2 = i + 1 < comArquivo.Count ? comArquivo[i + 1] : null;

                        // Adiciona quebra de página antes de cada par (exceto o primeiro)
                        if (i > 0)
                            col.Item().PageBreak();

                        col.Item().Row(row =>
                        {
                            // Primeira foto da linha
                            row.RelativeItem().Padding(4).Column(inner =>
                            {
                                try
                                {
                                    var imageBytes = CropCenterToStandard(foto1.CaminhoArquivo);
                                    inner.Item().Height(8f, Unit.Centimetre) // Aumentado para 8cm
                                         .Border(1).BorderColor(CorBorda)
                                         .Image(imageBytes).FitArea();
                                    if (!string.IsNullOrEmpty(foto1.Legenda))
                                        inner.Item().PaddingTop(3)
                                             .Text(foto1.Legenda).FontSize(7)
                                             .FontColor(CorLabelCinza).Italic().AlignCenter();
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[PDF] Erro ao carregar foto: {ex.Message}");
                                    inner.Item().Height(8f, Unit.Centimetre)
                                        .Border(1).BorderColor(CorBorda)
                                        .Background(CorLinhaAltern).AlignCenter().AlignMiddle()
                                        .Text("Foto indisponível").FontSize(8).FontColor("#aaaaaa");
                                }
                            });

                            // Segunda foto da linha (se existir)
                            if (foto2 != null)
                            {
                                row.RelativeItem().Padding(4).Column(inner =>
                                {
                                    try
                                    {
                                        var imageBytes = CropCenterToStandard(foto2.CaminhoArquivo);
                                        inner.Item().Height(8f, Unit.Centimetre)
                                             .Border(1).BorderColor(CorBorda)
                                             .Image(imageBytes).FitArea();
                                        if (!string.IsNullOrEmpty(foto2.Legenda))
                                            inner.Item().PaddingTop(3)
                                                 .Text(foto2.Legenda).FontSize(7)
                                                 .FontColor(CorLabelCinza).Italic().AlignCenter();
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[PDF] Erro ao carregar foto: {ex.Message}");
                                        inner.Item().Height(8f, Unit.Centimetre)
                                            .Border(1).BorderColor(CorBorda)
                                            .Background(CorLinhaAltern).AlignCenter().AlignMiddle()
                                            .Text("Foto indisponível").FontSize(8).FontColor("#aaaaaa");
                                    }
                                });
                            }
                            else
                            {
                                // Espaço vazio se número ímpar de fotos
                                row.RelativeItem();
                            }
                        });
                    }
                });
            });
        }

        // ── DOCUMENTOS ANEXADOS ───────────────────────────────────────────────
        // Lista com nome (link clicável via file://) + tamanho
        private static void SecaoDocumentos(IContainer c, List<Data.Models.Photo> docs, string? pastaAnexos)
        {
            c.Border(1).BorderColor(CorBorda).Column(bloco =>
            {
                bloco.Item()
                    .Background(CorCabecalhoSec)
                    .BorderBottom(2).BorderColor(CorBorda)
                    .Padding(8)
                    .Text($"Anexos ({docs.Count})")
                    .FontSize(11).Bold().FontColor(CorLabelCinza);

                bloco.Item().Column(inner =>
                {
                    for (int i = 0; i < docs.Count; i++)
                    {
                        var doc = docs[i];
                        var nomeArq = Path.GetFileName(doc.CaminhoArquivo);
                        var bg = i % 2 == 0 ? CorBranco : CorLinhaAltern;
                        var borda = i < docs.Count - 1 ? 1 : 0;

                        string tamanho = "—";
                        try
                        {
                            if (File.Exists(doc.CaminhoArquivo))
                            {
                                var bytes = new FileInfo(doc.CaminhoArquivo).Length;
                                tamanho = bytes >= 1_048_576
                                    ? $"{bytes / 1_048_576.0:F1} MB"
                                    : $"{bytes / 1024} KB";
                            }
                        }
                        catch { }

                        var label = string.IsNullOrEmpty(doc.Legenda) ? nomeArq : doc.Legenda;
                        var caminhoLink = pastaAnexos != null
                            ? Path.Combine(pastaAnexos, nomeArq)
                            : doc.CaminhoArquivo;
                        var uri = $"file:///{caminhoLink.Replace('\\', '/')}";

                        inner.Item().Background(bg)
                             .BorderBottom(borda).BorderColor(CorBorda)
                             .Padding(5).Row(row =>
                             {
                                 row.ConstantItem(16).AlignMiddle()
                                    .Text("📎").FontSize(9).FontColor(CorLabelCinza);

                                 row.RelativeItem().AlignMiddle()
                                    .Hyperlink(uri)
                                    .Text(label)
                                    .FontSize(8).FontColor("#1565c0").Underline();

                                 row.ConstantItem(55).AlignMiddle().AlignRight()
                                    .Text(tamanho).FontSize(7).FontColor(CorLabelCinza);
                             });
                    }
                });
            });
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        // Ícone unicode para o tempo (compatível com fonte Arial)
        private static string IconeTempo(string tempo)
        {
            var t = tempo.ToLowerInvariant();
            if (t.Contains("claro") || t.Contains("sol") || t.Contains("ensol")) return "☀";
            if (t.Contains("parcial") || t.Contains("variav")) return "⛅";
            if (t.Contains("nublado") || t.Contains("nuvem")) return "☁";
            if (t.Contains("chuv") || t.Contains("rain")) return "☂";
            if (t.Contains("tempestade") || t.Contains("trovoada")) return "⚡";
            return "";
        }

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
    }
}
