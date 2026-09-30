extends CanvasLayer
## Animation channels, sprite geometry and audio cues are converted from the original
## ObjectionCutIn_Ema prefab and its 60 Hz Timeline. Only viewport calibration and
## Unity-to-Godot shader/particle rendering are handled here.
const ROOT := "res://Manosaba/scenes/vfx/refute_cutin/"
const ART := "res://Manosaba/images/vfx/refute_cutin/"
const AUDIO := "res://Manosaba/audio/vfx/refute_cutin/"
const TIMELINE = preload("res://Manosaba/scenes/vfx/refute_cutin/original_timeline.json")
const HIRO_TIMELINE = preload("res://Manosaba/scenes/vfx/refute_cutin/hiro_timeline.json")
const CREATURE_HIRO_TIMELINE = preload("res://Manosaba/scenes/vfx/refute_cutin/creature_hiro_timeline.json")
const GLASS_SHADER = preload("res://Manosaba/scenes/vfx/refute_cutin/glass.gdshader")
const BACKGROUND_SHADER = preload("res://Manosaba/scenes/vfx/refute_cutin/background.gdshader")
const SILHOUETTE_SHADER = preload("res://Manosaba/scenes/vfx/refute_cutin/silhouette.gdshader")
# The supplied recording runs approximately 10% slower than the native Timeline.
const PLAYBACK_RATE := 0.9
const DURATION := (200.0 / 60.0) / PLAYBACK_RATE
@export var audio_volume := 1.0
@export var voice_path := ""
@export_range(0, 2) var expression := 2
@export_enum("Ema", "Hiro", "CreatureHiro") var character := 0
var elapsed := 0.0
var phase := 0
var stage: Node2D
var glass: Sprite2D
var ema: Sprite2D
var portrait: Sprite2D
var fragments: Array[Sprite2D] = []
var _sprites: Array[Sprite2D] = []
var _audio_players: Array[AudioStreamPlayer] = []
var _definitions: Array
var _cues: Array
var _played: Array[bool] = []
var _silhouettes: Array[ShaderMaterial] = []
var _data: Dictionary
var _particles: Array[Dictionary] = []
var _extra_particles: Array[Dictionary] = []

