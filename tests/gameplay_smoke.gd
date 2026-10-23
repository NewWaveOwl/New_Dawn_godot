extends SceneTree

var errors := 0
var fixture := "__codex_gameplay_%d" % Time.get_ticks_usec()
var map_path := ""
var template_paths: Array[String] = []
var created_saves: Array[String] = []
var autosave_created := false

func _initialize() -> void:
    run.call_deferred()

func check(condition: bool, text: String) -> void:
    if not condition:
        push_error(text)
        errors += 1

func widget(name: String) -> Node:
    return current_scene.find_child(name, true, false)

func close_message() -> void:
    current_scene.get_node("Message").confirmed.emit()
    await process_frame

func click(canvas: Control, point: Vector2) -> void:
    var event := InputEventMouseButton.new()
    event.button_index = MOUSE_BUTTON_LEFT
    event.pressed = true
    event.position = point
    canvas.call("_GuiInput", event)

func write_fixtures() -> void:
    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path("res://Saves/Maps"))
    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path("res://Saves/Units"))
    map_path = ProjectSettings.globalize_path("res://Saves/Maps/" + fixture + ".map")
    var file := FileAccess.open(map_path, FileAccess.WRITE)
    file.store_string("VER|5\nSIZE|12|8\n")
    for r in 8:
        for c in 12:
            file.store_string("T|%d|%d|Field\n" % [c - r / 2, r])
    file.close()
    for suffix in ["A", "B"]:
        var path := ProjectSettings.globalize_path("res://Saves/Units/" + fixture + suffix + ".csv")
        template_paths.append(path)
        file = FileAccess.open(path, FileAccess.WRITE)
        file.store_string("Name,HP,DMG,Range,Speed,Armored,CoreType,FuelConsumption,IsMech,UnitsPerTile,PriceAlloy,UnitType,Special\nTemplateRebel%s,50,10,1,4,False,None,1,False,1,17,Rebel,None\n" % suffix)
        file.close()

