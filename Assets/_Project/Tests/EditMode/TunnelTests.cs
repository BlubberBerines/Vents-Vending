using NUnit.Framework;
using VentsAndVending.Saving;
using VentsAndVending.Tunnels;

namespace VentsAndVending.Tests.EditMode
{
    public sealed class TunnelTests
    {
        [Test]
        public void Discover_ReturnsTrueOnlyTheFirstTime()
        {
            var state = new TunnelState();
            Assert.IsTrue(state.Discover(TunnelIds.FanJunction));
            Assert.IsFalse(state.Discover(TunnelIds.FanJunction));
            Assert.IsTrue(state.IsDiscovered(TunnelIds.FanJunction));
            Assert.IsFalse(state.IsDiscovered(TunnelIds.FanRoom));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Discover_IgnoresBlankIds(string id)
        {
            var state = new TunnelState();
            Assert.IsFalse(state.Discover(id));
            Assert.AreEqual(0, state.Discovered.Count);
        }

        [Test]
        public void Switch_UsesFallbackUntilSet()
        {
            var state = new TunnelState();
            Assert.IsTrue(state.GetSwitch(TunnelIds.FanBreaker, true), "breakers start on");
            Assert.IsFalse(state.GetSwitch(TunnelIds.StaffDoor, false), "latches start shut");
        }

        [Test]
        public void SetSwitch_RaisesEventOnlyOnChange()
        {
            var state = new TunnelState();
            int events = 0;
            state.SwitchChanged += (id, on) => events++;

            Assert.IsFalse(state.SetSwitch(TunnelIds.FanBreaker, true, true), "already on by default");
            Assert.IsTrue(state.SetSwitch(TunnelIds.FanBreaker, false, true));
            Assert.IsFalse(state.SetSwitch(TunnelIds.FanBreaker, false, true));
            Assert.IsTrue(state.SetSwitch(TunnelIds.FanBreaker, true, true));
            Assert.AreEqual(2, events);
        }

        [Test]
        public void CaptureRestore_RoundTripsRoomsAndSwitches()
        {
            var state = new TunnelState();
            state.Discover(TunnelIds.Nook);
            state.Discover(TunnelIds.PowerDistribution);
            state.SetSwitch(TunnelIds.FanBreaker, false, true);
            state.SetSwitch(TunnelIds.StaffDoor, true, false);

            var restored = new TunnelState();
            restored.Discover(TunnelIds.FanRoom); // stale state must be replaced, not merged
            restored.Restore(state.Capture());

            Assert.IsTrue(restored.IsDiscovered(TunnelIds.Nook));
            Assert.IsTrue(restored.IsDiscovered(TunnelIds.PowerDistribution));
            Assert.IsFalse(restored.IsDiscovered(TunnelIds.FanRoom));
            Assert.IsFalse(restored.GetSwitch(TunnelIds.FanBreaker, true));
            Assert.IsTrue(restored.GetSwitch(TunnelIds.StaffDoor, false));
        }

        [Test]
        public void Restore_ToleratesNullAndBlankEntries()
        {
            var save = new TunnelSave { DiscoveredRooms = null, Switches = null };
            var state = new TunnelState();
            Assert.DoesNotThrow(() => state.Restore(save));
            Assert.DoesNotThrow(() => state.Restore(null));

            save = new TunnelSave();
            save.DiscoveredRooms.Add("");
            save.Switches.Add(null);
            save.Switches.Add(new SwitchSave { Id = "", On = true });
            state.Restore(save);
            Assert.AreEqual(0, state.Discovered.Count);
            Assert.AreEqual(0, state.Switches.Count);
        }

        [Test]
        public void Capture_IsSortedForStableSaveFiles()
        {
            var state = new TunnelState();
            state.Discover(TunnelIds.FanRoom);
            state.Discover(TunnelIds.Nook);
            state.SetSwitch("ZZZ", true, false);
            state.SetSwitch("AAA", true, false);
            TunnelSave save = state.Capture();
            CollectionAssert.AreEqual(new[] { TunnelIds.Nook, TunnelIds.FanRoom }, save.DiscoveredRooms);
            Assert.AreEqual("AAA", save.Switches[0].Id);
        }

        [Test]
        public void VersionOneSave_LoadsWithEmptyTunnelState()
        {
            Assert.IsTrue(SaveCodec.TryParse("{\"Version\":1,\"Credits\":40}", out SaveData data, out string error), error);
            Assert.IsNotNull(data.Tunnels);
            Assert.AreEqual(0, data.Tunnels.DiscoveredRooms.Count);
            Assert.AreEqual(0, data.Tunnels.Switches.Count);
            Assert.AreEqual(40, data.Credits);
        }

        [Test]
        public void TunnelState_SurvivesJsonRoundTrip()
        {
            var state = new TunnelState();
            state.Discover(TunnelIds.FanJunction);
            state.SetSwitch(TunnelIds.FanBreaker, false, true);
            var data = new SaveData { Tunnels = state.Capture() };

            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(data), out SaveData parsed, out _));
            var restored = new TunnelState();
            restored.Restore(parsed.Tunnels);
            Assert.IsTrue(restored.IsDiscovered(TunnelIds.FanJunction));
            Assert.IsFalse(restored.GetSwitch(TunnelIds.FanBreaker, true));
        }
    }
}
