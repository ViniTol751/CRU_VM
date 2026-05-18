# Implementation Plan: empresa-logo-obra

## Overview

Implementar o fluxo **Empresa → Acompanhante → Obra** no RDO App. O trabalho se concentra em três áreas:

1. **CadastrosPage** — nova aba "Empresas" (CRUD completo) + atualização do modal de Acompanhante para vincular empresa
2. **NovaObraPage** — auto-preenchimento de Grupo e logo ao selecionar Responsável Cliente com empresa vinculada
3. **MainPage** — verificação/ajuste do clipping de imagem nos cards

Não há migrations EF nem alterações em modelos de dados — tudo já está pronto no banco via `GarantirColunasExtras`.

---

## Tasks

- [x] 1. Adicionar aba "Empresas" ao XAML da CadastrosPage
  - Inserir `BtnAbaEmpresas` **antes** de `BtnAbaObras` na barra de abas (StackPanel de botões de aba), com o mesmo estilo dos demais botões de aba (`Padding="20,14"`, `Background="Transparent"`, `Click="BtnAbaEmpresas_Click"`)
  - Criar `PainelEmpresas` (StackPanel com `x:Name="PainelEmpresas"`, `Visibility="Collapsed"`, `Spacing="12"`) dentro do ScrollViewer de conteúdo, seguindo a mesma estrutura dos painéis existentes
  - O painel deve conter: barra de busca (`BuscaEmpresasBox` + `LimparBuscaEmpresasBtn`), contador (`EmpresasCountText`), botão "+ Nova Empresa" e `ListView` (`EmpresasListView`)
  - Template de item da lista: thumbnail 40×40 px (Border com Image + FontIcon placeholder quando `ImagemPath` é nulo), nome da empresa, botões Editar (✏) e Excluir (🗑) com os mesmos estilos `EditBtnBgBrush`/`DangerBgBrush` dos outros painéis
  - Adicionar `BannerSemEmpresas` (Border com TextBlock + Button "Ir para Empresas") dentro do `PainelAcompanhantes`, visível apenas quando não há empresas ativas
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.12, 1.13, 5.1, 5.3_

