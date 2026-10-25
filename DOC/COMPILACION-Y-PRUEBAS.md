# Compilación, pruebas y estado

## Estado actual

- `dotnet build` compila sin errores ni avisos y `dotnet test` pasa (89 pruebas).
- Pendiente: publicación Native AOT y prueba manual de la UI en distintas terminales.
- Repositorio git sin remoto y sin commits (todo sin seguimiento).

## Pruebas automáticas (`tests/Nav.Tests`)

xUnit 2.9 sobre `net10.0`. Se ejecutan con `dotnet test` desde la raíz. `Nav.csproj` expone
sus tipos `internal` al proyecto de pruebas con `InternalsVisibleTo`.

| Fichero | Cubre |
|---|---|
| `UiTests.cs` | `Clip`, `ClipLeft`, `Pad`, `FormatSize`, saneado de caracteres de control |
| `OptionsTests.cs` | Argumentos, `NAVOPT`, errores de opciones |
| `StringsTests.cs` | Detección de idioma (`NAV_LANG`, `LC_*`, `LANG`) y textos |
| `DirectoryScannerTests.cs` | Clasificación, orden, ocultos, `-d`, carpeta inexistente (con ramas por SO) |
| `TextDocumentTests.cs` | Binarios, codificaciones, saneado de caracteres de control/Unicode, truncado y ajuste de línea |
| `LauncherTests.cs` | `ProcessStartInfo` de ejecutables, scripts y shells por plataforma |
| `TestSupport.cs` | `EnvScope` (variables de entorno), `TempDir`, colección sin paralelismo |

Convenciones:

- Las pruebas que cambian variables de entorno llevan `[Collection(EnvironmentCollection.Name)]`
  y usan `EnvScope`, porque las variables son del proceso y xUnit paraleliza entre clases.
- Las pruebas dependientes del sistema operativo salen al principio (`return`) si no es el
  suyo; en una máquina solo se ejercita la rama de su plataforma.
- Sin cubrir: `App` y `TextViewer` (bucle, dibujado, Ctrl+O) y `Program` (flujo de salida); dependen de una
  terminal real. Si se quiere probar `App`, habría que extraer la lógica de selección,
  búsqueda y layout de la E/S de consola.

## Requisitos

- SDK de .NET 10.
- Para publicar con Native AOT, en la plataforma de destino:
  - Windows: Visual Studio con la carga "Desarrollo para escritorio con C++".
  - Linux: `clang` y `zlib1g-dev`.
  - macOS: Xcode Command Line Tools.

AOT no hace compilación cruzada entre sistemas operativos: se compila en cada plataforma.

## Comandos

```
dotnet build
dotnet test
dotnet run --project src/Nav -- [argumentos de nav]

dotnet publish src/Nav -c Release -r win-x64
dotnet publish src/Nav -c Release -r linux-x64
dotnet publish src/Nav -c Release -r linux-arm64
dotnet publish src/Nav -c Release -r osx-arm64

# Rápido para probar en Linux desde Windows (sin AOT):
dotnet publish src/Nav -c Release -r linux-x64 -p:PublishAot=false -p:PublishSingleFile=true --self-contained
```

`dotnet run` necesita una terminal real: con stdin/stdout redirigidos NAV se niega a
arrancar (por ejemplo, no funcionará dentro de un runner sin TTY).

## Plan de primera puesta en marcha

1. ~~`dotnet build` y corregir errores~~ (hecho).
2. Probar en Windows Terminal, cmd/conhost clásico y por SSH a un Linux.
3. Publicar con AOT en cada plataforma y comprobar que arranca.
4. Primer commit y, si procede, remoto.

## Lista de comprobación manual

Hasta que haya pruebas automáticas, validar a mano:

- [ ] Arranca en la carpeta actual y con `nav <ruta>`; error con ruta inexistente.
- [ ] Redimensionar la terminal redibuja sin romper el marco; probar muy estrecha (24 col.).
- [ ] Flechas, RePág/AvPág, Inicio/Fin con muchos archivos (scroll) y con carpeta vacía.
- [ ] Búsqueda por prefijo, por contenido, y Retroceso (borra letra / sube de carpeta).
- [ ] Enter en carpeta, en `.exe`/ejecutable, en `.bat`/`.ps1`/`.sh`, y en archivo normal.
- [ ] Visor de texto: ↑↓, RePág/AvPág, Inicio/Fin, Esc/Q; redimensionar con el visor abierto; archivo vacío; binario (aviso); archivo con secuencias ESC (no deben alterar la terminal); UTF-8, UTF-16 y Latin-1; archivo > 8 MB.
- [ ] Tras ejecutar un programa, la tecla de retorno funciona y se conserva la selección.
- [ ] `-a`, `-d`, `NAVOPT`, `NAV_LANG=es|en`, `--version`, `--help`, opción inválida.
- [ ] Carpeta sin permisos (mensaje en rojo, NAV no se cae).
- [ ] Nombres con caracteres de control o muy largos; enlaces simbólicos.
- [ ] `shell/nav.sh` y `shell/nav.ps1`: `cd` al salir con Esc.
- [ ] Ctrl+O: `exit` vuelve; `nav ruta` dentro vuelve al NAV padre en `ruta`; `nav --new`.
- [ ] Salida con Ctrl+C. Restaura el terminal (cursor visible, sin pantalla alternativa).
- [ ] En PowerShell, cmd clásico y terminales SSH (comprobar que los caracteres `┌─│` se ven).

## Limitaciones y riesgos conocidos

- **Ancho de caracteres:** `Ui.Pad`/`Clip` cuentan `char`, no ancho de celda. Nombres con CJK,
  emojis o caracteres combinantes descuadrarán el marco.
- **Redibujado completo** en cada tecla y sondeo cada 40 ms para detectar cambios de tamaño
  (sin evento SIGWINCH). Suficiente en local; por SSH lento puede notarse en carpetas grandes.
- **Carpetas enormes:** `Scan` carga todo en memoria y ordena de una vez; no hay carga
  diferida.
- **Teclas:** el modo `TreatControlCAsInput` y `ReadKey` dependen del terminal; algunas
  combinaciones (Ctrl+O, F1) pueden ser interceptadas por multiplexores o emuladores.
- **Cambio de carpeta:** solo con los scripts de `shell/`. No hay script para `cmd.exe` ni
  `fish`.
- **Mensajes de `nav.ps1`:** el error "nav no esta en el PATH" está solo en español.
- **Windows y `/`:** en Windows, un argumento que empieza por `/` se toma como opción, no
  como ruta; hay que usar rutas con `\` o con letra de unidad.
- **Aviso en modo retorno:** `Console.IsInputRedirected` se comprueba después del modo
  retorno, así que `nav ruta` en la shell hija funciona aunque no sea interactiva.
