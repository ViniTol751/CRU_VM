# Script para gerar o banco SQLite inicial com todos os usuários pré-cadastrados
# Executa as migrations e insere os usuários com senha F0cus@2026!
# Hash SHA256 de F0cus@2026!: 7A1CD40647CC2F9DF2F715A309DFB96D05E9FD816513FAA3B2EBF43485F9706B

$dbPath = "C:\dev\TesteAPI\Instalador\rdo_inicial.db"
$hash   = "7A1CD40647CC2F9DF2F715A309DFB96D05E9FD816513FAA3B2EBF43485F9706B"

# Remove banco anterior se existir
if (Test-Path $dbPath) { Remove-Item $dbPath -Force }

Write-Host "Gerando banco inicial em: $dbPath" -ForegroundColor Cyan

# Gera o banco via migrations do projeto RDO.Data
$env:RDO_DB_PATH = $dbPath
dotnet ef database update --project "C:\dev\TesteAPI\RDO.Data\RDO.Data.csproj" --startup-project "C:\dev\TesteAPI\RDO.app\RDO.app.csproj"

Write-Host "Inserindo usuarios..." -ForegroundColor Cyan

# Insere todos os usuarios usando sqlite3 via ADO.NET
Add-Type -Path "C:\dev\TesteAPI\RDO.app\publish\unpackaged\Microsoft.Data.Sqlite.dll"

$conn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$dbPath")
$conn.Open()

$usuarios = @(
    @{ Nome="Aroldo Daiola Borges";                     Email="aroldo.borges";       Perfil="Technician" },
    @{ Nome="Bernalize do Rosario Vila Nova Marcolino";  Email="bernalize.marcolino"; Perfil="Technician" },
    @{ Nome="Bruno Felix Alcantara Souza";               Email="bruno.felix";         Perfil="Technician" },
    @{ Nome="Bruno Pires Ribeiro";                       Email="bruno.pires";         Perfil="Technician" },
    @{ Nome="Edson Martins Garcia";                      Email="edson.martins";       Perfil="Technician" },
    @{ Nome="Felipe Aparecido do Prado";                 Email="felipe.aparecido";    Perfil="Technician" },
    @{ Nome="Felipe Franco de Paula";                    Email="felipe.franco";       Perfil="Technician" },
    @{ Nome="Felipe Goncalves Duarte";                   Email="felipe.goncalves";    Perfil="Technician" },
    @{ Nome="Focus da Silva Junior";                     Email="focus.silva";         Perfil="Technician" },
    @{ Nome="Gabriel DAlessandro Bravo";                 Email="gabriel.bravo";       Perfil="Technician" },
    @{ Nome="Gabriel Favareli Furtado";                  Email="gabriel.favareli";    Perfil="Technician" },
    @{ Nome="Gabriel Margato";                           Email="gabriel.margato";     Perfil="Technician" },
    @{ Nome="Gustavo Henrique Aristeu de Queiroz";       Email="gustavo.henrique";    Perfil="Technician" },
    @{ Nome="Jose Henrique David Alves de Oliveira";     Email="jose.henrique";       Perfil="Technician" },
    @{ Nome="Juliana Bertoni Justino";                   Email="juliana.bertoni";     Perfil="Technician" },
    @{ Nome="Luis Felipe Xavier";                        Email="luis.felipe";         Perfil="Technician" },
    @{ Nome="Maicon Salomao Caetano";                    Email="maicon.salomao";      Perfil="Technician" },
    @{ Nome="Marcus Vinicius Ataide";                    Email="marcus.vinicius";     Perfil="Technician" },
    @{ Nome="Murillo Vitto Reis Pereira";                Email="murillo.vitto";       Perfil="Technician" },
    @{ Nome="Murilo Leandro Franco";                     Email="murilo.leandro";      Perfil="Technician" },
    @{ Nome="Natan Lemes Saura";                         Email="natan.lemes";         Perfil="Technician" },
    @{ Nome="Rafael Feitosa da Silva";                   Email="rafael.feitosa";      Perfil="Technician" },
    @{ Nome="Roberto Tilhaqui Junior";                   Email="roberto.tilhaqui";    Perfil="Technician" },
    @{ Nome="Simone Schuindt Martins";                   Email="simone.schuindt";     Perfil="Technician" },
    @{ Nome="Thales Garcia Neubern";                     Email="thales.garcia";       Perfil="Technician" },
    @{ Nome="Victor Almeida Arantes Vilela";             Email="victor.almeida";      Perfil="Technician" },
    @{ Nome="Vinicius Toledo de Carvalho";               Email="vinicius.toledo";     Perfil="Admin"      },
    @{ Nome="Wellington Henrique de Bessa Bortolozo";    Email="wellington.henrique"; Perfil="Technician" },
    @{ Nome="Wesley Danilo de Araujo";                   Email="wesley.danilo";       Perfil="Technician" },
    @{ Nome="Wesley Gregorio dos Santos";                Email="wesley.gregorio";     Perfil="Technician" }
)

$now = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")

foreach ($u in $usuarios) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
INSERT OR IGNORE INTO User (Name, Email, PasswordHash, Profile, IsActive, IsDeleted, UpdatedAt, Nome, SenhaHash, Perfil, Ativo)
VALUES (@nome, @email, @hash, @perfil, 1, 0, @now, @nome, @hash, @perfil, 1)
"@
    $cmd.Parameters.AddWithValue("@nome",   $u.Nome)   | Out-Null
    $cmd.Parameters.AddWithValue("@email",  $u.Email)  | Out-Null
    $cmd.Parameters.AddWithValue("@hash",   $hash)     | Out-Null
    $cmd.Parameters.AddWithValue("@perfil", $u.Perfil) | Out-Null
    $cmd.Parameters.AddWithValue("@now",    $now)      | Out-Null
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Host "  + $($u.Email)" -ForegroundColor Green
}

$conn.Close()
Write-Host "`nBanco gerado com sucesso: $dbPath" -ForegroundColor Green
Write-Host "Total de usuarios: $($usuarios.Count)" -ForegroundColor Green
