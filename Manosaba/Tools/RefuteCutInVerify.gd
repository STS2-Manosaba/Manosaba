extends SceneTree
## Run with --path Manosaba --script Tools/RefuteCutInVerify.gd
## Add -- --capture to render sample PNGs with a graphical renderer.
var failures := 0

func _initialize() -> void:
	call_deferred("verify")

func check(condition: bool, message: String) -> void:
	if not condition:
		push_error(message)
		failures += 1

func verify() -> void:
	root.size = Vector2i(960, 540)
	RenderingServer.set_default_clear_color(Color(0.07, 0.035, 0.085))
	var capture := "--capture" in OS.get_cmdline_user_args()
	if "--pack" in OS.get_cmdline_user_args():
		check(ProjectSettings.load_resource_pack("res://_mods_out/Manosaba.pck"), "Exported PCK could not be mounted")
	for suffix in ["ema1", "ema2", "ema3", "hiro1", "hiro2", "hiro3", "witch_hiro1", "witch_hiro2", "witch_hiro3"]:
		check(load("res://Manosaba/images/cards/test_refute_cut_in_%s.png" % suffix) is Texture2D,
			"Missing test card portrait: " + suffix)
	for card in ["present_evidence", "rebuttal", "agreement", "sophistry", "question", "exhaustive_method", "statement", "acc_uh_earts_indictment", "acc_uh_earts_revelation", "d_arime"]:
		var voice = load("res://Manosaba/audio/SFX/ema/%s.ogg" % card)
		check(voice is AudioStream and voice.get_length() > 0.0, "Missing card voice: " + card)
	for bgm in ["AccUhEarts (-告-).mp3", "AccUhEarts (-発-).mp3", "DArime.mp3"]:
		var music = load("res://Manosaba/audio/bgm/" + bgm)
		check(music is AudioStream and music.get_length() > 0.0, "Missing BGM: " + bgm)
	var scene: PackedScene = load("res://Manosaba/scenes/vfx/refute_cutin/refute_cutin.tscn")
	check(scene != null, "Cut-in scene failed to load")
	if scene == null:
		quit(1)
		return
	# Voice ownership must survive overlay cleanup while muting suppresses creation.
	AudioServer.set_bus_mute(0, true)
	for volume in [0.0, 1.0]:
		var voice_room := Node.new()
		root.add_child(voice_room)
		var voiced = scene.instantiate()
		voiced.audio_volume = volume
		voiced.voice_path = "res://Manosaba/audio/SFX/ema/rebuttal.ogg"
		voice_room.add_child(voiced)
		var voices = voice_room.get_children().filter(func(child): return child is AudioStreamPlayer)
		check(voices.size() == int(volume), "Voice mute/room ownership failed")
		if not voices.is_empty():
			check(voices[0].playing, "Card voice did not start with cut-in")
		voiced.queue_free()
		await process_frame
		if not voices.is_empty():
			check(is_instance_valid(voices[0]), "Overlay cleanup truncated card voice")
		voice_room.queue_free()
		await process_frame
		if not voices.is_empty():
			check(not is_instance_valid(voices[0]), "Room exit left card voice playing")
	# Every public variant must load its own portrait and the actor's original Timeline.
	var portrait_paths: Array[String] = []
	for actor in range(3):
		for expression in range(3):
			var variant = scene.instantiate()
			variant.character = actor
			variant.expression = expression
			variant.audio_volume = 0.0
			root.add_child(variant)
			variant.set_process(false)
			variant.seek(2.0)
			check(is_instance_valid(variant.portrait) and variant.portrait.visible, "Variant portrait did not appear")
			check(not portrait_paths.has(variant.portrait.texture.resource_path), "Two variants selected the same portrait")
			portrait_paths.append(variant.portrait.texture.resource_path)
			check(variant._audio_players.size() == 2, "Original audio cues missing")
			if actor == 2:
				check(variant._extra_particles.size() > 0, "Witch Hiro lost her original particle effects")
			if capture:
				await process_frame
				await RenderingServer.frame_post_draw
				root.get_texture().get_image().save_png("res://../docs/refute-cutin/variant_%d_%d.png" % [actor, expression+1])
			variant.queue_free()
			await process_frame
	var effect = scene.instantiate()
	effect.audio_volume = 0.0
	root.add_child(effect)
	effect.set_process(false)
	for time in [0.0, 0.4, 0.65, 0.95, 1.6, 2.8, 3.4]:
		effect.seek(time)
		check(effect.glass.texture != null and effect.ema.texture != null, "Missing cut-in textures")
		if capture:
			await process_frame
			await RenderingServer.frame_post_draw
			root.get_texture().get_image().save_png("res://../docs/refute-cutin/frame_%04d.png" % int(time * 1000))
	check(effect.phase == 2, "Fractured glass phase was not reached")
	effect.seek(1.65)
	check(effect.fragments.any(func(shard): return shard.visible), "Shards did not appear")
	# Original geometry projects at 75 pixels per Unity unit, expression is selected once.
	effect.seek(2.0)
	check(effect.ema.position.distance_to(Vector2(881.775, 693.825)) < 1.0, "Original character framing changed")
	check(absf(effect.ema.scale.x - 0.63877) < 0.002, "Original character size changed")
	check(effect.ema.name == "RefuteCutIn_Ema_003", "Reference expression was not selected")
	if "--record" in OS.get_cmdline_user_args():
		DirAccess.make_dir_recursive_absolute("res://../docs/refute-cutin/frames")
		for index in range(113):
			effect.seek(float(index) / 30.0)
			await process_frame
			await RenderingServer.frame_post_draw
			root.get_texture().get_image().save_png("res://../docs/refute-cutin/frames/%04d.png" % index)
	root.size = Vector2i(1280, 1024)
	effect.seek(1.6)
	check(is_equal_approx(effect.stage.scale.x, effect.stage.scale.y), "Aspect ratio distorted")
	check(effect.stage.position.y > 0.0, "4:3 viewport was not letterboxed")
	effect.queue_free()
	await process_frame
	# Exercise real timing, cleanup and replay rather than only sampled stills.
	var playing_variants: Array[Node] = []
	for variant_index in range(9):
		var playing = scene.instantiate()
		playing.character = floori(float(variant_index) / 3.0)
		playing.expression = variant_index % 3
		playing.audio_volume = 0.0
		root.add_child(playing)
		playing_variants.append(playing)
	await create_timer(3.95).timeout
	for playing in playing_variants:
		check(not is_instance_valid(playing), "Cut-in failed to clean up after playback")
	# Room exit must also remove the effect early.
	var room := Node.new()
	root.add_child(room)
	var interrupted = scene.instantiate()
	interrupted.audio_volume = 0.0
	room.add_child(interrupted)
	room.queue_free()
	await process_frame
	check(not is_instance_valid(interrupted), "Room exit left an orphan overlay")
	print("Refute cut-in verification: ", failures, " failure(s)")
	quit(0 if failures == 0 else 1)
