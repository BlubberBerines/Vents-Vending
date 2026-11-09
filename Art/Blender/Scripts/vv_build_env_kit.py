"""Environment kit: modular scenery architecture in the station art style (ref: user's kit sheets, "WALL PANELS /
FLOOR PANELS / CEILING PANELS (MODULAR)"). Run headless so the live Blender session is untouched:

  blender -b --factory-startup -P Art/Blender/Scripts/vv_build_env_kit.py [-- <shot> ...]

Every module is rebuilt and exported to Assets/_Project/Art/Environment/Kit/<Kind>/<name>.fbx (Unity remaps the VV_MAT_*
slots to the M_Env_* toon library, see EnvironmentKitSetup.cs); kit renders go to Art/Blender/Environment/Kit/review.
Env vars: VV_SAMPLES (Cycles samples, default 96), VV_EXPORT_ONLY=1 (export only, no renders).

Module rules (2 m grid, metres, Z up):
- Walls: 2.0 wide (X -1..1) x 3.0 tall (Z 0..3) x 0.30 deep. Room side faces -Y (+Z after Unity import). Pivot = bottom
  centre of the room-facing frame surface (y = 0); details may protrude up to ~17 cm into the room.
- Floors: 2.0 x 2.0, walkable top at z = 0 (raised detail < 1 cm), 0.2 thick below. Pivot = top centre.
- Ceilings: 2.0 x 2.0, room-facing underside at z = 0 (placed at 3 m), body above; fixtures hang at most 20 cm.
- Chunky angular forms with softened bevels; wear is shader-driven in Blender (Cycles Bevel chips + AO grime).
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

REPO = r"F:\Vents & Vending"
SCRIPTS = os.path.join(REPO, "Art", "Blender", "Scripts")
LIB = os.path.join(REPO, "Art", "Blender", "Library", "VV_Materials.blend")
OUT_DIR = os.path.join(REPO, "Art", "Blender", "Environment", "Kit")
UNITY_DIR = os.path.join(REPO, "Assets", "_Project", "Art", "Environment", "Kit")

ns = {"__name__": "vv"}
for f in ("vv_lib.py", "vv_materials.py"):
    p = os.path.join(SCRIPTS, f)
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), ns)
Builder, mk_box, mk_cyl, mk_sphere = ns["Builder"], ns["mk_box"], ns["mk_cyl"], ns["mk_sphere"]
add_wear_layer, _clone, _spots, _sock = ns["add_wear_layer"], ns["_clone"], ns["_spots"], ns["_sock"]

FD, PU, OR, HZ, ST, AM, IM = ("VV_MAT_FrameDark", "VV_MAT_StationPurple", "VV_MAT_TrimOrange", "VV_MAT_HazardStripe",
                              "VV_MAT_StencilLilac", "VV_MAT_EmissiveAmber", "VV_MAT_IndustrialMetal")
GL, CY, FM, VO, RD = ("VV_MAT_WindowGlow", "VV_MAT_EmissiveCyanEnv", "VV_MAT_FloorMetal", "VV_MAT_Void",
                      "VV_MAT_EmissiveRed")


def lin(hex_rgb):
    """sRGB hex -> linear RGB tuple (Principled Base Color is linear)."""
    h = hex_rgb.lstrip("#")
    return tuple(((int(h[i:i + 2], 16) / 255.0) ** 2.2) for i in (0, 2, 4))


# ---------------------------------------------------------------- materials

def materials():
    with bpy.data.libraries.load(LIB, link=False) as (src, dst):
        dst.materials = [m for m in src.materials if m in (
            "VV_MAT_WornPaint", "VV_MAT_DarkMetal", "VV_MAT_OrangePaint", "VV_MAT_IndustrialMetal", "VV_MAT_EmissiveRed")]

    def base(m, colour, rough=None):
        b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        bi = _sock(b, "Base Color")
        if bi.is_linked and bi.links[0].from_node.type == "RGB":
            bi.links[0].from_node.outputs[0].default_value = (*colour, 1)
        else:                       # library paints drive colour procedurally: replace with the env colour
            rgb = m.node_tree.nodes.new("ShaderNodeRGB")
            rgb.location = (b.location.x - 600, b.location.y + 200)
            rgb.outputs[0].default_value = (*colour, 1)
            m.node_tree.links.new(rgb.outputs[0], bi)
        if rough is not None:
            _sock(b, "Roughness").default_value = rough
        m.diffuse_color = (*colour, 1)
        return b

    bare = (0.5, 0.5, 0.53)

    purple = _clone("VV_MAT_WornPaint", "VV_MAT_StationPurple")          # primary structural paint
    base(purple, lin("#574468"), 0.62)
    _spots(purple, lin("#3b2e4a"), scale=2.2, size=0.6, strength=0.55)   # broad patchy fading
    add_wear_layer(purple, bare, chip_metal=0.9, chip_amount=0.58, grime=0.8, chip_scale=14.0, edge_radius=0.018)

    frame = _clone("VV_MAT_DarkMetal", "VV_MAT_FrameDark")                # frames, rails, housings
    base(frame, lin("#3b3247"), 0.5)
    add_wear_layer(frame, (0.42, 0.40, 0.45), chip_metal=1.0, chip_amount=0.45, grime=0.6, chip_scale=16.0,
                   edge_radius=0.016)

    orange = _clone("VV_MAT_OrangePaint", "VV_MAT_TrimOrange")            # brackets, caps, hazard accents
    base(orange, lin("#e08a2a"), 0.55)
    add_wear_layer(orange, (0.30, 0.27, 0.30), chip_metal=0.8, chip_amount=0.8, grime=0.6, chip_scale=12.0,
                   edge_radius=0.02)

    hazard = _clone("VV_MAT_OrangePaint", "VV_MAT_HazardStripe")          # 45-degree stripes, object space
    b = base(hazard, lin("#e08a2a"), 0.6)
    nt = hazard.node_tree
    tc = nt.nodes.new("ShaderNodeTexCoord"); tc.location = (-1500, 300)
    sep = nt.nodes.new("ShaderNodeSeparateXYZ"); sep.location = (-1300, 300)
    nt.links.new(_sock(tc, "Object", True), sep.inputs[0])
    add = nt.nodes.new("ShaderNodeMath"); add.operation = "ADD"; add.location = (-1100, 300)
    nt.links.new(sep.outputs[0], add.inputs[0]); nt.links.new(sep.outputs[2], add.inputs[1])
    k = nt.nodes.new("ShaderNodeMath"); k.operation = "MULTIPLY"; k.inputs[1].default_value = 1.0 / 0.26; k.location = (-950, 300)
    nt.links.new(add.outputs[0], k.inputs[0])
    fr = nt.nodes.new("ShaderNodeMath"); fr.operation = "FRACT"; fr.location = (-800, 300)
    nt.links.new(k.outputs[0], fr.inputs[0])
    gt = nt.nodes.new("ShaderNodeMath"); gt.operation = "GREATER_THAN"; gt.inputs[1].default_value = 0.5; gt.location = (-650, 300)
    nt.links.new(fr.outputs[0], gt.inputs[0])
    mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = "RGBA"; mix.location = (-450, 300)
    nt.links.new(gt.outputs[0], _sock(mix, "Factor_Float"))
    _sock(mix, "A_Color").default_value = (*lin("#e08a2a"), 1)
    _sock(mix, "B_Color").default_value = (*lin("#2a2430"), 1)
    nt.links.new(_sock(mix, "Result_Color", True), _sock(b, "Base Color"))
    add_wear_layer(hazard, (0.30, 0.27, 0.30), chip_metal=0.8, chip_amount=0.6, grime=0.6, chip_scale=18.0)

    stencil = _clone("VV_MAT_WornPaint", "VV_MAT_StencilLilac")           # painted emblems / markings
    base(stencil, lin("#9c89bb"), 0.75)
    add_wear_layer(stencil, lin("#6b5a86"), chip_amount=0.5, grime=0.4, chip_scale=30.0, edge_radius=0.006)

    floor = _clone("VV_MAT_DarkMetal", "VV_MAT_FloorMetal")               # walkable deck plate
    base(floor, lin("#4a4253"), 0.55)
    _spots(floor, lin("#2c2733"), scale=1.6, size=0.6, strength=0.5)
    add_wear_layer(floor, (0.46, 0.44, 0.48), chip_metal=1.0, chip_amount=0.5, grime=0.75, chip_scale=14.0,
                   edge_radius=0.016)

    def emissive(name, colour, glow, strength):
        m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        m.use_nodes = True
        e = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        _sock(e, "Base Color").default_value = (*lin(colour), 1)
        _sock(e, "Emission Color").default_value = (*lin(glow), 1)
        _sock(e, "Emission Strength").default_value = strength
        _sock(e, "Roughness").default_value = 0.2
        m.diffuse_color = (*lin(colour), 1)

    emissive("VV_MAT_EmissiveAmber", "#ffb35a", "#ffa040", 12.0)          # lamps, light panels
    emissive("VV_MAT_WindowGlow", "#2a1840", "#a24bff", 1.6)               # window: violet glow from beyond
    emissive("VV_MAT_EmissiveCyanEnv", "#7ff3ff", "#38e1ff", 5.0)          # screens, indicators
    emissive("VV_MAT_Void", "#030204", "#000000", 0.0)                     # pits / slots / screen bezels
    glass = bpy.data.materials.get("VV_MAT_GlassTint") or bpy.data.materials.new("VV_MAT_GlassTint")   # viewport glass
    glass.use_nodes = True
    gb = next(n for n in glass.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    _sock(gb, "Base Color").default_value = (*lin("#6a5a88"), 1)
    _sock(gb, "Roughness").default_value = 0.08
    _sock(gb, "Transmission Weight").default_value = 0.9
    _sock(gb, "Alpha").default_value = 0.25
    glass.diffuse_color = (*lin("#6a5a88"), 0.25)
    _sock(next(n for n in bpy.data.materials["VV_MAT_Void"].node_tree.nodes if n.type == "BSDF_PRINCIPLED"),
          "Roughness").default_value = 0.9


# ---------------------------------------------------------------- geometry helpers

def mk_l_plate(cx, cz, sx, sz, leg, width, front_y, depth, bevel):
    """Solid L-shaped corner plate in the XZ plane: outer corner at (cx, cz), legs run inward; depth runs to +Y."""
    def f(bm):
        pts = [(0, 0), (leg, 0), (leg, width), (width, width), (width, leg), (0, leg)]
        ring = [bm.verts.new((cx - sx * u, front_y, cz - sz * v)) for u, v in pts]
        face = bm.faces.new(ring)
        ext = bmesh.ops.extrude_face_region(bm, geom=[face])
        moved = [e for e in ext["geom"] if isinstance(e, bmesh.types.BMVert)]
        bmesh.ops.translate(bm, verts=moved, vec=(0, depth, 0))
        bm.normal_update()
        edges = list({e for v in ring + moved for e in v.link_edges})
        bmesh.ops.bevel(bm, geom=edges, offset=bevel, segments=2, affect="EDGES", profile=0.5, clamp_overlap=True)
    return f


class Wall:
    """Wall module builder: shared frame + per-variant field. Boxes are placed by their room-facing side (front_y)."""

    def __init__(self):
        self.b = Builder()

    def box(self, mat, size, front_y, x, z, bevel, segs=2, rot=(0, 0, 0)):
        self.b.part(mat, mk_box(size, bevel=bevel, segs=segs), loc=(x, front_y + size[1] / 2, z), rot=rot)

    def bolt(self, x, y, z, r=0.018):
        self.b.part(IM, mk_cyl(r, 0.022, 6), loc=(x, y, z), rot=(90, 0, 0))

    def frame(self, field=True):
        for sx in (-1, 1):
            self.box(FD, (0.16, 0.30, 3.0), 0.0, sx * 0.92, 1.5, 0.035)
        self.box(FD, (1.70, 0.26, 0.16), 0.03, 0, 2.92, 0.03)               # top rail
        self.box(FD, (1.70, 0.26, 0.16), 0.03, 0, 0.08, 0.03)               # bottom rail / skirting
        self.box(FD, (1.70, 0.24, 0.09), 0.05, 0, 0.86, 0.025)              # mid rail over the kick band
        if field:
            self.box(PU, (1.70, 0.20, 2.02), 0.085, 0, 1.87, 0.02)          # main purple field
        for sx in (-1, 1):
            for sz, zc in ((1, 3.0), (-1, 0.0)):                            # orange L brackets + bolts
                self.b.part(OR, mk_l_plate(sx * 1.0, zc, sx, sz, 0.40, 0.13, -0.035, 0.05, 0.014))
                for bx, bz in ((sx * 0.70, zc - sz * 0.065), (sx * 0.935, zc - sz * 0.33)):
                    self.bolt(bx, -0.045, bz)
            self.box(OR, (0.20, 0.05, 0.16), -0.03, sx * 0.92, 0.86, 0.014)  # mid-rail caps
            for z in (1.25, 1.75, 2.25):                                    # rivets
                self.b.part(IM, mk_sphere(0.014, 8, 5), loc=(sx * 0.92, -0.002, z), scale=(1, 0.6, 1))

    def kick_hazard(self):
        self.box(HZ, (1.70, 0.20, 0.70), 0.09, 0, 0.47, 0.015)

    def kick_vents(self):
        self.box(PU, (1.70, 0.20, 0.70), 0.09, 0, 0.47, 0.015)
        for z in (0.32, 0.47, 0.62):                                        # three louvre slots
            self.box(VO, (1.10, 0.03, 0.07), 0.075, 0, z, 0.008, segs=1)
            self.box(FD, (1.14, 0.03, 0.02), 0.07, 0, z + 0.045, 0.006, segs=1)

    def inner_plate(self):
        self.box(PU, (1.22, 0.04, 1.42), 0.055, 0, 1.86, 0.03)
        for bx in (-0.53, 0.53):
            for bz in (1.23, 2.49):
                self.bolt(bx, 0.048, bz, 0.016)

    def lamp(self, z=2.58):
        self.box(FD, (0.52, 0.10, 0.15), -0.05, 0, z, 0.03)
        self.box(AM, (0.40, 0.03, 0.07), -0.065, 0, z, 0.012, segs=1)
        for sx in (-1, 1):
            self.box(OR, (0.06, 0.11, 0.17), -0.058, sx * 0.27, z, 0.012, segs=1)

    def emblem(self, ex, ez, y=0.049, r_out=0.20, w=0.045):
        for k in range(4):
            a = math.radians(45 + 90 * k)
            cx, cz = ex + math.cos(a) * (r_out - w) * 0.72, ez + math.sin(a) * (r_out - w) * 0.72
            self.b.part(ST, mk_box((r_out * 1.05, 0.012, w), bevel=0.004, segs=1), loc=(cx, y, cz), rot=(0, 45 - 90 * k, 0))
        self.b.part(ST, mk_box((w * 1.5, 0.012, w * 1.5), bevel=0.004, segs=1), loc=(ex, y, ez), rot=(0, 45, 0))


def wall_standard():
    w = Wall(); w.frame(); w.kick_hazard(); w.inner_plate()
    w.emblem(-0.30, 1.72)
    w.box(ST, (0.46, 0.012, 0.055), 0.043, 0.20, 1.80, 0.004, segs=1)       # text bars (real text = decal later)
    w.box(ST, (0.32, 0.012, 0.055), 0.043, 0.13, 1.67, 0.004, segs=1)
    w.lamp()
    return w.b.mesh("ENV_Wall_Standard_A")


def wall_window():
    """Inset window with a violet glow from beyond, chunky surround, mullion cross, orange sill; louvred kick band."""
    w = Wall(); w.frame(); w.kick_vents()
    wz, ww, wh, t = 1.95, 1.16, 1.12, 0.12
    for sx in (-1, 1):                                                      # surround sides
        w.box(FD, (t, 0.12, wh + 2 * t), -0.03, sx * (ww / 2 + t / 2), wz, 0.025)
    for sz in (-1, 1):                                                      # surround top / bottom
        w.box(FD, (ww, 0.12, t), -0.03, 0, wz + sz * (wh / 2 + t / 2), 0.025)
    w.box(GL, (ww, 0.02, wh), 0.06, 0, wz, 0.0, segs=1)                     # recessed glowing pane
    w.box(FD, (0.07, 0.06, wh), 0.0, 0, wz, 0.012, segs=1)                  # mullion cross
    w.box(FD, (ww, 0.06, 0.07), 0.0, 0, wz + 0.12, 0.012, segs=1)
    w.box(OR, (ww + 2 * t + 0.08, 0.16, 0.07), -0.08, 0, wz - wh / 2 - t - 0.035, 0.015)   # orange sill
    for sx in (-1, 1):
        for sz in (-1, 1):
            w.bolt(sx * (ww / 2 + t / 2), -0.04, wz + sz * (wh / 2 + t / 2), 0.016)
    return w.b.mesh("ENV_Wall_Window_A")


def wall_braced():
    """Structural X-brace across the field with an orange hub plate and foot gussets; hazard kick band."""
    w = Wall(); w.frame(); w.kick_hazard()
    x0, x1, z0, z1 = -0.80, 0.80, 0.97, 2.78
    ang = math.degrees(math.atan2(z1 - z0, x1 - x0))
    length = math.hypot(x1 - x0, z1 - z0)
    cz = (z0 + z1) / 2
    w.box(FD, (length, 0.07, 0.15), 0.01, 0, cz, 0.025, rot=(0, -ang, 0))
    w.box(FD, (length, 0.07, 0.15), 0.005, 0, cz, 0.025, rot=(0, ang, 0))
    w.box(OR, (0.30, 0.05, 0.30), -0.035, 0, cz, 0.02, rot=(0, 45, 0))     # hub plate
    for k in range(4):
        a = math.radians(90 * k)
        w.bolt(math.cos(a) * 0.09, -0.045, cz + math.sin(a) * 0.09, 0.016)
    for sx in (-1, 1):                                                      # gussets where the brace meets the frame
        for zz in (z0 + 0.07, z1 - 0.07):
            w.box(OR, (0.18, 0.04, 0.12), 0.0, sx * 0.72, zz, 0.012, segs=1)
    return w.b.mesh("ENV_Wall_Braced_A")


def wall_utility():
    """Utility panel: control box with two cyan screens + buttons, a conduit up into the top rail; vent kick band."""
    w = Wall(); w.frame(); w.kick_vents(); w.inner_plate()
    bx, bz = -0.12, 1.62
    w.box(FD, (0.62, 0.16, 0.56), -0.12, bx, bz, 0.03)                      # control box housing
    w.box(OR, (0.66, 0.17, 0.06), -0.13, bx, bz + 0.30, 0.015)              # orange hood
    for sx in (-1, 1):
        w.box(VO, (0.20, 0.02, 0.16), -0.13, bx + sx * 0.14, bz + 0.09, 0.006, segs=1)    # screen bezels
        w.box(CY, (0.17, 0.02, 0.13), -0.135, bx + sx * 0.14, bz + 0.09, 0.004, segs=1)   # cyan screens
    for i, mat in enumerate((OR, AM, RD)):                                  # buttons
        w.b.part(mat, mk_cyl(0.025, 0.03, 10), loc=(bx - 0.14 + i * 0.14, -0.135, bz - 0.15), rot=(90, 0, 0))
    w.box(IM, (0.10, 0.03, 0.10), -0.13, bx + 0.22, bz - 0.15, 0.01, segs=1)  # breaker
    w.box(OR, (0.03, 0.05, 0.14), -0.17, bx + 0.22, bz - 0.12, 0.008, segs=1)
    ztop = bz + 0.33
    w.b.part(FD, mk_cyl(0.045, 2.84 - ztop, 10), loc=(0.42, -0.06, (ztop + 2.84) / 2))   # conduit up to the rail
    w.b.part(FD, mk_cyl(0.045, 0.30, 10), loc=(0.29, -0.06, ztop), rot=(0, 90, 0))       # elbow run into the box
    for z in (2.15, 2.6):
        w.box(OR, (0.14, 0.03, 0.05), -0.11, 0.42, z, 0.008, segs=1)          # conduit clamps
    w.emblem(0.42, 1.42, r_out=0.13, w=0.03)
    return w.b.mesh("ENV_Wall_Utility_A")


def wall_viewport():
    """Big see-through viewport (depth: looks out over shafts, machine halls, docked ships). Opening 1.3 x 1.4 m at
    z 1.25-2.65 with a chunky surround, two bars and an orange sill; the tinted glass is a separate child (Unity uses a
    transparent material on it)."""
    w = Wall(); w.frame(field=False); w.kick_hazard()
    oz0, oz1, oh = 1.25, 2.65, 0.65                                         # opening z range, half width
    t = 0.14
    # purple field in pieces around the opening (field spans x +-0.85, z 0.91-2.88, front at y 0.085)
    side_w = 0.85 - (oh + t)
    for sx in (-1, 1):
        w.box(PU, (side_w, 0.20, 1.97), 0.085, sx * (oh + t + side_w / 2), 1.895, 0.02)
    w.box(PU, (2 * (oh + t), 0.20, (oz0 - t) - 0.91), 0.085, 0, (0.91 + oz0 - t) / 2, 0.015)
    w.box(PU, (2 * (oh + t), 0.20, 2.88 - (oz1 + t)), 0.085, 0, (oz1 + t + 2.88) / 2, 0.015)
    for sx in (-1, 1):
        w.box(FD, (t, 0.14, oz1 - oz0 + 2 * t), -0.04, sx * (oh + t / 2), (oz0 + oz1) / 2, 0.03)          # surround sides
    for zz in (oz0 - t / 2, oz1 + t / 2):
        w.box(FD, (oh * 2, 0.14, t), -0.04, 0, zz, 0.03)                                                  # top / bottom
    for sx in (-0.22, 0.22):
        w.box(FD, (0.05, 0.08, oz1 - oz0), 0.0, sx, (oz0 + oz1) / 2, 0.012, segs=1)                       # bars
    w.box(OR, (oh * 2 + 2 * t + 0.1, 0.20, 0.07), -0.10, 0, oz0 - t - 0.035, 0.015)                       # orange sill
    for sx in (-1, 1):
        for zz in (oz0 - t / 2, oz1 + t / 2):
            w.bolt(sx * (oh + t / 2), -0.05, zz, 0.016)
    frame = w.b.mesh("ENV_Wall_Viewport_A")
    gl = Builder()
    gl.part("VV_MAT_GlassTint", mk_box((oh * 2, 0.02, oz1 - oz0), bevel=0.0, segs=1), loc=(0, 0.06, (oz0 + oz1) / 2))
    return frame, {"Glass": gl.mesh("ENV_Wall_Viewport_A_Glass")}


def _floor_frame(b):
    """2x2 floor: dark base slab, raised border frame flush at z = 0, orange corner plates with bolts."""
    b.part(FD, mk_box((2.0, 2.0, 0.17), bevel=0.02, segs=1), loc=(0, 0, -0.03 - 0.085))
    for s in (-1, 1):
        b.part(FD, mk_box((2.0, 0.10, 0.06), bevel=0.02, segs=2), loc=(0, s * 0.95, -0.03))
        b.part(FD, mk_box((0.10, 1.80, 0.06), bevel=0.02, segs=2), loc=(s * 0.95, 0, -0.03))
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.part(OR, mk_box((0.20, 0.20, 0.012), bevel=0.006, segs=1), loc=(sx * 0.9, sy * 0.9, -0.004))
            b.part(IM, mk_cyl(0.016, 0.01, 6), loc=(sx * 0.9, sy * 0.9, 0.002))


def floor_solid():
    b = Builder(); _floor_frame(b)
    b.part(FM, mk_box((1.80, 1.80, 0.05), bevel=0.012, segs=1), loc=(0, 0, -0.025))   # deck plate, top flush at 0
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.part(IM, mk_cyl(0.014, 0.006, 6), loc=(sx * 0.78, sy * 0.78, 0.001))
    b.part(FD, mk_box((1.80, 0.02, 0.006), bevel=0.0, segs=1), loc=(0, 0, -0.001))     # centre seam
    return b.mesh("ENV_Floor_Solid_A")


def floor_grate():
    b = Builder(); _floor_frame(b)
    b.part(VO, mk_box((1.80, 1.80, 0.02), bevel=0.0, segs=1), loc=(0, 0, -0.16))       # dark pit floor
    for k in range(-7, 8):                                                   # grate bars, tops flush at 0
        b.part(FD, mk_box((1.80, 0.04, 0.05), bevel=0.008, segs=1), loc=(0, k * 0.115, -0.025))
    for x in (-0.6, 0.0, 0.6):                                               # cross supports
        b.part(FD, mk_box((0.05, 1.80, 0.08), bevel=0.008, segs=1), loc=(x, 0, -0.07))
    for s in (-1, 1):                                                        # hazard edge strips
        b.part(HZ, mk_box((1.40, 0.06, 0.012), bevel=0.004, segs=1), loc=(0, s * 0.95, -0.004))
    return b.mesh("ENV_Floor_Grate_A")


def _ceiling_frame(b):
    """2x2 ceiling: room-facing underside at z = 0, body above; chunky border, recessed purple panel, orange corners."""
    b.part(FD, mk_box((2.0, 2.0, 0.22), bevel=0.02, segs=1), loc=(0, 0, 0.19))
    for s in (-1, 1):
        b.part(FD, mk_box((2.0, 0.12, 0.10), bevel=0.02, segs=2), loc=(0, s * 0.94, 0.05))
        b.part(FD, mk_box((0.12, 1.76, 0.10), bevel=0.02, segs=2), loc=(s * 0.94, 0, 0.05))
    b.part(PU, mk_box((1.76, 1.76, 0.04), bevel=0.012, segs=1), loc=(0, 0, 0.08))
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.part(OR, mk_box((0.22, 0.22, 0.03), bevel=0.008, segs=1), loc=(sx * 0.89, sy * 0.89, -0.012))


def ceiling_standard():
    b = Builder(); _ceiling_frame(b)
    b.part(FD, mk_cyl(0.07, 2.0, 12), loc=(0, 0.45, -0.12), rot=(0, 90, 0))   # pipe run along X
    b.part(FD, mk_cyl(0.045, 2.0, 10), loc=(0, 0.62, -0.09), rot=(0, 90, 0))
    for x in (-0.55, 0.55):                                                   # hangers + orange clamps
        b.part(FD, mk_box((0.05, 0.36, 0.05), bevel=0.01, segs=1), loc=(x, 0.53, -0.02))
        b.part(OR, mk_cyl(0.085, 0.07, 12), loc=(x, 0.45, -0.12), rot=(0, 90, 0))
        b.part(OR, mk_cyl(0.06, 0.06, 10), loc=(x + 0.1, 0.62, -0.09), rot=(0, 90, 0))
    for x in (-0.5, 0.0, 0.5):                                                # vent slots in the panel
        b.part(VO, mk_box((0.30, 0.50, 0.02), bevel=0.0, segs=1), loc=(x, -0.35, 0.058))
    return b.mesh("ENV_Ceiling_Standard_A")


def ceiling_light():
    b = Builder(); _ceiling_frame(b)
    b.part(FD, mk_box((1.10, 0.60, 0.10), bevel=0.025, segs=2), loc=(0, 0, 0.01))     # light housing
    b.part(AM, mk_box((0.94, 0.44, 0.03), bevel=0.01, segs=1), loc=(0, 0, -0.045))    # glowing panel
    for x in (-0.25, 0.0, 0.25):                                               # diffuser bars
        b.part(FD, mk_box((0.03, 0.46, 0.02), bevel=0.0, segs=1), loc=(x, 0, -0.065))
    for sx in (-1, 1):
        b.part(OR, mk_box((0.06, 0.64, 0.12), bevel=0.012, segs=1), loc=(sx * 0.58, 0, 0.0))
    return b.mesh("ENV_Ceiling_Light_A")


# ---------------------------------------------------------------- layer connectors (station layers spec §6, §8)

def mk_prism_yz(points, x0, x1):
    """Closed prism: a polygon in the YZ plane (list of (y, z), counter-clockwise seen from +X) extruded from x0 to x1."""
    def f(bm):
        a = [bm.verts.new((x0, y, z)) for y, z in points]
        c = [bm.verts.new((x1, y, z)) for y, z in points]
        bm.faces.new(list(reversed(a)))
        bm.faces.new(c)
        n = len(points)
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], a[j], c[j], c[i]))
    return f


DOOR_HALF, DOOR_H = 0.65, 2.4          # opening x +-0.65, z 0..2.4
LEAF_W, LEAF_H = 0.335, 2.42


def wall_door_sliding():
    """Full-size powered sliding door (den door, staff doors, bulkheads). Telescoping leaves: two panels per side
    stack into the 0.35 m jamb pocket (Leaf_*1 outer slides 0.335, Leaf_*2 inner slides 0.655)."""
    w = Wall()
    for sx in (-1, 1):
        w.box(FD, (0.16, 0.30, 3.0), 0.0, sx * 0.92, 1.5, 0.035)                      # stiles
        w.box(PU, (0.20, 0.20, 2.84), 0.085, sx * 0.745, 1.42, 0.02)                   # jamb fields (pocket)
        for sz, zc in ((1, 3.0), (-1, 0.0)):
            w.b.part(OR, mk_l_plate(sx * 1.0, zc, sx, sz, 0.40, 0.13, -0.035, 0.05, 0.014))
            for bx, bz in ((sx * 0.70, zc - sz * 0.065), (sx * 0.935, zc - sz * 0.33)):
                w.bolt(bx, -0.045, bz)
        w.box(FD, (0.12, 0.10, DOOR_H + 0.06), -0.04, sx * (DOOR_HALF + 0.06), DOOR_H / 2, 0.025)   # surround sides
        w.box(HZ, (0.05, 0.02, 1.0), -0.05, sx * (DOOR_HALF + 0.06), 0.55, 0.004, segs=1)      # hazard on the jambs
    w.box(FD, (1.70, 0.26, 0.16), 0.03, 0, 2.92, 0.03)                                 # top rail
    w.box(PU, (1.48, 0.20, 0.44), 0.085, 0, 2.62, 0.02)                                # header field
    w.box(FD, (DOOR_HALF * 2 + 0.24, 0.10, 0.12), -0.04, 0, DOOR_H + 0.06, 0.025)      # surround top
    w.box(FD, (0.30, 0.06, 0.10), -0.07, 0, 2.66, 0.015)                               # status lamp housing
    w.box(CY, (0.22, 0.02, 0.05), -0.08, 0, 2.66, 0.006, segs=1)                       # cyan status lamp
    w.box(VO, (DOOR_HALF * 2, 0.30, 0.02), 0.0, 0, 0.01, 0.0, segs=1)                  # threshold track
    frame = w.b.mesh("ENV_Wall_DoorSliding_A")

    leaves = {}
    for side, sx in (("L", -1), ("R", 1)):
        for k, (cx, y0) in enumerate(((DOOR_HALF - LEAF_W / 2, 0.10), (LEAF_W / 2, 0.165)), start=1):
            lb = Builder()
            x = sx * cx
            lb.part(FD, mk_box((LEAF_W, 0.06, LEAF_H), bevel=0.015, segs=1), loc=(x, y0 + 0.03, LEAF_H / 2))
            lb.part(PU, mk_box((LEAF_W - 0.08, 0.012, 1.5), bevel=0.008, segs=1), loc=(x, y0 - 0.002, 1.45))
            lb.part(HZ, mk_box((LEAF_W - 0.04, 0.012, 0.28), bevel=0.004, segs=1), loc=(x, y0 - 0.002, 0.32))
            if k == 2:                                                                # meeting edge: orange strip + half diamond
                lb.part(OR, mk_box((0.035, 0.016, LEAF_H - 0.1), bevel=0.006, segs=1), loc=(sx * 0.02, y0 - 0.004, LEAF_H / 2))
                lb.part(ST, mk_box((0.16, 0.012, 0.16), bevel=0.004, segs=1), loc=(sx * 0.07, y0 - 0.008, 1.5), rot=(0, 45, 0))
            leaves[f"Leaf_{side}{k}"] = lb.mesh(f"ENV_Wall_DoorSliding_A_Leaf_{side}{k}")
    return frame, leaves


VENT_HALF, VENT_SILL, VENT_TOP = 0.45, 1.8, 2.8   # opening = full 1.0 m duct height (flush with the riser roof)


def wall_vent_mouth():
    """Head-height vent mouth into the crawl layer: opening 0.9 x 0.85 with its sill at 1.8 m (climb from a crate),
    hazard-striped lip proud of the face, a short stub to the wall back (the riser duct attaches there), grille hinged
    open above it."""
    w = Wall()
    for sx in (-1, 1):
        w.box(FD, (0.16, 0.30, 3.0), 0.0, sx * 0.92, 1.5, 0.035)
        for sz, zc in ((1, 3.0), (-1, 0.0)):
            w.b.part(OR, mk_l_plate(sx * 1.0, zc, sx, sz, 0.40, 0.13, -0.035, 0.05, 0.014))
            for bx, bz in ((sx * 0.70, zc - sz * 0.065), (sx * 0.935, zc - sz * 0.33)):
                w.bolt(bx, -0.045, bz)
        w.box(OR, (0.20, 0.05, 0.16), -0.03, sx * 0.92, 0.86, 0.014)
        w.box(PU, (0.40, 0.20, 0.85), 0.085, sx * (VENT_HALF + 0.20), (VENT_SILL + VENT_TOP) / 2, 0.015)  # field beside
    w.box(FD, (1.70, 0.26, 0.16), 0.03, 0, 2.92, 0.03)
    w.box(FD, (1.70, 0.26, 0.16), 0.03, 0, 0.08, 0.03)
    w.box(FD, (1.70, 0.24, 0.09), 0.05, 0, 0.86, 0.025)
    w.kick_hazard()
    w.box(PU, (1.70, 0.20, VENT_SILL - 0.91), 0.085, 0, (0.91 + VENT_SILL) / 2, 0.02)        # field below the mouth
    w.box(PU, (1.70, 0.20, 2.84 - VENT_TOP), 0.085, 0, (VENT_TOP + 2.84) / 2, 0.015)          # field above
    # mouth: surround, hazard lip (its top is the climb ledge at exactly 1.8), stub to the wall back
    for sx in (-1, 1):
        w.box(FD, (0.10, 0.40, VENT_TOP - VENT_SILL + 0.10), -0.06, sx * (VENT_HALF + 0.05), (VENT_SILL + VENT_TOP) / 2, 0.02)
    w.box(FD, (VENT_HALF * 2 + 0.20, 0.40, 0.10), -0.06, 0, VENT_TOP + 0.05, 0.02)
    w.box(HZ, (VENT_HALF * 2 + 0.20, 0.42, 0.08), -0.08, 0, VENT_SILL - 0.04, 0.01, segs=1)
    frame = w.b.mesh("ENV_Wall_VentMouth_A")

    g = Builder()                                   # grille hinged at the top edge, swung up and out 70 degrees
    hinge_y, hinge_z, ang = -0.07, VENT_TOP + 0.02, math.radians(70)

    def at(u):                                      # u = distance down the grille from the hinge when closed
        return (hinge_y - math.sin(ang) * u, hinge_z - math.cos(ang) * u)

    cy_, cz_ = at(0.42)
    g.part(FD, mk_box((VENT_HALF * 2 - 0.02, 0.03, 0.84), bevel=0.01, segs=1), loc=(0, cy_, cz_), rot=(-70, 0, 0))
    for k in range(-3, 4):
        yy, zz = at(0.42)
        g.part(VO, mk_box((0.06, 0.035, 0.70), bevel=0.0, segs=1), loc=(k * 0.11, yy - 0.004, zz), rot=(-70, 0, 0))
    for sx in (-1, 1):
        g.part(OR, mk_cyl(0.025, 0.08, 8), loc=(sx * 0.32, hinge_y, hinge_z), rot=(0, 90, 0))
    return frame, {"Grille": g.mesh("ENV_Wall_VentMouth_A_Grille")}


DUCT_W, DUCT_H, DUCT_T = 1.0, 1.0, 0.06


RING = 0.04       # ring bar thickness, outside the duct shell (never inside the crawl section)


def ring_x(b, mat, x, depth=0.06, z0=0.0):
    """Ring around a duct section that runs along X (section in the YZ plane), floor at z0."""
    t, r, w2, h = DUCT_T, RING, DUCT_W / 2, DUCT_H
    b.part(mat, mk_box((depth, DUCT_W + 2 * (t + r), r), bevel=0.008, segs=1), loc=(x, 0, z0 + h + t + r / 2))
    b.part(mat, mk_box((depth, DUCT_W + 2 * (t + r), r), bevel=0.008, segs=1), loc=(x, 0, z0 - t - r / 2))
    for s_ in (-1, 1):
        b.part(mat, mk_box((depth, r, h + 2 * t), bevel=0.008, segs=1), loc=(x, s_ * (w2 + t + r / 2), z0 + h / 2))


def ring_y(b, mat, y, depth=0.06, z0=0.0, x=0.0):
    """Ring around a duct section that runs along Y (section in the XZ plane), floor at z0."""
    t, r, w2, h = DUCT_T, RING, DUCT_W / 2, DUCT_H
    b.part(mat, mk_box((DUCT_W + 2 * (t + r), depth, r), bevel=0.008, segs=1), loc=(x, y, z0 + h + t + r / 2))
    b.part(mat, mk_box((DUCT_W + 2 * (t + r), depth, r), bevel=0.008, segs=1), loc=(x, y, z0 - t - r / 2))
    for s_ in (-1, 1):
        b.part(mat, mk_box((r, depth, h + 2 * t), bevel=0.008, segs=1), loc=(x + s_ * (w2 + t + r / 2), y, z0 + h / 2))


def _duct_ribs(b, x0, x1, step=0.5):
    x = x0 + step / 2
    while x < x1:
        ring_x(b, FD, x)
        x += step


def duct_straight():
    """2 m crawl duct along X: inner 1.0 x 1.0, floor at z 0 (placed at y +3.3 in Unity). AC dressing."""
    b = Builder()
    L = 2.0
    b.part(FD, mk_box((L, DUCT_W + 2 * DUCT_T, DUCT_T), bevel=0.0, segs=1), loc=(0, 0, -DUCT_T / 2))
    b.part(PU, mk_box((L, DUCT_W + 2 * DUCT_T, DUCT_T), bevel=0.0, segs=1), loc=(0, 0, DUCT_H + DUCT_T / 2))
    for s in (-1, 1):
        b.part(PU, mk_box((L, DUCT_T, DUCT_H), bevel=0.0, segs=1), loc=(0, s * (DUCT_W + DUCT_T) / 2, DUCT_H / 2))
        ring_x(b, OR, s * (L / 2 - 0.04), depth=0.08)                                # end flanges
    _duct_ribs(b, -L / 2 + 0.25, L / 2 - 0.25)
    b.part(FD, mk_box((L - 0.2, 0.30, 0.012), bevel=0.0, segs=1), loc=(0, 0, 0.006))   # walking strip (grimy plate)
    return b.mesh("ENV_Duct_Straight_A")


def duct_corner():
    """90-degree crawl duct turn, 2 x 2 footprint: arms to -X and +Y, centred on the origin."""
    b = Builder()
    h, t, w2 = DUCT_H, DUCT_T, DUCT_W / 2
    for z, mat in ((-t / 2, FD), (h + t / 2, PU)):                                    # floor / roof (L shape)
        b.part(mat, mk_box((1.5 + t, DUCT_W + 2 * t, t), bevel=0.0, segs=1), loc=(-0.25 - t / 2, 0, z))
        b.part(mat, mk_box((DUCT_W + 2 * t, 1.0 - w2, t), bevel=0.0, segs=1), loc=(0, (w2 + 1.0) / 2, z))
    b.part(PU, mk_box((1.5 + t, t, h), bevel=0.0, segs=1), loc=(-0.25 + t / 2, -(w2 + t / 2), h / 2))   # outer -Y wall
    b.part(PU, mk_box((t, 1.5 + t, h), bevel=0.0, segs=1), loc=(w2 + t / 2, 0.25 - t / 2, h / 2))      # outer +X wall
    b.part(PU, mk_box((1.0 - w2, t, h), bevel=0.0, segs=1), loc=(-(w2 + 1.0) / 2, w2 + t / 2, h / 2))  # inner walls
    b.part(PU, mk_box((t, 1.0 - w2, h), bevel=0.0, segs=1), loc=(-(w2 + t / 2), (w2 + 1.0) / 2, h / 2))
    ring_x(b, OR, -0.96, depth=0.08)
    ring_y(b, OR, 0.96, depth=0.08)
    return b.mesh("ENV_Duct_Corner_A")


RISE, RUN = 1.5, 2.0


def duct_riser():
    """Sloped crawl duct from a vent mouth (floor z 0 at y 0) up 1.5 m over 2 m along +Y into the crawl layer."""
    b = Builder()
    t, h, w2 = DUCT_T, DUCT_H, DUCT_W / 2
    b.part(FD, mk_prism_yz([(0, -t), (RUN, RISE - t), (RUN, RISE), (0, 0)], -w2 - t, w2 + t))           # floor
    b.part(PU, mk_prism_yz([(0, h), (RUN, RISE + h), (RUN, RISE + h + t), (0, h + t)], -w2 - t, w2 + t))  # roof
    for s in (-1, 1):
        x0, x1 = (w2, w2 + t) if s > 0 else (-w2 - t, -w2)
        b.part(PU, mk_prism_yz([(0, -t), (RUN, RISE - t), (RUN, RISE + h + t), (0, h + t)], x0, x1))
    ang = math.degrees(math.atan2(RISE, RUN))
    # (no grip bars on the slope: any bump steeper than the controller's 45 deg slope limit stops a crawl)
    ring_y(b, OR, RUN - 0.04, depth=0.08, z0=RISE)                                     # top flange
    return b.mesh("ENV_Duct_Riser_A")


MODULES = {      # name -> (builder, Unity sub-folder); a builder returns a mesh or (mesh, {child_name: mesh})
    "ENV_Wall_Standard_A": (wall_standard, "Walls"),
    "ENV_Wall_Window_A": (wall_window, "Walls"),
    "ENV_Wall_Braced_A": (wall_braced, "Walls"),
    "ENV_Wall_Utility_A": (wall_utility, "Walls"),
    "ENV_Floor_Solid_A": (floor_solid, "Floors"),
    "ENV_Floor_Grate_A": (floor_grate, "Floors"),
    "ENV_Ceiling_Standard_A": (ceiling_standard, "Ceilings"),
    "ENV_Ceiling_Light_A": (ceiling_light, "Ceilings"),
    "ENV_Wall_DoorSliding_A": (wall_door_sliding, "Walls"),
    "ENV_Wall_VentMouth_A": (wall_vent_mouth, "Walls"),
    "ENV_Duct_Straight_A": (duct_straight, "Ducts"),
    "ENV_Duct_Corner_A": (duct_corner, "Ducts"),
    "ENV_Duct_Riser_A": (duct_riser, "Ducts"),
    "ENV_Wall_Viewport_A": (wall_viewport, "Walls"),
}


# ---------------------------------------------------------------- export + presentation

def export_fbx(obj, sub):
    """FBX for Unity: metres, Y up, transforms baked, pivot kept at the module origin."""
    folder = os.path.join(UNITY_DIR, sub)
    os.makedirs(folder, exist_ok=True)
    for o in bpy.context.scene.objects:
        o.select_set(o == obj or o.parent == obj)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(folder, f"{obj.name}.fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"}, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE", use_mesh_modifiers=True,
                             add_leaf_bones=False, bake_anim=False, path_mode="STRIP")
    return path


def stage():
    sc = bpy.context.scene
    sc.name = "ENV_Presentation"
    world = bpy.data.worlds.new("ENV_World")
    world.use_nodes = True
    bg = next(n for n in world.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs[0].default_value = (0.010, 0.007, 0.016, 1)
    sc.world = world

    def area(name, loc, rot, colour, power, size):
        l = bpy.data.lights.new(name, "AREA"); l.color = colour; l.energy = power; l.size = size
        o = bpy.data.objects.new(name, l); o.location = loc; o.rotation_euler = [math.radians(a) for a in rot]
        sc.collection.objects.link(o)

    def cam(name, loc, target, lens):
        cd = bpy.data.cameras.new(name); cd.lens = lens
        o = bpy.data.objects.new(name, cd); o.location = loc
        o.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
        sc.collection.objects.link(o)
        return o

    area("LGT_Key_Amber", (0.0, -5.5, 4.0), (50, 0, -15), (1.0, 0.72, 0.45), 650, 2.5)
    area("LGT_Rim_Cyan", (8.5, -1.5, 2.5), (80, 0, 75), (0.3, 0.85, 1.0), 300, 1.2)
    area("LGT_Fill_Magenta", (-3.5, -2.5, 0.8), (75, 0, -65), (1.0, 0.3, 0.85), 160, 1.5)
    area("LGT_Down", (3.0, -2.0, 2.6), (0, 0, 0), (0.85, 0.85, 1.0), 120, 3.0)   # floors read; under the ceilings
    return {
        "Walls": (cam("CAM_Walls", (5.0, -7.0, 1.8), (3.0, 0, 1.4), 28), (2000, 1100)),
        "Floors": (cam("CAM_Floors", (3.0, -5.4, 3.2), (3.0, -1.0, 0.0), 30), (1600, 1000)),
        "Ceilings": (cam("CAM_Ceilings", (2.0, -3.4, 1.0), (2.0, -1.0, 3.0), 24), (1600, 1000)),
        "Layers": (cam("CAM_Layers", (13.6, -4.6, 2.9), (10.6, 0.6, 1.9), 26), (1800, 1100)),
    }


def main():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    materials()
    sc = bpy.context.scene
    objs = {}
    for name, (fn, sub) in MODULES.items():
        built = fn()
        me, children = built if isinstance(built, tuple) else (built, {})
        ns["clean_mesh"](me)
        o = bpy.data.objects.new(name, me)
        sc.collection.objects.link(o)
        objs[name] = o
        for child, cme in children.items():                 # moving parts (door leaves, grille): child objects
            ns["clean_mesh"](cme)
            c = bpy.data.objects.new(child, cme)
            sc.collection.objects.link(c)
            c.parent = o
        path = export_fbx(o, sub)
        print(f"@@ {name}: {ns['tris'](me)} tris, dims {tuple(round(v, 3) for v in o.dimensions)} -> {path}")

    # presentation layout: walls in a row (x = 0..6), a floor strip in front, ceilings 3 m above the floors
    for i, n in enumerate(("ENV_Wall_Standard_A", "ENV_Wall_Window_A", "ENV_Wall_Braced_A", "ENV_Wall_Utility_A")):
        objs[n].location = (i * 2.0, 0, 0)
    for i, n in enumerate(("ENV_Floor_Solid_A", "ENV_Floor_Grate_A", "ENV_Floor_Solid_A", "ENV_Floor_Grate_A")):
        o = objs[n] if i < 2 else bpy.data.objects.new(f"{n}_copy{i}", objs[n].data)
        if i >= 2:
            sc.collection.objects.link(o)
        o.location = (i * 2.0, -1.0, 0)
    # layer connectors: door + vent mouth on the L3 wall line, riser behind the mouth, an L2 run after it
    objs["ENV_Wall_DoorSliding_A"].location = (8.4, 0, 0)
    objs["ENV_Wall_VentMouth_A"].location = (10.4, 0, 0)
    objs["ENV_Duct_Riser_A"].location = (10.4, 0.30, 1.8)
    objs["ENV_Duct_Straight_A"].location = (10.4, 2.30 + 1.0, 3.3)
    objs["ENV_Duct_Straight_A"].rotation_euler = (0, 0, math.radians(90))
    objs["ENV_Duct_Corner_A"].location = (13.2, -1.6, 0)
    for i, n in enumerate(("ENV_Ceiling_Standard_A", "ENV_Ceiling_Light_A")):
        objs[n].location = (1.0 + i * 2.0, -1.0, 3.0)

    if os.environ.get("VV_EXPORT_ONLY"):
        return
    cams = stage()
    os.makedirs(os.path.join(OUT_DIR, "review"), exist_ok=True)
    sc.render.engine = "CYCLES"
    sc.cycles.samples = int(os.environ.get("VV_SAMPLES", "96"))
    sc.cycles.use_denoising = True
    try:
        prefs = bpy.context.preferences.addons["cycles"].preferences
        for kind in ("OPTIX", "CUDA"):
            try:
                prefs.compute_device_type = kind; prefs.get_devices()
                if any(d.type == kind for d in prefs.devices):
                    for d in prefs.devices:
                        d.use = True
                    sc.cycles.device = "GPU"; break
            except TypeError:
                continue
    except Exception as e:
        print("cycles device:", e)
    sc.view_settings.view_transform = "AgX"
    sc.view_settings.look = "AgX - Medium High Contrast"
    shots = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else list(cams)
    for shot in shots:
        camo, (rx, ry) = cams[shot]
        for n in ("ENV_Ceiling_Standard_A", "ENV_Ceiling_Light_A"):        # ceilings only in the Ceilings shot
            objs[n].hide_render = shot != "Ceilings"
        sc.camera = camo
        sc.render.resolution_x, sc.render.resolution_y = rx, ry
        sc.render.filepath = os.path.join(OUT_DIR, "review", f"ENV_Kit_{shot}.png")
        bpy.ops.render.render(write_still=True)
        print("@@ rendered", sc.render.filepath)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, "ENV_Kit_A_v001.blend"))
    print("@@ saved")


main()
