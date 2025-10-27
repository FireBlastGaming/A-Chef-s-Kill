# devai-watch.ps1
param(
  [string]$Root = (Get-Location).Path
)

# Build paths safely
$AssetsPath = Join-Path $Root "Assets"
if (!(Test-Path $AssetsPath)) {
  Write-Host "ERROR: Couldn't find '$AssetsPath'. Start this script from the folder that contains Assets/." -ForegroundColor Red
  exit 1
}

$Inbox = Join-Path $Root ".devai\inbox"
if (!(Test-Path $Inbox)) { New-Item -ItemType Directory $Inbox | Out-Null }
$LogFile = Join-Path $Inbox "save-events.jsonl"

$LastWriteTable = @{}
$DebounceMs = 400

function Write-JsonLine([hashtable]$obj) {
  $obj | ConvertTo-Json -Compress -Depth 6 | Add-Content -Path $LogFile
}

function Get-FileTail([string]$path, [int]$lines = 200) {
  if (!(Test-Path $path)) { return "" }
  try {
    $all = Get-Content -LiteralPath $path -Raw
    $arr = $all -split "`n"
    if ($arr.Count -le $lines) { return $all }
    return ($arr[-$lines..-1] -join "`n")
  } catch { return "" }
}

# Watch Assets recursively for *.cs
$fsw = New-Object IO.FileSystemWatcher $AssetsPath, "*.cs"
$fsw.IncludeSubdirectories = $true
$fsw.NotifyFilter = [IO.NotifyFilters]'FileName, LastWrite, Size'

$action = {
  $assetsPath = $Event.MessageData  # injected below
  $path = $Event.SourceEventArgs.FullPath
  if (!(Test-Path $path)) { return }

  $now = Get-Date
  $key = $path.ToLower()
  if ($LastWriteTable.ContainsKey($key)) {
    $delta = ($now - $LastWriteTable[$key]).TotalMilliseconds
    if ($delta -lt $DebounceMs) { return }
  }
  $LastWriteTable[$key] = $now

  $contentTail = Get-FileTail -path $path -lines 200
  # Relative path from project root
  $rel = $path.Replace((Join-Path (Split-Path $assetsPath -Parent) ''), '').TrimStart('\').Replace('\','/')

  Write-JsonLine @{
    kind="code-save"; ts=[DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    user=$env:USERNAME; rel=$rel; ext=".cs"; content=$contentTail
  }
  Write-Host "Logged save: $rel"
}

# Pass $AssetsPath to the action as MessageData
Register-ObjectEvent $fsw Changed -Action $action -MessageData $AssetsPath | Out-Null
Register-ObjectEvent $fsw Created -Action $action -MessageData $AssetsPath | Out-Null
Register-ObjectEvent $fsw Renamed -Action $action -MessageData $AssetsPath | Out-Null

Write-Host "DevAI watcher running. Watching: $AssetsPath\*.cs (recursive)"
Write-Host "Press Ctrl+C to stop."
while ($true) { Start-Sleep -Seconds 1 }
