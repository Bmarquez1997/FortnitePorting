class MappingCollection:
    def __init__(self, node_name, textures=(), scalars=(), vectors=(), switches=(), component_masks=()):
        self.node_name = node_name
        self.textures = textures
        self.scalars = scalars
        self.vectors = vectors
        self.switches = switches
        self.component_masks = component_masks


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

# Start base groups (default, eye, toon, layer, etc)
default_mappings = MappingCollection(
    node_name="FPv4 Base Material",
    textures=[
        SlotMapping("Diffuse"),
        SlotMapping("D", "Diffuse"),
        SlotMapping("Base Color", "Diffuse"),
        SlotMapping("BaseColor", "Diffuse"),
        SlotMapping("Concrete", "Diffuse"),
        SlotMapping("Trunk_BaseColor", "Diffuse"),
        SlotMapping("Diffuse Top", "Diffuse"),
        SlotMapping("BaseColor_Trunk", "Diffuse"),
        SlotMapping("CliffTexture", "Diffuse"),
        SlotMapping("PM_Diffuse", "Diffuse"),
        SlotMapping("___Diffuse", "Diffuse"),
        SlotMapping("BaseColorTexture", "Diffuse"),
        SlotMapping("BaseColor Map", "Diffuse"),

        SlotMapping("Background Diffuse", alpha_slot="Background Diffuse Alpha"),
        SlotMapping("BG Diffuse Texture", "Background Diffuse", alpha_slot="Background Diffuse Alpha"),

        SlotMapping("M"),
        SlotMapping("Mask", "M"),
        SlotMapping("M Mask", "M"),

        SlotMapping("SpecularMasks"),
        SlotMapping("S", "SpecularMasks"),
        SlotMapping("SRM", "SpecularMasks"),
        SlotMapping("S Mask", "SpecularMasks"),
        SlotMapping("Specular Mask", "SpecularMasks"),
        SlotMapping("SpecularMask", "SpecularMasks"),
        SlotMapping("Concrete_SpecMask", "SpecularMasks"),
        SlotMapping("Trunk_Specular", "SpecularMasks"),
        SlotMapping("Specular Top", "SpecularMasks"),
        SlotMapping("SMR_Trunk", "SpecularMasks"),
        SlotMapping("Cliff Spec Texture", "SpecularMasks"),
        SlotMapping("PM_SpecularMasks", "SpecularMasks"),
        SlotMapping("__PBR Masks", "SpecularMasks"),
        SlotMapping("MetallicRoughnessTexture", "SpecularMasks"),
        SlotMapping("Bake Packed Maps", "SpecularMasks"),

        SlotMapping("Normals"),
        SlotMapping("N", "Normals"),
        SlotMapping("Normal", "Normals"),
        SlotMapping("NormalMap", "Normals"),
        SlotMapping("ConcreteTextureNormal", "Normals"),
        SlotMapping("Trunk_Normal", "Normals"),
        SlotMapping("Normals Top", "Normals"),
        SlotMapping("Normal_Trunk", "Normals"),
        SlotMapping("CliffNormal", "Normals"),
        SlotMapping("PM_Normals", "Normals"),
        SlotMapping("_Normal", "Normals"),
        SlotMapping("NormalTexture", "Normals"),
        SlotMapping("Normal Map", "Normals"),
        SlotMapping("Baked Normal", "Normals"),

        SlotMapping("Emissive", "Emission"),
        SlotMapping("EmissiveColor", "Emission"),
        SlotMapping("EmissiveTexture", "Emission"),
        SlotMapping("L1_Emissive", "Emission", coords="UV2"),
        SlotMapping("PM_Emissive", "Emission"),
        SlotMapping("Visor_Emissive", "Emission"),

        SlotMapping("MaskTexture"),
        SlotMapping("OpacityMask", "MaskTexture"),

        SlotMapping("FX Mask"),
        SlotMapping("SkinFX_Mask", "FX Mask"),
        SlotMapping("SkinFX Mask", "FX Mask"),
        SlotMapping("TechArtMask", "FX Mask"),
        SlotMapping("FxMask", "FX Mask"),
        SlotMapping("FX_Mask", "FX Mask"),
    ],
    scalars=[
        SlotMapping("RoughnessMin", "Roughness Min"),
        SlotMapping("SpecRoughnessMin", "Roughness Min"),
        SlotMapping("RawRoughnessMin", "Roughness Min"),
        SlotMapping("Rough Min", "Roughness Min"),
        SlotMapping("RoughnessMax", "Roughness Max"),
        SlotMapping("SpecRoughnessMax", "Roughness Max"),
        SlotMapping("RawRoughnessMax", "Roughness Max"),
        SlotMapping("Rough Max", "Roughness Max"),
        
        SlotMapping("emissive mult", "Emission Strength"),
        SlotMapping("DayMult", "Emission Strength"),
    ],
    vectors=[
        SlotMapping("TintColor", "Diffuse"),
        SlotMapping("BaseColorFactor", "Background Diffuse"),
        
        SlotMapping("EmissiveMultiplier", "Emission Multiplier"),
        SlotMapping("Emissive Multiplier", "Emission Multiplier"),
        SlotMapping("Emissive Color", "Emission Multiplier"),
        SlotMapping("EmissiveColor", "Emission Multiplier"),
        SlotMapping("Emissive", "Emission Multiplier"),
        SlotMapping("TN_Emissive Color", "Emission Multiplier"),
    ],
    switches=[
        SlotMapping("SwizzleRoughnessToGreen"),
    ]
)

