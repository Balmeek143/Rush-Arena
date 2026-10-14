$editorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
$proj = (Resolve-Path '.').Path
$logPath = Join-Path $proj 'TestResults\build_cloudflare.log'
$args = @('-batchmode', '-nographics', '-projectPath', $proj, '-logFile', $logPath, '-quit', '-executeMethod', 'RushArena.Editor.MultiplayerSetup.BuildCloudflareWebGL')

Write-Host "Starting Cloudflare WebGL Build..."
$proc = Start-Process -FilePath $editorPath -ArgumentList $args -WindowStyle Hidden -Wait -PassThru
if ($proc.ExitCode -ne 0) {
    Write-Error "Build failed with exit code $($proc.ExitCode). See $logPath"
    if (Test-Path $logPath) { Get-Content $logPath -Tail 40 }
    exit 1
}
Write-Host "Cloudflare WebGL Build completed successfully!"
