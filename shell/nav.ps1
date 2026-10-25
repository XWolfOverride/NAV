# NAV para PowerShell: al salir con Esc te deja en la carpeta donde estabas.
# Anadir al perfil ($PROFILE):
#   . C:\ruta\a\NAV\shell\nav.ps1
function nav {
    $exe = Get-Command -Name nav -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $exe) { Write-Error "nav no esta en el PATH"; return }
    $tmp = [System.IO.Path]::GetTempFileName()
    try {
        & $exe.Source --cd-file $tmp @args
        $rc = $LASTEXITCODE
        $dir = Get-Content -LiteralPath $tmp -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        if ($dir) {
            $dir = $dir.Trim()
            if (Test-Path -LiteralPath $dir -PathType Container) { Set-Location -LiteralPath $dir }
        }
    }
    finally {
        Remove-Item -LiteralPath $tmp -ErrorAction SilentlyContinue
    }
    # Shell abierta con Ctrl+O desde NAV: se cierra para volver a ese NAV.
    if ($rc -eq 75 -and $env:NAV_RETURN_FILE) { exit }
}