base_layer_mappings = MappingCollection(
    node_name="FPv4 Base Layer",
    textures=[
        SlotMapping("Diffuse", alpha_slot="MaskTexture"),
        SlotMapping("SpecularMasks"),
        SlotMapping("Normals"),
        SlotMapping("EmissiveTexture"),
        SlotMapping("MaskTexture"),
        SlotMapping("Background Diffuse", alpha_slot="Background Diffuse Alpha"),
    ]
)

eye_mappings = MappingCollection(
    node_name="FPv4 3L Eyes",
    textures=[
        SlotMapping("Diffuse", closure=True),
        SlotMapping("Normal", closure=True),
        SlotMapping("SpecularMasks", closure=True),
        SlotMapping("SRM", "SpecularMasks", closure=True),
        SlotMapping("Emissive", closure=True),
    ],
    vectors=[
        SlotMapping("Eye Right UV Position"),
        SlotMapping("Eye Left UV Position"),
        SlotMapping("Eye Right UV Position (UV0)", "Eye Right UV Position"),
        SlotMapping("Eye Left UV Position (UV0)", "Eye Left UV Position"),

        SlotMapping("Eye Camera Light Vector"),
        SlotMapping("Eye UV Highlight Pos"),

        SlotMapping("EyeTintColor"),
    ],
    scalars=[
        SlotMapping("Eye Roughness Min"),
        SlotMapping("Eye Metallic Mult"),

        SlotMapping("Emissive Mult"),
        SlotMapping("Eye Texture AspectRatio"),
        SlotMapping("Eye Cornea Radius (UV)"),

        SlotMapping("Eye UV Highlight Size"),

        SlotMapping("Eye Iris Normal Flatten"),
        SlotMapping("EyeTintMask_Radius"),

        SlotMapping("Eye Cornea Mask Hardness"),
        SlotMapping("Eye Iris UV Radius"),
        SlotMapping("Eye Refraction Mix"),
        SlotMapping("Eye Refraction Mult"),
        SlotMapping("Eye Iris Depth Scale"),
        SlotMapping("Eye Cornea IOR"),
    ],
    switches=[
        SlotMapping("SwizzleRoughnessToGreen"),
        SlotMapping("Eye Use Sun Highlight"),
        SlotMapping("Eye Use UV Highlight"),
        SlotMapping("UseEyeColorTinting"),
    ]
)
# End base groups

# Start FX groups

