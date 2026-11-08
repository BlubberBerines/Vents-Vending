using UnityEngine;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// "Less perfect" pass (style guide §5): props lean, bow, sag and get patched so the world reads hand-built, not
    /// machined. Everything is deterministic (seeded from position) so rebuilding the scene gives the same result.
    /// Structures — walls, floors, ducts — stay true; this only touches props and dressing.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        /// <summary>Per prop name: how it's allowed to be wonky. Each instance gets 0.65×, 1× or 1.35× of this, mirrored at random.</summary>
        private static readonly System.Collections.Generic.Dictionary<string, Wonk> WonkMap = new System.Collections.Generic.Dictionary<string, Wonk>
        {
            // Soft furnishings
            { "Mattress", new Wonk { Sag = 0.08f, Puff = 0.12f, Bulge = 0.03f, Twist = 3f } },
            { "Pillow", new Wonk { Puff = 0.55f, Bulge = 0.12f, Taper = 0.08f, Twist = 9f } },
            { "Blanket", new Wonk { Sag = 0.04f, Puff = 0.6f, Twist = 5f, Lean = 0.03f } },
            { "BlanketRoll", new Wonk { Bulge = 0.2f, Puff = 0.15f, Twist = 12f } },
            { "Bench_W", new Wonk { Sag = 0.035f } },
            { "Bench_E", new Wonk { Sag = 0.035f } },
            // Cardboard and crates
            { "Box_Open", new Wonk { Bulge = 0.12f, Taper = 0.06f, Lean = 0.03f, Twist = 6f } },
            { "Box_Taped", new Wonk { Bulge = 0.14f, Puff = 0.18f, Taper = 0.04f, Twist = 5f } },
            { "LooseBox", new Wonk { Bulge = 0.12f, Taper = 0.08f, Lean = 0.035f, Twist = 7f } },
            { "FlatPacks", new Wonk { Sag = 0.03f, Puff = 0.15f, Lean = 0.04f, Twist = 4f } },
            { "Body", new Wonk { Bulge = 0.07f, Taper = 0.05f, Twist = 4f } },
            { "Band", new Wonk { Bulge = 0.07f, Taper = 0.05f, Twist = 4f } },
            { "BandV", new Wonk { Bulge = 0.07f, Taper = 0.05f, Twist = 4f } },
            { "Goods_Crate", new Wonk { Bulge = 0.08f, Taper = 0.05f, Twist = 5f } },
            // Furniture
            { "Pallet", new Wonk { Twist = 2.5f, Sag = 0.02f } },
            { "Tabletop", new Wonk { Sag = 0.022f, Twist = 1.5f } },
            { "Awning", new Wonk { Sag = 0.07f } },
            { "Pedestal", new Wonk { Taper = 0.06f } },
            // Pass 2 clutter
            { "Rug", new Wonk { Twist = 3f, Sag = 0.002f } },
            { "PizzaBox", new Wonk { Sag = 0.006f, Twist = 2.5f } },
            { "Jacket", new Wonk { Bulge = 0.14f, Taper = -0.14f, Twist = 6f, Lean = 0.02f } },
            { "Boot", new Wonk { Taper = 0.25f, Bulge = 0.06f, Twist = 4f } },
            { "Toolbox", new Wonk { Taper = 0.05f, Twist = 1.5f } },
            { "OldBoard", new Wonk { Twist = 5f, Lean = 0.03f } },
        };

        /// <summary>Props whose colour drifts a little per instance so repeats don't match exactly.</summary>
        private static readonly System.Collections.Generic.HashSet<string> TintJitterNames = new System.Collections.Generic.HashSet<string>
        {
            "Body", "LooseBox", "Box_Taped", "FlatPacks", "Pallet", "Goods_Crate",
        };

        private static readonly float[] WonkScales = { 0.65f, 1f, 1.35f };

        /// <summary>Wonk for this instance: scaled and mirrored from a hash of its position (bands share their crate's centre, so they match).</summary>
        private static Wonk InstanceWonk(Wonk baseWonk, Vector3 position)
        {
            int h = Hash(position);
            return baseWonk.Scaled(WonkScales[(h & 0x7fffffff) % 3], ((h >> 4) & 1) == 0 ? 1f : -1f);
        }

        private static void JitterTints()
        {
            var block = new MaterialPropertyBlock();
            foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!TintJitterNames.Contains(r.gameObject.name) || r.sharedMaterial == null || !r.sharedMaterial.HasProperty("_BaseColor"))
                {
                    continue;
                }
                var rng = Rng(r.transform.position);
                Color c = r.sharedMaterial.GetColor("_BaseColor");
                float value = 0.9f + (float)rng.NextDouble() * 0.2f;
                float warm = ((float)rng.NextDouble() - 0.5f) * 0.06f;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", new Color(c.r * value + warm, c.g * value, c.b * value - warm, c.a));
                r.SetPropertyBlock(block);
            }
        }

        /// <summary>Switches on a little procedural dirt/wear for a plain prop material (they start factory-clean).</summary>
        private static Material Worn(Material mat, float grime, float patches, float edgeWear = 0f)
        {
            mat.SetFloat("_GrimeAmount", grime);
            mat.SetFloat("_GrimePatches", patches);
            mat.SetFloat("_EdgeWear", edgeWear);
            UnityEditor.EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ Cables

        /// <summary>A cable drooping between supports roughly every <paramref name="span"/> metres.</summary>
        private static void SagCable(Transform parent, Vector3 a, Vector3 b, float radius, Material mat, float span = 1.7f, float sag = 0.07f)
        {
            var g = Group("Cable", parent);
            var rng = Rng(a + b);
            int spans = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(a, b) / span));
            const int segments = 4;
            for (int s = 0; s < spans; s++)
            {
                Vector3 p0 = Vector3.Lerp(a, b, (float)s / spans);
                Vector3 p1 = Vector3.Lerp(a, b, (float)(s + 1) / spans);
                float drop = sag * (0.6f + 0.8f * (float)rng.NextDouble());
                for (int i = 0; i < segments; i++)
                {
                    float t0 = (float)i / segments, t1 = (float)(i + 1) / segments;
                    Vector3 q0 = Vector3.Lerp(p0, p1, t0) + Vector3.down * (drop * 4f * t0 * (1f - t0));
                    Vector3 q1 = Vector3.Lerp(p0, p1, t1) + Vector3.down * (drop * 4f * t1 * (1f - t1));
                    CylinderBetween("CableSeg", g, q0, q1, radius, mat);
                }
            }
        }

        private static GameObject CylinderBetween(string name, Transform parent, Vector3 a, Vector3 b, float radius, Material mat)
        {
            Vector3 d = b - a;
            var go = Part(name, parent, (a + b) * 0.5f, new Vector3(radius * 2f, d.magnitude * 0.5f + radius * 0.6f, radius * 2f), mat, PrimitiveType.Cylinder);
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            return go;
        }

        // ------------------------------------------------------------------ Hand-placed imperfections

        /// <summary>Riveted patch plate on a wall. <paramref name="normal"/> points out of the wall into the room.</summary>
        private static void PatchPlate(Transform parent, Vector3 centre, Vector3 normal, Vector2 size, float roll, Material plate, Material rivet)
        {
            var g = Group("PatchPlate", parent);
            g.position = centre + normal * 0.012f;
            g.rotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, roll);
            NoCollider(Block("Plate", g, g.position, new Vector3(size.x, size.y, 0.018f), plate)).transform.rotation = g.rotation;
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    Vector3 local = new Vector3(sx * (size.x * 0.5f - 0.04f), sy * (size.y * 0.5f - 0.04f), -0.012f);
                    Part("Rivet", g, Vector3.zero, Vector3.one * 0.028f, rivet, PrimitiveType.Sphere).transform.localPosition = local;
                }
            }
        }

        /// <summary>A dark crack in a wall with an X of grey tape over it.</summary>
        private static void TapedCrack(Transform parent, Vector3 centre, Vector3 normal, Material crack, Material tape)
        {
            var g = Group("TapedCrack", parent);
            g.position = centre;
            g.rotation = Quaternion.LookRotation(-normal);
            Part("Crack", g, Vector3.zero, new Vector3(0.018f, 0.42f, 0.006f), crack).transform.localRotation = Quaternion.Euler(0f, 0f, 14f);
            Part("CrackFork", g, new Vector3(0.05f, 0.12f, 0f), new Vector3(0.012f, 0.16f, 0.006f), crack).transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
            Part("Tape_A", g, new Vector3(0f, 0f, -0.006f), new Vector3(0.36f, 0.06f, 0.006f), tape).transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
            Part("Tape_B", g, new Vector3(0f, 0f, -0.009f), new Vector3(0.34f, 0.06f, 0.006f), tape).transform.localRotation = Quaternion.Euler(0f, 0f, -33f);
        }

        private static void BuildHideoutImperfections(Transform hideout)
        {
            var g = Group("Imperfections", hideout);
            Material plate = ArtLibrary.Get("M_RustyMetal");
            Material rivet = ArtLibrary.Get("M_DarkMetal");
            Material crack = GetOrCreateMaterial("Prop_Seam", new Color(0.05f, 0.04f, 0.06f), 0.2f);
            Material tape = Worn(GetOrCreateMaterial("Prop_DuctTape", new Color(0.62f, 0.63f, 0.66f), 0.45f), 0.3f, 0.3f);

            PatchPlate(g, new Vector3(3.2f, 1.15f, -17.4f), Vector3.left, new Vector2(0.5f, 0.42f), 6f, plate, rivet);
            PatchPlate(g, new Vector3(1.05f, 1.35f, -14.5f), Vector3.back, new Vector2(0.38f, 0.3f), -9f, plate, rivet);
            PatchPlate(g, new Vector3(-0.6f, 1.2f, -2.0f), Vector3.right, new Vector2(0.45f, 0.6f), 4f, plate, rivet);
            PatchPlate(g, new Vector3(-6.8f, 1.0f, 6.5f), Vector3.left, new Vector2(0.55f, 0.35f), -5f, plate, rivet);
            TapedCrack(g, new Vector3(3.19f, 0.65f, -18.7f), Vector3.left, crack, tape);
            TapedCrack(g, new Vector3(1.19f, 1.5f, -8.2f), Vector3.left, crack, tape);
        }

        // ------------------------------------------------------------------ Determinism helpers

        private static int Hash(Vector3 p)
        {
            unchecked
            {
                return (Mathf.RoundToInt(p.x * 50f) * 73856093) ^ (Mathf.RoundToInt(p.y * 50f) * 19349663) ^ (Mathf.RoundToInt(p.z * 50f) * 83492791);
            }
        }

        private static System.Random Rng(Vector3 p) => new System.Random(Hash(p));
    }
}
