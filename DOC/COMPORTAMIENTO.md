# Comportamiento funcional

Reglas que implementa el código actual. Si cambias una, actualiza este documento y el README.

## Clasificación (`DirectoryScanner.Classify`)

| Plataforma | Ejecutable | Script | Otro |
|---|---|---|---|
| Windows | `.exe`, `.com` | `.bat`, `.cmd`, `.ps1` | resto |
| Linux / macOS | cualquier bit de ejecución (usuario, grupo u otros) | `.sh` sin bit de ejecución | resto |

- Las extensiones se comparan sin distinguir mayúsculas.
- Las carpetas (`DirectoryInfo`) son `EntryKind.Directory`. Un enlace simbólico a carpeta se
  trata como carpeta; `LinkTarget` se muestra en la línea de estado como `→ destino`.
- Si falla la lectura de permisos Unix, el archivo cae a `.sh`/Otro.

## Ocultos y filtros

- Se usa el atributo `Hidden`. En Linux/macOS .NET lo marca para nombres que empiezan por `.`.
- `-a` los incluye. `-d` deja solo carpetas.
- Se enumera con `IgnoreInaccessible = true` y `AttributesToSkip = 0` (no se salta nada por
  atributos; el filtrado de ocultos es explícito).
- Sin recursión; sin `.` ni `..` (el ascenso se hace con Retroceso).

## Orden

Primero por `EntryKind` (Directory < Executable < Script < Other), luego por nombre sin
distinguir mayúsculas, y como desempate comparación ordinal exacta.

## Rejilla (layout)

- Marco de 1 carácter de ancho. Alto útil de la rejilla: `alto - 4` filas (mínimo 1).
  Las 3 últimas líneas son separador, línea de estado y borde inferior; la primera es la
  superior con la ruta.
