param(
    [Parameter(Mandatory = $true)]
    [string]$ProcessName,

    [ValidateRange(0, 1)]
    [float]$Volume = 1
)

$ErrorActionPreference = "Stop"

$naudioCore = Join-Path $env:USERPROFILE ".nuget\packages\naudio.core\2.2.1\lib\netstandard2.0\NAudio.Core.dll"
$naudioWasapi = Join-Path $env:USERPROFILE ".nuget\packages\naudio.wasapi\2.2.1\lib\netstandard2.0\NAudio.Wasapi.dll"
Add-Type -Path $naudioCore
Add-Type -Path $naudioWasapi

$enumerator = [NAudio.CoreAudioApi.MMDeviceEnumerator]::new()
try {
    $device = $enumerator.GetDefaultAudioEndpoint(
        [NAudio.CoreAudioApi.DataFlow]::Render,
        [NAudio.CoreAudioApi.Role]::Multimedia)

    $device.AudioSessionManager.RefreshSessions()
    $sessions = $device.AudioSessionManager.Sessions
    for ($i = 0; $i -lt $sessions.Count; $i++) {
        $session = $sessions[$i]
        try {
            $processId = [int]$session.GetProcessID
            if ($processId -le 0) { continue }
            $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
            if ($process -and $process.ProcessName -ieq [IO.Path]::GetFileNameWithoutExtension($ProcessName)) {
                $session.SimpleAudioVolume.Volume = $Volume
                Write-Host "Set $($process.ProcessName) PID $processId to $Volume"
            }
        }
        finally {
            $session.Dispose()
        }
    }
}
finally {
    if ($device) { $device.Dispose() }
    $enumerator.Dispose()
}
