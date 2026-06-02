extends Control

const Curriculum = preload("res://scripts/Curriculum.gd")

var vocab_data = []
var current_idx = 0
var state = "READING" # READING, WRITING, SUMMARY

var word_label: Label
var romaji_label: Label
var draw_prompt: Label
var btn_next: Button
var btn_clear: Button
var draw_canvas: Control
var current_line: Line2D
var all_lines: Array = []
var is_drawing = false
var undo_count = 0

var tool_container: HBoxContainer
var btn_pen: Button
var btn_eraser: Button
var current_tool = "PEN"

# Validation Viewport
var val_viewport: SubViewport
var val_bg: ColorRect
var val_label: Label
var val_draw_node: Node2D
var ref_image: Image

func _ready():
	var level = Global.get_current_level()
	setup_ui()
	setup_validation_viewport()
	
	word_label.text = "Đang tải dữ liệu..."
	word_label.add_theme_font_size_override("font_size", 60)
	word_label.modulate = Color.WHITE
	romaji_label.hide()
	
	_fetch_vocab_from_server(level)

func _fetch_vocab_from_server(level: int):
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
				
	# Fallback if server fails or empty
	if Curriculum.LEVELS.has(level):
		vocab_data = Curriculum.LEVELS[level]
	else:
		vocab_data = [{"kana": "Error", "romaji": "Failed to fetch from server"}]
	
	show_reading_phase()

func setup_ui():
	# Background
	var bg = ColorRect.new()
	bg.color = Color(0.12, 0.12, 0.16)
	bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(bg)
	
	# Back Button
	var btn_back = Button.new()
	btn_back.text = "Trở về Menu"
	btn_back.position = Vector2(20, 20)
	btn_back.custom_minimum_size = Vector2(150, 40)
	btn_back.pressed.connect(func(): Global.goto_scene("res://scenes/Main.tscn"))
	add_child(btn_back)
	
	# Skip Button
	var btn_skip = Button.new()
	btn_skip.text = "Bỏ qua ⏭️"
	btn_skip.set_anchors_and_offsets_preset(Control.PRESET_TOP_RIGHT, Control.PRESET_MODE_MINSIZE, 20)
	btn_skip.custom_minimum_size = Vector2(150, 40)
	btn_skip.pressed.connect(_skip_vocab)
	add_child(btn_skip)
	
	# Kana Label
	word_label = Label.new()
	word_label.add_theme_font_size_override("font_size", 200)
	word_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	word_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	word_label.set_anchors_preset(Control.PRESET_CENTER)
	word_label.grow_horizontal = Control.GROW_DIRECTION_BOTH
	word_label.grow_vertical = Control.GROW_DIRECTION_BOTH
	word_label.position.y = -50
	add_child(word_label)
	
	# Romaji Label
	romaji_label = Label.new()
	romaji_label.add_theme_font_size_override("font_size", 40)
	romaji_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	romaji_label.set_anchors_preset(Control.PRESET_CENTER)
	romaji_label.grow_horizontal = Control.GROW_DIRECTION_BOTH
	romaji_label.position.y = 150
	romaji_label.modulate = Color(0.6, 0.8, 1.0)
	add_child(romaji_label)
	
	# Draw Prompt
	draw_prompt = Label.new()
	draw_prompt.text = "Hãy vẽ đè lên chữ trên để luyện tập!"
	draw_prompt.add_theme_font_size_override("font_size", 24)
	draw_prompt.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	draw_prompt.set_anchors_preset(Control.PRESET_TOP_WIDE)
	draw_prompt.position.y = 80
	draw_prompt.modulate = Color(1.0, 0.8, 0.4)
	draw_prompt.hide()
	add_child(draw_prompt)
	
	# Draw Canvas
	draw_canvas = Control.new()
	draw_canvas.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	draw_canvas.mouse_filter = Control.MOUSE_FILTER_PASS
	draw_canvas.gui_input.connect(_on_canvas_gui_input)
	add_child(draw_canvas)
	
	# Toolbox (Pen, Eraser, Undo, Clear)
	var tool_box = HBoxContainer.new()
	tool_box.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	tool_box.grow_horizontal = Control.GROW_DIRECTION_BOTH
	tool_box.position.y = -180
	tool_box.add_theme_constant_override("separation", 20)
	tool_box.alignment = BoxContainer.ALIGNMENT_CENTER
	add_child(tool_box)
	
	btn_pen = Button.new()
	btn_pen.text = "✏️ Bút vẽ"
	btn_pen.custom_minimum_size = Vector2(150, 50)
	btn_pen.pressed.connect(func(): set_tool("PEN"))
	tool_box.add_child(btn_pen)
	
	btn_eraser = Button.new()
	btn_eraser.text = "🧽 Cục tẩy"
	btn_eraser.custom_minimum_size = Vector2(150, 50)
	btn_eraser.pressed.connect(func(): set_tool("ERASER"))
	tool_box.add_child(btn_eraser)
	
	var btn_undo = Button.new()
	btn_undo.text = "↩️ Hoàn tác"
	btn_undo.custom_minimum_size = Vector2(150, 50)
	btn_undo.pressed.connect(_undo_stroke)
	tool_box.add_child(btn_undo)
	
	btn_clear = Button.new()
	btn_clear.text = "🗑️ Xóa tất cả"
	btn_clear.custom_minimum_size = Vector2(150, 50)
	btn_clear.pressed.connect(_clear_drawing)
	tool_box.add_child(btn_clear)
	
	tool_container = tool_box
	tool_container.hide()
	
	# Next button
	btn_next = Button.new()
	btn_next.text = "Tiếp tục"
	btn_next.custom_minimum_size = Vector2(250, 60)
	btn_next.add_theme_font_size_override("font_size", 24)
	btn_next.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	btn_next.grow_horizontal = Control.GROW_DIRECTION_BOTH
	btn_next.position.y = -100
	btn_next.pressed.connect(_on_next_pressed)
	add_child(btn_next)

