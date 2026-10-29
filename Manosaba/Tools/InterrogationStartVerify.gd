extends SceneTree
var failures := 0

func _initialize() -> void:
	call_deferred("verify")

func check(condition: bool, message: String) -> void:
	if not condition:
		push_error(message)
		failures += 1

func verify() -> void:
	root.size = Vector2i(960, 540)
	RenderingServer.set_default_clear_color(Color(0.18, 0.16, 0.20))
	AudioServer.set_bus_mute(0, true)
	if "--pack" in OS.get_cmdline_user_args():
		check(ProjectSettings.load_resource_pack("res://_mods_out/Manosaba.pck"), "Cannot mount exported pack")
	var music = load("res://Manosaba/audio/bgm/rAwTell Owk....mp3")
	check(music is AudioStream and music.get_length() > 0.0, "BGM missing or empty")
	var scene: PackedScene = load("res://Manosaba/scenes/vfx/interrogation_start/interrogation_start.tscn")
	check(scene != null, "Entrance scene cannot load")
	if scene == null:
		quit(1)
		return
	var effect = scene.instantiate()
	root.add_child(effect)
	effect.set_process(false)
	check(effect._nodes.size() == 42, "Native UI hierarchy changed")
	check(effect._sprites.filter(func(s): return s != null).size() == 23, "Missing native UI sprites")
	# The pans must follow the original animated LibraRoot, including fractional
	# frames and backwards seeking. A fixed world angle loses their +/-25 degree swing.
	var pan_indices: Array[int] = []
	for index in range(effect._nodes.size()):
		if bool(effect.ANIMATION.data.nodes[index].constrained):
			pan_indices.append(index)
	check(pan_indices.size() == 2, "Missing pan rotation constraints")
	var smallest := [INF, INF]
	var largest := [-INF, -INF]
	for time in [0.0, 0.125, 0.25, 0.5, 0.75, 1.0, 1.5, 2.0, 2.5, 0.125]:
		effect.seek(time)
		for pan in range(pan_indices.size()):
			var index := pan_indices[pan]
			var source := int(effect.ANIMATION.data.nodes[index].rotation_source)
			var angle: float = effect._nodes[index].global_rotation
			check(absf(angle_difference(angle, effect._nodes[source].global_rotation)) < 0.0001, "Pan did not follow its animated rotation source")
			check(absf(angle_difference(angle, effect._nodes[index + 2].global_rotation)) < 0.0001, "Pan sprite lost inherited swing")
			smallest[pan] = minf(smallest[pan], angle)
			largest[pan] = maxf(largest[pan], angle)
	for pan in range(pan_indices.size()):
		check(largest[pan] - smallest[pan] > deg_to_rad(40.0), "Pan is stationary or lost the native swing amplitude")
	var capture := "--capture" in OS.get_cmdline_user_args()
	if capture:
		DirAccess.make_dir_recursive_absolute("res://../docs/interrogation-start")
	for time in [0.0, 0.25, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.2]:
		effect.seek(time)
		if capture:
			await process_frame
			await RenderingServer.frame_post_draw
			root.get_texture().get_image().save_png("res://../docs/interrogation-start/frame_%04d.png" % int(time * 1000))
	if "--record" in OS.get_cmdline_user_args():
		DirAccess.make_dir_recursive_absolute("res://../docs/interrogation-start/swing_frames")
		for frame in range(100):
			effect.seek(float(frame) / 30.0)
			await process_frame
			await RenderingServer.frame_post_draw
			root.get_texture().get_image().save_png("res://../docs/interrogation-start/swing_frames/%04d.png" % frame)
	effect.seek(2.0)
	check(effect._nodes.any(func(n): return n.name == &"Start_01" and n.is_visible_in_tree()), "Title did not appear")
	root.size = Vector2i(1280, 720)
	await process_frame
	effect.seek(2.0)
	check(effect.stage.position.distance_to(Vector2(640, 360)) < 0.1, "Effect did not center after resize")
	check(absf(effect.stage.scale.x - 0.5) < 0.001, "Effect aspect ratio changed")
	effect.queue_free()
	await process_frame
	# Real playback must finish and room exit must also remove the overlay.
	var room := Node.new()
	root.add_child(room)
	var playing = scene.instantiate()
	room.add_child(playing)
	await create_timer(3.6).timeout
	check(not is_instance_valid(playing), "Entrance did not clean up after playback")
	var interrupted = scene.instantiate()
	room.add_child(interrupted)
	room.queue_free()
	await process_frame
	check(not is_instance_valid(interrupted), "Leaving room leaked overlay")
	print("Interrogation start verification: ", "PASS" if failures == 0 else str(failures) + " failures")
	quit(0 if failures == 0 else 1)
