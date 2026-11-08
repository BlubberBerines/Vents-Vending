using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VentsAndVending.Player.Appearance;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// The player's body (GDD §3.1) in the same capsule-and-sphere cartoon style as the customers, with every clothing
    /// variant prebuilt and switched on by the equipped items, plus the clothing catalogue and the den mirror.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        private const string ClothingFolder = "Assets/_Project/ScriptableObjects/Clothing";

        private static List<ClothingDefinition> CreateClothing()
        {
            EnsureFolder(ClothingFolder);
            var list = new List<ClothingDefinition>();
            void Add(string id, string name, string desc, OutfitSlot slot, string style, Color a, Color b, int price, bool starter, bool bareArms = false)
            {
                string path = $"{ClothingFolder}/Clothing_{id}.asset";
                var c = AssetDatabase.LoadAssetAtPath<ClothingDefinition>(path);
                if (c == null)
                {
                    c = ScriptableObject.CreateInstance<ClothingDefinition>();
                    AssetDatabase.CreateAsset(c, path);
                }
                c.Configure(id, name, desc, slot, style, a, b, price, starter, bareArms);
                EditorUtility.SetDirty(c);
                list.Add(c);
            }

            Color teal = new Color(0.2f, 0.52f, 0.56f), orange = new Color(0.95f, 0.5f, 0.15f), mustard = new Color(0.85f, 0.68f, 0.2f);
            Add("jumpsuit", "Vent Jumpsuit", "Standard-issue maintenance. Previous owner: unknown. Smell: known.", OutfitSlot.Torso, "jumpsuit", teal, orange, 0, true);
            Add("tee", "Old Tee", "Says 'I SURVIVED SECTOR 3'. You haven't yet.", OutfitSlot.Torso, "tee", mustard, mustard, 0, true, bareArms: true);
            Add("hoodie", "Grubby Hoodie", "Hood up = invisible. (It doesn't work.)", OutfitSlot.Torso, "hoodie", new Color(0.5f, 0.16f, 0.2f), new Color(0.35f, 0.36f, 0.4f), 45, false);
            Add("labcoat", "Lab Coat", "Instantly makes your juice 20% more scientific.", OutfitSlot.Torso, "labcoat", new Color(0.92f, 0.94f, 0.96f), new Color(0.2f, 0.85f, 1f), 80, false);
            Add("cargo", "Cargo Pants", "Seventeen pockets. All of them have crumbs.", OutfitSlot.Legs, "cargo", new Color(0.36f, 0.4f, 0.24f), new Color(0.22f, 0.24f, 0.16f), 0, true);
            Add("shorts", "Duct Shorts", "Held together with optimism and tape.", OutfitSlot.Legs, "shorts", new Color(0.62f, 0.64f, 0.68f), new Color(0.2f, 0.2f, 0.22f), 20, false);
            Add("boots", "Work Boots", "Steel toes, rubber soles, questionable laces.", OutfitSlot.Feet, "boots", new Color(0.42f, 0.27f, 0.16f), new Color(0.1f, 0.09f, 0.08f), 0, true);
            Add("sneakers", "Glow Sneakers", "They light up. Terrible for sneaking.", OutfitSlot.Feet, "sneakers", new Color(0.95f, 0.95f, 0.97f), new Color(1f, 0.3f, 0.85f), 60, false);
            Add("gloves", "Fingerless Gloves", "For the tactile vent-dweller.", OutfitSlot.Hands, "gloves", new Color(0.16f, 0.17f, 0.2f), new Color(0.16f, 0.17f, 0.2f), 0, true);
            Add("beanie", "Hazard Beanie", "Visible from orbit. Warm, though.", OutfitSlot.Head, "beanie", new Color(1f, 0.8f, 0.2f), new Color(0.08f, 0.08f, 0.08f), 0, true);
            Add("cap", "Glorp-Mart Cap", "Free with any purchase over 400 CC.", OutfitSlot.Head, "cap", new Color(0.9f, 0.3f, 0.75f), Color.white, 25, false);
            Add("hardhat", "Hard Hat", "OSHA-ish compliant.", OutfitSlot.Head, "hardhat", new Color(1f, 0.55f, 0.12f), Color.white, 40, false);
            Add("goggles", "Welding Goggles", "See the world in a flattering green.", OutfitSlot.Face, "goggles", new Color(0.2f, 0.2f, 0.22f), new Color(0.3f, 0.9f, 0.5f), 35, false);
            Add("backpack", "Dealer Backpack", "Carries everything except plausible deniability.", OutfitSlot.Back, "backpack", new Color(0.15f, 0.2f, 0.38f), new Color(1f, 0.82f, 0.3f), 70, false);
            Add("cape", "Duct-Tape Cape", "For the vent hero nobody asked for.", OutfitSlot.Back, "cape", new Color(0.7f, 0.72f, 0.76f), new Color(0.1f, 0.1f, 0.12f), 15, false);
            return list;
        }

        private static void BuildPlayerBody(Transform player)
        {
            List<ClothingDefinition> catalogue = CreateClothing();
            Material skinMat = GetOrCreateMaterial("Body_Skin", Color.white, 0.35f);
            Material baseMat = GetOrCreateMaterial("Body_BaseLayer", new Color(0.42f, 0.44f, 0.48f), 0.3f);
            Material cloth = GetOrCreateMaterial("Body_Cloth", Color.white, 0.3f);
            Material eyeWhite = GetOrCreateMaterial("Char_EyeWhite", new Color(0.97f, 0.96f, 0.92f), 0.6f);
            Material pupil = GetOrCreateMaterial("Char_Pupil", new Color(0.03f, 0.02f, 0.05f), 0.9f);

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(player, false);
            Transform root = bodyGo.transform;
            var skin = new List<Renderer>();
            var baseLayer = new List<Renderer>();
            var head = new List<Renderer>();
            var variants = new List<PlayerBody.Variant>();

            GameObject P(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, PrimitiveType type, List<Renderer> into = null)
            {
                var go = Part(name, parent, pos, scale, mat, type);
                into?.Add(go.GetComponent<Renderer>());
                return go;
            }

            // Core: shorts, vest, neck, head with a simple face
            P("Pelvis", root, new Vector3(0f, 0.9f, 0f), new Vector3(0.32f, 0.12f, 0.22f), baseMat, PrimitiveType.Capsule, baseLayer).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            P("Torso", root, new Vector3(0f, 1.2f, 0f), new Vector3(0.34f, 0.31f, 0.24f), baseMat, PrimitiveType.Capsule, baseLayer);
            head.Add(P("Neck", root, new Vector3(0f, 1.5f, 0f), new Vector3(0.1f, 0.06f, 0.1f), skinMat, PrimitiveType.Cylinder, skin).GetComponent<Renderer>());
            var headGo = P("Head", root, new Vector3(0f, 1.67f, 0f), new Vector3(0.28f, 0.3f, 0.28f), skinMat, PrimitiveType.Sphere, skin);
            head.Add(headGo.GetComponent<Renderer>());
            foreach (float x in new[] { -0.06f, 0.06f })
            {
                head.Add(P("Eye", root, new Vector3(x, 1.7f, 0.125f), new Vector3(0.07f, 0.08f, 0.04f), eyeWhite, PrimitiveType.Sphere).GetComponent<Renderer>());
                head.Add(P("Pupil", root, new Vector3(x, 1.7f, 0.145f), new Vector3(0.03f, 0.035f, 0.02f), pupil, PrimitiveType.Sphere).GetComponent<Renderer>());
            }

            // Limbs (pivots + joints the animator drives)
            Transform Pivot(string name, Transform parent, Vector3 pos)
            {
                var t = Group(name, parent);
                t.localPosition = pos;
                return t;
            }
            Transform hipL = Pivot("Hip_L", root, new Vector3(-0.1f, 0.88f, 0f)), hipR = Pivot("Hip_R", root, new Vector3(0.1f, 0.88f, 0f));
            Transform kneeL = Pivot("Knee_L", hipL, new Vector3(0f, -0.42f, 0f)), kneeR = Pivot("Knee_R", hipR, new Vector3(0f, -0.42f, 0f));
            Transform shL = Pivot("Shoulder_L", root, new Vector3(-0.21f, 1.42f, 0f)), shR = Pivot("Shoulder_R", root, new Vector3(0.21f, 1.42f, 0f));
            Transform elL = Pivot("Elbow_L", shL, new Vector3(0f, -0.3f, 0f)), elR = Pivot("Elbow_R", shR, new Vector3(0f, -0.3f, 0f));
            foreach (var (hip, knee) in new[] { (hipL, kneeL), (hipR, kneeR) })
            {
                P("Thigh", hip, new Vector3(0f, -0.21f, 0f), new Vector3(0.15f, 0.21f, 0.15f), skinMat, PrimitiveType.Capsule, skin);
                P("Shin", knee, new Vector3(0f, -0.21f, 0f), new Vector3(0.13f, 0.21f, 0.13f), skinMat, PrimitiveType.Capsule, skin);
                P("Sock", knee, new Vector3(0f, -0.42f, 0.04f), new Vector3(0.11f, 0.07f, 0.2f), baseMat, PrimitiveType.Cube, baseLayer);
            }
            foreach (var (sh, el) in new[] { (shL, elL), (shR, elR) })
            {
                P("UpperArm", sh, new Vector3(0f, -0.15f, 0f), new Vector3(0.11f, 0.15f, 0.11f), skinMat, PrimitiveType.Capsule, skin);
                P("Forearm", el, new Vector3(0f, -0.14f, 0f), new Vector3(0.1f, 0.14f, 0.1f), skinMat, PrimitiveType.Capsule, skin);
                P("Hand", el, new Vector3(0f, -0.31f, 0.01f), new Vector3(0.1f, 0.11f, 0.08f), skinMat, PrimitiveType.Sphere, skin);
            }

            // ---------------------------------------------------------- Clothing variants
            void V(string style, System.Action<List<GameObject>, List<Renderer>, List<Renderer>> build, bool onHead = false)
            {
                var parts = new List<GameObject>();
                var primary = new List<Renderer>();
                var secondary = new List<Renderer>();
                build(parts, primary, secondary);
                foreach (GameObject g in parts)
                {
                    g.SetActive(false);
                }
                if (onHead)
                {
                    foreach (GameObject g in parts)
                    {
                        head.Add(g.GetComponent<Renderer>());
                    }
                }
                variants.Add(new PlayerBody.Variant { StyleId = style, Parts = parts.ToArray(), Primary = primary.ToArray(), Secondary = secondary.ToArray() });
            }
            GameObject Add(List<GameObject> parts, List<Renderer> tint, string name, Transform parent, Vector3 pos, Vector3 scale, PrimitiveType type, Vector3? euler = null)
            {
                var g = Part(name, parent, pos, scale, cloth, type);
                if (euler.HasValue)
                {
                    g.transform.localRotation = Quaternion.Euler(euler.Value);
                }
                parts.Add(g);
                tint.Add(g.GetComponent<Renderer>());
                return g;
            }
            void Sleeves(List<GameObject> parts, List<Renderer> tint, bool full)
            {
                foreach (var (sh, el) in new[] { (shL, elL), (shR, elR) })
                {
                    Add(parts, tint, "Sleeve", sh, new Vector3(0f, full ? -0.15f : -0.07f, 0f), new Vector3(0.135f, full ? 0.155f : 0.08f, 0.135f), PrimitiveType.Capsule);
                    if (full)
                    {
                        Add(parts, tint, "Cuff", el, new Vector3(0f, -0.13f, 0f), new Vector3(0.12f, 0.13f, 0.12f), PrimitiveType.Capsule);
                    }
                }
            }

            V("jumpsuit", (parts, a, b) =>
            {
                Add(parts, a, "Suit", root, new Vector3(0f, 1.19f, 0f), new Vector3(0.37f, 0.33f, 0.27f), PrimitiveType.Capsule);
                Sleeves(parts, a, true);
                Add(parts, b, "Belt", root, new Vector3(0f, 0.98f, 0f), new Vector3(0.39f, 0.03f, 0.29f), PrimitiveType.Cylinder);
                head.Add(Add(parts, b, "Collar", root, new Vector3(0f, 1.46f, 0f), new Vector3(0.2f, 0.025f, 0.2f), PrimitiveType.Cylinder).GetComponent<Renderer>());
                Add(parts, b, "Patch", root, new Vector3(0.09f, 1.3f, 0.13f), new Vector3(0.07f, 0.05f, 0.01f), PrimitiveType.Cube);
            });
            V("tee", (parts, a, b) =>
            {
                Add(parts, a, "Tee", root, new Vector3(0f, 1.2f, 0f), new Vector3(0.36f, 0.32f, 0.26f), PrimitiveType.Capsule);
                Sleeves(parts, a, false);
            });
            V("hoodie", (parts, a, b) =>
            {
                Add(parts, a, "Hoodie", root, new Vector3(0f, 1.19f, 0f), new Vector3(0.39f, 0.34f, 0.29f), PrimitiveType.Capsule);
                Sleeves(parts, a, true);
                head.Add(Add(parts, b, "Hood", root, new Vector3(0f, 1.5f, -0.11f), new Vector3(0.26f, 0.14f, 0.16f), PrimitiveType.Sphere).GetComponent<Renderer>());
                Add(parts, b, "Pocket", root, new Vector3(0f, 1.06f, 0.14f), new Vector3(0.22f, 0.08f, 0.02f), PrimitiveType.Cube);
            });
            V("labcoat", (parts, a, b) =>
            {
                Add(parts, a, "Coat", root, new Vector3(0f, 1.19f, 0f), new Vector3(0.38f, 0.33f, 0.28f), PrimitiveType.Capsule);
                Sleeves(parts, a, true);
                Add(parts, a, "Tail", root, new Vector3(0f, 0.82f, -0.02f), new Vector3(0.38f, 0.36f, 0.26f), PrimitiveType.Cube);
                Add(parts, b, "PenPocket", root, new Vector3(-0.09f, 1.3f, 0.14f), new Vector3(0.05f, 0.08f, 0.01f), PrimitiveType.Cube);
            });
            V("cargo", (parts, a, b) =>
            {
                foreach (var (hip, knee) in new[] { (hipL, kneeL), (hipR, kneeR) })
                {
                    Add(parts, a, "Leg", hip, new Vector3(0f, -0.21f, 0f), new Vector3(0.18f, 0.22f, 0.18f), PrimitiveType.Capsule);
                    Add(parts, a, "Shin", knee, new Vector3(0f, -0.2f, 0f), new Vector3(0.16f, 0.21f, 0.16f), PrimitiveType.Capsule);
                    Add(parts, b, "Pocket", hip, new Vector3(hip == hipL ? -0.085f : 0.085f, -0.24f, 0f), new Vector3(0.03f, 0.1f, 0.09f), PrimitiveType.Cube);
                }
                Add(parts, a, "Seat", root, new Vector3(0f, 0.9f, 0f), new Vector3(0.35f, 0.13f, 0.24f), PrimitiveType.Capsule, new Vector3(0f, 0f, 90f));
            });
            V("shorts", (parts, a, b) =>
            {
                foreach (Transform hip in new[] { hipL, hipR })
                {
                    Add(parts, a, "Short", hip, new Vector3(0f, -0.1f, 0f), new Vector3(0.18f, 0.11f, 0.18f), PrimitiveType.Capsule);
                }
                Add(parts, a, "Seat", root, new Vector3(0f, 0.9f, 0f), new Vector3(0.35f, 0.13f, 0.24f), PrimitiveType.Capsule, new Vector3(0f, 0f, 90f));
                Add(parts, b, "Tape", root, new Vector3(0f, 0.95f, 0.115f), new Vector3(0.2f, 0.03f, 0.01f), PrimitiveType.Cube);
            });
            V("boots", (parts, a, b) =>
            {
                foreach (Transform knee in new[] { kneeL, kneeR })
                {
                    Add(parts, a, "Boot", knee, new Vector3(0f, -0.4f, 0.04f), new Vector3(0.14f, 0.12f, 0.25f), PrimitiveType.Cube);
                    Add(parts, b, "Sole", knee, new Vector3(0f, -0.465f, 0.045f), new Vector3(0.15f, 0.025f, 0.26f), PrimitiveType.Cube);
                }
            });
            V("sneakers", (parts, a, b) =>
            {
                foreach (Transform knee in new[] { kneeL, kneeR })
                {
                    Add(parts, a, "Sneaker", knee, new Vector3(0f, -0.42f, 0.05f), new Vector3(0.13f, 0.09f, 0.25f), PrimitiveType.Cube);
                    Add(parts, b, "GlowSole", knee, new Vector3(0f, -0.468f, 0.05f), new Vector3(0.14f, 0.02f, 0.26f), PrimitiveType.Cube);
                }
            });
            V("gloves", (parts, a, b) =>
            {
                foreach (Transform el in new[] { elL, elR })
                {
                    Add(parts, a, "Glove", el, new Vector3(0f, -0.3f, 0.01f), new Vector3(0.115f, 0.1f, 0.095f), PrimitiveType.Sphere);
                }
            });
            V("beanie", (parts, a, b) =>
            {
                Add(parts, a, "Beanie", root, new Vector3(0f, 1.77f, -0.01f), new Vector3(0.3f, 0.2f, 0.3f), PrimitiveType.Sphere);
                Add(parts, b, "Band", root, new Vector3(0f, 1.73f, 0f), new Vector3(0.305f, 0.03f, 0.305f), PrimitiveType.Cylinder);
            }, true);
            V("cap", (parts, a, b) =>
            {
                Add(parts, a, "Dome", root, new Vector3(0f, 1.76f, -0.01f), new Vector3(0.3f, 0.17f, 0.3f), PrimitiveType.Sphere);
                Add(parts, b, "Brim", root, new Vector3(0f, 1.74f, 0.15f), new Vector3(0.2f, 0.02f, 0.14f), PrimitiveType.Cube);
            }, true);
            V("hardhat", (parts, a, b) =>
            {
                Add(parts, a, "Shell", root, new Vector3(0f, 1.78f, 0f), new Vector3(0.32f, 0.2f, 0.34f), PrimitiveType.Sphere);
                Add(parts, a, "BrimRing", root, new Vector3(0f, 1.73f, 0.01f), new Vector3(0.4f, 0.015f, 0.42f), PrimitiveType.Cylinder);
                Add(parts, b, "Lamp", root, new Vector3(0f, 1.8f, 0.16f), new Vector3(0.05f, 0.04f, 0.03f), PrimitiveType.Cube);
            }, true);
            V("goggles", (parts, a, b) =>
            {
                foreach (float x in new[] { -0.065f, 0.065f })
                {
                    Add(parts, a, "Rim", root, new Vector3(x, 1.7f, 0.13f), new Vector3(0.09f, 0.03f, 0.09f), PrimitiveType.Cylinder, new Vector3(90f, 0f, 0f));
                    Add(parts, b, "Lens", root, new Vector3(x, 1.7f, 0.145f), new Vector3(0.075f, 0.01f, 0.075f), PrimitiveType.Cylinder, new Vector3(90f, 0f, 0f));
                }
                Add(parts, a, "Strap", root, new Vector3(0f, 1.7f, 0f), new Vector3(0.29f, 0.03f, 0.29f), PrimitiveType.Cylinder);
            }, true);
            V("backpack", (parts, a, b) =>
            {
                Add(parts, a, "Pack", root, new Vector3(0f, 1.22f, -0.2f), new Vector3(0.3f, 0.36f, 0.16f), PrimitiveType.Cube);
                Add(parts, b, "Flap", root, new Vector3(0f, 1.36f, -0.26f), new Vector3(0.28f, 0.1f, 0.04f), PrimitiveType.Cube);
                foreach (float x in new[] { -0.11f, 0.11f })
                {
                    Add(parts, b, "Strap", root, new Vector3(x, 1.25f, 0.02f), new Vector3(0.04f, 0.34f, 0.3f), PrimitiveType.Cube);
                }
            });
            V("cape", (parts, a, b) =>
            {
                Add(parts, a, "Cape", root, new Vector3(0f, 1.05f, -0.17f), new Vector3(0.42f, 0.7f, 0.02f), PrimitiveType.Cube, new Vector3(8f, 0f, 0f));
                Add(parts, b, "TapeX", root, new Vector3(0f, 1.1f, -0.195f), new Vector3(0.36f, 0.04f, 0.01f), PrimitiveType.Cube, new Vector3(8f, 0f, 30f));
            });

            // Nothing on the body should block the player's own movement or interaction rays
            foreach (Collider c in bodyGo.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }
            foreach (Renderer r in bodyGo.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.On;
            }

            var body = bodyGo.AddComponent<PlayerBody>();
            body.Configure(catalogue, skin.ToArray(), baseLayer.ToArray(), new Renderer[0], variants.ToArray(), head.ToArray());
            bodyGo.AddComponent<PlayerBodyAnimator>().Configure(root, hipL, hipR, kneeL, kneeR, shL, shR, elL, elR);
        }

        /// <summary>A cracked, taped-up mirror on the chamber's west wall that opens the wardrobe.</summary>
        private static void BuildWardrobeMirror(Transform parent)
        {
            Material frame = ArtLibrary.Get("M_WorkshopOrange");
            Material glass = GetOrCreateMaterial("Mirror_Glass", new Color(0.55f, 0.66f, 0.72f), 0.95f, new Color(0.05f, 0.08f, 0.1f));
            Material crack = GetOrCreateMaterial("Prop_Seam", new Color(0.05f, 0.04f, 0.06f), 0.2f);
            Material tape = GetOrCreateMaterial("Prop_DuctTape", new Color(0.62f, 0.63f, 0.66f), 0.45f);

            var root = new GameObject("Wardrobe Mirror");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(new Vector3(-3.2f, 1.32f, -17.1f), Quaternion.Euler(0f, -90f, 2.5f)); // local -Z faces into the room
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0f, -0.03f);
            col.size = new Vector3(0.48f, 0.74f, 0.06f);
            Transform t = root.transform;
            Part("Frame", t, new Vector3(0f, 0f, -0.02f), new Vector3(0.46f, 0.72f, 0.035f), frame);
            Part("Glass", t, new Vector3(0f, 0f, -0.04f), new Vector3(0.38f, 0.64f, 0.006f), glass);
            Part("Crack", t, new Vector3(0.05f, 0.08f, -0.045f), new Vector3(0.012f, 0.38f, 0.002f), crack).transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
            Part("Crack", t, new Vector3(-0.04f, -0.02f, -0.045f), new Vector3(0.01f, 0.22f, 0.002f), crack).transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            Part("Tape", t, new Vector3(0.06f, 0.1f, -0.048f), new Vector3(0.2f, 0.045f, 0.003f), tape).transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            root.AddComponent<WardrobeMirror>();
        }
    }
}
