"""Stage 4 materials: edge-driven wear for the VV library + vendor-specific materials.

Run after vv_lib.py.

Wear is computed in the shader, not stored on the mesh:
- Edges: Cycles' Bevel node gives a rounded normal near edges; 1 - dot(bevel normal, true normal) is an edge mask
  that works on any topology (low-poly boxes included). Broken up by noise it becomes paint chips (refs R3/R5/R7).
- Creases: the Ambient Occlusion node darkens recesses (grime).
EEVEE ignores the Bevel node, so EEVEE previews show the base material; approval renders use Cycles, and
Stage 7 bakes the result to textures for Unity (see Docs/Art/ArtDirection.md §0.4).
"""
import bpy

WEAR_TAG = "VV_WearLayer"


def _sock(node, ident, outputs=False):
    return next(s for s in (node.outputs if outputs else node.inputs) if s.identifier == ident)


def add_wear_layer(mat, chip_colour, chip_metal=None, chip_amount=0.5, grime=0.45, chip_scale=24.0,
                   edge_radius=0.012):
    """Insert edge chips and crease grime in front of Base Color (idempotent: replaces a previous layer)."""
    nt = mat.node_tree
    _strip_wear(mat)                       # re-running replaces the previous layer (sources reconnected first)
    b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    base_in = _sock(b, "Base Color")
    met_in = _sock(b, "Metallic")
    if base_in.is_linked:
        src = base_in.links[0].from_socket
    else:
        rgb = nt.nodes.new("ShaderNodeRGB")
        rgb.outputs[0].default_value = base_in.default_value
        src = rgb.outputs[0]

    def node(kind, x, y):
        n = nt.nodes.new(kind); n.label = WEAR_TAG; n.location = (x, y); return n

    # Edge mask from the Bevel node (Cycles)
    bev = node("ShaderNodeBevel", -1100, -600); bev.samples = 8
    _sock(bev, "Radius").default_value = edge_radius
    geo = node("ShaderNodeNewGeometry", -1100, -800)
    dot = node("ShaderNodeVectorMath", -900, -700); dot.operation = "DOT_PRODUCT"
    nt.links.new(_sock(bev, "Normal", True), dot.inputs[0])
    nt.links.new(_sock(geo, "Normal", True), dot.inputs[1])
    edge = node("ShaderNodeMapRange", -700, -700)
    nt.links.new(_sock(dot, "Value", True), _sock(edge, "Value"))
    _sock(edge, "From Min").default_value = 0.985
    _sock(edge, "From Max").default_value = 0.80
    _sock(edge, "To Min").default_value = 0.0
    _sock(edge, "To Max").default_value = 1.0
    # Noise breaks the edge band into chips
    tc = node("ShaderNodeTexCoord", -1100, -1000)
    nz = node("ShaderNodeTexNoise", -900, -1000)
    _sock(nz, "Scale").default_value = chip_scale
    _sock(nz, "Detail").default_value = 6.0
    _sock(nz, "Roughness").default_value = 0.65
    nt.links.new(_sock(tc, "Object", True), _sock(nz, "Vector"))
    nn = node("ShaderNodeMapRange", -700, -1000)                # noise 0.3..0.7 -> 0..1 (full-strength breakup)
    nt.links.new(_sock(nz, "Fac", True), _sock(nn, "Value"))
    _sock(nn, "From Min").default_value = 0.3
    _sock(nn, "From Max").default_value = 0.7
    mul = node("ShaderNodeMath", -500, -800); mul.operation = "MULTIPLY"
    nt.links.new(_sock(edge, "Result", True), mul.inputs[0])
    nt.links.new(_sock(nn, "Result", True), mul.inputs[1])
    ramp = node("ShaderNodeValToRGB", -320, -800)
    t = 0.55 - chip_amount * 0.4
    ramp.color_ramp.elements[0].position = t
    ramp.color_ramp.elements[1].position = t + 0.04
    nt.links.new(_sock(mul, "Value", True), _sock(ramp, "Fac"))
    chip = node("ShaderNodeMix", -60, -500); chip.data_type = "RGBA"
    nt.links.new(_sock(ramp, "Color", True), _sock(chip, "Factor_Float"))
    nt.links.new(src, _sock(chip, "A_Color"))
    _sock(chip, "B_Color").default_value = (*chip_colour, 1)
    # Crease grime from ambient occlusion
    ao = node("ShaderNodeAmbientOcclusion", -700, -1250); ao.samples = 8; ao.only_local = True
    _sock(ao, "Distance").default_value = 0.06
    inv = node("ShaderNodeMath", -500, -1250); inv.operation = "SUBTRACT"; inv.inputs[0].default_value = 1.0
    nt.links.new(_sock(ao, "AO", True), inv.inputs[1])
    gm = node("ShaderNodeMath", -320, -1250); gm.operation = "MULTIPLY"; gm.inputs[1].default_value = grime
    nt.links.new(_sock(inv, "Value", True), gm.inputs[0])
    dirt = node("ShaderNodeMix", 120, -500); dirt.data_type = "RGBA"; dirt.blend_type = "MULTIPLY"
    nt.links.new(_sock(gm, "Value", True), _sock(dirt, "Factor_Float"))
    nt.links.new(_sock(chip, "Result_Color", True), _sock(dirt, "A_Color"))
    _sock(dirt, "B_Color").default_value = (0.22, 0.18, 0.2, 1)
    nt.links.new(_sock(dirt, "Result_Color", True), base_in)
    if chip_metal is not None:
        prev = met_in.links[0].from_socket if met_in.is_linked else None
        mm = node("ShaderNodeMix", 120, -250); mm.data_type = "FLOAT"
        nt.links.new(_sock(ramp, "Color", True), _sock(mm, "Factor_Float"))
        if prev is not None:
            nt.links.new(prev, _sock(mm, "A_Float"))
        else:
            _sock(mm, "A_Float").default_value = met_in.default_value
        _sock(mm, "B_Float").default_value = chip_metal
        nt.links.new(_sock(mm, "Result_Float", True), met_in)


