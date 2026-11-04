extends CanvasLayer

const FADE_SECONDS := 0.5
const SECOND_STILL_HOLD_SECONDS := 2.0

@export var first_still: Texture2D
@export var second_still: Texture2D
@export var voice_stream: AudioStream
@export_range(0.0, 1.0) var audio_volume := 1.0
@export var voice_gain_db := 0.0


func _ready() -> void:
	var first := _add_still(first_still)
	await _fade(first, 1.0)

	var voice := AudioStreamPlayer.new()
	voice.stream = voice_stream
	voice.bus = &"SFX" if AudioServer.get_bus_index(&"SFX") >= 0 else &"Master"
	voice.volume_linear = audio_volume * db_to_linear(voice_gain_db)
	add_child(voice)
	voice.play()
	await voice.finished

	if second_still == null:
		await _fade(first, 0.0)
	else:
		var second := _add_still(second_still)
		await _fade(second, 1.0)
		first.hide()
		await get_tree().create_timer(SECOND_STILL_HOLD_SECONDS, true, false, true).timeout
		await _fade(second, 0.0)

	queue_free()


func _add_still(texture: Texture2D) -> TextureRect:
	var still := TextureRect.new()
	still.texture = texture
	still.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	still.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_COVERED
	still.mouse_filter = Control.MOUSE_FILTER_IGNORE
	still.modulate.a = 0.0
	add_child(still)
	still.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	return still


func _fade(still: TextureRect, alpha: float) -> void:
	var tween := create_tween().set_ignore_time_scale()
	tween.tween_property(still, "modulate:a", alpha, FADE_SECONDS)
	await tween.finished
