# Sistema de Reservas de Pistas Deportivas

Este proyecto es una aplicación de escritorio desarrollada en C# y Windows Forms (.NET Framework 4.8) para la gestión de reservas de pistas deportivas (tenis, pádel, fútbol sala, etc.).

## Características

- **Visualización de reservas** por día y pista.
- **Creación, edición y eliminación** de reservas.
- **Gestión de promociones** y cálculo automático del coste.
- **Restricción de días** para evitar reservas en fechas no permitidas.
- **Persistencia de datos** en archivo JSON.
- **Gestión de responsable y opción de traer pelotas propias**.
- **Interfaz intuitiva** y fácil de usar.

## Estructura del Proyecto

- `Form1.cs` y `Form1.Designer.cs`: Formulario principal para visualizar y gestionar reservas.
- `NewBookingForm.cs` y `NewBookingForm.Designer.cs`: Formulario para crear o editar reservas.
- `BusinessLogic.cs`: Lógica de negocio, modelos de datos y persistencia.
- `Properties/AssemblyInfo.cs`: Información del ensamblado.
- `packages/`: Dependencias externas (por ejemplo, Newtonsoft.Json).

## Requisitos

- Visual Studio 2019 o superior
- .NET Framework 4.8
- Paquete NuGet: [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/)

## Instalación

1. Clona este repositorio o descarga el código fuente.
2. Abre la solución en Visual Studio.
3. Restaura los paquetes NuGet (Newtonsoft.Json).
4. Compila y ejecuta el proyecto.

## Uso

1. Selecciona una fecha para ver las reservas existentes.
2. Haz clic en **Nueva Reserva** para crear una nueva.
3. Haz clic derecho sobre una reserva para editarla o eliminarla.
4. Usa el botón **Restringir / Permitir Día** para bloquear o desbloquear reservas en una fecha.
5. Los datos se guardan automáticamente al cerrar la aplicación.

## Licencia

Este proyecto se distribuye bajo la licencia MIT. Consulta el archivo `LICENSE.md` para más detalles.

---

**Desarrollado por:**  
J. Ricardo
2026
