# Mappings for each sub-group
# Add in order used in-game
# Keep float/vector/switch param nodes instead of setting values
# Switch RGB to CombineXYZ when any value is less than 0
# When alpha is used for vector, create separate "Param Alpha" value node
# Closure flag in mappings? Check socket for color vs closure?

import traceback
import bpy
from mathutils import Vector

#from ..material import *
from ..material.enums import *
from ..material.mappings_v4 import *
from ..material.mappings_registry import *
from ..material.names import *
from ..material.utils import *
from ..enums import *
from ..utils import *
from ...utils import *
from ...logger import Log

class MaterialImportContextNew:
    def import_material_new(self, material_slot, material_data, meta, as_material_data=False):

        if not as_material_data:
            temp_material = material_slot.material
            material_slot.link = 'OBJECT' if self.type in [EExportType.WORLD, EExportType.PREFAB] else 'DATA'
            material_slot.material = temp_material
    
        material_name = material_data.get("Name")
        material_hash = material_data.get("Hash")
        additional_hash = 0
   
        texture_data = meta.get("TextureData")
        if texture_data is not None:
            override_material_data = None
            for data in texture_data:
                additional_hash += data.get("Hash")
                if td_override_material := data.get("OverrideMaterial"):
                    override_material_data = td_override_material
                    break
            
            if override_material_data:
                material_data = override_material_data
                material_name = override_material_data.get("Name")
                material_hash = override_material_data.get("Hash")
        
        textures = material_data.get("Textures")
        scalars = material_data.get("Scalars")
        vectors = material_data.get("Vectors")
        switches = material_data.get("Switches")
        component_masks = material_data.get("ComponentMasks")
        
        if texture_data is not None:
            
            for data in texture_data:
                index = data.get("Index")

                texture_suffix = f"_Texture_{index + 1}" if index > 0 else ""
                spec_suffix = f"_{index + 1}" if index > 0 else ""

                if diffuse := data.get("Diffuse"):
                    replace_or_add_parameter_from_texture(textures, f"Diffuse{texture_suffix}", diffuse)
                if normal := data.get("Normal"):
                    replace_or_add_parameter_from_texture(textures, f"Normals{texture_suffix}", normal)
                if specular := data.get("Specular"):
                    replace_or_add_parameter_from_texture(textures, f"SpecularMasks{spec_suffix}", specular)
        
        override_parameters = where(self.override_parameters, lambda param: param.get("MaterialNameToAlter") in [material_name, "Global"])
        if override_parameters is not None:
            for parameters in override_parameters:
                additional_hash += parameters.get("Hash")
    
        if additional_hash != 0:
            material_hash += additional_hash
            material_name += f"_{hash_code(material_hash)}"
                
            
        if existing_material := first(bpy.data.materials, lambda mat: mat.get("Hash") == hash_code(material_hash)):
            if not as_material_data:
                material_slot.material = existing_material
                return

        # same name but different hash
        if (name_existing := first(bpy.data.materials, lambda mat: mat.name == material_name)) and name_existing.get("Hash") != material_hash:
            material_name += f"_{hash_code(material_hash)}"
            
        if not as_material_data and material_slot.material.name.casefold() != material_name.casefold():
            material_slot.material = bpy.data.materials.new(material_name)

        if not as_material_data:
            material_slot.material["Hash"] = hash_code(material_hash)
            material_slot.material["OriginalName"] = material_data.get("Name")

        material = bpy.data.materials.new(material_name) if as_material_data else material_slot.material
        material.use_nodes = True
        material.surface_render_method = "DITHERED"

        nodes = material.node_tree.nodes
        nodes.clear()
        links = material.node_tree.links
        links.clear()
        
        textures = material_data.get("Textures")
        scalars = material_data.get("Scalars")
        vectors = material_data.get("Vectors")
        switches = material_data.get("Switches")
        component_masks = material_data.get("ComponentMasks")
        
        if override_parameters is not None:
            for parameters in override_parameters:
                for texture in parameters.get("Textures"):
                    replace_or_add_parameter(textures, texture)
    
                for scalar in parameters.get("Scalars"):
                    replace_or_add_parameter(scalars, scalar)
    
                for vector in parameters.get("Vectors"):
                    replace_or_add_parameter(vectors, vector)

        output_node = nodes.new(type="ShaderNodeOutputMaterial")
        output_node.location = (200, 0)

        shader_node = nodes.new(type="ShaderNodeGroup")
        shader_node.node_tree = bpy.data.node_groups.get("FPv4 Material Build")

        def replace_shader_node(name):
            nonlocal shader_node
            nodes.remove(shader_node)
            shader_node = nodes.new(type="ShaderNodeGroup")
            shader_node.node_tree = bpy.data.node_groups.get(name)
            
        # for cleaner code sometimes bc stuff gets repetitive
        def set_param(name, value, override_shader=None):
            
            nonlocal shader_node
            target_node = override_shader or shader_node
            target_node.inputs[name].default_value = value

        def get_node(target_node, slot):
            node_links = target_node.inputs[slot].links
            if node_links is None or len(node_links) == 0:
                return None
            
            return node_links[0].from_node

        def get_first_node(target_node, slots):
            for slot in slots:
                node_links = target_node.inputs[slot].links
                if len(node_links) > 0:
                    return node_links[0].from_node
                
            return None

        unused_parameter_height = 0

        # parameter handlers
        def texture_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                path = data.get("Texture").get("Path")
                texture_name = path.split(".")[1]

                node = nodes.new(type="ShaderNodeTexImage")
                node.image = self.import_image(path)
                node.image.alpha_mode = 'CHANNEL_PACKED'
                node.image.colorspace_settings.name = "sRGB" if data.get("Texture").get("sRGB") else "Non-Color"
                node.interpolation = "Smart"
                node.hide = True

                mappings = first(target_mappings.textures, lambda x: x.name.casefold() == name.casefold())
                if mappings is None or texture_name in texture_ignore_names:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node.label = name
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 50
                    else:
                        nodes.remove(node)
                    return

                x, y = get_socket_pos(target_node, target_node.inputs.find(mappings.slot))
                if mappings.closure:
                    setup_closure(node, x, y, mappings.slot, target_node, nodes, links)
                else:
                    node.location = x - 300, y
                    links.new(node.outputs[0], target_node.inputs[mappings.slot])

                if mappings.alpha_slot:
                    links.new(node.outputs[1], target_node.inputs[mappings.alpha_slot])
                if mappings.switch_slot:  # Set based on lambda?
                    target_node.inputs[mappings.switch_slot].default_value = 1 if value else 0
                if mappings.coords != "UV0":
                    uv = nodes.new(type="ShaderNodeUVMap")
                    uv.location = node.location.x - 250, node.location.y
                    uv.uv_map = mappings.coords
                    links.new(uv.outputs[0], node.inputs[0])
            except KeyError:
                nodes.remove(node)
                pass
            except Exception:
                traceback.print_exc()

        def scalar_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                value = data.get("Value")

                mappings = first(target_mappings.scalars, lambda x: x.name.casefold() == name.casefold())
                if mappings is None:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node = nodes.new(type="ShaderNodeValue")
                        node.outputs[0].default_value = value
                        node.label = name
                        node.width = 250
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 100
                    return

                value = mappings.value_func(value) if mappings.value_func else value
                target_socket = target_node.inputs[mappings.slot]

                match target_socket.type:
                    case "INT":
                        target_socket.default_value = int(value)
                    case "BOOL":
                        target_socket.default_value = int(value) == 1
                    case _:
                        target_socket.default_value = value
                    
                if mappings.switch_slot:
                    target_node.inputs[mappings.switch_slot].default_value = 1 if value else 0
            except KeyError as e:
                pass
            except Exception:
                traceback.print_exc()

        def vector_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                value = data.get("Value")

                mappings = first(target_mappings.vectors, lambda x: x.name.casefold() == name.casefold())
                if mappings is None:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node = nodes.new(type="ShaderNodeRGB")
                        node.outputs[0].default_value = (value["R"], value["G"], value["B"], value["A"])
                        node.label = name
                        node.width = 250
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 200
                    return

                value = mappings.value_func(value) if mappings.value_func else value
                if isinstance(target_node.inputs[mappings.slot], bpy.types.NodeSocketColor):
                    target_node.inputs[mappings.slot].default_value = (value["R"], value["G"], value["B"], 1.0)
                else:
                    target_node.inputs[mappings.slot].default_value = (value["R"], value["G"], value["B"])
                if mappings.alpha_slot:
                    target_node.inputs[mappings.alpha_slot].default_value = value["A"]
                if mappings.switch_slot:
                    target_node.inputs[mappings.switch_slot].default_value = 1 if value else 0
            except KeyError:
                pass
            except Exception:
                traceback.print_exc()

        def component_mask_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                value = data.get("Value")

                mappings = first(target_mappings.component_masks, lambda x: x.name.casefold() == name.casefold())
                if mappings is None:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node = nodes.new(type="ShaderNodeRGB")
                        node.outputs[0].default_value = (value["R"], value["G"], value["B"], value["A"])
                        node.label = name
                        node.width = 250
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 200
                    return

                value = mappings.value_func(value) if mappings.value_func else value
                target_node.inputs[mappings.slot].default_value = (value["R"], value["G"], value["B"], value["A"])
            except KeyError:
                pass
            except Exception:
                traceback.print_exc()

        def switch_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                value = data.get("Value")

                mappings = first(target_mappings.switches, lambda x: x.name.casefold() == name.casefold())
                if mappings is None:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node = nodes.new("ShaderNodeGroup")
                        node.node_tree = bpy.data.node_groups.get("FPv4 Switch")
                        node.inputs[0].default_value = 1 if value else 0
                        node.label = name
                        node.width = 250
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 125
                    return

                value = mappings.value_func(value) if mappings.value_func else value
                target_socket = target_node.inputs[mappings.slot]
                match target_socket.type:
                    case "INT":
                        target_socket.default_value = 1 if value else 0
                    case "BOOLEAN":
                        target_socket.default_value = value
            except KeyError:
                pass
            except Exception:
                traceback.print_exc()

        # TODO: Add all params, and connect param nodes to groups instead of just values
        def setup_params(mappings, target_node, add_unused_params=False):
            for texture in textures:
                texture_param(texture, mappings, target_node, add_unused_params) # TODO: need to account for default textures

            for scalar in scalars:
                scalar_param(scalar, mappings, target_node, add_unused_params)

            for vector in vectors:
                vector_param(vector, mappings, target_node, add_unused_params) # TODO: use Combine XYZ node instead of Color when value has negatives?

            for component_mask in component_masks:
                component_mask_param(component_mask, mappings, target_node, add_unused_params)

            for switch in switches:
                switch_param(switch, mappings, target_node, add_unused_params)

        def move_texture_node(target_node, slot_name, source_node=None):
            source = shader_node if source_node is None else source_node
            if texture_node := get_node(source, slot_name):
                x, y = get_socket_pos(target_node, target_node.inputs.find(slot_name))
                texture_node.location = x - 300, y
                links.new(texture_node.outputs[0], target_node.inputs[slot_name])
                
            links.new(target_node.outputs[slot_name], source.inputs[slot_name])

        def connect_or_add_default_texture(target_node, target_slot, texture_name, sRGB=True, pre_node_link=None):
            texture_node = get_node(target_node, target_slot)
            if texture_node is None:
                texture_node = nodes.new(type="ShaderNodeTexImage")
                texture_node.image = bpy.data.images.get(texture_name)
                texture_node.image.alpha_mode = 'CHANNEL_PACKED'
                texture_node.image.colorspace_settings.name = "sRGB" if sRGB else "Non-Color"
                texture_node.interpolation = "Smart"
                texture_node.hide = True
    
                x, y = get_socket_pos(target_node, target_node.inputs.find(target_slot))
                texture_node.location = x - 300, y
                links.new(texture_node.outputs[0], target_node.inputs[target_slot])
                
            if pre_node_link is not None:
                links.new(pre_node_link, texture_node.inputs[0])

        def connect_texture_uvs(target_node, slot_name, uv_output):
            if texture_node := get_node(target_node, slot_name):
                links.new(uv_output, texture_node.inputs[0])

        # TODO: Handle individually or as one group?
        all_mappings = find_all_matching_mappings(material_data)
        base_mappings = find_all_matching_mappings(material_data, type=ENodeType.NT_Base)
        base_fx_mappings = find_all_matching_mappings(material_data, type=ENodeType.NT_Core_FX)
        advanced_fx_mappings = find_all_matching_mappings(material_data, type=ENodeType.NT_Advanced_FX)

        # TODO
        # Separate handling for layered
        # Ensure only one base node
        # Set SwizzleRoughness

        # TODO: Move to mappings to allow for other build nodes?
        set_param("AO", self.options.get("AmbientOcclusion"))
        set_param("Cavity", self.options.get("Cavity"))
        set_param("Subsurface Scale", self.options.get("Subsurface"))

        node_position = -200
        previous_node = shader_node

        if len(all_mappings) > 0:
            for mapping in all_mappings:
                Log.info(f"Adding node: {mapping.node_name}")
                new_node = nodes.new(type="ShaderNodeGroup")
                new_node.node_tree = bpy.data.node_groups.get(mapping.node_name)
                new_node.location = (node_position, 0)
                setup_params(mapping, new_node, False)
                links.new(new_node.outputs[0], previous_node.inputs[0])
                previous_node = new_node
                node_position -= 500 # TODO: add node spacing in mappings (500 for no closures, 1k for closures?)


        # Temp to add all params for debugging
        setup_params(MappingCollection(), shader_node, True)

        links.new(shader_node.outputs[0], output_node.inputs[0])

        # post parameter handling
        
        if material_name in vertex_crunch_names or get_param(scalars, "HT_CrunchVerts") == 1 or any(toon_outline_names, lambda x: x in material_name):
            self.full_vertex_crunch_materials.append(material)
            return
        
        if get_param(switches, "Use Vertex Colors for Mask"):
            elements = {}
            for scalar in scalars:
                name = scalar.get("Name")
                if "Hide Element" not in name:
                    continue

                elements[name] = scalar.get("Value")
            
            self.partial_vertex_crunch_materials[material] = elements

                    
    def import_material_standalone_new(self, data):
        is_object_import = EMaterialImportMethod.OBJECT == EMaterialImportMethod(self.options.get("MaterialImportMethod"))
        materials = data.get("Materials")

        if materials is None:
            return
        
        if is_object_import:
            self.collection = create_or_get_collection("Materials") if self.options.get("ImportIntoCollection") else bpy.context.scene.collection
            
        for material in materials:
            name = material.get("Name")
            Log.info(f"Importing Material: {name}")
            if is_object_import:
                bpy.ops.mesh.primitive_cube_add()
                mat_mesh = bpy.context.active_object
                mat_mesh.name = name
                mat_mesh.data.materials.append(bpy.data.materials.new(name))
                self.import_material_new(mat_mesh.material_slots[material.get("Slot")], material, {})
            else:
                self.import_material_new(None, material, {}, True)