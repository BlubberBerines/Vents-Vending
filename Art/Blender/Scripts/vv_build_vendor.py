"""Builds NPC_Alien_Vendor_001 (Stage 3 static model) in the VV_Presentation scene.

Run after vv_lib.py in the same namespace. Character faces -Y; the character's LEFT side is +X.
Object names follow Docs/Art/CharacterStandards.md (VENDOR_*).
"""
import math

import bmesh
import bpy
from mathutils import Vector

ARM_ANGLE = 45.0
HEAD_SCALE = 1.15                                   # oversized head (brief §2.2)
HEAD_CENTRE = Vector((0, 0.008, 1.405 + 0.212 * HEAD_SCALE))   # chin stays on the collar


def _vendor_materials():
    material("VENDOR_MAT_Workwear", (0.075, 0.062, 0.095), rough=0.85)          # dark purple-grey workwear
    material("VENDOR_MAT_SkinDark", (0.12, 0.03, 0.19), rough=0.55)            # brows / markings
    material("VENDOR_MAT_Sclera", (0.95, 0.88, 0.62), rough=0.25)
    material("VENDOR_MAT_Iris", (1.0, 0.55, 0.05), rough=0.2, emit=(1.0, 0.45, 0.03), strength=0.6)
    material("VENDOR_MAT_Pupil", (0.01, 0.008, 0.012), rough=0.15)
    material("VENDOR_MAT_Mouth", (0.18, 0.02, 0.06), rough=0.6)
    material("VENDOR_MAT_Teeth", (0.85, 0.82, 0.7), rough=0.35)


def _body_skeleton():
    s = Skel()
    J = {}
    J["pelvis"] = s.v((0, 0.015, 0.88), (0.190, 0.140))
    J["belly"] = s.v((0, -0.012, 1.00), (0.195, 0.155)); s.e(J["pelvis"], J["belly"])
    J["chest"] = s.v((0, -0.008, 1.16), (0.240, 0.160)); s.e(J["belly"], J["chest"])
    J["upper"] = s.v((0, 0.006, 1.29), (0.215, 0.130)); s.e(J["chest"], J["upper"])
    J["neck"] = s.v((0, 0.02, 1.36), 0.078); s.e(J["upper"], J["neck"])
    J["neck_top"] = s.v((0, 0.022, 1.43), 0.072); s.e(J["neck"], J["neck_top"])
    for side, tag in ((1, "L"), (-1, "R")):
        d = Vector((side * math.sin(math.radians(ARM_ANGLE)), 0, -math.cos(math.radians(ARM_ANGLE))))
        sh = s.v((side * 0.258, 0.012, 1.30), 0.100); s.e(J["upper"], sh)
        mu = s.v(s.V[sh] + d * 0.14, 0.086); s.e(sh, mu)
        el = s.v(s.V[sh] + d * 0.30 + Vector((0, -0.02, 0)), 0.072); s.e(mu, el)
        mf = s.v(s.V[el] + d * 0.10 + Vector((0, -0.006, 0)), 0.080); s.e(el, mf)
        wr = s.v(s.V[el] + d * 0.27 + Vector((0, -0.012, 0)), 0.056); s.e(mf, wr)
        palm = s.v(s.V[wr] + d * 0.09, (0.070, 0.050)); s.e(wr, palm)
        for k, oy in enumerate((-0.046, -0.016, 0.014, 0.042)):
            ln = (0.062, 0.070, 0.066, 0.054)[k]
            b0 = s.v(s.V[palm] + d * 0.055 + Vector((0, oy, 0)), 0.022); s.e(palm, b0)
            b1 = s.v(s.V[b0] + d * ln, 0.0205); s.e(b0, b1)
            b2 = s.v(s.V[b1] + d * ln * 0.8, 0.019); s.e(b1, b2)
        t0 = s.v(s.V[palm] + Vector((-side * 0.018, -0.058, 0.008)), 0.026); s.e(palm, t0)
        t1 = s.v(s.V[t0] + Vector((0, -0.04, 0)) + d * 0.034, 0.023); s.e(t0, t1)
        t2 = s.v(s.V[t1] + Vector((0, -0.022, 0)) + d * 0.03, 0.020); s.e(t1, t2)
        J["shoulder_" + tag], J["wrist_" + tag], J["dir_" + tag] = s.V[sh], s.V[wr], d
        hip = s.v((side * 0.118, 0.015, 0.83), 0.125); s.e(J["pelvis"], hip)
        th = s.v((side * 0.124, 0.0, 0.66), 0.112); s.e(hip, th)
        kn = s.v((side * 0.126, -0.012, 0.47), 0.086); s.e(th, kn)
        ca = s.v((side * 0.128, 0.012, 0.32), 0.090); s.e(kn, ca)
        an = s.v((side * 0.130, 0.018, 0.105), 0.062); s.e(ca, an)
        he = s.v((side * 0.130, 0.05, 0.045), (0.056, 0.046)); s.e(an, he)
        ba = s.v((side * 0.134, -0.105, 0.05), (0.074, 0.048)); s.e(an, ba)
        to = s.v((side * 0.136, -0.195, 0.046), (0.066, 0.044)); s.e(ba, to)
    return s, J


