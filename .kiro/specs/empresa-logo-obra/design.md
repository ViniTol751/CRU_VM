# Design Document — empresa-logo-obra

## Overview

Esta feature conecta o modelo `Empresa` (já existente no banco) às entidades `Acompanhante` e `Obra`, completando a camada de UI e a lógica de negócio que ainda não foram implementadas. O resultado é um fluxo guiado: **Empresa → Acompanhante → Obra**, onde o logo da empresa contratante é propagado automaticamente para a obra a partir do Acompanhante selecionado como Responsável Cliente.

### Estado atual do código

Após leitura dos arquivos-chave, o estado atual é:

| Componente | Estado |
|---|---|
| `Empresa.cs` | ✅ Modelo completo com `Id, Nome, ImagemPath, IsActive, UpdatedAt, IsDeleted, Ativo` |
| `Companion.EmpresaId` | ✅ Campo `int? EmpresaId` já existe no modelo |
| `RdoDbContext.Empresas` | ✅ `DbSet<Empresa> Empresas` já declarado |
| Tabela `Empresa` no SQLite | ✅ Criada via `GarantirColunasExtras` em `App.xaml.cs` (CREATE TABLE IF NOT EXISTS) |
| Coluna `Companion.EmpresaId` | ✅ Adicionada via `GarantirColunasExtras` em `App.xaml.cs` (ALTER TABLE IF NOT EXISTS) |
| UI de Empresas (CadastrosPage) | ❌ Não implementada |
| Vinculação Empresa no modal de Acompanhante | ❌ Não implementada |
| Auto-preenchimento em NovaObraPage | ❌ Não implementado |
| Exibição de imagem nos cards (MainPage) | ⚠️ Parcialmente — `Stretch="UniformToFill"` já presente, mas sem clipping explícito |
| PDF — logo da empresa | ✅ `FitArea()` já implementado em `DesenharCabecalho` |

**Conclusão sobre migrations:** Nenhuma migration EF adicional é necessária. A tabela `Empresa` e a coluna `Companion.EmpresaId` já são garantidas em runtime pelo método `GarantirColunasExtras` em `App.xaml.cs`. O padrão do projeto é usar esse mecanismo de "ALTER TABLE IF NOT EXISTS" para bancos criados via `EnsureCreated` (sem histórico de migrations).

---

## Architecture

O projeto segue uma arquitetura de duas camadas simples, adequada para um app desktop offline-first:

```
┌─────────────────────────────────────────────────────────┐
│  RDO.app (WinUI 3 / .NET 8)                             │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │ CadastrosPage│  │ NovaObraPage │  │   MainPage    │  │
│  │  (Views)     │  │  (Views)     │  │   (Views)     │  │
│  └──────┬───────┘  └──────┬───────┘  └──────┬────────┘  │
│         │                 │                  │           │
│  ┌──────▼─────────────────▼──────────────────▼────────┐  │
│  │              RdoDbContext (EF Core / SQLite)        │  │
│  └──────────────────────────────────────────────────── ┘  │
└─────────────────────────────────────────────────────────┘
│  RDO.Data                                               │
│  Models: Empresa, Companion (Acompanhante), Project (Obra)│
└─────────────────────────────────────────────────────────┘
```

Não há camada de serviço separada — todo acesso ao banco é feito diretamente nas views via `using var db = new RdoDbContext(DbContextHelper.GetOptions())`, seguindo o padrão existente no projeto.

