# Integración con la shell

Un proceso hijo no puede cambiar el directorio de su padre. NAV resuelve dos necesidades con
ficheros temporales y una función de shell que envuelve al ejecutable.

Ficheros: [shell/nav.sh](../shell/nav.sh) (bash/zsh) y [shell/nav.ps1](../shell/nav.ps1)
(PowerShell). Constantes en `AppInfo` (`Program.cs`).

## 1. Quedarse en la carpeta al salir (`--cd-file`)

```
función nav(args):
    tmp = fichero temporal
    nav.exe --cd-file tmp args        # NAV, al terminar, escribe en tmp la carpeta final
    dir = contenido de tmp
    borrar tmp
    si dir existe y es distinto del actual: cd dir
```

- NAV escribe `app.CurrentDirectory` (sin salto de línea final) tras `app.Run()`.
- Los scripts llaman al **binario**, no a sí mismos: `command nav` en bash/zsh y
  `Get-Command -CommandType Application` en PowerShell. Por eso `nav` debe estar en el `PATH`.
- Con Esc o Ctrl+C se sale igual y se aplica el `cd`.
- Si el fichero no se puede escribir, NAV imprime el error en stderr y sale con 0.

## 2. Ctrl+O: shell hija y retorno al NAV padre

Objetivo: desde NAV abrir una shell; si ahí se escribe `nav [ruta]`, no apilar otro NAV, sino
volver al NAV original ya en esa ruta.

### Protocolo

1. **Ctrl+O** (`App.OpenShell`):
   - Crea un fichero vacío `nav-<pid>-<guid>.dir` en el directorio temporal.
   - Lanza la shell (`Launcher.CreateShell`) en la carpeta actual con la variable de entorno
     `NAV_RETURN_FILE=<ese fichero>`.
   - Sale de la pantalla alternativa e imprime un aviso (`Strings.ShellBanner`).
2. **Dentro de la shell**, la función `nav` (de `shell/`) llama a `nav --cd-file tmp ...`.
   `Program.cs` detecta que `NAV_RETURN_FILE` existe y apunta a un fichero real (y no se pasó
   `--new`): **no abre UI**; escribe la ruta absoluta pedida (o el directorio actual) en el
   fichero de retorno, imprime `Strings.ReturnHint` en stderr y sale con **código 75**.
3. La función de shell ve `rc == 75` y `NAV_RETURN_FILE` definido y **cierra la shell**
   (`exit`). En ese caso no hace `cd`.
4. De vuelta en el NAV padre, tras `WaitForExit()`, lee el fichero de retorno y lo borra. Si
   contiene una ruta válida, la carga (`Load(target)`); si no, recarga la carpeta de antes
   seleccionando el archivo que estaba seleccionado.

### Elección de shell (`Launcher.CreateShell`)

1. `NAV_SHELL` si está definida.
2. Windows: `pwsh.exe` → `powershell.exe` → `%ComSpec%` → `cmd.exe` (buscados en `PATH`).
3. Unix: `$SHELL` o `/bin/sh`.
4. Si el nombre del ejecutable es `pwsh`/`powershell` se añade `-NoLogo`.

### Casos límite

- **Sin scripts de `shell/` cargados** en la shell hija: `nav ruta` sí devuelve 75 y escribe
  el fichero, pero nadie cierra la shell; el mensaje del aviso le dice al usuario que NAV abrirá
  esa carpeta al hacer `exit`.
- `nav --new` ignora `NAV_RETURN_FILE` y abre un NAV completo (la variable se hereda, pero
  `options.New` la desactiva).
- Si el fichero de retorno se borrase, `File.Exists` falla y `nav` arranca normal.
- Una ruta inexistente en modo retorno da error (`Strings.NotADirectory`) y código 1, sin
  tocar el fichero.
- Anidamiento: cada NAV padre crea su propio fichero (pid + GUID), así que varios niveles
  (`nav --new` dentro de Ctrl+O dentro de ...) no se pisan.

## Si se añade otra shell (fish, cmd...)

Replicar el contrato: (a) llamar al binario con `--cd-file`, (b) hacer `cd` al contenido,
(c) si el código de salida es 75 y `NAV_RETURN_FILE` está definida, cerrar la shell sin hacer
`cd`. `cmd.exe` puro no tiene funciones: haría falta un `.bat` (con `doskey` o en `PATH`).
