extends SceneTree

var editor: Control
var test_path := ProjectSettings.globalize_path("res://.godot/editor-smoke.map")
var errors := 0

func _initialize() -> void:
    run.call_deferred()

func check(condition: bool, message: String) -> void:
    if not condition:
        push_error(message)
        errors += 1

func widget(name: String) -> Node:
    return editor.find_child(name, true, false)

func click_tile(canvas: Control, point: Vector2, button: MouseButton) -> void:
    var event := InputEventMouseButton.new()
    event.position = point
    event.button_index = button
    event.pressed = true
    canvas.call("_GuiInput", event)

func run() -> void:
    var menu: Control = load("res://scenes/menus/main_menu.tscn").instantiate()
    root.add_child(menu)
    current_scene = menu
    menu.get_node("MapEditor").pressed.emit()
    await process_frame
    await process_frame
    editor = current_scene
    check(editor.name == "MapEditor", "Menu must navigate to editor")
    var palette: ItemList = widget("Palette")
    var canvas: Control = widget("MapCanvas")
    check(palette.item_count == 9, "Nine terrain palette entries")
    check(palette.get_item_text(0) == "Field", "Field is first/default")
    for i in palette.item_count:
        check(palette.get_item_icon(i) != null, "Every terrain has a texture")
    widget("MapName").text = test_path
    widget("Save").pressed.emit()
    var initial := FileAccess.get_file_as_string(test_path)
    check(initial.contains("VER|5") and initial.contains("SIZE|12|8"), "Default map header")
    check(initial.count("T|") == 96, "Default tile count")
    check(initial.count("|Field") == 96, "Default terrain is Field")
    check(initial.contains("T|-3|6|Field"), "Legacy axial row offset")

    palette.select(1)
    palette.item_selected.emit(1)
    click_tile(canvas, Vector2(48, 41), MOUSE_BUTTON_LEFT)
    var motion := InputEventMouseMotion.new()
    motion.position = Vector2(120, 83)
    motion.button_mask = MOUSE_BUTTON_MASK_LEFT
    canvas.call("_GuiInput", motion)
    var release := InputEventMouseButton.new()
    release.button_index = MOUSE_BUTTON_LEFT
    release.pressed = false
    canvas.call("_Input", release)
    click_tile(canvas, Vector2(192, 41), MOUSE_BUTTON_RIGHT)
    widget("Save").pressed.emit()
    var painted := FileAccess.get_file_as_string(test_path)
    check(painted.contains("T|0|0|Forest"), "Left paint")
    check(painted.contains("T|1|0|Forest"), "Drag paint")
    check(painted.contains("T|2|0|Mountain"), "Right paint is Mountain")
    # Once stopped, hover must not paint.
    motion.position = Vector2(264, 83)
    canvas.call("_GuiInput", motion)
    widget("Save").pressed.emit()
    check(FileAccess.get_file_as_string(test_path) == painted, "Stop painting on release/exit")

    widget("GridWidth").text = "2"
    widget("GridHeight").text = "3"
    widget("Create").pressed.emit()
    widget("Load").pressed.emit()
    check(widget("GridWidth").text == "12" and widget("GridHeight").text == "8", "Load restores dimensions")
    widget("Save").pressed.emit()
    check(FileAccess.get_file_as_string(test_path) == painted, "Painted map round trip")
    widget("GridWidth").text = "bad"
    widget("GridHeight").text = "0"
    widget("Create").pressed.emit()
    widget("Save").pressed.emit()
    check(FileAccess.get_file_as_string(test_path).contains("SIZE|12|8"), "Invalid inputs retain legacy defaults")
    widget("GridWidth").text = "999"
    widget("GridHeight").text = "1"
    widget("Create").pressed.emit()
    widget("Save").pressed.emit()
    check(FileAccess.get_file_as_string(test_path).contains("SIZE|200|1"), "Dimensions clamp to 200")
    widget("GridWidth").text = "200"
    widget("GridHeight").text = "200"
    widget("Create").pressed.emit()
    widget("Save").pressed.emit()
    check(FileAccess.get_file_as_string(test_path).count("T|") == 40000, "Maximum grid preserves every tile")
    var map_scroll: ScrollContainer = widget("MapScroll")
    await process_frame
    map_scroll.scroll_horizontal = 1000
    map_scroll.scroll_vertical = 1000
    await process_frame
    check(map_scroll.scroll_horizontal > 0 and map_scroll.scroll_vertical > 0, "Large map scrolls in both directions")
    # Exercise the saved-map list and selection precedence with a unique file.
    var list_name := "__codex_editor_%d" % Time.get_ticks_usec()
    var list_path := ProjectSettings.globalize_path("res://Saves/Maps/" + list_name + ".map")
    check(not FileAccess.file_exists(list_path), "List fixture must be new")
    widget("GridWidth").text = "2"
    widget("GridHeight").text = "3"
    widget("Create").pressed.emit()
    widget("MapName").text = list_path
    widget("Save").pressed.emit()
    var maps: ItemList = widget("MapList")
    var list_index := -1
    for i in maps.item_count:
        if maps.get_item_text(i).begins_with(list_name + "\n"):
            list_index = i
    check(list_index >= 0, "Save refreshes map list with timestamp")
    if list_index >= 0:
        maps.select(list_index)
        maps.item_selected.emit(list_index)
        check(widget("MapName").text == list_name, "Selection sets map name")
        widget("MapName").text = "does-not-exist"
        widget("Load").pressed.emit()
        check(widget("GridWidth").text == "2" and widget("GridHeight").text == "3", "Selected map takes precedence over typed name")
    maps.deselect_all()
    DirAccess.remove_absolute(list_path)
    widget("MapName").text = test_path
    # Invalid save version must show the original error and retain the current map.
    var file := FileAccess.open(test_path, FileAccess.WRITE)
    file.store_string("VER|4\nSIZE|2|2\n")
    file.close()
    widget("Load").pressed.emit()
    await process_frame
    var dialog := editor.get_child(editor.get_child_count() - 1)
    check(dialog is AcceptDialog and dialog.dialog_text.contains("Unsupported save version 4. Expected 5."), "Legacy version rejection")
    dialog.confirmed.emit()
    await process_frame
    widget("MapName").text = ""
    widget("Save").pressed.emit()
    await process_frame
    dialog = editor.get_child(editor.get_child_count() - 1)
    check(dialog is AcceptDialog and dialog.dialog_text == "Please enter a map name.", "Blank save name validation")
    dialog.confirmed.emit()
    await process_frame
    DirAccess.remove_absolute(test_path)
    widget("Home").pressed.emit()
    await process_frame
    await process_frame
    check(current_scene.name == "MainMenu", "Home returns to menu")
    print("Map editor smoke checks: ", "PASS" if errors == 0 else "FAIL (%d)" % errors)
    quit(0 if errors == 0 else 1)
