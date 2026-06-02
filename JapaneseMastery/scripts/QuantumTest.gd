extends Control

const Curriculum = preload("res://scripts/Curriculum.gd")

var vocab_data = []
var question_pool = []
var current_q_idx = 0
var score = 0
var passing_score = 40 # 80% of 50

var state = "PLAYING" # PLAYING, RESULT, SUMMARY
var current_q = {}
var selected_option = -1
var question_start_time = 0
var weak_words = []
var times_taken = []

# UI Elements
var bg: ColorRect
var progress_label: Label
var word_label: Label
var prompt_label: Label
var options_container: GridContainer
var btn_submit: Button
var btn_back: Button
var feedback_label: Label

func _ready():
	var mode = 1
	if Global.get("quantum_mode") != null:
		mode = Global.quantum_mode
		
	setup_ui()
	
	word_label.text = "Đang tải dữ liệu Quantum..."
	word_label.show()
	prompt_label.hide()
	options_container.hide()
	btn_submit.hide()
	progress_label.hide()
	
	_load_quantum_vocab(mode)

func _load_quantum_vocab(mode: int):
	vocab_data = []
	var current_lvl = Global.get_current_level()
	
	if mode == 1:
		var level_data = Curriculum.get_level(current_lvl)
		for item in level_data:
			vocab_data.append({
				"kana": item.get("kana", item.get("wordJp", "")),
				"romaji": item.get("romaji", ""),
				"meaning": item.get("meaning", "")
			})
	else:
		for i in range(1, current_lvl + 1):
			var level_data = Curriculum.get_level(i)
			for item in level_data:
				vocab_data.append({
					"kana": item.get("kana", item.get("wordJp", "")),
					"romaji": item.get("romaji", ""),
					"meaning": item.get("meaning", "")
				})
				
	_setup_question_pool(mode)

func _setup_question_pool(mode: int):
	question_pool = []
	if mode == 1:
		for word in vocab_data:
			question_pool.append({"word": word, "type": "reading"})
			if word.get("meaning", "") != "":
				question_pool.append({"word": word, "type": "meaning"})
			else:
				question_pool.append({"word": word, "type": "romaji_to_kana"})
	else:
		vocab_data.shuffle()
		var selected_words = []
		for i in range(min(50, vocab_data.size())):
			selected_words.append(vocab_data[i])
			
		for word in selected_words:
			if word.get("meaning", "") != "":
				var type_choice = ["reading", "meaning"][randi() % 2]
				question_pool.append({"word": word, "type": type_choice})
			else:
				var type_choice = ["reading", "romaji_to_kana"][randi() % 2]
				question_pool.append({"word": word, "type": type_choice})
				
	question_pool.shuffle()
	passing_score = int(question_pool.size() * 0.8)
		
	progress_label.show()
	next_question()

func setup_ui():
	# Background
	bg = ColorRect.new()
	bg.color = Color(0.12, 0.12, 0.16)
	bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(bg)
	
	btn_back = Button.new()
	btn_back.text = "Hủy & Trở về Menu"
	btn_back.position = Vector2(20, 20)
	btn_back.custom_minimum_size = Vector2(200, 40)
	btn_back.pressed.connect(func(): Global.goto_scene("res://scenes/Main.tscn"))
	add_child(btn_back)
	
	progress_label = Label.new()
	progress_label.add_theme_font_size_override("font_size", 24)
	progress_label.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	progress_label.position = Vector2(-200, 20)
	add_child(progress_label)
	
	word_label = Label.new()
	word_label.add_theme_font_size_override("font_size", 120)
	word_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	word_label.set_anchors_preset(Control.PRESET_TOP_WIDE)
	word_label.position.y = 80
	add_child(word_label)
	
	prompt_label = Label.new()
	prompt_label.add_theme_font_size_override("font_size", 30)
	prompt_label.modulate = Color(0.7, 0.7, 0.7)
	prompt_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	prompt_label.set_anchors_preset(Control.PRESET_TOP_WIDE)
	prompt_label.position.y = 250
	add_child(prompt_label)
	
	options_container = GridContainer.new()
	options_container.columns = 2
	options_container.set_anchors_preset(Control.PRESET_CENTER)
	options_container.grow_horizontal = Control.GROW_DIRECTION_BOTH
	options_container.grow_vertical = Control.GROW_DIRECTION_BOTH
	options_container.add_theme_constant_override("h_separation", 30)
	options_container.add_theme_constant_override("v_separation", 30)
	options_container.position.y = 100
	add_child(options_container)
	
	btn_submit = Button.new()
	btn_submit.text = "Xác nhận"
	btn_submit.custom_minimum_size = Vector2(200, 60)
	btn_submit.add_theme_font_size_override("font_size", 24)
	btn_submit.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	btn_submit.grow_horizontal = Control.GROW_DIRECTION_BOTH
	btn_submit.position.y = -80
	btn_submit.pressed.connect(_on_submit_pressed)
	add_child(btn_submit)
	
	feedback_label = Label.new()
	feedback_label.add_theme_font_size_override("font_size", 30)
	feedback_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	feedback_label.set_anchors_preset(Control.PRESET_CENTER)
	feedback_label.grow_horizontal = Control.GROW_DIRECTION_BOTH
	feedback_label.custom_minimum_size = Vector2(900, 0)
	feedback_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	feedback_label.position.y = 50
	feedback_label.hide()
	add_child(feedback_label)

