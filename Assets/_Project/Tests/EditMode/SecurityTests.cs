using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Inventory;
using VentsAndVending.Player;
using VentsAndVending.Security;

namespace VentsAndVending.Tests
{
    public class SecurityTests
    {
        // ------------------------------------------------------------------ Rules

        [Test]
        public void Visibility_VentsHide_CrouchAndCrawlHelp()
        {
            Assert.AreEqual(0f, SecurityRules.Visibility(PlayerMovementState.Normal, inVent: true));
            Assert.Less(SecurityRules.Visibility(PlayerMovementState.Crawling, false), SecurityRules.Visibility(PlayerMovementState.Crouching, false));
            Assert.Less(SecurityRules.Visibility(PlayerMovementState.Crouching, false), SecurityRules.Visibility(PlayerMovementState.Normal, false));
        }

        [Test]
        public void Suspiciousness_InnocentPlayerIsIgnored_DealingIsWorst()
        {
            Assert.AreEqual(0f, SecurityRules.Suspiciousness(0, false, false));
            float carrying = SecurityRules.Suspiciousness(2, false, false);
            float dealing = SecurityRules.Suspiciousness(2, true, false);
            Assert.Greater(carrying, 0f);
            Assert.Greater(dealing, carrying);
            Assert.Greater(SecurityRules.Suspiciousness(0, false, true), 0f, "sprinting alone is a little suspicious");
        }

        [Test]
        public void SuspicionRate_RequiresSightConeAndSuspicion()
        {
            Assert.AreEqual(0f, SecurityRules.SuspicionRate(5f, 13f, 10f, 110f, lineOfSight: false, 1f, 1f, 1f));
            Assert.AreEqual(0f, SecurityRules.SuspicionRate(5f, 13f, 70f, 110f, true, 1f, 1f, 1f), "outside the cone");
            Assert.AreEqual(0f, SecurityRules.SuspicionRate(20f, 13f, 0f, 110f, true, 1f, 1f, 1f), "out of range");
            Assert.AreEqual(0f, SecurityRules.SuspicionRate(5f, 13f, 0f, 110f, true, 1f, 0f, 1f), "nothing suspicious");
            Assert.Greater(SecurityRules.SuspicionRate(5f, 13f, 0f, 110f, true, 1f, 1f, 1f), 0f);
        }

        [Test]
        public void SuspicionRate_CloserAndMoreVisibleIsFaster()
        {
            float far = SecurityRules.SuspicionRate(12f, 13f, 0f, 110f, true, 1f, 1f, 1f);
            float near = SecurityRules.SuspicionRate(2f, 13f, 0f, 110f, true, 1f, 1f, 1f);
            float nearCrouched = SecurityRules.SuspicionRate(2f, 13f, 0f, 110f, true, 0.6f, 1f, 1f);
            Assert.Greater(near, far);
            Assert.Greater(near, nearCrouched);
        }

        [Test]
        public void Vigilance_GrowsWithIncidents_Capped()
        {
            Assert.AreEqual(1f, SecurityRules.Vigilance(0));
            Assert.Greater(SecurityRules.Vigilance(2), SecurityRules.Vigilance(1));
            Assert.AreEqual(SecurityRules.Vigilance(6), SecurityRules.Vigilance(50));
        }

        [TestCase(0, 100, 15)]
        [TestCase(3, 100, 30)]
        [TestCase(3, 12, 12)]   // never more than the player has
        [TestCase(2, 0, 0)]
        public void Fine_IsCappedByBalance(int units, int balance, int expected)
        {
            Assert.AreEqual(expected, SecurityRules.Fine(units, balance));
        }

        [Test]
        public void Confiscation_TakesOnlyContraband()
        {
            var juice = ScriptableObject.CreateInstance<ItemDefinition>();
            juice.Configure("juice", "juice", "", ItemCategory.Product, 10, 12, Color.green);
            juice.SetContraband(true);
            var gel = ScriptableObject.CreateInstance<ItemDefinition>();
            gel.Configure("gel", "gel", "", ItemCategory.Ingredient, 10, 3, Color.cyan);

            var inv = new InventoryModel(4);
            inv.Add(juice, 2, ItemQuality.Premium);
            inv.Add(gel, 3);
            inv.Add(juice, 1, ItemQuality.Poor);
            Assert.AreEqual(3, SecurityRules.CountContraband(inv));

            var taken = SecurityRules.ConfiscateContraband(inv);
            Assert.AreEqual(2, taken.Count);
            Assert.AreEqual(0, SecurityRules.CountContraband(inv));
            Assert.AreEqual(3, inv.Count(gel));

            Object.DestroyImmediate(juice);
            Object.DestroyImmediate(gel);
        }

        // ------------------------------------------------------------------ Alert state machine

        [Test]
        public void Alert_BuildsToSuspiciousThenPursuit()
        {
            var alert = new DroneAlert();
            alert.Tick(0.5f, 1f, true, false);
            Assert.AreEqual(AlertState.Suspicious, alert.State);
            alert.Tick(0.6f, 1f, true, false);
            Assert.AreEqual(AlertState.Pursuing, alert.State);
        }

