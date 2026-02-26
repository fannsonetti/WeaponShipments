using System;
using S1API.Internal.Abstraction;
using S1API.Saveables;
using WeaponShipments.Quests;

namespace WeaponShipments.Data
{
    /// <summary>
    /// Persists quest trigger and one-off state across save/load and game restarts.
    /// </summary>
    public class WSPersistent : Saveable
    {
        [Serializable]
        public class PersistedData
        {
            public bool Act0Started = false;

            public int LeadDay = -1;
            public bool Sent1900 = false;
            public bool Revealed2200 = false;
            public int StandByDay = -1;
            public bool Manor2AMTriggered = false;
            public bool ManorProximityTriggered = false;

            public bool Area1Triggered = false;
            public bool Area2Triggered = false;
            public bool Area3Triggered = false;
            public bool Area4Triggered = false;
            public bool Area5Triggered = false;
            public bool Area6Triggered = false;
            public bool Area7Triggered = false;

            public bool AwaitingWakeup = false;
            public bool DealCompleteAwaitingSleep = false;
            public bool AwaitingWarehouseTalk = false;
            public bool AwaitingVeeperTeleport = false;

            public bool WarehouseZoneEnteredAfterDelivery = false;

            public bool DrillPlaced = false;
            public bool ToolPlaced = false;
            public bool WirePlaced = false;
        }

        [SaveableField("WSPersistent")]
        private PersistedData _data = new PersistedData();

        public static WSPersistent Instance { get; private set; }

        public WSPersistent()
        {
            Instance = this;
        }

        public PersistedData Data => _data;

        protected override void OnLoaded()
        {
            base.OnLoaded();
            Act2StartingSmallQuest.RestorePersistentTriggers(
                _data.ManorProximityTriggered,
                _data.Area1Triggered, _data.Area2Triggered, _data.Area3Triggered,
                _data.Area4Triggered, _data.Area5Triggered, _data.Area6Triggered, _data.Area7Triggered);
        }
    }
}