- [x] 2. Implementar lógica C# da aba Empresas na CadastrosPage
  - [x] 2.1 Atualizar `MostrarAba` e `CarregarTodos` para suportar a nova aba
    - Em `MostrarAba(string aba)`: adicionar `PainelEmpresas.Visibility`, `BtnAbaEmpresas.BorderBrush/BorderThickness/Foreground` seguindo o padrão exato dos outros cases; chamar `AtualizarBannerSemEmpresas()` ao final
    - Em `CarregarTodos()`: adicionar chamada `FiltrarEmpresas(BuscaEmpresasBox?.Text ?? "")`
    - Adicionar `BtnAbaEmpresas_Click` que chama `MostrarAba("Empresas")`
    - Implementar `AtualizarBannerSemEmpresas()`: consulta `db.Empresas.Any(e => e.IsActive)` e define `BannerSemEmpresas.Visibility`
    - _Requirements: 1.1, 5.1, 5.3_

  - [x] 2.2 Implementar `FiltrarEmpresas` e handlers de busca
    - `FiltrarEmpresas(string termo)`: consultar `db.Empresas.Where(e => e.IsActive).OrderBy(e => e.Nome).ToList()`, filtrar por `termo` (case-insensitive no `Nome`), atribuir a `EmpresasListView.ItemsSource`, atualizar `EmpresasCountText.Text`
    - `BuscaEmpresas_TextChanged`: chamar `FiltrarEmpresas`, controlar visibilidade de `LimparBuscaEmpresasBtn`
    - `LimparBuscaEmpresas_Click`: limpar texto e ocultar botão
    - _Requirements: 1.2, 1.12, 1.13_

  - [x] 2.3 Implementar `AbrirModalEmpresa` (criar e editar)
    - Criar `TextBox nomeBox` e área de preview de logo (Border 80×80 com Image + FontIcon placeholder)
    - Criar `TextBlock logoNomeTexto` para exibir o nome do arquivo selecionado
    - Criar `Button btnSelecionarLogo` que abre `FileOpenPicker` filtrado para `.png`, inicializado com `WinRT.Interop.WindowNative.GetWindowHandle` (mesmo padrão de `NovaObraPage.ImagemBorder_Tapped`); ao selecionar arquivo, atualizar `logoPathSelecionado` e o preview
    - No `PrimaryButtonClick`: validar `string.IsNullOrWhiteSpace(nomeBox.Text)` → exibir `avisoTexto` "O nome é obrigatório." e `args.Cancel = true` (padrão idêntico ao modal de Funcionário)
    - Ao confirmar: copiar PNG para `%LOCALAPPDATA%\RDOApp\Imagens\Empresas\` com `File.Copy(overwrite: true)` dentro de try/catch `IOException`; a cópia deve ocorrer **antes** de `db.SaveChangesAsync()`
    - Criar ou atualizar registro `Empresa` no banco; chamar `CarregarTodos()`
    - Ao editar (`existente != null`): pré-preencher `nomeBox.Text` e exibir thumbnail atual
    - _Requirements: 1.5, 1.6, 1.7, 1.8, 1.9, 6.1, 6.2, 6.3_

  - [x] 2.4 Implementar handlers de CRUD de Empresa
    - `AdicionarEmpresa_Click`: chamar `await AbrirModalEmpresa(null)`
    - `EditarEmpresa_Click`: extrair `Empresa` do `Tag` do botão, chamar `await AbrirModalEmpresa(empresa)`
    - `ExcluirEmpresa_Click`: extrair `Empresa` do `Tag`, chamar `await ConfirmarExclusao(empresa.Nome)` (helper existente); se confirmado, definir `empresa.IsActive = false` e `db.SaveChangesAsync()` (sem deletar arquivo); chamar `FiltrarEmpresas`
    - _Requirements: 1.10, 1.11, 6.4_

  - [ ]* 2.5 Escrever testes de propriedade para filtragem de empresas
    - **Property 1: Lista de empresas é filtrada e ordenada corretamente**
    - **Validates: Requirements 1.2, 1.12**

  - [ ]* 2.6 Escrever testes de propriedade para validação de nome obrigatório
    - **Property 2: Validação de nome obrigatório rejeita qualquer string vazia ou só espaços**
    - **Validates: Requirements 1.7**

  - [ ]* 2.7 Escrever testes de propriedade para soft-delete
    - **Property 3: Soft-delete preserva arquivo e referências**
    - **Validates: Requirements 1.11, 6.4**

  - [ ]* 2.8 Escrever testes de propriedade para sobrescrita de arquivo
    - **Property 9: Sobrescrita de arquivo com mesmo nome**
    - **Validates: Requirements 6.3**

- [ ] 3. Checkpoint — Verificar aba Empresas
  - Garantir que todos os testes passam; verificar que a aba Empresas aparece como primeira aba, que o CRUD funciona, que o banner aparece quando não há empresas. Perguntar ao usuário se houver dúvidas.

- [x] 4. Atualizar modal de Acompanhante para vincular Empresa
  - [x] 4.1 Adicionar ComboBox de Empresa ao modal de Acompanhante
    - Em `AbrirModalAcompanhante(Acompanhante? existente)`: carregar `db.Empresas.Where(e => e.IsActive).OrderBy(e => e.Nome).ToList()` no início do método
    - Criar `ComboBox empresaComboBox` com `PlaceholderText = "Selecione uma empresa (opcional)"` e popular com as empresas ativas
    - Se não houver empresas ativas, exibir `TextBlock` com "Nenhuma empresa cadastrada. Cadastre uma empresa primeiro." e `HyperlinkButton`/`Button` que chama `MostrarAba("Empresas")` e fecha o dialog
    - Adicionar `empresaComboBox` ao formulário do modal, em linha com ou acima do campo "GRUPO / CLIENTE"
    - _Requirements: 2.1, 2.8_

  - [x] 4.2 Implementar lógica de preenchimento automático de Grupo no modal
    - No `SelectionChanged` do `empresaComboBox`: se empresa selecionada, `grupoBox.Text = empresa.Nome; grupoBox.IsReadOnly = true`; se seleção limpa, `grupoBox.IsReadOnly = false`
    - Ao salvar (após `dialog.ShowAsync() == Primary`): `item.EmpresaId = empresaComboBox.SelectedItem is Empresa e ? (int?)e.Id : null`
    - Ao editar (`existente != null` com `EmpresaId` não nulo): pré-selecionar a empresa correspondente no ComboBox (`empresaComboBox.SelectedItem = empresas.FirstOrDefault(e => e.Id == existente.EmpresaId)`)
    - _Requirements: 2.2, 2.3, 2.4, 2.5, 2.6_

  - [x] 4.3 Exibir nome da empresa vinculada na lista de Acompanhantes
    - No template XAML do `AcompanhantesListView` (em `CadastrosPage.xaml`): adicionar um `TextBlock` secundário abaixo do nome do acompanhante que exibe o nome da empresa vinculada
    - Como o `DataTemplate` faz binding direto em `Acompanhante`, criar um `ViewModel` auxiliar ou usar um `Converter` que resolve `EmpresaId` → nome; alternativamente, popular a lista com um tipo anônimo/DTO que já inclui `EmpresaNome`
    - Seguir o padrão visual dos outros campos secundários (FontSize="11", Foreground=TextSecondaryBrush)
    - _Requirements: 2.7_

  - [ ]* 2.9 Escrever testes de propriedade para seleção de empresa no modal
    - **Property 4: Seleção de empresa no modal de Acompanhante preenche Grupo e torna campo somente leitura**
    - **Validates: Requirements 2.2**

  - [ ]* 2.10 Escrever testes de propriedade para round-trip de EmpresaId
    - **Property 5: EmpresaId é persistido corretamente no round-trip de Acompanhante**
    - **Validates: Requirements 2.4, 2.6**

- [ ] 5. Checkpoint — Verificar vinculação Empresa-Acompanhante
  - Garantir que todos os testes passam; verificar que o ComboBox de empresa aparece no modal, que o Grupo é preenchido automaticamente, que o EmpresaId é salvo e restaurado na edição. Perguntar ao usuário se houver dúvidas.

- [x] 6. Implementar auto-preenchimento de Grupo e logo na NovaObraPage
  - [x] 6.1 Modificar `CarregarTerceiros` para incluir `EmpresaId` nos itens do ComboBox
    - O `ComboBoxItem.Tag` já armazena o objeto `Acompanhante` completo — verificar que o objeto carregado do banco inclui `EmpresaId` (campo já existe no modelo `Companion`); se o EF não carrega por padrão, garantir que a query inclui o campo
    - _Requirements: 3.1_

  - [x] 6.2 Atualizar `ResponsavelCliente_Changed` com lógica de empresa vinculada
    - Substituir o método existente pela versão do design: verificar `terceiro.EmpresaId.HasValue`; se sim, consultar `db.Empresas.Find(terceiro.EmpresaId.Value)`
    - Se empresa encontrada e `File.Exists(empresa.ImagemPath)`: preencher `GrupoBox.Text = empresa.Nome`, `GrupoBox.IsReadOnly = true`, definir `_imagemPath = empresa.ImagemPath`, exibir `PreviewImagem` com `BitmapImage(new Uri(empresa.ImagemPath))`, mostrar `BtnRemoverImagem`
    - Se empresa encontrada mas arquivo não existe: preencher `GrupoBox.Text = empresa.Nome`, `GrupoBox.IsReadOnly = true`, limpar preview (`_imagemPath = null`, `PreviewImagem.Visibility = Collapsed`, `ImagemPlaceholder.Visibility = Visible`)
    - Se `EmpresaId` nulo: manter comportamento original (lógica do terceiro "-" e `terceiro.Grupo`)
    - Ao trocar para acompanhante sem empresa: limpar `_imagemPath`, ocultar `PreviewImagem` e `BtnRemoverImagem`, mostrar `ImagemPlaceholder`, limpar `ImagemNomeTexto.Text`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_

  - [x] 6.3 Verificar que `CriarBtn_Click` copia corretamente o logo da empresa para a pasta de obras
    - A lógica existente já copia `_imagemPath` para `%LOCALAPPDATA%\RDOApp\Imagens\` quando `_imagemPath != _obraExistente?.ImagemPath` — confirmar que esse caminho cobre o caso onde `_imagemPath` aponta para `Empresas\`
    - Se necessário, ajustar a condição de cópia para garantir que o arquivo é sempre copiado quando `_imagemPath` aponta para a pasta `Empresas\` (arquivo "master"), mesmo que o nome seja igual ao `ImagemPath` da obra existente
    - _Requirements: 3.8_

  - [ ]* 6.4 Escrever testes de propriedade para auto-preenchimento
    - **Property 6: Auto-preenchimento de Grupo e imagem a partir da empresa vinculada**
    - **Validates: Requirements 3.2, 3.3**

  - [ ]* 6.5 Escrever testes de propriedade para limpeza ao trocar acompanhante
    - **Property 7: Troca de Acompanhante limpa estado auto-preenchido**
    - **Validates: Requirements 3.5**

- [ ] 7. Checkpoint — Verificar auto-preenchimento na NovaObraPage
  - Garantir que todos os testes passam; verificar que ao selecionar acompanhante com empresa, Grupo e preview são preenchidos; ao trocar para acompanhante sem empresa, o estado é limpo. Perguntar ao usuário se houver dúvidas.

- [x] 8. Verificar e ajustar exibição de imagem nos cards da MainPage
  - [x] 8.1 Verificar clipping da imagem nos cards da MainPage
    - Localizar o `Border`/`Image` que exibe `ImagemPath` nos cards de obra no XAML da `MainPage`
    - Confirmar que `Stretch="UniformToFill"` já está presente; se o `Border` pai tem `CornerRadius` mas a imagem transborda os cantos, adicionar `ClipToBounds` ou um `RectangleGeometry` como `Clip` no Border interno
    - _Requirements: 4.1, 4.2_

  - [x] 8.2 Verificar exibição de imagem no popup de detalhes da obra
    - Localizar o código C# que cria o `ContentDialog` de detalhes da obra na `MainPage`
    - Garantir que a imagem da obra é exibida com `Stretch = Stretch.UniformToFill` e `CornerRadius` adequado, seguindo o padrão do design
    - _Requirements: 4.3_

  - [x] 8.3 Verificar `PreviewImagem` na NovaObraPage
    - Confirmar que o `Image` com `x:Name="PreviewImagem"` no XAML da `NovaObraPage` tem `Stretch="UniformToFill"` e que o `Border` pai tem `CornerRadius` e clipping adequados
    - _Requirements: 4.6_

  - [ ]* 8.4 Escrever teste de propriedade para PDF sem logo em disco
    - **Property 8: Geração de PDF não falha quando ImagemPath não existe em disco**
    - **Validates: Requirements 4.5**

- [x] 9. Implementar orientação de fluxo ao usuário (banners e mensagens)
  - [x] 9.1 Banner na aba Acompanhantes quando não há empresas
    - O `BannerSemEmpresas` já foi adicionado ao XAML na tarefa 1; implementar `AtualizarBannerSemEmpresas()` no code-behind (já referenciado na tarefa 2.1) para controlar sua visibilidade
    - O botão "Ir para Empresas" no banner deve chamar `MostrarAba("Empresas")`
    - _Requirements: 5.1_

  - [x] 9.2 Mensagem informativa no ComboBox de Acompanhante da NovaObraPage quando não há acompanhantes
    - Em `CarregarTerceiros()`: após popular o ComboBox, verificar se a lista está vazia; se sim, adicionar um `ComboBoxItem` desabilitado com o texto "Nenhum acompanhante cadastrado. Acesse Cadastros → Acompanhante Técnico para adicionar."
    - _Requirements: 5.2_

  - [x] 9.3 Garantir que "Cadastrar Acompanhante" navega para aba correta
    - Verificar que `CadastrarTerceiro_Click` em `NovaObraPage` passa `AbaInicial = "Terceiros"` (que é mapeado para "Acompanhantes" em `CadastrosPage.OnNavigatedTo`) — comportamento já existente, apenas confirmar que continua correto após as alterações
    - _Requirements: 5.4_

- [x] 10. Checkpoint final — Garantir que todos os testes passam
  - Executar todos os testes (unitários, de propriedade e de integração); verificar que o build compila sem erros ou warnings; confirmar que o fluxo completo Empresa → Acompanhante → Obra funciona end-to-end. Perguntar ao usuário se houver dúvidas.

---

## Notes

- Tarefas marcadas com `*` são opcionais e podem ser puladas para um MVP mais rápido
- Cada tarefa referencia os requisitos específicos para rastreabilidade
- A ordem das tarefas segue o fluxo de dependência: Empresas → Acompanhante → Obra → MainPage
- Não há migrations EF — a tabela `Empresa` e a coluna `Companion.EmpresaId` já existem via `GarantirColunasExtras` em `App.xaml.cs`
- O PDF já está completo (`FitArea()` + `File.Exists` guard) — nenhuma alteração necessária
- Os modelos de dados já estão completos — nenhuma alteração necessária
- Padrões a seguir: `ContentDialog` em code-behind, `MostrarAba` para navegação de abas, `FileOpenPicker` com `WinRT.Interop` para seleção de arquivos, `File.Copy(overwrite: true)` para armazenamento de imagens
- Biblioteca de PBT recomendada: FsCheck integrado com xUnit
