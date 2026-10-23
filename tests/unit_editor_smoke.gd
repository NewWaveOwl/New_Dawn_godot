extends SceneTree

var editor: Control
var errors := 0
var csv_path := ProjectSettings.globalize_path("res://.godot/unit-editor-smoke.csv")

func _initialize() -> void:
    run.call_deferred()

func check(condition: bool, text: String) -> void:
    if not condition:
        push_error(text)
        errors += 1

func row(index: int) -> Node:
    return editor.find_child("Row%d" % index, true, false).get_node("Cells")

func widget(name: String) -> Node:
    return editor.find_child(name, true, false)

func close_message() -> void:
    var message := editor.get_node("Message")
    message.confirmed.emit()
    await process_frame

func save_to(path: String) -> void:
    widget("Save").pressed.emit()
    await process_frame
    var picker: FileDialog = editor.get_node("SaveFile")
    check(picker.current_file == "MyUnits.csv", "Default save suggestion")
    picker.hide()
    picker.file_selected.emit(path)
    await process_frame
    check(editor.get_node("Message").dialog_text == "Units saved successfully!", "Save success message")
    await close_message()

func open_editor() -> void:
    current_scene.get_node("UnitEditor").pressed.emit()
    await process_frame
    await process_frame
    editor = current_scene

func run() -> void:
    var menu: Control = load("res://scenes/menus/main_menu.tscn").instantiate()
    root.add_child(menu)
    current_scene = menu
    await open_editor()
    check(editor.name == "UnitEditor", "Menu opens editor")
    var rows: Node = widget("Rows")
    check(rows.get_child_count() > 0, "Default rows loaded")
    if not FileAccess.file_exists(ProjectSettings.globalize_path("res://Saves/Units/DefaultUnits.csv")):
        check(rows.get_child_count() == 14, "Original 14 fallback units")
        check(row(0).get_node("UnitName").text == "Railgunner", "Original default row order")
        check(row(13).get_node("UnitName").text == "Rebel", "Rebel remains the last row")
    check(row(0).get_node("UnitName") is Label, "Names are read only")
    check(row(0).get_node("Special").item_count == 5, "Special column contains five original core types")
    # Use an isolated fixture with a known name and hidden values.
    var file := FileAccess.open(csv_path, FileAccess.WRITE)
    file.store_string("Name,HP,DMG,Range,Speed,Armored,CoreType,FuelConsumption,IsMech,UnitsPerTile,PriceAlloy,UnitType,Special\nRailgunner,100,75,3,2,False,None,7,True,1,123,Railgunner,RailgunnerSpecial\nUnknown,55,10,1,4,False,None,2,False,1,15,Rebel,None\n")
    file.close()
    widget("Load").pressed.emit()
    await process_frame
    var picker: FileDialog = editor.get_node("LoadFile")
    check(picker.title == "Load Unit Templates", "Load dialog title")
    picker.hide()
    picker.file_selected.emit(csv_path)
    await process_frame
    check(editor.get_node("Message").dialog_text == "Units loaded successfully!", "Load success message")
    await close_message()
    check(rows.get_child_count() == 2, "Load replaces rows")
    check(row(0).get_node("Price").text == "123", "Loaded price")
    row(0).get_node("Price").text = "-20"
    row(0).get_node("HP").text = "999"
    row(0).get_node("DMG").text = "-1"
    row(0).get_node("Range").text = "99"
    row(0).get_node("Speed").text = "-5"
    row(0).get_node("Armored").button_pressed = true
    row(0).get_node("Special").select(1)
    row(0).get_node("Special").item_selected.emit(1)
    await save_to(csv_path)
    var saved := FileAccess.get_file_as_string(csv_path)
    check(saved.contains("Railgunner,250,1,5,0,True,SafeEjection,7,True,1,0,Railgunner,RailgunnerSpecial"), "Original clamps, core, hidden fields, and type inference")
    check(saved.contains("Unknown,55,10,1,4,False,None,2,False,1,15,Rebel,None"), "Unknown unit names retain Rebel fallback")
    # Invalid numeric text retains the last committed model value.
    row(0).get_node("HP").text = "invalid"
    await save_to(csv_path)
    check(FileAccess.get_file_as_string(csv_path) == saved, "Invalid numeric edit does not replace model value")
    # Bad CSV reports the original parser error without losing displayed rows.
    file = FileAccess.open(csv_path, FileAccess.WRITE)
    file.store_string("header\nbad\n")
    file.close()
    widget("Load").pressed.emit()
    await process_frame
    picker = editor.get_node("LoadFile")
    picker.hide()
    picker.file_selected.emit(csv_path)
    await process_frame
    check(editor.get_node("Message").dialog_text.contains("not enough fields"), "Malformed CSV error")
    await close_message()
    check(rows.get_child_count() == 2, "Failed load retains rows")
    widget("Home").pressed.emit()
    await process_frame
    editor.get_node("HomePrompt").canceled.emit()
    await process_frame
    check(current_scene == editor, "Home Cancel stays in editor")
    widget("Save").pressed.emit()
    await process_frame
    picker = editor.get_node("SaveFile")
    picker.hide()
    picker.canceled.emit()
    await process_frame
    check(current_scene == editor, "Ordinary Save cancellation stays in editor")
    widget("Home").pressed.emit()
    await process_frame
    editor.get_node("HomePrompt").custom_action.emit("no")
    await process_frame
    await process_frame
    check(current_scene.name == "MainMenu", "Home No returns to menu")
    await open_editor()
    widget("Home").pressed.emit()
    await process_frame
    editor.get_node("HomePrompt").confirmed.emit()
    await process_frame
    picker = editor.get_node("SaveFile")
    picker.hide()
    picker.canceled.emit()
    await process_frame
    await process_frame
    check(current_scene.name == "MainMenu", "Legacy Home Yes then canceled save still returns home")
    await open_editor()
    widget("Home").pressed.emit()
    await process_frame
    editor.get_node("HomePrompt").confirmed.emit()
    await process_frame
    picker = editor.get_node("SaveFile")
    picker.hide()
    picker.file_selected.emit(csv_path)
    await process_frame
    check(editor.get_node("Message").dialog_text == "Units saved successfully!", "Home Yes saves before exit")
    check(current_scene == editor, "Save result is acknowledged before leaving")
    await close_message()
    await process_frame
    check(current_scene.name == "MainMenu", "Home Yes returns after save acknowledgement")
    DirAccess.remove_absolute(csv_path)
    print("Unit editor smoke checks: ", "PASS" if errors == 0 else "FAIL (%d)" % errors)
    quit(0 if errors == 0 else 1)
