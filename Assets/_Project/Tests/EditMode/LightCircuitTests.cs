using NUnit.Framework;
using UnityEngine;
using VentsAndVending.World;

namespace VentsAndVending.Tests
{
    /// <summary>Ceiling light circuits toggled from a switch (den button): lights and glowing panels go dark together.</summary>
    public class LightCircuitTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        private LightCircuit MakeCircuit(string id, out Light light, out Renderer panel, out Material glow, out Material plain)
        {
            _root = new GameObject("circuit");
            light = new GameObject("light").AddComponent<Light>();
            light.transform.SetParent(_root.transform);
            panel = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Renderer>();
            panel.transform.SetParent(_root.transform);
            glow = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            glow.SetColor("_EmissionColor", new Color(2f, 1f, 0.3f));
            plain = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            plain.SetColor("_EmissionColor", Color.black);
            panel.sharedMaterials = new[] { plain, glow };
            var circuit = _root.AddComponent<LightCircuit>();
            circuit.Configure(id, new[] { light }, new[] { panel });
            return circuit;
        }

        [Test]
        public void SetOff_DisablesLights()
        {
            var c = MakeCircuit("L3_CEILING", out Light light, out _, out _, out _);
            c.SetOn(false);
            Assert.IsFalse(c.IsOn);
            Assert.IsFalse(light.enabled);
        }

        [Test]
        public void Toggle_RestoresLights()
        {
            var c = MakeCircuit("L3_CEILING", out Light light, out _, out _, out _);
            c.SetOn(false);
            c.Toggle();
            Assert.IsTrue(c.IsOn);
            Assert.IsTrue(light.enabled);
        }

        [Test]
        public void Off_DarkensOnlyEmissiveSlots()
        {
            var c = MakeCircuit("L3_CEILING", out _, out Renderer panel, out _, out _);
            c.SetOn(false);
            var block = new MaterialPropertyBlock();
            panel.GetPropertyBlock(block, 1);
            Assert.AreEqual(Color.black, block.GetColor("_EmissionColor"));
            panel.GetPropertyBlock(block, 0);
            Assert.IsTrue(block.isEmpty, "the non-emissive slot is left alone");
        }

        [Test]
        public void On_ClearsTheOverride()
        {
            var c = MakeCircuit("L3_CEILING", out _, out Renderer panel, out _, out _);
            c.SetOn(false);
            c.SetOn(true);
            var block = new MaterialPropertyBlock();
            panel.GetPropertyBlock(block, 1);
            Assert.IsTrue(block.isEmpty);
        }

        [Test]
        public void ToggleById_FindsTheCircuit()
        {
            var c = MakeCircuit("L3_CEILING", out Light light, out _, out _, out _);
            Assert.IsTrue(LightCircuit.Toggle("L3_CEILING"));
            Assert.IsFalse(c.IsOn);
            Assert.IsFalse(light.enabled);
            Assert.IsFalse(LightCircuit.Toggle("NO_SUCH_CIRCUIT"));
        }
    }
}
