from .base_context import BaseImportContext
from .mesh_context import MeshImportContext
from .texture_context import TextureImportContext
from .material_context import MaterialImportContext
from .material_context_v4 import MaterialImportContextNew
from .sound_context import SoundImportContext
from .anim_context import AnimImportContext
from .font_context import FontImportContext
from .pose_context import PoseImportContext
from .tasty_context import TastyImportContext

class ImportContext(BaseImportContext, MeshImportContext, MaterialImportContext, MaterialImportContextNew, 
                   AnimImportContext, TextureImportContext, SoundImportContext, 
                   FontImportContext, PoseImportContext, TastyImportContext):
    
    def __init__(self, meta_data):
        BaseImportContext.__init__(self, meta_data)

__all__ = [
    'ImportContext',
    'BaseImportContext',
    'MeshImportContext',
    'TextureImportContext',
    'MaterialImportContext',
    'MaterialImportContextNew',
    'SoundImportContext',
    'AnimImportContext',
    'FontImportContext',
    'PoseImportContext',
    'TastyImportContext'
]