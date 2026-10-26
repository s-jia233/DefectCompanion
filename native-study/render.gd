extends SceneTree
func _initialize():
	call_deferred("render_all")
func render_all():
	var viewport = SubViewport.new()
	viewport.size = Vector2i(420, 390)
	viewport.transparent_bg = true
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	root.add_child(viewport)
	var sprite = ClassDB.instantiate("SpineSprite")
	sprite.skeleton_data_res = load("res://animations/characters/defect/defect_skel_data.tres")
	sprite.position = Vector2(210,340)
	sprite.scale = Vector2(0.095,0.095)
	sprite.update_mode = 2
	viewport.add_child(sprite)
	DirAccess.make_dir_recursive_absolute("C:/Users/lenovo/Documents/Codex/2026-10-23/w/work/defect-desktop/native-study/samples")
	for name in ["idle_loop", "attack", "cast", "process", "relaxed_loop"]:
		sprite.get_skeleton().set_to_setup_pose()
		sprite.get_animation_state().set_animation(name, false, 0)
		sprite.update_skeleton(0.4)
		await process_frame
		await RenderingServer.frame_post_draw
		viewport.get_texture().get_image().save_png("C:/Users/lenovo/Documents/Codex/2026-10-23/w/work/defect-desktop/native-study/samples/"+name+".png")
		print("RENDERED ",name)
	quit()
