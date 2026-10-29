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
	AudioServer.set_bus_mute(0, true)
	if "--pack" in OS.get_cmdline_user_args():
		check(ProjectSettings.load_resource_pack("res://_mods_out/Manosaba.pck"), "Cannot mount exported pack")
	var music = load("res://Manosaba/audio/bgm/EmA (-狂-).mp3")
	check(music is AudioStream and music.get_length() > 0.0, "Missing EmA (-狂-) BGM")
	var scene: PackedScene = load("res://Manosaba/scenes/vfx/magic_to_kill_witch/magic_to_kill_witch.tscn")
	check(scene != null, "Reveal scene cannot load")
	if scene == null:
		quit(1)
		return
	var effect = scene.instantiate()
	root.add_child(effect)
	effect.set_process(false)
	check(effect.IMAGE.get_size() == Vector2(4096, 2048), "Wrong original scene texture")
	effect.seek(0.0)
	check(is_equal_approx(effect.white.color.a, 1.0), "Reveal must begin in white")
	effect.seek(2.75)
	check(effect.main.position.distance_to(Vector2(1344, -432)) < 0.1, "First pan did not reach native 120,90")
	check(is_equal_approx(effect.white.color.a, 0.0), "White fade did not finish")
	effect.seek(5.27)
	check(effect.main.position.distance_to(Vector2(-864, 648)) < 0.1, "Second pan did not reach native 5,-10")
	effect.seek(6.58)
	check(effect.middle.visible and effect.middle.modulate.a > 0.0 and effect.middle.modulate.a < 1.0, "Reveal lost its double exposure")
	check(float(effect.blur.get_shader_parameter("blur_pixels")) > 0.0, "Reveal blur did not start")
	effect.seek(8.68)
	check(effect.main.position.length() < 0.1, "Full-body reveal is not centered")
	check(absf(effect.main.scale.x - 1920.0 / 4096.0 * 1.15) < 0.001, "Native final zoom changed")
	check(not effect.middle.visible, "Close-up did not fade away")
	check(is_zero_approx(float(effect.blur.get_shader_parameter("blur_pixels"))), "Reveal blur did not clear")
	effect.seek(11.0)
	check(effect.visuals.modulate.a > 0.0 and effect.visuals.modulate.a < 1.0, "Scene did not fade back to combat")
	# Seeking backward must restore the original layer, blur and white state.
	effect.seek(0.0)
	check(is_equal_approx(effect.white.color.a, 1.0) and not effect.middle.visible, "Backward seek retained later layers")
	var capture := "--capture" in OS.get_cmdline_user_args()
	if capture:
		DirAccess.make_dir_recursive_absolute("res://../docs/magic-to-kill-witch")
		for time in [0.0, 1.5, 2.5, 4.0, 5.0, 6.58, 7.0, 8.68, 10.0]:
			effect.seek(time)
			await process_frame
			await RenderingServer.frame_post_draw
			root.get_texture().get_image().save_png("res://../docs/magic-to-kill-witch/frame_%05d.png" % int(time * 1000))
	if "--record" in OS.get_cmdline_user_args():
		DirAccess.make_dir_recursive_absolute("res://../docs/magic-to-kill-witch/frames")
		for frame in range(339):
			effect.seek(float(frame) / 30.0)
			await process_frame
			await RenderingServer.frame_post_draw
			root.get_texture().get_image().save_png("res://../docs/magic-to-kill-witch/frames/%04d.png" % frame)
	root.size = Vector2i(1280, 1024)
	await process_frame
	effect.seek(8.68)
	check(effect.stage.position.distance_to(Vector2(640, 512)) < 0.1, "Viewport resize did not center scene")
	check(is_equal_approx(effect.stage.scale.x, effect.stage.scale.y), "Viewport resize distorted scene")
	effect.queue_free()
	await process_frame
	var room := Node.new()
	root.add_child(room)
	var playing = scene.instantiate()
	room.add_child(playing)
	await create_timer(11.6).timeout
	check(not is_instance_valid(playing), "Scene did not clean up after real playback")
	var interrupted = scene.instantiate()
	room.add_child(interrupted)
	room.queue_free()
	await process_frame
	check(not is_instance_valid(interrupted), "Room exit leaked scene overlay")
	print("Magic to Kill the Witch verification: ", "PASS" if failures == 0 else str(failures) + " failures")
	quit(0 if failures == 0 else 1)