func next_question():
	if current_q_idx >= question_pool.size():
		show_summary()
		return
		
	state = "PLAYING"
	progress_label.text = "Câu %d / %d" % [current_q_idx + 1, question_pool.size()]
	question_start_time = Time.get_ticks_msec()
	
	var q = question_pool[current_q_idx]
	var vocab = q["word"]
	var q_type = q["type"]
	
	var correct_ans = ""
	var pool = []
	
	if q_type == "reading":
		correct_ans = vocab["romaji"]
		for v in vocab_data:
			if v["romaji"] != correct_ans and v["romaji"] != "": pool.append(v["romaji"])
		word_label.text = vocab["kana"]
		prompt_label.text = "Chọn cách đọc:"
	elif q_type == "meaning":
		correct_ans = vocab["meaning"]
		for v in vocab_data:
			if v.get("meaning", "") != correct_ans and v.get("meaning", "") != "": pool.append(v["meaning"])
		word_label.text = vocab["kana"]
		prompt_label.text = "Nghĩa của từ này là gì?"
	else:
		correct_ans = vocab["kana"]
		for v in vocab_data:
			if v["kana"] != correct_ans and v["kana"] != "": pool.append(v["kana"])
		word_label.text = vocab["romaji"]
		prompt_label.text = "Chọn mặt chữ:"
		
	pool.shuffle()
	var options = []
	for i in range(min(3, pool.size())):
		options.append(pool[i])
	options.append(correct_ans)
	options.shuffle()
	
	current_q = {
		"correct": correct_ans,
		"options": options,
		"word_jp": vocab["kana"]
	}
	
	for child in options_container.get_children():
		child.queue_free()
		
	selected_option = -1
	
	for i in range(options.size()):
		var btn = Button.new()
		btn.text = options[i]
		btn.custom_minimum_size = Vector2(400, 100)
		btn.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		btn.add_theme_font_size_override("font_size", 24)
		btn.pressed.connect(_on_option_pressed.bind(i))
		options_container.add_child(btn)
		
	word_label.show()
	prompt_label.show()
	options_container.show()
	
	btn_submit.text = "Xác nhận"
	btn_submit.disabled = true
	btn_submit.show()
	
	feedback_label.hide()

func _on_option_pressed(idx: int):
	if state != "PLAYING": return
	selected_option = idx
	btn_submit.disabled = false
	
	var i = 0
	for btn in options_container.get_children():
		if i == idx:
			btn.modulate = Color(0.5, 0.8, 1.0)
		else:
			btn.modulate = Color.WHITE
		i += 1

