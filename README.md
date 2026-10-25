# NAV

Navegador de carpetas rápido para consola. Versión moderna y multiplataforma
(Windows, Linux, macOS, también por SSH) del NAV original para DOS escrito en Turbo Pascal 7.

```
┌[/home/ivan/proyectos]──────┬──────────────────────────────┬────────────┐
│.git/                       │legacy-dos/                   │src/        │
│build.sh                    │LICENSE                       │README.md   │
├────────────────────────────┴──────────────────────────────┴────────────┤
│ src_____________________  Carpeta                 2026-10-24 19:15 3/6 │
└[F1 Ayuda]────────────────────────────────────────[NAV 2.0 by XWolf]────┘
```

Ves el contenido de la carpeta en columnas, te mueves con el teclado, entras en carpetas,
lanzas programas y scripts, y al salir puedes quedarte en la carpeta donde estabas.

> **Estado:** compila y tiene pruebas unitarias; la interfaz aún no se ha probado a mano en
> todas las terminales (ver [DOC/COMPILACION-Y-PRUEBAS.md](DOC/COMPILACION-Y-PRUEBAS.md)).

## Compilar desde fuentes

NAV no publica binarios: se compila en tu máquina con un solo comando. El script comprueba
que tienes lo necesario (**no instala nada**; si falta algo, te dice qué y cómo instalarlo),
descarga la última versión, compila con Native AOT y deja el ejecutable (`nav` / `nav.exe`)
en la raíz del proyecto. El binario no necesita .NET para ejecutarse.

Necesitas: git, el SDK de .NET 10 y un compilador nativo (Visual Studio Build Tools con
«Desarrollo para escritorio con C++» en Windows; `clang`, `zlib` y un enlazador en Linux;
Xcode Command Line Tools en macOS).

```powershell
irm https://raw.githubusercontent.com/XWolfOverride/NAV/main/build.ps1 | iex                # Windows
```
```bash
curl -fsSL https://raw.githubusercontent.com/XWolfOverride/NAV/main/build.sh | bash         # Linux y macOS
```

Crea la carpeta `NAV/` en el directorio actual. **Para actualizar a una nueva versión, vuelve a
ejecutar el mismo comando** (o `./build.sh` / `.\build.ps1` dentro de la carpeta): hace `git pull`
y recompila. El script termina explicando cómo instalar el ejecutable en tu `PATH`:

```powershell
# Windows (PowerShell, sin administrador); después reabre la terminal
mkdir $HOME\bin -Force; copy nav.exe $HOME\bin
$p = [Environment]::GetEnvironmentVariable("Path","User"); [Environment]::SetEnvironmentVariable("Path","$p;$HOME\bin","User")
```
```bash
# Linux / macOS (para todos los usuarios: sudo install -m 755 nav /usr/local/bin/)
mkdir -p ~/.local/bin && install -m 755 nav ~/.local/bin/
```

> Ejecutar un script descargado requiere confiar en él: léelo antes en [`build.sh`](build.sh) /
> [`build.ps1`](build.ps1) o descárgalo y ejecútalo tú.