def _arm_t(c, J, tag):
    """Distance of point c along the arm from the shoulder (negative = towards the torso)."""
    return (c - J["shoulder_" + tag]).dot(J["dir_" + tag])


def _arm_dist(c, J, tag):
    sh, d = J["shoulder_" + tag], J["dir_" + tag]
    t = (c - sh).dot(d)
    return (c - (sh + d * t)).length


def _head_mesh():
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=6, use_grid_fill=True)
    for vt in bm.verts:
        vt.co = vt.co.normalized()
    for vt in bm.verts:
        x, y, z = vt.co
        fx = 1.0 + 0.12 * math.exp(-((z + 0.25) / 0.4) ** 2)        # full cheeks
        if z < -0.55:
            fx *= 1.0 - 0.38 * (-z - 0.55) / 0.45                     # narrow chin
        fy = 0.86 if y < 0 else 1.06                                  # flatter face, fuller back of the skull
        if z > 0.35:
            fy *= 1.0 - 0.10 * (z - 0.35)
        fz = 1.0 - 0.10 * max(0.0, z) ** 2                            # slightly flattened dome
        vt.co = Vector((x * 0.192 * fx, y * 0.178 * fy, z * 0.212 * fz))
        # brow shelf and a broad, low nose
        d = (Vector((x * 0.7, y, z)) - Vector((0, -1, 0.22))).length
        if d < 0.38:
            vt.co.y -= 0.016 * (1 - d / 0.38)
        dn = (Vector((x, y, z)) - Vector((0, -1, -0.18))).length
        if dn < 0.30:
            vt.co.y -= 0.03 * (1 - dn / 0.30) ** 2
        vt.co *= HEAD_SCALE
    bmesh.ops.translate(bm, verts=bm.verts, vec=HEAD_CENTRE)
    for f in bm.faces:
        f.smooth = True
    me = bpy.data.meshes.new("_vv_head")
    bm.to_mesh(me)
    bm.free()
    return me


