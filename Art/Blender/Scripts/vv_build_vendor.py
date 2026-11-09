"""Builds NPC_Alien_Vendor_001 in the VV_Presentation scene (Stage 3/4 model; v003 follows the owner's character sheet).

Run after vv_lib.py and vv_materials.py (build_materials) in the same namespace.
Character faces -Y; the character's LEFT side is +X. Object names follow Docs/Art/CharacterStandards.md (VENDOR_*).

Design notes from the character sheet (2026-11-09):
- Head, neck and tendrils are ONE continuous surface: they come from a single branching skin skeleton.
- Long smooth neck, swept-back cranium, small muzzle with a small crooked smile, almond eyes under heavy lids.
- Bare purple spotted hands and forearms, orange wrist cuffs, collar device with a cyan screen, apron straps
  crossing at the back with a tie, a front pocket and shopping-cart emblems, pink and cyan product vials.
"""
import math

import bmesh
import bpy
from mathutils import Matrix, Vector

ARM_ANGLE = 45.0
SLEEVE_T = 0.47           # sleeves end mid-forearm (distance along the upper-arm axis from the shoulder)


def _vendor_materials():
    material("VENDOR_MAT_SkinDark", (0.12, 0.03, 0.19), rough=0.55)            # brows
    material("VENDOR_MAT_Sclera", (0.95, 0.88, 0.62), rough=0.25)
    material("VENDOR_MAT_Iris", (1.0, 0.55, 0.05), rough=0.2, emit=(1.0, 0.45, 0.03), strength=0.6)
    material("VENDOR_MAT_Pupil", (0.01, 0.008, 0.012), rough=0.15)
    material("VENDOR_MAT_Mouth", (0.18, 0.02, 0.06), rough=0.6)
    material("VENDOR_MAT_Teeth", (0.85, 0.82, 0.7), rough=0.35)
    material("VENDOR_MAT_Print", (0.17, 0.07, 0.14), rough=0.8)                # cart emblem print
    material("VENDOR_MAT_Leather", (0.21, 0.085, 0.07), rough=0.6)             # vial pouch


def _body_skeleton():
    s = Skel()
    J = {}
    J["pelvis"] = s.v((0, 0.015, 0.88), (0.190, 0.140))
    J["belly"] = s.v((0, -0.012, 1.00), (0.195, 0.155)); s.e(J["pelvis"], J["belly"])
    J["chest"] = s.v((0, -0.008, 1.16), (0.240, 0.160)); s.e(J["belly"], J["chest"])
    J["upper"] = s.v((0, 0.006, 1.29), (0.215, 0.130)); s.e(J["chest"], J["upper"])
    J["neck"] = s.v((0, 0.02, 1.36), 0.078); s.e(J["upper"], J["neck"])
    for side, tag in ((1, "L"), (-1, "R")):
        d = Vector((side * math.sin(math.radians(ARM_ANGLE)), 0, -math.cos(math.radians(ARM_ANGLE))))
        sh = s.v((side * 0.258, 0.012, 1.30), 0.100); s.e(J["upper"], sh)
        mu = s.v(s.V[sh] + d * 0.13, 0.088); s.e(sh, mu)
        ae = s.v(s.V[sh] + d * 0.245, 0.071); s.e(mu, ae)                                  # narrows above the elbow
        el = s.v(s.V[sh] + d * 0.30 + Vector((0, 0.008, 0)), (0.080, 0.084)); s.e(ae, el)   # elbow knob
        d2 = (d + Vector((0, -0.25, 0))).normalized()                                      # forearm bends ~14 deg
        be = s.v(s.V[el] + d2 * 0.05, 0.070); s.e(el, be)                                  # pinch below the elbow
        mf = s.v(s.V[el] + d2 * 0.12, 0.082); s.e(be, mf)                                  # forearm mass
        wr = s.v(s.V[el] + d2 * 0.27, 0.056); s.e(mf, wr)
        palm = s.v(s.V[wr] + d2 * 0.09, (0.070, 0.050)); s.e(wr, palm)
        for k, oy in enumerate((-0.046, -0.016, 0.014, 0.042)):
            ln = (0.062, 0.070, 0.066, 0.054)[k]
            b0 = s.v(s.V[palm] + d2 * 0.055 + Vector((0, oy, 0)), 0.022); s.e(palm, b0)
            b1 = s.v(s.V[b0] + d2 * ln, 0.0205); s.e(b0, b1)
            b2 = s.v(s.V[b1] + d2 * ln * 0.8, 0.019); s.e(b1, b2)
        t0 = s.v(s.V[palm] + Vector((-side * 0.018, -0.058, 0.008)), 0.026); s.e(palm, t0)
        t1 = s.v(s.V[t0] + Vector((0, -0.04, 0)) + d2 * 0.034, 0.023); s.e(t0, t1)
        t2 = s.v(s.V[t1] + Vector((0, -0.022, 0)) + d2 * 0.03, 0.020); s.e(t1, t2)
        J["shoulder_" + tag], J["wrist_" + tag], J["dir_" + tag] = s.V[sh], s.V[wr], d
        J["elbow_" + tag], J["fdir_" + tag] = s.V[el], d2
        hip = s.v((side * 0.118, 0.015, 0.83), 0.125); s.e(J["pelvis"], hip)
        th = s.v((side * 0.124, 0.0, 0.66), 0.112); s.e(hip, th)
        ak = s.v((side * 0.125, -0.008, 0.545), 0.092); s.e(th, ak)                         # narrows above the knee
        kn = s.v((side * 0.126, -0.03, 0.47), (0.096, 0.102)); s.e(ak, kn)                 # kneecap, slightly forward
        bk = s.v((side * 0.127, -0.006, 0.405), 0.082); s.e(kn, bk)                        # pinch below the knee
        ca = s.v((side * 0.128, 0.014, 0.30), 0.093); s.e(bk, ca)                          # calf
        J["knee_" + tag] = s.V[kn]
        an = s.v((side * 0.130, 0.018, 0.105), 0.062); s.e(ca, an)
        he = s.v((side * 0.130, 0.05, 0.045), (0.056, 0.046)); s.e(an, he)
        ba = s.v((side * 0.134, -0.105, 0.05), (0.074, 0.048)); s.e(an, ba)
        to = s.v((side * 0.136, -0.195, 0.046), (0.066, 0.044)); s.e(ba, to)
    return s, J