# Dynamic layer numbers, replace # with number when mapping values (setup_params() extra parameter, loop until "Use i Layers" == false?)
layer_mappings = MappingCollection(
    node_name="FPv4 Layer",
    textures=[
        SlotMapping("Diffuse", alpha_slot="MaskTexture"),
        SlotMapping("SpecularMasks"),
        SlotMapping("Normals"),
        SlotMapping("EmissiveTexture"),
        SlotMapping("MaskTexture"),
        SlotMapping("Background Diffuse", alpha_slot="Background Diffuse Alpha"),
        
        SlotMapping("Diffuse_Texture_#", "Diffuse", alpha_slot="MaskTexture"),
        SlotMapping("SpecularMasks_#", "SpecularMasks"),
        SlotMapping("Normals_Texture_#", "Normals"),
        SlotMapping("Emissive_Texture_#", "EmissiveTexture"),
        SlotMapping("MaskTexture_#", "MaskTexture"),
        SlotMapping("Background Diffuse #", "Background Diffuse", alpha_slot="Background Diffuse Alpha"),
    ],
    switches=[
        SlotMapping("Is Transparent"), # "Is Transparent" = override_blend_mode is not EBlendMode.BLEND_Opaque
        SlotMapping("Use # Layers", "Use Layer")
    ]
)

skin_mappings = MappingCollection(
    node_name="FPv4 Skin",
    vectors=[
        SlotMapping("Skin Boost Color And Exponent", "Skin Color", alpha_slot="Skin Boost"),
        SlotMapping("SkinTint", "Skin Color", alpha_slot="Skin Boost"),
        SlotMapping("SkinColor", "Skin Color", alpha_slot="Skin Boost"),
    ]
)

cloth_fuzz_mappings = MappingCollection(
    node_name="FPv4 Cloth Fuzz",
    textures=[
        SlotMapping("ClothFuzz Texture", default="T_Fuzz_MASK", closure=True),
    ],
    scalars=[
        SlotMapping("Fuzz Tiling"),
        SlotMapping("ClothFuzzTiling", "Fuzz Tiling"),
        SlotMapping("Fuzz Exponent"),
        SlotMapping("ClothFuzzExponent", "Fuzz Exponent"),
        SlotMapping("Fuzz Fresnel Blend"),
        SlotMapping("Cloth Base Color Intensity"),
        SlotMapping("Cloth_BaseColorIntensity", "Cloth Base Color Intensity"),
        SlotMapping("Cloth Roughness"),
        SlotMapping("Cloth_Roughness", "Cloth Roughness"),
    ],
    vectors=[
        SlotMapping("Cloth Channel"),
        SlotMapping("ClothFuzzMaskChannel", "Cloth Channel"),

        SlotMapping("ClothFuzz Tint"),
        SlotMapping("Fuzz Tint", "ClothFuzz Tint"),
        SlotMapping("ClothFuzzTint", "ClothFuzz Tint"),
        SlotMapping("Cloth Fuzz Tint", "ClothFuzz Tint"),
    ],
    switches=[
        SlotMapping("Use Cloth Fuzz"),
        SlotMapping("UseClothFuzz", "Use Cloth Fuzz"),
    ],
    component_masks=[
        SlotMapping("Cloth Channel"),
        SlotMapping("ClothFuzzMaskChannel", "Cloth Channel"),
    ]
)

thinfilm_mappings = MappingCollection(
    node_name="FPv4 ThinFilm",
    textures=[
        SlotMapping("ThinFilm_Texture", default="T_ThinFilm_Spectrum_COLOR", closure=True),
        SlotMapping("ThinFilmTexture", "ThinFilm_Texture", default="T_ThinFilm_Spectrum_COLOR", closure=True),
        SlotMapping("Thin Film Texture", "ThinFilm_Texture", default="T_ThinFilm_Spectrum_COLOR", closure=True),
    ],
    scalars=[
        SlotMapping("ThinFilm_Intensity"),
        SlotMapping("ThinFilmIntensity", "ThinFilm_Intensity"),
        SlotMapping("RoughnessInfluence"),
        SlotMapping("ThinFilm_RoughnessScale", "RoughnessInfluence"),
        SlotMapping("ThinFilmRoughnessScale", "RoughnessInfluence"),
        SlotMapping("ThinFilm_Exponent"),
        SlotMapping("ThinFilmExponent", "ThinFilm_Exponent"),
        SlotMapping("ThinFilm_Offset"),
        SlotMapping("ThinFilmOffset", "ThinFilm_Offset"),
        SlotMapping("ThinFilm_Scale"),
        SlotMapping("ThinFilmScale", "ThinFilm_Scale"),
        SlotMapping("ThinFilm_Warp"),
        SlotMapping("ThinFilmWarp", "ThinFilm_Warp"),
    ],
    vectors=[
        SlotMapping("ThinFilmMaskChannel"),
        SlotMapping("ThinFilm_Channel", "ThinFilmMaskChannel"),
    ],
    switches=[
        SlotMapping("Use Thin Film"),
        SlotMapping("UseThinFilm", "Use Thin Film"),
    ],
    component_masks=[
        SlotMapping("ThinFilm_Channel"),
        SlotMapping("ThinFilmMaskChannel", "ThinFilm_Channel"),
    ]
)

