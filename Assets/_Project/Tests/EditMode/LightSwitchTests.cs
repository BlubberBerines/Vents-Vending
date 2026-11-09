using NUnit.Framework;
using UnityEngine;
using VentsAndVending.World;

namespace VentsAndVending.Tests
{
    /// <summary>The den's ceiling-light button.</summary>
    public class LightSwitchTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        private (LightCircuit circuit, LightSwitch sw, Light light) Make()
        {
            _root = new GameObject("rig");
            var light = new GameObject("light").AddComponent<Light>();
            light.transform.SetParent(_root.transform);
            var circuit = new GameObject("circuit").AddComponent<LightCircuit>();
            circuit.transform.SetParent(_root.transform);
            circuit.Configure("L3_CEILING", new[] { light }, new Renderer[0]);
            var sw = new GameObject("switch").AddComponent<LightSwitch>();
            sw.transform.SetParent(_root.transform);
            sw.Configure("L3_CEILING", null);
            return (circuit, sw, light);
        }

        [Test]
        public void Press_TurnsTheCircuitOff_ThenOn()
        {
            var (circuit, sw, light) = Make();
            sw.Press();
            Assert.IsFalse(circuit.IsOn);
            Assert.IsFalse(light.enabled);
            sw.Press();
            Assert.IsTrue(circuit.IsOn);
        }

        [Test]
        public void Prompt_SaysWhatPressingWillDo()
        {
            var (_, sw, _) = Make();
            Assert.AreEqual("Lights off", sw.GetPrompt(null));
            sw.Press();
            Assert.AreEqual("Lights on", sw.GetPrompt(null));
        }

        [Test]
        public void NoCircuit_PromptIsNull()
        {
            _root = new GameObject("switch");
            var sw = _root.AddComponent<LightSwitch>();
            sw.Configure("MISSING", null);
            Assert.IsNull(sw.GetPrompt(null));
        }
    }
}