func run() -> void:
    write_fixtures()
    var menu: Control = load("res://scenes/menus/main_menu.tscn").instantiate()
    root.add_child(menu)
    current_scene = menu
    menu.get_node("NewGame").pressed.emit()
    await process_frame
    await process_frame
    check(current_scene.name == "NewGameSetup", "Menu opens setup")
    check(widget("UnitTemplates").disabled, "Default units disable template dropdown")
    var maps: ItemList = widget("Maps")
    var map_index := -1
    for i in maps.item_count:
        if maps.get_item_text(i).begins_with(fixture + " "):
            map_index = i
    check(map_index >= 0, "Setup lists saved maps")
    if map_index < 0:
        quit(1)
        return
    maps.select(map_index)
    maps.item_selected.emit(map_index)
    widget("CustomUnits").button_pressed = true
    var templates: OptionButton = widget("UnitTemplates")
    check(not templates.disabled, "Custom units enable dropdown")
    # Select a different item from the preselected newest item, matching WPF SelectionChanged.
    var chosen := -1
    for i in templates.item_count:
        if i > 0 and templates.get_item_text(i).begins_with(fixture):
            chosen = i
    check(chosen >= 0, "Custom fixtures available")
    if chosen < 0:
        quit(1)
        return
    var chosen_suffix := templates.get_item_text(chosen).right(1)
    templates.select(chosen)
    templates.item_selected.emit(chosen)
    check(widget("TemplateTime").text.begins_with("Last modified:"), "Template timestamp")
    if DisplayServer.get_name() != "headless":
        await RenderingServer.frame_post_draw
        root.get_texture().get_image().save_png("res://.godot/new-game-preview.png")
    widget("Play").pressed.emit()
    await process_frame
    await process_frame
    check(current_scene.name == "MainGame", "Play opens gameplay")
    check(widget("GameCanvas") != null, "Gameplay map canvas")
    var shop: ItemList = widget("Shop")
    check(shop.item_count == 7, "Custom unit plus five buildings and divider")
    var rebel_index := -1
    for i in shop.item_count:
        if shop.get_item_text(i).begins_with("TemplateRebel" + chosen_suffix):
            rebel_index = i
    check(rebel_index >= 0, "Chosen custom template used")
    check(widget("P1Alloy").text == "", "Original initial HUD omission preserved")
    widget("Move").pressed.emit()
    check(widget("Info").text == "No unit selected. Click a unit first.", "Move without selection")
    shop.select(rebel_index)
    shop.item_selected.emit(rebel_index)
    var canvas: Control = widget("GameCanvas")
    var hex_h := sqrt(3.0) * 48.0
    click(canvas, Vector2(264, 6 * hex_h + hex_h / 2))
    check(widget("Info").text.begins_with("Deployed TemplateRebel"), "Click deploys custom unit")
    check(widget("P1Alloy").text == "28" and widget("P1Fuel").text == "7", "Resource HUD after purchase")
    check(shop.get_selected_items().size() == 0, "Deployment clears shop selection")
    widget("NextTurn").pressed.emit()
    check(widget("TurnPlayer").text == "Player 2", "Next turn")
    if DisplayServer.get_name() != "headless":
        await process_frame
        await RenderingServer.frame_post_draw
        root.get_texture().get_image().save_png("res://.godot/gameplay-preview.png")
    var saves_dir := ProjectSettings.globalize_path("res://Saves/GameSaves")
    DirAccess.make_dir_recursive_absolute(saves_dir)
    var before := DirAccess.get_files_at(saves_dir)
    var now := Time.get_datetime_string_from_system().replace("-", "_").replace("T", "_").replace(":", "_")
    var expected_name := "autosave_game" + now + ".sagm"
    if not FileAccess.file_exists(saves_dir.path_join(expected_name)):
        widget("Save").pressed.emit()
        await process_frame
        check(current_scene.get_node("Message").dialog_text.replace("\\", "/") == saves_dir.replace("\\", "/"), "Game Save shows save directory")
        await close_message()
        for name in DirAccess.get_files_at(saves_dir):
            if not before.has(name):
                created_saves.append(saves_dir.path_join(name))
        check(created_saves.size() == 1, "Timestamped game save created")
        var saved := FileAccess.get_file_as_string(created_saves[0])
        check(saved.contains("G|1|1|2|False"), "Serialized turn state")
        check(saved.count("\nU|") == 3 and saved.contains("P|1|28|7"), "Saved commanders, deployment and wallet")
        widget("Load").pressed.emit()
        await process_frame
        var dialog: Window = current_scene.get_node("GameSaves")
        dialog.find_child("LoadSave", true, false).pressed.emit()
        await process_frame
        check(dialog.get_node("Message").dialog_text == "Select a save first.", "Load requires selection")
        dialog.get_node("Message").confirmed.emit()
        await process_frame
        var list: ItemList = dialog.find_child("Saves", true, false)
        var save_index := -1
        for i in list.item_count:
            if list.get_item_text(i).begins_with(created_saves[0].get_file().get_basename() + " "):
                save_index = i
        check(save_index >= 0, "New save listed")
        list.select(save_index)
        list.item_activated.emit(save_index)
        await process_frame
        await process_frame
        check(current_scene.name == "MainGame", "Load reopens gameplay")
        widget("NextTurn").pressed.emit()
        check(widget("TurnPlayer").text == "Player 1", "Loaded Player 2 turn resumes correctly")
    var autosave := saves_dir.path_join("autosave_current.sagm")
    if not FileAccess.file_exists(autosave):
        widget("Home").pressed.emit()
        await process_frame
        await process_frame
        check(current_scene.name == "MainMenu" and FileAccess.file_exists(autosave), "Home autosaves and returns")
        autosave_created = true
    else:
        # Avoid replacing an existing user autosave. The isolated C# test covers Home.
        change_scene_to_file("res://scenes/menus/main_menu.tscn")
        await process_frame
    DirAccess.remove_absolute(map_path)
    for path in template_paths:
        DirAccess.remove_absolute(path)
    for path in created_saves:
        DirAccess.remove_absolute(path)
    if autosave_created:
        DirAccess.remove_absolute(autosave)
    print("Setup/gameplay scene checks: ", "PASS" if errors == 0 else "FAIL (%d)" % errors)
    quit(0 if errors == 0 else 1)
