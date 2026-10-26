extends SceneTree
const BASE="C:/Users/lenovo/Documents/Codex/2026-10-23/w/work/defect-desktop/"
func _initialize():call_deferred("export_all")
func export_all():
	var viewport=SubViewport.new()
	viewport.size=Vector2i(320,240)
	viewport.transparent_bg=true
	viewport.render_target_update_mode=SubViewport.UPDATE_ALWAYS
	root.add_child(viewport)
	var sprite=ClassDB.instantiate("SpineSprite")
	sprite.skeleton_data_res=load("res://animations/characters/defect/defect_skel_data.tres")
	sprite.update_mode=2
	sprite.position=Vector2(160,220)
	sprite.scale=Vector2(.085,.085)
	viewport.add_child(sprite)
	sprite.get_animation_state().set_animation("idle_loop",true,0)
	var script=GDScript.new()
	script.source_code=FileAccess.get_file_as_string(BASE+"mod-study/turbo_native.gd")
	if script.reload()!=OK:push_error("Unable to compile reference rig");quit(1);return
	var rig=script.new()
	if not rig.bind_body(sprite):push_error("Native rig binding mismatch");quit(1);return
	var age=[0.0]
	sprite.connect("world_transforms_changed",func(_value):
		rig.apply_pose(age[0],false)
		for slot in sprite.get_skeleton().get_slots():
			var name=slot.get_data().get_name()
			if not rig.arc_slot(name):slot.set_color(Color(1,1,1,0))
	)
	var sheet=Image.create(320*8,240*8,false,Image.FORMAT_RGBA8)
	for frame in range(94):
		rig.restore()
		age[0]=frame/60.0
		sprite.update_skeleton(0 if frame==0 else 1.0/60)
		await process_frame
		await RenderingServer.frame_post_draw
		var n=frame%64
		sheet.blit_rect(viewport.get_texture().get_image(),Rect2i(0,0,320,240),Vector2i(n%8*320,n/8*240))
		if n==63 or frame==93:
			sheet.save_png(BASE+"assets/mod-turbo-"+str(frame/64)+".png")
			sheet.fill(Color(0,0,0,0))
	print("EXPORTED_TURBO_NATIVE_ARCS 94")
	rig.release(false)
	quit()
