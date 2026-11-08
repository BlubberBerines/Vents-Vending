using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Generates rounded-box meshes baked at real size (so corners stay round on long/flat shapes) and caches them as
    /// assets. Used by the soften pass to take the harsh edges off props (Docs/Design/ArtStyleGuide.md §5).
    /// </summary>
    public static class RoundedMeshFactory
    {
        private const string Folder = "Assets/_Project/Art/Meshes/Generated";

        /// <summary>Rounded box of <paramref name="size"/> metres with corner <paramref name="radius"/> metres.</summary>
        public static Mesh Get(Vector3 size, float radius, int roundSegments = 4)
        {
            radius = Mathf.Clamp(radius, 0.001f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.5f - 0.0005f);
            string name = $"RB2_{size.x:0.###}x{size.y:0.###}x{size.z:0.###}_r{radius:0.###}_s{roundSegments}".Replace(',', '.');
            string path = $"{Folder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(Folder);
            Mesh mesh = Build(size, radius, Mathf.Max(1, roundSegments));
            mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>
        /// Swaps a (scaled) primitive cube for a rounded mesh of the same size, resets its scale and keeps any box
        /// collider the same size. Safe to call on objects that aren't cubes (they're left alone).
        /// </summary>
        public static bool RoundCube(GameObject go, float radius)
        {
            if (!go.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null || filter.sharedMesh.name != "Cube")
            {
                return false;
            }

            Vector3 size = go.transform.localScale;
            filter.sharedMesh = Get(size, radius);
            go.transform.localScale = Vector3.one;
            if (go.TryGetComponent(out BoxCollider box))
            {
                box.center = Vector3.zero;
                box.size = size;
            }
            return true;
        }

        private static Mesh Build(Vector3 size, float r, int k)
        {
            Vector3 half = size * 0.5f;
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
                List<float> cu = AxisCoords(lenU, r, k);
                List<float> cv = AxisCoords(lenV, r, k);
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
                        vertices.Add(inner + normal * r);
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

        /// <summary>k steps across each rounded band, one step across the flat middle.</summary>
        private static List<float> AxisCoords(float length, float r, int k)
        {
            var coords = new List<float>();
            float h = length * 0.5f;
            for (int i = 0; i <= k; i++)
            {
                coords.Add(-h + r * i / k);
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