func _ready() -> void:
	_data = [TIMELINE, HIRO_TIMELINE, CREATURE_HIRO_TIMELINE][character].data
	_definitions = _data.sprites
	_cues = _data.audio
	stage = Node2D.new()
	add_child(stage)
	for definition: Dictionary in _definitions:
		var sprite := Sprite2D.new()
		sprite.texture = load(ART + str(definition.texture))
		sprite.name = str(definition.name)
		sprite.z_index = int(definition.sort)
		stage.add_child(sprite)
		_sprites.append(sprite)
		var mat_name := str(definition.material)
		if mat_name.begins_with("Glasses_Fix_") or mat_name.begins_with("Iuminescence_dezolve_"):
			var material := ShaderMaterial.new()
			material.shader = GLASS_SHADER
			# Unity material property names intentionally point to the opposite filename suffix.
			material.set_shader_parameter("kirakira", load(ART + str(_data.theme) + "_cracks_rgb.png"))
			material.set_shader_parameter("kirakira2", load(ART + str(_data.theme) + "_glitter_rgb.png"))
			material.set_shader_parameter("silhouette_only", mat_name.begins_with("Iuminescence_dezolve_"))
			material.set_shader_parameter("glow_tint", _color(definition, "_Color", Color(1,0.8705882,1)))
			material.set_shader_parameter("edge_tint", _color(definition, "_EclipseFlame", Color(1,0,0.9404259)))
			sprite.material = material
		elif mat_name.begins_with("Background_"):
			var material := ShaderMaterial.new()
			material.shader = BACKGROUND_SHADER
			material.set_shader_parameter("background_a", _color(definition, "_BackgroundA", Color(1,0,0.6876597)))
			material.set_shader_parameter("background_b", _color(definition, "_BackgroundB", Color.BLACK))
			sprite.material = material
		elif mat_name.begins_with("Iuminescence_Silhouette_") or mat_name == "Shadow_Fix":
			var material := ShaderMaterial.new()
			material.shader = SILHOUETTE_SHADER
			material.set_shader_parameter("tint", _color(definition, "_Color", Color(1,0.8705882,1)))
			if mat_name == "Shadow_Fix":
				material.set_shader_parameter("tint", Color(0.45098, 0.490196, 0.6))
				material.set_shader_parameter("use_red_as_alpha", false)
				material.set_shader_parameter("inside_glass", true)
				var mask_name := "RefuteCutIn_StainedGlass_001.png" if character == 0 else "Hiro_CutIn_StainedGlass_001.png"
				material.set_shader_parameter("glass_mask", load(ART + mask_name))
				_silhouettes.append(material)
			sprite.material = material
		if str(definition.name).begins_with("GlassFragment"):
			fragments.append(sprite)
		if str(definition.name) == "RefuteCutIn_StainedGlass_001":
			glass = sprite
		if int(definition.expression) == expression:
			portrait = sprite
			ema = sprite # Compatibility with the original Ema preview harness.
	for cue: Dictionary in _cues:
		var player := AudioStreamPlayer.new()
		player.stream = load(AUDIO + str(cue.file))
		player.bus = &"SFX" if AudioServer.get_bus_index(&"SFX") >= 0 else &"Master"
		player.volume_linear = audio_volume
		add_child(player)
		_audio_players.append(player)
		_played.append(false)
	_create_particles()
	_create_extra_particles()
	# Start the card-specific voice in the same ready callback as the first visual frame.
	if audio_volume > 0.0 and not voice_path.is_empty():
		var voice := AudioStreamPlayer.new()
		voice.stream = load(voice_path)
		voice.bus = &"SFX" if AudioServer.get_bus_index(&"SFX") >= 0 else &"Master"
		voice.volume_linear = audio_volume
		# Keep longer lines alive after the overlay finishes; room exit still stops them.
		get_parent().add_child(voice)
		voice.finished.connect(voice.queue_free)
		voice.play()
	seek(0.0)

func _color(definition: Dictionary, key: String, fallback: Color) -> Color:
	var colors: Dictionary = definition.get("colors", {})
	if not colors.has(key):
		return fallback
	var value: Dictionary = colors[key]
	return Color(float(value.r), float(value.g), float(value.b), 1.0)

func _create_particles() -> void:
	var config: Dictionary = _data.particles
	var rng := RandomNumberGenerator.new()
	rng.seed = int(config.seed)
	var texture: Texture2D = load(ART + str(config.texture))
	for index in range(int(ceil(float(config.duration)*float(config.rate)))):
		var sprite := Sprite2D.new()
		sprite.texture = texture
		sprite.z_index = 1100
		stage.add_child(sprite)
		_particles.append({"sprite": sprite,
			"birth": float(config.start)+float(index)/float(config.rate),
			"life": rng.randf_range(float(config.lifetime[0]), float(config.lifetime[1])),
			"speed": rng.randf_range(float(config.speed[0]), float(config.speed[1])),
			"size": rng.randf_range(float(config.size[0]), float(config.size[1])),
			"direction": Vector2.from_angle(rng.randf()*TAU),
			"radius": sqrt(rng.randf())*float(config.radius)})

func _seek_particles(native_time: float) -> void:
	var keys: Array = _data.particles.alpha
	for particle in _particles:
		var sprite := particle.sprite as Sprite2D
		var age := native_time-float(particle.birth)
		sprite.visible = age >= 0.0 and age < float(particle.life)
		if not sprite.visible:
			continue
		var normalized := age/float(particle.life)
		var alpha := float(keys.back()[1])
		for i in range(keys.size()-1):
			if normalized <= float(keys[i+1][0]):
				var weight := inverse_lerp(float(keys[i][0]), float(keys[i+1][0]), normalized)
				alpha = lerpf(float(keys[i][1]), float(keys[i+1][1]), clampf(weight,0.0,1.0))
				break
		var direction: Vector2 = particle.direction
		sprite.position = Vector2(960,540)+direction*(float(particle.radius)+age*float(particle.speed))*75.0
		sprite.scale = Vector2.ONE*float(particle.size)*75.0/float(sprite.texture.get_width())
		sprite.modulate.a = alpha

