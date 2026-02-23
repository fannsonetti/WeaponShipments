using MelonLoader;
using S1API.Entities;
using S1API.GameTime;
using S1API.Quests;
using UnityEngine;
using WeaponShipments.Data;

namespace WeaponShipments.Quests
{
    /// <summary>
    /// Act 3: Unpacking. Go to the warehouse and unpack everything from the van onto the floor.
    /// </summary>
    public class Act3UnpackingQuest : Quest
    {
        protected override string Title => "Unpacking";
        protected override string Description => "Unpack the van at the warehouse.";
        protected override bool AutoBegin => false;

        private static readonly Vector3 WarehouseUnpackPos = new Vector3(-26f, -4.3f, 173.5f);
        private const float UnpackProximityRadius = 5f;
        private const float UnpackDurationRequired = 2f;
        private bool _tickHooked;
        private float _nearVanTime;
        private GameObject _cachedVan;

        private WSSaveData.SavedUnpackingQuest Saved => WSSaveData.Instance?.UnpackingQuest;

        private int _stageFallback = 0;

        internal int Stage
        {
            get => Saved != null ? Saved.Stage : _stageFallback;
            set
            {
                if (Saved != null) Saved.Stage = value;
                _stageFallback = value;
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            if (QuestEntries.Count == 0)
                CreateEntries();
            EnsureTickHooked();
        }

        protected override void OnCreated()
        {
            base.OnCreated();
            if (QuestEntries.Count == 0)
                CreateEntries();
        }

        private void CreateEntries()
        {
            AddEntry("Go to the warehouse and unpack everything from the van", WarehouseUnpackPos);
        }

        internal void EnsureTickHooked()
        {
            if (_tickHooked || Stage < 1) return;
            _tickHooked = true;
            TimeManager.OnTick += OnTick;
        }

        private void OnTick()
        {
            if (Stage != 1 || QuestEntries.Count == 0) return;

            var player = Player.Local;
            if (player == null) return;

            if (_cachedVan == null)
                _cachedVan = GameObject.Find("equipmentvan") ?? GameObject.Find("equipmentcar");
            var van = _cachedVan;
            if (van == null) { _nearVanTime = 0f; return; }

            var vanPos = van.transform.root != null ? van.transform.root.position : van.transform.position;
            bool nearVan = Vector3.Distance(player.Position, vanPos) <= UnpackProximityRadius &&
                          Vector3.Distance(vanPos, WarehouseUnpackPos) <= UnpackProximityRadius;

            if (nearVan)
            {
                _nearVanTime += UnityEngine.Time.deltaTime;
                if (_nearVanTime >= UnpackDurationRequired)
                {
                    TimeManager.OnTick -= OnTick;
                    _tickHooked = false;
                    if (QuestEntries.Count >= 1) QuestEntries[0].Complete();
                    Stage = 2;
                    WeaponShipments.Quests.QuestManager.TryStartMovingUpOnZoneEntry();
                    MelonLogger.Msg("[Act3] Unpacking complete.");
                }
            }
            else
            {
                _nearVanTime = 0f;
            }
        }
    }
}
