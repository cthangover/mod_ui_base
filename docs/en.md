# UI — the shared interface of every game

UI is the interface layer that the games share: the toolbar in the top-right corner, the in-game menu, the save indicator, notifications in the top-left corner, the background view and the common look of panels, buttons and lists. It has no story or rules of its own. Almost every other mod builds on it, which is why it is in every game and why so many mods list it as a requirement.

![A scene in Cthangover at noon: the clock at the top left, the toolbar at the top right, the scene's choices on the right.](assets/scene-hud.jpg)

## The toolbar

Each icon opens a panel. The buttons are not fixed: every mod in the game adds its own, so a novel without items has no bag, and a game with the Scenario Studio has one more icon.

![The Cthangover toolbar: tools, map, bag, characters, recruits, skills, menu (gear) and journal.](assets/toolbar.png)

In Cthangover, from left to right:

1. **Tools** — the developer tools menu (only when editors are in the game).
2. **Map** — world map (Map Engine).
3. **Bag** — inventory (Items).
4. **Characters** — party and characters (Characters).
5. **Recruits** — recruited characters (Recruiting).
6. **Skills** — skills (Skills).
7. **Menu** — the in-game menu described below (this mod).
8. **Journal** — quests (Quests).

Other games show other sets; RPG Plus, for example, adds the **character sheet**.

## The in-game menu

The gear icon opens the menu: **Resume**, **Save**, **Load**, **Settings**, **Main menu** and **Exit**. Save is greyed out in scenes that do not allow saving, as in this picture. Load, Save and Settings open the same windows as in the main menu.

![The in-game menu over a scene: Menu, Resume, Save, Load, Settings, Main menu, Exit.](assets/game-menu.jpg)

## Notifications

Short messages appear in the top-left corner: a new journal entry, a hint from the story. Up to four are shown at once, each for about ten seconds; more wait their turn.

![A notification under the clock: "Look around. Something is lying in the alley." — a hint from Cthangover's prologue.](assets/notification.png)

## For mod authors

**Add a toolbar button and its panel** with a `ui` entry in your manifest. This is how Items adds the bag:

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

- `button.index` orders the icons (map 10, bag 20, characters 30, skills 40, menu 50, journal 60, studio 90);
- `button.icon` is the icon's name, `button.locale` the tooltip's translation key;
- an entry without `button` is a panel or overlay that the game opens itself — Crafting adds its workbench as `"kind": "overlay"` in the `ModLastPanel` slot and opens it from scenarios.

**Open a panel from a scenario** with the `ui.toggle_panel` action, and **show a notification** with the `notify` command:

```scenario
notify "The journal has a new entry."
```

**Use the common look.** Panels built in code take their frames, labels, chips and buttons from the toolkit (`UiKit`) and its colour and size tokens, so a new panel matches the rest of the game and follows its theme.

## If something goes wrong

1. **A toolbar button is missing.** The mod that adds it is not part of this game; check the game's scheme in **Mods & Collections**.
2. **Save is greyed out.** The current scene does not allow saving; move to another place and try again. Authors allow it with `save_allowed: true` in the header of one of the scene's scenarios.
3. **A panel opens but is empty.** The panel's mod has nothing to show yet in this game — for example, no quests have started.
