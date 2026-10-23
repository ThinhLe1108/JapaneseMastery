extends Control

const Curriculum = preload("res://scripts/Curriculum.gd")

var vocab_data = []
var current_idx = 0
var state = "READING" # READING, WRITING, SUMMARY

var word_label: Label
var romaji_label: Label
var draw_prompt: Label
var btn_next: Button
var draw_canvas: Control
var tool_container: HBoxContainer

var kanjivg_data = {}
var bg_strokes = []
var current_stroke_idx = 0
var current_stroke_progress = 0
var active_stroke_points = []
var is_tracing = false
var trace_tolerance = 45.0
var current_stroke_width = 16.0

func _ready():
	var level = Global.get_current_level()
	if Global.get("solo_target_level") != null:
		level = Global.solo_target_level
	setup_ui()
	_load_kanjivg()
	
	word_label.text = "Loading data..."
	word_label.add_theme_font_size_override("font_size", 60)
	word_label.modulate = Color.WHITE
	romaji_label.hide()
	
	_fetch_vocab_from_server(level)

func _fetch_vocab_from_server(level: int):
	var level_data = Curriculum.get_level(level)
	if level_data.size() > 0:
		vocab_data = level_data
		show_reading_phase()
		return
		
	var req = HTTPRequest.new()
	add_child(req)
	req.request_completed.connect(_on_vocab_fetched.bind(req, level))
	
	var headers = PackedStringArray(["Authorization: Bearer " + Network.jwt_token])
	var user_id = Database.get_account().get("user_id", -1)
	var url = Network.BASE_URL + "/player/vocabularies?level=%d" % level
	if user_id != -1:
		url += "&userId=%d" % user_id
	req.request(url, headers, HTTPClient.METHOD_GET)

func _on_vocab_fetched(result, code, headers, body, req: HTTPRequest, level: int):
	req.queue_free()
	if code == 200:
		var json = JSON.new()
		if json.parse(body.get_string_from_utf8()) == OK:
			var data = json.data
			if typeof(data) == TYPE_ARRAY and data.size() > 0:
				vocab_data = []
				for item in data:
					vocab_data.append({
						"kana": item.get("wordJp", ""),
						"romaji": item.get("romaji", ""),
						"meaning": item.get("meaning", "")
					})
				show_reading_phase()
				return
				
	vocab_data = [{"kana": "Error", "romaji": "Failed to fetch from server"}]
	show_reading_phase()

func setup_ui():
	var bg = ColorRect.new()
	bg.color = Color(0.12, 0.12, 0.16)
	bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(bg)
	
	draw_canvas = Control.new()
	draw_canvas.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	draw_canvas.mouse_filter = Control.MOUSE_FILTER_PASS
	draw_canvas.gui_input.connect(_on_canvas_gui_input)
	add_child(draw_canvas)
	
	var btn_back = Button.new()
	btn_back.text = "Back to Menu"
	btn_back.position = Vector2(20, 20)
	btn_back.custom_minimum_size = Vector2(150, 40)
	btn_back.pressed.connect(func(): Global.goto_scene("res://scenes/Main.tscn"))
	add_child(btn_back)
	
	var btn_skip = Button.new()
	btn_skip.text = "Skip ⏭️"
	btn_skip.set_anchors_and_offsets_preset(Control.PRESET_TOP_RIGHT, Control.PRESET_MODE_MINSIZE, 20)
	btn_skip.custom_minimum_size = Vector2(150, 40)
	btn_skip.pressed.connect(_skip_vocab)
	add_child(btn_skip)
	
	word_label = Label.new()
	word_label.add_theme_font_size_override("font_size", 200)
	word_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	word_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	word_label.set_anchors_preset(Control.PRESET_CENTER)
	word_label.grow_horizontal = Control.GROW_DIRECTION_BOTH
	word_label.grow_vertical = Control.GROW_DIRECTION_BOTH
	word_label.position.y = -50
	add_child(word_label)
	
	romaji_label = Label.new()
	romaji_label.add_theme_font_size_override("font_size", 32)
	romaji_label.set_anchors_preset(Control.PRESET_TOP_WIDE)
	romaji_label.offset_left = 50
	romaji_label.offset_top = 80
	romaji_label.offset_right = -200 # Avoid overlapping with Skip button
	romaji_label.autowrap_mode = TextServer.AUTOWRAP_WORD
	romaji_label.modulate = Color(0.6, 0.8, 1.0)
	add_child(romaji_label)
	
	draw_prompt = Label.new()
	draw_prompt.text = "Trace the character"
	draw_prompt.add_theme_font_size_override("font_size", 36)
	draw_prompt.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	draw_prompt.set_anchors_preset(Control.PRESET_TOP_WIDE)
	draw_prompt.position.y = 80
	draw_prompt.modulate = Color.WHITE
	draw_prompt.hide()
	add_child(draw_prompt)
	
	tool_container = HBoxContainer.new()
	tool_container.hide()
	add_child(tool_container)
	
	btn_next = Button.new()
	btn_next.text = "Continue"
	btn_next.custom_minimum_size = Vector2(250, 60)
	btn_next.add_theme_font_size_override("font_size", 24)
	btn_next.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	btn_next.grow_horizontal = Control.GROW_DIRECTION_BOTH
	btn_next.position.y = -100
	btn_next.pressed.connect(_on_next_pressed)
	add_child(btn_next)

