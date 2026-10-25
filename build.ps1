# NAV - autocompilacion NativeAOT para Windows (PowerShell 5.1 o superior).
#
#   irm https://raw.githubusercontent.com/XWolfOverride/NAV/main/build.ps1 | iex
#
# Que hace:
#   1. Comprueba que estan instaladas las herramientas necesarias (NO instala nada).
#   2. Descarga (o actualiza) la ultima version del codigo con git.
#   3. Compila `nav` con NativeAOT.
#   4. Deja nav.exe en la raiz del proyecto y te explica como instalarlo.
#
# Variable opcional:
#   $env:NAV_REPO_URL   URL del repositorio (util para forks)
#
# Nota: todo va dentro de una funcion y se sale con `return`, para que al ejecutarlo con
# `| iex` un fallo no cierre tu ventana de PowerShell.

function Invoke-NavBuild {
    $ErrorActionPreference = 'Stop'

    $repoUrl = if ($env:NAV_REPO_URL) { $env:NAV_REPO_URL } else { 'https://github.com/XWolfOverride/NAV' }
    $dirName = 'NAV'

    function Ok($m)   { Write-Host "[OK] " -ForegroundColor Green -NoNewline; Write-Host $m }
    function Bad($m)  { Write-Host "[X]  " -ForegroundColor Red -NoNewline; Write-Host $m }
    function Info($m) { Write-Host ">>   " -ForegroundColor Cyan -NoNewline; Write-Host $m }

    Write-Host ""
    Write-Host "NAV: autocompilacion" -ForegroundColor White
    Write-Host ""

    # ---------------------------------------------------------- arquitectura
    $arch = $env:PROCESSOR_ARCHITEW6432
    if (-not $arch) { $arch = $env:PROCESSOR_ARCHITECTURE }
    switch ($arch) {
        'AMD64' { $rid = 'win-x64' }
        'ARM64' { $rid = 'win-arm64' }
        default { Write-Host "Arquitectura no soportada por el script: $arch" -ForegroundColor Red; return }
    }
    Info "Sistema detectado: $rid"
    Write-Host ""

    # ---------------------------------------------------------- prerrequisitos
    $missing = @()
    $hints = @()

    if (Get-Command git -ErrorAction SilentlyContinue) {
        Ok ("git (" + ((git --version) -replace 'git version ', '') + ")")
    } else {
        Bad 'git'; $missing += 'git'; $hints += 'winget install Git.Git'
    }

    $dotnetOk = $false
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        $ver = (& dotnet --version 2>$null)
        $major = 0
        if ($ver -and [int]::TryParse(($ver -split '\.')[0], [ref]$major) -and $major -ge 10) {
            Ok "SDK de .NET ($ver)"; $dotnetOk = $true
        } else {
            Bad "SDK de .NET: version $ver, se necesita 10 o superior"
        }
    } else {
        Bad 'SDK de .NET 10'
    }
    if (-not $dotnetOk) { $missing += 'SDK de .NET 10'; $hints += 'winget install Microsoft.DotNet.SDK.10' }

    # Compilador nativo: Build Tools / Visual Studio con la carga de trabajo de C++.
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $hasCpp = $false
    if (Test-Path $vswhere) {
        $vsPath = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath 2>$null
        if ($vsPath) { $hasCpp = $true }
    }
    if ($hasCpp) {
        Ok 'Compilador de C++ (Visual Studio Build Tools)'
        # ILCompiler invoca "vswhere.exe" por nombre: nos aseguramos de que este en el PATH de este proceso.
        $vsDir = Split-Path $vswhere
        if (($env:PATH -split ';') -notcontains $vsDir) { $env:PATH = "$env:PATH;$vsDir" }
    } else {
        Bad 'Compilador de C++ (Visual Studio Build Tools con "Desktop development with C++")'
        $missing += 'Compilador de C++'
        $hints += 'winget install Microsoft.VisualStudio.2022.BuildTools --override "--passive --wait --add Microsoft.VisualStudio.Workload.VCTools --includeRecommended"'
    }

    if ($missing.Count -gt 0) {
        Write-Host ""
        Write-Host ("Faltan herramientas necesarias: " + ($missing -join ', ')) -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Instalalas con (este script no instala nada por ti):"
        Write-Host ""
        foreach ($h in $hints) { Write-Host "    $h" }
        Write-Host ""
        Write-Host "Cierra y vuelve a abrir PowerShell tras instalarlas y repite este comando."
        Write-Host "Detalles en el README del proyecto."
        return
    }

    # ---------------------------------------------------------- codigo fuente
    Write-Host ""
    $here = (Get-Location).Path

    function Update-Repo($path) {
        # Sin remoto (copia local aun sin publicar) no hay nada que actualizar.
        if (-not (git -C $path remote)) {
            Info "El repositorio no tiene remoto configurado: se compila el codigo tal como esta."
            return $true
        }
        git -C $path pull --ff-only
        if ($LASTEXITCODE -ne 0) {
            Write-Host "No se pudo actualizar (hay cambios locales?). Resuelvelo y repite." -ForegroundColor Red
            return $false
        }
        return $true
    }

    if ((Test-Path (Join-Path $here 'Nav.slnx')) -and (Test-Path (Join-Path $here '.git'))) {
        $root = $here
        Info "Usando el repositorio actual: $root"
        if (-not (Update-Repo $root)) { return }
    } elseif (Test-Path (Join-Path $here "$dirName\.git")) {
        $root = Join-Path $here $dirName
        Info "Actualizando $root"
        if (-not (Update-Repo $root)) { return }
    } else {
        $root = Join-Path $here $dirName
        Info "Descargando $repoUrl"
        git clone --depth 1 $repoUrl $root
        if ($LASTEXITCODE -ne 0) { Write-Host "No se pudo clonar el repositorio." -ForegroundColor Red; return }
    }

    # ---------------------------------------------------------- compilacion
    $env:DOTNET_NOLOGO = '1'
    $pub = Join-Path $root 'publish'
    if (Test-Path $pub) { Remove-Item $pub -Recurse -Force }

    $proj = Join-Path $root 'src\Nav\Nav.csproj'
    $out = Join-Path $pub 'nav'
    Info "Compilando nav (NativeAOT, $rid)... puede tardar un par de minutos"
    dotnet publish $proj -c Release -r $rid -o $out -nologo -v q
    if ($LASTEXITCODE -ne 0) { Write-Host "Fallo la compilacion de nav." -ForegroundColor Red; return }
    $built = Join-Path $out 'nav.exe'
    if (-not (Test-Path $built)) { Write-Host "No se encontro nav.exe tras compilar." -ForegroundColor Red; return }
    Copy-Item $built (Join-Path $root 'nav.exe') -Force
    Remove-Item $pub -Recurse -Force
    Ok "nav.exe -> $(Join-Path $root 'nav.exe')"

    # ---------------------------------------------------------- final
    $f = Get-Item (Join-Path $root 'nav.exe')
    Write-Host ""
    Write-Host "Listo. " -ForegroundColor Green -NoNewline
    Write-Host "El programa esta en: $($f.FullName)"
    Write-Host ("    {0,8:N1} MB  nav.exe" -f ($f.Length / 1MB))
    Write-Host ""
    Write-Host "Para instalarlo en tu PATH (sin administrador), ejecuta:" -ForegroundColor White
    Write-Host ""
    Write-Host "    cd `"$root`""
    Write-Host '    mkdir $HOME\bin -Force | Out-Null'
    Write-Host '    copy nav.exe $HOME\bin'
    Write-Host '    $p = [Environment]::GetEnvironmentVariable("Path", "User")'
    Write-Host '    [Environment]::SetEnvironmentVariable("Path", "$p;$HOME\bin", "User")'
    Write-Host ""
    Write-Host "Para quedarte en la carpeta al salir (y poder usar Ctrl+O), anade a tu perfil (`$PROFILE):" -ForegroundColor White
    Write-Host ""
    Write-Host "    . `"$(Join-Path $root 'shell\nav.ps1')`""
    Write-Host ""
    Write-Host "Cierra y vuelve a abrir la terminal y pruebalo con:  nav"
    Write-Host "Para actualizar a una nueva version, vuelve a ejecutar este script."
    Write-Host ""
}

Invoke-NavBuild
