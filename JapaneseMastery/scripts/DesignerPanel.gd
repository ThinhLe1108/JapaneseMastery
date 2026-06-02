extends Control

var status_label: Label
var items_container: VBoxContainer
var name_input: LineEdit
var price_input: LineEdit
var type_opt: OptionButton
var btn_add: Button

var all_items = []
var current_page = 0
var items_per_page = 6
var search_input: LineEdit
var lbl_page: Label
var btn_prev: Button
var btn_next: Button

func _ready():
	_setup_ui()
	_fetch_items()

func _setup_ui():
	# Background
	var bg = ColorRect.new()
	bg.color = Color(0.12, 0.12, 0.2)
	bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(bg)
	
	# Title
	var title = Label.new()
	title.text = "BẢNG ĐIỀU KHIỂN DESIGNER"
	title.add_theme_font_size_override("font_size", 36)
	title.set_anchors_preset(Control.PRESET_TOP_WIDE)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.position.y = 20
	add_child(title)
	
	# Back Button
	var btn_back = Button.new()
	btn_back.text = "Trở về Menu"
	btn_back.position = Vector2(20, 20)
	btn_back.custom_minimum_size = Vector2(150, 40)
	btn_back.pressed.connect(func(): Global.goto_scene("res://scenes/Main.tscn"))
	add_child(btn_back)
	
	status_label = Label.new()
	status_label.position = Vector2(20, 70)
	status_label.set_anchors_preset(Control.PRESET_TOP_WIDE)
	status_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	add_child(status_label)
	
	# Main Content Area
	var main_area = CenterContainer.new()
	main_area.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	main_area.offset_top = 100
	add_child(main_area)
	
	var vbox = VBoxContainer.new()
	vbox.custom_minimum_size = Vector2(800, 0)
	vbox.add_theme_constant_override("separation", 15)
	main_area.add_child(vbox)
	
	# Form Section
	var form_title = Label.new()
	form_title.text = "--- ĐĂNG BÁN VẬT PHẨM MỚI ---"
	form_title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(form_title)
	
	var form_hbox = HBoxContainer.new()
	form_hbox.add_theme_constant_override("separation", 10)
	vbox.add_child(form_hbox)
	
	name_input = LineEdit.new()
	name_input.placeholder_text = "Tên vật phẩm (VD: Khung rồng lửa)"
	name_input.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	form_hbox.add_child(name_input)
	
	price_input = LineEdit.new()
	price_input.placeholder_text = "Giá bán (G-Coins)"
	price_input.custom_minimum_size = Vector2(150, 0)
	form_hbox.add_child(price_input)
	
	type_opt = OptionButton.new()
	type_opt.add_item("THEME")
	type_opt.add_item("AVATAR_FRAME")
	type_opt.add_item("PARTICLE")
	type_opt.add_item("BGM")
	form_hbox.add_child(type_opt)
	
	btn_add = Button.new()
	btn_add.text = "Đăng Bán"
	btn_add.custom_minimum_size = Vector2(120, 0)
	btn_add.pressed.connect(_on_add_pressed)
	form_hbox.add_child(btn_add)
	
	var sep = HSeparator.new()
	vbox.add_child(sep)
	
	# List Section
	var list_title = Label.new()
	list_title.text = "--- DANH SÁCH VẬT PHẨM CỦA BẠN ---"
	list_title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(list_title)
	
	var filter_hbox = HBoxContainer.new()
	search_input = LineEdit.new()
	search_input.placeholder_text = "🔍 Tìm Tên / Loại..."
	search_input.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	search_input.text_changed.connect(_on_search_changed)
	filter_hbox.add_child(search_input)
	vbox.add_child(filter_hbox)
	
	var scroll = ScrollContainer.new()
	scroll.custom_minimum_size = Vector2(0, 350)
	vbox.add_child(scroll)
	
	items_container = VBoxContainer.new()
	items_container.add_theme_constant_override("separation", 10)
	scroll.add_child(items_container)
	
	var page_hbox = HBoxContainer.new()
	page_hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	btn_prev = Button.new()
	btn_prev.text = "◀ Trước"
	btn_prev.pressed.connect(_change_page.bind(-1))
	lbl_page = Label.new()
	lbl_page.text = "1 / 1"
	btn_next = Button.new()
	btn_next.text = "Sau ▶"
	btn_next.pressed.connect(_change_page.bind(1))
	page_hbox.add_child(btn_prev)
	page_hbox.add_child(lbl_page)
	page_hbox.add_child(btn_next)
	vbox.add_child(page_hbox)

func _get_headers() -> PackedStringArray:
	return PackedStringArray(["Authorization: Bearer " + Network.jwt_token, "Content-Type: application/json"])

