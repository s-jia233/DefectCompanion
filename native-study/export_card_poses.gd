extends SceneTree
const BASE="C:/Users/lenovo/Documents/Codex/2026-10-23/w/work/defect-desktop/"
var sprite
var skeleton
var config
var time=0.0
var head_only=false
func _initialize():call_deferred("run")
func matrix(b):return Transform2D(Vector2(b.get_a(),b.get_c()),Vector2(b.get_b(),b.get_d()),Vector2(b.get_world_x(),b.get_world_y()))
func world(b,m):
	b.set_a(m.x.x);b.set_c(m.x.y);b.set_b(m.y.x);b.set_d(m.y.y);b.set_world_x(m.origin.x);b.set_world_y(m.origin.y)
func smooth(x):
	var q=clampf(x,0,1)
	return q*q*(3-2*q)
func branch(bone):
	var found=[]
	var name=bone.get_data().get_bone_name()
	for b in skeleton.get_bones():
		var p=b
		while p!=null:
			if p.get_data().get_bone_name()==name:found.append(b);break
			p=p.get_parent()
	return found
func turn(bone,pivot,angle):
	var rotation=Transform2D(angle,Vector2.ZERO)
	rotation.origin=pivot-rotation.basis_xform(pivot)
	for b in branch(bone):world(b,rotation*matrix(b))
func ik(side,target,weight):
	var upper=skeleton.find_bone("arm_upper_"+side)
	var lower=skeleton.find_bone("arm_lower_"+side)
	var hand=skeleton.find_bone("hand_"+side)
	var s=matrix(upper).origin
	var e=matrix(lower).origin
	var w=matrix(hand).origin
	var hand_angle=matrix(hand).x.angle()
	target=w.lerp((target-Vector2(210,324))/.085,weight)
	var a=s.distance_to(e)
	var b=e.distance_to(w)
	var delta=target-s
	var length=clampf(delta.length(),absf(a-b)+.1,(a+b)*.985)
	target=s+delta.normalized()*length
	var base=delta.angle()
	var angle=acos(clampf((a*a+length*length-b*b)/(2*a*length),-1,1))
	var elbow=s+Vector2.from_angle(base+angle)*a
	turn(upper,s,(elbow-s).angle()-(e-s).angle())
	e=matrix(lower).origin
	w=matrix(hand).origin
	turn(lower,e,(target-e).angle()-(w-e).angle())
	turn(hand,matrix(hand).origin,hand_angle-matrix(hand).x.angle())
