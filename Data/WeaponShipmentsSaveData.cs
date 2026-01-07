using System;
using S1API.Internal.Abstraction;
using S1API.Saveables;
using WeaponShipments.Saveables;

namespace WeaponShipments.Data
{
    /// <summary>
    /// S1API saveable backing store for the Weapon Shipments business.
    /// Automatically discovered by S1API because it inherits Saveable.
    /// </summary>
    public class WeaponShipmentsSaveData : Saveable
    {
        [Serializable]
        public class SavedAct0ContactQuest
        {
            public int Stage = 0;
            public bool AwaitingWakeup = false;
            public int LeadDay = -1;
            public bool Sent1900 = false;
            public bool Revealed2200 = false;
        }

        [Serializable]
        public class PersistedData
        {
            // Grouped, human-readable save sections
            public SavedStock Stock = new SavedStock();
            public SavedProperties Properties = new SavedProperties();
            public SavedStats Stats = new SavedStats();
            public SavedAct0ContactQuest Act0Contact = new SavedAct0ContactQuest();
        }

        // This is what S1API serializes to JSON
        [SaveableField("WeaponShipmentsState")]
        private PersistedData _data = new PersistedData();

        /// <summary>
        /// Live instance S1API created. BusinessState/UI can reference this.
        /// </summary>
        public static WeaponShipmentsSaveData Instance { get; private set; }

        public WeaponShipmentsSaveData()
        {
            Instance = this;
        }

        /// <summary>
        /// Expose data for other systems (BusinessState, UI) to read/write.
        /// </summary>
        public PersistedData Data => _data;

        /// <summary>
        /// Expose Act0 quest save data for the quest system.
        /// </summary>
        public SavedAct0ContactQuest Act0Contact => _data.Act0Contact;

        /// <summary>
        /// Called by S1API after JSON has been loaded.
        /// Push values into BusinessState's runtime fields.
        /// </summary>
        protected override void OnLoaded()
        {
            base.OnLoaded();
            BusinessState.ApplyLoadedData(_data);
        }
    }
}