silk_mappings = MappingCollection(
    node_name="FPv4 Silk",
    scalars=[
        SlotMapping("Silk Fresnel"),
        SlotMapping("SilkFresnelMin"),
        SlotMapping("SilkFresnelMax"),
        SlotMapping("SilkEdgeAniso"),
        SlotMapping("SilkBaseColorBrightness"),
    ],
    vectors=[
        SlotMapping("SilkMaskChannel"),
        SlotMapping("Silk_Channel", "SilkMaskChannel"),
        
        SlotMapping("SilkEdgeTint"),
    ],
    switches=[
        SlotMapping("Use Silk"),
        SlotMapping("UseSilk", "Use Silk"),
    ],
    component_masks=[
        SlotMapping("Use Silk"),
        SlotMapping("UseSilk", "Use Silk"),
    ],
)

metal_lut_mappings = MappingCollection(
    node_name="FPv4 MetalLUT",
    scalars=[
        SlotMapping("Metal LUT Curve"),
        SlotMapping("MetalLutIntensity"),
    ],
    vectors=[
        SlotMapping("MetalLUTMaskChannel"),
        SlotMapping("MetalLUT_Channel", "MetalLUTMaskChannel"),
        
        SlotMapping("LUTChannel"),
    ],
    switches=[
        SlotMapping("Use MetalLUT"),
        SlotMapping("UseMetalLUT", "Use MetalLUT"),
    ],
    component_masks=[
        SlotMapping("Use MetalLUT"),
        SlotMapping("UseMetalLUT", "Use MetalLUT"),
    ],
)

composite_mappings = MappingCollection(
    node_name="FPv4 Composite",
    textures=[
        SlotMapping("UV2Composite_AlphaTexture", closure=True),
        SlotMapping("UV2Composite_Diffuse", closure=True),
        SlotMapping("UV2Composite_Normals", closure=True),
        SlotMapping("UV2Composite_SRM", closure=True),
    ],
    scalars=[
        SlotMapping("UV2Composite_AlphaStrength"),
    ],
    vectors=[
        SlotMapping("UV2Composite_AlphaChannel"),
    ],
    switches=[
        SlotMapping("UV2Composite_AlphaTextureUseUV1"),
        SlotMapping("UseDiffuseAlphaChannel"),
        SlotMapping("UseUV2Diffuse"),
        SlotMapping("UseUV2Normals"),
        SlotMapping("UseUV2SRM"),
    ]
)

detail_mappings = MappingCollection(
    node_name="FPv4 Detail",
    textures=[
        SlotMapping("Detail Diffuse", closure=True),
        SlotMapping("Detail Normal", closure=True),
        SlotMapping("Detail SRM", closure=True),
    ],
    scalars=[
        SlotMapping("Detail Texture - Tiling"),
        SlotMapping("Detail Texture - UV Rotation"),
        SlotMapping("Detail Diffuse - Strength"),
        SlotMapping("Detail Normal - Flatten Normal"),
        SlotMapping("Detail Specular - Strength"),
        SlotMapping("Detail Roughness - Strength"),
        SlotMapping("Detail Metallic - Strength"),
    ],
    vectors=[
        SlotMapping("Detail Texture - Channel Mask"),
    ],
    switches=[
        SlotMapping("Detail Texture - Use UV2"),
        SlotMapping("Use Detail Diffuse?"),
        SlotMapping("Use Detail Normal?"),
        SlotMapping("Use Detail SRM?"),
    ]
)

