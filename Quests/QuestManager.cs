using S1API.Quests;
using MelonLoader;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using WeaponShipments.Data;
using CustomNPCTest.NPCs;

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
        private const string STEAL_GUID = "ws_steal_supplies";
        private const string STEAL_NAME = "Steal Supplies";
        private const string SELL_GUID = "ws_sell_stock";
        private const string SELL_NAME = "Sell Stock";
        private const string BUY_GUID = "ws_buy_supplies";
        private const string BUY_NAME = "Buy Supplies";

        private static Act1NewNumberQuest? _cachedQuest1;
        private static Act2StartingSmallQuest? _cachedQuest2;
        private static Act3UnpackingQuest? _cachedQuest3;
        private static Act3MovingUpQuest? _cachedQuest4;
        private static StealSuppliesQuest? _cachedStealQuest;
        private static SellStockQuest? _cachedSellQuest;
        private static BuySuppliesQuest? _cachedBuyQuest;

        public static void Initialize()
        {
            _ = GetNewNumberQuest();
            _ = GetStartingSmallQuest();
            _ = GetUnpackingQuest();
            _ = GetMovingUpQuest();
            _ = GetStealSuppliesQuest();
            _ = GetSellStockQuest();
            _ = GetBuySuppliesQuest();
        }

        public static Act1NewNumberQuest? GetNewNumberQuest() => GetOrCreateQuest1();
        public static Act2StartingSmallQuest? GetStartingSmallQuest() => GetOrCreateQuest2();
        public static Act3UnpackingQuest? GetUnpackingQuest() => GetOrCreateQuest3();
        public static Act3MovingUpQuest? GetMovingUpQuest() => GetOrCreateQuest4();
        public static StealSuppliesQuest? GetStealSuppliesQuest() => GetOrCreateStealQuest();
        public static SellStockQuest? GetSellStockQuest() => GetOrCreateSellQuest();
        public static BuySuppliesQuest? GetBuySuppliesQuest() => GetOrCreateBuyQuest();

        /// <summary>Start the steal quest with waypoint at the pickup location. Call when player starts a steal from the app.</summary>
        public static void StartStealRun(string origin, string destination)
        {
            GetStealSuppliesQuest()?.StartWithPickupAt(origin, destination);
        }

        /// <summary>Activate delivery step when player approaches crate. Call from ShipmentProximityDetector.</summary>
        public static void ActivateStealDeliveryStep()
        {
            GetStealSuppliesQuest()?.ActivateDeliveryStep();
        }

        /// <summary>Complete the current steal run. Call when player delivers stolen supplies.</summary>
        public static void CompleteStealRun()
        {
            GetStealSuppliesQuest()?.CompleteStealRun();
        }

        /// <summary>Start the sell quest. Call when a sell job starts.</summary>
        public static void StartSellRun(Vector3 pickupPos, string pickupLabel, Vector3 deliveryPos, string deliveryLabel)
        {
            GetSellStockQuest()?.StartWithPickupAt(pickupPos, pickupLabel, deliveryPos, deliveryLabel);
        }

        /// <summary>Complete the current sell run. Call when player delivers stock.</summary>
        public static void CompleteSellRun()
        {
            GetSellStockQuest()?.CompleteSellRun();
        }

        /// <summary>Start the buy supplies quest. Call when player buys supplies from the app.</summary>
        public static void StartBuySupplies(float arrivesAtSeconds)
        {
            GetBuySuppliesQuest()?.StartWithArrivalIn(arrivesAtSeconds);
        }

        public static void ClearCache()
        {
            _cachedQuest1 = null;
            _cachedQuest2 = null;
            _cachedQuest3 = null;
            _cachedQuest4 = null;
            _cachedStealQuest = null;
            _cachedSellQuest = null;
            _cachedBuyQuest = null;
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

        private static StealSuppliesQuest? GetOrCreateStealQuest()
        {
            if (_cachedStealQuest != null && QuestManagerQuests.Contains(_cachedStealQuest))
                return _cachedStealQuest;
            _cachedStealQuest = null;

            var byName = S1API.Quests.QuestManager.GetQuestByName(STEAL_NAME);
            if (byName is StealSuppliesQuest sq)
            {
                _cachedStealQuest = sq;
                return sq;
            }

            for (int i = 0; i < QuestManagerQuests.Count; i++)
            {
                if (QuestManagerQuests[i] is StealSuppliesQuest found)
                {
                    _cachedStealQuest = found;
                    return found;
                }
            }

            var created = S1API.Quests.QuestManager.CreateQuest<StealSuppliesQuest>(STEAL_GUID);
            if (created is StealSuppliesQuest steal)
            {
                _cachedStealQuest = steal;
                return steal;
            }

            MelonLogger.Error("[QuestManager] Failed to create StealSuppliesQuest");
            return null;
        }

        private static SellStockQuest? GetOrCreateSellQuest()
        {
            if (_cachedSellQuest != null && QuestManagerQuests.Contains(_cachedSellQuest))
                return _cachedSellQuest;
            _cachedSellQuest = null;

            var byName = S1API.Quests.QuestManager.GetQuestByName(SELL_NAME);
            if (byName is SellStockQuest sq)
            {
                _cachedSellQuest = sq;
                return sq;
            }

            for (int i = 0; i < QuestManagerQuests.Count; i++)
            {
                if (QuestManagerQuests[i] is SellStockQuest found)
                {
                    _cachedSellQuest = found;
                    return found;
                }
            }

            var created = S1API.Quests.QuestManager.CreateQuest<SellStockQuest>(SELL_GUID);
            if (created is SellStockQuest sell)
            {
                _cachedSellQuest = sell;
                return sell;
            }

            MelonLogger.Error("[QuestManager] Failed to create SellStockQuest");
            return null;
        }

        private static BuySuppliesQuest? GetOrCreateBuyQuest()
        {
            if (_cachedBuyQuest != null && QuestManagerQuests.Contains(_cachedBuyQuest))
                return _cachedBuyQuest;
            _cachedBuyQuest = null;

            var byName = S1API.Quests.QuestManager.GetQuestByName(BUY_NAME);
            if (byName is BuySuppliesQuest bq)
            {
                _cachedBuyQuest = bq;
                return bq;
            }

            for (int i = 0; i < QuestManagerQuests.Count; i++)
            {
                if (QuestManagerQuests[i] is BuySuppliesQuest found)
                {
                    _cachedBuyQuest = found;
                    return found;
                }
            }

            var created = S1API.Quests.QuestManager.CreateQuest<BuySuppliesQuest>(BUY_GUID);
            if (created is BuySuppliesQuest buy)
            {
                _cachedBuyQuest = buy;
                return buy;
            }

            MelonLogger.Error("[QuestManager] Failed to create BuySuppliesQuest");
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
            quest.OnQuestStarted();
            quest.EnsureTickHooked();
            Archie.SetDialogueFromUnpackingState();
            MelonLogger.Msg("[Act3] Unpacking quest started.");
        }

        /// <summary>Called when Unpacking completes. Only starts if TotalEarnings >= MovingUpMinEarnings.</summary>
        public static void TryStartMovingUpOnZoneEntry()
        {
            if (BusinessState.TotalEarnings < BusinessConfig.MovingUpMinEarnings)
                return;

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

        private static List<Quest> QuestManagerQuests => (List<Quest>)typeof(S1API.Quests.QuestManager)
            .GetField("Quests", BindingFlags.NonPublic | BindingFlags.Static)
            .GetValue(null);
    }
}
