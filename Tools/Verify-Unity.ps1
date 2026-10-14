param([Parameter(Mandatory=$true)][string]$UnityPath)
$ErrorActionPreference = 'Stop'
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$editorPath = (Resolve-Path -LiteralPath $UnityPath).Path
$resultsPath = Join-Path $projectPath 'TestResults'
New-Item -ItemType Directory -Path $resultsPath -Force | Out-Null

function Invoke-Editor([string]$name, [string[]]$extra) {
    $logPath = Join-Path $resultsPath ($name + '.log')
    $arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $projectPath + '"'), '-logFile', ('"' + $logPath + '"')) + $extra
    $process = Start-Process -FilePath $editorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "$name exited $($process.ExitCode); see $logPath" }
    if (!(Test-Path -LiteralPath $logPath)) { throw "$name produced no editor log." }
    if (Select-String -LiteralPath $logPath -Pattern 'error CS\d+|Scripts have compiler errors|Failed to resolve packages') { throw "$name failed compilation/import; see $logPath" }
}

Invoke-Editor 'configure' @('-quit','-executeMethod','RushArena.Editor.MilestoneSetup.Configure')
foreach ($mode in @('EditMode','PlayMode')) {
    $xmlPath = Join-Path $resultsPath ($mode+'.xml')
    # Remove only this explicitly resolved previous result, to avoid mistaking stale success for a new run.
    if (Test-Path -LiteralPath $xmlPath) { Remove-Item -LiteralPath $xmlPath }
    Invoke-Editor $mode @('-runTests','-testPlatform',$mode,'-testResults',('"'+$xmlPath+'"'))
    if (!(Test-Path -LiteralPath $xmlPath)) { throw "$mode produced no test results." }
    [xml]$result = Get-Content -LiteralPath $xmlPath -Raw
    $run = $result.'test-run'
    if ($null -eq $run -or [int]$run.total -le 0 -or [int]$run.failed -ne 0 -or $run.result -ne 'Passed') {
        throw "$mode did not pass; inspect $xmlPath"
    }
    Write-Output "$mode passed: $($run.passed) tests."
}
Write-Output 'Unity automated checks passed. PC play, graphics/frame time and physical mobile checks remain separate.'
