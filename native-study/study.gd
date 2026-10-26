extends SceneTree
func _initialize():
	print("STUDY_START")
	print("MOUNT ", ProjectSettings.load_resource_pack("E:/SteamLibrary/steamapps/common/Slay the Spire 2/SlayTheSpire2.pck", false))
	print("SPINE_CLASS ", ClassDB.class_exists("SpineSprite"))
	var data = load("res://animations/characters/defect/defect_skel_data.tres")
	print("DATA ",data)
	if data:
		var sprite = ClassDB.instantiate("SpineSprite")
		sprite.skeleton_data_res = data
		root.add_child(sprite)
		for bone in sprite.get_skeleton().get_bones():
			print("BONE ",bone.get_data().get_bone_name())
		for a in sprite.get_skeleton().get_data().get_animations():
			print("ANIMATION ", a.get_name(), " ", a.get_duration())
	quit()
