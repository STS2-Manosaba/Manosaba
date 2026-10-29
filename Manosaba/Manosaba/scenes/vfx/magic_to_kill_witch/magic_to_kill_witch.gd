extends CanvasLayer
## Original Still 20_1, Adv09 camera pans and double-exposure reveal.
const DATA = preload("res://Manosaba/scenes/vfx/magic_to_kill_witch/original_scene.json")
const IMAGE = preload("res://Manosaba/images/vfx/magic_to_kill_witch/Still_020_001.png")
const BLUR = preload("res://Manosaba/scenes/vfx/magic_to_kill_witch/reveal_blur.gdshader")
var elapsed := 0.0
var visuals: Node2D
var stage: Node2D
var main: Sprite2D
var middle: Sprite2D
var white: ColorRect
var backdrop: ColorRect
var blur: ShaderMaterial

func _ready() -> void:
	visuals = Node2D.new()
	add_child(visuals)
	backdrop = ColorRect.new()
	backdrop.color = Color.BLACK
	backdrop.mouse_filter = Control.MOUSE_FILTER_IGNORE
	visuals.add_child(backdrop)
	stage = Node2D.new()
	visuals.add_child(stage)
	main = Sprite2D.new()
	main.texture = IMAGE
	blur = ShaderMaterial.new()
	blur.shader = BLUR
	main.material = blur
	stage.add_child(main)
	middle = Sprite2D.new()
	middle.texture = IMAGE
	stage.add_child(middle)
	white = ColorRect.new()
	white.mouse_filter = Control.MOUSE_FILTER_IGNORE
	visuals.add_child(white)
	seek(0.0)

func _process(delta: float) -> void:
	elapsed += delta
	if elapsed >= float(DATA.data.duration):
		queue_free()
		return
	seek(elapsed)

func _ease_out(time: float, duration: float) -> float:
	var progress := clampf(time / duration, 0.0, 1.0)
	return 1.0 - (1.0 - progress) * (1.0 - progress)

func _camera(sprite: Sprite2D, x: float, y: float, zoom: float) -> void:
	sprite.position = Vector2((x / 100.0 - 0.5) * 1920.0, (0.5 - y / 100.0) * 1080.0)
	sprite.scale = Vector2.ONE * (1920.0 / IMAGE.get_width()) * zoom

func seek(time: float) -> void:
	var data: Dictionary = DATA.data
	var viewport := get_viewport().get_visible_rect().size
	backdrop.size = viewport
	white.size = viewport
	stage.position = viewport * 0.5
	stage.scale = Vector2.ONE * maxf(viewport.x / 1920.0, viewport.y / 1080.0)
	var cuts: Array = data.cuts
	var first: Dictionary = data.first
	var second: Dictionary = data.second
	var reveal: Dictionary = data.reveal
	main.visible = true
	middle.visible = false
	middle.modulate.a = 1.0
	blur.set_shader_parameter("blur_pixels", 0.0)
	if time < float(cuts[1]):
		var y := lerpf(float(first.position[1]), float(first.target_y), _ease_out(time - float(cuts[0]), float(first.duration)))
		_camera(main, float(first.position[0]), y, float(first.scale))
	elif time < float(cuts[2]):
		var y := lerpf(float(second.position[1]), float(second.target_y), _ease_out(time - float(cuts[1]), float(second.duration)))
		_camera(main, float(second.position[0]), y, float(second.scale))
	else:
		var reveal_time := time - float(cuts[2])
		var zoom := lerpf(float(reveal.scale), float(reveal.target_scale), _ease_out(reveal_time, float(reveal.duration)))
		_camera(main, float(reveal.position[0]), float(reveal.position[1]), zoom)
		blur.set_shader_parameter("blur_pixels", 6.0 * (1.0 - clampf(reveal_time / float(reveal.blur_duration), 0.0, 1.0)))
		if reveal_time < float(reveal.crossfade):
			middle.visible = true
			_camera(middle, float(second.position[0]), float(second.target_y), float(second.scale))
			middle.modulate.a = 1.0 - clampf(reveal_time / float(reveal.crossfade), 0.0, 1.0)
	white.color = Color(1.0, 1.0, 1.0, 1.0 - clampf((time - float(cuts[0])) / float(data.white_fade), 0.0, 1.0))
	visuals.modulate.a = clampf((float(data.duration) - time) / float(data.exit_fade), 0.0, 1.0)
