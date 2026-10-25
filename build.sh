#!/usr/bin/env bash
#
# NAV — autocompilación NativeAOT para Linux y macOS.
#
#   curl -fsSL https://raw.githubusercontent.com/XWolfOverride/NAV/main/build.sh | bash
#
# Qué hace:
#   1. Comprueba que están instaladas las herramientas necesarias (NO instala nada).
#   2. Descarga (o actualiza) la última versión del código con git.
#   3. Compila `nav` con NativeAOT.
#   4. Deja el binario en la raíz del proyecto y te explica cómo instalarlo.
#
# Variables opcionales:
#   NAV_REPO_URL   URL del repositorio (útil para forks)

set -euo pipefail

REPO_URL="${NAV_REPO_URL:-https://github.com/XWolfOverride/NAV}"
DIR_NAME="NAV"

if [ -t 1 ]; then
    B=$'\e[1m'; G=$'\e[32m'; R=$'\e[31m'; Y=$'\e[33m'; C=$'\e[36m'; N=$'\e[0m'
else
    B=''; G=''; R=''; Y=''; C=''; N=''
fi

ok()   { printf '%s✔%s %s\n' "$G" "$N" "$*"; }
bad()  { printf '%s✖%s %s\n' "$R" "$N" "$*"; }
info() { printf '%s»%s %s\n' "$C" "$N" "$*"; }
die()  { printf '\n%s%s%s\n' "$R" "$*" "$N" >&2; exit 1; }

echo
echo "${B}NAV — autocompilación${N}"
echo

# ---------------------------------------------------------------- sistema
OS="$(uname -s)"
case "$OS" in
    Linux)  PREFIX="linux" ;;
    Darwin) PREFIX="osx" ;;
    *) die "Sistema no soportado por este script: $OS. En Windows usa build.ps1." ;;
esac

case "$(uname -m)" in
    x86_64|amd64)  RID="$PREFIX-x64" ;;
    aarch64|arm64) RID="$PREFIX-arm64" ;;
    *) die "Arquitectura no soportada por el script: $(uname -m)" ;;
esac
info "Sistema detectado: $RID"
echo

# ---------------------------------------------------------------- gestor de paquetes (solo para sugerir)
PM=""
if [ "$OS" = "Darwin" ]; then
    PM="brew"
elif command -v apt-get >/dev/null 2>&1; then
    PM="apt"
elif command -v dnf >/dev/null 2>&1; then
    PM="dnf"
elif command -v pacman >/dev/null 2>&1; then
    PM="pacman"
fi

# ---------------------------------------------------------------- prerrequisitos
missing=()      # descripción de lo que falta
pkgs=()         # paquetes del gestor del sistema que lo resolverían
manual=()       # lo que no se resuelve con el gestor

if command -v git >/dev/null 2>&1; then
    ok "git ($(git --version | awk '{print $3}'))"
else
    bad "git"; missing+=("git"); pkgs+=("git")
fi

dotnet_ok=0
if command -v dotnet >/dev/null 2>&1; then
    dotnet_ver="$(dotnet --version 2>/dev/null || true)"
    dotnet_major="${dotnet_ver%%.*}"
    if [[ "$dotnet_major" =~ ^[0-9]+$ ]] && [ "$dotnet_major" -ge 10 ]; then
        ok "SDK de .NET ($dotnet_ver)"
        dotnet_ok=1
    else
        bad "SDK de .NET: versión ${dotnet_ver:-desconocida}, se necesita 10 o superior"
    fi
else
    bad "SDK de .NET 10"
fi
if [ "$dotnet_ok" -eq 0 ]; then
    missing+=("SDK de .NET 10")
    if [ "$OS" = "Darwin" ]; then
        manual+=("brew install --cask dotnet-sdk")
    else
        manual+=("SDK de .NET 10: https://learn.microsoft.com/dotnet/core/install/linux (o: curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0)")
    fi
fi

if [ "$OS" = "Darwin" ]; then
    # En macOS el compilador y el enlazador vienen con las Command Line Tools de Xcode.
    if xcode-select -p >/dev/null 2>&1 && command -v clang >/dev/null 2>&1; then
        ok "Xcode Command Line Tools"
    else
        bad "Xcode Command Line Tools"; missing+=("Xcode Command Line Tools"); manual+=("xcode-select --install")
    fi
