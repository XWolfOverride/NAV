# NAV para bash/zsh: al salir con Esc te deja en la carpeta donde estabas.
# Anadir a ~/.bashrc o ~/.zshrc:
#   source /ruta/a/NAV/shell/nav.sh
nav() {
    local tmp dir rc
    tmp="$(mktemp "${TMPDIR:-/tmp}/nav.XXXXXX")" || return 1
    command nav --cd-file "$tmp" "$@"
    rc=$?
    dir="$(cat -- "$tmp" 2>/dev/null)"
    rm -f -- "$tmp"
    # Shell abierta con Ctrl+O desde NAV: se cierra para volver a ese NAV.
    if [ "$rc" -eq 75 ] && [ -n "$NAV_RETURN_FILE" ]; then
        exit 0
    fi
    if [ -n "$dir" ] && [ "$dir" != "$PWD" ] && [ -d "$dir" ]; then
        cd -- "$dir" || return
    fi
    return $rc
}