def _arm_t(c, J, tag):
    return (c - J["shoulder_" + tag]).dot(J["dir_" + tag])


def _arm_dist(c, J, tag):
    sh, d = J["shoulder_" + tag], J["dir_" + tag]
    t = (c - sh).dot(d)
    return (c - (sh + d * t)).length


def _head_skeleton(colliders):
    """Neck -> skull -> muzzle plus eight tendrils, all one skin skeleton so the surface is continuous."""
    s = Skel()
    N = {}
    N["neck0"] = s.v((0, 0.03, 1.33), 0.086)
    N["neck1"] = s.v((0, 0.022, 1.42), 0.068); s.e(N["neck0"], N["neck1"])
    N["neck2"] = s.v((0, 0.010, 1.50), 0.072); s.e(N["neck1"], N["neck2"])
    N["jaw"] = s.v((0, -0.014, 1.565), (0.098, 0.094)); s.e(N["neck2"], N["jaw"])
    N["face"] = s.v((0, -0.03, 1.645), (0.116, 0.106)); s.e(N["jaw"], N["face"])
    N["brow"] = s.v((0, -0.02, 1.725), (0.110, 0.100)); s.e(N["face"], N["brow"])
    # Swept-back cranium as a chain of nodes, so tendril roots can be spread out: the skin modifier only closes
    # junctions cleanly with at most three connections per node (more leaves holes).
    N["crown"] = s.v((0, 0.045, 1.79), (0.096, 0.088)); s.e(N["brow"], N["crown"])
    N["crownB"] = s.v((0, 0.08, 1.805), (0.084, 0.076)); s.e(N["crown"], N["crownB"])
    N["crest"] = s.v((0, 0.12, 1.81), (0.070, 0.064)); s.e(N["crownB"], N["crest"])
    N["crest2"] = s.v((0, 0.16, 1.80), (0.058, 0.054)); s.e(N["crest"], N["crest2"])
    N["crest3"] = s.v((0, 0.19, 1.778), (0.046, 0.044)); s.e(N["crest2"], N["crest3"])
    N["crest4"] = s.v((0, 0.212, 1.75), (0.038, 0.036)); s.e(N["crest3"], N["crest4"])
    N["nape"] = s.v((0, 0.075, 1.70), (0.072, 0.066)); s.e(N["crownB"], N["nape"])          # back of the skull
    N["dome"] = s.v((0, 0.03, 1.835), (0.07, 0.066)); s.e(N["crown"], N["dome"])            # rounds the top of the skull
    N["muzzle"] = s.v((0, -0.10, 1.585), (0.068, 0.052)); s.e(N["jaw"], N["muzzle"])
    # (attach node, start point, initial direction, length, droop): each arcs briefly up/out, then falls, framing the
    # face and flowing down the back of the head. Start points are mirrored; attachments alternate to spread load.
    tendrils = [
        ("crown", (0.05, -0.02, 1.84), (0.45, -0.25, 1.0), 0.30, 2.9),        # forelock arcing up and over to the side
        ("face", (0.10, -0.03, 1.72), (0.75, -0.25, 0.7), 0.34, 3.2),         # by the left eye
        ("brow", (-0.10, -0.03, 1.72), (-0.75, -0.25, 0.7), 0.34, 3.2),       # by the right eye
        ("crownB", (0.10, 0.05, 1.77), (0.95, 0.2, 0.5), 0.38, 2.8),
        ("crest", (-0.10, 0.05, 1.77), (-0.95, 0.2, 0.5), 0.38, 2.8),
        ("crest2", (0.06, 0.17, 1.80), (0.62, 0.78, 0.4), 0.42, 2.6),
        ("crest3", (0.0, 0.21, 1.78), (0.0, 1.0, 0.5), 0.46, 2.6),
        ("crest3", (-0.06, 0.17, 1.80), (-0.62, 0.78, 0.4), 0.42, 2.6),
    ]
    radii = [0.046, 0.043, 0.039, 0.035, 0.032, 0.033]          # slightly bulbous tips, like the sheet
    skull_nodes = ["face", "brow", "crown", "crownB", "crest", "crest2", "crest3", "crest4", "nape"]

    def chain_points(root, start, d0, length, droop):
        d0 = Vector(d0).normalized()
        step = length / 5
        # first joint just OUTSIDE the parent node's radius (an edge starting inside its parent's hull tears holes)
        base = s.V[N[root]]
        out = (Vector(start) - base).normalized()
        p = base + out * (max(s.R[N[root]]) + 0.02)
        pts = [p.copy()]
        for k in range(1, 6):
            dk = (d0 + Vector((0, 0, -droop * k / 4))).normalized()
            p = p + dk * step
            pts.append(p.copy())
        return [pts[0]] + drape(pts, radii, colliders)[1:]

    attached = {}
    for root, start, d0, length, droop in tendrils:
        # try the intended skull node first, then the others nearest the start point; keep the first that stays sealed
        order = [root] + sorted((n for n in skull_nodes if n != root), key=lambda n: (s.V[N[n]] - Vector(start)).length)
        done = False
        for node in order:
            base = s.V[N[node]]
            for jit in (0.0, 12.0, -12.0, 25.0, -25.0):          # nudge the root direction around the node
                rot = Matrix.Rotation(math.radians(jit), 3, "Z")
                st = base + rot @ (Vector(start) - base)
                trial = Skel()
                trial.V, trial.E, trial.R = list(s.V), list(s.E), list(s.R)
                trial.chain(N[node], chain_points(node, st, rot @ Vector(d0), length, droop), radii)
                if nonmanifold_edges(trial, N["neck0"]) == 0:
                    s.V, s.E, s.R = trial.V, trial.E, trial.R
                    done = True
                    break
            if done:
                break
        if not done:
            print("tendril skipped (no clean attachment):", root, start)
    return s, N


