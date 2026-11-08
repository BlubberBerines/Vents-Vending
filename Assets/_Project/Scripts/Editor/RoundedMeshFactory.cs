using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Hand-made imperfection for a rounded box (style guide §5 "nothing is factory-perfect"). Fractions are relative
    /// to the box; Sag and Lean are in metres; Twist is in degrees bottom-to-top.
    /// </summary>
    public struct Wonk
    {
        /// <summary>Sides push out around mid-height (cardboard bowing, a stuffed sack).</summary>
        public float Bulge;
        /// <summary>Top and bottom faces dome outward in the middle (pillows, cushions).</summary>
        public float Puff;
        /// <summary>Top is narrower than the bottom.</summary>
        public float Taper;
        /// <summary>Top face dips in the middle, bottom stays flat (mattress, tabletop, awning).</summary>
        public float Sag;
        /// <summary>Top shears sideways along X.</summary>
        public float Lean;
        /// <summary>Top rotates about Y relative to the bottom.</summary>
        public float Twist;

        public bool IsZero => Bulge == 0f && Puff == 0f && Taper == 0f && Sag == 0f && Lean == 0f && Twist == 0f;

        public Wonk Scaled(float k, float sign) => new Wonk
        {
            Bulge = Bulge * k, Puff = Puff * k, Taper = Taper * k, Sag = Sag * k, Lean = Lean * k * sign, Twist = Twist * k * sign
        };

        public string Key => $"b{Bulge:0.###}p{Puff:0.###}t{Taper:0.###}s{Sag:0.###}l{Lean:0.###}w{Twist:0.#}";
    }

    /// <summary>
    /// Generates rounded-box meshes baked at real size (so corners stay round on long/flat shapes) and caches them as
    /// assets. Used by the soften pass to take the harsh edges off props (Docs/Design/ArtStyleGuide.md §5).
    /// With a <see cref="Wonk"/> the flat faces are subdivided and gently deformed so props stop looking machined.
    /// </summary>
    public static class RoundedMeshFactory
    {
        private const string Folder = "Assets/_Project/Art/Meshes/Generated";
        private const int WonkSteps = 4;

        /// <summary>Rounded box of <paramref name="size"/> metres with corner <paramref name="radius"/> metres.</summary>
        public static Mesh Get(Vector3 size, float radius, int roundSegments = 4, Wonk wonk = default)
        {
            radius = Mathf.Clamp(radius, 0.001f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.5f - 0.0005f);
            string name = $"RB2_{size.x:0.###}x{size.y:0.###}x{size.z:0.###}_r{radius:0.###}_s{roundSegments}".Replace(',', '.');
            if (!wonk.IsZero)
            {
                name = $"RB3_{size.x:0.###}x{size.y:0.###}x{size.z:0.###}_r{radius:0.###}_{wonk.Key}".Replace(',', '.').Replace('-', 'm');
            }
            string path = $"{Folder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(Folder);
            Mesh mesh = Build(size, radius, Mathf.Max(1, roundSegments), wonk);
            mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>
        /// Swaps a (scaled) primitive cube for a rounded mesh of the same size, resets its scale and keeps any box
        /// collider the same size. Safe to call on objects that aren't cubes (they're left alone).
        /// </summary>
        public static bool RoundCube(GameObject go, float radius, Wonk wonk = default)
        {
            if (!go.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null || filter.sharedMesh.name != "Cube")
            {
                return false;
            }

            Vector3 size = go.transform.localScale;
            filter.sharedMesh = Get(size, radius, 4, wonk);
            go.transform.localScale = Vector3.one;
            if (go.TryGetComponent(out BoxCollider box))
            {
                box.center = Vector3.zero;
                box.size = size;
            }
            return true;
        }

        private static Mesh Build(Vector3 size, float r, int k, Wonk wonk)
        {
            Vector3 half = size * 0.5f;
            bool deform = !wonk.IsZero;
            int mid = deform ? WonkSteps : 1;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            // Faces as (normal, u, v) with cross(u, v) == normal so clockwise winding faces outwards.
            (Vector3 n, Vector3 u, Vector3 v)[] faces =
            {
                (Vector3.right, Vector3.back, Vector3.up),
                (Vector3.left, Vector3.forward, Vector3.up),
                (Vector3.up, Vector3.right, Vector3.back),
                (Vector3.down, Vector3.right, Vector3.forward),
                (Vector3.forward, Vector3.right, Vector3.up),
                (Vector3.back, Vector3.left, Vector3.up),
            };

            foreach (var (n, u, v) in faces)
            {
                float lenU = Mathf.Abs(Vector3.Dot(size, u));
                float lenV = Mathf.Abs(Vector3.Dot(size, v));
                float lenN = Mathf.Abs(Vector3.Dot(size, n));
                List<float> cu = AxisCoords(lenU, r, k, mid);
                List<float> cv = AxisCoords(lenV, r, k, mid);
                int start = vertices.Count;

                for (int j = 0; j < cv.Count; j++)
                {
                    for (int i = 0; i < cu.Count; i++)
                    {
                        Vector3 p = n * (lenN * 0.5f) + u * cu[i] + v * cv[j];
                        Vector3 inner = new Vector3(
                            Mathf.Clamp(p.x, -half.x + r, half.x - r),
                            Mathf.Clamp(p.y, -half.y + r, half.y - r),
                            Mathf.Clamp(p.z, -half.z + r, half.z - r));
                        Vector3 d = p - inner;
                        Vector3 normal = d.sqrMagnitude > 1e-10f ? d.normalized : n;
                        Vector3 surface = inner + normal * r;
                        if (deform)
                        {
                            Vector3 moved = Deform(surface, half, wonk);
                            normal = DeformedNormal(surface, normal, moved, half, wonk);
                            surface = moved;
                        }
                        vertices.Add(surface);
                        normals.Add(normal);
                    }
                }

                int w = cu.Count;
                for (int j = 0; j < cv.Count - 1; j++)
                {
                    for (int i = 0; i < w - 1; i++)
                    {
                        int a = start + j * w + i;
                        int b = a + 1;
                        int d = a + w;
                        int c = d + 1;
                        // Unity is left-handed: with cross(u, v) == n this order is clockwise seen from outside.
                        triangles.Add(a); triangles.Add(c); triangles.Add(d);
                        triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    }
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 Deform(Vector3 p, Vector3 half, Wonk w)
        {
            float nx = Mathf.Clamp(p.x / half.x, -1f, 1f);
            float ny = Mathf.Clamp(p.y / half.y, -1f, 1f);
            float nz = Mathf.Clamp(p.z / half.z, -1f, 1f);
            float up = (ny + 1f) * 0.5f;            // 0 at the bottom, 1 at the top
            float middle = (1f - nx * nx) * (1f - nz * nz);

            float side = (1f + w.Bulge * (1f - ny * ny)) * (1f - w.Taper * up);
            p.x *= side;
            p.z *= side;
            p.y *= 1f + w.Puff * middle;
            p.y -= w.Sag * middle * up;
            p.x += w.Lean * up;

            float a = w.Twist * (up - 0.5f) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            return new Vector3(p.x * cos - p.z * sin, p.y, p.x * sin + p.z * cos);
        }

        /// <summary>Normal of the deformed surface from two deformed tangents (finite differences).</summary>
        private static Vector3 DeformedNormal(Vector3 p, Vector3 n, Vector3 moved, Vector3 half, Wonk w)
        {
            const float e = 0.002f;
            Vector3 t1 = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 t2 = Vector3.Cross(n, t1);
            Vector3 d1 = Deform(p + t1 * e, half, w) - moved;
            Vector3 d2 = Deform(p + t2 * e, half, w) - moved;
            Vector3 result = Vector3.Cross(d1, d2).normalized;
            if (Vector3.Dot(result, Deform(p + n * e, half, w) - moved) < 0f)
            {
                result = -result;
            }
            return result.sqrMagnitude > 0.5f ? result : n;
        }

        /// <summary>k steps across each rounded band, <paramref name="mid"/> steps across the flat middle.</summary>
        private static List<float> AxisCoords(float length, float r, int k, int mid)
        {
            var coords = new List<float>();
            float h = length * 0.5f;
            for (int i = 0; i <= k; i++)
            {
                coords.Add(-h + r * i / k);
            }
            for (int i = 1; i < mid; i++)
            {
                coords.Add(-h + r + (length - 2f * r) * i / mid);
            }
            for (int i = 0; i <= k; i++)
            {
                coords.Add(h - r + r * i / k);
            }
            return coords;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
