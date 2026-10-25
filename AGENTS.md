# NAV — AGENTS.md

Navegador de carpetas para consola (Windows, Linux, macOS, tambien por SSH). Reescritura en
C# del NAV original para DOS en Turbo Pascal 7 (`legacy-dos/NAV.PAS`, units en `legacy-dos/x/`,
codificacion CP437). Del original se conserva el concepto, no la implementacion.

## Decisiones tomadas
- C# / .NET 10 con Native AOT (`src/Nav/Nav.csproj`). Sin dependencias externas; la terminal se
  maneja con secuencias ANSI/VT y la pantalla alternativa (`Terminal.cs`). Nada grafico.
- Sin modo blanco y negro (la opcion -m del original se elimino a proposito).
- Textos en espanol e ingles (`Strings.cs`), deteccion por locale o `NAV_LANG`.
- Opciones: `-a` ocultos, `-d` solo carpetas, variable `NAVOPT`.
- Cambio de carpeta al salir: `--cd-file` + funciones de `shell/nav.sh` y `shell/nav.ps1`.
- Enter sobre un archivo no ejecutable abre el visor de texto (solo lectura, sin binarios, caracteres de control sustituidos por `?`).
- Ctrl+O abre una shell en la carpeta actual; `exit` vuelve a NAV. Dentro de esa shell, `nav [ruta]`
  no abre otro NAV: escribe la ruta en el fichero de `NAV_RETURN_FILE` y sale con codigo 75; las
  funciones de shell ven el 75 y cierran la shell, asi se vuelve al NAV padre en esa ruta.
  `nav --new` fuerza un NAV nuevo.

## Estado
- Compila sin errores (`dotnet build`) y `tests/Nav.Tests` (xUnit, 89 pruebas) pasa con `dotnet test`.
  Falta probar a mano la interfaz en Windows Terminal, cmd/conhost y por SSH, y ejecutar `build.sh` en Linux/macOS.
- Repositorio git sin remoto y sin commits todavia.

## Compilar
`dotnet build`, `dotnet test`, `dotnet run --project src/Nav`. Distribucion: sin binarios publicados; `build.sh` / `build.ps1` comprueban requisitos, compilan con AOT y dejan `nav`/`nav.exe` en la raiz (ver README).
Documentacion tecnica en `DOC/`.