func pose():
	var fade=smooth(time/.36)*(1-smooth((time-config.hold)/(config.duration-config.hold)))
	if config.hands!=null and config.clip!="art-rocket":
		for i in range(2):ik("l" if i==0 else "r",Vector2(config.hands[i][0],config.hands[i][1]),fade)
	if config.clip=="art-rocket":
		var press=smooth(time/.06)*(1-smooth((time-config.hold)/(config.duration-config.hold)))
		var jab=smooth((time-.10)/.12)
		ik("l",Vector2(182,267),press)
		ik("r",Vector2(255,243).lerp(Vector2(298,226),jab),press)
	if config.clip=="art-claw":
		var weight=smooth(time/.10)*(1-smooth((time-.82)/.78))
		var target=Vector2(254,223).lerp(Vector2(277,184),smooth((time-.06)/.24))
		if time>=.30:target=Vector2(277,184).lerp(Vector2(309,262),smooth((time-.30)/.16))
		if time>=.54:target=Vector2(309,262).lerp(Vector2(285,273),smooth((time-.54)/.28))
		ik("r",target,weight)
		ik("l",Vector2(180,264).lerp(Vector2(177,242),smooth((time-.28)/.20)),weight)
		var hand=skeleton.find_bone("hand_r")
		turn(hand,matrix(hand).origin,deg_to_rad(18-49*smooth((time-.30)/.16))*weight)
	if config.clip=="art-ball-lightning":
		var weight=smooth(time/.18)*(1-smooth((time-1.10)/.80))
		var push=smooth((time-.64)/.15)
		ik("r",Vector2(276,246).lerp(Vector2(310,223),push),weight)
		ik("l",Vector2(184,262),weight)
	if config.clip=="art-ice-spear":
		var weight=smooth(time/.24)*(1-smooth((time-1.55)/.75))
		var thrust=smooth((time-.82)/.20)*(1-smooth((time-1.15)/.35))
		ik("l",Vector2(181,263)+Vector2(26,-6)*thrust,weight)
		ik("r",Vector2(254,238)+Vector2(26,-6)*thrust,weight)
	if config.clip=="art-overclock":
		var weight=smooth(time/.25)*(1-smooth((time-1.25)/.85))
		ik("l",Vector2(179,259),weight)
		ik("r",Vector2(279,245),weight)
		var head=skeleton.find_bone("head")
		turn(head,matrix(head).origin,deg_to_rad(-4)*weight)
		for side in ["l","r"]:
			var hand=skeleton.find_bone("hand_"+side)
			turn(hand,matrix(hand).origin,deg_to_rad(sin(time*5.0)*7.0*(1 if side=="r" else -1))*weight)
	if config.clip=="art-learning":
		var weight=smooth(time/.28)*(1-smooth((time-1.85)/.75))
		var reveal=smooth((time-.52)/.40)
		ik("r",Vector2(260,174).lerp(Vector2(281,235),reveal),weight)
		ik("l",Vector2(181,251),weight)
		var head=skeleton.find_bone("head")
		turn(head,matrix(head).origin,deg_to_rad(-7+3*sin(time*2.2))*weight)
		# Extend the existing neck and its two original rear pipes. The head
		# remains rigid; the pipe bases remain anchored inside the scarf.
		var neck=skeleton.find_bone("neck")
		var pipe=skeleton.find_bone("head_pipe_attach")
		var neck_matrix=matrix(neck)
		var pipe_matrix=matrix(pipe)
		var lift=(18.0/.085)*weight
		var height=maxf(100.0,neck_matrix.origin.y-matrix(head).origin.y)
		var stretch=Transform2D(Vector2(1,0),Vector2(0,1+lift/height),Vector2(0,-neck_matrix.origin.y*lift/height))
		world(neck,stretch*neck_matrix)
		for bone in branch(head):
			var m=matrix(bone);m.origin.y-=lift;world(bone,m)
		var base_y=neck_matrix.origin.y+70.0
		var pipe_height=maxf(160.0,base_y-pipe_matrix.origin.y)
		var pipe_stretch=Transform2D(Vector2(1,0),Vector2(0,1+lift/pipe_height),Vector2(0,-base_y*lift/pipe_height))
		world(pipe,pipe_stretch*pipe_matrix)
	if config.clip=="art-fusion":
		var weight=smooth(time/.30)*(1-smooth((time-1.65)/.85))
		var press=smooth((time-.42)/.64)
		ik("l",Vector2(208,257).lerp(Vector2(221,251),press),weight)
		ik("r",Vector2(288,249).lerp(Vector2(314,241),press),weight)
		var hand=skeleton.find_bone("hand_r")
		turn(hand,matrix(hand).origin,deg_to_rad(-16)*weight)
	for slot in skeleton.get_slots():
		var name=slot.get_data().get_name()
		if head_only and name!="head":slot.set_color(Color(1,1,1,0))
		if config.clip=="art-rocket" and (name=="r hand" or name.begins_with("blender-")):slot.set_color(Color(1,1,1,0))
		if config.clip in ["art-thunder","art-turbo","art-ice-spear"] and (name=="l hand" or name=="r hand" or name.begins_with("blender-")):slot.set_color(Color(1,1,1,0))
func sample(t):
	var v=config.pose
	if t<config.release:return v[0]*smooth(t/config.release)
	if t<config.hold:return lerpf(v[0],v[1],smooth((t-config.release)/(config.hold-config.release)))
	return lerpf(v[1],v[2],smooth((t-config.hold)/(config.duration-config.hold)))
