using NUnit.Framework;
using UnityEngine;
using VentsAndVending.World;

namespace VentsAndVending.Tests
{
    /// <summary>Pushable crate rules (station layers spec §6.2): one step along a cardinal axis, never into a wall or off an edge.</summary>
    public class CratePushTests
    {
        [Test]
        public void Axis_SnapsToZ() => Assert.AreEqual(Vector3.forward, CratePushLogic.PushAxis(new Vector3(0.3f, -0.5f, 0.9f)));

        [Test]
        public void Axis_SnapsToNegX() => Assert.AreEqual(Vector3.left, CratePushLogic.PushAxis(new Vector3(-0.8f, 0.2f, 0.4f)));

        [Test]
        public void Axis_LookingStraightDown_StillCardinal()
        {
            Vector3 axis = CratePushLogic.PushAxis(new Vector3(0.01f, -1f, -0.02f));
            Assert.AreEqual(0f, axis.y);
            Assert.AreEqual(1f, axis.magnitude, 1e-5);
        }

        [Test]
        public void Blocked_Refused() => Assert.IsFalse(CratePushLogic.CanPush(true, true));

        [Test]
        public void NoGround_Refused() => Assert.IsFalse(CratePushLogic.CanPush(false, false));

        [Test]
        public void Clear_Allowed() => Assert.IsTrue(CratePushLogic.CanPush(false, true));
    }
}
