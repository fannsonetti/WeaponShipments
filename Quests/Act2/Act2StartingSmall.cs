using CustomNPCTest.NPCs;
using MelonLoader;
using S1API.Cartel;
using S1API.GameTime;
using S1API.Law;
using S1API.Quests;
using S1API.Vehicles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.AI;
using WeaponShipments.Data;
using WeaponShipments.NPCs;
using WeaponShipments.Services;
using S1API.Entities;

namespace WeaponShipments.Quests
{
    /// <summary>
    /// Quest 2 (Act 2): "Starting Small". Current Manny/Archie/Igor dialogue and teleporting, then manor, truck, deliver, unlock manufacturing.
    /// </summary>
    public class Act2StartingSmallQuest : Quest
    {
        protected override string Title => "Starting Small";
        protected override string Description => "Get your first base and start production.";
        protected override bool AutoBegin => false;
        protected override Sprite? QuestIcon => WeaponShipments.Utils.QuestIconLoader.Load("quest_starting_small.png");

        private WSSaveData.SavedStartingSmallQuest Saved => WSSaveData.Instance?.StartingSmallQuest;
        private WSPersistent.PersistedData P => WSPersistent.Instance?.Data;

        private int _stageFallback = 0;
        private bool _awaitingWakeupFallback = false;
        private int _leadDayFallback = -1;
        private bool _sent1900Fallback = false;
        private bool _revealed2200Fallback = false;

        private int Stage
        {
            get => Saved != null ? Saved.Stage : _stageFallback;
            set
            {
                if (Saved != null) Saved.Stage = value;
                _stageFallback = value;
            }
        }

        private bool AwaitingWakeup
        {
            get => P != null ? P.AwaitingWakeup : _awaitingWakeupFallback;
            set
            {
                if (P != null) P.AwaitingWakeup = value;
                _awaitingWakeupFallback = value;
            }
        }

        private int LeadDay
        {
            get => P != null ? P.LeadDay : _leadDayFallback;
            set
            {
                if (P != null) P.LeadDay = value;
                _leadDayFallback = value;
            }
        }

        private bool Sent1900
        {
            get => Saved != null ? Saved.Sent1900 : (P != null ? P.Sent1900 : _sent1900Fallback);
            set
            {
                if (Saved != null) Saved.Sent1900 = value;
                if (P != null) P.Sent1900 = value;
                _sent1900Fallback = value;
            }
        }

        private bool Revealed2200
        {
            get => Saved != null ? Saved.Revealed2200 : (P != null ? P.Revealed2200 : _revealed2200Fallback);
            set
            {
                if (Saved != null) Saved.Revealed2200 = value;
                if (P != null) P.Revealed2200 = value;
                _revealed2200Fallback = value;
            }
        }

        private int StandByDay
        {
            get => P != null ? P.StandByDay : _standByDayFallback;
            set
            {
                if (P != null) P.StandByDay = value;
                _standByDayFallback = value;
            }
        }

        private int _standByDayFallback = -1;

        private bool Manor2AMTriggered
        {
            get => P != null ? P.Manor2AMTriggered : _manor2AMTriggeredFallback;
            set
            {
                if (P != null) P.Manor2AMTriggered = value;
                _manor2AMTriggeredFallback = value;
            }
        }

        private bool _manor2AMTriggeredFallback = false;

        private QuestEntry _waitForEmployeeEntry;
        private QuestEntry _mannyMeetupEntry;
        private QuestEntry _hireArchieEntry;

        private static readonly Vector3 DocksPos = new Vector3(-98.23f, -1.535f, -38.7985f);
        private static readonly Vector3 WarehouseDeliveryPOI = new Vector3(-60.308f, -1.535f, 35.6748f);
        private static readonly Vector3 WarehouseStashPOI = new Vector3(-26f, -4.3f, 173.5f);
        private static readonly Vector3 EquipmentPos = new Vector3(-48.5173f, -2.1f, 40.4007f);

        private static readonly Vector3 ManorWaypoint = new Vector3(166.4817f, 10.9525f, -78.4776f);
        private const float ManorProximityUnits = 5f;

        private static readonly Vector3 ManorVeeperPosition = new Vector3(167.5437f, 10.5f, -80.6857f);
        private static readonly Vector3 ManorVeeperRotationEuler = new Vector3(12f, 160f, 0f);

        private static object _manorVeeper;
        private static List<CartelGoon> _manorGoons = new List<CartelGoon>();
        private static bool _manorProximityTriggered;

        private const float CartelAreaTriggerUnits = 10f;

        private static readonly Vector3 Area1HotboxPos = new Vector3(87.8479f, 4.9217f, -104.5733f);
        private static readonly Vector3 Area1HotboxRot = new Vector3(0f, 168f, 0f);
        private static readonly Vector3[] Area1GoonPositions = { new Vector3(89.898f, 4.975f, -105.5867f), new Vector3(88.9903f, 4.975f, -102.4628f) };
        private static readonly (Vector3 pos, string weapon)[] Area2Goons = {
            (new Vector3(90.0122f, 0.975f, 3.08f), "Avatar/Equippables/M1911"),
            (new Vector3(93.4589f, 0.975f, 3.4219f), "Avatar/Equippables/M1911")
        };
        private static readonly Vector3 Area3GoonSpawnPos = new Vector3(32.5622f, 0.9732f, 9.8543f);
        private static readonly (Vector3 pos, string weapon)[] Area3Goons = {
            (Area3GoonSpawnPos, "Avatar/Equippables/M1911"),
            (Area3GoonSpawnPos + Vector3.right, "Avatar/Equippables/M1911")
        };
        private static readonly string Area3Agent28Message = "You should lay low inside the black market.";
        private static readonly Vector3 Area4GoonPos = new Vector3(-20.5156f, 0.975f, 49.4325f);
        private static readonly (Vector3 pos, string weapon)[] Area5Goons = { (new Vector3(-25.7118f, 0.975f, 92.4534f), "Avatar/Equippables/M1911") };

        private static readonly Vector3 Checkpoint6GoonPos1 = new Vector3(-58.1712f, -1.5814f, 116.3431f);
        private static readonly Vector3 Checkpoint6GoonPos2 = new Vector3(-66.3852f, -3.035f, 135.8637f);
        private static readonly (Vector3 pos, string weapon)[][] CheckpointPositionsAndWeapons = {
            new[] { (Area1GoonPositions[0], "Avatar/Equippables/M1911"), (Area1GoonPositions[1], "Avatar/Equippables/M1911") },
            Area2Goons,
            Area3Goons,
            new[] { (Area4GoonPos, "Avatar/Equippables/M1911"), (Area4GoonPos + Vector3.right * 2f, "Avatar/Equippables/M1911") },
            new[] { (Checkpoint6GoonPos1, "Avatar/Equippables/M1911"), (Checkpoint6GoonPos2, "Avatar/Equippables/M1911") }
        };

