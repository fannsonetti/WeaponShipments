using S1API.Quests;
using MelonLoader;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using WeaponShipments.Data;

namespace WeaponShipments.Quests
{
    public static class QuestManager
    {
        private const string QUEST1_GUID = "ws_new_number_new_problems";
        private const string QUEST1_NAME = "New Number, New Problems";

        private const string QUEST2_GUID = "ws_starting_small";
        private const string QUEST2_NAME = "Starting Small";
        private const string QUEST3_GUID = "ws_unpacking";
        private const string QUEST3_NAME = "Unpacking";
        private const string QUEST4_GUID = "ws_moving_up";
        private const string QUEST4_NAME = "Moving Up";

        private static Act1NewNumberQuest? _cachedQuest1;
        private static Act2StartingSmallQuest? _cachedQuest2;
        private static Act3UnpackingQuest? _cachedQuest3;
        private static Act3MovingUpQuest? _cachedQuest4;

        public static void Initialize()
        {
            _ = GetNewNumberQuest();
            _ = GetStartingSmallQuest();
            _ = GetUnpackingQuest();
            _ = GetMovingUpQuest();
        }

        public static Act1NewNumberQuest? GetNewNumberQuest() => GetOrCreateQuest1();
        public static Act2StartingSmallQuest? GetStartingSmallQuest() => GetOrCreateQuest2();
        public static Act3UnpackingQuest? GetUnpackingQuest() => GetOrCreateQuest3();
        public static Act3MovingUpQuest? GetMovingUpQuest() => GetOrCreateQuest4();

        public static void ClearCache()
        {
            _cachedQuest1 = null;
            _cachedQuest2 = null;
            _cachedQuest3 = null;
            _cachedQuest4 = null;
        }

        public static void AgentMeetup() => GetNewNumberQuest()?.AgentMeetup();
        public static void CompleteDialogueAndUnlockProperty() => GetNewNumberQuest()?.CompleteDialogueAndUnlockProperty();

        public static void PurchaseWarehouse() => GetStartingSmallQuest()?.PurchaseWarehouse();

        public static void PurchaseGarage() => GetMovingUpQuest()?.PurchaseGarage();
        public static void WaitForEmployee() => GetStartingSmallQuest()?.WaitForEmployee();
        public static void MannyMeetup() => GetStartingSmallQuest()?.MannyMeetup();
        public static void HireArchie() => GetStartingSmallQuest()?.HireArchie();
        public static void CompleteHireArchie() => GetStartingSmallQuest()?.CompleteHireArchie();
        public static void DeliverTruckToWarehouse() => GetStartingSmallQuest()?.DeliverTruckToWarehouse();

        /// <summary>Debug: force 19:00 text without waiting.</summary>
        public static void DebugForce19_00Text() => GetStartingSmallQuest()?.DebugForce19_00Text();
        /// <summary>Debug: force 22:00 Manny reveal without waiting.</summary>
        public static void DebugForce22_00Reveal() => GetStartingSmallQuest()?.MannyMeetup();

        private static Act1NewNumberQuest? GetOrCreateQuest1()
        {
            if (_cachedQuest1 != null && QuestManagerQuests.Contains(_cachedQuest1))
                return _cachedQuest1;
            _cachedQuest1 = null;

            var byName = S1API.Quests.QuestManager.GetQuestByName(QUEST1_NAME);
            if (byName is Act1NewNumberQuest q1)
            {
                _cachedQuest1 = q1;
                return q1;
            }

            for (int i = 0; i < QuestManagerQuests.Count; i++)
            {
                if (QuestManagerQuests[i] is Act1NewNumberQuest found)
                {
                    _cachedQuest1 = found;
                    return found;
                }
            }

            var created = S1API.Quests.QuestManager.CreateQuest<Act1NewNumberQuest>(QUEST1_GUID);
            if (created is Act1NewNumberQuest act1)
            {
                _cachedQuest1 = act1;
                return act1;
            }

            MelonLogger.Error("[QuestManager] Failed to create Act1NewNumberQuest");
            return null;
        }

        private static Act2StartingSmallQuest? GetOrCreateQuest2()
        {
            if (_cachedQuest2 != null && QuestManagerQuests.Contains(_cachedQuest2))
                return _cachedQuest2;
            _cachedQuest2 = null;

            var byName = S1API.Quests.QuestManager.GetQuestByName(QUEST2_NAME);
            if (byName is Act2StartingSmallQuest q2)
            {
                _cachedQuest2 = q2;
                return q2;
            }

            for (int i = 0; i < QuestManagerQuests.Count; i++)
            {
                if (QuestManagerQuests[i] is Act2StartingSmallQuest found)
                {
                    _cachedQuest2 = found;
                    return found;
                }
            }

            var created = S1API.Quests.QuestManager.CreateQuest<Act2StartingSmallQuest>(QUEST2_GUID);
            if (created is Act2StartingSmallQuest act2)
            {
                _cachedQuest2 = act2;
                return act2;
            }

            MelonLogger.Error("[QuestManager] Failed to create Act2StartingSmallQuest");
            return null;
        }

