extends SceneTree

var errors := 0

func _initialize() -> void:
    run.call_deferred()

func check(condition: bool, message: String) -> void:
    if not condition:
        push_error(message)
        errors += 1

func run() -> void:
    # Inspect saved children before entering the tree: _Ready cannot build them.
    var cases := {
        "res://scenes/menus/main_menu.tscn": ["NewGame", "MapEditor", "UnitEditor"],
        "res://scenes/menus/new_game_setup.tscn": ["KillCommander", "CustomUnits", "Maps", "Play"],
        "res://scenes/editors/map_editor.tscn": ["MapName", "GridWidth", "MapCanvas", "Palette", "Save"],
        "res://scenes/editors/unit_editor.tscn": ["TableHeader", "Rows", "Save", "Home"],
        "res://scenes/game/main_game.tscn": ["Shop", "GameCanvas", "P1Fuel", "Move", "NextTurn", "Home"],
        "res://scenes/dialogs/game_saves.tscn": ["Saves", "LoadSave", "CloseSaves"],
        "res://scenes/editors/unit_editor_row.tscn": ["UnitName", "Price", "HP", "Special"]
    }
    for path in cases:
        var scene: Node = load(path).instantiate()
        for name in cases[path]:
            var control := scene.find_child(name, true, false)
            check(control != null and control.owner == scene, "%s saves %s" % [path, name])
        if scene.name == "NewGameSetup":
            check(scene.get_node("%DefaultUnits").button_pressed, "Saved default unit selection")
            check(scene.get_node("%UnitTemplates").disabled, "Saved dropdown starts disabled")
            check(scene.get_node("%DefaultUnits").button_group == scene.get_node("%CustomUnits").button_group, "Saved shared radio group")
            check(scene.get_node("%Maps").item_count == 0, "No machine-specific map entries saved")
        if scene.name == "MainGame":
            check(not scene.get_node("StartHint").visible, "Standalone hint hidden in editor")
        # Native controls still lay out with no root script running.
        if scene is Control and scene.name != "UnitRow":
            var name: String = scene.name
            scene.set_script(null)
            root.add_child(scene)
            await process_frame
            await process_frame
            check(scene.get_child_count() > 0, name + " has static editor layout")
            if DisplayServer.get_name() != "headless":
                await RenderingServer.frame_post_draw
                root.get_texture().get_image().save_png("res://.godot/ui-scene-" + name + ".png")
            root.remove_child(scene)
        scene.free()
    # F6 still shows the existing start-from-menu hint instead of a fake match.
    var game: Control = load("res://scenes/game/main_game.tscn").instantiate()
    root.add_child(game)
    await process_frame
    check(game.get_node("StartHint").visible, "Standalone gameplay keeps start hint")
    check(not game.get_node("Background").visible, "Standalone gameplay hides inactive controls")
    game.free()
    print("Saved UI scene checks: ", "PASS" if errors == 0 else "FAIL")
    quit(0 if errors == 0 else 1)
