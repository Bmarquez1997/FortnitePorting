from .enums import ENodeType
from ...utils import *

class MappingCollection:
    node_name = ""
    type = None
    order = 99
    node_spacing = 500
    textures = ()
    scalars = ()
    colors = ()
    vectors = ()
    switches = ()
    component_masks = ()
    build_node = "FPv4 Material Build"
    surface_render_method = "DITHERED"
    show_transparent_back = True
    

    @classmethod
    def meets_criteria(self, material_data):
        # Placeholder for criteria checking logic
        # TODO: Default = any_slots_match()?
        def matches(name, slots):
            return any(slots, lambda x: x.name.casefold() == name.casefold())

        match_tex = any(material_data.get("Textures"), lambda tex: matches(tex.get("Name"), self.textures))
        match_scal = any(material_data.get("Scalars"), lambda scal: matches(scal.get("Name"), self.scalars))
        match_col = any(material_data.get("Vectors"), lambda vec: matches(vec.get("Name"), self.colors))
        match_vec = any(material_data.get("Vectors"), lambda vec: matches(vec.get("Name"), self.vectors))
        match_switch = any(material_data.get("Switches"), lambda switch: matches(switch.get("Name"), self.switches))
        match_comp = any(material_data.get("ComponentMasks"), lambda comp: matches(comp.get("Name"), self.component_masks))


        return match_tex or match_scal or match_col or match_vec or match_switch or match_comp
    
    @classmethod
    def meets_criteria_dynamic(self, material_data, index):
        # Placeholder for dynamic criteria checking logic
        return False

    # TODO: get_slots_dynamic(slot_collection, index)?
    # TODO: get_default_textures()?


class SlotMapping:
    def __init__(self, name, slot=None, alpha_slot=None, switch_slot=None, value_func=None, coords="UV0", default=None, closure=False):
        self.name = name
        self.slot = name if slot is None else slot
        self.alpha_slot = alpha_slot
        self.switch_slot = switch_slot
        self.value_func = value_func
        self.coords = coords
        self.default = default
        self.closure = closure


class DefaultTexture:
    def __init__(self, name, sRGB=True):
        self.name = name
        self.sRGB = sRGB


class MappingRegistry:
    def __init__(self):
        self.mappings = []


    def register(self, mapping):
        self.mappings.append(mapping)
        return mapping


    def get_all_mappings(self):
        return self.mappings


    def get_mappings_for_type(self, node_type):
        mappings = [m for m in self.get_all_mappings() if m.type == node_type]
        return sorted(mappings, key=lambda mapping: mapping.order)
    

    def find_all_matching_mappings(self, material_data, type):
        matches = []
        all_mappings = self.get_mappings_for_type(type) if type is not None else self.get_all_mappings()
        for mappings in all_mappings:
            if mappings.meets_criteria(material_data):
                matches.append(mappings)
        return sorted(matches, key=lambda mapping: (mapping.type, mapping.order), reverse=True)


# Global registry instance
registry = MappingRegistry()


def get_all_mappings():
    return registry.get_all_mappings()


def get_mappings_for_type(node_type):
    return registry.get_mappings_for_type(node_type)


def find_all_matching_mappings(material_data, type=None):
    return registry.find_all_matching_mappings(material_data, type=type)