def _strip_wear(mat):
    """Remove a wear layer and reconnect what fed it (used when cloning a material that already has one)."""
    nt = mat.node_tree
    b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    chip = next((n for n in nt.nodes if n.label == WEAR_TAG and n.type == "MIX" and n.blend_type == "MIX"
                 and n.data_type == "RGBA"), None)
    src = _sock(chip, "A_Color").links[0].from_socket if chip and _sock(chip, "A_Color").is_linked else None
    mm = next((n for n in nt.nodes if n.label == WEAR_TAG and n.type == "MIX" and n.data_type == "FLOAT"), None)
    msrc = _sock(mm, "A_Float").links[0].from_socket if mm and _sock(mm, "A_Float").is_linked else None
    for n in [n for n in nt.nodes if n.label == WEAR_TAG]:
        nt.nodes.remove(n)
    if src is not None:
        nt.links.new(src, _sock(b, "Base Color"))
    if msrc is not None:
        nt.links.new(msrc, _sock(b, "Metallic"))


def _clone(src_name, dst_name):
    m = bpy.data.materials.get(dst_name)
    if m is not None:
        bpy.data.materials.remove(m)
    m = bpy.data.materials[src_name].copy()
    m.name = dst_name
    m.use_fake_user = False
    _strip_wear(m)
    return m


