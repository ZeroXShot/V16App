# V16 Beacon Tracker

Aplicación Unity para rastrear y visualizar balizas V16 de emergencia en España.

## Características

- 🗺️ **Mapa interactivo** con tiles de OpenStreetMap
- 📍 **Visualización de balizas** activas en tiempo real
- 📊 **Panel de analíticas** con estadísticas por comunidad/provincia
- 🔍 **Búsqueda** por carretera, comunidad o coordenadas
- 📱 **Multi-plataforma**: Android, Desktop (Windows/Linux), WebGL

## Estructura del Proyecto

```
V16App/
├── Assets/
│   ├── Scripts/
│   │   ├── API/           # Servicios de API DGT
│   │   ├── Map/           # Gestión del mapa
│   │   ├── UI/            # Paneles de interfaz
│   │   └── Core/          # Controladores principales
│   ├── Editor/            # Scripts de compilación
│   └── Scenes/            # Escena principal
└── ProjectSettings/       # Configuración de Unity
```

## API DGT

La aplicación utiliza la API de eTraffic de la DGT:

```
POST https://etraffic.dgt.es/etrafficWEB/api/cache/getFilteredData
Content-Type: application/json

{"filtrosVia":["Otras vialidades"],"filtrosCausa":[]}
```

**Nota**: La respuesta está codificada en Base64 + XOR con clave 0x4B.

## Compilación

### Requisitos
- Unity 2021.3 LTS o superior
- Android SDK (para builds Android)
- WebGL Build Support

### Desde Unity Editor

1. Abre el proyecto en Unity
2. Ve a **Build > Build All Platforms** para compilar todo
   - O usa **Build > Build Android** / **Build Desktop** / **Build WebGL** individualmente

### Desde línea de comandos

```bash
# Android
Unity -batchmode -quit -projectPath . -executeMethod Builder.BuildAndroid

# Desktop Linux
Unity -batchmode -quit -projectPath . -executeMethod Builder.BuildDesktop

# WebGL
Unity -batchmode -quit -projectPath . -executeMethod Builder.BuildWebGL
```

## Controles

- **Arrastrar**: Pan del mapa
- **Rueda del ratón / Pinch**: Zoom
- **Click en marcador**: Ver detalles de baliza
- **+/-**: Zoom in/out
- **R**: Refrescar datos
- **Escape**: Cerrar paneles

## Licencia

Este proyecto es de código abierto para fines educativos.