func api_request(endpoint: String, method: int, body_str: String, callback: Callable):
	var req = HTTPRequest.new()
	add_child(req)
	req.request_completed.connect(func(res, code, headers, body):
		if code == 401:
			Network.logout(true)
			Global.goto_scene("res://scenes/Login.tscn")
			return
		callback.call(code, body)
		req.queue_free()
	)
	req.request(Network.BASE_URL + endpoint, _get_headers(), method, body_str)

func _fetch_items():
	var user_id = Database.get_account().get("user_id", -1)
	status_label.text = "Đang tải vật phẩm..."
	api_request("/api/designer/%d/items" % user_id, HTTPClient.METHOD_GET, "", _on_items_fetched)

func _on_items_fetched(code, body):
	if code == 200:
		status_label.text = "Tải vật phẩm thành công!"
		var json = JSON.new()
		if json.parse(body.get_string_from_utf8()) == OK:
			all_items = json.data
			current_page = 0
			_update_list_ui()
	else:
		status_label.text = "Lỗi tải vật phẩm: " + str(code)

func _on_search_changed(t: String):
	current_page = 0
	_update_list_ui()

func _change_page(dir: int):
	current_page += dir
	_update_list_ui()

func _update_list_ui():
	for c in items_container.get_children():
		c.queue_free()
		
	var filtered = []
	var q = search_input.text.strip_edges().to_lower()
	if q.is_empty():
		filtered = all_items
	else:
		for itm in all_items:
			var n = itm.get("name", "").to_lower()
			var typ = itm.get("type", "").to_lower()
			if q in n or q in typ:
				filtered.append(itm)
				
	var total_pages = ceili(float(filtered.size()) / items_per_page)
	if total_pages == 0: total_pages = 1
	if current_page >= total_pages: current_page = total_pages - 1
	if current_page < 0: current_page = 0
	
	lbl_page.text = "%d / %d" % [current_page + 1, total_pages]
	btn_prev.disabled = (current_page == 0)
	btn_next.disabled = (current_page >= total_pages - 1)
	
	var start_idx = current_page * items_per_page
	var end_idx = min(start_idx + items_per_page, filtered.size())
	
	for i in range(start_idx, end_idx):
		var itm = filtered[i]
		var row = HBoxContainer.new()
		row.add_theme_constant_override("separation", 20)
		
		var lbl_name = Label.new()
		lbl_name.text = itm.get("name", "Unknown")
		lbl_name.custom_minimum_size = Vector2(250, 0)
		row.add_child(lbl_name)
		
		var lbl_type = Label.new()
		lbl_type.text = "Loại: " + itm.get("type", "")
		lbl_type.custom_minimum_size = Vector2(150, 0)
		row.add_child(lbl_type)
		
		var lbl_price = Label.new()
		lbl_price.text = "Giá: %d GC" % int(itm.get("price", 0))
		lbl_price.add_theme_color_override("font_color", Color(1.0, 0.85, 0.3))
		lbl_price.custom_minimum_size = Vector2(100, 0)
		row.add_child(lbl_price)
		
		var lbl_status = Label.new()
		var is_approved = itm.get("isApproved", false)
		if is_approved:
			lbl_status.text = "Đã duyệt (Đang bán)"
			lbl_status.add_theme_color_override("font_color", Color(0.4, 1.0, 0.4))
		else:
			lbl_status.text = "Đang chờ duyệt"
			lbl_status.add_theme_color_override("font_color", Color(1.0, 0.8, 0.4))
		row.add_child(lbl_status)
		
		items_container.add_child(row)

func _on_add_pressed():
	var n = name_input.text.strip_edges()
	var p_str = price_input.text.strip_edges()
	
	if n.is_empty() or p_str.is_empty():
		status_label.text = "Vui lòng nhập tên và giá."
		return
		
	if not p_str.is_valid_int():
		status_label.text = "Giá phải là một số nguyên dương hợp lệ."
		return
		
	var price_val = p_str.to_int()
	if price_val < 0:
		status_label.text = "Giá không được là số âm."
		return
		
	var user_id = Database.get_account().get("user_id", -1)
	var dict = {
		"name": n,
		"price": price_val,
		"type": type_opt.get_item_text(type_opt.selected)
	}
	
	status_label.text = "Đang đăng bán..."
	api_request("/api/designer/%d/items" % user_id, HTTPClient.METHOD_POST, JSON.stringify(dict), func(code, body):
		if code == 200:
			status_label.text = "Đăng thành công! Vui lòng đợi Moderator duyệt."
			name_input.text = ""
			price_input.text = ""
			_fetch_items()
		else:
			status_label.text = "Lỗi đăng vật phẩm: " + body.get_string_from_utf8()
	)