func show_reading_phase():
	state = "READING"
	draw_canvas.hide()
	tool_container.hide()
	draw_prompt.hide()
	
	var vocab = vocab_data[current_idx]
	word_label.add_theme_font_size_override("font_size", 200)
	word_label.text = vocab["kana"]
	word_label.modulate = Color.WHITE
	
	romaji_label.text = "Reading: " + vocab["romaji"]
	if vocab.has("meaning") and vocab["meaning"] != "":
		romaji_label.text += "\nMeaning: " + vocab["meaning"]
	romaji_label.show()
	romaji_label.modulate = Color(0.6, 0.8, 1.0)
	
	btn_next.show()
	btn_next.text = "Start Tracing"

func show_writing_phase():
	state = "WRITING"
	word_label.modulate = Color(1, 1, 1, 0.0) # Hide text, show only paths
	romaji_label.hide()
	
	draw_canvas.show()
	tool_container.hide() # Tracing doesn't need pen/eraser tools
	draw_prompt.show()
	draw_prompt.text = "Trace the character"
	draw_prompt.modulate = Color.WHITE
	
	btn_next.hide()
	_start_tracing_mode()

func show_summary():
	state = "SUMMARY"
	draw_canvas.hide()
	tool_container.hide()
	draw_prompt.hide()
	
	word_label.text = "Completed Level " + str(Global.get_current_level()) + "!"
	word_label.add_theme_font_size_override("font_size", 60)
	word_label.modulate = Color.WHITE
	
	romaji_label.text = "Take the Level-up Test to unlock the next level."
	romaji_label.show()
	
	btn_next.text = "Main Menu"
	btn_next.show()

func _skip_vocab():
	current_idx += 1
	if current_idx >= vocab_data.size():
		show_summary()
	else:
		show_reading_phase()

func _on_next_pressed():
	if state == "READING":
		show_writing_phase()
	elif state == "WRITING":
		current_idx += 1
		if current_idx >= vocab_data.size():
			show_summary()
		else:
			show_reading_phase()
	elif state == "SUMMARY":
		Global.goto_scene("res://scenes/Main.tscn")

func _load_kanjivg():
	if not FileAccess.file_exists("res://kanjivg_paths.json"):
		return
	var file = FileAccess.open("res://kanjivg_paths.json", FileAccess.READ)
	if file:
		var json = JSON.new()
		if json.parse(file.get_as_text()) == OK:
			kanjivg_data = json.data

func _start_tracing_mode():
	current_stroke_idx = 0
	current_stroke_progress = 0
	active_stroke_points.clear()
	is_tracing = false
	bg_strokes.clear()
	
	var vocab = vocab_data[current_idx]["kana"]
	
	draw_prompt.text = "Trace the word"
	draw_prompt.modulate = Color.WHITE
	btn_next.hide()
	
	var max_scale = 4.0
	var screen_width = get_viewport_rect().size.x - 200.0
	var required_width = vocab.length() * 109.0 + (vocab.length() - 1) * 20.0
	var scale_factor = min(max_scale, screen_width / required_width)
	
	current_stroke_width = 16.0 * (scale_factor / max_scale)
	
	var char_size = 109.0 * scale_factor
	var spacing = 20.0 * (scale_factor / max_scale)
	var total_width = vocab.length() * char_size + (vocab.length() - 1) * spacing
	
	var center = get_viewport_rect().size / 2.0
	var start_x = center.x - total_width / 2.0
	var start_y = center.y - char_size / 2.0
	
	for i in range(vocab.length()):
		var char_to_trace = vocab[i]
		var char_offset = Vector2(start_x + i * (char_size + spacing), start_y)
		
		if kanjivg_data.has(char_to_trace):
			var paths = kanjivg_data[char_to_trace]
			for path_str in paths:
				var curve = parse_svg_path(path_str)
				var tessellated = curve.tessellate(5, 4.0)
				var final_points = []
				for pt in tessellated:
					final_points.append(pt * scale_factor + char_offset)
				bg_strokes.append(final_points)
				
	_update_trace_visuals()

