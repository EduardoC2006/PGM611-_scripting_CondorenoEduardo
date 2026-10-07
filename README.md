# PGM611 - Scripting - Condoreno Eduardo

**High Forest**: videojuego 2D de plataformas hecho en Unity para la materia **PGM611 - Scripting** (Unifranz). Parte de la guía "Creando un videojuego 2D con Unity" y la amplía hasta un nivel completo.

## Datos del proyecto

- **Unity:** 6000.3.16f1
- **Render Pipeline:** Universal Render Pipeline (2D / Renderer2D)
- **Input:** modo "Both" (Input Manager + Input System)

## El juego

Un caballero cruza el bosque recolectando abejas hasta llegar al cofre del final. Los caracoles se pueden pisar, los jabalíes embisten y quitan vidas, y caer al vacío también cuesta una vida.

| Acción | Tecla |
|---|---|
| Moverse | A / D o flechas |
| Saltar | Espacio |
| Atacar con la espada | Clic izquierdo o Ctrl |
| Pausa | Esc o P |

### Características

- Menú principal animado con opciones (controles y volumen) y el mejor récord.
- Paisaje de fondo con parallax: montañas, bosques y pinos.
- Nivel largo con escalones, abismos, plataformas flotantes y decoración.
- 3 vidas con corazones, invulnerabilidad al recibir daño y reaparición tras caer.
- Enemigos con IA: el jabalí patrulla y embiste, el caracol camina; ambos con animación al morir.
- Animación de muerte del caballero y pantalla de Game Over.
- Cofre final, pantalla de victoria, cronómetro y récords guardados (PlayerPrefs).
- Pausa, sonidos (salto, aterrizaje, golpes, abejas) y fundidos entre escenas.

## Scripts

| Script | Qué hace |
|---|---|
| `Jugador` | Movimiento, salto, ataque, vidas, daño, recolección y muerte del caballero |
| `Camara` | Sigue al jugador con suavidad, sin salirse de los bordes del nivel |
| `Enemigo` | Patrulla, persecución y muerte de jabalíes y caracoles |
| `Flotar` | Movimiento de las abejas en el aire |
| `Partida` | Cronómetro, pausa, Game Over, victoria y récords |
| `Meta` | Cofre del final del nivel |
| `OpcionesMenu` | Botones del menú y volumen |
| `Parallax` / `Desplazar` | Capas del fondo (juego y menú) |
| `FondoCamara` | Estira el cielo para cubrir toda la cámara |
| `Fundido` | Transiciones entre escenas |
| `Editor/ConstructorJuego` | Arma las escenas, prefabs y animaciones desde código y exporta el ejecutable |
| `Editor/IniciarDesdeMenu` | Hace que Play en el editor empiece en el menú |

## Estructura

```
Assets/
  Scenes/       MenuPrincipal (escena 0) y Juego (escena 1)
  Scripts/      scripts del juego
  Editor/       herramientas del editor
  Prefab/       Jugador, Abeja, Puerquito, Caracol
  Animaciones/  clips y controladores de cada personaje
  Sprites/      pack High Forest, paisaje, decoración, HUD y botones
  Audio/        música y efectos
  Tiles/        tiles del piso
```

## Cómo abrirlo y jugarlo

1. Clonar el repositorio.
2. Abrir Unity Hub, **Add > Add project from disk** y seleccionar la carpeta clonada.
3. Abrir con la versión **6000.3.16f1** de Unity.
4. Presionar Play (siempre empieza en el menú principal).

Para exportar el ejecutable: **File > Build Profiles > Build**, o el menú **Juego > 3. Exportar ejecutable (Linux)**, que lo deja en `Build/Linux/`.

La carpeta `Library/` no está versionada: Unity la regenera sola la primera vez que se abre el proyecto.

## Créditos de recursos

- Sprites: [Legacy Fantasy - High Forest](https://anokolisa.itch.io/sidescroller-pixelart-sprites-asset-pack-forest-16x16) por Anokolisa
- Sonido: [Minifantasy Dungeon SFX / Music](https://leohpaz.itch.io/minifantasy-dungeon-sfx-pack) por Leohpaz
- Botones: [UI Button Megapack](https://csmikelandre.itch.io/uibutton-megapack-84pngs) por Csmikelandre

## Autor

Jhoel Eduardo Condoreno Ch. - Ingeniería de Sistemas, Unifranz