O armazenamento de imagens segue o padrão já estabelecido:
- Logos de empresas: `%LOCALAPPDATA%\RDOApp\Imagens\Empresas\`
- Imagens de obras: `%LOCALAPPDATA%\RDOApp\Imagens\`

---

## Components and Interfaces

### 1. Data Layer — sem alterações necessárias

Os modelos e o contexto já estão prontos. A única adição é garantir que `Empresa` não implemente `ILocalSyncEntity` por enquanto (já está correto — não implementa a interface), mantendo compatibilidade futura sem ativar sync.

### 2. CadastrosPage — nova aba Empresas

**Novos elementos XAML** (em `CadastrosPage.xaml`):
- `BtnAbaEmpresas` — botão de aba, posicionado **antes** de `BtnAbaObras` na barra de abas
- `PainelEmpresas` — painel com busca, contador e `ListView` de empresas
- Template de item da lista: thumbnail 40×40 (Border com Image + placeholder FontIcon), nome, botões Editar/Excluir
- `BannerSemEmpresas` — banner informativo na aba Acompanhantes (visível quando não há empresas ativas)

**Novos métodos em `CadastrosPage.xaml.cs`**:

```csharp
// Filtragem e exibição
private void FiltrarEmpresas(string termo)
// Busca textual em tempo real
private void BuscaEmpresas_TextChanged(object sender, TextChangedEventArgs e)
// CRUD
private async void AdicionarEmpresa_Click(object sender, RoutedEventArgs e)
private async void EditarEmpresa_Click(object sender, RoutedEventArgs e)
private async void ExcluirEmpresa_Click(object sender, RoutedEventArgs e)
private async Task AbrirModalEmpresa(Empresa? existente)
// Navegação de aba
private void BtnAbaEmpresas_Click(object sender, RoutedEventArgs e)
```

**Modificações em métodos existentes**:

`MostrarAba(string aba)` — adicionar case "Empresas":
```csharp
PainelEmpresas.Visibility = aba == "Empresas" ? Visibility.Visible : Visibility.Collapsed;
BtnAbaEmpresas.BorderBrush = aba == "Empresas" ? cor : transp;
BtnAbaEmpresas.BorderThickness = aba == "Empresas" ? new Thickness(0,0,0,2) : new Thickness(0);
BtnAbaEmpresas.Foreground = aba == "Empresas" ? cor : muted;
// Mostrar/ocultar banner na aba Acompanhantes
AtualizarBannerSemEmpresas();
```

`CarregarTodos()` — adicionar chamada a `FiltrarEmpresas(...)`.

`AbrirModalAcompanhante(Acompanhante? existente)` — adicionar:
- `ComboBox empresaComboBox` listando empresas ativas ordenadas por nome
- Ao selecionar empresa: `grupoBox.Text = empresa.Nome; grupoBox.IsReadOnly = true`
- Ao limpar seleção: `grupoBox.IsReadOnly = false`
- Ao salvar: `item.EmpresaId = empresaComboBox.SelectedItem is Empresa e ? e.Id : null`
- Pré-seleção ao editar: buscar empresa pelo `existente.EmpresaId`

`FiltrarAcompanhantes(string termo)` — ao popular a lista, incluir nome da empresa vinculada como texto secundário no item.

**Modal de Empresa** (`AbrirModalEmpresa`):

```csharp
private async Task AbrirModalEmpresa(Empresa? existente)
{
    var nomeBox = new TextBox { PlaceholderText = "Nome da empresa" };
    var logoNomeTexto = new TextBlock { ... };  // exibe nome do arquivo selecionado
    string? logoPathSelecionado = existente?.ImagemPath;

    // Thumbnail preview no modal (Border 80×80 + Image)
    var previewBorder = new Border { Width = 80, Height = 80, ... };

    // Botão selecionar PNG
    var btnSelecionarLogo = new Button { Content = "Selecionar logo (.PNG)" };
    btnSelecionarLogo.Click += async (s, ev) =>
    {
        var picker = new FileOpenPicker();
        // inicializar com hwnd...
        picker.FileTypeFilter.Add(".png");
        var file = await picker.PickSingleFileAsync();
        if (file != null) { logoPathSelecionado = file.Path; ... }
    };

    // Validação no PrimaryButtonClick: nome obrigatório
    // Ao salvar: File.Copy para Empresas\, persistir no banco
}
```

**Caminho de cópia de logo**:
```csharp
var pastaEmpresas = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "RDOApp", "Imagens", "Empresas");
Directory.CreateDirectory(pastaEmpresas);
var destino = Path.Combine(pastaEmpresas, Path.GetFileName(logoPathSelecionado));
File.Copy(logoPathSelecionado, destino, overwrite: true);
```

### 3. NovaObraPage — auto-preenchimento

**Modificação em `ResponsavelCliente_Changed`**:

```csharp
private void ResponsavelCliente_Changed(object sender, SelectionChangedEventArgs e)
{
    if (ResponsavelClienteBox.SelectedItem is ComboBoxItem item &&
        item.Tag is Acompanhante terceiro)
    {
        // Lógica de empresa (nova)
        if (terceiro.EmpresaId.HasValue)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var empresa = db.Empresas.Find(terceiro.EmpresaId.Value);
            if (empresa != null)
            {
                // Preenche grupo com nome da empresa (somente leitura)
                GrupoBox.IsReadOnly = true;
                GrupoBox.Text = empresa.Nome;
                GrupoBox.PlaceholderText = "";

                // Preenche preview de imagem se arquivo existe
                if (!string.IsNullOrEmpty(empresa.ImagemPath) && File.Exists(empresa.ImagemPath))
                {
                    _imagemPath = empresa.ImagemPath;
                    ImagemNomeTexto.Text = Path.GetFileName(empresa.ImagemPath);
                    ImagemPlaceholder.Visibility = Visibility.Collapsed;
                    PreviewImagem.Visibility = Visibility.Visible;
                    PreviewImagem.Source = new BitmapImage(new Uri(empresa.ImagemPath));
                    BtnRemoverImagem.Visibility = Visibility.Visible;
                }
                else
                {
                    // Empresa sem logo válido: limpa preview, mantém grupo preenchido
                    _imagemPath = null;
                    PreviewImagem.Visibility = Visibility.Collapsed;
                    BtnRemoverImagem.Visibility = Visibility.Collapsed;
                    ImagemPlaceholder.Visibility = Visibility.Visible;
                    ImagemNomeTexto.Text = "";
                }
                return;
            }
        }

        // Comportamento original (sem empresa vinculada)
        if (terceiro.Nome == "-")
        {
            GrupoBox.IsReadOnly = false;
            GrupoBox.Text = "";
            GrupoBox.PlaceholderText = "Ex: Cargill, Siemens...";
        }
        else
        {
            GrupoBox.IsReadOnly = true;
            GrupoBox.Text = terceiro.Grupo;
            GrupoBox.PlaceholderText = "";
        }

        // Limpa imagem ao trocar para acompanhante sem empresa
        _imagemPath = null;
        PreviewImagem.Visibility = Visibility.Collapsed;
        BtnRemoverImagem.Visibility = Visibility.Collapsed;
        ImagemPlaceholder.Visibility = Visibility.Visible;
        ImagemNomeTexto.Text = "";
    }
}
```

**Modificação em `CriarBtn_Click`** — a lógica de cópia de arquivo já existente cobre o caso do logo da empresa: se `_imagemPath` aponta para `Empresas\`, o arquivo é copiado para `Imagens\` normalmente (o código compara `_imagemPath != _obraExistente?.ImagemPath` para decidir se copia). Nenhuma alteração estrutural necessária — o comportamento já está correto.

### 4. MainPage — exibição de imagem nos cards

**Estado atual:** O XAML já tem `Stretch="UniformToFill"` no `Image` dentro do card. O `Border` pai tem `CornerRadius="8,8,0,0"` mas não tem `Clip` explícito — em WinUI 3, o `CornerRadius` no `Border` já aplica clipping ao conteúdo filho quando `ClipToBounds` está implícito via o sistema de layout.

**Verificação necessária:** Confirmar em runtime se a imagem transborda os cantos arredondados. Se transbordar, adicionar um `RectangleGeometry` como `Clip` no `Border` interno que contém o `Image`:

```xml
<!-- Solução se necessário: adicionar Clip ao Border da imagem -->
<Border CornerRadius="8,8,0,0">
    <Border.Clip>
        <RectangleGeometry Rect="0,0,280,160" RadiusX="8" RadiusY="8"/>
    </Border.Clip>
    <Image Source="{Binding ImagemPath, Converter={StaticResource ImagePathConverter}}"
           Stretch="UniformToFill"/>