        private static List<CartelGoon> _checkpointGoons = new List<CartelGoon>();
        private static bool _area1Triggered;
        private static bool _area2Triggered;
        private static bool _area3Triggered;
        private static bool _area4Triggered;
        private static bool _area5Triggered;

        private static readonly (string code, bool black, Vector3 pos, Vector3 rot)[] Area2Vehicles = {
            ("Bruiser", true, new Vector3(95.1127f, 0.9781f, 4.8815f), new Vector3(0f, 115f, 0f)),
            ("Shitbox", true, new Vector3(88.9878f, 0.884f, 5.2527f), new Vector3(0f, 105f, 0f))
        };
        private static readonly (string code, bool black, Vector3 pos, Vector3 rot)[] Area3Vehicles = {
            ("Dinkler", true, new Vector3(32.1902f, 0.9725f, 7.1645f), new Vector3(0f, 292f, 0f))
        };
        private static readonly (string code, bool black, Vector3 pos, Vector3 rot)[] Area4Vehicles = {
            ("Hotbox", false, new Vector3(-19.1016f, 0.9217f, 45.9477f), new Vector3(0f, 307f, 0f)),
            ("Dinkler", true, new Vector3(-23.4799f, 0.9138f, 51.1484f), new Vector3(0f, 346f, 0f))
        };
        private static readonly (string code, bool black, Vector3 pos, Vector3 rot)[] Area5Vehicles = {
            ("Dinkler", true, new Vector3(-28.5322f, 0.9138f, 89.8599f), new Vector3(0f, 11f, 0f))
        };
        private static readonly (string code, bool black, Vector3 pos, Vector3 rot)[] ExtraQuestVehicles = {
            ("Bruiser", false, new Vector3(36.0964f, 0.6f, 50.8586f), new Vector3(0f, 164f, 0f)),
            ("Shitbox", true, new Vector3(30.7958f, 0.6f, 57.8652f), new Vector3(0f, 98f, 0f)),
            ("Bruiser", false, new Vector3(-37.9073f, -3.1382f, 143.4657f), new Vector3(0f, 260f, 0f))
        };

        private static readonly Vector3 LoseTheCartelMarkerPos = new Vector3(-60.7774f, -1.535f, 35.9243f);

        private const float CheckpointTriggerUnits = 30f;
        private const float Checkpoint5To7TriggerUnits = 20f;
        private static readonly Vector3[] CheckpointVehiclePositions = {
            Area1HotboxPos,
            Area2Vehicles[0].pos,
            Area3Vehicles[0].pos,
            Area4Vehicles[0].pos
        };
        private static readonly Vector3[] CheckpointGoonSpawnPositions = {
            new Vector3(84.8797f, 4.975f, -104.9853f),
            new Vector3(91.6779f, 0.975f, 7.5306f),
            new Vector3(27.0366f, 1.0632f, 8.0946f),
            new Vector3(-22.1626f, 0.6818f, 47.7096f),
            new Vector3(-27.3319f, 1.065f, 86.2879f)
        };
        private static readonly Vector3 Checkpoint6TriggerPos = new Vector3(-53.8771f, -3.025f, 130.4513f);
        private static readonly Vector3 Checkpoint7TriggerPos = new Vector3(-142.7857f, -3.025f, 122.6559f);
        private static readonly string Checkpoint6Agent28Message = "You lost the tail. Head to the black market.";
        private static readonly string BlackMarketDeliveryAgent28Message = "I'm going to paint the van and move all the equipment in. Get some rest.";
        private static bool _area6Triggered;
        private static bool _area7Triggered;
        private static readonly List<GameObject> _cartelVehicles = new List<GameObject>();
        private static int _cartelVehicleCounter;

        private bool _timeHooksAttached = false;
        private bool _loadedFromSave = false;

        protected override void OnLoaded()
        {
            base.OnLoaded();
            _loadedFromSave = true;

            if (QuestEntries.Count == 0)
                CreateEntries();

            RebindEntriesFromList();
        }

        protected override void OnCreated()
        {
            base.OnCreated();

            if (QuestEntries.Count == 0)
                CreateEntries();

            RebindEntriesFromList();
            AttachTimeHooksOnce();

            if (_loadedFromSave || (Saved != null && Saved.Stage > 0))
                MelonCoroutines.Start(ApplyStageSideEffectsNextFrame());
            else
                RestoreStageStateForNewQuestOnly();
        }

        private static readonly Vector3[] ManorGoonPositions = {
            new Vector3(175.2189f, 10.9723f, -78.302f),
            new Vector3(163.368f, 10.9547f, -78.874f),
            new Vector3(168f, 10.95f, -76f)
        };

        private void CreateEntries()
        {
            AddEntry("Wait for Agent 28 to find an employee", DocksPos);
            AddEntry("Wait for the drop-off location", DocksPos);
            AddEntry("Meet up with Manny", DocksPos);
            AddEntry("Hire Archie", DocksPos);
            AddEntry("Stand by for the window");
            AddEntry("Go to the Cartel Manor", ManorWaypoint);
            AddEntry("Defeat the goons", DocksPos);
            AddEntry("Lose the cartel");
            AddEntry("Deliver to the black market", WarehouseDeliveryPOI);
        }

        private void RebindEntriesFromList()
        {
            if (QuestEntries.Count >= 1) _waitForEmployeeEntry = QuestEntries[0];
            if (QuestEntries.Count >= 3) _mannyMeetupEntry = QuestEntries[2];
            if (QuestEntries.Count >= 4) _hireArchieEntry = QuestEntries[3];
        }

        private void AttachTimeHooksOnce()
        {
            if (_timeHooksAttached) return;
            _timeHooksAttached = true;
            TimeManager.OnSleepEnd += OnSleepEnd;
            TimeManager.OnTick += OnTick;
            MelonCoroutines.Start(ManorProximityAndGoonsLoop());
        }

        private static bool TryGetEffectivePlayerOrVehiclePosition(out Vector3 position)
        {
            position = Vector3.zero;
            var player = Player.Local;
            if (player == null) return false;
            var driverVehicle = TryGetVehicleDrivenByPlayer(player);
            if (driverVehicle != null && TryGetVehiclePosition(driverVehicle, out var vehiclePos))
            {
                position = vehiclePos;
                return true;
            }
            position = player.Position;
            return true;
        }

        private static object TryGetVehicleDrivenByPlayer(Player player)
        {
            if (player == null) return null;
            try
            {
                if (_cachedLandVehicleType == null)
                {
                    var asm = System.Reflection.Assembly.Load("Assembly-CSharp");
                    _cachedLandVehicleType = asm?.GetType("ScheduleOne.Vehicles.LandVehicle") ?? asm?.GetType("LandVehicle");
                    if (_cachedLandVehicleType != null)
                        _cachedDriverPlayerProp = _cachedLandVehicleType.GetProperty("DriverPlayer", BindingFlags.Public | BindingFlags.Instance);
                }
                if (_cachedLandVehicleType == null || _cachedDriverPlayerProp == null) return null;
                var vehicles = Object.FindObjectsOfType(_cachedLandVehicleType);
                foreach (var v in vehicles)
                {
                    if (v == null) continue;
                    var driver = _cachedDriverPlayerProp.GetValue(v);
                    if (driver != null && driver.Equals(player))
                        return v;
                }
            }
            catch { /* ignore */ }
            return null;
        }