def _hit_back(tree, x, z):
    loc, _, _, _ = tree.ray_cast(Vector((x, 3.0, z)), Vector((0, -1, 0)))
    return loc


def _aim(v):
    """Euler (degrees) that turns +Z onto direction v."""
    e = Vector((0, 0, 1)).rotation_difference(v.normalized()).to_euler()
    return tuple(math.degrees(a) for a in e)


def _strap(builder, mat, a, b, width=0.042, thick=0.014):
    """A flat strap between two points (in a vertical-ish plane)."""
    mid = (a + b) * 0.5
    builder.part(mat, mk_box((width, thick, (b - a).length), bevel=0.004, segs=1), loc=mid, rot=_aim(b - a))


def _cart(builder, mat, c, scale=1.0):
    """Shopping-cart emblem facing -Y at point c (basket, handle, wheels)."""
    s = scale
    builder.part(mat, mk_box((0.062 * s, 0.005, 0.036 * s), bevel=0.002, segs=1), loc=c + Vector((0.006 * s, 0, 0.004 * s)))
    builder.part(mat, mk_box((0.008 * s, 0.005, 0.03 * s), bevel=0.001, segs=1), loc=c + Vector((-0.03 * s, 0, 0.016 * s)), rot=(0, -25, 0))
    builder.part(mat, mk_box((0.022 * s, 0.005, 0.007 * s), bevel=0.001, segs=1), loc=c + Vector((-0.044 * s, 0, 0.03 * s)))
    builder.part(mat, mk_box((0.062 * s, 0.005, 0.006 * s), bevel=0.001, segs=1), loc=c + Vector((0.006 * s, 0, -0.02 * s)))
    for wx in (-0.016, 0.026):
        builder.part(mat, mk_cyl(0.008 * s, 0.006, 10), loc=c + Vector((wx * s, 0, -0.032 * s)), rot=(90, 0, 0))


