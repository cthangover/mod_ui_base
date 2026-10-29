# UI — la interfaz común de todos los juegos

UI es la capa de interfaz que comparten todos los juegos: la barra de herramientas en la esquina superior derecha, el menú del juego, el indicador de guardado, las notificaciones en la esquina superior izquierda, la vista del fondo y el aspecto común de paneles, botones y listas. No tiene historia ni reglas propias. Casi todos los demás mods se construyen sobre ella, y por eso está presente en todos los juegos y tantos mods la declaran como dependencia.

![Una escena de Cthangover al mediodía: el reloj arriba a la izquierda, la barra de herramientas arriba a la derecha y las opciones de la escena a la derecha.](assets/scene-hud.jpg)

## La barra de herramientas

Cada icono abre su panel. Los botones no son fijos: cada mod del juego añade los suyos, así que una novela sin objetos no tiene bolsa, y un juego con Scenario Studio muestra un icono más.

![La barra de herramientas de Cthangover: herramientas, mapa, bolsa, personajes, reclutas, habilidades, menú (engranaje) y diario.](assets/toolbar.png)

En Cthangover, de izquierda a derecha:

1. **Herramientas** — el menú de herramientas de desarrollo (solo cuando hay editores en el juego).
2. **Mapa** — el mapa del mundo (Map Engine).
3. **Bolsa** — el inventario (Items).
4. **Personajes** — el grupo y los personajes (Characters).
5. **Reclutas** — los personajes reclutados (Recruiting).
6. **Habilidades** — las habilidades (Skills).
7. **Menú** — el menú del juego, descrito más abajo (este mod).
8. **Diario** — las misiones (Quests).

Otros juegos muestran otros conjuntos; RPG Plus, por ejemplo, añade la **hoja de personaje**.

## El menú del juego

El icono del engranaje abre el menú: **Continuar**, **Guardar**, **Cargar**, **Ajustes**, **Menú principal** y **Salir**. En las escenas que no permiten guardar, la opción Guardar aparece deshabilitada, como en la imagen. Cargar, Guardar y Ajustes abren las mismas ventanas que en el menú principal.

![El menú del juego sobre una escena: Menu, Resume, Save, Load, Settings, Main menu, Exit.](assets/game-menu.jpg)

## Notificaciones

En la esquina superior izquierda aparecen mensajes breves: una entrada nueva del diario, una pista de la historia. Se ven hasta cuatro a la vez, cada uno unos diez segundos; los demás esperan su turno.

![Una notificación bajo el reloj: «Mira a tu alrededor. Hay algo tirado en el callejón.» — una pista del prólogo de Cthangover.](assets/notification.png)

## Para autores de mods

**Añadir un botón a la barra de herramientas con su panel** se hace con una entrada `ui` en el manifiesto. Así añade Items la bolsa:

```json
"ui": [
  {
    "id": "inventory",
    "kind": "panel",
    "slot": "ModBeforeUIPanel",
    "scene_path": "scenes/ui/inventory_panel.tscn",
    "button": { "icon": "bag", "locale": "ui/inventory/title", "index": 20 },
    "priority": 10
  }
]
```

- `button.index` ordena los iconos (mapa 10, bolsa 20, personajes 30, habilidades 40, menú 50, diario 60, estudio 90);
- `button.icon` es el nombre del icono y `button.locale`, la clave de traducción del tooltip;
- una entrada sin `button` es un panel o una capa superpuesta que el juego abre por su cuenta: Crafting añade su mesa de trabajo como `"kind": "overlay"` en el slot `ModLastPanel` y la abre desde los escenarios.

**Abrir un panel desde un escenario** se hace con la acción `ui.toggle_panel`, y **mostrar una notificación**, con el comando `notify`:

```scenario
notify "Nueva entrada en el diario."
```

**Usa el aspecto común.** Los paneles creados en código toman sus marcos, etiquetas, chips y botones del kit de interfaz (`UiKit`) y de sus tokens de color y tamaño, así que un panel nuevo encaja con el resto del juego y sigue su tema.

## Si algo falla

1. **Falta un botón en la barra de herramientas.** El mod que lo añade no forma parte de este juego; revisa el esquema del juego en **Mods y colecciones**.
2. **Guardar aparece deshabilitado.** La escena actual no permite guardar; ve a otro sitio e inténtalo de nuevo. Los autores lo permiten con `save_allowed: true` en la cabecera de uno de los escenarios de la escena.
3. **Un panel se abre vacío.** Su mod todavía no tiene nada que mostrar en este juego; por ejemplo, aún no ha empezado ninguna misión.
