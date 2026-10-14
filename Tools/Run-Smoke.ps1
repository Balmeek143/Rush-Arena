$exePath = (Resolve-Path 'Builds/WindowsShared/RushArenaShared.exe').Path
$proc = Start-Process -FilePath $exePath -ArgumentList @('-rush-shared-smoke', '-rush-room', 'fresh-verification-room') -PassThru
Start-Sleep -Seconds 12
if (!$proc.HasExited) {
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
}

$logPath = "$env:USERPROFILE\AppData\LocalLow\RushArena\RUSH Arena\Player.log"
if (Test-Path $logPath) {
    Write-Host "=== Player.log (Last 60 lines) ==="
    Get-Content $logPath -Tail 60
} else {
    Write-Host "No Player.log found!"
}
