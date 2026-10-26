extends "res://export_card_poses.gd"
var layer=0
func run():
	var viewport=SubViewport.new()
	viewport.size=Vector2i(320,240);viewport.transparent_bg=true;viewport.render_target_update_mode=SubViewport.UPDATE_ALWAYS
	root.add_child(viewport)
	sprite=ClassDB.instantiate("SpineSprite");sprite.skeleton_data_res=load("res://animations/characters/defect/defect_skel_data.tres")
	sprite.update_mode=2;sprite.position=Vector2(160,220);sprite.scale=Vector2(.085,.085);viewport.add_child(sprite);skeleton=sprite.get_skeleton()
	sprite.connect("before_animation_state_update",func(_v):
		for slot in skeleton.get_slots():slot.set_color(Color.WHITE)
	)
	sprite.connect("world_transforms_changed",func(_v):
		pose()
		var passed=false
		var slots=skeleton.get_draw_order() if skeleton.has_method("get_draw_order") else skeleton.get_slots()
		for slot in slots:
			var n=slot.get_data().get_name()
			if n==("neck" if layer<2 else "pipe 1"):passed=true
			var selected=n=="neck" if layer<2 else n in ["pipe 1","pipe 2"]
			var keep=selected if layer%2==0 else passed and not selected
			if not keep:slot.set_color(Color(1,1,1,0))
	)
	config=JSON.parse_string(FileAccess.get_file_as_string(BASE+"art-direction.json"))["machine_learning"]
	var dir=BASE+"mod-study/learning-neck-raw/";DirAccess.make_dir_recursive_absolute(dir)
	for frame in range(int(round(config.duration*60))):
		time=frame/60.0;head_only=false
		skeleton.set_to_setup_pose();sprite.get_animation_state().clear_tracks();sprite.get_animation_state().set_animation(config.base,false,0)
		for pass_index in range(4):
			layer=pass_index;sprite.update_skeleton(sample(time) if layer==0 else 0)
			await process_frame;await RenderingServer.frame_post_draw
			viewport.get_texture().get_image().save_png(dir+str(frame)+"-"+str(layer)+".png")
	print("EXPORTED_ORIGINAL_NECK_AND_PIPES 156");quit()