flipbook_mappings = MappingCollection(
    node_name="FPv4 Flipbook",
    textures=[
        SlotMapping("Flipbook", closure=True),
    ],
    scalars=[
        SlotMapping("SubImages"),
        SlotMapping("SubUV_Frames", "SubImages"),
        SlotMapping("FB_MouthRowCount", "SubImages"),
        SlotMapping("FB_MouthColumnCount", "SubImages"),
        SlotMapping("Flipbook X"),
        SlotMapping("FB_MouthUVOffsetX", "Flipbook X"),
        SlotMapping("Flipbook Y"),
        SlotMapping("FB_MouthUVOffsetY", "Flipbook Y"),
        SlotMapping("Flipbook Scale"),
        SlotMapping("Use Second UV Channel", "Use Second UV"),

        SlotMapping("Affects Base Color"),
        SlotMapping("Multiply Flipbook Emissive"),
        
        SlotMapping("BumpOffset Intensity"),
        SlotMapping("Bump Height"),
    ],
    vectors=[
        SlotMapping("FlipbookTint"),
    ],
    switches=[
        SlotMapping("Use Flipbook"),
        SlotMapping("Use Sub UV texture", "Use Flipbook"),
        SlotMapping("FB_UseMouth", "Use Flipbook"),
        SlotMapping("Use Second UV"),
        SlotMapping("FB_MouthUseUV2", "Use Second UV"),
        SlotMapping("Affects Base Color"),
        SlotMapping("Multiply Flipbook Emissive"),
    ]
)

eyelash_mappings = MappingCollection(
    node_name="FPv4 Eyelash",
    textures=[
        SlotMapping("EyelashMask"),
    ],
    scalars=[
        SlotMapping("EyelashMetallic"),
        SlotMapping("EyelashRoughness"),
        SlotMapping("EyelashSpec"),
    ],
    vectors=[
        SlotMapping("EyelashColor"),
        SlotMapping("EyelashVertexColorMaskChannel"),
    ],
    switches=[
        SlotMapping("Use Eyelashes"),
        SlotMapping("UseEyelashes", "Use Eyelashes"),
    ],
)

gradient_mappings = MappingCollection(
    node_name="FPv4 Gradient",
    textures=[
        SlotMapping("Layer Mask", alpha_slot="Layer Mask Alpha"),
        SlotMapping("Layer1_Gradient", closure=True),
        SlotMapping("Layer2_Gradient", closure=True),
        SlotMapping("Layer3_Gradient", closure=True),
        SlotMapping("Layer4_Gradient", closure=True),
        SlotMapping("Layer5_Gradient", closure=True),
    ],
    switches=[
        SlotMapping("use Alpha Channel as mask", "Use Layer Mask Alpha"),
        SlotMapping("useGmapGradientLayers"),
        SlotMapping("useGmapGradientLayers", "Use Gmap Gradient Layers")
    ],
    component_masks=[
        SlotMapping("GmapSkinCustomization_Channel")
    ]
)

