"""Shared helpers for Vents & Vending Blender build scripts (run inside Blender via the MCP or the Text Editor).

Conventions (Docs/Art/CharacterStandards.md): metres, characters face -Y, feet at Z = 0, presentation scene
`VV_Presentation` with subjects parented to `PRES_Pivot`.
"""
import math
import os

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
from mathutils.bvhtree import BVHTree

SCENE = "VV_Presentation"
REPO = r"F:\Vents & Vending"


def scene():
    return bpy.data.scenes[SCENE]


def collection(name):
    sc = scene()
    c = bpy.data.collections.get(name) or bpy.data.collections.new(name)
    if c.name not in sc.collection.children:
        sc.collection.children.link(c)
    return c


def inp(node, ident):
    return next(s for s in node.inputs if s.identifier == ident)


def material(name, colour, rough=0.6, metal=0.0, emit=None, strength=0.0):
    """Get a library material if it exists, else create a simple principled one (character-only materials)."""
    m = bpy.data.materials.get(name)
    if m is not None and name.startswith("VV_MAT_"):
        return m
    m = m or bpy.data.materials.new(name)
    m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    inp(b, "Base Color").default_value = (*colour, 1)
    inp(b, "Roughness").default_value = rough
    inp(b, "Metallic").default_value = metal
    if emit:
        inp(b, "Emission Color").default_value = (*emit, 1)
        inp(b, "Emission Strength").default_value = strength
    m.diffuse_color = (*colour, 1)
    return m


def replace_object(name, mesh, coll, parent="PRES_Pivot"):
    old = bpy.data.objects.get(name)
    if old is not None:
        om = old.data
        bpy.data.objects.remove(old)
        if om is not None and om is not mesh and om.users == 0:
            bpy.data.meshes.remove(om)
    stale = bpy.data.meshes.get(name)
    if stale is not None and stale is not mesh and stale.users == 0:
        bpy.data.meshes.remove(stale)
    mesh.name = name
    o = bpy.data.objects.new(name, mesh)
    coll.objects.link(o)
    if parent:
        o.parent = bpy.data.objects[parent]
    return o


def tris(mesh):
    return sum(len(p.vertices) - 2 for p in mesh.polygons)


# ---------------------------------------------------------------- skeleton -> skin mesh

class Skel:
    def __init__(self):
        self.V, self.E, self.R = [], [], []

    def v(self, p, r):
        self.V.append(Vector(p))
        self.R.append(r if isinstance(r, tuple) else (r, r))
        return len(self.V) - 1

    def e(self, a, b):
        self.E.append((a, b))

    def chain(self, start, points, radii):
        prev = start
        out = []
        for p, r in zip(points, radii):
            i = self.v(p, r)
            if prev is not None:
                self.e(prev, i)
            prev = i
            out.append(i)
        return out


def catmull_rom(points, samples):
    """Smooth curve through `points` (Catmull-Rom), `samples` evenly spaced in parameter."""
    pts = [points[0]] + list(points) + [points[-1]]
    out = []
    for k in range(samples):
        u = k / (samples - 1) * (len(points) - 1)
        i = min(int(u), len(points) - 2)
        t = u - i
        p0, p1, p2, p3 = pts[i], pts[i + 1], pts[i + 2], pts[i + 3]
        t2, t3 = t * t, t * t * t
        out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    return out


def loft(centre, radius_fn, segments=10, cap_start=False, cap_end=True, ellipse=None):
    """Tube along a polyline `centre`; radius_fn(t in 0..1) -> radius (or (rx, ry)). Rounded end cap."""
    bm = bmesh.new()
    n = len(centre)
    rings = []
    prev_side = None
    for i, c in enumerate(centre):
        t = i / (n - 1)
        tang = (centre[min(i + 1, n - 1)] - centre[max(i - 1, 0)]).normalized()
        ref = Vector((0, 0, 1)) if abs(tang.z) < 0.9 else Vector((1, 0, 0))
        side = tang.cross(ref).normalized()
        if prev_side is not None and side.dot(prev_side) < 0:
            side = -side
        prev_side = side
        up = side.cross(tang).normalized()
        r = radius_fn(t)
        rx, ry = (r, r) if not isinstance(r, tuple) else r
        ring = []
        for k in range(segments):
            a = 2 * math.pi * k / segments
            ring.append(bm.verts.new(c + side * (math.cos(a) * rx) + up * (math.sin(a) * ry)))
        rings.append(ring)
    for i in range(n - 1):
        a, b = rings[i], rings[i + 1]
        for k in range(segments):
            bm.faces.new((a[k], a[(k + 1) % segments], b[(k + 1) % segments], b[k]))
    if cap_end:
        r1 = radius_fn(1.0)
        r1 = max(r1) if isinstance(r1, tuple) else r1
        tip = bm.verts.new(centre[-1] + (centre[-1] - centre[-2]).normalized() * r1 * 0.9)   # rounded tip
        for k in range(segments):
            bm.faces.new((rings[-1][k], rings[-1][(k + 1) % segments], tip))
    if cap_start:
        base = bm.verts.new(centre[0])
        for k in range(segments):
            bm.faces.new((rings[0][(k + 1) % segments], rings[0][k], base))
    bm.normal_update()
    me = bpy.data.meshes.new("_vv_loft")
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    return me


