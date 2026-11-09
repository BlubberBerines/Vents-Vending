"""Reference gate for the vendor head (blender-image-to-3d protocol, run live in Blender).

Renders orthographic silhouettes of head + neck + tentacles + face parts (frame bottom on the collar top),
fills enclosed holes like the reference mattes, aligns each to its reference matte by bounding box and reports
IoU plus 10-band width differences (render minus reference, as a fraction of the reference width).
Reference mattes: Art/Blender/Characters/AlienVendor/ref/head_<view>.png (alpha = subject above the collar).
Needs Pillow on sys.path (C:/Users/Jarro/.vv-tools/pylib).
"""
import math
import os
import sys
from collections import deque

import bpy
import numpy as np

sys.path.insert(0, r"C:\Users\Jarro\.vv-tools\pylib")
from PIL import Image  # noqa: E402

ROOT = r"F:\Vents & Vending\Art\Blender\Characters\AlienVendor"
PARTS = ("VENDOR_Head", "VENDOR_Neck", "VENDOR_Tendril", "VENDOR_Eyes", "VENDOR_Eyelids", "VENDOR_Brows", "VENDOR_Mouth")
COLLAR_TOP = 1.405
VIEWS = {  # camera azimuth (degrees, 0 = front), matching the sheet's panels
    "front": 0.0, "threequarter": -55.0,  # 3/4 panel azimuth calibrated by sweep (best fit -55)
    "side": 90.0, "rear": 180.0,
}


def _fill(mask):
    h, w = mask.shape
    reach = np.zeros_like(mask)
    q = deque((y, x) for y in range(h) for x in (0, w - 1) if not mask[y, x])
    q.extend((y, x) for x in range(w) for y in (0, h - 1) if not mask[y, x])
    for y, x in q:
        reach[y, x] = True
    while q:
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and not mask[ny, nx] and not reach[ny, nx]:
                reach[ny, nx] = True
                q.append((ny, nx))
    return ~reach


def _norm(mask, size=256):
    ys, xs = np.where(mask)
    crop = mask[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    img = Image.fromarray((crop * 255).astype(np.uint8)).resize((size, size), Image.NEAREST)
    aspect = crop.shape[1] / crop.shape[0]
    return np.asarray(img) > 127, aspect


def _measure(ref_mask, ren_mask):
    a, ra = _norm(ref_mask)
    b, rb = _norm(ren_mask)
    iou = (a & b).sum() / max(1, (a | b).sum())
    bands = []
    for i in range(10):
        rows = slice(i * 25, (i + 1) * 25)
        wa = a[rows].sum(axis=1).mean()
        wb = b[rows].sum(axis=1).mean()
        bands.append(round((wb - wa) / max(1.0, wa), 3))
    return round(float(iou), 3), round(rb - ra, 3), bands


def run(res=600, ortho=0.95, save_sheet=True):
    sc = bpy.data.scenes["VV_Presentation"]
    out_dir = os.path.join(ROOT, "review", "head")
    os.makedirs(out_dir, exist_ok=True)
    keep = {n for n in bpy.data.objects.keys() if n.startswith(PARTS)}
    saved = {o.name: o.hide_render for o in sc.objects}
    r = sc.render
    old = (r.engine, r.film_transparent, r.resolution_x, r.resolution_y, sc.camera, r.filepath, r.image_settings.color_mode)
    cd = bpy.data.cameras.get("CAM_RefOrtho") or bpy.data.cameras.new("CAM_RefOrtho")
    cd.type = "ORTHO"
    cd.ortho_scale = ortho
    cam = bpy.data.objects.get("CAM_RefOrtho") or bpy.data.objects.new("CAM_RefOrtho", cd)
    if cam.name not in sc.collection.objects:
        sc.collection.objects.link(cam)
    zc = COLLAR_TOP + bpy.data.objects["PRES_Pivot"].location.z + ortho / 2
    results = {}
    try:
        for o in sc.objects:
            o.hide_render = (o.type == "MESH" and o.name not in keep) or o.type == "LIGHT"
        r.engine = "BLENDER_WORKBENCH"
        sc.display.shading.light = "FLAT"
        sc.display.shading.color_type = "SINGLE"
        sc.display.shading.single_color = (1, 1, 1)
        r.film_transparent = True
        r.resolution_x = r.resolution_y = res
        r.image_settings.color_mode = "RGBA"
        sc.camera = cam
        sheets = []
        for view, az in VIEWS.items():
            a = math.radians(az)
            cam.location = (6 * math.sin(a), -6 * math.cos(a), zc)
            cam.rotation_euler = (math.radians(90), 0, a)
            path = os.path.join(out_dir, f"sil_{view}.png")
            r.filepath = path
            bpy.ops.render.render(write_still=True, scene=sc.name)
            ren = _fill(np.asarray(Image.open(path).getchannel("A")) > 16)
            ref = np.asarray(Image.open(os.path.join(ROOT, "ref", f"head_{view}_v2.png")).getchannel("A")) > 127
            iou, aspect, bands = _measure(ref, ren)
            results[view] = {"iou": iou, "aspect_diff": aspect, "bands": bands}
            if save_sheet:
                a_, _ = _norm(ref)
                b_, _ = _norm(ren)
                over = np.zeros((256, 256, 3), np.uint8)
                over[a_ & b_] = (200, 200, 200)
                over[a_ & ~b_] = (230, 70, 70)       # red: reference only (model too small there)
                over[~a_ & b_] = (70, 140, 230)      # blue: model only (model too big there)
                sheets.append(over)
        if save_sheet and sheets:
            Image.fromarray(np.concatenate(sheets, axis=1)).save(os.path.join(out_dir, "gate_overlay.png"))
    finally:
        r.engine, r.film_transparent, r.resolution_x, r.resolution_y, sc.camera, r.filepath, r.image_settings.color_mode = old
        for o in sc.objects:
            if o.name in saved:
                o.hide_render = saved[o.name]
    results["mean_iou"] = round(sum(v["iou"] for k, v in results.items() if isinstance(v, dict)) / len(VIEWS), 3)
    return results