sequin_mappings = MappingCollection(
    node_name="FPv4 Sequin",
    textures=[
        SlotMapping("SequinOffset", default="T_SequinTile.png", closure=True),
        SlotMapping("SequinOffest", "SequinOffset", default="T_SequinTile.png", closure=True),
        SlotMapping("SequinRoughness", default="T_SequinTile_roughness.png", closure=True),
        SlotMapping("SequinNormal", default="T_SequinTile_N.png", closure=True),
        SlotMapping("StripeMask", closure=True),
        SlotMapping("SequinThinFilmColor", default="T_ThinFilm_Spectrum_COLOR", closure=True),
    ],
    scalars=[
        SlotMapping("SequinTile"),
        SlotMapping("SequinRotationAngle"),
        SlotMapping("SequinThinFilmUVExponent"),
        SlotMapping("SequinThinFilmUVScale"),
        SlotMapping("SequinThinFilmUVOffset"),
        SlotMapping("SequinThinFilmStrength_Basecolor"),
        SlotMapping("SequinThinFilmStrength_Emissive"),
        SlotMapping("SequinFresnel"),
        SlotMapping("SequinColorOffsetMin"),
        SlotMapping("SequinColorOffsetMax"),
        SlotMapping("SequinBrightness"),
        SlotMapping("SequinEmissiveIntensity"),
        SlotMapping("Sequin_MinRoughness"),
        SlotMapping("Sequin_MaxRoughness"),
        SlotMapping("SequinBaseRoughnessBlendAmount"),
        SlotMapping("SequinMetalness"),
        SlotMapping("UseBaseMetalness"),
        SlotMapping("SequinSparkleSpeed"),
        SlotMapping("SequinDiamondTile"),
        SlotMapping("SparkleBrightness"),
        SlotMapping("SequinNormalIntensity"),
        SlotMapping("StripedColorBlend"),
        SlotMapping("StripedNormalBlend"),
    ],
    vectors=[
        SlotMapping("SequinMaskChannel"),
        SlotMapping("SequinFalloffColor01"),
        SlotMapping("SequinFalloffColor02"),
        SlotMapping("SparkleColor"),
    ],
    switches=[
        SlotMapping("UseSequins"),
        SlotMapping("MFSequin_UseThinFilmOnSequins", "UseThinFilmOnSequins"),
        SlotMapping("MFSequin_UseBaseColor"),
        SlotMapping("UseBaseRoughness"),
        SlotMapping("MFSequin_UseBaseRoughness", "UseBaseRoughness"),
        SlotMapping("UseBaseNormal"),
        SlotMapping("MFSequin_UseBaseNormal", "UseBaseNormal"),
        SlotMapping("UseStripes"),
    ]
)

sequin_trim_mappings = MappingCollection(
    node_name="FPv4 Sequin",
    textures=[
        SlotMapping("SequinOffset_Main", "SequinOffset", default="T_SequinTile.png", closure=True),
        SlotMapping("SequinOffest_Main", "SequinOffset", default="T_SequinTile.png", closure=True),
        SlotMapping("SequinRoughness_Main", "SequinRoughness", default="T_SequinTile_roughness.png", closure=True),
        SlotMapping("SequinNormal_,Main", "SequinNormal", default="T_SequinTile_N.png", closure=True),
        SlotMapping("SequinThinFilm_Trim", "SequinThinFilmColor", default="T_ThinFilm_Spectrum_COLOR", closure=True),
    ],
    scalars=[
        SlotMapping("SequinRotationAngle_Main", "SequinRotationAngle"),
    ],
    vectors=[
        SlotMapping("Sequin_SecondaryChannel", "SequinMaskChannel"),
        SlotMapping("SequinFalloffColor01_Main", "SequinFalloffColor01"),
        SlotMapping("SequinFalloffColor02_Main", "SequinFalloffColor02"),
    ],
)

sequin_secondary_mappings = MappingCollection(
    node_name="FPv4 Sequin",
    textures=[
        SlotMapping("SequinOffset_Secondary", "SequinOffset", default="T_SequinTile.png", closure=True),
        SlotMapping("SequinOffest_Secondary", "SequinOffset", default="T_SequinTile.png", closure=True),
        SlotMapping("SequinRoughness_Secondary", "SequinRoughness", default="T_SequinTile_roughness.png", closure=True),
        SlotMapping("SequinNormal_,Secondary", "SequinNormal", default="T_SequinTile_N.png", closure=True),
        SlotMapping("SequinThinFilm_Secondary", "SequinThinFilmColor", default="T_ThinFilm_Spectrum_COLOR", closure=True),
    ],
    scalars=[
        SlotMapping("SequinRotationAngle_Secondary", "SequinRotationAngle"),
    ],
    vectors=[
        SlotMapping("Sequin_TrimChannel", "SequinMaskChannel"),
        SlotMapping("SequinFalloffColor01_Secondary", "SequinFalloffColor01"),
        SlotMapping("SequinFalloffColor02_Secondary", "SequinFalloffColor02"),
    ],
)

