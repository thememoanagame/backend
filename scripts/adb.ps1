<#
.SYNOPSIS
    Script para conectar ao ADB via Wi-Fi/TCP.
.DESCRIPTION
    Configura o dispositivo Android conectado via USB para escutar conexões TCP na porta especificada e conecta ao IP fornecido.
#>

Param(
    [Parameter(Mandatory = $true, HelpMessage = "Informe o IP e porta do celular no formato IP:PORTA (ex: 192.168.1.50:5555)")]
    [ValidatePattern('^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?):([0-9]{1,5})$')]
    [string]$Target
)

# Separando IP e Porta
$IpAddress, $Port = $Target.Split(':')

# Validação do número da porta
if ([int]$Port -lt 1 -or [int]$Port -gt 65535) {
    Write-Error "A porta fornecida ($Port) é inválida. Informe um valor entre 1 e 65535."
    exit
}

# Verificando se o ADB está disponível no sistema
if (-not (Get-Command "adb" -ErrorAction SilentlyContinue)) {
    Write-Error "O utilitário 'adb' não foi encontrado no PATH do sistema. Certifique-se de que o Android SDK Platform-Tools está instalado."
    exit
}

# Verificando se há pelo menos um dispositivo USB conectado
Write-Host "Verificando dispositivos USB conectados..." -ForegroundColor Cyan
$devices = adb devices | Select-String -Pattern "\bdevice\b"

if (-not $devices) {
    Write-Error "Nenhum dispositivo Android foi detectado via USB com a depuração ativada."
    exit
}

Write-Host "Dispositivo USB encontrado. Alterando modo ADB para TCPIP na porta $Port..." -ForegroundColor Yellow
adb tcpip $Port

if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha ao definir o modo TCP/IP no dispositivo."
    exit
}

# Pequena pausa para reinicialização do serviço de depuração no dispositivo
Start-Sleep -Seconds 2

Write-Host "Conectando ao dispositivo em $Target..." -ForegroundColor Yellow
adb connect $Target

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nConexão estabelecida com sucesso! Você já pode desconectar o cabo USB." -ForegroundColor Green
    adb devices
} else {
    Write-Error "Falha ao conectar ao endereço $Target. Verifique se o celular está na mesma rede Wi-Fi."
}