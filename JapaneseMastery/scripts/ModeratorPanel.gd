extends Control

var status_label: Label
var list_container: VBoxContainer
var search_input: LineEdit
var lbl_page: Label
var btn_prev: Button
var btn_next: Button

var btn_tab_pending_tests: Button
var btn_tab_approved_tests: Button
var btn_tab_pending_items: Button

var current_mode = "pending_tests" # pending_tests, approved_tests, pending_items
var all_data = []
var current_page = 0
var items_per_page = 5

func _ready():
	for c in get_children():
		c.queue_free()
	_setup_ui()
	_fetch_data()

func _setup_ui():
	var bg = ColorRect.new()
	bg.color = Color(0.12, 0.12, 0.2)
	bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(bg)
	
	var title = Label.new()
	title.text = "BẢNG KIỂM DUYỆT (MODERATOR)"
	title.add_theme_font_size_override("font_size", 36)
	title.set_anchors_preset(Control.PRESET_TOP_WIDE)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.position.y = 20
	add_child(title)
	
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
	
	var tabs = HBoxContainer.new()
	tabs.alignment = BoxContainer.ALIGNMENT_CENTER
	tabs.set_anchors_preset(Control.PRESET_TOP_WIDE)
	tabs.position.y = 100
	tabs.add_theme_constant_override("separation", 20)
	add_child(tabs)
	
	btn_tab_pending_tests = Button.new()
	btn_tab_pending_tests.text = "Bài Test Chờ Duyệt"
	btn_tab_pending_tests.custom_minimum_size = Vector2(200, 40)
	btn_tab_pending_tests.pressed.connect(_on_tab_pressed.bind("pending_tests"))
	tabs.add_child(btn_tab_pending_tests)
	
	btn_tab_approved_tests = Button.new()
	btn_tab_approved_tests.text = "Bài Test Đã Duyệt"
	btn_tab_approved_tests.custom_minimum_size = Vector2(200, 40)
	btn_tab_approved_tests.pressed.connect(_on_tab_pressed.bind("approved_tests"))
	tabs.add_child(btn_tab_approved_tests)
	
	btn_tab_pending_items = Button.new()
	btn_tab_pending_items.text = "Vật Phẩm Shop Chờ Duyệt"
	btn_tab_pending_items.custom_minimum_size = Vector2(200, 40)
	btn_tab_pending_items.pressed.connect(_on_tab_pressed.bind("pending_items"))
	tabs.add_child(btn_tab_pending_items)
	
	var main_area = CenterContainer.new()
	main_area.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	main_area.offset_top = 160
	add_child(main_area)
	
	var vbox = VBoxContainer.new()
	vbox.custom_minimum_size = Vector2(900, 0)
	vbox.add_theme_constant_override("separation", 15)
	main_area.add_child(vbox)
	
	var filter_hbox = HBoxContainer.new()
	search_input = LineEdit.new()
	search_input.placeholder_text = "🔍 Tìm kiếm theo tên / ID / người tạo..."
	search_input.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	search_input.text_changed.connect(_on_search_changed)
	filter_hbox.add_child(search_input)
	vbox.add_child(filter_hbox)
	
	var scroll = ScrollContainer.new()
	scroll.custom_minimum_size = Vector2(0, 450)
	vbox.add_child(scroll)
	
	list_container = VBoxContainer.new()
	list_container.add_theme_constant_override("separation", 10)
	scroll.add_child(list_container)
	
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
	
	_update_tab_colors()

func _on_tab_pressed(mode: String):
	current_mode = mode
	_update_tab_colors()
	_fetch_data()

func _update_tab_colors():
	btn_tab_pending_tests.modulate = Color(0.4, 1.0, 0.4) if current_mode == "pending_tests" else Color(1,1,1)
	btn_tab_approved_tests.modulate = Color(0.4, 1.0, 0.4) if current_mode == "approved_tests" else Color(1,1,1)
	btn_tab_pending_items.modulate = Color(0.4, 1.0, 0.4) if current_mode == "pending_items" else Color(1,1,1)

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

func _fetch_data():
	var user_id = Database.get_account().get("user_id", -1)
	status_label.text = "Đang tải dữ liệu..."
	
	var endpoint = ""
	if current_mode == "pending_tests":
		endpoint = "/api/moderator/%d/pending-tests" % user_id
	elif current_mode == "approved_tests":
		endpoint = "/api/moderator/%d/approved-tests" % user_id
	elif current_mode == "pending_items":
		endpoint = "/api/moderator/%d/pending-items" % user_id
		
	api_request(endpoint, HTTPClient.METHOD_GET, "", _on_data_fetched)

func _on_data_fetched(code, body):
	if code == 200:
		status_label.text = "Tải dữ liệu thành công!"
		var json = JSON.new()
		if json.parse(body.get_string_from_utf8()) == OK:
			var arr: Array = json.data
			arr.sort_custom(_sort_by_id_desc)
			all_data = arr
			current_page = 0
			_update_list_ui()
		else:
			status_label.text = "Lỗi phân tích dữ liệu"
	else:
		status_label.text = "Lỗi tải dữ liệu: " + str(code)