gmap_material_mappings = MappingCollection(
    node_name="FPv4 Gmap Material", # TODO: Rename to "Gmap" or "GMap Color"?
    textures=[
        SlotMapping("Diffuse"),
        SlotMapping("M"),
        SlotMapping("Color Mask 1"),
        SlotMapping("Color Mask 2"),
        SlotMapping("Color Mask 3"),
        SlotMapping("ColorVariety/Scratch/Dirt Mask"),
    ],
    vectors=[
        SlotMapping("Base Color: Color A"),
        SlotMapping("Base Color: Color B"),
        SlotMapping("Base Color: Color C"),
        SlotMapping("Color Mask 1-R: Color A"),
        SlotMapping("Color Mask 1-R: Color B"),
        SlotMapping("Color Mask 1-R: Color C"),
        SlotMapping("Color Mask 1-G: Color A"),
        SlotMapping("Color Mask 1-G: Color B"),
        SlotMapping("Color Mask 1-G: Color C"),
        SlotMapping("Color Mask 1-B: Color A"),
        SlotMapping("Color Mask 1-B: Color B"),
        SlotMapping("Color Mask 1-B: Color C"),
        SlotMapping("Color Mask 2-R: Color A"),
        SlotMapping("Color Mask 2-R: Color B"),
        SlotMapping("Color Mask 2-R: Color C"),
        SlotMapping("Color Mask 2-G: Color A"),
        SlotMapping("Color Mask 2-G: Color B"),
        SlotMapping("Color Mask 2-G: Color C"),
        SlotMapping("Color Mask 2-B: Color A"),
        SlotMapping("Color Mask 2-B: Color B"),
        SlotMapping("Color Mask 2-B: Color C"),
        SlotMapping("Color Mask 3-R: Color A"),
        SlotMapping("Color Mask 3-R: Color B"),
        SlotMapping("Color Mask 3-R: Color C"),
        SlotMapping("Color Mask 3-G: Color A"),
        SlotMapping("Color Mask 3-G: Color B"),
        SlotMapping("Color Mask 3-G: Color C"),
        SlotMapping("Color Mask 3-B: Color A"),
        SlotMapping("Color Mask 3-B: Color B"),
        SlotMapping("Color Mask 3-B: Color C"),
        SlotMapping("Color Variety Mask: Color A"),
        SlotMapping("Color Variety Mask: Color B"),
        SlotMapping("Color Variety Mask: Color C"),
        SlotMapping("Scratch Color A"),
        SlotMapping("Scratch Color B"),
        SlotMapping("Dirt Color A"),
        SlotMapping("Dirt Color B"),
    ],
    scalars=[
        SlotMapping("Color Variety Mask: Opacity"),
    ],
    switches=[
        SlotMapping("Use Diffuse as Base Color"),
        SlotMapping("Uses 2+ Color Masks", "Mask 2"),
        SlotMapping("Uses 3 Color Masks", "Mask 3"),
        SlotMapping("Uses ColorVariety/Scratch/Dirt Mask", "ColorVariety/Scratch/Dirt Mask")
    ]
)
# End FX groups


# Start tail groups (Hair, Fur, etc)
hair_mappings = MappingCollection(
    node_name="FPv4 Hair",
    textures=[
        SlotMapping("Hair Mask"),
        SlotMapping("AnisotropicTangentWeight", alpha_slot="AnisotropicTangentWeight Alpha"),
        SlotMapping("Strands Normal"),
    ],
    vectors=[
        SlotMapping("Hair_Color_Variation"),
        SlotMapping("Paint_Hair_Color_Darkness"),
        SlotMapping("Paint_Hair_Color_Brightness")
    ],
    scalars=[
        SlotMapping("AmbientOcclusion_Black"),
        SlotMapping("BaseColor_Fresnel_Brightness"),
        SlotMapping("Basecolor_Fresnel_Exponent"),
        SlotMapping("Fresnel_Brightness_Multiple"),

        SlotMapping("Paint_Hair_Contrast"),
        SlotMapping("Gmap_intensity"),

        SlotMapping("Specular_POWER"),
        SlotMapping("Specular _POWER", "Specular_POWER"),
        SlotMapping("Hair_Specular_MIN"),
        SlotMapping("Hair_Specular_MAX"),
        SlotMapping("Hair_Metallic"),
        SlotMapping("Roughness_power"),
        SlotMapping("Roughness Min"),
        SlotMapping("Roughness Max"),
        SlotMapping("Roughness_Noise_Tiling"),
        SlotMapping("Hair_Noise_Roughness_Min"),

        SlotMapping("Emissive_Brightness"),

        SlotMapping("AnisotropyMaxWeight"),
        SlotMapping("Hair_Anisotropy_Min"),
        SlotMapping("Hair_Anisotropy_Max"),
        SlotMapping("Scraggle"),

        SlotMapping("Hair_Mesh_Normal_Flatness"),
        SlotMapping("Paint_Hair_Normal_Flatness"),
    ],
    switches=[
        SlotMapping("UseAnisotropicShading"),
    ]
)

