param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$editorPath = (Resolve-Path -LiteralPath $UnityPath).Path
$resultsPath = Join-Path $projectPath 'TestResults'
New-Item -ItemType Directory -Path $resultsPath -Force | Out-Null

function Run-UnityMethod([string]$methodName, [string]$logName) {
    Write-Host "Executing Unity method: $methodName..."
    $logPath = Join-Path $resultsPath ($logName + '.log')
    $arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $projectPath + '"'), '-logFile', ('"' + $logPath + '"'), '-quit', '-executeMethod', $methodName)
    $process = Start-Process -FilePath $editorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        Write-Error "$methodName exited with code $($process.ExitCode). See $logPath"
        if (Test-Path $logPath) { Get-Content $logPath -Tail 40 }
        exit 1
    }
    Write-Host "Success: $methodName"
}

Run-UnityMethod 'RushArena.Editor.MultiplayerSetup.Configure' 'multiplayer_configure'
Run-UnityMethod 'RushArena.Editor.MultiplayerSetup.BuildWindows' 'build_windows'
Run-UnityMethod 'RushArena.Editor.MultiplayerSetup.BuildCloudflareWebGL' 'build_cloudflare_webgl'
Run-UnityMethod 'RushArena.Editor.MultiplayerSetup.BuildWebGL' 'build_webgl'

Write-Host "All builds completed successfully!"