func _sort_by_id_desc(a: Dictionary, b: Dictionary) -> bool:
	return int(a.get("id", 0)) > int(b.get("id", 0))

func _on_search_changed(t: String):
	current_page = 0
	_update_list_ui()

func _change_page(dir: int):
	current_page += dir
	_update_list_ui()

func _update_list_ui():
	for c in list_container.get_children():
		c.queue_free()
		
	var filtered = []
	var q = search_input.text.strip_edges().to_lower()
	if q.is_empty():
		filtered = all_data
	else:
		for d in all_data:
			var search_str = ""
			if current_mode == "pending_items":
				search_str = str(d.get("name", "")) + " " + str(d.get("type", "")) + " " + str(d.get("designer", {}).get("username", ""))
			else:
				search_str = str(d.get("testCode", "")) + " " + str(d.get("title", "")) + " " + str(d.get("authorName", ""))
			if q in search_str.to_lower():
				filtered.append(d)
				
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
		var d = filtered[i]
		if current_mode == "pending_items":
			_create_item_row(d)
		else:
			_create_test_row(d)

func _create_test_row(test_data):
	var panel = PanelContainer.new()
	var margin = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 10)
	margin.add_theme_constant_override("margin_right", 10)
	margin.add_theme_constant_override("margin_top", 10)
	margin.add_theme_constant_override("margin_bottom", 10)
	panel.add_child(margin)
	
	var hbox = HBoxContainer.new()
	margin.add_child(hbox)
	
	var lbl_info = Label.new()
	lbl_info.text = "Code: %s | %s | Tác giả: %s" % [test_data.get("testCode", ""), test_data.get("title", ""), test_data.get("authorName", "")]
	lbl_info.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.add_child(lbl_info)
	
	if current_mode == "pending_tests":
		var btn_approve = Button.new()
		btn_approve.text = "Duyệt (Mở khóa)"
		btn_approve.modulate = Color(0.4, 1.0, 0.4)
		btn_approve.pressed.connect(_process_test.bind(test_data["id"], true))
		hbox.add_child(btn_approve)
		
		var btn_reject = Button.new()
		btn_reject.text = "Từ chối (Xóa)"
		btn_reject.modulate = Color(1.0, 0.4, 0.4)
		btn_reject.pressed.connect(_process_test.bind(test_data["id"], false))
		hbox.add_child(btn_reject)
		
	list_container.add_child(panel)

func _create_item_row(item_data):
	var panel = PanelContainer.new()
	var margin = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 10)
	margin.add_theme_constant_override("margin_right", 10)
	margin.add_theme_constant_override("margin_top", 10)
	margin.add_theme_constant_override("margin_bottom", 10)
	panel.add_child(margin)
	
	var hbox = HBoxContainer.new()
	margin.add_child(hbox)
	
	var lbl_info = Label.new()
	lbl_info.text = "[%s] %s | Designer: %s | Giá: %d GC" % [item_data.get("type", ""), item_data.get("name", ""), item_data.get("designer", {}).get("username", ""), int(item_data.get("price", 0))]
	lbl_info.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.add_child(lbl_info)
	
	var btn_approve = Button.new()
	btn_approve.text = "Duyệt lên Shop"
	btn_approve.modulate = Color(0.4, 1.0, 0.4)
	btn_approve.pressed.connect(_process_item.bind(item_data["id"], true))
	hbox.add_child(btn_approve)
	
	var btn_reject = Button.new()
	btn_reject.text = "Từ chối (Xóa)"
	btn_reject.modulate = Color(1.0, 0.4, 0.4)
	btn_reject.pressed.connect(_process_item.bind(item_data["id"], false))
	hbox.add_child(btn_reject)
		
	list_container.add_child(panel)

func _process_test(test_id, approve):
	var user_id = Database.get_account().get("user_id", -1)
	var dict = {"approve": approve}
	status_label.text = "Đang xử lý bài test..."
	api_request("/api/moderator/%d/tests/%d/approve" % [user_id, test_id], HTTPClient.METHOD_PUT, JSON.stringify(dict), func(code, body):
		if code == 200:
			status_label.text = "Xử lý thành công!"
			_fetch_data()
		else:
			status_label.text = "Lỗi xử lý: " + body.get_string_from_utf8()
	)

func _process_item(item_id, approve):
	var user_id = Database.get_account().get("user_id", -1)
	var dict = {"approve": approve}
	status_label.text = "Đang xử lý vật phẩm..."
	api_request("/api/moderator/%d/items/%d/approve" % [user_id, item_id], HTTPClient.METHOD_PUT, JSON.stringify(dict), func(code, body):
		if code == 200:
			status_label.text = "Xử lý thành công!"
			_fetch_data()
		else:
			status_label.text = "Lỗi xử lý: " + body.get_string_from_utf8()
	)
