"""Non-destructive background Blender build: baked surface studies and a 6 m viewport.
Run: blender -b --factory-startup -P Art/Blender/Scripts/vv_build_concourse_samples.py
Outputs are separate from the established environment kit.
"""
import math
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'Assets/_Project/Art/Environment/ConcourseSamples'
OUT.mkdir(parents=True, exist_ok=True)
sc = bpy.context.scene
sc.render.engine = 'CYCLES'
sc.cycles.samples = 4

def material(name, color, metal=0.5):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    b.inputs['Base Color'].default_value = (*color, 1)
    b.inputs['Metallic'].default_value = metal
    b.inputs['Roughness'].default_value = .6
    return m

frame = material('CS_Frame', (.045, .055, .075))
trim = material('CS_Amber', (.8, .32, .035))
glass = material('CS_Glass', (.14, .32, .42))
gb = next(n for n in glass.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
gb.inputs['Transmission Weight'].default_value = .92
gb.inputs['Roughness'].default_value = .08

def box(name, loc, size, mat, parts):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(mat)
    bevel = o.modifiers.new('Soft manufactured edges', 'BEVEL')
    bevel.width = .012
    bevel.segments = 2
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    parts.append(o)
    return o

def baked_material(name, color):
    m = material(name, color)
    nt = m.node_tree
    b = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
    output = next(n for n in nt.nodes if n.type == 'OUTPUT_MATERIAL')
    uv = nt.nodes.new('ShaderNodeTexCoord')
    noise = nt.nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = 38
    noise.inputs['Detail'].default_value = 5
    nt.links.new(uv.outputs['UV'], noise.inputs['Vector'])
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = .2
    ramp.color_ramp.elements[0].color = (*(v*.35 for v in color), 1)
    ramp.color_ramp.elements[1].position = .8
    ramp.color_ramp.elements[1].color = (*(min(1,v*1.6+.025) for v in color), 1)
    nt.links.new(noise.outputs['Fac'], ramp.inputs[0])
    emit = nt.nodes.new('ShaderNodeEmission')
    nt.links.new(ramp.outputs['Color'], emit.inputs['Color'])
    nt.links.new(emit.outputs[0], output.inputs['Surface'])
    image = bpy.data.images.new(name+'_BaseColor', width=512, height=512)
    image.filepath_raw = str(OUT / (name+'_BaseColor.png'))
    tex = nt.nodes.new('ShaderNodeTexImage')
    tex.image = image
    nt.nodes.active = tex
    bpy.ops.mesh.primitive_plane_add(size=2)
    plane = bpy.context.object
    plane.data.materials.append(m)
    bpy.ops.object.bake(type='EMIT', margin=8)
    image.file_format = 'PNG'
    image.save()
    nt.links.new(b.outputs[0], output.inputs['Surface'])
    nt.links.new(tex.outputs['Color'], b.inputs['Base Color'])
    # Keep the bake plane in a separate source-only collection, excluded from exports.
    plane.hide_render = True
    plane.hide_viewport = True
    return m

purple = baked_material('CS_CeramicPurple', (.22, .15, .32))
steel = baked_material('CS_BrushedSteel', (.22, .27, .31))
deck = baked_material('CS_RubberDeck', (.075, .09, .115))

def export(name, parts, offset):
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    o = bpy.context.object
    o.name = name
    sc.cursor.location = (0,0,0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')), use_selection=True,
        object_types={'MESH'}, axis_forward='-Z', axis_up='Y', add_leaf_bones=False,
        bake_anim=False, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, mesh_smooth_type='FACE', path_mode='STRIP')
    o.location.x += offset
    print('CONCOURSE_SAMPLE', name, len(o.data.polygons))

for idx, (name, mat, ribbed) in enumerate([
    ('CS_Wall_Ceramic',purple,False),('CS_Wall_ServiceRibs',steel,True)]):
    parts=[]
    box('back', (0,.12,1.5),(2,.24,3),frame,parts)
    box('panel',(0,-.025,1.5),(1.78,.08,2.72),mat,parts)
    for x in [-.91,.91]: box('rail',(x,-.08,1.5),(.1,.16,3),frame,parts)
    if ribbed:
        for z in [.4,.8,1.2,1.6,2,2.4]: box('rib',(0,-.10,z),(1.65,.12,.06),frame,parts)
    else:
        for x in [-.48,.48]: box('inlay',(x,-.075,1.5),(.025,.025,2.35),trim,parts)
    export(name,parts,idx*3)

for idx, (name, mat, grooves) in enumerate([
    ('CS_Floor_Rubber',deck,True),('CS_Floor_Steel',steel,False),
    ('CS_Ceiling_Baffles',steel,True),('CS_Ceiling_Cassette',purple,False)]):
    parts=[]
    ceiling='Ceiling' in name
    sign=1 if ceiling else -1
    box('deck',(0,0,sign*.1),(2,2,.2),frame,parts)
    box('surface',(0,0,sign*.025),(1.82,1.82,.05),mat,parts)
    if grooves:
        for x in [-.72,-.48,-.24,0,.24,.48,.72]:
            box('strip',(x,0,sign*.003),(.045,1.65,.006),frame,parts)
    else:
        for y in [-.65,.65]: box('trim',(0,y,sign*.003),(1.65,.025,.006),trim,parts)
    export(name,parts,(idx+2)*3)

parts=[]
for x in [-2.86,0,2.86]: box('mullion',(x,0,1.5),(.16,.32,3),frame,parts)
for z in [.14,2.86]: box('frame',(0,0,z),(6,.32,.28),frame,parts)
for x in [-1.43,1.43]: box('glass',(x,.03,1.5),(2.68,.045,2.42),glass,parts)
for x in [-2.7,2.7]: box('edge',(x,-.18,1.5),(.035,.03,2.35),trim,parts)
export('CS_Viewport_Panoramic6m',parts,24)

source=ROOT/'Art/Blender/Environment/ConcourseSamples'
source.mkdir(parents=True, exist_ok=True)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(source/'ConcourseSamples.blend'))
print('CONCOURSE_SAMPLES_COMPLETE',OUT)