fur_mappings = MappingCollection(
    node_name="FPv4 Fur",
    textures=[
        SlotMapping("Strand Map"),
        SlotMapping("Hair Mask Height", "Strand Map"),
        SlotMapping("AnisotropicTangentWeight", alpha_slot="AnisotropicTangentWeight Alpha"),
        SlotMapping("Strands Normal"),
    ],
    vectors=[
        SlotMapping("Fur_Color_Darkness"),
        SlotMapping("Paint_Hair_Color_Darkness", "Fur_Color_Darkness"),
        SlotMapping("Fur_Color_Brightness"),
        SlotMapping("Paint_Hair_Color_Brightness", "Fur_Color_Brightness"),
    ],
    scalars=[
        SlotMapping("AmbientOcclusion_Black"),
        SlotMapping("BaseColor_Fresnel_Brightness"),
        SlotMapping("Basecolor_Fresnel_Exponent"),
        SlotMapping("Fresnel_Brightness_Multiple"),

        SlotMapping("Fur_Contrast"),
        SlotMapping("Paint_Hair_Contrast", "Fur_Contrast"),
        SlotMapping("Gmap_Intensity"),

        SlotMapping("Specular_POWER"),
        SlotMapping("Specular _POWER", "Specular_POWER"),
        SlotMapping("Fur_Specular_Min"),
        SlotMapping("Hair_Specular_Min", "Fur_Specular_Min"),
        SlotMapping("Fur_Specular_Max"),
        SlotMapping("Hair_Specular_Max", "Fur_Specular_Max"),
        SlotMapping("Fur_Metallic"),
        SlotMapping("Metallic_Min"),
        SlotMapping("Hair_Metallic_Min", "Metallic_Min"),
        SlotMapping("Metallic_Max"),
        SlotMapping("Hair_Metallic_Max", "Metallic_Max"),
        SlotMapping("Roughness_power"),
        SlotMapping("Roughness_Min"),
        SlotMapping("Roughness Min", "Roughness_Min"),
        SlotMapping("Roughness_Max"),
        SlotMapping("Roughness Max", "Roughness_Max"),
        SlotMapping("Roughness_Noise_Tiling"),
        SlotMapping("Scraggle_NoiseZ_Tiling", "Roughness_Noise_Tiling"),
        SlotMapping("Roughness_Noise_Min"),
        SlotMapping("Hair_Noise_Roughness_Min", "Roughness_Noise_Min"),

        SlotMapping("Emissive_Fur_Brightness"),

        SlotMapping("AnisotropyMaxWeight"),
        SlotMapping("Fur_Anisotropy_Min"),
        SlotMapping("Hair_Anisotropy0_Min", "Fur_Anisotropy_Min"),
        SlotMapping("Fur_Anisotropy_Max"),
        SlotMapping("Hair_Anisotropy0_Max", "Fur_Anisotropy_Max"),
        SlotMapping("Scraggle_Strength"),
        SlotMapping("Scraggle", "Scraggle_Strength"),

        SlotMapping("Mesh_Normal_Flatness"),
        SlotMapping("Hair_Mesh_Normal_Flatness", "Mesh_Normal_Flatness"),
        SlotMapping("Fur_Normal_Flatness"),
        SlotMapping("Paint_Hair_Normal_Flatness", "Fur_Normal_Flatness"),
    ],
    switches=[
        SlotMapping("UseAnisotropicShading")
    ]
)
# End tail groups