        [Test]
        public void Alert_SuspicionDecaysBackToUnaware()
        {
            var alert = new DroneAlert();
            alert.Tick(0.4f, 1f, true, false);
            Assert.AreEqual(AlertState.Suspicious, alert.State);
            for (int i = 0; i < 100; i++)
            {
                alert.Tick(0.1f, 0f, false, false);
            }
            Assert.AreEqual(AlertState.Unaware, alert.State);
            Assert.AreEqual(0f, alert.Suspicion);
        }

        [Test]
        public void Alert_LosingSight_Searches_ThenGivesUp()
        {
            var alert = new DroneAlert();
            alert.Tick(2f, 1f, true, false);
            Assert.AreEqual(AlertState.Pursuing, alert.State);

            alert.Tick(DroneAlert.LoseSightSeconds + 0.1f, 0f, false, false);
            Assert.AreEqual(AlertState.Searching, alert.State);

            for (int i = 0; i < 100; i++)
            {
                alert.Tick(0.1f, 0f, false, false);
            }
            Assert.AreEqual(AlertState.Unaware, alert.State);
        }

        [Test]
        public void Alert_RespottedWhileSearching_ResumesPursuitQuickly()
        {
            var alert = new DroneAlert();
            alert.Tick(2f, 1f, true, false);
            alert.Tick(DroneAlert.LoseSightSeconds + 0.1f, 0f, false, false);
            Assert.AreEqual(AlertState.Searching, alert.State);

            alert.Tick(0.5f, 1f, true, false); // 0.7 + 0.5 → full
            Assert.AreEqual(AlertState.Pursuing, alert.State);
        }

        [Test]
        public void Alert_StayingClose_GetsCaught_AndResets()
        {
            var alert = new DroneAlert();
            int caught = 0;
            alert.Caught += () => caught++;
            alert.Tick(2f, 1f, true, false);
            alert.Tick(DroneAlert.CatchSeconds * 0.5f, 1f, true, true);
            Assert.AreEqual(0, caught);
            alert.Tick(DroneAlert.CatchSeconds * 0.6f, 1f, true, true);
            Assert.AreEqual(1, caught);
            Assert.AreEqual(AlertState.Unaware, alert.State);
            Assert.AreEqual(0f, alert.Suspicion);
        }

        [Test]
        public void Alert_BreakingAwayResetsCatchProgress()
        {
            var alert = new DroneAlert();
            int caught = 0;
            alert.Caught += () => caught++;
            alert.Tick(2f, 1f, true, false);
            alert.Tick(DroneAlert.CatchSeconds * 0.8f, 1f, true, true);
            alert.Tick(1f, 1f, true, false);   // ran out of range — progress bleeds off
            alert.Tick(DroneAlert.CatchSeconds * 0.5f, 1f, true, true);
            Assert.AreEqual(0, caught);
        }

        // ---------------------------------------------------------------- Underpass: trespass + hiding

        [Test]
        public void Hidden_IsInvisibleInAnyPosture() =>
            Assert.AreEqual(0f, SecurityRules.Visibility(PlayerMovementState.Normal, false, hidden: true));

        [Test]
        public void Trespassing_EmptyHanded_IsSuspicious() =>
            Assert.AreEqual(0.6f, SecurityRules.Suspiciousness(0, false, false, trespassing: true), 1e-4f);

        [Test]
        public void Trespassing_StacksWithContraband() =>
            Assert.AreEqual(0.5f + 0.08f + 0.6f, SecurityRules.Suspiciousness(1, false, false, trespassing: true), 1e-4f);

        [Test]
        public void Escort_OnlyWhenRestrictedAndHasExit()
        {
            Assert.IsTrue(SecurityRules.ShouldEscort(true, true));
            Assert.IsFalse(SecurityRules.ShouldEscort(true, false));
            Assert.IsFalse(SecurityRules.ShouldEscort(false, true));
        }

        [Test]
        public void FlightHeight_IsRelativeToTheFloor_WhenPatrollingAndPursuing()
        {
            Assert.AreEqual(-1.4f, SecurityRules.FlightHeight(false, -3.6f, 2.2f, 2.2f, 0f), 1e-4f);
            Assert.AreEqual(-1.6f, SecurityRules.FlightHeight(true, -3.6f, 2.2f, 2.0f, 0.5f), 1e-4f, "pursuit ignores waypoint offsets");
            Assert.AreEqual(3.0f, SecurityRules.FlightHeight(false, 0f, 2.7f, 2.2f, 0.3f), 1e-4f);
        }

        [Test]
        public void OnSameLevel_IgnoresPlayersOnAnotherFloor()
        {
            Assert.IsTrue(SecurityRules.OnSameLevel(-3.56f, -3.6f), "player in the underpass");
            Assert.IsFalse(SecurityRules.OnSameLevel(0.04f, -3.6f), "player in the gallery above the open grate");
            Assert.IsTrue(SecurityRules.OnSameLevel(0.02f, 0f), "plaza drones and players share the floor");
            Assert.IsTrue(SecurityRules.OnSameLevel(0.6f, 0f), "standing on a crate in the plaza still counts");
        }

        [Test]
        public void DefaultArgs_MatchOldBehaviour() =>
            Assert.AreEqual(0f, SecurityRules.Suspiciousness(0, false, false));
    }
}
