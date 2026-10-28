@tool
extends EditorScenePostImport

# Blender's FBX exporter always wraps the skeleton in an extra "Armature"
# node:  Scene / Armature / Skeleton3D / MeshInstance3D
# Other tools (including Mixamo's own FBX export) produce a flat structure:
# Scene / Skeleton3D / MeshInstance3D
# That extra node breaks relative NodePath lookups used by AnimationPlayer
# tracks that were authored against the flat structure, causing
# "couldn't resolve track" warnings for every bone at once.
#
# This script removes the "Armature" node and reparents its children
# (Skeleton3D, and anything else directly under it) up one level, folding
# Armature's own transform into them so nothing shifts position/rotation.
#
# USAGE:
#   1. Save this file somewhere in your Godot project (e.g. res://import_scripts/).
#   2. Select the imported .fbx file in the FileSystem dock.
#   3. In the Import dock, set "Import Script" to this file.
#   4. Click "Reimport".

func _post_import(scene: Node) -> Object:
	var armature := scene.find_child("Armature", true, false)
	if armature == null or not (armature is Node3D):
		return scene  # already flat, or nothing to fix

	var skeleton := armature.find_child("Skeleton3D", true, false)
	if skeleton == null:
		return scene

	var parent := armature.get_parent()
	if parent == null:
		return scene

	var armature_transform: Transform3D = armature.transform

	for child in armature.get_children():
		armature.remove_child(child)
		parent.add_child(child)
		child.owner = scene
		if child is Node3D:
			child.transform = armature_transform * child.transform

	parent.remove_child(armature)
	armature.free()

	return scene
