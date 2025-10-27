param([string]$FilePath)

# Ensure inbox exists
$inbox = ".devai\inbox"
if (!(Test-Path $inbox)) { New-Item -ItemType Directory $inbox | Out-Null }

# Read last ~200 lines of the file
$content = Get-Content $FilePath -Raw
$lines = $content -split "`n"
if ($lines.Count -gt 200) { $content = ($lines[-200..-1] -join "`n") }

# Build one-line JSON event
$event = @{
    kind    = "code-save"
    ts      = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    user    = $env:USERNAME
    rel     = $FilePath
    ext     = ".cs"
    content = $content
}

# Append to local log
$logFile = ".devai\inbox\save-events.jsonl"
$event | ConvertTo-Json -Compress | Add-Content -Path $logFile

Write-Host "DevAI: logged save for $FilePath"