def densify(points, radii, segments):
    """Insert a midpoint into each listed segment index (i -> i+1) so a chain can bend around obstacles."""
    pts, rad = [], []
    for i, (p, r) in enumerate(zip(points, radii)):
        pts.append(p)
        rad.append(r)
        if i in segments and i + 1 < len(points):
            pts.append((p + points[i + 1]) * 0.5)
            rad.append((r + radii[i + 1]) * 0.5)
    return pts, rad


def drape(points, radii, tree, margin=0.012, iterations=6):
    """Push a hanging chain out of `tree` (BVH of colliders) joint by joint; each push is carried down the chain,
    so a tendril that would pass through a shoulder pad ends up resting on top of it."""
    out = [points[0].copy()]
    carry = Vector((0, 0, 0))
    for i in range(1, len(points)):
        p = points[i] + carry
        need = radii[i] + margin
        for _ in range(iterations):
            loc, nrm, _, dist = tree.find_nearest(p)
            if loc is None:
                break
            inside = (p - loc).dot(nrm) < 0.0
            if not inside and dist >= need:
                break
            p = loc + nrm * need
        carry = p - points[i]
        out.append(p)
    return out


def evaluate(mesh, setup):
    """Run modifiers set up by `setup(obj)` on `mesh` and return the evaluated mesh (input mesh is freed)."""
    o = bpy.data.objects.new("_vv_tmp", mesh)
    scene().collection.objects.link(o)
    setup(o)
    dg = scene().view_layers[0].depsgraph
    dg.update()
    out = bpy.data.meshes.new_from_object(o.evaluated_get(dg))
    bpy.data.objects.remove(o)
    bpy.data.meshes.remove(mesh)
    return out


def skin_mesh(skel, root=0, subsurf=1, smooth=0.6):
    me = bpy.data.meshes.new("_vv_skel")
    me.from_pydata([tuple(p) for p in skel.V], skel.E, [])

    def setup(o):
        sk = o.modifiers.new("Skin", "SKIN")
        sk.use_smooth_shade = True
        sk.branch_smoothing = smooth
        for i, sv in enumerate(me.skin_vertices[0].data):
            sv.radius = skel.R[i]
            sv.use_root = i == root
        if subsurf:
            s = o.modifiers.new("Sub", "SUBSURF")
            s.levels = subsurf
            s.render_levels = subsurf

    return evaluate(me, setup)


def nonmanifold_edges(skel, root=0, subsurf=0):
    """Build the skin mesh for `skel` and count non-manifold edges (0 = sealed surface)."""
    me = skin_mesh(skel, root=root, subsurf=subsurf, smooth=0.5)
    bm = bmesh.new()
    bm.from_mesh(me)
    n = sum(1 for e in bm.edges if not e.is_manifold)
    bm.free()
    bpy.data.meshes.remove(me)
    return n


def flatten_soles(mesh, lo=0.045, hi=0.062, max_abs_y=1.0):
    """Squash everything below `hi` so the underside becomes a flat sole on Z = 0."""
    for v in mesh.vertices:
        z = v.co.z
        if z < hi and abs(v.co.y) < max_abs_y:
            v.co.z = 0.0 if z < lo else (z - lo) / (hi - lo) * hi
    mesh.update()


def solidify(mesh, thickness, offset=-1.0):
    def setup(o):
        s = o.modifiers.new("Solid", "SOLIDIFY")
        s.thickness = thickness
        s.offset = offset
        s.use_even_offset = True
    return evaluate(mesh, setup)


def decimate(mesh, ratio):
    def setup(o):
        d = o.modifiers.new("Dec", "DECIMATE")
        d.ratio = ratio
        d.use_collapse_triangulate = False
    return evaluate(mesh, setup)


def subdivide(mesh, levels=1):
    def setup(o):
        s = o.modifiers.new("Sub", "SUBSURF")
        s.levels = levels
        s.render_levels = levels
    return evaluate(mesh, setup)


def region_shell(src, pred, offset):
    """Copy the faces of `src` whose (centre, normal) satisfy `pred`, pushed out along vertex normals."""
    bm = bmesh.new()
    bm.from_mesh(src)
    bm.normal_update()
    kill = [f for f in bm.faces if not pred(f.calc_center_median(), f.normal)]
    bmesh.ops.delete(bm, geom=kill, context="FACES")
    loose = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=loose, context="VERTS")
    bm.normal_update()
    for v in bm.verts:
        v.co += v.normal * offset
    me = bpy.data.meshes.new("_vv_shell")
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    return me


def bvh(meshes):
    bm = bmesh.new()
    for me in meshes:
        bm.from_mesh(me)
    tree = BVHTree.FromBMesh(bm)
    bm.free()
    return tree