func _create_extra_particles() -> void:
	for config: Dictionary in _data.get("extra_particles", []):
		var rng := RandomNumberGenerator.new()
		rng.seed = int(config.seed)
		var textures: Array[Texture2D] = []
		for filename in config.textures:
			textures.append(load(ART+str(filename)))
		var births: Array[float] = []
		if float(config.rate) > 0.0:
			for i in range(int(ceil((float(config.end)-float(config.start))*float(config.rate)))):
				births.append(float(config.start)+float(i)/float(config.rate))
		for burst in config.bursts:
			for i in range(int(burst[1])):
				births.append(float(config.start)+float(burst[0]))
		for birth in births:
			var sprite := Sprite2D.new()
			sprite.texture = textures[0]
			sprite.z_index = int(config.sort)
			if bool(config.additive):
				var material := CanvasItemMaterial.new()
				material.blend_mode = CanvasItemMaterial.BLEND_MODE_ADD
				sprite.material = material
			stage.add_child(sprite)
			var spawn := Vector2(rng.randf()-0.5, rng.randf()-0.5)
			if int(config.shape) == 5:
				spawn *= Vector2(float(config.shape_scale[0]), float(config.shape_scale[1]))
			else:
				spawn = Vector2.from_angle(rng.randf()*TAU)*sqrt(rng.randf())*float(config.radius)
			_extra_particles.append({"sprite": sprite, "config": config, "textures": textures,
				"birth": birth, "spawn": spawn,
				"life": rng.randf_range(float(config.lifetime[0]), float(config.lifetime[1])),
				"speed": rng.randf_range(float(config.speed[0]), float(config.speed[1])),
				"size": rng.randf_range(float(config.size[0]), float(config.size[1]))})

func _gradient(keys: Array, time: float, channel: int) -> float:
	if keys.is_empty():
		return 1.0
	for i in range(keys.size()-1):
		if time <= float(keys[i+1][0]):
			var weight := clampf(inverse_lerp(float(keys[i][0]), float(keys[i+1][0]), time),0.0,1.0)
			return lerpf(float(keys[i][channel]),float(keys[i+1][channel]),weight)
	return float(keys.back()[channel])

func _curve(values: Array, time: float) -> float:
	var sample := clampf(time,0.0,1.0)*float(values.size()-1)
	var first := mini(int(sample),values.size()-1)
	return lerpf(float(values[first]),float(values[mini(first+1,values.size()-1)]),sample-float(first))

func _seek_extra_particles(native_time: float) -> void:
	for particle in _extra_particles:
		var config: Dictionary = particle.config
		var sprite := particle.sprite as Sprite2D
		var age := native_time-float(particle.birth)
		sprite.visible = age >= 0.0 and age < float(particle.life) and native_time < float(config.end)
		if not sprite.visible:
			continue
		var life := age/float(particle.life)
		var frame := mini(int(native_time*60),200)
		var emitter: Array = config.frames[frame]
		var scale := Vector2(float(emitter[2]),float(emitter[3]))
		var direction := Vector2(float(config.direction[0]),float(config.direction[1]))
		var pos := Vector2(float(emitter[0]),float(emitter[1]))+(Vector2(particle.spawn)+direction*age*float(particle.speed))*scale
		sprite.position = Vector2(960+pos.x*75,540-pos.y*75)
		var texture_index: int = int(_curve(config.uv_curve,life)*float(config.cycles)*particle.textures.size()) % particle.textures.size()
		sprite.texture = particle.textures[texture_index]
		# Unity billboard size is in world units, independent of the atlas pixel dimensions.
		var size := float(particle.size)*_curve(config.sizes,life)*75
		sprite.scale = scale*size/float(sprite.texture.get_width())
		sprite.modulate = Color(_gradient(config.colors,life,1)*float(config.initial_color[0]),
			_gradient(config.colors,life,2)*float(config.initial_color[1]),
			_gradient(config.colors,life,3)*float(config.initial_color[2]),
			_gradient(config.alpha,life,1)*float(config.initial_color[3]))