func setup_validation_viewport():
	val_viewport = SubViewport.new()
	var vp_size = get_viewport_rect().size
	if vp_size.x == 0: vp_size = Vector2(1280, 720)
	val_viewport.size = Vector2i(vp_size)
	val_viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	val_viewport.transparent_bg = false
	add_child(val_viewport)
	
	val_bg = ColorRect.new()
	val_bg.color = Color.BLACK
	val_bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	val_viewport.add_child(val_bg)
	
	val_label = Label.new()
	val_label.add_theme_font_size_override("font_size", 200)
	val_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	val_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	val_label.set_anchors_preset(Control.PRESET_CENTER)
	val_label.grow_horizontal = Control.GROW_DIRECTION_BOTH
	val_label.grow_vertical = Control.GROW_DIRECTION_BOTH
	val_label.position.y = -50
	val_label.modulate = Color.WHITE
	val_viewport.add_child(val_label)
	
	val_draw_node = Node2D.new()
	val_viewport.add_child(val_draw_node)

func set_tool(tool_name: String):
	current_tool = tool_name
	if current_tool == "PEN":
		btn_pen.modulate = Color(0.5, 0.8, 1.0)
		btn_eraser.modulate = Color.WHITE
	else:
		btn_eraser.modulate = Color(0.5, 0.8, 1.0)
		btn_pen.modulate = Color.WHITE

func show_reading_phase():
	state = "READING"
	_clear_drawing()
	draw_canvas.hide()
	tool_container.hide()
	draw_prompt.hide()
	
	var vocab = vocab_data[current_idx]
	word_label.add_theme_font_size_override("font_size", 200)
	word_label.text = vocab["kana"]
	word_label.modulate = Color.WHITE
	
	romaji_label.text = "Cách đọc: " + vocab["romaji"]
	if vocab.has("meaning") and vocab["meaning"] != "":
		romaji_label.text += "\nNghĩa: " + vocab["meaning"]
	romaji_label.show()
	
	btn_next.text = "Chuyển sang Luyện viết"

