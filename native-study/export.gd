extends SceneTree
const DEST = "C:/Users/lenovo/Documents/Codex/2026-10-23/w/work/defect-desktop/assets/"
func _initialize():
	call_deferred("export_all")
func export_all():
	var viewport = SubViewport.new()
	viewport.size = Vector2i(320,240)
	viewport.transparent_bg = true
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	root.add_child(viewport)
	var sprite = ClassDB.instantiate("SpineSprite")
	sprite.skeleton_data_res = load("res://animations/characters/defect/defect_skel_data.tres")
	sprite.position = Vector2(160,220)
	sprite.scale = Vector2(0.085,0.085)
	sprite.update_mode = 2
	viewport.add_child(sprite)
	var manifest = {}
	for name in ["idle_loop","attack","cast","process","hurt","die"]:
		var duration = sprite.get_skeleton().get_data().find_animation(name).get_duration()
		var count = int(round(duration*60))
		var clip = {"duration":duration,"fps":60,"count":count,"anchors":[]}
		sprite.get_animation_state().clear_tracks()
		sprite.get_skeleton().set_to_setup_pose()
		sprite.get_animation_state().set_animation(name, name.ends_with("loop"), 0)
		var sheet = Image.create(320*8,240*8,false,Image.FORMAT_RGBA8)
		for i in range(count):
			sprite.update_skeleton(0 if i==0 else 1.0/60.0)
			await process_frame
			await RenderingServer.frame_post_draw
			var im = viewport.get_texture().get_image()
			var local = i%64
			sheet.blit_rect(im,Rect2i(0,0,320,240),Vector2i(local%8*320,local/8*240))
			var anchors = []
			for bone in ["hand_l","hand_r","orb","head"]:
				var p = sprite.get_global_bone_transform(bone).origin
				anchors.append([snappedf(p.x,0.01),snappedf(p.y,0.01)])
			clip.anchors.append(anchors)
			if local==63 or i==count-1:
				sheet.save_png(DEST+"native-"+name+"-"+str(i/64)+".png")
				sheet.fill(Color(0,0,0,0))
		manifest[name] = clip
		print("EXPORTED ",name," ",count)
	var output = FileAccess.open(DEST+"native-animation.json",FileAccess.WRITE)
	output.store_string(JSON.stringify(manifest))
	quit()