func _on_submit_pressed():
	if state == "PLAYING":
		var is_correct = (options_container.get_child(selected_option).text == current_q["correct"])
		var time_taken = Time.get_ticks_msec() - question_start_time
		
		var avg_time = 3000.0 # Default if no history
		if times_taken.size() > 0:
			var total = 0.0
			for t in times_taken: total += t
			avg_time = total / times_taken.size()
			
		# --- THUẬT TOÁN HÀM SÓNG LƯỢNG TỬ (QUANTUM WAVEFUNCTION COLLAPSE) ---
		var is_weak = false
		var status_str = ""
		
		if not is_correct:
			is_weak = true # Sai = Chắc chắn sụp đổ về trạng thái 0
			status_str = "sai"
		else:
			# Mô phỏng Gói Sóng Gauss (Gaussian Wave Packet) trong Cơ học lượng tử
			var p_know = 1.0
			if time_taken > avg_time:
				var delta_t = float(time_taken - avg_time)
				var sigma = 2000.0 # Độ phân tán lượng tử (Quantum Dispersion) = 2 giây
				p_know = exp(-pow(delta_t, 2) / (2.0 * pow(sigma, 2)))
				
				var collapse_val = randf() # Biến số lượng tử (0.0 -> 1.0)
				if collapse_val > p_know:
					is_weak = true # Sụp đổ về trạng thái "Quên" (Guessing)
					status_str = "đoán mò"
				else:
					is_weak = true # Do dự nhưng hàm sóng chưa sụp đổ hẳn
					status_str = "do dự"
					
		if is_weak:
			var vocab_word = current_q["word_jp"]
			var found = false
			for w in weak_words:
				if w["word"] == vocab_word:
					found = true
					var current_status = w.get("status", "")
					if status_str == "sai":
						w["status"] = "sai" # Sai ghi đè tất cả
					elif status_str == "đoán mò" and current_status == "do dự":
						w["status"] = "đoán mò" # Đoán mò ghi đè do dự
					break
					
			if not found:
				weak_words.append({"word": vocab_word, "status": status_str})
		
		if is_correct:
			times_taken.append(time_taken)
			score += 1
			feedback_label.text = "Chính xác!"
			feedback_label.modulate = Color(0.4, 1.0, 0.4)
			Global.add_coins(5)
		else:
			feedback_label.text = "Sai rồi! Đáp án: " + current_q["correct"]
			feedback_label.modulate = Color(1.0, 0.4, 0.4)
			
			# Cơ chế Duolingo: Sai thì bị đẩy xuống cuối hàng đợi để làm lại!
			question_pool.append(question_pool[current_q_idx])
			
		state = "RESULT"
		word_label.hide()
		prompt_label.hide()
		options_container.hide()
		
		btn_submit.text = "Tiếp theo"
		feedback_label.show()
		
	elif state == "RESULT":
		current_q_idx += 1
		next_question()
		
	elif state == "SUMMARY":
		Global.goto_scene("res://scenes/Main.tscn")

func show_summary():
	state = "SUMMARY"
	word_label.hide()
	prompt_label.hide()
	options_container.hide()
	progress_label.hide()
	btn_back.hide()
	
	feedback_label.show()
	feedback_label.position.y = -50
	
	var summary_text = ""
	if weak_words.size() == 0:
		summary_text = "HOÀN HẢO!\nBạn đã làm đúng 100% và không hề do dự!\nĐiểm: %d/%d\n(Thưởng thêm 500 G-Coins)" % [score, question_pool.size()]
		feedback_label.modulate = Color(0.8, 0.4, 1.0)
		Global.add_coins(500)
	else:
		summary_text = "PHÂN TÍCH QUANTUM\n"
		summary_text += "Điểm: %d/%d. Số từ sai hoặc do dự: %d\n" % [score, question_pool.size(), weak_words.size()]
		summary_text += "Bạn cần học lại: "
		
		var display_words = []
		for w in weak_words:
			var word_str = w["word"]
			# Sử dụng Word Joiner (U+2060) để ngăn Godot ngắt dòng giữa các ký tự của 1 từ tiếng Nhật
			var nobr = ""
			for i in range(word_str.length()):
				nobr += word_str[i]
				if i < word_str.length() - 1:
					nobr += String.chr(0x2060)
					
			if w.get("status", "") == "đoán mò":
				nobr += " (Đoán mò)"
			elif w.get("status", "") == "do dự":
				nobr += " (Do dự)"
			elif w.get("status", "") == "sai":
				nobr += " (Sai)"
				
			display_words.append(nobr)
			
		summary_text += ", ".join(display_words)
			
		feedback_label.modulate = Color(1.0, 0.7, 0.2)
		Global.add_coins(100) # Small reward
		
	feedback_label.text = summary_text
	btn_submit.text = "Về Menu"