def hit_front(tree, x, z, inset=0.0):
    """Front-most surface point at (x, z) looking from -Y towards +Y."""
    loc, nrm, _, _ = tree.ray_cast(Vector((x, -3.0, z)), Vector((0, 1, 0)))
    if loc is None:
        return None
    return loc + Vector((0, inset, 0))


# ---------------------------------------------------------------- multi-material builder

class Builder:
    """Accumulates parts into one bmesh with per-part materials and transforms."""

    def __init__(self):
        self.bm = bmesh.new()
        self.mats = []

    def part(self, mat, make, loc=(0, 0, 0), rot=(0, 0, 0), scale=None):
        bf, bv = set(self.bm.faces), set(self.bm.verts)
        make(self.bm)
        nf = [f for f in self.bm.faces if f not in bf]
        nv = [v for v in self.bm.verts if v not in bv]
        if mat not in self.mats:
            self.mats.append(mat)
        for f in nf:
            f.material_index = self.mats.index(mat)
        m = Matrix.Translation(Vector(loc)) @ Euler([math.radians(a) for a in rot]).to_matrix().to_4x4()
        if scale is not None:
            m = m @ Matrix.Diagonal((*scale, 1.0))
        bmesh.ops.transform(self.bm, matrix=m, verts=nv)
        return nv

    def mesh(self, name="_vv_built", smooth=True):
        me = bpy.data.meshes.new(name)
        self.bm.normal_update()
        self.bm.to_mesh(me)
        self.bm.free()
        for m in self.mats:
            me.materials.append(bpy.data.materials[m])
        for p in me.polygons:
            p.use_smooth = smooth
        return me


def mk_box(size, bevel=0.0, segs=2):
    def f(bm):
        eb = set(bm.edges)
        r = bmesh.ops.create_cube(bm, size=1.0)
        bmesh.ops.scale(bm, vec=size, verts=r["verts"])
        if bevel > 0:
            edges = [e for e in bm.edges if e not in eb]
            bmesh.ops.bevel(bm, geom=edges, offset=bevel, segments=segs, affect="EDGES", profile=0.5, clamp_overlap=True)
    return f


def mk_cyl(r1, depth, segs=16, r2=None):
    def f(bm):
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs, radius1=r1,
                              radius2=r1 if r2 is None else r2, depth=depth)
    return f


def mk_sphere(r, u=16, v=10):
    def f(bm):
        bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)
    return f


def mk_mesh(src):
    """Append an existing mesh's geometry (e.g. a skin or shell result) as a builder part."""
    def f(bm):
        bm.from_mesh(src)
    return f


# ---------------------------------------------------------------- rendering

def use_cycles(samples=48):
    """Approval renders for materials use Cycles (edge wear relies on the Bevel node)."""
    sc = scene()
    try:
        sc.render.engine = "CYCLES"
    except TypeError as e:
        print("engine:", e)
        return
    sc.cycles.samples = samples
    sc.cycles.use_denoising = True
    try:
        prefs = bpy.context.preferences.addons["cycles"].preferences
        for kind in ("OPTIX", "CUDA", "HIP", "ONEAPI", "METAL"):
            try:
                prefs.compute_device_type = kind
                prefs.get_devices()
                if any(d.type == kind for d in prefs.devices):
                    for d in prefs.devices:
                        d.use = True
                    sc.cycles.device = "GPU"
                    break
            except TypeError:
                continue
    except Exception as e:  # CPU fallback is fine
        print("cycles device:", e)


def render_views(prefix, out_dir, show_collections, views=None, face=False):
    """Render the standard approval views. Only `show_collections` (plus presentation helpers) are visible."""
    sc = scene()
    os.makedirs(out_dir, exist_ok=True)
    keep = set(show_collections) | {"VV_Presentation", "VV_Reference"}
    hidden = []
    for c in sc.collection.children:
        if c.name not in keep and not c.hide_render:
            c.hide_render = True
            hidden.append(c)
    piv = bpy.data.objects["PRES_Pivot"]
    r = sc.render
    r.resolution_x, r.resolution_y = 1080, 1350
    views = views or [("Front", "CAM_Turnaround", 0), ("Side", "CAM_Turnaround", 90),
                      ("Rear", "CAM_Turnaround", 180), ("ThreeQuarter", "CAM_ThreeQuarter", 0)]
    if face:
        views = views + [("FaceCloseup", "CAM_FaceCloseup", 0)]
    out = []
    try:
        for name, cam, yaw in views:
            piv.rotation_euler = (0, 0, math.radians(yaw))
            sc.camera = bpy.data.objects[cam]
            r.filepath = os.path.join(out_dir, f"{prefix}_{name}.png")
            bpy.ops.render.render(write_still=True, scene=sc.name)
            out.append(r.filepath)
    finally:
        piv.rotation_euler = (0, 0, 0)
        sc.camera = bpy.data.objects["CAM_Turnaround"]
        for c in hidden:
            c.hide_render = False
    return out