</Border>
```

**Popup de detalhes** (criado em code-behind via `ContentDialog`): ao exibir a imagem da obra no popup, usar o mesmo padrão:
```csharp
var imgBorder = new Border
{
    Width = 400, Height = 200,
    CornerRadius = new CornerRadius(8),
    Child = new Image
    {
        Source = new BitmapImage(new Uri(obra.ImagemPath)),
        Stretch = Stretch.UniformToFill
    }
};
```

### 5. PDF — sem alterações necessárias

O método `DesenharCabecalho` já implementa corretamente:
```csharp
if (!string.IsNullOrEmpty(rel.Obra?.ImagemPath) && File.Exists(rel.Obra.ImagemPath))
    logoRow.ConstantItem(80).Image(rel.Obra.ImagemPath).FitArea();
else
    logoRow.ConstantItem(80); // espaço vazio, sem erro
```

`FitArea()` no QuestPDF mantém o aspect ratio e encaixa a imagem na área definida (80pt de largura) — comportamento correto para o requisito 4.4. O guard `File.Exists` já cobre o requisito 4.5 (omitir sem interromper).

---

## Data Models

### Empresa (sem alterações)

```csharp
public class Empresa
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? ImagemPath { get; set; }       // caminho absoluto local
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public bool Ativo { get => IsActive; set => IsActive = value; }
}
```

### Companion (sem alterações)

```csharp
public class Companion : ILocalSyncEntity
{
    // ... campos existentes ...
    public int? EmpresaId { get; set; }  // já existe
}
```

### Project / Obra (sem alterações)

`ImagePath` / `ImagemPath` já existe. Nenhum campo novo necessário.

### Fluxo de dados

```
Empresa (Id, Nome, ImagemPath)
    │
    │ EmpresaId (FK, nullable)
    ▼