def _spots(mat, colour, scale, size, strength, tag="VV_Spots"):
    """Darker markings: soft Voronoi spots in object space mixed over the base colour."""
    nt = mat.node_tree
    for n in [n for n in nt.nodes if n.label == tag]:
        nt.nodes.remove(n)
    b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    base_in = _sock(b, "Base Color")
    src = base_in.links[0].from_socket
    tc = nt.nodes.new("ShaderNodeTexCoord"); tc.label = tag; tc.location = (-1100, 500)
    vo = nt.nodes.new("ShaderNodeTexVoronoi"); vo.label = tag; vo.location = (-900, 500)
    _sock(vo, "Scale").default_value = scale
    _sock(vo, "Randomness").default_value = 0.85
    nt.links.new(_sock(tc, "Object", True), _sock(vo, "Vector"))
    rp = nt.nodes.new("ShaderNodeValToRGB"); rp.label = tag; rp.location = (-700, 500)
    rp.color_ramp.elements[0].position = size * 0.6
    rp.color_ramp.elements[1].position = size
    nt.links.new(_sock(vo, "Distance", True), _sock(rp, "Fac"))
    inv = nt.nodes.new("ShaderNodeMath"); inv.label = tag; inv.location = (-450, 500); inv.operation = "SUBTRACT"
    inv.inputs[0].default_value = 1.0
    nt.links.new(_sock(rp, "Color", True), inv.inputs[1])
    sc = nt.nodes.new("ShaderNodeMath"); sc.label = tag; sc.location = (-300, 500); sc.operation = "MULTIPLY"
    nt.links.new(_sock(inv, "Value", True), sc.inputs[0]); sc.inputs[1].default_value = strength
    mx = nt.nodes.new("ShaderNodeMix"); mx.label = tag; mx.location = (-100, 450); mx.data_type = "RGBA"
    nt.links.new(_sock(sc, "Value", True), _sock(mx, "Factor_Float"))
    nt.links.new(src, _sock(mx, "A_Color"))
    _sock(mx, "B_Color").default_value = (*colour, 1)
    nt.links.new(_sock(mx, "Result_Color", True), base_in)


def build_materials():
    light_chip = (0.62, 0.58, 0.54)            # pale primer showing through paint (refs: bright edge chips)
    bare_metal = (0.55, 0.55, 0.58)
    lib = bpy.data.materials
    add_wear_layer(lib["VV_MAT_OrangePaint"], light_chip, chip_amount=0.55)
    add_wear_layer(lib["VV_MAT_OchrePaint"], light_chip, chip_amount=0.45)
    add_wear_layer(lib["VV_MAT_WornPaint"], bare_metal, chip_metal=0.9, chip_amount=0.55)
    add_wear_layer(lib["VV_MAT_LightArmour"], (0.12, 0.11, 0.13), chip_metal=0.6, chip_amount=0.5)
    add_wear_layer(lib["VV_MAT_IndustrialMetal"], bare_metal, chip_metal=1.0, chip_amount=0.45)
    add_wear_layer(lib["VV_MAT_DarkMetal"], (0.32, 0.32, 0.35), chip_metal=1.0, chip_amount=0.4)
    add_wear_layer(lib["VV_MAT_CharcoalFabric"], (0.15, 0.14, 0.16), chip_amount=0.25, grime=0.5, edge_radius=0.02)
    add_wear_layer(lib["VV_MAT_WornFabric"], (0.66, 0.6, 0.48), chip_amount=0.2, grime=0.55, edge_radius=0.02)

    # Vendor skin: library skin + darker purple markings + crease shading (no chips on skin)
    skin = _clone("VV_MAT_AlienSkin", "VENDOR_MAT_Skin")
    _spots(skin, (0.13, 0.035, 0.22), scale=9.0, size=0.36, strength=0.65)
    add_wear_layer(skin, (0.4, 0.16, 0.55), chip_amount=-1.0, grime=0.35)

    # Workwear: dark purple-grey twill with scuffed edges and grime
    ww = _clone("VV_MAT_CharcoalFabric", "VENDOR_MAT_Workwear")
    for n in ww.node_tree.nodes:
        if n.type == "MIX" and n.data_type == "RGBA" and n.blend_type == "MIX":
            _sock(n, "A_Color").default_value = (0.085, 0.07, 0.105, 1)
            _sock(n, "B_Color").default_value = (0.06, 0.05, 0.08, 1)
            break
    add_wear_layer(ww, (0.2, 0.18, 0.22), chip_amount=0.25, grime=0.5, edge_radius=0.02)

    # Apron: worn beige fabric with grime and a few magenta product stains
    ap = _clone("VV_MAT_WornFabric", "VENDOR_MAT_Apron")
    _spots(ap, (0.34, 0.1, 0.25), scale=3.2, size=0.16, strength=0.55, tag="VV_Stains")
    add_wear_layer(ap, (0.7, 0.64, 0.52), chip_amount=0.2, grime=0.55, edge_radius=0.02)
    return ["VENDOR_MAT_Skin", "VENDOR_MAT_Workwear", "VENDOR_MAT_Apron"]