func show_writing_phase():
	state = "WRITING"
	word_label.modulate = Color(1, 1, 1, 0.2) # Faint background
	romaji_label.hide()
	
	draw_canvas.show()
	tool_container.show()
	draw_prompt.show()
	
	set_tool("PEN")
	
	btn_next.text = "Hoàn thành nét chữ"
	undo_count = 0
	
	# Prepare reference image
	var vocab = vocab_data[current_idx]
	val_label.text = vocab["kana"]
	val_label.show()
	val_draw_node.hide()
	
	await get_tree().process_frame
	await get_tree().process_frame
	
	ref_image = val_viewport.get_texture().get_image()
	val_label.hide()

func show_summary():
	state = "SUMMARY"
	_clear_drawing()
	draw_canvas.hide()
	tool_container.hide()
	draw_prompt.hide()
	
	word_label.text = "Hoàn thành Level " + str(Global.get_current_level()) + "!"
	word_label.add_theme_font_size_override("font_size", 60)
	word_label.modulate = Color.WHITE
	
	romaji_label.text = "Hãy làm Bài Test Lên Cấp để mở khóa level tiếp theo."
	romaji_label.show()
	
	btn_next.text = "Về Menu"

func _on_next_pressed():
	if state == "READING":
		show_writing_phase()
	elif state == "WRITING":
		btn_next.disabled = true
		romaji_label.text = "Đang phân tích nét chữ..."
		romaji_label.modulate = Color.WHITE
		romaji_label.show()
		
		var validation = await validate_drawing_pixel_perfect()
		btn_next.disabled = false
		
		if not validation["valid"]:
			romaji_label.text = validation["msg"]
			romaji_label.modulate = Color(1.0, 0.4, 0.4)
			return
			
		romaji_label.text = validation["msg"]
		romaji_label.modulate = Color(0.4, 1.0, 0.4)
		
		btn_next.disabled = true
		await get_tree().create_timer(1.0).timeout
		btn_next.disabled = false
		
		current_idx += 1
		if current_idx >= vocab_data.size():
			show_summary()
		else:
			show_reading_phase()
	elif state == "SUMMARY":
		Global.goto_scene("res://scenes/Main.tscn")

func _skip_vocab():
	current_idx += 1
	if current_idx >= vocab_data.size():
		show_summary()
	else:
		show_reading_phase()

