using System;
using S1API.Internal.Abstraction;
using S1API.Saveables;

namespace WeaponShipments.Data
{
    /// <summary>
    /// S1API saveable backing store for the Weapon Shipments business.
    /// This is automatically discovered by S1API because it inherits Saveable.
    /// </summary>
    public class WeaponShipmentsSaveData : Saveable
    {
        [Serializable]
        public class PersistedData
        {
            public float Supplies = 0f;
            public float Stock = 0f;


            public bool HasPendingSupplyShipment;
            public float SupplyShipmentArrives;


            public bool EquipmentOwned = false;
            public bool StaffOwned = false;
            public bool SecurityOwned = false;


            public float TotalEarnings = 0f;
            public int TotalSalesCount = 0;
            public float TotalStockProduced = 0f;

            public int ResupplyJobsStarted = 0;
            public int ResupplyJobsCompleted = 0;

            public int HylandSellAttempts = 0;
            public int HylandSellSuccesses = 0;


            public bool WarehouseOwned = false;
            public bool WarehouseCompromised = false;

            public bool GarageOwned = false;

            public bool BunkerOwned = false;

        }

        // This is what S1API actually serializes
        [SaveableField("WeaponShipmentsState")]
        private PersistedData _data = new PersistedData();

        /// <summary>
        /// Live instance S1API created. BusinessState uses this.
        /// </summary>
        public static WeaponShipmentsSaveData Instance { get; private set; }

        public WeaponShipmentsSaveData()
        {
            Instance = this;
        }

        /// <summary>
        /// Expose data for BusinessState to sync with.
        /// </summary>
        public PersistedData Data => _data;

        /// <summary>
        /// Called by S1API after JSON has been loaded.
        /// Pushes values into BusinessState's static fields.
        /// </summary>
        protected override void OnLoaded()
        {
            base.OnLoaded();
            BusinessState.ApplyLoadedData(_data);
        }
    }
}