Companion (Id, Nome, Grupo, EmpresaId)
    │
    │ ResponsavelCliente = companion.Nome
    │ ImagemPath ← copiado de empresa.ImagemPath
    ▼
Project / Obra (Id, Nome, Grupo, ImagemPath)
    │
    ├── MainPage cards (Image Stretch=UniformToFill)
    ├── Popup detalhes (Image Stretch=UniformToFill)
    └── PDF header (FitArea)
```

### Armazenamento de arquivos

```
%LOCALAPPDATA%\RDOApp\
├── Imagens\                    ← imagens de obras (existente)
│   ├── logo_ambev.png          ← cópia feita ao salvar obra
│   └── ...
└── Imagens\Empresas\           ← logos de empresas (novo)
    ├── logo_ambev.png          ← original, mantido mesmo após desativação
    └── ...
```

**Invariante importante:** O arquivo em `Empresas\` é o "master". Ao salvar uma obra, uma cópia é feita para `Imagens\`. Isso garante que a obra continue com sua imagem mesmo se a empresa for editada ou desativada posteriormente.

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Lista de empresas é filtrada e ordenada corretamente

*Para qualquer* conjunto de registros `Empresa` no banco (mix de ativas e inativas), o resultado de `FiltrarEmpresas(termo)` deve conter apenas empresas com `IsActive = true` cujo `Nome` contém `termo` (case-insensitive), ordenadas alfabeticamente por `Nome`.

**Validates: Requirements 1.2, 1.12**

### Property 2: Validação de nome obrigatório rejeita qualquer string vazia ou só espaços

*Para qualquer* string composta inteiramente de espaços em branco (incluindo a string vazia), tentar salvar uma `Empresa` com esse valor como `Nome` deve ser rejeitado — o registro não deve ser persistido no banco e uma mensagem de validação deve ser exibida.

**Validates: Requirements 1.7**

### Property 3: Soft-delete preserva arquivo e referências

*Para qualquer* `Empresa` com `IsActive = true` e com `ImagemPath` apontando para um arquivo existente em disco, após executar a exclusão lógica (`IsActive = false`): (a) o arquivo de logo ainda deve existir no caminho original, e (b) todos os registros `Companion` que tinham `EmpresaId = empresa.Id` ainda devem ter `EmpresaId` não nulo.

**Validates: Requirements 1.11, 6.4**

### Property 4: Seleção de empresa no modal de Acompanhante preenche Grupo e torna campo somente leitura

*Para qualquer* `Empresa` ativa, ao selecioná-la no ComboBox do modal de Acompanhante: o campo "Grupo / Cliente" deve ter `Text = empresa.Nome` e `IsReadOnly = true`.

**Validates: Requirements 2.2**

### Property 5: EmpresaId é persistido corretamente no round-trip de Acompanhante

*Para qualquer* `Empresa` ativa, ao criar ou editar um `Acompanhante` com essa empresa selecionada e salvar: o registro `Companion` no banco deve ter `EmpresaId = empresa.Id`. Ao reabrir o modal de edição desse `Acompanhante`, a empresa deve estar pré-selecionada no ComboBox.

**Validates: Requirements 2.4, 2.6**

### Property 6: Auto-preenchimento de Grupo e imagem a partir da empresa vinculada

*Para qualquer* `Acompanhante` com `EmpresaId` não nulo e empresa correspondente com `ImagemPath` válido (arquivo existe em disco): ao selecionar esse `Acompanhante` no `ResponsavelClienteBox` da `NovaObraPage`, `GrupoBox.Text` deve ser igual a `empresa.Nome`, `GrupoBox.IsReadOnly` deve ser `true`, e `_imagemPath` deve ser igual a `empresa.ImagemPath`.

**Validates: Requirements 3.2, 3.3**

### Property 7: Troca de Acompanhante limpa estado auto-preenchido

*Para qualquer* sequência onde o usuário primeiro seleciona um `Acompanhante` com empresa vinculada (que preenche `_imagemPath` e `GrupoBox`) e depois seleciona um `Acompanhante` sem `EmpresaId`: `_imagemPath` deve ser `null`, `PreviewImagem.Visibility` deve ser `Collapsed`, e `GrupoBox.IsReadOnly` deve ser `false` (se o novo acompanhante for "-") ou `true` com o grupo do acompanhante.

**Validates: Requirements 3.5**

### Property 8: Geração de PDF não falha quando ImagemPath não existe em disco

*Para qualquer* `Obra` com `ImagemPath` apontando para um caminho inexistente em disco, a chamada a `RdoPdfExportService.ExportAsync(relatorioId)` deve completar sem lançar exceção e retornar um caminho de arquivo válido (PDF gerado com sucesso, sem o logo da empresa no cabeçalho).

**Validates: Requirements 4.5**

### Property 9: Sobrescrita de arquivo com mesmo nome

*Para qualquer* dois arquivos PNG com o mesmo nome de arquivo (`Path.GetFileName`), ao copiar o segundo para `%LOCALAPPDATA%\RDOApp\Imagens\Empresas\` com `overwrite: true`, o conteúdo do arquivo no destino deve ser igual ao conteúdo do segundo arquivo (não do primeiro).

**Validates: Requirements 6.3**

---

## Error Handling

### Arquivo de logo não encontrado em disco

**Contexto:** `ImagemPath` pode apontar para um arquivo que foi movido ou deletado externamente.

**Tratamento:**
- `NovaObraPage.ResponsavelCliente_Changed`: verificar `File.Exists(empresa.ImagemPath)` antes de exibir preview. Se não existir, preencher `GrupoBox` normalmente mas deixar campo de imagem vazio (sem erro ao usuário). Requisito 3.6.
- `MainPage` cards: o `ImagePathConverter` existente já retorna `null` para caminhos inválidos, fazendo o `Image` não renderizar e o placeholder ficar visível.
- `RdoPdfExportService`: o guard `File.Exists` já está implementado — omite o logo sem interromper a geração.

### Validação de formulário (modal de Empresa)

- Nome vazio/whitespace: exibir `avisoTexto` com "O nome é obrigatório." e cancelar via `args.Cancel = true` no `PrimaryButtonClick`. Padrão idêntico ao modal de Funcionário.
- Logo não selecionado: **permitido** — logo é opcional. O campo `ImagemPath` pode ser `null`.

### Falha na cópia de arquivo

```csharp
try
{
    Directory.CreateDirectory(pastaEmpresas);
    File.Copy(logoPathSelecionado, destino, overwrite: true);
}
catch (IOException ex)
{
    await MostrarErro($"Não foi possível copiar o arquivo de logo: {ex.Message}");
    return; // não salvar no banco se a cópia falhou
}
```

A cópia deve ocorrer **antes** de `db.SaveChangesAsync()` para garantir que o `ImagemPath` persistido aponte para um arquivo que realmente existe.

### Empresa desativada referenciada por Acompanhante

Ao popular o ComboBox de empresas no modal de Acompanhante, filtrar apenas `IsActive = true`. Se um `Acompanhante` existente tem `EmpresaId` de uma empresa desativada, o ComboBox não terá esse item — o campo ficará sem seleção e o `Grupo` será exibido com o valor salvo anteriormente (comportamento aceitável, sem erro).

### Banco sem empresas cadastradas

- Modal de Acompanhante: exibir mensagem "Nenhuma empresa cadastrada. Cadastre uma empresa primeiro." com botão que chama `MostrarAba("Empresas")`. Requisito 2.8.
- Aba Acompanhantes: exibir `BannerSemEmpresas` com botão "Ir para Empresas". Requisito 5.1.

---

## Testing Strategy

### Avaliação de PBT para esta feature

Esta feature é predominantemente **UI e CRUD** em um app WinUI 3 desktop. A maior parte da lógica é:
- Manipulação de estado de controles XAML (visibilidade, IsReadOnly, Text)
- Operações de banco de dados via EF Core
- Cópia de arquivos

A lógica pura testável via PBT se concentra em:
1. Funções de filtragem/ordenação (`FiltrarEmpresas`)
2. Lógica de validação (nome obrigatório)
3. Lógica de estado (auto-preenchimento, limpeza ao trocar acompanhante)
4. Comportamento de soft-delete
5. Comportamento de sobrescrita de arquivo

**Biblioteca recomendada:** [FsCheck](https://fscheck.github.io/FsCheck/) para C# / .NET 8, integrado com xUnit.

### Testes de propriedade (PBT)

Cada teste deve rodar mínimo **100 iterações**. Tag de referência: `// Feature: empresa-logo-obra, Property N: <texto>`

