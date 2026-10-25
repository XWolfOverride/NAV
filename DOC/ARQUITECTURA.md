# Arquitectura

## Estructura del repositorio

```
NAV/
├── Nav.slnx              Solución (un solo proyecto)
├── AGENTS.md             Decisiones y estado para agentes de IA (CLAUDE.md solo lo importa)
├── README.md             Guía de uso para humanos
├── DOC/                  Esta documentación
├── src/Nav/              Código C# de la aplicación
├── tests/Nav.Tests/      Pruebas xUnit (ver COMPILACION-Y-PRUEBAS.md)
├── shell/                Funciones de shell (nav.sh, nav.ps1) para cd al salir y Ctrl+O
└── legacy-dos/           Código original en Turbo Pascal 7 (solo referencia)
```

## Proyecto `src/Nav/Nav.csproj`

- `net10.0`, `OutputType=Exe`, `AssemblyName=nav`, `Version=2.0.0`.
- `PublishAot=true`, `IsAotCompatible=true`, `InvariantGlobalization=true`,
  `OptimizationPreference=Size`, `StripSymbols=true`.
- `Nullable` e `ImplicitUsings` activados; `AllowUnsafeBlocks=true`.
- Namespace raíz `Nav`. Todas las clases son `internal`.

`InvariantGlobalization` implica que no hay cultura del sistema: los formatos usan
`CultureInfo.InvariantCulture` y el idioma se detecta a mano (ver `Strings.Detect`).

## Ficheros de `src/Nav/`

| Fichero | Responsabilidad |
|---|---|
| `Program.cs` | Punto de entrada (top-level statements) y la clase `AppInfo` (versión, nombre de variable de retorno, código 75). Orquesta: idioma → opciones → modos especiales → `App`. |
| `Options.cs` | Parseo de argumentos y de `NAVOPT`. Acumula errores en `Errors` en vez de lanzar excepciones. |
| `App.cs` | Estado y bucle principal: carga de carpeta, entrada de teclado, búsqueda, selección, ejecución de programas, Ctrl+O, dibujado de la rejilla y diálogo de ayuda. Es el fichero grande (~490 líneas). |
| `DirectoryScanner.cs` | Lee una carpeta, filtra ocultos/solo carpetas, clasifica cada entrada (`EntryKind`) y ordena. |
| `FileEntry.cs` | `enum EntryKind` (el orden del enum **es** el orden del listado) y `record FileEntry`. |
| `Launcher.cs` | Construye `ProcessStartInfo` para ejecutar un archivo o abrir una shell interactiva según plataforma. |
| `Terminal.cs` | Capa mínima de terminal: pantalla alternativa, tamaño, `MoveTo`, escritura, activación de VT en Windows. |
| `TextDocument.cs` | Carga de un archivo de texto: detección de binarios, decodificación, saneado de caracteres peligrosos y ajuste de línea. Sin E/S de terminal (probable con pruebas). |
| `TextViewer.cs` | Visor a pantalla completa de solo lectura: scroll, redibujado y redimensionado. |
| `Theme.cs` | Secuencias SGR de color por tipo de entrada y elementos de UI. |
| `Ui.cs` | Utilidades de texto: recorte (`Clip`, `ClipLeft`), relleno (`Pad`), `FormatSize`, saneado de caracteres de control. |
| `Strings.cs` | Todos los textos en español/inglés (`T(en, es)`), detección de idioma y el texto de `Usage`. |

## Flujo de ejecución (`Program.cs`)

1. `Strings.Detect()`: decide idioma.
2. `Options.Parse(args)` (aplica antes `NAVOPT`). Si hay errores: se imprimen, se muestra el
   uso y sale con código 2. `--help` → uso, código 0. `--version` → `nav 2.0`, código 0.
3. **Modo retorno:** si no hay `--new` y existe `NAV_RETURN_FILE` apuntando a un fichero
   existente, NAV no abre UI: escribe la ruta destino en ese fichero y sale con código 75
   (ver [INTEGRACION-SHELL.md](INTEGRACION-SHELL.md)). Si la ruta no es carpeta, error y código 1.
4. Si stdin o stdout están redirigidos: error "necesita terminal interactiva", código 1.
5. `Console.CancelKeyPress` se cancela para que Ctrl+C no mate NAV mientras corre un hijo.
6. `new App(options)`, `Open(ruta)`; si falla, mensaje en stderr y código 1.
7. `app.Run()` (bucle de UI hasta Esc / Ctrl+C).
8. Si hay `--cd-file`, se escribe `app.CurrentDirectory` en ese fichero. Código 0.

## Flujo interno de `App`

- **Estado:** `_dir`, `_files`, `_selection`, `_scroll`, `_search`, `_message`, y la geometría
  calculada (`_width`, `_height`, `_columns`, `_columnWidth`, `_rows`).
- **`Run`:** `Terminal.Enter()` → `Draw()` → bucle `while (Handle(WaitKey())) Draw();` →
  `Terminal.Leave()` en `finally`.
- **`WaitKey`:** sondeo cada 40 ms con `Console.KeyAvailable`; si cambia el tamaño de la
  terminal redibuja (no hay evento de resize portable).
- **`Handle`:** devuelve `false` para salir. Cualquier tecla limpia `_message`.
- **`Load(path, selectName)`:** escanea, reemplaza estado, reinicia selección/scroll/búsqueda,
  intenta `Environment.CurrentDirectory = full`. Devuelve `false` y deja `_message` si hay
  `IOException`, `UnauthorizedAccessException`, `SecurityException` o `ArgumentException`
  (en ese caso el estado anterior se conserva).
- **`Execute`:** sale de la pantalla alternativa, ejecuta el hijo heredando la consola, espera,
  pide una tecla, vuelve a entrar y recarga la carpeta seleccionando el archivo ejecutado.
- **`OpenShell`:** ver [INTEGRACION-SHELL.md](INTEGRACION-SHELL.md).
- **`Draw`:** construye toda la pantalla en un `StringBuilder` y la escribe de una vez.
  Se redibuja completa en cada tecla (sin diferencias incrementales).
- **`ShowHelp`:** redibuja, pinta un cuadro centrado y bloquea en `ReadKey`.

## Convenciones de código

- Comentarios `///` y `//` en español, sin tildes ni eñes en `.cs`.
- Excepciones filtradas con `catch (...) when (...)`; las que se ignoran a propósito van como
  `catch (Exception) { }` en accesos a metadatos del sistema de archivos (fecha, tamaño,
  enlace simbólico) que pueden fallar.
- Los colores se definen solo en `Theme.cs`; los textos solo en `Strings.cs`.
- Nada de lógica de salida directa a consola en el bucle de UI salvo a través de `Terminal`.
