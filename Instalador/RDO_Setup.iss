#define MyAppName "RDO · Relatório Diário de Obra"
#define MyAppVersion "1.0.3.0"
#define MyAppPublisher "Focus Engenharia Elétrica"
#define MyAppExeName "RDO.app.exe"
#define MyAppSourceDir "C:\dev\TesteAPI\RDO.app\publish\unpackaged"
#define MyAppIconFile "C:\dev\TesteAPI\RDO.app\FOCUS.ico"

[Setup]
AppId={{DE971E92-1C12-4098-966C-1D42255CD82D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://www.focusengenharia.com.br
DefaultDirName={autopf}\FocusEngenharia\RDO
DefaultGroupName=Focus Engenharia\RDO
AllowNoIcons=yes
OutputDir=C:\dev\TesteAPI\Instalador\Output
OutputBaseFilename=RDO_Setup_v{#MyAppVersion}
SetupIconFile={#MyAppIconFile}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardResizable=no
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
MinVersion=10.0.17763
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; Aparência
WizardImageFile=compiler:WizModernImage.bmp
WizardSmallImageFile=compiler:WizModernSmallImage.bmp

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar ícone na Área de Trabalho"; GroupDescription: "Ícones adicionais:"; Flags: unchecked
Name: "startupicon"; Description: "Iniciar automaticamente com o Windows"; GroupDescription: "Inicialização:"; Flags: unchecked

[Files]
; Todos os arquivos da pasta publish\unpackaged
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; resources.pri — PRI do build atual, essencial para WinUI 3 resolver ms-appx:/// URIs
Source: "C:\dev\TesteAPI\RDO.app\publish\unpackaged\RDO.app.pri"; DestDir: "{app}"; DestName: "resources.pri"; Flags: ignoreversion
; Pasta Assets com imagens (logo, ícones, tela de login)
Source: "C:\dev\TesteAPI\RDO.app\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs
; Banco SQLite inicial com usuários pré-cadastrados — copiado para AppData apenas se não existir
Source: "C:\dev\TesteAPI\Instalador\rdo_inicial.db"; DestDir: "{localappdata}\RDOApp"; DestName: "rdo_local.db"; Flags: onlyifdoesntexist uninsneveruninstall
; Configuração da API — copiado para AppData apenas se não existir (preserva edições do usuário)
Source: "C:\dev\TesteAPI\Instalador\app_config.json"; DestDir: "{localappdata}\RDOApp"; Flags: onlyifdoesntexist uninsneveruninstall
; Windows App Runtime 1.8 — necessário para WinUI 3
Source: "C:\dev\TesteAPI\RDO.app\AppPackages\RDO.app_1.0.3.0_x64_Debug_Test\WindowsAppRuntime_Official.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
; Atalho no Menu Iniciar
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar RDO"; Filename: "{uninstallexe}"

; Atalho na Área de Trabalho (opcional)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Iniciar com Windows (opcional)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "RDO"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
; Instalar Windows App Runtime 1.8 (silencioso, obrigatório para WinUI 3)
Filename: "{tmp}\WindowsAppRuntime_Official.exe"; Parameters: "--quiet"; Flags: waituntilterminated runhidden; StatusMsg: "Instalando Windows App Runtime 1.8..."
; Abrir o app após instalar
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir RDO agora"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Limpar banco de dados local ao desinstalar (opcional — remova se quiser preservar os dados)
; Type: filesandordirs; Name: "{localappdata}\RDO"

[Code]
// Verifica se o Windows 10 1809+ está instalado
function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsWin64 then
  begin
    MsgBox('Este aplicativo requer Windows 10 de 64 bits (versão 1809 ou superior).', mbError, MB_OK);
    Result := False;
  end;
end;
