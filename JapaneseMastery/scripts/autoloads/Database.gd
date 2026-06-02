extends Node

const SAVE_PATH = "user://user_data.json"

var data = {
	"account": {
		"display_name": "New Player",
		"current_level": 1,
		"g_coin_balance": 0,
		"role": "PLAYER"
	},
	"memory_records": {}
}

func _ready():
	load_data()

func save_data():
	var file = FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	if file:
		var json_string = JSON.stringify(data)
		file.store_string(json_string)
		file.close()
	else:
		push_error("Could not save to " + SAVE_PATH)

func load_data():
	if not FileAccess.file_exists(SAVE_PATH):
		save_data() # Create initial file
		return
		
	var file = FileAccess.open(SAVE_PATH, FileAccess.READ)
	if file:
		var json_string = file.get_as_text()
		var json = JSON.new()
		var error = json.parse(json_string)
		if error == OK:
			var loaded_data = json.data
			if loaded_data.has("account"):
				data["account"] = loaded_data["account"]
				if not data["account"].has("current_level"):
					data["account"]["current_level"] = 1
			if loaded_data.has("memory_records"):
				data["memory_records"] = loaded_data["memory_records"]
		else:
			push_error("Error parsing save data JSON.")
		file.close()

func get_account():
	return data["account"]

func update_account(key: String, value: Variant):
	data["account"][key] = value
	save_data()

func get_memory_record(kanji: String) -> Dictionary:
	if data["memory_records"].has(kanji):
		return data["memory_records"][kanji]
	return {}

func update_memory_record(kanji: String, record_data: Dictionary):
	data["memory_records"][kanji] = record_data
	save_data()