def build_vendor():
    _vendor_materials()
    coll = collection("VENDOR")
    report = {}

    # ---------------------------------------------------------------- body (purple skin, full, under the clothes)
    skel, J = _body_skeleton()
    body = skin_mesh(skel, root=0)
    flatten_soles(body)
    body.materials.append(bpy.data.materials["VV_MAT_AlienSkin"])
    replace_object("VENDOR_Body", body, coll)
    report["VENDOR_Body"] = tris(body)

    def is_hand(c):
        return any(_arm_dist(c, J, t) < 0.2 and _arm_t(c, J, t) > 0.585 for t in ("L", "R"))

    def is_arm(c):
        return any(_arm_dist(c, J, t) < 0.16 and 0.0 < _arm_t(c, J, t) <= 0.585 for t in ("L", "R"))

    # ---------------------------------------------------------------- head, face
    head = _head_mesh()
    head.materials.append(bpy.data.materials["VV_MAT_AlienSkin"])
    tree = bvh([head])
    replace_object("VENDOR_Head", head, coll)
    report["VENDOR_Head"] = tris(head)

    eyes = Builder()
    lids = Builder()
    brows = Builder()
    S, HC = HEAD_SCALE, HEAD_CENTRE
    eye_r = 0.068 * S
    for side in (1, -1):
        x, z = side * 0.088 * S, HC.z + 0.05 * S
        p = hit_front(tree, x, z)
        c = p + Vector((0, eye_r * 0.45, 0))
        eyes.part("VENDOR_MAT_Sclera", mk_sphere(eye_r, 12, 8), loc=c)
        eyes.part("VENDOR_MAT_Iris", mk_sphere(eye_r * 0.66, 10, 6), loc=c + Vector((0, -eye_r * 0.74, -eye_r * 0.06)), scale=(1, 0.42, 0.95))
        eyes.part("VENDOR_MAT_Pupil", mk_sphere(eye_r * 0.32, 8, 5), loc=c + Vector((0, -eye_r * 0.98, -eye_r * 0.06)), scale=(0.75, 0.35, 1.15))

        # Upper lid: top hemisphere shell (a little larger than the eye, so nothing pokes through) tilted
        # down over the top ~40% of the eye for a sly half-lidded look
        def lid(bm, r=eye_r * 1.16):
            res = bmesh.ops.create_uvsphere(bm, u_segments=14, v_segments=9, radius=r)
            low = [v for v in res["verts"] if v.co.z < -0.002]
            bmesh.ops.delete(bm, geom=low, context="VERTS")
        tilt = -14.0 if side > 0 else -6.0                            # left lid slightly higher: one eye more open
        lids.part("VV_MAT_AlienSkin", lid, loc=c, rot=(tilt, 0, 0))

        # Brows: chunky ridges sunk into the brow shelf; the left one raised (mischief), the right one lowered (suspicion)
        # Brows ride on the lid tops so they move with the eye area: left raised and arched, right low and flat
        roll = 18.0 if side > 0 else -10.0
        lift = 0.016 if side > 0 else -0.002
        bpos = c + Vector((0, -eye_r * 0.62, eye_r * 1.02 + lift))
        brows.part("VENDOR_MAT_SkinDark", mk_box((eye_r * 1.95, 0.05, 0.032), bevel=0.013, segs=2),
                   loc=bpos, rot=(-24, 0, side * roll))
    me = eyes.mesh(); replace_object("VENDOR_Eyes", me, coll); report["VENDOR_Eyes"] = tris(me)
    me = lids.mesh(); replace_object("VENDOR_Eyelids", me, coll); report["VENDOR_Eyelids"] = tris(me)
    me = brows.mesh(); replace_object("VENDOR_Brows", me, coll); report["VENDOR_Brows"] = tris(me)

    # Mouth: a wide crooked slot (left corner higher), inset into the face, with two teeth
    mb = bmesh.new()
    n = 14
    front, back = [], []
    mw, mz = 0.165 * S, HC.z - 0.105 * S                        # wide, low mouth

    def mouth_top(u):
        return mz + 0.05 * u * u + 0.03 * u                         # crooked: the left corner curls higher

    for i in range(n + 1):
        u = -1.0 + 2.0 * i / n
        x = u * mw
        zt = mouth_top(u)
        h = 0.008 + 0.058 * max(0.0, 1.0 - u * u) ** 0.7
        p = hit_front(tree, x, zt - h * 0.5)
        y = p.y - 0.004                                              # rim just proud of the skin so the opening reads
        front.append((mb.verts.new((x, y, zt)), mb.verts.new((x, y, zt - h))))
        back.append((mb.verts.new((x, y + 0.045, zt - 0.002)), mb.verts.new((x, y + 0.045, zt - h + 0.002))))
    for i in range(n):
        (ft0, fb0), (ft1, fb1) = front[i], front[i + 1]
        (bt0, bb0), (bt1, bb1) = back[i], back[i + 1]
        mb.faces.new((ft0, ft1, bt1, bt0))      # roof
        mb.faces.new((fb1, fb0, bb0, bb1))      # floor
        mb.faces.new((bt0, bt1, bb1, bb0))      # back wall
    mb.faces.new((front[0][0], back[0][0], back[0][1], front[0][1]))
    mb.faces.new((front[n][1], back[n][1], back[n][0], front[n][0]))
    mme = bpy.data.meshes.new("_vv_mouth")
    mb.to_mesh(mme)
    mb.free()
    mouth = Builder()
    mouth.part("VENDOR_MAT_Mouth", mk_mesh(mme))
    bpy.data.meshes.remove(mme)
    for tx in (-0.034, 0.038):
        zt = mouth_top(tx / mw)
        p = hit_front(tree, tx, zt - 0.012)
        mouth.part("VENDOR_MAT_Teeth", mk_box((0.04, 0.016, 0.03), bevel=0.006, segs=1),
                   loc=(tx, p.y + 0.012, zt - 0.015))
    me = mouth.mesh(smooth=False); replace_object("VENDOR_Mouth", me, coll); report["VENDOR_Mouth"] = tris(me)

    # Tendrils: five thick dangling head-tails from the back and sides of the skull
    # (azimuth from straight back, root height above head centre, length); side pair sweeps forward over the shoulders
    specs = [(-108, 0.05, 0.40), (-52, 0.12, 0.48), (0, 0.15, 0.54), (52, 0.12, 0.48), (108, 0.05, 0.40)]
    centre = HC
    for i, (az, dz, length) in enumerate(specs, 1):
        zr = HC.z + dz * S
        a = math.radians(az)
        out = Vector((math.sin(a), math.cos(a), 0.0))               # az 0 = straight back (+Y)
        loc, _, _, _ = tree.ray_cast(Vector((centre.x, centre.y, zr)), out)
        root = loc - out * 0.02
        t = Skel()
        pts = [root,
               root + out * 0.07 + Vector((0, 0, -0.01)),
               root + out * 0.115 + Vector((0, 0, -0.10)),
               root + out * 0.13 + Vector((0, 0, -0.10 - length * 0.35)),
               root + out * 0.12 + Vector((0, 0, -0.10 - length * 0.65)),
               root + out * 0.085 + Vector((0, -0.01, -0.10 - length * 0.9))]
        t.chain(None, pts, [0.060, 0.056, 0.050, 0.042, 0.032, 0.020])
        tm = skin_mesh(t, root=0, subsurf=1, smooth=0.0)
        tm.materials.append(bpy.data.materials["VV_MAT_AlienSkin"])
        name = f"VENDOR_Tendril_{i:02d}"
        replace_object(name, tm, coll)
        report[name] = tris(tm)

    # ---------------------------------------------------------------- clothing shells from the body
    jacket = region_shell(body, lambda c, n: (
        (0.84 < c.z < 1.385 and abs(c.x) < 0.30 and not is_hand(c)) or is_arm(c)), 0.024)
    jb = Builder()
    jb.part("VENDOR_MAT_Workwear", mk_mesh(jacket))
    bpy.data.meshes.remove(jacket)
    # Stand-up collar: a flared ring around the neck base
    cb = bmesh.new()
    seg = 24
    rings = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        dv = Vector((math.sin(a) * 1.25, -math.cos(a), 0))
        rings.append([cb.verts.new(dv * 0.118 + Vector((0, 0.02, 1.33))), cb.verts.new(dv * 0.112 + Vector((0, 0.02, 1.405))),
                      cb.verts.new(dv * 0.084 + Vector((0, 0.02, 1.405))), cb.verts.new(dv * 0.086 + Vector((0, 0.02, 1.34)))])
    for i in range(seg):
        a_, b_ = rings[i], rings[(i + 1) % seg]
        for k in range(4):
            cb.faces.new((a_[k], b_[k], b_[(k + 1) % 4], a_[(k + 1) % 4]))
    cme = bpy.data.meshes.new("_vv_collar")
    cb.to_mesh(cme)
    cb.free()
    jb.part("VENDOR_MAT_Workwear", mk_mesh(cme))
    bpy.data.meshes.remove(cme)
    me = jb.mesh(); replace_object("VENDOR_Jacket", me, coll); report["VENDOR_Jacket"] = tris(me)

    trousers = region_shell(body, lambda c, n: 0.17 < c.z < 0.93 and not is_arm(c) and not is_hand(c), 0.016)
    trousers.materials.append(bpy.data.materials["VENDOR_MAT_Workwear"])
    replace_object("VENDOR_Trousers", trousers, coll); report["VENDOR_Trousers"] = tris(trousers)

    gloves = region_shell(body, lambda c, n: any(_arm_dist(c, J, t) < 0.2 and _arm_t(c, J, t) > 0.55 for t in ("L", "R")), 0.011)
    gloves.materials.append(bpy.data.materials["VV_MAT_CharcoalFabric"])
    replace_object("VENDOR_Gloves", gloves, coll); report["VENDOR_Gloves"] = tris(gloves)

    boots_shell = region_shell(body, lambda c, n: c.z < 0.25, 0.032)
    flatten_soles(boots_shell, lo=0.0, hi=0.0001)
    for v in boots_shell.vertices:
        v.co.z = max(0.0, v.co.z)
    boots_shell.update()
    bb = Builder()
    bb.part("VV_MAT_CharcoalFabric", mk_mesh(boots_shell))
    bpy.data.meshes.remove(boots_shell)
    for side in (1, -1):
        bb.part("VV_MAT_DarkMetal", mk_box((0.20, 0.36, 0.045), bevel=0.014, segs=2), loc=(side * 0.134, -0.07, 0.0225))
        bb.part("VV_MAT_OrangePaint", mk_box((0.17, 0.10, 0.075), bevel=0.02, segs=2), loc=(side * 0.135, -0.205, 0.075), rot=(-18, 0, 0))
        bb.part("VV_MAT_DarkMetal", mk_box((0.18, 0.03, 0.05), bevel=0.008, segs=1), loc=(side * 0.13, -0.06, 0.20))   # strap
    me = bb.mesh(); replace_object("VENDOR_Boots", me, coll); report["VENDOR_Boots"] = tris(me)

    # ---------------------------------------------------------------- apron (worn beige bib apron)
    jt = bvh([bpy.data.objects["VENDOR_Jacket"].data, bpy.data.objects["VENDOR_Trousers"].data])
    ab = bmesh.new()
    cols, rows = 9, 15
    z_top, z_bot = 1.24, 0.46
    grid = []
    hang_y = None
    for r in range(rows):
        z = z_top + (z_bot - z_top) * r / (rows - 1)
        if z > 1.02:
            half = 0.145
        elif z > 0.92:
            half = 0.145 + (1.02 - z) / 0.10 * 0.085
        else:
            half = 0.23 + (0.92 - z) * 0.06
        row = []
        xs = [-half + 2 * half * c / (cols - 1) for c in range(cols)]
        if z >= 0.84:
            hits = [hit_front(jt, x, z) for x in xs]
            row_front = min(p.y for p in hits if p is not None)
        for c, x in enumerate(xs):
            if z >= 0.84:
                # follow the body, but never wrap round the sides: at most a gentle curve back from the front
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
    ap = Builder()
    ap.part("VV_MAT_WornFabric", mk_mesh(ame))
    bpy.data.meshes.remove(ame)
    # neck straps from the bib corners over the collar
    for side in (1, -1):
        top = hit_front(jt, side * 0.12, 1.24)
        ap.part("VV_MAT_WornFabric", mk_box((0.035, 0.02, 0.17), bevel=0.006, segs=1),
                loc=(side * 0.105, top.y + 0.02, 1.32), rot=(-28, 0, side * 14))
    me = ap.mesh(); replace_object("VENDOR_Apron", me, coll); report["VENDOR_Apron"] = tris(me)

    # ---------------------------------------------------------------- shoulder pads (orange, two plates each)
    pads = Builder()
    for side in (1, -1):
        sh = J["shoulder_" + ("L" if side > 0 else "R")]
        # Angular painted plates (flat tops, small bevels) with a dark rim strip and two bolts
        pads.part("VV_MAT_OrangePaint", mk_box((0.25, 0.26, 0.065), bevel=0.014, segs=1),
                  loc=sh + Vector((side * 0.035, 0.0, 0.07)), rot=(0, side * 30, 0))
        pads.part("VV_MAT_OrangePaint", mk_box((0.18, 0.24, 0.055), bevel=0.012, segs=1),
                  loc=sh + Vector((side * 0.13, 0.0, -0.015)), rot=(0, side * 56, 0))
        for oy in (-0.08, 0.08):
            pads.part("VV_MAT_IndustrialMetal", mk_cyl(0.011, 0.012, 8),
                      loc=sh + Vector((side * 0.05, oy, 0.1)), rot=(0, side * 22, 0))
    me = pads.mesh(); replace_object("VENDOR_ShoulderPads", me, coll); report["VENDOR_ShoulderPads"] = tris(me)

    # ---------------------------------------------------------------- belt with buckle and pouches
    zc = 0.885
    band = [v.co for v in bpy.data.objects["VENDOR_Trousers"].data.vertices if abs(v.co.z - zc) < 0.035]
    seg = 32
    radius = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        dvec = Vector((math.sin(a), -math.cos(a), 0))               # i = 0 is the front (-Y)
        best = max((p.x * dvec.x + p.y * dvec.y for p in band if (Vector((p.x, p.y, 0)).normalized().dot(dvec) > 0.9)), default=0.2)
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

    for deg, size in ((62, (0.11, 0.065, 0.12)), (298, (0.11, 0.065, 0.12)), (180, (0.15, 0.06, 0.10))):
        p, yaw = on_belt(deg, 0.035)
        bl.part("VV_MAT_WornFabric", mk_box(size, bevel=0.012, segs=2), loc=p + Vector((0, 0, -0.03)), rot=(0, 0, yaw))
        bl.part("VV_MAT_CharcoalFabric", mk_box((size[0] + 0.006, size[1] + 0.008, 0.035), bevel=0.008, segs=1),
                loc=p + Vector((0, 0, 0.03)), rot=(0, 0, yaw))
    me = bl.mesh(); replace_object("VENDOR_Belt", me, coll); report["VENDOR_Belt"] = tris(me)

    # ---------------------------------------------------------------- accessories
    ac = Builder()
    # Cyan chest device on the left chest
    p = hit_front(jt, 0.19, 1.19)
    ac.part("VV_MAT_DarkMetal", mk_box((0.11, 0.04, 0.085), bevel=0.012, segs=2), loc=p + Vector((0, -0.012, 0)), rot=(-6, 0, 12))
    ac.part("VV_MAT_EmissiveCyan", mk_box((0.075, 0.01, 0.05), bevel=0.004, segs=1), loc=p + Vector((-0.004, -0.034, 0.004)), rot=(-6, 0, 12))
    for k in range(3):
        ac.part("VV_MAT_OrangePaint", mk_cyl(0.007, 0.012, 8), loc=p + Vector((0.035 - k * 0.02, -0.03, -0.034)), rot=(90, 0, 12))
    # Shop emblem on the apron bib: orange disc with a dark diamond
    ep = hit_front(bvh([bpy.data.objects["VENDOR_Apron"].data]), 0.0, 1.14)
    ac.part("VV_MAT_OrangePaint", mk_cyl(0.036, 0.008, 20), loc=ep + Vector((0, -0.004, 0)), rot=(90, 0, 0))
    ac.part("VV_MAT_DarkMetal", mk_box((0.034, 0.006, 0.034), bevel=0.003, segs=1), loc=ep + Vector((0, -0.009, 0)), rot=(0, 45, 0))
    # Vial rack on the right hip (character's right = -X): three magenta product vials
    rp, ryaw = on_belt(270, 0.03)
    ac.part("VV_MAT_DarkMetal", mk_box((0.035, 0.13, 0.05), bevel=0.008, segs=1), loc=rp + Vector((0, 0, -0.035)), rot=(0, 0, ryaw))
    for k in range(3):
        vp = rp + Vector((-0.012, -0.042 + k * 0.042, 0.0))
        ac.part("VV_MAT_Glass", mk_cyl(0.016, 0.09, 10), loc=vp)
        ac.part("VV_MAT_EmissiveMagenta", mk_cyl(0.012, 0.06, 10), loc=vp + Vector((0, 0, -0.01)))
        ac.part("VV_MAT_IndustrialMetal", mk_cyl(0.018, 0.016, 10), loc=vp + Vector((0, 0, 0.052)))
    # Price scanner clipped to the left hip
    lp, lyaw = on_belt(90, 0.04)
    ac.part("VV_MAT_DarkMetal", mk_box((0.045, 0.07, 0.13), bevel=0.01, segs=2), loc=lp + Vector((0, 0, -0.06)), rot=(0, 0, lyaw))
    ac.part("VV_MAT_EmissiveCyan", mk_box((0.006, 0.05, 0.05), bevel=0.002, segs=1), loc=lp + Vector((0.024, 0, -0.04)), rot=(0, 0, lyaw))
    ac.part("VV_MAT_OchrePaint", mk_box((0.05, 0.075, 0.03), bevel=0.006, segs=1), loc=lp + Vector((0, 0, -0.13)), rot=(0, 0, lyaw))
    me = ac.mesh(); replace_object("VENDOR_Accessories", me, coll); report["VENDOR_Accessories"] = tris(me)

    report["TOTAL"] = sum(v for k, v in report.items())
    report["TOTAL_visible_est"] = report["TOTAL"] - report["VENDOR_Body"]
    return report
