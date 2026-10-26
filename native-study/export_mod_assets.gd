extends SceneTree
const BASE = "C:/Users/lenovo/Documents/Codex/2026-10-23/w/work/defect-desktop/"
const MOD = "E:/SteamLibrary/steamapps/workshop/content/2868840/3810696310/assets/"
func _initialize():
	call_deferred("export_all")
func smooth(a,b,x):
	var q=clampf((x-a)/(b-a),0,1)
	return q*q*(3-2*q)
func data_texture(f):
	var w=f.get_32()
	var h=f.get_32()
	return ImageTexture.create_from_image(Image.create_from_data(w,h,false,Image.FORMAT_RGBAF,f.get_buffer(w*h*16)))
func export_wall(viewport):
	viewport.size=Vector2i(320,267)
	var f=FileAccess.open(MOD+"defragment/geometry.bin",FileAccess.READ)
	var ints=[]
	for i in range(14):ints.append(f.get_32())
	var scan=f.get_float()
	var scale=f.get_float()
	var records=data_texture(f)
	var bins=data_texture(f)
	var candidates=data_texture(f)
	var lights=FileAccess.open(MOD+"defragment/settled-light.bin",FileAccess.READ)
	var lw=lights.get_32()
	var lh=lights.get_32()
	var light=ImageTexture.create_from_image(Image.create_from_data(lw,lh,false,Image.FORMAT_RGBA8,lights.get_buffer(lw*lh*4)))
	var shader=Shader.new()
	shader.code=FileAccess.get_file_as_string(MOD+"defragment/procedural.gdshader")
	var settled=Shader.new()
	settled.code=shader.code.replace("const bool opening = true;","const bool opening = false;")
	var material=ShaderMaterial.new()
	material.shader=shader
	material.set_shader_parameter("records",records)
	material.set_shader_parameter("bins",bins)
	material.set_shader_parameter("candidates",candidates)
	material.set_shader_parameter("grid",Vector4(ints[7],ints[8],ints[9],ints[6]))
	material.set_shader_parameter("placement",Vector4(ints[10],ints[11],scale,ints[4]))
	material.set_shader_parameter("extent",Vector2(528,440))
	material.set_shader_parameter("settled_light",light)
	var node=ColorRect.new()
	node.size=Vector2(320,267)
	node.material=material
	viewport.add_child(node)
	var raw=records.get_image().get_data()
	var sheet=Image.create(320*8,267*8,false,Image.FORMAT_RGBA8)
	for frame in range(108):
		var t=frame/60.0/0.75
		material.shader=shader if t<0.85 else settled
		material.set_shader_parameter("records",records)
		material.set_shader_parameter("bins",bins)
		material.set_shader_parameter("candidates",candidates)
		material.set_shader_parameter("grid",Vector4(ints[7],ints[8],ints[9],ints[6]))
		material.set_shader_parameter("placement",Vector4(ints[10],ints[11],scale,ints[4]))
		material.set_shader_parameter("extent",Vector2(528,440))
		material.set_shader_parameter("settled_light",light)
		material.set_shader_parameter("clock",Vector4(t,1-smooth(2.03,2.38,t),smooth(1.21,1.86,t)*scan if t>1.21 and t<2.38 else 0,smooth(1.18,1.34,t)))
		if t<0.85:
			var transforms=PackedVector4Array()
			var visibility=PackedVector4Array()
			for i in range(98):
				var o=i*9*16
				var center=Vector2(raw.decode_float(o),raw.decode_float(o+4))
				var scatter=Vector2(raw.decode_float(o+8),raw.decode_float(o+12))
				var delay=raw.decode_float(o+16)
				var turn=raw.decode_float(o+20)
				var depth=raw.decode_float(o+44)
				var v=smooth(.08+delay,.5+delay,t)
				var k=1-v
				var s=.94+.06*v
				var pos=center+scatter*k
				transforms.append(Vector4(pos.x,pos.y,cos(turn*k)/s,sin(turn*k)/s))
				visibility.append(Vector4(smooth(delay*.35,.18+delay*.35,t),depth*k,s,0))
			material.set_shader_parameter("entrance",transforms)
			material.set_shader_parameter("entrance_visibility",visibility)
		await process_frame
		await RenderingServer.frame_post_draw
		var n=frame%64
		sheet.blit_rect(viewport.get_texture().get_image(),Rect2i(0,0,320,267),Vector2i(n%8*320,n/8*267))
		if n==63 or frame==107:
			sheet.save_png(BASE+"assets/mod-wall-"+str(frame/64)+".png")
			sheet.fill(Color(0,0,0,0))
	node.free()
	print("EXPORTED_MOD_WALL 108")