- Ancho de columna deseado = nombre más largo (con `/` o `\` final en carpetas) acotado entre
  12 y `min(40, ancho interior)`. Nº de columnas = las que caben (separadas por `│`); el ancho
  sobrante se reparte entre columnas.
- Las entradas se colocan **por filas** (índice = `fila * columnas + columna`), por eso
  arriba/abajo mueven `±columnas` y izquierda/derecha `±1`.
- RePág/AvPág mueven `±columnas * filas`. Inicio/Fin saltan a primera/última entrada.
- El scroll mantiene siempre visible la fila seleccionada.
- Tamaño mínimo asumido: `Terminal.Width ≥ 24`, `Terminal.Height ≥ 8`.
- La ruta se muestra en el borde superior entre corchetes, recortada por la izquierda
  (se ve el final).

## Línea de estado

`[campo de búsqueda]` + tipo (`Carpeta`/`Ejecutable`/`Script`/`Otros`) + tamaño (vacío en
carpetas, formato `B/KB/MB/GB/TB/PB`) + fecha `yyyy-MM-dd HH:mm` + `→ destino` si es enlace +
`posición/total` alineado a la derecha. Si hay `_message`, sustituye a la info y se pinta en
rojo hasta la siguiente tecla.

El borde inferior muestra `[F1 Ayuda]` a la izquierda y `[NAV 2.0 by XWolf]` a la derecha
(este último solo si cabe).

## Búsqueda rápida

- Cualquier carácter imprimible sin Ctrl/Alt se añade al texto de búsqueda **solo si** hay
  coincidencia; la búsqueda nunca queda "vacía de resultados".
- Coincidencia: primero por prefijo; si no hay, por contenido (`Contains`); ambas sin
  distinguir mayúsculas. Se selecciona la primera entrada que coincida.
- Retroceso con texto: borra la última letra y reposiciona en la coincidencia (si queda
  texto). Retroceso sin texto: sube a la carpeta superior y deja seleccionada la carpeta de
  la que se venía.
- Cualquier movimiento (flechas, páginas, inicio/fin), Enter o cambiar de carpeta limpia la
  búsqueda.

## Enter / lanzamiento

- Carpeta → entra. Ejecutable o script → se ejecuta. Otro → visor de texto (ver más abajo),
  o mensaje "Archivo binario: no se puede mostrar" si parece binario.
- Implementación en `Launcher.Create` (el directorio de trabajo es la carpeta actual):

| Caso | Comando |
|---|---|
| Windows `.bat` / `.cmd` | `%ComSpec% /c <ruta>` |
| Windows `.ps1` | `pwsh.exe` si está en `PATH`, si no `powershell.exe`: `-NoLogo -File <ruta>` |
| Windows otro (`.exe`, `.com`) | la propia ruta |
| Unix, `Script` | `/bin/sh <ruta>` |
| Unix, `Executable` | la propia ruta |

- Durante la ejecución NAV sale de la pantalla alternativa. Al terminar imprime
  `[nav] Pulsa una tecla para volver (codigo de salida N)...` (el código solo si ≠ 0) y espera
  una tecla.
- Ctrl+C no mata NAV (`CancelKeyPress` cancelado); el hijo lo recibe de la consola.

## Idioma

`Strings.Detect`: primera variable no vacía entre `NAV_LANG`, `LC_ALL`, `LC_MESSAGES`, `LANG`.
Si ninguna y es Windows, se consulta `GetUserDefaultUILanguage` (idioma primario `0x0A` =
español). Es español si el valor empieza por `es`; en cualquier otro caso, inglés.

## Opciones (`Options.Parse`)

1. Se aplica `NAVOPT` como letras de opción (se ignora `-` o `/` inicial), p. ej. `ad`.
2. Argumentos: `--help`, `--version`, `--new`, `--cd-file F`; otro `--x` es error.
3. `-letras` (y `/letras` solo en Windows): `a`, `d`, `h`/`?`; otra letra es error.
4. El primer argumento que no es opción es la ruta inicial; un segundo es error.
5. Los argumentos desde la línea de comandos se aplican después y por tanto pueden sumar a
   `NAVOPT`, pero no hay forma de desactivar una opción de `NAVOPT`.

## Códigos de salida

| Código | Significado |
|---|---|
| 0 | Salida normal, `--help`, `--version` |
| 1 | Carpeta inicial inválida, sin terminal interactiva, destino de retorno que no es carpeta |
| 2 | Error de opciones |
| 75 | NAV anidado que devolvió la carpeta al NAV padre (`AppInfo.ReturnExitCode`) |

## Colores (`Theme.cs`)

Fondo azul como el original. Marco y carpetas: blanco brillante; ejecutables: verde brillante;
scripts: cian brillante; otros: blanco normal; selección: blanco sobre verde; búsqueda:
amarillo; error: blanco sobre rojo; ayuda: negro sobre gris.

## Visor de texto (`TextDocument` + `TextViewer`)

Se abre con Enter sobre una entrada `EntryKind.Other` (`App.View`). Solo lectura, a pantalla
completa sobre la pantalla alternativa ya activa: no sale de ella; al cerrar, `App` redibuja.

**Carga (`TextDocument.Load`)**
- Lee como mucho `MaxBytes` (8 MiB) con `FileShare.ReadWrite`. Si hay más, `Truncated = true`
  y se recorta un carácter UTF-8 que haya quedado cortado.
- **Binario** (`LooksBinary`, sobre los primeros 8 KiB): cualquier byte `0x00`, o más de un 10 %
  de bytes `< 0x20` salvo `\t \n \r \f \b` y ESC. Un BOM UTF-16 (`FF FE` / `FE FF`) se considera
  texto aunque contenga ceros. Si es binario, `Load` devuelve `null`.
- **Codificación:** BOM UTF-8 (se quita), UTF-16 LE/BE con BOM; sin BOM, UTF-8 estricto y, si
  falla, Latin-1.
- Saltos de línea `\r\n`, `\r` y `\n`; el salto final no añade una línea vacía. Un archivo
  vacío da cero líneas (el visor muestra "(vacia)").

**Saneado (`TextDocument.Sanitize`)**, aplicado a cada línea al cargar. Se sustituye por `?`:
- cualquier `char.IsControl` (C0, `0x7F`, C1; en particular ESC, así que ninguna secuencia de
  terminal pasa);
- `U+2028`, `U+2029`, `U+FEFF`, `U+200E/F`, `U+202A–202E`, `U+2066–2069`;
- sustitutos (surrogates) sueltos. Los pares válidos (emoji) se conservan.
- El tabulador se expande a espacios hasta el siguiente múltiplo de 4.

**Ajuste de línea (`TextDocument.Wrap`)**: cada línea se parte en filas de `ancho` caracteres sin
separar un par de sustitutos; las líneas vacías ocupan una fila. Al redimensionar se recalcula
conservando la línea original que estaba arriba.

**Teclas:** ↑/↓ una fila, RePág/AvPág `alto del cuerpo − 1`, Inicio/Fin al principio/final;
Esc, Q o Ctrl+C cierran el visor (no salen de NAV). Cualquier otra tecla se ignora.

**Pantalla:** primera línea con nombre y tamaño (barra cian), cuerpo con fondo negro, última
línea con teclas, `filaInicial-filaFinal/total  porcentaje%` y `[truncado]` si procede.
