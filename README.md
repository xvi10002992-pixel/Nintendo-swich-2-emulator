# Switch 2 PC Emulator

**Aplicación de escritorio para Windows (x64) con biblioteca local de juegos y motor de emulación Eglantine.**

> Proyecto comunitario, no oficial y no afiliado a Nintendo. Este repositorio no incluye juegos, firmware ni claves de descifrado. Utiliza únicamente copias y archivos de sistema que tengas derecho a usar.

## Características

- Aplicación de Windows compilada como `.exe`.
- Biblioteca de juegos local: añade archivos `.XCI`, `.NSP` y `.NRO` o escanea carpetas.
- Inicio de juegos con el motor de emulación Eglantine.
- Interfaz y biblioteca locales: el lanzador no necesita Internet para funcionar.
- Compatible con uso de teclado configurable desde los ajustes de controles del motor.
- Preferencias guardadas en el perfil local de Windows.

## Descarga

Los paquetes para Windows se publican en **Actions → Build Windows → Artifacts**. Cuando se publique una etiqueta `v*`, también se creará un paquete en Releases.

El paquete contiene `NintendoSwitch2Emulator.exe` y el motor Eglantine en la carpeta `engine`.

## Primer uso

1. Descarga y extrae el paquete de Windows.
2. Ejecuta `NintendoSwitch2Emulator.exe`.
3. Añade una carpeta con tus juegos o selecciona archivos individualmente.
4. Instala en Eglantine el firmware y las claves que hayas obtenido legalmente de tu propia consola, usando las herramientas del propio emulador.
5. En Eglantine, abre **Emulation → Configure → Controls** para configurar el teclado y guardar tu perfil.
6. Vuelve al lanzador y pulsa dos veces un juego o selecciona **Jugar**.

Una vez instalado y configurado el motor, el lanzador y la ejecución local de juegos no necesitan conexión a Internet. Las funciones online o multijugador no están incluidas en esta promesa.

## Compilar el lanzador

Requiere .NET 8 SDK y Windows:

```powershell
dotnet publish src/Launcher/Launcher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Motor de emulación y compatibilidad

El motor se obtiene de [Eglantine](https://github.com/Eglantine-Community/Eglantine), un proyecto independiente de código abierto derivado de Eden, Yuzu y Sudachi. Su compatibilidad depende de cada juego, del hardware, de los controladores gráficos y de la versión del motor. No se garantiza que todos los juegos funcionen.

Versión del motor empaquetada: [Eglantine v0.1.0-rc3](https://github.com/Eglantine-Community/Eglantine/releases/tag/v0.1.0-rc3).

El código fuente correspondiente de Eglantine se incluye en el paquete de distribución para cumplir las condiciones de su licencia. Consulta `THIRD_PARTY_NOTICES.md`.

## Controles

El lanzador inicia el motor con el archivo seleccionado como argumento, usando el modo de carga documentado por Eglantine. Los controles se configuran dentro de Eglantine; el lanzador ofrece una guía para encontrarlos, pero no altera ni fuerza perfiles de entrada.

## Licencias

- Lanzador: MIT (ver `LICENSE`).
- Motor Eglantine: GPL-3.0-or-later, con componentes heredados bajo sus licencias correspondientes. Ver `THIRD_PARTY_NOTICES.md` y el código fuente incluido.
