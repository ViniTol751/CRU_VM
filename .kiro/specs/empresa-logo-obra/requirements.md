# Requirements Document

## Introduction

Esta feature introduz o conceito de **Empresa** no RDO App e o conecta às entidades existentes de Acompanhante Técnico e Obra. O objetivo é permitir que o logo da empresa contratante seja associado automaticamente a uma obra a partir do Acompanhante Técnico selecionado como Responsável Cliente, eliminando a necessidade de o usuário selecionar manualmente a imagem da obra na maioria dos casos.

O fluxo guiado é: **criar Empresa → criar Acompanhante vinculado à Empresa → criar Obra (logo preenchido automaticamente)**. Além disso, todos os locais que exibem a imagem da obra (card na tela principal, popup de detalhes, cabeçalho do PDF) passam a renderizar a imagem de forma padronizada (centralizada e recortada).

O modelo `Empresa` já existe no banco de dados (`RDO.Data/Models/Empresa.cs`) com os campos `Id`, `Nome`, `ImagemPath`, `IsActive`, `UpdatedAt` e `IsDeleted`. O modelo `Companion` já possui o campo `EmpresaId` (nullable). A feature completa a camada de UI e a lógica de negócio que ainda não foram implementadas.

---

## Glossary

- **Empresa**: Entidade que representa uma empresa contratante ou cliente. Possui nome, logo (imagem PNG) e status ativo/inativo. Mapeada para `RDO.Data.Models.Empresa`.
- **Acompanhante**: Pessoa técnica de acompanhamento vinculada a uma Empresa. Alias C# de `RDO.Data.Models.Companion`. Campos relevantes: `Nome`, `Cargo`, `Grupo`, `Contato`, `Ativo`, `EmpresaId`.
- **Obra**: Projeto de construção gerenciado pelo app. Alias C# de `RDO.Data.Models.Project`. Campo relevante: `ImagemPath` (alias `ImagemPath`), `Grupo` (alias `Grupo`), `ClientManager` (alias `ResponsavelCliente`).
- **Logo**: Arquivo de imagem PNG que representa visualmente uma Empresa. Armazenado localmente em `%LOCALAPPDATA%\RDOApp\Imagens\Empresas\`.
- **ImagemPath**: Caminho absoluto local para o arquivo de imagem associado a uma Obra ou Empresa.
- **CadastrosPage**: Página de cadastros com abas (Obras, Funcionários, Equipamentos, Acompanhante Técnico). Receberá uma nova aba "Empresas".
- **NovaObraPage**: Página de criação/edição de Obra. Contém o ComboBox `ResponsavelClienteBox` que lista Acompanhantes ativos.
- **GrupoBox**: Campo de texto na `NovaObraPage` que exibe o grupo/cliente da obra, preenchido automaticamente a partir do Acompanhante selecionado.
- **PreviewImagem**: Controle de imagem na `NovaObraPage` que exibe a pré-visualização da imagem da obra.
- **RdoDbContext**: Contexto Entity Framework Core com acesso ao SQLite local. Já expõe `DbSet<Empresa> Empresas`.
- **Thumbnail**: Miniatura da imagem exibida na lista de Empresas, com dimensões fixas de 40×40 px.
- **Crop centralizado**: Técnica de exibição de imagem onde a imagem é recortada ao centro para preencher uniformemente o espaço disponível, independentemente das dimensões originais. Implementado via `Stretch="UniformToFill"` com `Clip` ou `ViewBox` no WinUI 3.

---

## Requirements

### Requirement 1: CRUD de Empresas na aba Cadastros

**User Story:** Como usuário do RDO App, quero cadastrar, editar e desativar empresas com nome e logo, para que eu possa associá-las a Acompanhantes Técnicos e ter os logos preenchidos automaticamente nas obras.

#### Acceptance Criteria

1. THE `CadastrosPage` SHALL exibir uma aba "Empresas" ao lado das abas existentes (Obras, Funcionários, Equipamentos, Acompanhante Técnico).
2. WHEN o usuário clica na aba "Empresas", THE `CadastrosPage` SHALL exibir a lista de empresas ativas, ordenadas alfabeticamente por nome.
3. THE `CadastrosPage` SHALL exibir, para cada empresa na lista, o thumbnail do logo (40×40 px), o nome da empresa e os botões de editar e excluir.
4. IF a empresa não possui logo cadastrado, THEN THE `CadastrosPage` SHALL exibir um ícone de placeholder no lugar do thumbnail.
5. WHEN o usuário clica em "Nova Empresa", THE `CadastrosPage` SHALL abrir um modal com campos obrigatórios: Nome (TextBox) e um seletor de logo PNG (FileOpenPicker filtrado para `.png`).
6. WHEN o usuário confirma a criação de uma empresa com nome preenchido, THE `CadastrosPage` SHALL copiar o arquivo PNG selecionado para `%LOCALAPPDATA%\RDOApp\Imagens\Empresas\` e persistir o registro no banco SQLite local com `IsActive = true`.
7. IF o usuário tenta salvar uma empresa sem preencher o campo Nome, THEN THE `CadastrosPage` SHALL exibir uma mensagem de validação "O nome é obrigatório." e cancelar o salvamento.
8. WHEN o usuário clica em "Editar" em uma empresa existente, THE `CadastrosPage` SHALL abrir o mesmo modal preenchido com os dados atuais, permitindo alterar nome e logo.
9. WHEN o usuário confirma a edição e selecionou um novo arquivo PNG, THE `CadastrosPage` SHALL copiar o novo arquivo para `%LOCALAPPDATA%\RDOApp\Imagens\Empresas\` e atualizar `ImagemPath` no banco.
10. WHEN o usuário clica em "Excluir" em uma empresa, THE `CadastrosPage` SHALL exibir um diálogo de confirmação com o nome da empresa antes de prosseguir.
11. WHEN o usuário confirma a exclusão, THE `CadastrosPage` SHALL definir `IsActive = false` no registro da empresa (exclusão lógica), sem remover o arquivo de imagem do disco.
12. THE `CadastrosPage` SHALL exibir um campo de busca textual na aba Empresas que filtra a lista por nome em tempo real.
13. THE `CadastrosPage` SHALL exibir o contador de registros no formato `N registro(s)` na aba Empresas, consistente com as demais abas.

---

### Requirement 2: Vinculação de Empresa ao Acompanhante Técnico

**User Story:** Como usuário do RDO App, quero vincular um Acompanhante Técnico a uma Empresa cadastrada, para que o grupo e o logo da empresa sejam propagados automaticamente ao criar uma obra.

#### Acceptance Criteria

1. WHEN o usuário abre o modal de criação ou edição de Acompanhante Técnico, THE `CadastrosPage` SHALL exibir um ComboBox "Empresa" listando todas as empresas ativas, ordenadas alfabeticamente por nome.
2. WHEN o usuário seleciona uma Empresa no ComboBox, THE `CadastrosPage` SHALL preencher automaticamente o campo "Grupo / Cliente" com o nome da empresa selecionada e torná-lo somente leitura.
3. IF o usuário não seleciona nenhuma Empresa, THEN THE `CadastrosPage` SHALL manter o campo "Grupo / Cliente" editável manualmente, preservando o comportamento atual.
4. WHEN o usuário salva o Acompanhante com uma Empresa selecionada, THE `CadastrosPage` SHALL persistir o `EmpresaId` no registro `Companion` no banco SQLite local.
5. WHEN o usuário salva o Acompanhante sem selecionar uma Empresa, THE `CadastrosPage` SHALL persistir `EmpresaId = null` no registro `Companion`.
6. WHEN o usuário abre o modal de edição de um Acompanhante que já possui `EmpresaId`, THE `CadastrosPage` SHALL pré-selecionar a empresa correspondente no ComboBox.
7. THE `CadastrosPage` SHALL exibir, na lista de Acompanhantes, o nome da empresa vinculada (quando existir) como informação secundária abaixo do nome do acompanhante.
8. IF não existir nenhuma empresa ativa cadastrada ao abrir o modal de Acompanhante, THEN THE `CadastrosPage` SHALL exibir uma mensagem informativa "Nenhuma empresa cadastrada. Cadastre uma empresa primeiro." com um link/botão que navega para a aba Empresas.

---

### Requirement 3: Preenchimento automático de Grupo e Logo na NovaObraPage

**User Story:** Como usuário do RDO App, quero que ao selecionar o Responsável Cliente (Acompanhante) na criação de uma obra, o grupo e o logo da empresa vinculada sejam preenchidos automaticamente, para que eu não precise selecionar a imagem manualmente.

#### Acceptance Criteria

1. WHEN o usuário seleciona um Acompanhante no ComboBox `ResponsavelClienteBox` da `NovaObraPage`, THE `NovaObraPage` SHALL consultar o `EmpresaId` do Acompanhante selecionado no banco local.
2. WHEN o Acompanhante selecionado possui `EmpresaId` não nulo e a empresa correspondente possui `ImagemPath` válido (arquivo existe em disco), THE `NovaObraPage` SHALL definir `_imagemPath` com o `ImagemPath` da empresa e exibir o `PreviewImagem` com a imagem da empresa.
3. WHEN o Acompanhante selecionado possui `EmpresaId` não nulo, THE `NovaObraPage` SHALL preencher o `GrupoBox` com o nome da empresa e torná-lo somente leitura.
4. WHEN o Acompanhante selecionado não possui `EmpresaId` (nulo), THE `NovaObraPage` SHALL manter o comportamento atual: preencher `GrupoBox` com `acompanhante.Grupo` e permitir edição manual se o acompanhante for o especial "-".
5. WHEN o usuário seleciona um Acompanhante com empresa vinculada e depois troca para um Acompanhante sem empresa, THE `NovaObraPage` SHALL limpar o `PreviewImagem` e restaurar `GrupoBox` ao comportamento editável.
6. WHEN o usuário seleciona um Acompanhante com empresa vinculada mas o arquivo de logo não existe em disco, THE `NovaObraPage` SHALL preencher o `GrupoBox` normalmente e deixar o campo de imagem vazio (sem preview), sem exibir erro ao usuário.
7. WHEN a imagem é preenchida automaticamente a partir da empresa, THE `NovaObraPage` SHALL exibir o botão "Remover imagem" (`BtnRemoverImagem`) permitindo que o usuário substitua ou remova o logo manualmente.
8. WHEN o usuário salva a obra com imagem preenchida automaticamente a partir da empresa, THE `NovaObraPage` SHALL copiar o arquivo de logo para `%LOCALAPPDATA%\RDOApp\Imagens\` (pasta padrão de imagens de obras) e persistir o caminho copiado em `ImagemPath` da obra, sem modificar o arquivo original em `Empresas\`.

---

### Requirement 4: Padronização da exibição de imagens da Obra

**User Story:** Como usuário do RDO App, quero que a imagem da obra seja exibida de forma uniforme em todos os locais do app (card na tela principal, popup de detalhes, cabeçalho do PDF), para que logos de diferentes proporções não distorçam o layout.

#### Acceptance Criteria

1. THE `MainPage` SHALL exibir a imagem da obra nos cards com `Stretch="UniformToFill"` e recorte centralizado, de modo que a imagem preencha o espaço disponível sem distorção, independentemente das dimensões originais do arquivo PNG.
2. WHEN a obra não possui `ImagemPath` ou o arquivo não existe em disco, THE `MainPage` SHALL exibir um placeholder visual (ícone ou cor de fundo) no espaço reservado para a imagem no card.
3. WHERE a obra possui imagem, THE popup de detalhes da obra SHALL exibir a imagem com `Stretch="UniformToFill"` e recorte centralizado, com dimensões fixas definidas no layout.
4. WHERE a obra possui imagem, THE gerador de PDF SHALL incluir a imagem no cabeçalho do relatório redimensionada proporcionalmente para caber na área definida, sem distorção (mantendo aspect ratio).
5. IF o arquivo de imagem referenciado em `ImagemPath` não existir em disco no momento da geração do PDF, THEN THE gerador de PDF SHALL omitir a imagem do cabeçalho sem interromper a geração do relatório.
6. THE `NovaObraPage` SHALL exibir o `PreviewImagem` com `Stretch="UniformToFill"` e recorte centralizado na área de preview, consistente com a exibição nos cards da `MainPage`.

---

### Requirement 5: Orientação de fluxo ao usuário (ordering constraint)

**User Story:** Como usuário do RDO App, quero que o app me oriente a criar os cadastros na ordem correta (Empresa → Acompanhante → Obra), para que eu não tente criar uma obra sem ter os pré-requisitos cadastrados.

#### Acceptance Criteria

1. WHEN o usuário acessa a aba "Acompanhante Técnico" na `CadastrosPage` e não existe nenhuma empresa ativa cadastrada, THE `CadastrosPage` SHALL exibir um banner informativo: "Para vincular um acompanhante a uma empresa, cadastre uma empresa primeiro." com um botão "Ir para Empresas" que ativa a aba Empresas.
2. WHEN o usuário acessa a `NovaObraPage` e não existe nenhum Acompanhante ativo cadastrado, THE `NovaObraPage` SHALL exibir uma mensagem informativa no campo `ResponsavelClienteBox`: "Nenhum acompanhante cadastrado. Acesse Cadastros → Acompanhante Técnico para adicionar."
3. THE `CadastrosPage` SHALL posicionar a aba "Empresas" como a primeira aba da barra de navegação, antes de "Obras", para reforçar visualmente que Empresa é o ponto de partida do fluxo.
4. WHEN o usuário clica no botão "Cadastrar Acompanhante" na `NovaObraPage` (botão que navega para `CadastrosPage`), THE `NovaObraPage` SHALL navegar para a aba "Acompanhante Técnico" da `CadastrosPage`, preservando o comportamento atual de salvar e restaurar o estado do formulário.

---

### Requirement 6: Persistência offline e integridade de dados

**User Story:** Como usuário do RDO App, quero que todas as operações de Empresa funcionem 100% offline com o banco SQLite local, para que o app não dependa de conectividade para cadastrar ou exibir logos.

#### Acceptance Criteria

1. THE `CadastrosPage` SHALL realizar todas as operações de leitura e escrita de Empresas exclusivamente no banco SQLite local via `RdoDbContext`, sem dependência de rede.
2. WHEN um arquivo PNG de logo é selecionado pelo usuário, THE `CadastrosPage` SHALL copiar o arquivo para `%LOCALAPPDATA%\RDOApp\Imagens\Empresas\` antes de persistir o caminho no banco, garantindo que o arquivo esteja disponível offline.
3. IF dois arquivos PNG com o mesmo nome são copiados para `%LOCALAPPDATA%\RDOApp\Imagens\Empresas\`, THEN THE `CadastrosPage` SHALL sobrescrever o arquivo existente (`File.Copy` com `overwrite: true`), consistente com o comportamento atual de imagens de obras.
4. WHEN uma empresa é desativada (`IsActive = false`), THE `CadastrosPage` SHALL manter o arquivo de logo em disco e manter o `EmpresaId` nos registros de `Companion` que a referenciam, sem cascata de exclusão.
5. WHEN o app é iniciado sem conexão de rede, THE `CadastrosPage` SHALL carregar e exibir todas as empresas, logos e acompanhantes a partir do banco SQLite local sem erros.
6. THE `Empresa` entity SHALL implementar os campos `UpdatedAt` e `IsDeleted` para compatibilidade futura com o serviço de sincronização remota, mesmo que a sincronização de Empresas não seja implementada nesta fase.
