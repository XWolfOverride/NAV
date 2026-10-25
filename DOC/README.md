# Documentación técnica de NAV

Punto de entrada para desarrolladores y agentes de IA. Para el uso como usuario, ver el
[README principal](../README.md). El resumen de decisiones y estado que leen los agentes de IA está
en [AGENTS.md](../AGENTS.md).

## Qué es NAV

Navegador de carpetas para consola, en C# (.NET 10, Native AOT, sin dependencias externas),
reescritura del NAV de DOS (Turbo Pascal 7). Una sola aplicación de consola que dibuja con
secuencias ANSI/VT sobre la pantalla alternativa del terminal.

## Índice

| Documento | Contenido |
|---|---|
| [ARQUITECTURA.md](ARQUITECTURA.md) | Estructura del repo, responsabilidad de cada fichero, flujo de ejecución |
| [COMPORTAMIENTO.md](COMPORTAMIENTO.md) | Reglas funcionales: clasificación, orden, búsqueda, layout, teclas, lanzamiento |
| [INTEGRACION-SHELL.md](INTEGRACION-SHELL.md) | Protocolos `--cd-file` y Ctrl+O / `NAV_RETURN_FILE` / código 75 |
| [COMPILACION-Y-PRUEBAS.md](COMPILACION-Y-PRUEBAS.md) | Build, publicación AOT, estado actual, pendientes y limitaciones conocidas |
| [LEGACY-DOS.md](LEGACY-DOS.md) | Qué es el código original y qué se conservó de él |

## Hechos clave (leer antes de tocar nada)

- Compila y `dotnet test` pasa (xUnit en `tests/Nav.Tests`). La UI interactiva solo está
  cubierta por la lista de comprobación manual.
- Sin dependencias NuGet; todo el manejo de terminal es propio (`Terminal.cs`).
- Compatibilidad con Native AOT: no usar reflexión, `System.Text.Json` sin generador de
  código ni otras APIs no compatibles. P/Invoke solo con `LibraryImport`.
- Sin modo blanco y negro (decisión deliberada, se quitó la opción `-m` del original).
- Textos siempre en español e inglés vía `Strings.cs`; no poner literales visibles en otros
  ficheros.
- Código y comentarios en español, sin tildes en los `.cs` (estilo existente).
- El código base son ~900 líneas en `src/Nav/`; se puede leer entero.
