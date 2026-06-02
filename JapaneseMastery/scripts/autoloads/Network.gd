extends Node

const BASE_URL = "http://localhost:8080/api"
var jwt_token = ""
var is_logged_in = false

signal login_success(user_data)
signal login_failed(error_msg)
signal register_success(user_data)
signal register_failed(error_msg)

var _http_request: HTTPRequest

func _ready():
	_http_request = HTTPRequest.new()
	add_child(_http_request)
	_http_request.request_completed.connect(_on_request_completed)
	
	get_tree().set_auto_accept_quit(false)
	
	var timer = Timer.new()
	timer.wait_time = 2.0
	timer.autostart = true
	timer.timeout.connect(_check_heartbeat)
	add_child(timer)

func _check_heartbeat():
	if is_logged_in and jwt_token != "":
		print("[Heartbeat] Checking token valid...")
		var user_id = Database.get_account().get("user_id", -1)
		if user_id != -1:
			var req = HTTPRequest.new()
			add_child(req)
			req.request_completed.connect(func(res, code, headers, body):
				if code == 401:
					print("[Heartbeat] Bị đá văng do đăng nhập ở nơi khác!")
					logout(true)
					Global.goto_scene("res://scenes/Login.tscn")
				req.queue_free()
			)
			var headers = PackedStringArray(["Authorization: Bearer " + jwt_token])
			req.request(BASE_URL + "/player/%d/heartbeat" % user_id, headers, HTTPClient.METHOD_GET)

func register(username, password):
	var body = JSON.stringify({"username": username, "password": password})
	var headers = PackedStringArray(["Content-Type: application/json"])
	_http_request.request(BASE_URL + "/auth/register", headers, HTTPClient.METHOD_POST, body)
	_http_request.set_meta("action", "register")

func login(username, password):
	var body = JSON.stringify({"username": username, "password": password})
	var headers = PackedStringArray(["Content-Type: application/json"])
	_http_request.request(BASE_URL + "/auth/login", headers, HTTPClient.METHOD_POST, body)
	_http_request.set_meta("action", "login")

func logout(local_only: bool = false):
	# Gọi API clear token
	var user_id = Database.get_account().get("user_id", -1)
	if not local_only and user_id != -1 and jwt_token != "":
		var req = HTTPRequest.new()
		add_child(req)
		req.request_completed.connect(func(res, code, headers, body): req.queue_free())
		var headers = PackedStringArray(["Authorization: Bearer " + jwt_token, "Content-Type: application/json"])
		req.request(BASE_URL + ("/auth/logout/%d" % user_id), headers, HTTPClient.METHOD_POST)

	jwt_token = ""
	is_logged_in = false
	Database.update_account("user_id", -1)
	Database.update_account("display_name", "New Player")
	Database.update_account("current_level", 1)
	Database.update_account("g_coin_balance", 0)
	Database.update_account("role", "PLAYER")

func _on_request_completed(result, response_code, headers, body):
	var action = _http_request.get_meta("action")
	var response_str = body.get_string_from_utf8()
	
	if response_code == 200:
		var json = JSON.new()
		var parse_result = json.parse(response_str)
		if parse_result == OK:
			var data = json.data
			jwt_token = data.get("token", "")
			is_logged_in = true
			
			# Cập nhật thông tin vào Database local tạm thời
			Database.update_account("user_id", int(data.get("id", -1)))
			Database.update_account("display_name", data.get("username", "Player"))
			Database.update_account("current_level", data.get("level", 1))
			Database.update_account("role", data.get("role", "PLAYER"))
			Database.update_account("g_coin_balance", data.get("gCoin", 0))
			
			fetch_and_sync_vocabularies()
			
			if action == "login":
				emit_signal("login_success", data)
			else:
				emit_signal("register_success", data)
		else:
			_handle_error(action, "Lỗi phân tích JSON từ server")
	else:
		var err_msg = "Thất bại! Mã lỗi: " + str(response_code)
		var json = JSON.new()
		if json.parse(response_str) == OK and json.data is Dictionary:
			if json.data.has("message"):
				err_msg = json.data["message"]
		_handle_error(action, err_msg)

func _handle_error(action, msg):
	if action == "login":
		emit_signal("login_failed", msg)
	else:
		emit_signal("register_failed", msg)

func fetch_and_sync_vocabularies():
	var req = HTTPRequest.new()
	add_child(req)
	req.request_completed.connect(func(res, code, headers, body):
		if code == 200:
			var json = JSON.new()
			if json.parse(body.get_string_from_utf8()) == OK:
				var arr = json.data
				for v in arr:
					var lvl = int(v.get("levelRequired", 1))
					if not Curriculum.LEVELS.has(lvl):
						Curriculum.LEVELS[lvl] = []
					
					var exists = false
					for item in Curriculum.LEVELS[lvl]:
						if item["kana"] == v["wordJp"]:
							item["romaji"] = v["romaji"] # Cập nhật nếu trùng
							exists = true
							break
					
					if not exists:
						Curriculum.LEVELS[lvl].append({"kana": v["wordJp"], "romaji": v["romaji"]})
		req.queue_free()
	)
	var headers = PackedStringArray(["Content-Type: application/json"])
	if jwt_token != "":
		headers.append("Authorization: Bearer " + jwt_token)
	req.request(BASE_URL + "/admin/vocabularies", headers, HTTPClient.METHOD_GET)

func _notification(what):
	if what == NOTIFICATION_WM_CLOSE_REQUEST:
		if is_logged_in and jwt_token != "":
			var user_id = Database.get_account().get("user_id", -1)
			if user_id != -1:
				var req = HTTPRequest.new()
				add_child(req)
				var headers = PackedStringArray(["Authorization: Bearer " + jwt_token, "Content-Type: application/json"])
				req.request(BASE_URL + ("/auth/logout/%d" % user_id), headers, HTTPClient.METHOD_POST)
				
				var t = Timer.new()
				t.wait_time = 0.5
				t.one_shot = true
				add_child(t)
				t.start()
				await t.timeout
		get_tree().quit()