func _process(delta: float) -> void:
	elapsed += delta
	var native_time := elapsed * PLAYBACK_RATE
	for i in range(_cues.size()):
		var cue: Dictionary = _cues[i]
		if not _played[i] and native_time >= float(cue.start):
			_played[i] = true
			if audio_volume > 0.0:
				_audio_players[i].play()
		if native_time >= float(cue.start) + float(cue.duration):
			_audio_players[i].stop()
	seek(elapsed)
	if elapsed >= DURATION:
		queue_free()

## Absolute-time evaluation is shared by gameplay, still-frame QA and preview export.
func seek(time: float) -> void:
	var viewport := get_viewport().get_visible_rect().size
	var fit := minf(viewport.x / 1920.0, viewport.y / 1080.0)
	stage.scale = Vector2.ONE * fit
	stage.position = (viewport - Vector2(1920, 1080)*fit)*0.5
	var native_time := clampf(time * PLAYBACK_RATE, 0.0, float(_data.native_duration))
	var frame := native_time * float(_data.sample_rate)
	var first := mini(int(frame), 200)
	var next := mini(first+1, 200)
	var weight := frame - float(first)
	for i in range(_sprites.size()):
		var sprite := _sprites[i]
		var definition: Dictionary = _definitions[i]
		var a: Array = definition.frames[first]
		var b: Array = definition.frames[next]
		var x := lerpf(float(a[0]), float(b[0]), weight)
		var y := lerpf(float(a[1]), float(b[1]), weight)
		var sx := lerpf(float(a[2]), float(b[2]), weight)
		var sy := lerpf(float(a[3]), float(b[3]), weight)
		var alpha := lerpf(float(a[5]), float(b[5]), weight)
		var units := float(_data.pixels_per_unit)
		sprite.position = Vector2(960+x*units, 540-y*units)
		sprite.scale = Vector2(sx, sy) * units / float(definition.ppu)
		sprite.rotation = -deg_to_rad(lerpf(float(a[4]), float(b[4]), weight))
		sprite.visible = float(a[6]) > 0.5
		var sprite_name := str(definition.name)
		if int(definition.expression) >= 0:
			sprite.visible = sprite.visible and sprite == portrait
		sprite.modulate.a = clampf(alpha, 0.0, 1.0)
		if sprite.material is ShaderMaterial:
			var material := sprite.material as ShaderMaterial
			# Original custom shaders use texture masks, rather than renderer vertex alpha.
			sprite.modulate.a = 1.0
			material.set_shader_parameter("animation_time", native_time)
			material.set_shader_parameter("tick", lerpf(float(a[7]), float(b[7]), weight))
			material.set_shader_parameter("fader", lerpf(float(a[8]), float(b[8]), weight))
			material.set_shader_parameter("fader2", lerpf(float(a[9]), float(b[9]), weight))
			material.set_shader_parameter("threshold", lerpf(float(a[10]), float(b[10]), weight))
		if sprite_name.begins_with("RefuteCutIn_StainedGlass_00") and sprite.visible:
			glass = sprite
			phase = int(sprite_name.right(3))-1
	if is_instance_valid(glass):
		for material in _silhouettes:
			material.set_shader_parameter("mask_center", glass.global_position)
			material.set_shader_parameter("mask_size", glass.texture.get_size()*glass.global_scale)
	_seek_particles(native_time)
	_seek_extra_particles(native_time)
