"""Coordinate-descent fit of the vendor's tentacle control points and skull radii against the head reference gate.

Run live in Blender after exec'ing vv_lib, vv_materials, vv_build_vendor and vv_head_gate into one namespace `ns`
(and after one full build_vendor(), so the colliders exist). State persists in bpy.app.driver_namespace["vv_fit"],
so a sweep can be split over several calls:  fit_init(ns); fit_sweep(ns, step); fit_dump(ns)
The result is printed as Python literals to paste back into vv_build_vendor.py; always check the shapes visually.
"""
import bpy
from mathutils import Vector

KEYS = ("_T1", "_T2", "_T5", "_T3")


def _params(ns):
    tent = ns["TENTACLES"]
    t3 = [list(p) for p in ns["_T3"]]
    return {"_T1": [list(p) for p in ns["_T1"]], "_T2": [list(p) for p in ns["_T2"]],
            "_T5": [list(p) for p in ns["_T5"]], "_T3": t3,
            "R": [ns["HEAD_R"].x, ns["HEAD_R"].y, ns["HEAD_R"].z],
            "rad": [t[1] for t in tent]}


def _apply(ns, P):
    ns["HEAD_R"] = Vector(P["R"])
    t1, t2, t5, t3 = ([tuple(p) for p in P[k]] for k in KEYS)
    r = P["rad"]
    ns["TENTACLES"][:] = [(t1, r[0]), (t2, r[1]), (t3, r[2]), (ns["_mirror"](t2), r[3]), (t5, r[4])]
    coll = bpy.data.objects["VENDOR_Head"].users_collection[0]
    colliders = ns["bvh"]([bpy.data.objects[n].data for n in ("VENDOR_ShoulderPads", "VENDOR_Jacket", "VENDOR_Apron", "VENDOR_Accessories")])
    head = ns["_head_mesh"]()
    head.materials.append(bpy.data.materials["VENDOR_MAT_Skin"])
    tree = ns["bvh"]([head])
    ns["replace_object"]("VENDOR_Head", head, coll)
    neck = ns["_neck_mesh"](tree)
    neck.materials.append(bpy.data.materials["VENDOR_MAT_Skin"])
    ns["replace_object"]("VENDOR_Neck", neck, coll)
    for i, spec in enumerate(ns["TENTACLES"], 1):
        tm = ns["_tentacle_mesh"](tree, colliders, *spec)
        tm.materials.append(bpy.data.materials["VENDOR_MAT_Skin"])
        ns["replace_object"](f"VENDOR_Tendril_{i:02d}", tm, coll)


K_MAX = 14.0   # 1/m: bends tighter than ~7 cm radius read as kinks on a 4-7 cm thick tentacle


def _score(ns, P, res=300):
    ns["TENTACLE_CENTRES"].clear()
    _apply(ns, P)
    r = ns["run"](res=res, save_sheet=False)
    kink = sum(max(0.0, ns["max_curvature"](c) - K_MAX) for c in ns["TENTACLE_CENTRES"])
    r["kink"] = round(kink, 2)
    return r["mean_iou"] - 0.25 * (r["mean_iou"] - min(r[v]["iou"] for v in ns["VIEWS"])) - 0.002 * kink, r


def fit_init(ns):
    P = _params(ns)
    s, r = _score(ns, P)
    bpy.app.driver_namespace["vv_fit"] = {"P": P, "s": s, "r": r, "i": 0}
    return s, r


def _coords(P):
    out = []
    for k in KEYS:
        for i in range(1, len(P[k])):          # root point (index 0) stays on the skull
            for a in range(3):
                if k == "_T3" and a == 0:
                    continue                    # the central arch stays on the midline
                if k in ("_T1", "_T5") and a == 0 and i >= 2:
                    continue                    # 1 and 5 hang beside the cheeks (front + rear views), x held
                out.append((k, i, a))
    out += [("R", None, a) for a in range(3)]
    out += [("rad", None, a) for a in range(5)]
    return out


def fit_sweep(ns, step=0.02, budget=40):
    """Try +/-step on up to `budget` coordinates (continuing where the last call stopped); keep improvements."""
    st = bpy.app.driver_namespace["vv_fit"]
    P, best = st["P"], st["s"]
    coords = _coords(P)
    for _ in range(budget):
        k, i, a = coords[st["i"] % len(coords)]
        st["i"] += 1
        d = step * (0.25 if k in ("R", "rad") else 1.0)
        for sgn in (1, -1):
            Q = {kk: [list(x) if isinstance(x, list) else x for x in vv] for kk, vv in P.items()}
            if i is None:
                Q[k][a] += sgn * d
            else:
                Q[k][i][a] += sgn * d
            s, r = _score(ns, Q)
            if s > best + 1e-4:
                P, best = Q, s
                st["r"] = r
                break
    st["P"], st["s"] = P, best
    _apply(ns, P)
    return best, st["i"], len(coords)


def fit_dump(ns):
    st = bpy.app.driver_namespace["vv_fit"]
    P = st["P"]
    rnd = lambda pts: [tuple(round(c, 3) for c in p) for p in pts]
    return {k: rnd(P[k]) for k in KEYS} | {"R": [round(x, 4) for x in P["R"]], "rad": [round(x, 4) for x in P["rad"]],
                                             "score": round(st["s"], 4)}
