extends SceneTree
func _initialize():call_deferred("run")
func run():
	var sprite=ClassDB.instantiate("SpineSprite")
	sprite.skeleton_data_res=load("res://animations/characters/defect/defect_skel_data.tres")
	sprite.update_mode=2
	root.add_child(sprite)
	sprite.get_animation_state().set_animation("idle_loop",true,0)
	sprite.update_skeleton(0)
	for b in sprite.get_skeleton().get_bones():
		var name=b.get_data().get_bone_name()
		if name.contains("arm") or name.contains("hand") or name.contains("shoulder") or name=="orb" or name=="head":
			print(name," ",b.get_world_x(),",",b.get_world_y()," parent=",b.get_parent().get_data().get_bone_name() if b.get_parent()!=null else "none")
	for s in sprite.get_skeleton().get_slots():print("SLOT ",s.get_data().get_name()," BONE ",s.get_bone().get_data().get_bone_name())
	quit()