Configura después el [cambio de carpeta al salir](#quedarte-en-la-carpeta-al-salir).

Para desarrollar: `dotnet build`, `dotnet test` y `dotnet run --project src/Nav`.
## Uso

```
nav [ruta] [-a] [-d] [-h] [--cd-file FICHERO] [--new] [--version]
```

| Opción | Efecto |
|---|---|
| `ruta` | Carpeta inicial (por defecto, la actual) |
| `-a` | Mostrar archivos ocultos |
| `-d` | Mostrar solo carpetas |
| `-h`, `--help` | Ayuda |
| `--cd-file F` | Al salir, escribe en `F` la carpeta final (lo usan los scripts de shell) |
| `--new` | Abrir un NAV nuevo aunque estés en una shell abierta desde NAV |
| `--version` | Muestra la versión |

Variables de entorno:

| Variable | Efecto |
|---|---|
| `NAVOPT` | Opciones por defecto, p. ej. `NAVOPT=a` o `NAVOPT=ad` |
| `NAV_LANG` | Fuerza el idioma: `es` o `en` (si no, se detecta por el sistema) |
| `NAV_SHELL` | Shell que abre Ctrl+O (si no, tu shell habitual) |

## Teclas

| Tecla | Acción |
|---|---|
| Enter | Entrar en la carpeta / ejecutar el programa o script / ver el archivo de texto |
| Retroceso | Borrar la búsqueda o, si no hay, subir a la carpeta superior |
| Flechas, RePág/AvPág, Inicio/Fin | Moverse |
| Escribir | Búsqueda rápida: por prefijo y, si no hay, por contenido del nombre |
| Ctrl+O | Abrir una shell en la carpeta actual (al salir con `exit` se vuelve a NAV) |
| F1 | Ayuda |
| Esc / Ctrl+C | Salir |

El listado se ordena en: carpetas, ejecutables, scripts y el resto (cada grupo por nombre).
Los colores indican el tipo (carpeta, ejecutable en verde, script en cian).

- **Ejecutables:** `.exe`/`.com` en Windows; archivos con bit de ejecución en Linux/macOS.
- **Scripts:** `.bat`/`.cmd`/`.ps1` en Windows; `.sh` sin bit de ejecución en Linux/macOS
  (se lanzan con `/bin/sh`).

Al ejecutar algo, NAV sale de su pantalla, el programa usa la consola normal y, al terminar,
pulsas una tecla para volver a NAV.

## Ver archivos de texto

Enter sobre un archivo que no es ejecutable ni script lo abre en un visor a pantalla
completa, de solo lectura:

| Tecla | Acción |
|---|---|
| ↑ / ↓ | Una línea arriba / abajo |
| RePág / AvPág | Una página arriba / abajo |
| Inicio / Fin | Principio / final del documento |
| Esc / Q | Cerrar el visor y volver a NAV |

- Las líneas largas se ajustan al ancho de la terminal (también al redimensionarla).
- Si el archivo parece binario (bytes nulos o muchos caracteres de control) no se abre y NAV
  lo avisa.
- Por seguridad, los caracteres de control (incluidas las secuencias de escape ANSI) y los
  controles Unicode que alteran la dirección o los saltos de línea se muestran como `?`.
  Los tabuladores se expanden a 4 columnas.
- Se admiten UTF-8 (con o sin BOM), UTF-16 con BOM y, si no es UTF-8 válido, Latin-1.
- Los archivos de más de 8 MB se muestran truncados (lo indica la barra inferior).

## Quedarte en la carpeta al salir

Un programa no puede cambiar la carpeta de la shell que lo lanzó. Por eso se usa una pequeña
función de shell que llama a NAV y hace el `cd` al salir. Añade una línea a tu perfil:

- **bash / zsh:** en `~/.bashrc` o `~/.zshrc`

  ```
  source /ruta/a/NAV/shell/nav.sh
  ```

- **PowerShell:** en `$PROFILE`

  ```
  . C:\ruta\a\NAV\shell\nav.ps1
  ```

Hace falta que el ejecutable `nav` esté en el `PATH`. Desde ese momento, al salir de NAV con
Esc te quedas en la carpeta en la que estabas navegando.

## Shell desde NAV (Ctrl+O)

Ctrl+O abre una shell en la carpeta actual. Con `exit` vuelves a NAV donde estabas.

Si dentro de esa shell escribes `nav` o `nav ruta`, no se abre un segundo NAV: la shell se
cierra y vuelves al NAV original, ya en esa carpeta. Esto requiere haber cargado el script de
`shell/` (sin él, NAV avisa de que abrirá esa carpeta al hacer `exit`). Si quieres un segundo
NAV de verdad: `nav --new`.

## Documentación técnica

Para desarrolladores y agentes de IA: [DOC/README.md](DOC/README.md).

## legacy-dos

Código original para DOS (Turbo Pascal 7), conservado como referencia. `x/` contiene las
units propias.

## Licencia

MIT. Copyright (c) 2019-2026 XWolfOverride.