func parse_svg_path(path_str: String) -> Curve2D:
	var curve = Curve2D.new()
	var regex = RegEx.new()
	regex.compile("([A-Za-z])|(-?[0-9]*\\.?[0-9]+)")
	var matches = regex.search_all(path_str)
	
	var i = 0
	var cmd = ""
	var current_pos = Vector2()
	
	while i < matches.size():
		var m = matches[i].get_string()
		if m.length() == 1 and m.to_lower() >= "a" and m.to_lower() <= "z":
			cmd = m
			i += 1
		else:
			pass # implicit repeated command
			
		if i >= matches.size(): break
			
		if cmd == "M":
			var x = matches[i].get_string().to_float()
			var y = matches[i+1].get_string().to_float()
			current_pos = Vector2(x, y)
			curve.add_point(current_pos)
			i += 2
		elif cmd == "c":
			var dx1 = matches[i].get_string().to_float()
			var dy1 = matches[i+1].get_string().to_float()
			var dx2 = matches[i+2].get_string().to_float()
			var dy2 = matches[i+3].get_string().to_float()
			var dx = matches[i+4].get_string().to_float()
			var dy = matches[i+5].get_string().to_float()
			var p_next = current_pos + Vector2(dx, dy)
			
			var pt_count = curve.get_point_count()
			if pt_count > 0:
				curve.set_point_out(pt_count - 1, Vector2(dx1, dy1))
			curve.add_point(p_next, Vector2(dx2, dy2) - Vector2(dx, dy))
			current_pos = p_next
			i += 6
		elif cmd == "s":
			var dx2 = matches[i].get_string().to_float()
			var dy2 = matches[i+1].get_string().to_float()
			var dx = matches[i+2].get_string().to_float()
			var dy = matches[i+3].get_string().to_float()
			
			var pt_count = curve.get_point_count()
			if pt_count > 0:
				var prev_in = curve.get_point_in(pt_count - 1)
				curve.set_point_out(pt_count - 1, -prev_in)
				
			var p_next = current_pos + Vector2(dx, dy)
			curve.add_point(p_next, Vector2(dx2, dy2) - Vector2(dx, dy))
			current_pos = p_next
			i += 4
		else:
			i += 1
	return curve

func _update_trace_visuals():
	for child in draw_canvas.get_children():
		child.queue_free()
		
	if bg_strokes.is_empty():
		draw_prompt.text = "No tracing data available for this word."
		btn_next.show()
		btn_next.disabled = false
		return
		
	for i in range(bg_strokes.size()):
		var line = Line2D.new()
		line.width = current_stroke_width
		line.antialiased = true
		line.begin_cap_mode = Line2D.LINE_CAP_ROUND
		line.end_cap_mode = Line2D.LINE_CAP_ROUND
		line.joint_mode = Line2D.LINE_JOINT_ROUND
		
		# Use solid colors (alpha = 1.0) to prevent Godot's Line2D from showing darker spots 
		# where joint triangles overlap.
		line.default_color = Color(0.25, 0.25, 0.25, 1.0) if i >= current_stroke_idx else Color(0.15, 0.55, 0.95, 1.0)
		if i == current_stroke_idx:
			line.default_color = Color(0.5, 0.5, 0.5, 1.0)
			
		for pt in bg_strokes[i]:
			line.add_point(pt)
		draw_canvas.add_child(line)
		
	if current_stroke_idx < bg_strokes.size():
		var active_line = Line2D.new()
		active_line.width = current_stroke_width
		active_line.antialiased = true
		active_line.begin_cap_mode = Line2D.LINE_CAP_ROUND
		active_line.end_cap_mode = Line2D.LINE_CAP_ROUND
		active_line.joint_mode = Line2D.LINE_JOINT_ROUND
		active_line.default_color = Color(0.4, 0.9, 1.0, 1.0)
		for pt in active_stroke_points:
			active_line.add_point(pt)
		draw_canvas.add_child(active_line)
	else:
		draw_prompt.text = "Perfect!"
		draw_prompt.modulate = Color(0.4, 1.0, 0.4)
		btn_next.show()
		btn_next.disabled = false

func _on_canvas_gui_input(event):
	if state != "WRITING": return
	
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT:
			is_tracing = event.pressed
			if is_tracing:
				_process_tracing(event.global_position)
	elif event is InputEventMouseMotion and is_tracing:
		_process_tracing(event.global_position)

func _process_tracing(pos: Vector2):
	if current_stroke_idx >= bg_strokes.size(): return
	
	var current_stroke = bg_strokes[current_stroke_idx]
	if current_stroke_progress < current_stroke.size():
		var advanced = false
		for i in range(15):
			var check_idx = current_stroke_progress + i
			if check_idx >= current_stroke.size(): break
			var target_pt = current_stroke[check_idx]
			if pos.distance_to(target_pt) < trace_tolerance:
				while current_stroke_progress <= check_idx:
					active_stroke_points.append(current_stroke[current_stroke_progress])
					current_stroke_progress += 1
				advanced = true
				break
				
		if advanced:
			_update_trace_visuals()
			
			if current_stroke_progress >= current_stroke.size():
				current_stroke_idx += 1
				current_stroke_progress = 0
				active_stroke_points.clear()
				is_tracing = false
				_update_trace_visuals()
				
				if current_stroke_idx >= bg_strokes.size():
					btn_next.show()
					btn_next.disabled = false
