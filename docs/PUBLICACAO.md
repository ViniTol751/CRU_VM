# Guia de Publicação — RDO App

## Pré-requisitos

- Visual Studio 2022 ou .NET SDK instalado
- Inno Setup instalado → https://jrsoftware.org/isdl.php
- Acesso à pasta `C:\dev\TesteAPI\`

---

## Passo 1 — Copiar o banco de usuários

Antes de gerar o instalador, copie o banco local atual (que já tem os usuários cadastrados) para a pasta do instalador:

```powershell
Copy-Item "$env:LOCALAPPDATA\RDOApp\rdo_local.db" "C:\dev\TesteAPI\Instalador\rdo_inicial.db"
```

> Isso garante que toda nova instalação já venha com os usuários pré-cadastrados.

---

## Passo 2 — Publicar o app

Abra o PowerShell e rode:

```powershell
dotnet publish "C:\dev\TesteAPI\RDO.app\RDO.app.csproj" -c Release -r win-x64 --self-contained true -p:AppxPackageSigningEnabled=false -o "C:\dev\TesteAPI\RDO.app\publish"
```

---

## Passo 3 — Copiar arquivos necessários

```powershell
Copy-Item "C:\dev\TesteAPI\RDO.app\publish\resources.pri" "C:\dev\TesteAPI\RDO.app\publish\unpackaged\resources.pri" -Force
Copy-Item -Recurse -Force "C:\dev\TesteAPI\RDO.app\Assets" "C:\dev\TesteAPI\RDO.app\publish\unpackaged\Assets"
```

---

## Passo 4 — Gerar o instalador

1. Abra o **Inno Setup**
2. Vá em **File → Open** e selecione:
   ```
   C:\dev\TesteAPI\Instalador\RDO_Setup.iss
   ```
3. Pressione **F9** para compilar

---

## Saída

O instalador será gerado em:

```
C:\dev\TesteAPI\Instalador\Output\RDO_Setup_v1.0.3.0.exe
```

Distribua este único arquivo `.exe` para as outras máquinas.

---

## O que o instalador faz

- Instala o app em `C:\Program Files\FocusEngenharia\RDO\`
- Cria atalho no **Menu Iniciar**
- Opção de criar atalho na **Área de Trabalho**
- Copia o banco inicial com usuários para `%LocalAppData%\RDOApp\rdo_local.db` (apenas se não existir)
- Inclui **desinstalador**
- Abre o app automaticamente após instalar

---

## Requisitos da máquina destino

- Windows 10 64-bit versão 1809 (build 17763) ou superior
- Não precisa instalar .NET nem nenhuma dependência

---

## Usuários pré-cadastrados

Todos os usuários da Focus Engenharia Elétrica vêm com:

- **Senha padrão:** `F0cus@2026!`
- **Login:** primeiro nome + ponto + primeiro sobrenome (ex: `vinicius.toledo`)

Para redefinir a senha, use o botão **ESQUECEU A SENHA?** na tela de login.

---

## Atualizar versão

Para gerar uma nova versão, edite o número da versão no arquivo `RDO_Setup.iss`:

```ini
#define MyAppVersion "1.0.4.0"
```

E repita os passos 1 a 4.
