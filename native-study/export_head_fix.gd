extends SceneTree
const BASE="C:/Users/lenovo/Documents/Codex/2026-10-23/w/work/defect-desktop/"
func _initialize():call_deferred("export_all")
func matrix(b):return Transform2D(Vector2(b.get_a(),b.get_c()),Vector2(b.get_b(),b.get_d()),Vector2(b.get_world_x(),b.get_world_y()))
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
	# A hidden SpineSprite skips pose evaluation. Keep it visible and mask slots only.
	sprite.connect("before_animation_state_update",func(_v):
		for s in sprite.get_skeleton().get_slots():s.set_color(Color.WHITE)
	)
	sprite.connect("world_transforms_changed",func(_v):
		for s in sprite.get_skeleton().get_slots():
			if s.get_data().get_name()!="head":s.set_color(Color(1,1,1,0))
	)
	var skeleton=sprite.get_skeleton()
	var meta=JSON.parse_string(FileAccess.get_file_as_string(BASE+"mod-study/coolheaded.mesh.json"))
	var charge=JSON.parse_string(FileAccess.get_file_as_string(BASE+"mod-study/charge-battery-binding.json"))
	var profile=meta.profiles[1]
	var cp=charge.profiles[1]
	var bones=[]
	for name in profile.bones:bones.append(skeleton.find_bone(name))
	var head=skeleton.find_bone("head")
	var pipe=skeleton.find_bone("head_pipe_attach")
	var slot=skeleton.find_slot("head")
	var result={}
	DirAccess.make_dir_recursive_absolute(BASE+"mod-study/head-raw")
	for name in ["idle_loop","attack","cast","process","hurt","die"]:
		var count=int(round(skeleton.get_data().find_animation(name).get_duration()*60))
		sprite.get_animation_state().clear_tracks()
		skeleton.set_to_setup_pose()
		sprite.get_animation_state().set_animation(name,name.ends_with("loop"),0)
		var poses=[]
		for frame in range(count):
			sprite.update_skeleton(0 if frame==0 else 1.0/60)
			var deform=slot.get_deform()
			var matrices=[]
			for bone in bones:matrices.append(matrix(bone))
			var points=[]
			for row in profile.points:
				var p=Vector2.ZERO
				for v in row:
					var d=Vector2(deform[int(v.offset)],deform[int(v.offset)+1]) if deform.size()>0 else Vector2.ZERO
					p+=matrices[int(v.bone)]*(Vector2(v.x,v.y)+d)*v.weight
				points.append(p)
			var angle=matrix(head).x.angle()-cp.headAngle
			var pose=[]
			for i in range(2):
				var mount=cp.mounts[i]
				var p=Vector2.ZERO
				for j in range(mount.ids.size()):p+=points[int(mount.ids[j])]*mount.weights[j]
				var cable=charge.cables[i]
				var offset=Vector2(cable.clamp.bbox[0]/4,cable.clamp.bbox[1]/4)-Vector2(cable.referenceMount.x,cable.referenceMount.y)
				var position=Vector2(210,324)+(p+offset.rotated(angle)/.3)*.085
				var attach=Vector2(210,324)+p*.085
				pose.append([position.x,position.y,attach.x,attach.y])
			var arc=Vector2(210,324)+(matrix(pipe)*Vector2(-75,-25))*.085
			var anchor=sprite.get_global_bone_transform("head").origin
			poses.append({"angle":angle,"clamps":pose,"arc":[arc.x,arc.y],"head":[anchor.x,anchor.y]})
			await process_frame
			await RenderingServer.frame_post_draw
			viewport.get_texture().get_image().save_png(BASE+"mod-study/head-raw/"+name+"-"+str(frame)+".png")
		result[name]=poses
		print("EXPORTED_MOVING_HEAD ",name," ",count)
	FileAccess.open(BASE+"mod-study/head-poses.json",FileAccess.WRITE).store_string(JSON.stringify(result))
	quit()