func validate_drawing_pixel_perfect() -> Dictionary:
	if all_lines.size() == 0:
		return { "valid": false, "msg": "Bạn chưa vẽ gì cả!" }
		
	# Copy user drawing to val_viewport
	for child in val_draw_node.get_children():
		child.queue_free()
		
	for line in all_lines:
		var dup = Line2D.new()
		dup.points = line.points
		dup.width = line.width
		dup.default_color = Color.WHITE
		dup.begin_cap_mode = line.begin_cap_mode
		dup.end_cap_mode = line.end_cap_mode
		dup.joint_mode = line.joint_mode
		val_draw_node.add_child(dup)
		
	val_draw_node.show()
	
	await get_tree().process_frame
	await get_tree().process_frame
	
	var user_image = val_viewport.get_texture().get_image()
	val_draw_node.hide()
	
	var target_pixels = 0
	var hit_pixels = 0
	var user_drawn_pixels = 0
	
	var template_points = []
	var user_points = []
	
	var w = ref_image.get_width()
	var h = ref_image.get_height()
	
	for y in range(0, h, 4):
		for x in range(0, w, 4):
			var is_target = ref_image.get_pixel(x, y).r > 0.5
			var is_user = user_image.get_pixel(x, y).r > 0.5
			
			if is_target:
				target_pixels += 1
				if is_user:
					hit_pixels += 1
			if is_user:
				user_drawn_pixels += 1
				
			# Mẫu điểm (Point Sampling) để tính Distance (d_i) nhẹ hơn
			if y % 8 == 0 and x % 8 == 0:
				if is_target:
					template_points.append(Vector2(x, y))
				if is_user:
					user_points.append(Vector2(x, y))

	if target_pixels == 0:
		return {"valid": false, "msg": "Lỗi hệ thống: Không có chữ mẫu."}
		
	if user_drawn_pixels == 0:
		return {"valid": false, "msg": "Bạn chưa vẽ gì cả!"}
		
	# 1. & 2. Distance and Error Score
	var d_array = []
	var sum_d = 0.0
	for up in user_points:
		var min_d_sq = 9999999.0
		for tp in template_points:
			var d_sq = up.distance_squared_to(tp)
			if d_sq < min_d_sq:
				min_d_sq = d_sq
		var d = sqrt(min_d_sq)
		d_array.append(d)
		sum_d += d
		
	var n = max(1, d_array.size())
	var e_score = sum_d / float(n)
	
	# 3. Frame
	var A = 0.0
	var B = 0.0
	var frame = max(0.0, 100.0 - 35.0*A - 10.0*B - e_score)
	
	# 4. Efficiency
	var N_strokes = float(all_lines.size())
	var N0 = N_strokes # Giả định N0 = N (do không có data template nét)
	var U = float(undo_count)
	var eff = N0 / max(1.0, N_strokes + 0.5 * U)
	
	# 5. Cleanliness
	var m = 0.0 # Giả định m=0 (chưa rõ logic)
	var c = 1.0 - 0.005 * m
	
	# 6. Smoothness
	var mu = e_score
	var sum_var = 0.0
	for d in d_array:
		sum_var += (d - mu) * (d - mu)
	var variance = sum_var / float(n)
	var sigma = sqrt(variance)
	var smooth = 1.0 - min(0.1, sigma * variance)
	
	# 7. Coverage
	var coverage_ratio = float(hit_pixels) / float(target_pixels)
	var cov_squared = coverage_ratio * coverage_ratio
	
	# 8. Final Accuracy
	var final_accuracy = frame * eff * c * smooth * cov_squared
	final_accuracy = clamp(final_accuracy, 0.0, 100.0)
	
	# Pass / Fail condition logic
	var coverage_pct = coverage_ratio * 100.0
	
	if coverage_pct < 70.0:
		return {"valid": false, "msg": "Vẽ chưa đủ nét! (Khớp: %d%%)" % int(coverage_pct)}
	
	if final_accuracy < 70.0:
		return {"valid": false, "msg": "Điểm Accuracy quá thấp: %d%%" % int(final_accuracy)}
		
	return {"valid": true, "msg": "Tuyệt vời! (Accuracy: %d%%)" % int(final_accuracy)}

func _on_canvas_gui_input(event):
	if state != "WRITING": return
	
	if current_tool == "PEN":
		if event is InputEventMouseButton:
			if event.button_index == MOUSE_BUTTON_LEFT:
				if event.pressed:
					is_drawing = true
					current_line = Line2D.new()
					current_line.width = 25.0
					current_line.default_color = Color(0.9, 0.9, 1.0)
					current_line.begin_cap_mode = Line2D.LINE_CAP_ROUND
					current_line.end_cap_mode = Line2D.LINE_CAP_ROUND
					current_line.joint_mode = Line2D.LINE_JOINT_ROUND
					draw_canvas.add_child(current_line)
					current_line.add_point(event.position)
					all_lines.append(current_line)
				else:
					is_drawing = false
		elif event is InputEventMouseMotion and is_drawing:
			if current_line:
				current_line.add_point(event.position)
				
	elif current_tool == "ERASER":
		if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
			is_drawing = event.pressed
			if is_drawing:
				_erase_stroke_at(event.position)
		elif event is InputEventMouseMotion and is_drawing:
			_erase_stroke_at(event.position)

func _erase_stroke_at(pos: Vector2):
	var to_remove = []
	for line in all_lines:
		for p in line.points:
			if p.distance_to(pos) < 30.0:
				to_remove.append(line)
				break
	for line in to_remove:
		line.queue_free()
		all_lines.erase(line)

func _undo_stroke():
	if all_lines.size() > 0:
		var last = all_lines.pop_back()
		last.queue_free()
		undo_count += 1

func _clear_drawing():
	for line in all_lines:
		line.queue_free()
	all_lines.clear()