else
    if command -v clang >/dev/null 2>&1; then
        ok "clang ($(clang --version | head -n1 | sed -E 's/.*version ([0-9.]+).*/\1/'))"
    else
        bad "clang"; missing+=("clang"); pkgs+=("clang")
    fi

    if command -v ld >/dev/null 2>&1; then
        ok "enlazador (ld)"
    else
        bad "enlazador (ld)"; missing+=("binutils")
        case "$PM" in
            apt)    pkgs+=("build-essential") ;;
            dnf)    pkgs+=("binutils") ;;
            pacman) pkgs+=("base-devel") ;;
            *)      pkgs+=("binutils") ;;
        esac
    fi

    have_zlib=0
    if [ -f /usr/include/zlib.h ]; then
        have_zlib=1
    elif command -v clang >/dev/null 2>&1 \
         && printf '#include <zlib.h>\nint main(void){return 0;}\n' | clang -x c -fsyntax-only - >/dev/null 2>&1; then
        have_zlib=1
    fi
    if [ "$have_zlib" -eq 1 ]; then
        ok "cabeceras de zlib"
    else
        bad "cabeceras de zlib"; missing+=("zlib (cabeceras)")
        case "$PM" in
            apt)    pkgs+=("zlib1g-dev") ;;
            dnf)    pkgs+=("zlib-devel") ;;
            pacman) pkgs+=("zlib") ;;
            *)      pkgs+=("zlib (cabeceras de desarrollo)") ;;
        esac
    fi
fi

if [ "${#missing[@]}" -gt 0 ]; then
    echo
    printf '%sFaltan herramientas necesarias:%s %s\n' "$Y" "$N" "$(IFS=,; echo "${missing[*]}" | sed 's/,/, /g')"
    echo
    if [ "${#pkgs[@]}" -gt 0 ]; then
        echo "Instálalas con (este script no instala nada por ti):"
        echo
        case "$PM" in
            apt)    echo "    sudo apt update && sudo apt install -y ${pkgs[*]}" ;;
            dnf)    echo "    sudo dnf install -y ${pkgs[*]}" ;;
            pacman) echo "    sudo pacman -S --needed ${pkgs[*]}" ;;
            brew)   echo "    brew install ${pkgs[*]}" ;;
            *)      echo "    (con el gestor de paquetes de tu sistema) ${pkgs[*]}" ;;
        esac
        echo
    fi
    for m in "${manual[@]}"; do
        echo "  · $m"
    done
    echo
    echo "Después vuelve a ejecutar este script. Detalles en el README del proyecto."
    exit 1
fi

# ---------------------------------------------------------------- código fuente
echo
update() {
    # Sin remoto (copia local aún sin publicar) no hay nada que actualizar.
    if [ -n "$(git -C "$1" remote)" ]; then
        git -C "$1" pull --ff-only || die "No se pudo actualizar (¿hay cambios locales?). Resuélvelo y repite."
    else
        info "El repositorio no tiene remoto configurado: se compila el código tal como está."
    fi
}

if [ -f Nav.slnx ] && [ -d .git ]; then
    ROOT="$PWD"
    info "Usando el repositorio actual: $ROOT"
    update "$ROOT"
elif [ -d "$DIR_NAME/.git" ]; then
    ROOT="$PWD/$DIR_NAME"
    info "Actualizando $ROOT"
    update "$ROOT"
else
    ROOT="$PWD/$DIR_NAME"
    info "Descargando $REPO_URL"
    git clone --depth 1 "$REPO_URL" "$ROOT" || die "No se pudo clonar el repositorio."
fi

# ---------------------------------------------------------------- compilación
export DOTNET_NOLOGO=1
export DOTNET_CLI_UI_LANGUAGE=en
rm -rf "$ROOT/publish"

info "Compilando nav (NativeAOT, $RID)... puede tardar un minuto"
dotnet publish "$ROOT/src/Nav/Nav.csproj" -c Release -r "$RID" \
    -o "$ROOT/publish/nav" -nologo -v q \
    || die "Falló la compilación de nav."
[ -f "$ROOT/publish/nav/nav" ] || die "No se encontró el binario nav tras compilar."
cp -f "$ROOT/publish/nav/nav" "$ROOT/nav"
chmod +x "$ROOT/nav"
rm -rf "$ROOT/publish"
ok "nav → $ROOT/nav"

# ---------------------------------------------------------------- final
case "${SHELL:-}" in
    */zsh) RC="~/.zshrc" ;;
    *)     RC="~/.bashrc" ;;
esac

echo
echo "${B}${G}Listo.${N} El binario está en: ${B}$ROOT/nav${N}"
ls -lh "$ROOT/nav" | awk '{printf "    %s  %s\n", $5, $9}'
echo
echo "${B}Para instalarlo${N} elige una opción (desde ${C}$ROOT${N}):"
echo
echo "  ${B}Solo para tu usuario${N} (sin sudo; asegúrate de que ~/.local/bin está en tu PATH):"
echo "    mkdir -p ~/.local/bin && install -m 755 nav ~/.local/bin/"
echo
echo "  ${B}Para todos los usuarios${N}:"
echo "    sudo install -m 755 nav /usr/local/bin/"
echo
echo "${B}Para quedarte en la carpeta al salir${N} (y poder usar Ctrl+O), añade a $RC:"
echo "    source $ROOT/shell/nav.sh"
echo
echo "Pruébalo con:  nav"
echo "Para actualizar a una nueva versión, vuelve a ejecutar este script."
echo
