"""Açık Blender'da (MCP) kiti 'Medieval' sahnesine kurar: exec(open(...).read()) ile çalıştırılır.
ONLY = ["cottage", ...] tanımlıysa sadece onlar. Kullanıcının diğer sahnelerine dokunmaz."""
import importlib
import sys

import bpy

KIT = r"C:/Users/nurba/desktop/zorse/tools/medieval"
if KIT not in sys.path:
    sys.path.insert(0, KIT)
import build_medieval as core
core = importlib.reload(core)

sc = bpy.data.scenes.get("Medieval") or bpy.data.scenes.new("Medieval")
bpy.context.window.scene = sc
sc.view_settings.view_transform = "Standard"  # renkler Unity'deki gibi (AgX soldurur)
for o in list(sc.objects):
    bpy.data.objects.remove(o, do_unlink=True)
for m in list(bpy.data.meshes):
    if m.users == 0:
        bpy.data.meshes.remove(m)

img = core.write_palette()
mat = bpy.data.materials.get("MedievalPalette") or core.material(img)
only = globals().get("ONLY")
x = 0.0
for key, fn in core.all_builders(reload=True).items():
    if only and key not in only:
        continue
    o = fn()
    for c in o.users_collection:
        c.objects.unlink(o)
    sc.collection.objects.link(o)
    o.data.materials.append(mat)
    o.location.x = x + o.dimensions.x / 2
    x += o.dimensions.x + 1.5
print("kuruldu:", [o.name for o in sc.objects])
