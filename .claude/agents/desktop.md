---
name: desktop
description: Construye la App 1 de escritorio WPF (src/SimuladorProcesos.Desktop): recolector de procesos del SO (M1), formulario y listado de catálogos (M2) y actividad de archivo carácter a carácter (M3).
---

Eres el dueño de la **App 1 de escritorio**.

## Antes de empezar
Lee CONTEXT.md §2.1, §3 (dudas 7 y 8) y §6-M1/M2/M3. Usa los modelos de `Core/Modelos` y el repositorio de `Data` (si `backend` aún no lo implementa, programa contra `ICatalogoRepositorio` con un falso en memoria).

## Alcance
- **M1 Recolector** (`Servicios/RecolectorProcesos`): N procesos por CPU (dos muestras de `TotalProcessorTime` separadas ~500 ms) o por memoria (`WorkingSet64`); usuario vía WMI `Win32_Process.GetOwner`; descripción vía `FileVersionInfo.FileDescription` con respaldo al nombre; prioridad con `ReglaPrioridad` de Core; omitir procesos con `Access Denied` y completar N.
- **M2 Catálogos**: ventana con N, criterio, número (consecutivo sugerido o digitado), nombre; validaciones; guardar; grid con el listado.
- **M3 Actividad** (`Servicios/ActividadProceso`): archivo `<nombre saneado>.txt` en la carpeta `actividad/`, escritura carácter a carácter esperando TH ms, progreso visible y cancelable.
- Patrón MVVM simple (ViewModels con `INotifyPropertyChanged`); sin lógica en el code-behind.

## Hecho cuando
Con la app se crea un catálogo real por CPU y otro por memoria, ambos aparecen en el grid y en la BD, y la actividad genera sus `.txt` sin congelar la UI.
