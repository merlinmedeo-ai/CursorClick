$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $root 'BUILD.ps1')
$dist = Join-Path $root 'dist'
$portable = Join-Path $dist 'CursorClick-Portable'
$zip = Join-Path $dist 'CursorClick-Portable.zip'
if (Test-Path $portable) { Remove-Item -LiteralPath $portable -Recurse -Force }
if (Test-Path $zip) { Remove-Item -LiteralPath $zip -Force }
New-Item -ItemType Directory -Path $portable -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'CursorClick.exe') -Destination $portable
Copy-Item -LiteralPath (Join-Path $root 'README.txt') -Destination $portable
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination $portable
Copy-Item -LiteralPath (Join-Path $root 'INSTALL.ps1') -Destination $portable
Copy-Item -LiteralPath (Join-Path $root 'UNINSTALL.ps1') -Destination $portable
Compress-Archive -Path (Join-Path $portable '*') -DestinationPath $zip -CompressionLevel Optimal
Write-Host "Portable package: $zip" -ForegroundColor Green