func matrix(bone):
	return Transform2D(Vector2(bone.get_a(),bone.get_c()),Vector2(bone.get_b(),bone.get_d()),Vector2(bone.get_world_x(),bone.get_world_y()))
func export_head(viewport):
	viewport.size=Vector2i(320,240)
	var meta=JSON.parse_string(FileAccess.get_file_as_string(BASE+"mod-study/coolheaded.mesh.json"))
	var charge=JSON.parse_string(FileAccess.get_file_as_string(BASE+"mod-study/charge-battery-binding.json"))
	var profile=meta.profiles[1]
	var cp=charge.profiles[1]
	var sprite=ClassDB.instantiate("SpineSprite")
	sprite.skeleton_data_res=load("res://animations/characters/defect/defect_skel_data.tres")
	sprite.update_mode=2
	sprite.visible=false
	viewport.add_child(sprite)
	var skeleton=sprite.get_skeleton()
	var bones=[]
	for name in profile.bones:bones.append(skeleton.find_bone(name))
	var head=skeleton.find_bone("head")
	var pipe=skeleton.find_bone("head_pipe_attach")
	var slot=skeleton.find_slot(meta.headSlot)
	var ice=Polygon2D.new()
	ice.texture=ImageTexture.create_from_image(Image.load_from_file(MOD+"coolheaded-ice.png"))
	ice.scale=Vector2(.085,.085)
	ice.position=Vector2(160,220)
	var uv=PackedVector2Array()
	for i in range(meta.uv.size()/2):uv.append(Vector2(meta.uv[i*2],meta.uv[i*2+1]))
	ice.uv=uv
	var polygons=[]
	for i in range(meta.indices.size()/3):polygons.append(PackedInt32Array([meta.indices[i*3],meta.indices[i*3+1],meta.indices[i*3+2]]))
	ice.polygons=polygons
	viewport.add_child(ice)
	var weights=[]
	for row in meta.weights:
		var sparse=[]
		for j in range(row.size()):
			if absf(row[j])>0.00000001:sparse.append([j,row[j]])
		weights.append(sparse)
	var result={}
	DirAccess.make_dir_recursive_absolute(BASE+"mod-study/ice-raw")
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
			var vertices=PackedVector2Array()
			for row in weights:
				var p=Vector2.ZERO
				for v in row:p+=points[int(v[0])]*v[1]
				vertices.append(p)
			ice.polygon=vertices
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
			poses.append({"angle":angle,"clamps":pose,"arc":[arc.x,arc.y]})
			await process_frame
			await RenderingServer.frame_post_draw
			viewport.get_texture().get_image().save_png(BASE+"mod-study/ice-raw/"+name+"-"+str(frame)+".png")
		result[name]=poses
		print("EXPORTED_MOD_HEAD ",name," ",count)
	FileAccess.open(BASE+"mod-study/head-poses.json",FileAccess.WRITE).store_string(JSON.stringify(result))
func export_all():
	var viewport=SubViewport.new()
	viewport.transparent_bg=true
	viewport.render_target_update_mode=SubViewport.UPDATE_ALWAYS
	root.add_child(viewport)
	await export_wall(viewport)
	if not OS.get_cmdline_user_args().has("--wall-only"):
		await export_head(viewport)
	quit()
