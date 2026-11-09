using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Player;

namespace VentsAndVending.Tests
{
    /// <summary>Final-review fix #3: an interrupted climb (teleport, disable) must hand control back.</summary>
    public class PlayerClimbCancelTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void Cancel_ReenablesTheControllerAndClearsClimbing()
        {
            _go = new GameObject("player");
            var cc = _go.AddComponent<CharacterController>();
            var climb = _go.AddComponent<PlayerClimb>();
            cc.enabled = false;                   // as it is mid-climb
            climb.Cancel();
            Assert.IsTrue(cc.enabled);
            Assert.IsFalse(climb.IsClimbing);
            Assert.IsFalse(climb.JumpConsumedThisFrame);
        }
    }
}
