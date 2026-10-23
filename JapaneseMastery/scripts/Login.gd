extends Control

var username_input: LineEdit
var password_input: LineEdit
var btn_login: Button
var btn_register: Button
var status_label: Label

func _ready():
	_setup_ui()
	Network.login_success.connect(_on_login_success)
	Network.login_failed.connect(_on_error)
	Network.register_success.connect(_on_register_success)
	Network.register_failed.connect(_on_error)

func _setup_ui():
	# Background
	var bg = ColorRect.new()
	bg.color = Color(0.1, 0.1, 0.15)
	bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(bg)
	
	# Title
	var title = Label.new()
	title.text = "JAPANESE MASTERY LOGIN"
	title.add_theme_font_size_override("font_size", 48)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.set_anchors_preset(Control.PRESET_TOP_WIDE)
	title.position.y = 80
	add_child(title)
	
	# VBox
	var vbox = VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_CENTER)
	vbox.custom_minimum_size = Vector2(400, 0)
	vbox.add_theme_constant_override("separation", 20)
	add_child(vbox)
	
	username_input = LineEdit.new()
	username_input.placeholder_text = "Username"
	username_input.custom_minimum_size = Vector2(400, 50)
	vbox.add_child(username_input)
	
	password_input = LineEdit.new()
	password_input.placeholder_text = "Password"
	password_input.secret = true
	password_input.custom_minimum_size = Vector2(400, 50)
	vbox.add_child(password_input)
	
	var hbox = HBoxContainer.new()
	hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	hbox.add_theme_constant_override("separation", 20)
	vbox.add_child(hbox)
	
	btn_login = Button.new()
	btn_login.text = "Login"
	btn_login.custom_minimum_size = Vector2(190, 50)
	btn_login.pressed.connect(_on_login_pressed)
	hbox.add_child(btn_login)
	
	btn_register = Button.new()
	btn_register.text = "Register"
	btn_register.custom_minimum_size = Vector2(190, 50)
	btn_register.pressed.connect(_on_register_pressed)
	hbox.add_child(btn_register)
	
	status_label = Label.new()
	status_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	status_label.modulate = Color(1.0, 0.4, 0.4)
	vbox.add_child(status_label)

func _on_login_pressed():
	if username_input.text.is_empty() or password_input.text.is_empty():
		status_label.text = "Please fill in all fields"
		return
	status_label.modulate = Color(0.8, 0.8, 0.8)
	status_label.text = "Connecting..."
	Network.login(username_input.text, password_input.text)

func _on_register_pressed():
	if username_input.text.is_empty() or password_input.text.is_empty():
		status_label.text = "Please fill in all fields"
		return
	status_label.modulate = Color(0.8, 0.8, 0.8)
	status_label.text = "Connecting..."
	Network.register(username_input.text, password_input.text)

func _on_login_success(data):
	status_label.modulate = Color(0.4, 1.0, 0.4)
	status_label.text = "Login successful!"
	await get_tree().create_timer(0.5).timeout
	Global.goto_scene("res://scenes/Main.tscn")

func _on_register_success(data):
	status_label.modulate = Color(0.4, 1.0, 0.4)
	status_label.text = "Registration successful!"
	await get_tree().create_timer(0.5).timeout
	Global.goto_scene("res://scenes/Main.tscn")

func _on_error(msg):
	status_label.modulate = Color(1.0, 0.4, 0.4)
	status_label.text = msg
