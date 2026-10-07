# PGM611 - Scripting - Condoreno Eduardo

Videojuego 2D de plataformas hecho en Unity para la materia **PGM611 - Scripting** (Unifranz), siguiendo la guía "Creando un videojuego 2D con Unity".

## Datos del proyecto

- **Nombre:** Jhoel_Condoreno_Juego (High Forest)
- **Unity:** 6000.3.16f1
- **Render Pipeline:** Universal Render Pipeline (2D / Renderer2D)
- **Input:** modo "Both" (Input Manager + Input System)

## El juego

Un caballero recorre el bosque recolectando abejas. Los caracoles se pueden pisar y los jabalíes reinician el nivel.

| Acción | Tecla |
|---|---|
| Moverse | A / D o flechas |
| Saltar | Espacio |
| Atacar | Clic izquierdo o Ctrl |

## Estructura

```
Assets/
  Scenes/       MenuPrincipal (escena 0) y Juego (escena 1)
  Scripts/      Jugador, Camara, OpcionesMenu, FondoCamara
  Prefab/       Jugador, Abeja, Puerquito, Caracol
  Animaciones/  clips y controladores de cada personaje
  Sprites/      pack Legacy-Fantasy High Forest 2.3 y botones
  Audio/        música y efectos (Minifantasy Dungeon)
  Tiles/        tiles del piso
  Editor/       ConstructorJuego: arma las escenas automáticamente
```

## Cómo abrirlo

1. Clonar el repositorio.
2. Abrir Unity Hub, **Add > Add project from disk** y seleccionar la carpeta clonada.
3. Abrir con la versión **6000.3.16f1** de Unity.
4. Abrir `Assets/Scenes/MenuPrincipal` y presionar Play.

La carpeta `Library/` no está versionada: Unity la regenera sola la primera vez que se abre el proyecto.

## Créditos de recursos

- Sprites: [Legacy Fantasy - High Forest](https://anokolisa.itch.io/sidescroller-pixelart-sprites-asset-pack-forest-16x16) por Anokolisa
- Sonido: [Minifantasy Dungeon SFX / Music](https://leohpaz.itch.io/minifantasy-dungeon-sfx-pack) por Leohpaz
- Botones: [UI Button Megapack](https://csmikelandre.itch.io/uibutton-megapack-84pngs) por Csmikelandre

## Autor

Jhoel Eduardo Condoreno Ch. - Ingeniería de Sistemas, Unifranz
