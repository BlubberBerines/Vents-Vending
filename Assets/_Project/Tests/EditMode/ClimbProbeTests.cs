using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Player;

namespace VentsAndVending.Tests
{
    /// <summary>
    /// World probing for climbs, against real colliders (final-review fixes: no climbing through walls, the landing
    /// capsule must fit, a real top surface under the landing, no mantles on stairs or ramps).
    /// The player stands at the origin facing +Z.
    /// </summary>
    public class ClimbProbeTests
    {
        private const float Radius = 0.3f;
        private static readonly PlayerTuning T = new PlayerTuning();
        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _made)
            {
                Object.DestroyImmediate(go);
            }

            _made.Clear();
        }

        private void Box(Vector3 min, Vector3 max, float tiltX = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = (min + max) * 0.5f;
            go.transform.localScale = max - min;
            go.transform.rotation = Quaternion.Euler(tiltX, 0f, 0f);
            _made.Add(go);
        }

        private bool Probe(out ClimbPlan plan, PlayerMovementState current = PlayerMovementState.Normal)
        {
            Box(new Vector3(-5f, -0.1f, -5f), new Vector3(5f, 0f, 0.6f)); // floor up to the obstacle
            Physics.SyncTransforms();
            return ClimbProbe.TryFind(Vector3.zero, Vector3.forward, Radius, T, current, ~0, null, out plan);
        }

        [Test]
        public void Crate_ClimbsOnTop()
        {
            Box(new Vector3(-0.5f, 0f, 0.6f), new Vector3(0.5f, 1f, 1.6f));
            Assert.IsTrue(Probe(out ClimbPlan plan));
            Assert.AreEqual(PlayerMovementState.Normal, plan.Posture);
            Assert.AreEqual(1f, plan.TopY, 0.05f);
            Assert.Greater(plan.Target.z, 0.6f);
        }

        [Test]
        public void ThinWallWithCrateBehind_Refused()
        {
            Box(new Vector3(-1f, 0f, 0.6f), new Vector3(1f, 3f, 0.9f));     // 0.3 m wall
            Box(new Vector3(-0.5f, 0f, 0.9f), new Vector3(0.5f, 1f, 1.9f)); // crate flush behind it
            Assert.IsFalse(Probe(out _));
        }

        [Test]
        public void LowBeamOverTheLanding_Refused()
        {
            Box(new Vector3(-0.5f, 0f, 0.6f), new Vector3(0.5f, 1f, 1.6f));
            Box(new Vector3(-1f, 1.6f, 0.98f), new Vector3(1f, 1.8f, 2f));  // over the landing, not the probe point
            Assert.IsFalse(Probe(out _));
        }

        [Test]
        public void ThinParapetOverADrop_Refused()
        {
            Box(new Vector3(-1f, 0f, 0.6f), new Vector3(1f, 0.9f, 0.98f));  // 0.38 m thick railing (the ledge ray lands on it), nothing beyond
            Assert.IsFalse(Probe(out _));
        }

        [Test]
        public void StairStep_IsNotAMantle()
        {
            for (int i = 0; i < 6; i++)
            {
                Box(new Vector3(-1f, 0f, 0.6f + i * 0.3f), new Vector3(1f, 0.2f * (i + 1), 0.9f + i * 0.3f));
            }

            Assert.IsFalse(Probe(out _));
        }

        [Test]
        public void Ramp_IsNotAMantle()
        {
            Box(new Vector3(-1f, -0.3f, 0.6f), new Vector3(1f, 0.6f, 3f), tiltX: -30f);
            Assert.IsFalse(Probe(out _));
        }

        [Test]
        public void OffCentreVentMouth_SnapsIntoTheOpening()
        {
            // a 0.9 m wide, 1.0 m tall opening whose sill is 0.8 m above the feet, 0.35 m off-centre to the right
            float cx = 0.35f;
            Box(new Vector3(-2f, 0f, 0.6f), new Vector3(2f, 0.8f, 2.6f));                 // sill block / duct floor
            Box(new Vector3(-2f, 0.8f, 0.6f), new Vector3(cx - 0.45f, 3f, 2.6f));          // left jamb
            Box(new Vector3(cx + 0.45f, 0.8f, 0.6f), new Vector3(2f, 3f, 2.6f));           // right jamb
            Box(new Vector3(cx - 0.45f, 1.8f, 0.6f), new Vector3(cx + 0.45f, 3f, 2.6f));   // lintel / duct roof
            Assert.IsTrue(Probe(out ClimbPlan plan), ClimbProbe.LastReject);
            Assert.AreEqual(PlayerMovementState.Crawling, plan.Posture);
            Assert.GreaterOrEqual(plan.Target.x, cx - 0.45f + 0.27f, "the landing slides to fit the opening");
            Assert.LessOrEqual(plan.Target.x, cx + 0.45f - 0.27f, "the landing slides to fit the opening");
        }

        [Test]
        public void VentMouthWithShortStubThenRiser_Climbable()
        {
            // like ENV_Wall_VentMouth_A + ENV_Duct_Riser_A: 0.3 m flat stub at the sill, then a 37 degree ramp up
            Box(new Vector3(-2f, 0f, 0.6f), new Vector3(2f, 0.8f, 0.9f));                  // wall below the mouth (stub floor)
            Box(new Vector3(-2f, 0.8f, 0.6f), new Vector3(-0.45f, 3f, 0.9f));              // jambs
            Box(new Vector3(0.45f, 0.8f, 0.6f), new Vector3(2f, 3f, 0.9f));
            Box(new Vector3(-0.45f, 1.8f, 0.6f), new Vector3(0.45f, 3f, 0.7f));            // thin lintel
            // ramp: 0.1 m slab whose top starts at (z 0.9, y 0.8) and rises 0.75 m per metre (centre on the slab's mid-line)
            Box(new Vector3(-0.5f, 1.61f, 0.63f), new Vector3(0.5f, 1.71f, 3.63f), tiltX: -36.87f);
            Box(new Vector3(-0.5f, 2.71f, 0.63f), new Vector3(0.5f, 2.81f, 3.63f), tiltX: -36.87f);   // duct roof 1.0 m above
            Assert.IsTrue(Probe(out ClimbPlan plan), ClimbProbe.LastReject);
            Assert.AreEqual(PlayerMovementState.Crawling, plan.Posture);
        }

        [Test]
        public void CrawlFit_InsideTheRealRiserMesh_IsClear()
        {
            // regression: a zero-length capsule (crawl height + step allowance) falsely overlapped the riser's MeshCollider
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/Kit/ENV_Duct_Riser_A.prefab");
            Assert.IsNotNull(prefab, "run Environment Kit/Configure Imports + Prefabs first");
            var riser = Object.Instantiate(prefab, Vector3.zero, Quaternion.Euler(0f, 180f, 0f));   // rises toward +Z from z 0
            _made.Add(riser);
            Physics.SyncTransforms();
            Assert.IsTrue(ClimbProbe.Fits(new Vector3(0f, 0.02f, 0.1f), Radius, T.CrawlCapsuleHeight, ~0, null));
        }

        [Test]
        public void CrouchedPlayer_StaysCrouchedOnTop()
        {
            Box(new Vector3(-0.5f, 0f, 0.6f), new Vector3(0.5f, 1f, 1.6f));
            Assert.IsTrue(Probe(out ClimbPlan plan, PlayerMovementState.Crouching));
            Assert.AreEqual(PlayerMovementState.Crouching, plan.Posture);
        }
    }
}
