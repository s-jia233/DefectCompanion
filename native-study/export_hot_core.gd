extends "res://export_card_poses.gd"
const CORE_DIR=BASE+"mod-study/core-raw/"
var ordinary=false
var cover=false
func run():
	var viewport=SubViewport.new()
	viewport.size=Vector2i(320,240);viewport.transparent_bg=true;viewport.render_target_update_mode=SubViewport.UPDATE_ALWAYS
	root.add_child(viewport)
	sprite=ClassDB.instantiate("SpineSprite")
	sprite.skeleton_data_res=load("res://animations/characters/defect/defect_skel_data.tres")
	sprite.update_mode=2;sprite.position=Vector2(160,220);sprite.scale=Vector2(.085,.085)
	viewport.add_child(sprite);skeleton=sprite.get_skeleton()
	sprite.connect("before_animation_state_update",func(_v):
		for slot in skeleton.get_slots():slot.set_color(Color.WHITE)
	)
	sprite.connect("world_transforms_changed",func(_v):
		if not ordinary:pose()
		var seen_core=false
		var slots=skeleton.get_draw_order() if skeleton.has_method("get_draw_order") else skeleton.get_slots()
		for slot in slots:
			var name=slot.get_data().get_name()
			if name=="orb":seen_core=true
			var keep=(seen_core and not name in ["orb","orb patch","orb sparkles","orb shine","bod","glow1","glow2","glow3","glow4"]) if cover else name=="orb"
			if not keep:slot.set_color(Color(1,1,1,0))
	)
	DirAccess.make_dir_recursive_absolute(CORE_DIR)
	var only=OS.get_cmdline_user_args()
	for name in ["idle_loop","attack","cast","process","hurt","die"]:
		if only.size()>0 and not only.has(name):continue
		ordinary=true
		var count=int(round(skeleton.get_data().find_animation(name).get_duration()*60))
		skeleton.set_to_setup_pose();sprite.get_animation_state().clear_tracks()
		sprite.get_animation_state().set_animation(name,name.ends_with("loop"),0)
		for frame in range(count):
			cover=false
			sprite.update_skeleton(0 if frame==0 else 1.0/60)
			await process_frame
			await RenderingServer.frame_post_draw
			viewport.get_texture().get_image().save_png(CORE_DIR+name+"-"+str(frame)+".png")
			cover=true;sprite.update_skeleton(0)
			await process_frame
			await RenderingServer.frame_post_draw
			viewport.get_texture().get_image().save_png(CORE_DIR+name+"-cover-"+str(frame)+".png")
		print("CORE_EXPORTED ",name," ",count)
	var designs=JSON.parse_string(FileAccess.get_file_as_string(BASE+"art-direction.json"))
	for skill in designs:
		config=designs[skill]
		if only.size()>0 and not only.has(skill) and not only.has(config.clip):continue
		ordinary=false;head_only=false
		var count=int(round(config.duration*60))
		for frame in range(count):
			time=frame/60.0;cover=false
			skeleton.set_to_setup_pose();sprite.get_animation_state().clear_tracks()
			sprite.get_animation_state().set_animation(config.base,false,0)
			sprite.update_skeleton(sample(time))
			await process_frame
			await RenderingServer.frame_post_draw
			viewport.get_texture().get_image().save_png(CORE_DIR+config.clip+"-"+str(frame)+".png")
			cover=true;sprite.update_skeleton(0)
			await process_frame
			await RenderingServer.frame_post_draw
			viewport.get_texture().get_image().save_png(CORE_DIR+config.clip+"-cover-"+str(frame)+".png")
		print("CORE_EXPORTED ",config.clip," ",count)
	quit()