def build_vendor():
    _vendor_materials()
    coll = collection("VENDOR")
    for o in [o for o in coll.objects if o.name.startswith("VENDOR_Tendril_") or o.name == "VENDOR_Gloves"]:
        m = o.data
        bpy.data.objects.remove(o)
        if m is not None and m.users == 0:
            bpy.data.meshes.remove(m)
    report = {}

    # ---------------------------------------------------------------- body (full; clothing is cut from it)
    skel, J = _body_skeleton()
    body = skin_mesh(skel, root=0)
    flatten_soles(body)

    def is_sleeve(c):
        return any(_arm_dist(c, J, t) < 0.17 and 0.0 < _arm_t(c, J, t) <= SLEEVE_T for t in ("L", "R"))

    def is_bare(c):          # forearm beyond the sleeve, and the hand
        return any(_arm_dist(c, J, t) < 0.24 and _arm_t(c, J, t) > SLEEVE_T - 0.03 for t in ("L", "R"))

    # ---------------------------------------------------------------- jacket, collar, cuffs
    jacket = region_shell(body, lambda c, n: (0.84 < c.z < 1.385 and abs(c.x) < 0.30 and not is_bare(c)) or is_sleeve(c), 0.024)
    jb = Builder()
    jb.part("VENDOR_MAT_Workwear", mk_mesh(jacket))
    bpy.data.meshes.remove(jacket)
    cb = bmesh.new()
    seg = 24
    rings = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        dv = Vector((math.sin(a) * 1.25, -math.cos(a), 0))
        rings.append([cb.verts.new(dv * 0.118 + Vector((0, 0.02, 1.33))), cb.verts.new(dv * 0.112 + Vector((0, 0.02, 1.405))),
                      cb.verts.new(dv * 0.090 + Vector((0, 0.02, 1.405))), cb.verts.new(dv * 0.092 + Vector((0, 0.02, 1.34)))])
    for i in range(seg):
        a_, b_ = rings[i], rings[(i + 1) % seg]
        for k in range(4):
            cb.faces.new((a_[k], b_[k], b_[(k + 1) % 4], a_[(k + 1) % 4]))
    cme = bpy.data.meshes.new("_vv_collar")
    cb.to_mesh(cme)
    cb.free()
    jb.part("VENDOR_MAT_Workwear", mk_mesh(cme))
    bpy.data.meshes.remove(cme)
    for tag in ("L", "R"):                                   # orange wrist cuffs where the sleeves end
        el, d2 = J["elbow_" + tag], J["fdir_" + tag]
        jb.part("VV_MAT_OrangePaint", mk_cyl(0.104, 0.075, 18), loc=el + d2 * 0.165, rot=_aim(d2))
        jb.part("VV_MAT_DarkMetal", mk_cyl(0.107, 0.018, 18), loc=el + d2 * 0.135, rot=_aim(d2))
    me = jb.mesh(); replace_object("VENDOR_Jacket", me, coll); report["VENDOR_Jacket"] = tris(me)

    # ---------------------------------------------------------------- trousers with knee pads
    trousers = region_shell(body, lambda c, n: 0.17 < c.z < 0.93 and not is_sleeve(c) and not is_bare(c), 0.016)
    tt = bvh([trousers])
    tb = Builder()
    tb.part("VENDOR_MAT_Workwear", mk_mesh(trousers))
    bpy.data.meshes.remove(trousers)
    for side in (1, -1):
        kp = hit_front(tt, side * 0.126, 0.47)
        tb.part("VV_MAT_CharcoalFabric", mk_box((0.15, 0.045, 0.14), bevel=0.02, segs=2), loc=kp + Vector((0, 0.008, 0.0)), rot=(-6, 0, 0))
    me = tb.mesh(); replace_object("VENDOR_Trousers", me, coll); report["VENDOR_Trousers"] = tris(me)

    # ---------------------------------------------------------------- boots
    boots_shell = region_shell(body, lambda c, n: c.z < 0.25, 0.032)
    for v in boots_shell.vertices:
        v.co.z = max(0.0, v.co.z)
    boots_shell.update()
    bb = Builder()
    bb.part("VV_MAT_CharcoalFabric", mk_mesh(boots_shell))
    bpy.data.meshes.remove(boots_shell)
    for side in (1, -1):
        bb.part("VV_MAT_DarkMetal", mk_box((0.20, 0.36, 0.045), bevel=0.014, segs=2), loc=(side * 0.134, -0.07, 0.0225))
        bb.part("VV_MAT_OrangePaint", mk_box((0.17, 0.10, 0.075), bevel=0.02, segs=2), loc=(side * 0.135, -0.205, 0.075), rot=(-18, 0, 0))
        bb.part("VV_MAT_OrangePaint", mk_box((0.16, 0.06, 0.09), bevel=0.016, segs=2), loc=(side * 0.13, 0.085, 0.08), rot=(10, 0, 0))
        bb.part("VV_MAT_DarkMetal", mk_box((0.18, 0.03, 0.05), bevel=0.008, segs=1), loc=(side * 0.13, -0.06, 0.20))
    me = bb.mesh(); replace_object("VENDOR_Boots", me, coll); report["VENDOR_Boots"] = tris(me)

    # ---------------------------------------------------------------- apron: bib, front pocket, straps, back cross + tie
    jt = bvh([bpy.data.objects["VENDOR_Jacket"].data, bpy.data.objects["VENDOR_Trousers"].data])
    ab = bmesh.new()
    cols, rows = 9, 15
    z_top, z_bot = 1.24, 0.46
    grid, hang_y = [], None
    for r in range(rows):
        z = z_top + (z_bot - z_top) * r / (rows - 1)
        half = 0.145 if z > 1.02 else (0.145 + (1.02 - z) / 0.10 * 0.085 if z > 0.92 else 0.23 + (0.92 - z) * 0.06)
        xs = [-half + 2 * half * c / (cols - 1) for c in range(cols)]
        if z >= 0.84:
            hits = [hit_front(jt, x, z) for x in xs]
            row_front = min(p.y for p in hits if p is not None)
        row = []
        for c, x in enumerate(xs):
            if z >= 0.84:
                p = hits[c]
                curve = row_front + 0.05 * (x / half) ** 2
                y = min(p.y if p is not None else curve, curve) - 0.014
            else:
                y = hang_y[c] - (0.84 - z) * 0.05
            row.append(ab.verts.new((x, y, z)))
        if z >= 0.84:
            hang_y = [v.co.y for v in row]
        grid.append(row)
    for r in range(rows - 1):
        for c in range(cols - 1):
            ab.faces.new((grid[r][c], grid[r][c + 1], grid[r + 1][c + 1], grid[r + 1][c]))
    ame = bpy.data.meshes.new("_vv_apron")
    ab.to_mesh(ame)
    ab.free()
    for p in ame.polygons:
        p.use_smooth = True
    ame = solidify(ame, 0.012, offset=1.0)
    at = bvh([ame])
    ap = Builder()
    ap.part("VENDOR_MAT_Apron", mk_mesh(ame))
    bpy.data.meshes.remove(ame)
    pk = hit_front(at, 0.0, 0.66)
    ap.part("VENDOR_MAT_Apron", mk_box((0.30, 0.012, 0.19), bevel=0.006, segs=1), loc=pk + Vector((0, -0.007, 0)))   # front pocket
    for side in (1, -1):                                      # front straps up over the shoulders
        top = hit_front(jt, side * 0.12, 1.24)
        _strap(ap, "VENDOR_MAT_Apron", top + Vector((0, -0.006, 0)), Vector((side * 0.13, 0.0, 1.345)))
    for side in (1, -1):                                      # back straps cross over the spine
        a = _hit_back(jt, side * 0.13, 1.30)
        b = _hit_back(jt, -side * 0.16, 0.93)
        lift = Vector((0, 0.016, 0))
        _strap(ap, "VENDOR_MAT_Apron", Vector((side * 0.13, 0.0, 1.345)), a + lift)
        _strap(ap, "VENDOR_MAT_Apron", a + lift, b + lift)
    bk = _hit_back(jt, 0.0, 0.83) + Vector((0, 0.03, 0))      # tie bow at the small of the back
    ap.part("VENDOR_MAT_Apron", mk_box((0.036, 0.034, 0.04), bevel=0.008, segs=1), loc=bk)
    for side in (1, -1):
        ap.part("VENDOR_MAT_Apron", mk_box((0.085, 0.026, 0.05), bevel=0.012, segs=2), loc=bk + Vector((side * 0.05, 0, 0.012)), rot=(0, side * -22, 0))
        ap.part("VENDOR_MAT_Apron", mk_box((0.036, 0.012, 0.17), bevel=0.004, segs=1), loc=bk + Vector((side * 0.03, 0.004, -0.095)), rot=(0, side * 12, 0))
    me = ap.mesh(); replace_object("VENDOR_Apron", me, coll); report["VENDOR_Apron"] = tris(me)

    # ---------------------------------------------------------------- shoulder pads
    pads = Builder()
    for side in (1, -1):
        sh = J["shoulder_" + ("L" if side > 0 else "R")]
        pads.part("VV_MAT_OrangePaint", mk_box((0.25, 0.26, 0.065), bevel=0.014, segs=1),
                  loc=sh + Vector((side * 0.035, 0.0, 0.07)), rot=(0, side * 30, 0))
        pads.part("VV_MAT_OrangePaint", mk_box((0.18, 0.24, 0.055), bevel=0.012, segs=1),
                  loc=sh + Vector((side * 0.13, 0.0, -0.015)), rot=(0, side * 56, 0))
        for oy in (-0.08, 0.08):
            pads.part("VV_MAT_IndustrialMetal", mk_cyl(0.011, 0.012, 8), loc=sh + Vector((side * 0.05, oy, 0.1)), rot=(0, side * 22, 0))
    me = pads.mesh(); replace_object("VENDOR_ShoulderPads", me, coll); report["VENDOR_ShoulderPads"] = tris(me)

    # ---------------------------------------------------------------- belt with buckle and pouches
    zc = 0.885
    band = [v.co for v in bpy.data.objects["VENDOR_Trousers"].data.vertices if abs(v.co.z - zc) < 0.035]
    seg = 32
    radius = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        dvec = Vector((math.sin(a), -math.cos(a), 0))
        best = max((p.x * dvec.x + p.y * dvec.y for p in band if Vector((p.x, p.y, 0)).normalized().dot(dvec) > 0.9), default=0.2)
        radius.append(best + 0.028)
    belt = bmesh.new()
    rings = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        dvec = Vector((math.sin(a), -math.cos(a), 0))
        ro, ri = radius[i], radius[i] - 0.02
        rings.append([belt.verts.new(dvec * ro + Vector((0, 0, zc + 0.04))), belt.verts.new(dvec * ro + Vector((0, 0, zc - 0.04))),
                      belt.verts.new(dvec * ri + Vector((0, 0, zc - 0.04))), belt.verts.new(dvec * ri + Vector((0, 0, zc + 0.04)))])
    for i in range(seg):
        a, b = rings[i], rings[(i + 1) % seg]
        for k in range(4):
            belt.faces.new((a[k], b[k], b[(k + 1) % 4], a[(k + 1) % 4]))
    bme = bpy.data.meshes.new("_vv_belt")
    belt.to_mesh(bme)
    belt.free()
    bl = Builder()
    bl.part("VV_MAT_CharcoalFabric", mk_mesh(bme))
    bpy.data.meshes.remove(bme)
    bl.part("VV_MAT_IndustrialMetal", mk_box((0.09, 0.025, 0.075), bevel=0.01, segs=1), loc=(0, -radius[0] - 0.006, zc))

    def on_belt(deg, push=0.0):
        i = int(round(deg / 360 * seg)) % seg
        a = 2 * math.pi * i / seg
        dvec = Vector((math.sin(a), -math.cos(a), 0))
        return dvec * (radius[i] + push) + Vector((0, 0, zc)), -math.degrees(a)

    for deg, size in ((62, (0.11, 0.065, 0.12)), (140, (0.13, 0.06, 0.10))):
        p, yaw = on_belt(deg, 0.035)
        bl.part("VV_MAT_WornFabric", mk_box(size, bevel=0.012, segs=2), loc=p + Vector((0, 0, -0.03)), rot=(0, 0, yaw))
        bl.part("VV_MAT_CharcoalFabric", mk_box((size[0] + 0.006, size[1] + 0.008, 0.035), bevel=0.008, segs=1),
                loc=p + Vector((0, 0, 0.03)), rot=(0, 0, yaw))
    me = bl.mesh(); replace_object("VENDOR_Belt", me, coll); report["VENDOR_Belt"] = tris(me)

    # ---------------------------------------------------------------- accessories
    ac = Builder()
    # Collar device with a cyan screen (front of the collar)
    ac.part("VV_MAT_DarkMetal", mk_box((0.12, 0.035, 0.058), bevel=0.01, segs=2), loc=(0, -0.112, 1.37), rot=(-8, 0, 0))
    ac.part("VV_MAT_EmissiveCyan", mk_box((0.08, 0.008, 0.03), bevel=0.003, segs=1), loc=(0, -0.131, 1.372), rot=(-8, 0, 0))
    # Shopping-cart emblems: on the bib and on the pocket
    eb = hit_front(at, 0.0, 1.13)
    _cart(ac, "VENDOR_MAT_Print", eb + Vector((0, -0.004, 0)), 1.0)
    _cart(ac, "VENDOR_MAT_Print", pk + Vector((0, -0.016, 0)), 1.3)
    # Vial pouch on the right hip (character's right = -X): two pink and one cyan product vial
    rp, ryaw = on_belt(282, 0.045)
    ac.part("VENDOR_MAT_Leather", mk_box((0.07, 0.14, 0.11), bevel=0.012, segs=2), loc=rp + Vector((0, 0, -0.05)), rot=(0, 0, ryaw))
    ac.part("VV_MAT_OrangePaint", mk_box((0.074, 0.144, 0.018), bevel=0.005, segs=1), loc=rp + Vector((0, 0, 0.008)), rot=(0, 0, ryaw))
    for k, liquid in enumerate(("VV_MAT_EmissiveMagenta", "VV_MAT_EmissiveCyan", "VV_MAT_EmissiveMagenta")):
        vp = rp + Vector((0.0, -0.04 + k * 0.04, 0.03 + 0.012 * (k % 2)))
        ac.part("VV_MAT_Glass", mk_cyl(0.016, 0.1, 10), loc=vp)
        ac.part(liquid, mk_cyl(0.012, 0.07, 10), loc=vp + Vector((0, 0, -0.01)))
        ac.part("VV_MAT_IndustrialMetal", mk_cyl(0.018, 0.016, 10), loc=vp + Vector((0, 0, 0.056)))
    # Price scanner clipped to the left hip
    lp, lyaw = on_belt(90, 0.04)
    ac.part("VV_MAT_DarkMetal", mk_box((0.045, 0.07, 0.13), bevel=0.01, segs=2), loc=lp + Vector((0, 0, -0.06)), rot=(0, 0, lyaw))
    ac.part("VV_MAT_EmissiveCyan", mk_box((0.006, 0.05, 0.05), bevel=0.002, segs=1), loc=lp + Vector((0.024, 0, -0.04)), rot=(0, 0, lyaw))
    ac.part("VV_MAT_OchrePaint", mk_box((0.05, 0.075, 0.03), bevel=0.006, segs=1), loc=lp + Vector((0, 0, -0.13)), rot=(0, 0, lyaw))
    me = ac.mesh(); replace_object("VENDOR_Accessories", me, coll); report["VENDOR_Accessories"] = tris(me)

    # ---------------------------------------------------------------- bare skin: forearms and hands
    skin = region_shell(body, lambda c, n: is_bare(c), 0.0)
    skin = decimate(skin, 0.6)
    skin.materials.append(bpy.data.materials["VENDOR_MAT_Skin"])
    bpy.data.meshes.remove(body)
    replace_object("VENDOR_Body", skin, coll); report["VENDOR_Body"] = tris(skin)

    # ---------------------------------------------------------------- head: neck + skull + muzzle + tendrils, one surface
    colliders = bvh([bpy.data.objects[n].data for n in ("VENDOR_ShoulderPads", "VENDOR_Jacket", "VENDOR_Apron", "VENDOR_Accessories")])
    hs, N = _head_skeleton(colliders)
    head = skin_mesh(hs, root=N["neck0"], subsurf=1, smooth=0.5)
    head.materials.append(bpy.data.materials["VENDOR_MAT_Skin"])
    tree = bvh([head])
    replace_object("VENDOR_Head", head, coll); report["VENDOR_Head"] = tris(head)

    # Eyes: almond, set wide on the face, under heavy half-lids; brows ride on the lids
    eyes, lids, brows = Builder(), Builder(), Builder()
    eye_r = 0.05
    for side in (1, -1):
        p = hit_front(tree, side * 0.056, 1.672)
        c = p + Vector((side * 0.002, eye_r * 0.6, 0))
        eyes.part("VENDOR_MAT_Sclera", mk_sphere(eye_r, 12, 8), loc=c, scale=(1.12, 1, 0.86))
        eyes.part("VENDOR_MAT_Iris", mk_sphere(eye_r * 0.7, 10, 6), loc=c + Vector((-side * 0.004, -eye_r * 0.72, -0.004)), scale=(1, 0.42, 0.95))
        eyes.part("VENDOR_MAT_Pupil", mk_sphere(eye_r * 0.32, 8, 5), loc=c + Vector((-side * 0.004, -eye_r * 0.97, -0.004)), scale=(0.8, 0.35, 1.0))

        def lid(bm, r=eye_r * 1.18):
            res = bmesh.ops.create_uvsphere(bm, u_segments=14, v_segments=9, radius=r)
            bmesh.ops.delete(bm, geom=[v for v in res["verts"] if v.co.z < -0.002], context="VERTS")
        lids.part("VENDOR_MAT_Skin", lid, loc=c, rot=(-16 if side > 0 else -8, 0, side * -8), scale=(1.12, 1, 0.9))
        roll = 16.0 if side > 0 else -10.0
        lift = 0.012 if side > 0 else -0.002
        brows.part("VENDOR_MAT_SkinDark", mk_box((eye_r * 2.1, 0.045, 0.028), bevel=0.012, segs=2),
                   loc=c + Vector((side * 0.006, -eye_r * 0.6, eye_r * 0.95 + lift)), rot=(-24, 0, side * roll))
    me = eyes.mesh(); replace_object("VENDOR_Eyes", me, coll); report["VENDOR_Eyes"] = tris(me)
    me = lids.mesh(); replace_object("VENDOR_Eyelids", me, coll); report["VENDOR_Eyelids"] = tris(me)
    me = brows.mesh(); replace_object("VENDOR_Brows", me, coll); report["VENDOR_Brows"] = tris(me)

    # Mouth: a small crooked smile on the muzzle, inset slot with two teeth
    mw, mz = 0.062, 1.567

    def mouth_top(u):
        return mz + 0.018 * u * u + 0.012 * u               # the left corner curls up

    mb = bmesh.new()
    n = 10
    front, back = [], []
    for i in range(n + 1):
        u = -1.0 + 2.0 * i / n
        x = u * mw
        zt = mouth_top(u)
        h = 0.005 + 0.02 * max(0.0, 1.0 - u * u) ** 0.7
        p = hit_front(tree, x, zt - h * 0.5)
        if p is None:                                            # ray grazed past the muzzle edge
            p = tree.find_nearest(Vector((x, -0.2, zt - h * 0.5)))[0]
        y = p.y - 0.003
        front.append((mb.verts.new((x, y, zt)), mb.verts.new((x, y, zt - h))))
        back.append((mb.verts.new((x, y + 0.03, zt - 0.002)), mb.verts.new((x, y + 0.03, zt - h + 0.002))))
    for i in range(n):
        (ft0, fb0), (ft1, fb1) = front[i], front[i + 1]
        (bt0, bb0), (bt1, bb1) = back[i], back[i + 1]
        mb.faces.new((ft0, ft1, bt1, bt0))
        mb.faces.new((fb1, fb0, bb0, bb1))
        mb.faces.new((bt0, bt1, bb1, bb0))
    mb.faces.new((front[0][0], back[0][0], back[0][1], front[0][1]))
    mb.faces.new((front[n][1], back[n][1], back[n][0], front[n][0]))
    mme = bpy.data.meshes.new("_vv_mouth")
    mb.to_mesh(mme)
    mb.free()
    mouth = Builder()
    mouth.part("VENDOR_MAT_Mouth", mk_mesh(mme))
    bpy.data.meshes.remove(mme)
    for tx in (-0.016, 0.02):
        zt = mouth_top(tx / mw)
        p = hit_front(tree, tx, zt - 0.008)
        mouth.part("VENDOR_MAT_Teeth", mk_box((0.02, 0.01, 0.016), bevel=0.003, segs=1), loc=(tx, p.y + 0.008, zt - 0.008))
    me = mouth.mesh(smooth=False); replace_object("VENDOR_Mouth", me, coll); report["VENDOR_Mouth"] = tris(me)

    report["TOTAL"] = sum(v for k, v in report.items())
    return report
