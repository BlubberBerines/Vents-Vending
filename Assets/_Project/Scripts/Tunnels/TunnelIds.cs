namespace VentsAndVending.Tunnels
{
    /// <summary>Stable IDs for the tunnel slice (Docs/Design/TunnelNetwork.md §5–6). Never rename: they live in save files.</summary>
    public static class TunnelIds
    {
        public const string Nook = "R01";
        public const string ServiceJunction = "R02";
        public const string LongCrawl = "R03";
        public const string FanJunction = "R04";
        public const string PlazaAccess = "R05";
        public const string DropHatch = "R06";
        public const string PlazaServiceHub = "R07";
        public const string CustomerVent = "R08";
        public const string PowerDistribution = "R16";
        public const string FanRoom = "R17";
        public const string StashDuct = "X01";
        public const string Underpass = "U01";

        /// <summary>Breaker in room 16 that powers the extraction fan (starts on).</summary>
        public const string FanBreaker = "BRK_FAN_04";
        /// <summary>The extraction fan system it controls.</summary>
        public const string Fan = "FAN_17";
        /// <summary>Staff door from the service hub into the plaza (starts latched; opens from the hub side only).</summary>
        public const string StaffDoor = "GATE_STAFF_07";
        /// <summary>Rusted floor grate in the Long Crawl, pried open with a crowbar (one-way drop into the underpass).</summary>
        public const string GrateGate = "GATE_GRATE_03";
        /// <summary>Emergency exit from the underpass into the plaza (push-bar, inside only).</summary>
        public const string UnderpassExit = "GATE_EXIT_U01";
        /// <summary>The crowbar lying in the stash duct has been taken.</summary>
        public const string CrowbarPickup = "PICKUP_CROWBAR_X01";
    }
}
