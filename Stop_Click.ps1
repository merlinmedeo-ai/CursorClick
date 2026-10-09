$ErrorActionPreference = 'SilentlyContinue'
$pidFile = Join-Path $env:TEMP 'VolantesCursorClick.pid'
$readyFile = Join-Path $env:TEMP 'VolantesCursorClick.ready'
$scriptPath = (Join-Path $PSScriptRoot 'CursorClick.ps1')
$stopped = New-Object System.Collections.Generic.List[int]

# Stop the native application first.
Get-Process -Name 'CursorClick' -ErrorAction SilentlyContinue | ForEach-Object {
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    $stopped.Add($_.Id)
}

# 1) Stop the exact PID recorded by CursorClick.ps1.
if (Test-Path -LiteralPath $pidFile) {
    $savedPidText = (Get-Content -LiteralPath $pidFile -ErrorAction SilentlyContinue | Select-Object -First 1)
    $savedPid = 0
    if ([int]::TryParse([string]$savedPidText, [ref]$savedPid) -and $savedPid -gt 0) {
        $p = Get-Process -Id $savedPid -ErrorAction SilentlyContinue
        if ($p) {
            # Verify command line when possible, so a stale/reused PID does not kill an unrelated process.
            $safeToStop = $true
            try {
                $wmi = Get-CimInstance Win32_Process -Filter ("ProcessId=" + $savedPid) -ErrorAction Stop
                if ($wmi -and $wmi.CommandLine) {
                    $safeToStop = ($wmi.CommandLine -like '*CursorClick.ps1*')
                }
            } catch {}
            if ($safeToStop) {
                Stop-Process -Id $savedPid -Force -ErrorAction SilentlyContinue
                $stopped.Add($savedPid)
            }
        }
    }
}

# 2) Fallback: find any remaining PowerShell instance whose command line runs CursorClick.ps1.
$procs = @()
try {
    $procs = Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object {
        $_.ProcessId -ne $PID -and $_.CommandLine -and (
            $_.CommandLine -like '*CursorClick.ps1*' -or $_.CommandLine -like ('*' + $scriptPath + '*')
        )
    }
} catch {
    try {
        $procs = Get-WmiObject Win32_Process -ErrorAction Stop | Where-Object {
            $_.ProcessId -ne $PID -and $_.CommandLine -and $_.CommandLine -like '*CursorClick.ps1*'
        }
    } catch {}
}

foreach ($proc in $procs) {
    $id = [int]$proc.ProcessId
    if (-not $stopped.Contains($id)) {
        Stop-Process -Id $id -Force -ErrorAction SilentlyContinue
        $stopped.Add($id)
    }
}

Remove-Item -LiteralPath $readyFile -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue

if ($stopped.Count -gt 0) {
    Write-Output ('STOPPED ' + (($stopped | Sort-Object -Unique) -join ','))
    exit 0
}

Write-Output 'NOT_RUNNING'
exit 0
