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
    public class WSSaveData : Saveable
    {
        [Serializable]
        public class SavedNewNumberQuest
        {
            public int Stage = 0;
        }

        [Serializable]
        public class SavedStartingSmallQuest
        {
            public int Stage = 0;
            public bool Sent1900 = false;
            public bool Revealed2200 = false;
            public bool MissedMeetupWindowToday = false;
            public bool SentUrgency10 = false;
            public bool SentUrgency1130 = false;
            public bool SentUrgency1230 = false;
        }

        [Serializable]
        public class SavedUnpackingQuest
        {
            public int Stage = 0;
        }

        [Serializable]
        public class SavedMovingUpQuest
        {
            public int Stage = 0;
        }

        [Serializable]
        public class PersistedData
        {
            // Grouped, human-readable save sections
            public SavedStock Stock = new SavedStock();
            public SavedProperties Properties = new SavedProperties();
            public SavedStats Stats = new SavedStats();
            public SavedNewNumberQuest NewNumberQuest = new SavedNewNumberQuest();
            public SavedStartingSmallQuest StartingSmallQuest = new SavedStartingSmallQuest();
            public SavedUnpackingQuest UnpackingQuest = new SavedUnpackingQuest();
            public SavedMovingUpQuest MovingUpQuest = new SavedMovingUpQuest();
        }

        // This is what S1API serializes to JSON
        [SaveableField("WeaponShipmentsState")]
        private PersistedData _data = new PersistedData();

        /// <summary>
        /// Live instance S1API created. BusinessState/UI can reference this.
        /// </summary>
        public static WSSaveData Instance { get; private set; }

        public WSSaveData()
        {
            Instance = this;
        }

        /// <summary>
        /// Expose data for other systems (BusinessState, UI) to read/write.
        /// </summary>
        public PersistedData Data => _data;

        /// <summary>
        /// Expose Quest 1 (New Number, New Problems) save data.
        /// </summary>
        public SavedNewNumberQuest NewNumberQuest => _data.NewNumberQuest;

        /// <summary>
        /// Expose Quest 2 (Starting Small) save data.
        /// </summary>
        public SavedStartingSmallQuest StartingSmallQuest => _data.StartingSmallQuest;

        /// <summary>
        /// Expose Quest 3 (Unpacking) save data.
        /// </summary>
        public SavedUnpackingQuest UnpackingQuest => _data.UnpackingQuest;

        /// <summary>
        /// Expose Quest 4 (Moving Up) save data.
        /// </summary>
        public SavedMovingUpQuest MovingUpQuest => _data.MovingUpQuest;

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