**Property 1 — Filtragem e ordenação de empresas**
```csharp
// Feature: empresa-logo-obra, Property 1: lista filtrada e ordenada
[Property]
public Property FiltrarEmpresas_RetornaApenasAtivasOrdenadas(
    NonEmptyArray<EmpresaArb> empresas, string termo)
{
    // Arrange: inserir empresas no banco em memória
    // Act: chamar lógica de filtragem
    // Assert: resultado contém apenas IsActive=true, nome contém termo, ordenado por nome
}
```

**Property 2 — Validação de nome**
```csharp
// Feature: empresa-logo-obra, Property 2: nome vazio/whitespace rejeitado
[Property]
public Property SalvarEmpresa_ComNomeVazio_Rejeita(string nome)
{
    // Arrange: nome = string.IsNullOrWhiteSpace(nome) ? nome : new string(' ', n)
    // Act: tentar salvar
    // Assert: banco não contém novo registro, mensagem de erro exibida
}
```

**Property 3 — Soft-delete preserva arquivo e referências**
```csharp
// Feature: empresa-logo-obra, Property 3: soft-delete preserva arquivo e EmpresaId
[Property]
public Property DesativarEmpresa_MantemArquivoECompanions(EmpresaArb empresa, ...)
```

**Property 5 — Round-trip EmpresaId em Acompanhante**
```csharp
// Feature: empresa-logo-obra, Property 5: EmpresaId round-trip
[Property]
public Property SalvarAcompanhante_ComEmpresa_PersistEEmpresaId(EmpresaArb empresa)
```

