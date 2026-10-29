extends CanvasLayer
## Native BeginDebateText UI curves sampled at 60 Hz. No captured game background.
const ANIMATION = preload("res://Manosaba/scenes/vfx/interrogation_start/original_animation.json")
const ART := "res://Manosaba/images/vfx/interrogation_start/"
# The supplied recording begins one second into the native Timeline.
const NATIVE_START := 1.0
const DURATION := 10.0 / 3.0
var elapsed := 0.0
var stage: Node2D
var _nodes: Array[Node2D] = []
var _sprites: Array[Sprite2D] = []
var _sizes: Array[Vector2] = []

func _ready() -> void:
	stage = Node2D.new()
	add_child(stage)
	for definition: Dictionary in ANIMATION.data.nodes:
		var node := Node2D.new()
		node.name = str(definition.name)
		var parent := int(definition.parent)
		(stage if parent < 0 else _nodes[parent]).add_child(node)
		_nodes.append(node)
		var size := Vector2(float(definition.size[0]), float(definition.size[1]))
		if parent >= 0:
			size += _sizes[parent] * (Vector2(float(definition.anchor_max[0]), float(definition.anchor_max[1])) - Vector2(float(definition.anchor_min[0]), float(definition.anchor_min[1])))
		_sizes.append(size)
		var sprite: Sprite2D = null
		if not str(definition.texture).is_empty():
			sprite = Sprite2D.new()
			sprite.texture = load(ART + str(definition.texture))
			sprite.scale = size / sprite.texture.get_size()
			sprite.position = size * Vector2(0.5 - float(definition.pivot[0]), float(definition.pivot[1]) - 0.5)
			if str(definition.material) == "Emission":
				var material := CanvasItemMaterial.new()
				material.blend_mode = CanvasItemMaterial.BLEND_MODE_ADD
				sprite.material = material
			node.add_child(sprite)
		_sprites.append(sprite)
	seek(0.0)

func _process(delta: float) -> void:
	elapsed += delta
	if elapsed >= DURATION:
		queue_free()
		return
	seek(elapsed)

func seek(time: float) -> void:
	var viewport := get_viewport().get_visible_rect().size
	stage.position = viewport * 0.5
	stage.scale = Vector2.ONE * minf(viewport.x / 2560.0, viewport.y / 1440.0)
	var sample := clampf((time + NATIVE_START) * 60.0, 0.0, 260.0)
	var frame := int(sample)
	var weight := sample - float(frame)
	for index in range(_nodes.size()):
		var definition: Dictionary = ANIMATION.data.nodes[index]
		var frames: Array = definition.frames
		var a: Array = frames[frame]
		var b: Array = frames[mini(frame + 1, 260)]
		var values: Array[float] = []
		for channel in range(10):
			values.append(lerpf(float(a[channel]), float(b[channel]), weight))
		var node := _nodes[index]
		var offset := Vector2.ZERO
		var parent := int(definition.parent)
		if parent >= 0:
			var anchor := Vector2(lerpf(float(definition.anchor_min[0]), float(definition.anchor_max[0]), float(definition.pivot[0])), lerpf(float(definition.anchor_min[1]), float(definition.anchor_max[1]), float(definition.pivot[1])))
			var parent_definition: Dictionary = ANIMATION.data.nodes[parent]
			offset = _sizes[parent] * (anchor - Vector2(float(parent_definition.pivot[0]), float(parent_definition.pivot[1])))
		node.position = Vector2(values[0] + offset.x, -values[1] - offset.y)
		node.scale = Vector2(values[2], values[3])
		node.rotation = -deg_to_rad(values[4])
		node.visible = float(a[10]) > 0.5
		node.modulate.a = clampf(values[9], 0.0, 1.0)
		if _sprites[index] != null:
			_sprites[index].modulate = Color(values[5], values[6], values[7], clampf(values[8], 0.0, 1.0))
	# Unity RotationConstraint follows the animated sibling LibraRoot. Resolve after
	# every transform has been sampled because the source follows its target in the rig.
	for index in range(_nodes.size()):
		var definition: Dictionary = ANIMATION.data.nodes[index]
		if bool(definition.constrained):
			var source := int(definition.rotation_source)
			_nodes[index].global_rotation = _nodes[source].global_rotation - deg_to_rad(float(definition.rotation_offset))