        private static Act3UnpackingQuest? GetOrCreateQuest3()
        {
            if (_cachedQuest3 != null && QuestManagerQuests.Contains(_cachedQuest3))
                return _cachedQuest3;
            _cachedQuest3 = null;

            var byName = S1API.Quests.QuestManager.GetQuestByName(QUEST3_NAME);
            if (byName is Act3UnpackingQuest q3)
            {
                _cachedQuest3 = q3;
                return q3;
            }

            for (int i = 0; i < QuestManagerQuests.Count; i++)
            {
                if (QuestManagerQuests[i] is Act3UnpackingQuest found)
                {
                    _cachedQuest3 = found;
                    return found;
                }
            }

            var created = S1API.Quests.QuestManager.CreateQuest<Act3UnpackingQuest>(QUEST3_GUID);
            if (created is Act3UnpackingQuest act3)
            {
                _cachedQuest3 = act3;
                return act3;
            }

            MelonLogger.Error("[QuestManager] Failed to create Act3UnpackingQuest");
            return null;
        }

        private static Act3MovingUpQuest? GetOrCreateQuest4()
        {
            if (_cachedQuest4 != null && QuestManagerQuests.Contains(_cachedQuest4))
                return _cachedQuest4;
            _cachedQuest4 = null;

            var byName = S1API.Quests.QuestManager.GetQuestByName(QUEST4_NAME);
            if (byName is Act3MovingUpQuest q4)
            {
                _cachedQuest4 = q4;
                return q4;
            }

            for (int i = 0; i < QuestManagerQuests.Count; i++)
            {
                if (QuestManagerQuests[i] is Act3MovingUpQuest found)
                {
                    _cachedQuest4 = found;
                    return found;
                }
            }

            var created = S1API.Quests.QuestManager.CreateQuest<Act3MovingUpQuest>(QUEST4_GUID);
            if (created is Act3MovingUpQuest act4)
            {
                _cachedQuest4 = act4;
                return act4;
            }

            MelonLogger.Error("[QuestManager] Failed to create Act3MovingUpQuest");
            return null;
        }

        public static void TryStartUnpackingIfEligible()
        {
            var data = WSSaveData.Instance?.Data;
            if (data == null || !data.Properties.Warehouse.SetupComplete) return;
            var quest = GetUnpackingQuest();
            if (quest == null || quest.Stage > 0) return;

            quest.Begin();
            if (quest.QuestEntries.Count >= 1) quest.QuestEntries[0].Begin();
            quest.Stage = 1;
            quest.EnsureTickHooked();
            SpawnUnpackingEquipmentBox();
            MelonLogger.Msg("[Act3] Unpacking quest started.");
        }

        /// <summary>Called when Unpacking completes.</summary>
        public static void TryStartMovingUpOnZoneEntry()
        {
            var quest = GetMovingUpQuest();
            if (quest == null || quest.Stage > 0) return;

            quest.Begin();
            if (quest.QuestEntries.Count >= 1) quest.QuestEntries[0].Begin();
            quest.Stage = 1;
            WeaponShipments.NPCs.NorthWarehouseLandlord.SetDialogueFromAct3State();
            SpawnMovingUpEquipmentBox();
            Services.MovingUpEquipmentInteractables.SetupEquipmentInteractables();
            MelonLogger.Msg("[Act3] Moving Up quest started.");
        }

        private static void SpawnUnpackingEquipmentBox()
        {
            var van = Services.WarehouseVeeperManager.GetWarehouseVeeper();
            var pos = van != null && van.transform.root != null
                ? van.transform.root.position
                : new Vector3(-26f, -4.3f, 173.5f);
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "UnpackingEquipmentBox";
            box.transform.position = pos;
            box.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            var col = box.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);

            var renderer = box.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.color = Color.white;
                    renderer.material = mat;
                }
            }

            var asm = System.Reflection.Assembly.Load("Assembly-CSharp");
            var ioType = asm?.GetType("ScheduleOne.Interaction.InteractableObject");
            if (ioType != null)
                box.AddComponent(ioType);
        }

        private static void SpawnMovingUpEquipmentBox()
        {
            var van = Services.WarehouseVeeperManager.GetWarehouseVeeper();
            var pos = van != null && van.transform.root != null
                ? van.transform.root.position
                : new Vector3(-26f, -4.3f, 173.5f);
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "MovingUpEquipmentBox";
            box.transform.position = pos;
            var col = box.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);
        }

        public static void TryStartAct3IfEligible()
        {
        }

        private static List<Quest> QuestManagerQuests => (List<Quest>)typeof(S1API.Quests.QuestManager)
            .GetField("Quests", BindingFlags.NonPublic | BindingFlags.Static)
            .GetValue(null);
    }
}