**Property 6 — Auto-preenchimento**
```csharp
// Feature: empresa-logo-obra, Property 6: auto-preenchimento de grupo e imagem
[Property]
public Property SelecionarAcompanhante_ComEmpresa_PreencheGrupoEImagem(
    AcompanhanteArb acomp, EmpresaArb empresa)
```

**Property 8 — PDF não falha com ImagemPath inválido**
```csharp
// Feature: empresa-logo-obra, Property 8: PDF gerado mesmo sem logo em disco
[Property]
public Property ExportPdf_ComImagemPathInexistente_NaoLancaExcecao(
    string caminhoInexistente)
```

**Property 9 — Sobrescrita de arquivo**
```csharp
// Feature: empresa-logo-obra, Property 9: sobrescrita de arquivo com mesmo nome
[Property]
public Property CopiarLogo_MesmoNome_SobrescreverConteudo(
    byte[] conteudo1, byte[] conteudo2)
```

### Testes de exemplo (unit/integration)

- **Aba Empresas existe na CadastrosPage** — verificar que `BtnAbaEmpresas` está presente no XAML
- **Aba Empresas é a primeira** — verificar posição relativa dos botões de aba
- **Modal de Empresa abre com campos corretos** — verificar presença de TextBox nome e botão de logo
- **Diálogo de confirmação de exclusão exibe nome** — verificar que o nome da empresa aparece no diálogo
- **Navegação "Cadastrar Acompanhante" preserva estado** — verificar que `CadastrosParams.AbaInicial = "Acompanhantes"` é passado

### Testes de fumaça (smoke)

- Verificar que `Empresa` tem campos `UpdatedAt` e `IsDeleted` (requisito 6.6)
- Verificar que `PreviewImagem` tem `Stretch="UniformToFill"` no XAML
- Verificar que `DesenharCabecalho` usa `File.Exists` antes de renderizar logo
- Verificar que todas as operações de Empresa usam `RdoDbContext` (sem chamadas de rede)

### Testes de integração

- **Criação de empresa com logo** — selecionar PNG, confirmar, verificar arquivo em `Empresas\` e registro no banco
- **Edição de empresa com novo logo** — verificar que `ImagemPath` é atualizado e novo arquivo existe
- **Salvar obra com logo da empresa** — verificar que arquivo é copiado de `Empresas\` para `Imagens\`
- **PDF com logo válido** — gerar PDF com obra que tem `ImagemPath` válido, verificar que imagem aparece no cabeçalho