        private static bool TryGetVehiclePosition(object vehicle, out Vector3 position)
        {
            position = Vector3.zero;
            if (vehicle == null) return false;
            var tr = (vehicle as Component)?.transform ?? (vehicle as GameObject)?.transform;
            if (tr != null) { position = tr.position; return true; }
            var posProp = vehicle.GetType().GetProperty("Position", BindingFlags.Public | BindingFlags.Instance);
            if (posProp != null && posProp.PropertyType == typeof(Vector3))
            {
                position = (Vector3)posProp.GetValue(vehicle);
                return true;
            }
            return false;
        }

        private static System.Type _cachedLandVehicleType;
        private static PropertyInfo _cachedDriverPlayerProp;

        private IEnumerator ManorProximityAndGoonsLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.6f);

                if (Stage == 7 && !_manorProximityTriggered)
                {
                    var player = Player.Local;
                    if (player != null && Vector3.Distance(player.Position, ManorWaypoint) <= 80f &&
                        TryGetEffectivePlayerOrVehiclePosition(out var effectivePos))
                    {
                        if (Vector3.Distance(effectivePos, ManorWaypoint) < ManorProximityUnits)
                        {
                            _manorProximityTriggered = true;
                            var persistent = WSPersistent.Instance?.Data;
                            if (persistent != null) persistent.ManorProximityTriggered = true;
                            SpawnManorGoons();
                            if (QuestEntries.Count >= 6) QuestEntries[5].Complete();
                            if (QuestEntries.Count >= 7) QuestEntries[6].Begin();
                            Stage = 8;
                            MelonCoroutines.Start(WarpArchieMannyIgorWhenReady());
                            MelonLogger.Msg("[Act2] Player at manor – defeat the goons.");
                        }
                    }
                }
                else if (Stage == 8)
                {
                    _manorGoons.RemoveAll(g => g == null || !g.IsConscious);
                    if (_manorGoons.Count == 0)
                    {
                        if (QuestEntries.Count >= 7) QuestEntries[6].Complete();
                        if (QuestEntries.Count >= 8) QuestEntries[7].Begin();
                        SetEntry8Objective("Go stash the Veeper in your warehouse");
                        Agent28.Instance?.SendTextMessage("Go stash the Veeper in your warehouse.");
                        Stage = 9;
                        SetManorVeeperPlayerOwned(true);
                        DisableOfficers();
                        MelonCoroutines.Start(SpawnCartelAreasDelayed());
                        MelonLogger.Msg("[Act2] Goons defeated – stash Veeper in warehouse.");
                    }
                }
                else if (Stage == 9)
                {
                    TrySpawnCheckpointGoonsOnEquipmentCarProximity();
                }
                else if (Stage == 10)
                {
                    var equipmentCar = GameObject.Find("equipmentvan");
                    var vanPos = equipmentCar != null
                        ? (equipmentCar.transform.root != null ? equipmentCar.transform.root.position : equipmentCar.transform.position)
                        : (Vector3?)null;
                    if (vanPos.HasValue && Vector3.Distance(vanPos.Value, WarehouseDeliveryPOI) <= 3f)
                    {
                        if (QuestEntries.Count >= 9) QuestEntries[8].Complete();
                        Stage = 11;
                        Complete();
                        SetWeaponManufacturingUnlocked();
                        SetManorVeeperPlayerOwned(false);
                        DeleteCartelVehicles();
                        WarpNpcGameObjectByName("Agent 28", Agent28WarehousePos, Agent28WarehouseRot, "Agent28 warehouse");
                        Agent28.SetDefaultDialogueActive();
                        Agent28.Instance?.SendTextMessage(BlackMarketDeliveryAgent28Message);
                        MelonLogger.Msg("[Act2] Truck delivered to black market – quest complete.");
                    }
                }
            }
        }

        private const float CheckpointScanRadius = 60f;

        private static bool IsPlayerNearAnyCheckpoint(Vector3 pos)
        {
            for (int i = 0; i < 6; i++)
            {
                var checkPos = i < 4 ? CheckpointVehiclePositions[i] : i == 4 ? Checkpoint6TriggerPos : Checkpoint7TriggerPos;
                if (Vector3.Distance(pos, checkPos) <= CheckpointScanRadius) return true;
            }
            return false;
        }

        private static void TrySpawnCheckpointGoonsOnEquipmentCarProximity()
        {
            Vector3 equipPos;
            var equipmentCar = GameObject.Find("equipmentvan");
            if (equipmentCar != null)
                equipPos = equipmentCar.transform.position;
            else
            {
                var player = Player.Local;
                if (player == null) return;
                if (!IsPlayerNearAnyCheckpoint(player.Position)) return;
                if (!TryGetEffectivePlayerOrVehiclePosition(out var fallbackPos)) return;
                equipPos = fallbackPos;
            }

            for (int i = 0; i < 6; i++)
            {
                Vector3 checkPos = i < 4 ? CheckpointVehiclePositions[i] : i == 4 ? Checkpoint6TriggerPos : Checkpoint7TriggerPos;
                float radius = (i >= 4) ? Checkpoint5To7TriggerUnits : CheckpointTriggerUnits;
                if (Vector3.Distance(equipPos, checkPos) > radius)
                    continue;

                bool alreadyTriggered = i switch { 0 => _area1Triggered, 1 => _area2Triggered, 2 => _area3Triggered, 3 => _area4Triggered, 4 => _area6Triggered, 5 => _area7Triggered, _ => true };
                if (alreadyTriggered) continue;

                if (i == 5)
                {
                    _area7Triggered = true;
                    SetPersistentAreaTriggered(7);
                    ClearPlayerWanted();
                    Agent28.Instance?.SendTextMessage(Checkpoint6Agent28Message);
                    var quest = QuestManager.GetStartingSmallQuest();
                    if (quest != null)
                    {
                        if (quest.QuestEntries.Count >= 8) quest.QuestEntries[7].Complete();
                        if (quest.QuestEntries.Count >= 9) quest.QuestEntries[8].Begin();
                        quest.Stage = 10;
                    }
                    MelonLogger.Msg("[Act2] Checkpoint 6 triggered – lost the tail, head to black market.");
                    continue;
                }

                if (i == 0)
                {
                    _area1Triggered = true;
                    SetPersistentAreaTriggered(1);
                    SetEntry8Objective("Lose the cartel");
                    SetLoseTheCartelMarker();
                    SetPlayerWantedDeadOrAlive();
                    var q = QuestManager.GetStartingSmallQuest();
                    if (q != null && q.QuestEntries.Count >= 5)
                        q.QuestEntries[4].Complete();
                }
                else if (i == 1)
                {
                    _area2Triggered = true;
                    SetPersistentAreaTriggered(2);
                    SetPlayerWantedDeadOrAlive();
                }
                else if (i == 2)
                {
                    _area3Triggered = true;
                    SetPersistentAreaTriggered(3);
                    SetPlayerWantedDeadOrAlive();
                }
                else if (i == 3)
                {
                    _area4Triggered = true;
                    SetPersistentAreaTriggered(4);
                    SetPlayerWantedDeadOrAlive();
                }
                else if (i == 4)
                {
                    _area6Triggered = true;
                    SetPersistentAreaTriggered(6);
                    SetPlayerWantedDeadOrAlive();
                }

                WarpAndActivateCheckpointGoons(i);
                MelonLogger.Msg($"[Act2] Checkpoint {i + 1} goons activated: {_checkpointGoons.Count} goons.");
            }
        }

        private static void SetPlayerWantedDeadOrAlive()
        {
            var player = Player.Local;
            if (player == null) return;
            LawManager.SetWantedLevel(player, PursuitLevel.Lethal);
            player.CrimeData.RecordLastKnownPosition(resetTimeSinceSighted: true);
            LawManager.CallPolice(player);
        }

        private static void ClearPlayerWanted()
        {
            var player = Player.Local;
            if (player == null) return;
            LawManager.SetWantedLevel(player, PursuitLevel.None);
        }

        private static void SetPersistentAreaTriggered(int area)
        {
            var d = WSPersistent.Instance?.Data;
            if (d == null) return;
            switch (area)
            {
                case 1: d.Area1Triggered = true; break;
                case 2: d.Area2Triggered = true; break;
                case 3: d.Area3Triggered = true; break;
                case 4: d.Area4Triggered = true; break;
                case 5: d.Area5Triggered = true; break;
                case 6: d.Area6Triggered = true; break;
                case 7: d.Area7Triggered = true; break;
            }
        }

        private static void SetEntry8Objective(string text)
        {
            var quest = QuestManager.GetStartingSmallQuest();
            if (quest == null || quest.QuestEntries.Count <= 7) return;
            var entry = quest.QuestEntries[7];
            if (entry == null) return;
            try
            {
                var t = entry.GetType();
                foreach (var name in new[] { "Text", "Objective", "Title", "Description" })
                {
                    var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(entry, text);
                        break;
                    }
                }
                var posProp = t.GetProperty("Position", BindingFlags.Public | BindingFlags.Instance);
                if (posProp != null && posProp.CanWrite)
                    posProp.SetValue(entry, text.Contains("stash") ? WarehouseStashPOI : LoseTheCartelMarkerPos);
            }
            catch { /* ignore */ }
        }

        private static void SetLoseTheCartelMarker()
        {
            var quest = QuestManager.GetStartingSmallQuest();
            if (quest == null || quest.QuestEntries.Count <= 7) return;
            var entry = quest.QuestEntries[7];
            if (entry == null) return;
            try
            {
                var t = entry.GetType();
                var posProp = t.GetProperty("Position", BindingFlags.Public | BindingFlags.Instance);
                if (posProp != null && posProp.CanWrite)
                    posProp.SetValue(entry, LoseTheCartelMarkerPos);
            }
            catch { /* ignore */ }
        }

        /// <summary>Restore trigger flags from Persistent save after load.</summary>
        public static void RestorePersistentTriggers(bool manorProximity, bool a1, bool a2, bool a3, bool a4, bool a5, bool a6, bool a7)
        {
            _manorProximityTriggered = manorProximity;
            _area1Triggered = a1;
            _area2Triggered = a2;
            _area3Triggered = a3;
            _area4Triggered = a4;
            _area5Triggered = a5;
            _area6Triggered = a6;
            _area7Triggered = a7;
        }

        private void RestoreStageStateForNewQuestOnly()
        {
            if (Stage == 0) return;

            if (_waitForEmployeeEntry == null || _mannyMeetupEntry == null || _hireArchieEntry == null)
            {
                MelonLogger.Warning("[Act2] RestoreStageState: one or more entries null.");
                return;
            }

            switch (Stage)
            {
                case 1:
                    Begin();
                    _waitForEmployeeEntry.Begin();
                    break;
                case 2:
                    Begin();
                    _waitForEmployeeEntry.Complete();
                    if (QuestEntries.Count >= 2) QuestEntries[1].Begin();
                    break;
                case 3:
                    Begin();
                    CompleteUpToEntry2();
                    if (QuestEntries.Count >= 3) QuestEntries[2].Begin();
                    break;
                case 4:
                    Begin();
                    CompleteUpToEntry3();
                    _hireArchieEntry.Begin();
                    break;
                case 5:
                    Begin();
                    CompleteUpToEntry3();
                    _hireArchieEntry.Complete();
                    if (QuestEntries.Count >= 5) QuestEntries[4].Begin();
                    break;
                case 6:
                    Begin();
                    CompleteUpToEquipment();
                    if (QuestEntries.Count >= 5) QuestEntries[4].Begin();
                    break;
                case 7:
                    Begin();
                    CompleteUpToEquipment();
                    if (QuestEntries.Count >= 5) QuestEntries[4].Complete();
                    if (QuestEntries.Count >= 6) QuestEntries[5].Begin();
                    SpawnManorVeeper();
                    MelonCoroutines.Start(ApplyManorVeeperAndWaypointNextFrame());
                    break;
                case 8:
                    Begin();
                    CompleteUpToEquipment();
                    CompleteUpToEntry7();
                    if (QuestEntries.Count >= 8) QuestEntries[7].Begin();
                    MelonCoroutines.Start(ApplyManorVeeperAndGoonsNextFrame());
                    break;
                case 9:
                    Begin();
                    CompleteUpToEquipment();
                    CompleteUpToEntry8();
                    if (QuestEntries.Count >= 8) QuestEntries[7].Begin();
                    SetEntry8Objective(_area1Triggered ? "Lose the cartel" : "Go stash the Veeper in your warehouse");
                    if (_area1Triggered) SetLoseTheCartelMarker();
                    MelonCoroutines.Start(ApplyManorVeeperOwnedAndCartelAreasNextFrame());
                    break;
                case 10:
                    Begin();
                    CompleteUpToEquipment();
                    CompleteUpToEntry9();
                    if (QuestEntries.Count >= 9) QuestEntries[8].Begin();
                    break;
                case 11:
                    Begin();
                    CompleteAllEntries();
                    Complete();
                    SetWeaponManufacturingUnlocked();
                    DeleteCartelVehicles();
                    Agent28.SetDefaultDialogueActive();
                    break;
            }
        }

        private void CompleteUpToEntry2()
        {
            _waitForEmployeeEntry?.Complete();
            if (QuestEntries.Count >= 2) QuestEntries[1].Complete();
        }

        private void CompleteUpToEntry3()
        {
            CompleteUpToEntry2();
            _mannyMeetupEntry?.Complete();
        }

        private void CompleteUpToEntry7()
        {
            CompleteUpToEquipment();
            if (QuestEntries.Count >= 6) QuestEntries[5].Complete();
            if (QuestEntries.Count >= 7) QuestEntries[6].Complete();
        }

        private void CompleteUpToEntry8()
        {
            CompleteUpToEntry7();
            if (QuestEntries.Count >= 8) QuestEntries[7].Complete();
        }

        private void CompleteUpToEntry9()
        {
            CompleteUpToEntry8();
            if (QuestEntries.Count >= 9) QuestEntries[8].Complete();
        }

        private void CompleteUpToEquipment()
        {
            _waitForEmployeeEntry?.Complete();
            if (QuestEntries.Count >= 2) QuestEntries[1].Complete();
            _mannyMeetupEntry?.Complete();
            _hireArchieEntry?.Complete();
        }

        private void CompleteAllEntries()
        {
            for (int i = 0; i < QuestEntries.Count; i++)
                QuestEntries[i].Complete();
        }

        private IEnumerator ApplyManorVeeperAndWaypointNextFrame()
        {
            yield return null;
            SpawnManorVeeper();
            SetManorWaypoint();
        }

        private IEnumerator ApplyManorVeeperAndGoonsNextFrame()
        {
            yield return null;
            if (_manorVeeper == null) SpawnManorVeeper();
            SetManorWaypoint();
            SpawnManorGoons();
        }

        private IEnumerator ApplyManorVeeperOwnedNextFrame()
        {
            yield return null;
            SetManorVeeperPlayerOwned(true);
        }

        private IEnumerator ApplyManorVeeperOwnedAndCartelAreasNextFrame()
        {
            yield return null;
            SetManorVeeperPlayerOwned(true);
            DisableOfficers();
            SpawnAllCartelAreas();
        }

        private IEnumerator EnsureAgent28DialogueWhenReady()
        {
            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForSeconds(0.5f);
                if (NPCs.Agent28.Instance != null)
                {
                    NPCs.Agent28.SetDialogueFromWarehouseState();
                    yield break;
                }
            }
        }

        private IEnumerator ApplyStageSideEffectsNextFrame()
        {
            yield return null;

            yield return new WaitForSeconds(1f);

            var persistent = WSPersistent.Instance?.Data;
            if (persistent != null)
            {
                RestorePersistentTriggers(persistent.ManorProximityTriggered, persistent.Area1Triggered, persistent.Area2Triggered, persistent.Area3Triggered, persistent.Area4Triggered, persistent.Area5Triggered, persistent.Area6Triggered, persistent.Area7Triggered);
                if (Stage == 7)
                {
                    _manorProximityTriggered = false;
                    persistent.ManorProximityTriggered = false;
                    _manorVeeper = null;
                    _manorGoons.Clear();
                }
            }

            if (_mannyMeetupEntry == null || _hireArchieEntry == null)
                yield break;

            if (Stage == 6 && !Manor2AMTriggered && TimeManager.CurrentTime >= 200 && TimeManager.ElapsedDays > StandByDay)
            {
                Manor2AMTriggered = true;
                if (QuestEntries.Count >= 5) QuestEntries[4].Complete();
                if (QuestEntries.Count >= 6) QuestEntries[5].Begin();
                Stage = 7;
                SpawnManorVeeper();
                SetManorWaypoint();
                MelonLogger.Msg("[Act2] 2:00 AM – Go to the Cartel Manor (restored after load).");
            }

            switch (Stage)
            {
                case 1:
                case 2:
                    MelonCoroutines.Start(EnsureAgent28DialogueWhenReady());
                    break;
                case 3:
                    MelonCoroutines.Start(TeleportMeetupNpcsToDocksWhenReady(forHireArchie: false));
                    break;
                case 4:
                    MelonCoroutines.Start(TeleportMeetupNpcsToDocksWhenReady(forHireArchie: true));
                    break;
                case 6:
                    SpawnManorVeeper();
                    break;
                case 7:
                    SpawnManorVeeper();
                    SetManorWaypoint();
                    break;
                case 8:
                    if (_manorVeeper == null) SpawnManorVeeper();
                    SetManorWaypoint();
                    SpawnManorGoons();
                    MelonCoroutines.Start(WarpArchieMannyIgorWhenReady());
                    break;
                case 9:
                    SetManorVeeperPlayerOwned(true);
                    DisableOfficers();
                    SpawnAllCartelAreas();
                    SetEntry8Objective(_area1Triggered ? "Lose the cartel" : "Go stash the Veeper in your warehouse");
                    if (_area1Triggered) SetLoseTheCartelMarker();
                    MelonCoroutines.Start(WarpArchieToWarehouseWhenReady());
                    break;
                case 10:
                    MelonCoroutines.Start(WarpArchieToWarehouseWhenReady());
                    break;
                case 11:
                    Agent28.SetDefaultDialogueActive();
                    WarpNpcGameObjectByName("Agent 28", Agent28WarehousePos, Agent28WarehouseRot, "Agent28 warehouse");
                    MelonCoroutines.Start(WarpArchieToWarehouseWhenReady());
                    break;
            }
        }

        private static void SpawnManorVeeper()
        {
            if (_manorVeeper != null) return;
            var v = VehicleRegistry.CreateVehicle("Veeper");
            if (v == null) { MelonLogger.Warning("[Act2] Failed to create manor Veeper."); return; }
            v.Color = VehicleColor.DarkGreen;
            v.IsPlayerOwned = false;
            var rot = Quaternion.Euler(ManorVeeperRotationEuler.x, ManorVeeperRotationEuler.y, ManorVeeperRotationEuler.z);
            v.Spawn(ManorVeeperPosition, rot);
            _manorVeeper = v;
            MelonLogger.Msg("[Act2] Spawned dark green Veeper at Cartel Manor.");
        }

        private static void SetManorVeeperPlayerOwned(bool owned)
        {
            if (_manorVeeper == null) return;
            var t = _manorVeeper.GetType();
            var prop = t.GetProperty("IsPlayerOwned", BindingFlags.Public | BindingFlags.Instance);
            prop?.SetValue(_manorVeeper, owned);
            if (owned)
                RenameManorVeeperToEquipmentCar();
        }

        private static void PaintEquipmentVanWhite()
        {
            var van = GameObject.Find("equipmentvan");
            if (van == null) return;
            var v = S1API.Vehicles.VehicleRegistry.GetByName("equipmentvan");
            if (v != null)
            {
                v.Color = S1API.Vehicles.VehicleColor.White;
                MelonLogger.Msg("[Act2] Agent 28 painted equipmentvan white.");
            }
        }

        private static void RenameManorVeeperToEquipmentCar()
        {
            if (_manorVeeper == null) return;
            GameObject go = null;
            if (_manorVeeper is Component c)
                go = c.gameObject;
            else
            {
                var prop = _manorVeeper.GetType().GetProperty("GameObject", BindingFlags.Public | BindingFlags.Instance);
                go = prop?.GetValue(_manorVeeper) as GameObject;
            }
            if (go != null)
            {
                var root = go.transform.root != null ? go.transform.root.gameObject : go;
                root.name = "equipmentvan";
                MelonLogger.Msg("[Act2] Renamed delivery van to equipmentvan.");
                return;
            }
            // Fallback: find Veeper (or Van) closest to manor spawn and rename it (like ShipmentSpawner)
            const string prefabName = "Veeper";
            GameObject rootToRename = null;
            float bestDistSq = float.MaxValue;
            foreach (var obj in Object.FindObjectsOfType<GameObject>())
            {
                if (!obj || !obj.name.Contains(prefabName) && !obj.name.Contains("Van")) continue;
                var root = obj.transform.root != null ? obj.transform.root.gameObject : obj;
                float distSq = (root.transform.position - ManorVeeperPosition).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    rootToRename = root;
                }
            }
            if (rootToRename != null)
            {
                rootToRename.name = "equipmentvan";
                MelonLogger.Msg("[Act2] Renamed delivery vehicle (found by proximity) to equipmentvan.");
            }
            else
            {
                MelonLogger.Warning("[Act2] Could not find Veeper/Van to rename as equipmentvan – checkpoint goons will not spawn.");
            }
        }

        private static void DisableOfficers()
        {
            var all = UnityEngine.Object.FindObjectsOfType<GameObject>();
            int count = 0;
            foreach (var go in all)
            {
                if (!go || !go.activeInHierarchy) continue;
                if (string.IsNullOrEmpty(go.name)) continue;
                if (go.name.IndexOf("Officer", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    go.SetActive(false);
                    count++;
                }
            }
            if (count > 0)
                MelonLogger.Msg("[Act2] Disabled {0} Officer GameObjects.", count);
        }

        private static void SetManorWaypoint() { }

        private static readonly string[] ManorGoonWeapons = { null, "Avatar/Equippables/Knife", null, null, null };

        private static void SpawnManorGoons()
        {
            if (_manorGoons.Count > 0) return;
            var cartel = Cartel.Instance;
            if (cartel?.GoonPool == null) { MelonLogger.Warning("[Act2] Cartel/GoonPool not available."); return; }
            var goons = cartel.GoonPool.SpawnGoonsAtPositions(ManorGoonPositions);
            if (goons != null)
            {
                for (int i = 0; i < goons.Count; i++)
                {
                    var goon = goons[i];
                    _manorGoons.Add(goon);
                    if (i < ManorGoonWeapons.Length && !string.IsNullOrEmpty(ManorGoonWeapons[i])) goon.SetDefaultWeapon(ManorGoonWeapons[i]);
                    goon.AttackPlayer();
                }
                MelonLogger.Msg($"[Act2] Spawned {_manorGoons.Count} cartel goons at manor (aggressive).");
            }
        }

        /// <summary>Spawn 2 checkpoint goons at this checkpoint (first trigger) or warp existing ones, then attack.</summary>
        private static void WarpAndActivateCheckpointGoons(int checkpointIndex)
        {
            if (checkpointIndex < 0 || checkpointIndex >= CheckpointPositionsAndWeapons.Length) return;
            var slots = CheckpointPositionsAndWeapons[checkpointIndex];
            if (slots.Length < 2) return;

            if (_checkpointGoons.Count == 0)
            {
                var cartel = Cartel.Instance;
                if (cartel?.GoonPool == null || cartel.GoonPool.AvailableGoonCount < 2) return;
                var positions = new[] { slots[0].pos, slots[1].pos };
                var goons = cartel.GoonPool.SpawnGoonsAtPositions(positions);
                if (goons == null || goons.Count < 2) return;
                _checkpointGoons.Add(goons[0]); _checkpointGoons.Add(goons[1]);
            }

            for (int i = 0; i < _checkpointGoons.Count && i < slots.Length; i++)
            {
                var goon = _checkpointGoons[i];
                if (goon == null || !goon.IsConscious) continue;
                var (pos, weapon) = slots[i];
                goon.WarpTo(pos);
                if (!string.IsNullOrEmpty(weapon)) goon.SetDefaultWeapon(weapon);
                goon.AttackPlayer();
            }
        }

        private static void SpawnCartelVehicle(string vehicleCode, bool black, Vector3 pos, Vector3 rotEuler)
        {
            var v = VehicleRegistry.CreateVehicle(vehicleCode);
            if (v == null) return;
            v.Color = black ? VehicleColor.Black : VehicleColor.DarkGreen;
            v.IsPlayerOwned = false;
            var rot = Quaternion.Euler(rotEuler.x, rotEuler.y, rotEuler.z);
            v.Spawn(pos, rot);
            GameObject go = v.GetType().GetProperty("GameObject", BindingFlags.Public | BindingFlags.Instance)?.GetValue(v) as GameObject;
            if (go == null)
                go = FindVehicleRootNearPosition(pos, vehicleCode);
            if (go != null)
            {
                var root = go.transform.root != null ? go.transform.root.gameObject : go;
                _cartelVehicleCounter++;
                root.name = $"cartelvehicle_{_cartelVehicleCounter}";
                _cartelVehicles.Add(root);
            }
        }

        private static GameObject FindVehicleRootNearPosition(Vector3 pos, string vehicleCode)
        {
            const float radius = 3f;
            GameObject best = null;
            float bestDist = float.MaxValue;
            foreach (var obj in Object.FindObjectsOfType<GameObject>())
            {
                if (obj == null || !obj.activeInHierarchy) continue;
                var root = obj.transform.root != null ? obj.transform.root.gameObject : obj;
                if (root.name.IndexOf(vehicleCode, System.StringComparison.OrdinalIgnoreCase) < 0
                    && !root.name.Contains("Clone"))
                    continue;
                float d = Vector3.Distance(root.transform.position, pos);
                if (d <= radius && d < bestDist)
                {
                    bestDist = d;
                    best = root;
                }
            }
            return best;
        }

        private static void DeleteCartelVehicles()
        {
            foreach (var go in _cartelVehicles)
            {
                if (go != null) Object.Destroy(go);
            }
            _cartelVehicles.Clear();
            MelonLogger.Msg("[Act2] Cartel vehicles removed.");
        }

        private static IEnumerator SpawnCartelAreasDelayed()
        {
            yield return null;
            SpawnAllCartelAreas();
        }

        private static void SpawnAllCartelAreas()
        {
            _cartelVehicleCounter = 0;
            SpawnCartelVehicle("Hotbox", true, Area1HotboxPos, Area1HotboxRot);
            foreach (var v in Area2Vehicles)
                SpawnCartelVehicle(v.code, v.black, v.pos, v.rot);
            foreach (var v in Area3Vehicles)
                SpawnCartelVehicle(v.code, v.black, v.pos, v.rot);
            foreach (var v in Area4Vehicles)
                SpawnCartelVehicle(v.code, v.black, v.pos, v.rot);
            foreach (var v in Area5Vehicles)
                SpawnCartelVehicle(v.code, v.black, v.pos, v.rot);
            foreach (var v in ExtraQuestVehicles)
                SpawnCartelVehicle(v.code, v.black, v.pos, v.rot);
            MelonLogger.Msg("[Act2] Checkpoint vehicles spawned. Goons spawn when equipmentvan gets within trigger radius.");
        }

        private static int TryGetPlayerLevel()
        {
            try
            {
                var player = Player.Local;
                if (player == null) return -1;
                var leveling = Type.GetType("S1API.Leveling.LevelManager, S1API.Forked");
                if (leveling == null) return -1;
                var method = leveling.GetMethod("GetLevel", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Player) }, null);
                if (method == null) return -1;
                var result = method.Invoke(null, new object[] { player });
                return result is int l ? l : -1;
            }
            catch { return -1; }
        }

        /// <summary>Called when player wakes after buying warehouse (from Quest 1). Starts Quest 2; first step = wait for employee. Text was already sent in New Number after Agent 28 dialogue.</summary>
        public void PurchaseWarehouse()
        {
            if (Stage != 0) return;

            var rank = TryGetPlayerLevel();
            MelonLogger.Msg($"[Act2] Starting Small quest started. Player rank level: {rank}");

            Begin();
            Stage = 1;
            _waitForEmployeeEntry?.Begin();
            LeadDay = TimeManager.ElapsedDays;
            AwaitingWakeup = false;
            Sent1900 = false;
            Revealed2200 = false;
        }

        /// <summary>Player chose to sleep first; we wait for wake then 19:00 (Manny message) / 22:00 (drop-off).</summary>
        public void WaitForEmployee()
        {
            if (Stage != 0) return;

            Begin();
            Stage = 1;
            _waitForEmployeeEntry?.Begin();
            AwaitingWakeup = true;
            LeadDay = -1;
            Sent1900 = false;
            Revealed2200 = false;
        }

        /// <summary>Debug: force 19:00 text without waiting for time.</summary>
        public void DebugForce19_00Text()
        {
            if (Stage != 1) return;
            Sent1900 = true;
            _waitForEmployeeEntry?.Complete();
            if (QuestEntries.Count >= 2) QuestEntries[1].Begin();
            Stage = 2;
            Agent28.Instance?.SendTextMessage("Manny knows someone looking for work. I'll text you the location at 21:00. Be ready.");
            MelonLogger.Msg("[Act2] Debug: forced 19:00 text.");
        }

        public void MannyMeetup()
        {
            if (Stage != 2) return;
            if (Revealed2200) return;

            Revealed2200 = true;
            DoMannyMeetupReveal();
            Manny.SetMeetupDialogueActive();
        }

        private void DoMannyMeetupReveal()
        {
            if (Stage != 2) return;

            if (QuestEntries.Count >= 2) QuestEntries[1].Complete();
            Stage = 3;
            _mannyMeetupEntry?.Begin();
            Agent28.Instance?.SendTextMessage("Meet him behind Randy's shop.");
            Manny.SetMeetupDialogueActive();
            TeleportMeetupNpcsToDocks();
            WarpNpcGameObjectByName("Agent 28", Agent28WarehousePos, Agent28WarehouseRot, "Agent28 warehouse");
        }

        public void HireArchie()
        {
            if (Stage != 3) return;

            _mannyMeetupEntry?.Complete();
            Stage = 4;
            _hireArchieEntry?.Begin();
            Archie.SetMeetupDialogueActive();
        }

        /// <summary>Player paid Archie; advance to stand by for the window.</summary>
        public void CompleteHireArchie()
        {
            if (Stage != 4) return;

            _hireArchieEntry?.Complete();
            Stage = 6;
            StandByDay = TimeManager.ElapsedDays;
            if (QuestEntries.Count >= 5) QuestEntries[4].Begin();
            SpawnManorVeeper();
        }

        /// <summary>Player delivered truck (called from dialogue or other trigger). Completion normally happens via proximity check when equipmentvan is within 3 units of black market.</summary>
        public void DeliverTruckToWarehouse()
        {
            if (Stage != 10) return;

            if (QuestEntries.Count >= 10) QuestEntries[9].Complete();
            Stage = 11;
            Complete();
            SetWeaponManufacturingUnlocked();
            DeleteCartelVehicles();
            Agent28.SetDefaultDialogueActive();
            Agent28.Instance?.SendTextMessage(BlackMarketDeliveryAgent28Message);
        }

        private static void SetWeaponManufacturingUnlocked()
        {
            var data = WSSaveData.Instance?.Data;
            if (data != null)
                data.Properties.Warehouse.SetupComplete = true;
            WarehouseEquipmentSetup.RefreshEquipmentVisibility();
            var p = WSPersistent.Instance?.Data;
            if (p != null) p.AwaitingVeeperTeleport = true;
            MelonLogger.Msg("[Act2] Weapon manufacturing unlocked; Veeper will teleport after sleep.");
        }

        private void OnSleepEnd(int minutesSkipped)
        {
            var pData = WSPersistent.Instance?.Data;
            if (pData != null && pData.AwaitingVeeperTeleport)
            {
                pData.AwaitingVeeperTeleport = false;
                Services.WarehouseVeeperManager.EnsureWarehouseVeeperExists();
                PaintEquipmentVanWhite();
                MelonLogger.Msg("[Act2] Veeper teleported to warehouse after sleep (painted white).");
            }

            if (Stage == 1 && Saved != null)
            {
                Saved.MissedMeetupWindowToday = false;
                Saved.SentUrgency10 = false;
                Saved.SentUrgency1130 = false;
                Saved.SentUrgency1230 = false;
                Agent28.SetMeetupDialogueActive();
            }

            if (Stage != 1) return;
            if (!AwaitingWakeup) return;

            AwaitingWakeup = false;
            LeadDay = TimeManager.ElapsedDays;
            MelonLogger.Msg($"[Act2] Wakeup detected; scheduling Manny texts for day {LeadDay}.");
        }

        private void OnTick()
        {
            if (LeadDay < 0 && Stage != 6 && Stage != 7) return;
            if (LeadDay >= 0 && Stage <= 2 && TimeManager.ElapsedDays != LeadDay) return;

            int t = TimeManager.CurrentTime;

            if (Stage == 1)
            {
                if (Saved != null && Saved.MissedMeetupWindowToday)
                {
                    if (t >= 1300)
                    {
                        Agent28.SetDefaultDialogueActive();
                    }
                    return;
                }

                if (t >= 1300)
                {
                    if (Saved != null) Saved.MissedMeetupWindowToday = true;
                    Agent28.Instance?.SendTextMessage("I don't have time for this.");
                    Agent28.SetDefaultDialogueActive();
                    MelonLogger.Msg("[Act2] Meetup window missed at 13:00 – sleep to retry.");
                    return;
                }

                if (Saved != null && !Saved.SentUrgency1230 && t >= 1230)
                {
                    Saved.SentUrgency1230 = true;
                    Agent28.Instance?.SendTextMessage("I'm leaving in 30 minutes. You coming or not?");
                }
                else if (Saved != null && !Saved.SentUrgency1130 && t >= 1130)
                {
                    Saved.SentUrgency1130 = true;
                    Agent28.Instance?.SendTextMessage("I'm not waiting around all day. Get a move on.");
                }
                else if (Saved != null && !Saved.SentUrgency10 && t >= 1000)
                {
                    Saved.SentUrgency10 = true;
                    Agent28.Instance?.SendTextMessage("I don't got all day.");
                }

                if (!Sent1900 && t >= 1900)
                {
                    Sent1900 = true;
                    _waitForEmployeeEntry?.Complete();
                    if (QuestEntries.Count >= 2) QuestEntries[1].Begin();
                    Stage = 2;
                    Agent28.Instance?.SendTextMessage(
                        "Manny knows someone looking for work. I'll text you the location at 21:00. Be ready."
                    );
                }
                return;
            }

            if (Stage == 2)
            {
                if (Sent1900 && !Revealed2200 && t >= 2100)
                {
                    Revealed2200 = true;
                    DoMannyMeetupReveal();
                }
                return;
            }

            if (Stage == 6 && !Manor2AMTriggered && t >= 200 && TimeManager.ElapsedDays > StandByDay)
            {
                Manor2AMTriggered = true;
                if (QuestEntries.Count >= 5) QuestEntries[4].Complete();
                if (QuestEntries.Count >= 6) QuestEntries[5].Begin();
                Stage = 7;
                SpawnManorVeeper();
                SetManorWaypoint();
                MelonLogger.Msg("[Act2] 2:00 AM – Go to the Cartel Manor.");
            }
        }

        private static readonly Vector3 ArchieWarehousePos = new Vector3(-30.9878f, -3.8f, 171.478f);
        private static readonly Quaternion ArchieWarehouseRot = Quaternion.Euler(0f, 90f, 0f);
        private static readonly Vector3 Agent28WarehousePos = new Vector3(-23.0225f, -5f, 170.31f);
        private static readonly Quaternion Agent28WarehouseRot = Quaternion.Euler(0f, 310f, 0f);
        private static readonly Vector3 NpcHiddenPos = new Vector3(0f, 500f, 0f);

        private void TeleportMeetupNpcsToDocks()
        {
            Vector3 archiePos = new Vector3(-97.7585f, -2.5f, -37.1382f);
            Vector3 mannyPos = new Vector3(-98.5142f, -2.5f, -36.5701f);
            Vector3 igorPos = new Vector3(-98.8593f, -2.5f, -36f);
            Quaternion faceRot = Quaternion.Euler(0f, 200f, 0f);
            Quaternion archieRot = Quaternion.Euler(0f, 240f, 0f);

            WarpNpcGameObjectByName("ArchieWS", archiePos, archieRot, "Archie warped");
            WarpNpcGameObjectByName("MannyWS", mannyPos, faceRot, "Manny warped");
            WarpNpcGameObjectByName("IgorWS", igorPos, faceRot, "Igor warped");
        }

        private IEnumerator TeleportMeetupNpcsToDocksWhenReady(bool forHireArchie = false)
        {
            Vector3 archiePos = new Vector3(-97.7585f, -2.5f, -37.1382f);
            Vector3 mannyPos = new Vector3(-98.5142f, -2.5f, -36.5701f);
            Vector3 igorPos = new Vector3(-98.8593f, -2.5f, -36f);
            Quaternion faceRot = Quaternion.Euler(0f, 200f, 0f);
            Quaternion archieRot = Quaternion.Euler(0f, 240f, 0f);

            bool archieDone = false, mannyDone = false, igorDone = false;
            for (int i = 0; i < 30; i++)
            {
                if (!archieDone && WarpNpcGameObjectByNameOnce("ArchieWS", archiePos, archieRot, "Archie docks"))
                    archieDone = true;
                if (!mannyDone && WarpNpcGameObjectByNameOnce("MannyWS", mannyPos, faceRot, "Manny docks"))
                    mannyDone = true;
                if (!igorDone && WarpNpcGameObjectByNameOnce("IgorWS", igorPos, faceRot, "Igor docks"))
                    igorDone = true;
                if (archieDone && mannyDone && igorDone)
                {
                    if (forHireArchie)
                        Archie.SetMeetupDialogueActive();
                    else
                        Manny.SetMeetupDialogueActive();
                    yield break;
                }
                yield return new WaitForSeconds(1f);
            }
            MelonLogger.Warning("[Act2] TeleportMeetupNpcsToDocksWhenReady timed out – some NPCs may not have spawned.");
        }

        private static IEnumerator WarpArchieMannyIgorWhenReady()
        {
            bool archieDone = false, mannyDone = false, igorDone = false;
            while (!archieDone || !mannyDone || !igorDone)
            {
                if (!archieDone && WarpNpcGameObjectByNameOnce("ArchieWS", ArchieWarehousePos, ArchieWarehouseRot, "Archie warehouse"))
                    archieDone = true;
                if (!mannyDone && WarpNpcGameObjectByNameOnce("MannyWS", NpcHiddenPos, Quaternion.identity, "Manny hidden"))
                    mannyDone = true;
                if (!igorDone && WarpNpcGameObjectByNameOnce("IgorWS", NpcHiddenPos, Quaternion.identity, "Igor hidden"))
                    igorDone = true;
                if (!archieDone || !mannyDone || !igorDone)
                    yield return new WaitForSeconds(1f);
            }
        }

        private static IEnumerator WarpArchieToWarehouseWhenReady()
        {
            while (!WarpNpcGameObjectByNameOnce("ArchieWS", ArchieWarehousePos, ArchieWarehouseRot, "Archie warehouse"))
                yield return new WaitForSeconds(1f);
        }

        private static bool WarpNpcGameObjectByNameOnce(string exactName, Vector3 pos, Quaternion rot, string logTag)
        {
            var target = GameObject.Find(exactName);
            if (target == null)
                return false;

            var agent = target.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
                agent.enabled = false;

            target.transform.position = pos;
            target.transform.rotation = rot;
            MelonLogger.Msg($"{logTag}: Warped '{target.name}' to {pos}.");
            return true;
        }

        private static void WarpNpcGameObjectByName(string exactName, Vector3 pos, Quaternion rot, string logTag)
        {
            if (!WarpNpcGameObjectByNameOnce(exactName, pos, rot, logTag))
                MelonLogger.Warning($"[Act2] {logTag}: '{exactName}' not found.");
        }
    }
}
