"""Builds VV_Human_Base (Stage 2 mannequin) in the VV_Presentation scene. Run after vv_lib.py in the same namespace."""
import math

import bmesh
import bpy
from mathutils import Vector


def build_human_base():
    coll = collection("BASE_Human")
    s = Skel()
    pelvis = s.v((0, 0.012, 0.95), (0.172, 0.128))
    waist = s.v((0, 0.004, 1.09), (0.150, 0.112)); s.e(pelvis, waist)
    chest = s.v((0, -0.012, 1.27), (0.190, 0.135)); s.e(waist, chest)
    upper = s.v((0, 0.0, 1.40), (0.165, 0.110)); s.e(chest, upper)
    neck = s.v((0, 0.014, 1.47), 0.062); s.e(upper, neck)
    neck_top = s.v((0, 0.012, 1.55), 0.058); s.e(neck, neck_top)
    for side in (1, -1):
        sh = s.v((side * 0.215, 0.012, 1.40), 0.082); s.e(upper, sh)
        d = Vector((side * math.sin(math.radians(45)), 0, -math.cos(math.radians(45))))
        mid_u = s.v(s.V[sh] + d * 0.13, 0.066); s.e(sh, mid_u)
        elbow = s.v(s.V[sh] + d * 0.28 + Vector((0, -0.02, 0)), 0.056); s.e(mid_u, elbow)
        mid_f = s.v(s.V[elbow] + d * 0.09 + Vector((0, -0.005, 0)), 0.060); s.e(elbow, mid_f)
        wrist = s.v(s.V[elbow] + d * 0.25 + Vector((0, -0.01, 0)), 0.042); s.e(mid_f, wrist)
        palm = s.v(s.V[wrist] + d * 0.075, (0.052, 0.036)); s.e(wrist, palm)
        for k, oy in enumerate((-0.036, -0.012, 0.012, 0.034)):
            length = (0.052, 0.058, 0.054, 0.044)[k]
            b0 = s.v(s.V[palm] + d * 0.045 + Vector((0, oy, 0)), 0.0165); s.e(palm, b0)
            b1 = s.v(s.V[b0] + d * length, 0.0155); s.e(b0, b1)
            b2 = s.v(s.V[b1] + d * length * 0.8, 0.0140); s.e(b1, b2)
        t0 = s.v(s.V[palm] + Vector((-side * 0.014, -0.045, 0.006)), 0.019); s.e(palm, t0)
        t1 = s.v(s.V[t0] + Vector((0, -0.032, 0)) + d * 0.028, 0.0165); s.e(t0, t1)
        t2 = s.v(s.V[t1] + Vector((0, -0.018, 0)) + d * 0.025, 0.0145); s.e(t1, t2)
        hip = s.v((side * 0.105, 0.012, 0.905), 0.108); s.e(pelvis, hip)
        thigh = s.v((side * 0.110, 0.0, 0.72), 0.098); s.e(hip, thigh)
        knee = s.v((side * 0.112, -0.014, 0.50), 0.074); s.e(thigh, knee)
        calf = s.v((side * 0.114, 0.012, 0.36), 0.078); s.e(knee, calf)
        ankle = s.v((side * 0.118, 0.018, 0.10), 0.054); s.e(calf, ankle)
        heel = s.v((side * 0.118, 0.045, 0.042), (0.050, 0.042)); s.e(ankle, heel)
        ball = s.v((side * 0.122, -0.10, 0.046), (0.066, 0.044)); s.e(ankle, ball)
        toe = s.v((side * 0.124, -0.185, 0.044), (0.058, 0.040)); s.e(ball, toe)
    body = skin_mesh(s, root=pelvis)
    flatten_soles(body)
    body.materials.append(bpy.data.materials["VV_PRES_Clay"])
    replace_object("BASE_Body", body, coll)

    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=4, use_grid_fill=True)
    for vt in bm.verts:
        vt.co = vt.co.normalized()
    for vt in bm.verts:
        x, y, z = vt.co
        jaw = 1.0 - 0.22 * max(0.0, -z) ** 1.3
        vt.co.x = x * 0.114 * jaw
        vt.co.y = y * (0.126 if y > 0 else 0.118) * (1.0 - 0.10 * max(0.0, -z))
        vt.co.z = z * 0.150
        d = (Vector((x, y, z)) - Vector((0, -1, -0.12))).length
        if d < 0.35:
            vt.co.y -= 0.02 * (1 - d / 0.35) ** 2
        d2 = (Vector((x * 0.6, y, z)) - Vector((0, -1, 0.28))).length
        if d2 < 0.3:
            vt.co.y -= 0.009 * (1 - d2 / 0.3)
    bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0.004, 1.65))
    for f in bm.faces:
        f.smooth = True
    hm = bpy.data.meshes.new("BASE_Head")
    bm.to_mesh(hm)
    bm.free()
    hm.materials.append(bpy.data.materials["VV_PRES_Clay"])
    replace_object("BASE_Head", hm, coll)
    return tris(body) + tris(hm)
