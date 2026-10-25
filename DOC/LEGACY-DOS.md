# Código original para DOS (`legacy-dos/`)

NAV nació como un navegador rápido de carpetas y lanzador de archivos para DOS, escrito en
Turbo Pascal 7 por XWolfOverride (2019-2021, licencia MIT). Se conserva solo como
**referencia**: no forma parte de la solución .NET ni se compila con ella. La codificación de
los fuentes es CP437.

## Contenido

| Ruta | Qué es |
|---|---|
| `NAV.PAS` | Programa principal (~780 líneas). Define conmutadores de compilación: `SPANISH`/`ENGLISH`, `INFOBOX`, `TEXT_READER` (WIP), `LOWRAM` y `DEBUG_*`. |
| `TUI.PAS`, `x/TUI.PAS` | Toolkit de interfaz de texto. |
| `x/CRTx.pas` | Extensiones de la unit CRT. |
| `x/*.PAS`, `*.DCU` | Units propias (`XDISK`, `XCLASSES`, `XWOLF`, `ENUMRATR`...). Varias (`MPEG`, `WinMM`, `Ximage`, `XIMGLIB`, `XREG`, `WINGDOWS`) parecen de otros proyectos Windows/Delphi; no se ha verificado cuáles usa realmente `NAV.PAS`. |
| `mk.bat` | Script de compilación con `tpc` (rutas del equipo original). |
| `NAV.EXE`, `NAVOLD.EXE`, `NAV.BIN`, `GBASURA.*`, `ERR.BAT`, `MEMTEST.BAT` | Binarios y utilidades auxiliares de la época. |

## Qué se conservó en la versión C#

- **Concepto:** rejilla de nombres en columnas dentro de un marco, fondo azul, selección por
  teclado, ejecución al pulsar Enter.
- **Orden del listado:** carpetas, ejecutables, scripts, resto.
- **Búsqueda rápida por prefijo** al teclear.
- **Línea de información (`INFOBOX`)** con el elemento seleccionado.
- **Opciones por letras** y variable de entorno de opciones (`LSOPT` → `NAVOPT`).
- **Textos en dos idiomas** (`DEFINE SPANISH/ENGLISH` → `Strings.cs`).
- **Guardar/restaurar pantalla** (SaveScreen/RestoreScreen → pantalla alternativa ANSI).

## Qué cambió a propósito

- Se eliminó el modo blanco y negro (opción `-m`).
- El directorio actual en DOS era global y persistía al salir; en un sistema moderno cada
  proceso tiene el suyo, de ahí la necesidad de `--cd-file` y las funciones de `shell/`
  (ver [INTEGRACION-SHELL.md](INTEGRACION-SHELL.md)).
- La búsqueda añade una segunda pasada por contenido del nombre, y el diseño en columnas se
  adapta al tamaño de la terminal.
- Ctrl+O (shell en la carpeta actual con retorno al NAV padre) es nuevo.

Al consultar el original, no copiar su implementación: la reescritura conserva el concepto,
no el código.
