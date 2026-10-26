extends SceneTree
func _initialize():call_deferred("run")
func run():
	var s=ClassDB.instantiate("SpineSprite")
	s.skeleton_data_res=load("res://animations/characters/defect/defect_skel_data.tres");s.update_mode=2;root.add_child(s)
	s.get_animation_state().set_animation("idle_loop",true,0);s.update_skeleton(.36)
	for b in s.get_skeleton().get_bones():
		var n=b.get_data().get_bone_name()
		if "neck" in n or "head" in n:
			print("BONE ",n," parent=",b.get_parent().get_data().get_bone_name() if b.get_parent()!=null else "none"," world=",Vector2(b.get_world_x(),b.get_world_y())," basis=",[b.get_a(),b.get_b(),b.get_c(),b.get_d()])
	for slot in s.get_skeleton().get_slots():
		var n=slot.get_data().get_name()
		if n in ["neck","pipe 1","pipe 2","head"]:
			var a=slot.get_attachment()
			print("SLOT ",n," bone=",slot.get_bone().get_data().get_bone_name()," attachment=",a.get_class()," properties=",a.get_property_list())
	quit()