func run():
	var viewport=SubViewport.new()
	viewport.size=Vector2i(320,240);viewport.transparent_bg=true;viewport.render_target_update_mode=SubViewport.UPDATE_ALWAYS
	root.add_child(viewport)
	sprite=ClassDB.instantiate("SpineSprite")
	sprite.skeleton_data_res=load("res://animations/characters/defect/defect_skel_data.tres")
	sprite.update_mode=2;sprite.position=Vector2(160,220);sprite.scale=Vector2(.085,.085)
	viewport.add_child(sprite);skeleton=sprite.get_skeleton()
	sprite.connect("before_animation_state_update",func(_v):
		for s in skeleton.get_slots():s.set_color(Color.WHITE)
	)
	sprite.connect("world_transforms_changed",func(_v):pose())
	var designs=JSON.parse_string(FileAccess.get_file_as_string(BASE+"art-direction.json"))
	var meta=JSON.parse_string(FileAccess.get_file_as_string(BASE+"mod-study/coolheaded.mesh.json"))
	var charge=JSON.parse_string(FileAccess.get_file_as_string(BASE+"mod-study/charge-battery-binding.json"))
	var cp=charge.profiles[1]
	var profile=meta.profiles[1]
	var bones=[]
	for name in profile.bones:bones.append(skeleton.find_bone(name))
	var result={}
	var only=OS.get_cmdline_user_args()
	if only.size()>0 and FileAccess.file_exists(BASE+"mod-study/art-poses.json"):
		result=JSON.parse_string(FileAccess.get_file_as_string(BASE+"mod-study/art-poses.json"))
	DirAccess.make_dir_recursive_absolute(BASE+"mod-study/art-raw")
	for skill in designs:
		if only.size()>0 and not only.has(skill):continue
		config=designs[skill]
		var count=int(round(config.duration*60))
		var frames=[]
		for frame in range(count):
			time=frame/60.0;head_only=false
			# Reset native animation at every sample: analytic pose modifications never accumulate.
			skeleton.set_to_setup_pose();sprite.get_animation_state().clear_tracks()
			sprite.get_animation_state().set_animation(config.base,false,0)
			sprite.update_skeleton(sample(time))
			var anchors=[]
			for name in ["hand_l","hand_r","orb","head"]:
				var p=sprite.get_global_bone_transform(name).origin
				anchors.append([p.x,p.y])
			var deform=skeleton.find_slot("head").get_deform()
			var matrices=[]
			for b in bones:matrices.append(matrix(b))
			var points=[]
			for row in profile.points:
				var p=Vector2.ZERO
				for v in row:
					var d=Vector2(deform[int(v.offset)],deform[int(v.offset)+1]) if deform.size()>0 else Vector2.ZERO
					p+=matrices[int(v.bone)]*(Vector2(v.x,v.y)+d)*v.weight
				points.append(p)
			var angle=matrix(skeleton.find_bone("head")).x.angle()-cp.headAngle
			var clamps=[]
			for i in range(2):
				var m=cp.mounts[i]
				var p=Vector2.ZERO
				for j in range(m.ids.size()):p+=points[int(m.ids[j])]*m.weights[j]
				var c=charge.cables[i]
				var offset=Vector2(c.clamp.bbox[0]/4,c.clamp.bbox[1]/4)-Vector2(c.referenceMount.x,c.referenceMount.y)
				var origin=Vector2(210,324)+(p+offset.rotated(angle)/.3)*.085
				var mount=Vector2(210,324)+p*.085
				clamps.append([origin.x,origin.y,mount.x,mount.y])
			var arc=Vector2(210,324)+(matrix(skeleton.find_bone("head_pipe_attach"))*Vector2(-75,-25))*.085
			frames.append({"anchors":anchors,"clamps":clamps,"arc":[arc.x,arc.y],"angle":angle})
			await process_frame
			await RenderingServer.frame_post_draw
			viewport.get_texture().get_image().save_png(BASE+"mod-study/art-raw/"+config.clip+"-"+str(frame)+".png")
			head_only=true;sprite.update_skeleton(0)
			await process_frame
			await RenderingServer.frame_post_draw
			viewport.get_texture().get_image().save_png(BASE+"mod-study/art-raw/"+config.clip+"-head-"+str(frame)+".png")
		result[config.clip]=frames
		print("EXPORTED_CARD_POSE ",config.clip," ",count)
	FileAccess.open(BASE+"mod-study/art-poses.json",FileAccess.WRITE).store_string(JSON.stringify(result))
	quit()
