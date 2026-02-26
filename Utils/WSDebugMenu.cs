using System;
using MelonLoader;
using S1API.Quests;
using UnityEngine;
using CustomNPCTest.NPCs;
using WeaponShipments.Data;
using WeaponShipments.Quests;

namespace WeaponShipments.Utils
{
    /// <summary>
    /// Debug menu for development. Toggled via "ws menu" console command.
    /// </summary>
    public class WSDebugMenu : MonoBehaviour
    {
        public static bool Visible { get; set; }

        private Vector2 _scroll;
        private const int WIDTH = 420;
        private const int HEIGHT = 520;
        private int _activeTab; // 0=Properties, 1=Quests

        private void OnGUI()
        {
            if (!Visible) return;

            var rect = new Rect(Screen.width / 2f - WIDTH / 2f, 40, WIDTH, HEIGHT);
            GUI.Box(rect, "WS Menu (ws menu to close)");

            var viewRect = new Rect(rect.x + 8, rect.y + 28, rect.width - 16, rect.height - 36);

            GUILayout.BeginArea(new Rect(rect.x + 8, rect.y + 28, rect.width - 16, 24));
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_activeTab == 0, "Properties", GUILayout.Height(22))) _activeTab = 0;
            if (GUILayout.Toggle(_activeTab == 1, "Quests", GUILayout.Height(22))) _activeTab = 1;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            var scrollRect = new Rect(viewRect.x, viewRect.y + 28, viewRect.width, viewRect.height - 28);
            var contentRect = new Rect(0, 0, rect.width - 32, 900);
            _scroll = GUI.BeginScrollView(scrollRect, _scroll, contentRect);
            GUILayout.BeginArea(contentRect);
            GUILayout.BeginVertical();

            if (_activeTab == 0)
                DrawPropertiesTab();
            else
                DrawQuestsTab();

            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.EndScrollView();
        }

        private void DrawPropertiesTab()
        {
            GUILayout.Label("Stock / Supplies");
            foreach (var p in new[] { BusinessState.PropertyType.Warehouse, BusinessState.PropertyType.Garage, BusinessState.PropertyType.Bunker })
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(p + " Stock:", GUILayout.Width(100));
                var stock = BusinessState.GetStock(p);
                if (float.TryParse(GUILayout.TextField(stock.ToString("0"), GUILayout.Width(60)), out var s))
                    BusinessState.SetStockForProperty(p, s);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(p + " Supplies:", GUILayout.Width(100));
                var supplies = BusinessState.GetSupplies(p);
                if (float.TryParse(GUILayout.TextField(supplies.ToString("0"), GUILayout.Width(60)), out var u))
                    BusinessState.SetSuppliesForProperty(p, u);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
            GUILayout.Label("Stats");
            DrawStatField("TotalEarnings", BusinessState.TotalEarnings, v => BusinessState.SetTotalEarnings(v));
            DrawStatField("TotalSalesCount", BusinessState.TotalSalesCount, v => BusinessState.SetTotalSalesCount((int)v));
            DrawStatField("TotalStockProduced", BusinessState.TotalStockProduced, v => BusinessState.SetTotalStockProduced(v));
            DrawStatField("ResupplyStarted", BusinessState.ResupplyJobsStarted, v => BusinessState.SetResupplyJobsStarted((int)v));
            DrawStatField("ResupplyCompleted", BusinessState.ResupplyJobsCompleted, v => BusinessState.SetResupplyJobsCompleted((int)v));
            DrawStatField("HylandAttempts", BusinessState.HylandSellAttempts, v => BusinessState.SetHylandSellAttempts((int)v));
            DrawStatField("HylandSuccesses", BusinessState.HylandSellSuccesses, v => BusinessState.SetHylandSellSuccesses((int)v));

            GUILayout.Space(8);
            GUILayout.Label("Property Owned");
            var data = WSSaveData.Instance?.Data;
            if (data != null)
            {
                foreach (var p in new[] { BusinessState.PropertyType.Warehouse, BusinessState.PropertyType.Garage, BusinessState.PropertyType.Bunker })
                {
                    bool owned = p switch
                    {
                        BusinessState.PropertyType.Warehouse => data.Properties.Warehouse.Owned,
                        BusinessState.PropertyType.Garage => data.Properties.Garage.Owned,
                        _ => data.Properties.Bunker.Owned
                    };
                    bool newVal = GUILayout.Toggle(owned, p.ToString());
                    if (newVal != owned)
                        BusinessState.SetPropertyOwned(p, newVal);
                }
            }
        }

        private void DrawQuestsTab()
        {
            GUILayout.Label("Quests (Debug)");
            var q1 = WeaponShipments.Quests.QuestManager.GetNewNumberQuest();
            var q2 = WeaponShipments.Quests.QuestManager.GetStartingSmallQuest();
            var q3 = WeaponShipments.Quests.QuestManager.GetUnpackingQuest();
            var q4 = WeaponShipments.Quests.QuestManager.GetMovingUpQuest();

            if (q1 != null)
            {
                int stage = GetQuestStage(q1);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Quest1 Stage:", GUILayout.Width(100));
                if (int.TryParse(GUILayout.TextField(stage.ToString(), GUILayout.Width(40)), out var s) && s != stage)
                    SetQuestStage(q1, s);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Reveal 22:00"))
                {
                    q1.AgentMeetup();
                    MelonLogger.Msg("[WS Debug] Quest1: Revealed 22:00.");
                }
            }

            if (q2 != null)
            {
                int stage = GetQuestStage(q2);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Quest2 Stage:", GUILayout.Width(100));
                if (int.TryParse(GUILayout.TextField(stage.ToString(), GUILayout.Width(40)), out var s) && s != stage)
                    SetQuestStage(q2, s);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Send 19:00 text")) q2.DebugForce19_00Text();
                if (GUILayout.Button("Reveal Manny 22:00")) WeaponShipments.Quests.QuestManager.DebugForce22_00Reveal();
                if (GUILayout.Button("Hire Archie")) q2.HireArchie();
                if (GUILayout.Button("Complete Hire Archie")) q2.CompleteHireArchie();
            }

            if (q3 != null)
            {
                int stage = GetQuestStage(q3);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Quest3 Unpacking:", GUILayout.Width(100));
                if (int.TryParse(GUILayout.TextField(stage.ToString(), GUILayout.Width(40)), out var s) && s != stage)
                    SetQuestStage(q3, s);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Advance from Talk to Archie")) (q3 as Act3UnpackingQuest)?.AdvanceFromTalkToArchie();
                if (GUILayout.Button("Advance to Stock (4->5)"))
                {
                    if (q3 is Act3UnpackingQuest uq)
                    {
                        SetQuestStage(q3, 5);
                        if (uq.QuestEntries.Count >= 4) uq.QuestEntries[3].Complete();
                        if (uq.QuestEntries.Count >= 5) uq.QuestEntries[4].Begin();
                        Archie.SetDialogueFromUnpackingState();
                    }
                }
                if (GUILayout.Button("Advance from Sell Briefing")) (q3 as Act3UnpackingQuest)?.AdvanceFromSellBriefing();
                if (GUILayout.Button("Complete Sell Job")) (q3 as Act3UnpackingQuest)?.OnSellJobCompleted();
            }

            if (q4 != null)
            {
                int stage = GetQuestStage(q4);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Quest4 MovingUp:", GUILayout.Width(100));
                if (int.TryParse(GUILayout.TextField(stage.ToString(), GUILayout.Width(40)), out var s) && s != stage)
                    SetQuestStage(q4, s);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Purchase Garage")) (q4 as Act3MovingUpQuest)?.PurchaseGarage();
                if (GUILayout.Button("Complete Talk to Agent 28"))
                {
                    if (q4.QuestEntries.Count >= 2) q4.QuestEntries[1].Complete();
                }
                if (GUILayout.Button("Complete Talk to Archie"))
                {
                    if (q4.QuestEntries.Count >= 3) q4.QuestEntries[2].Complete();
                }
            }
        }

        private static void DrawStatField(string label, float val, Action<float> setter)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", GUILayout.Width(140));
            if (float.TryParse(GUILayout.TextField(val.ToString("0"), GUILayout.Width(80)), out var v))
                setter(v);
            GUILayout.EndHorizontal();
        }

        private static int GetQuestStage(Quest q)
        {
            var save = WSSaveData.Instance;
            if (save == null) return 0;
            if (q is Act2StartingSmallQuest) return save.StartingSmallQuest.Stage;
            if (q is Act1NewNumberQuest) return save.NewNumberQuest.Stage;
            if (q is Act3UnpackingQuest) return save.UnpackingQuest.Stage;
            if (q is Act3MovingUpQuest) return save.MovingUpQuest.Stage;
            return 0;
        }

        private static void SetQuestStage(Quest q, int stage)
        {
            var save = WSSaveData.Instance;
            if (save == null) return;
            if (q is Act1NewNumberQuest) save.NewNumberQuest.Stage = stage;
            else if (q is Act2StartingSmallQuest) save.StartingSmallQuest.Stage = stage;
            else if (q is Act3UnpackingQuest) save.UnpackingQuest.Stage = stage;
            else if (q is Act3MovingUpQuest) save.MovingUpQuest.Stage = stage;
            MelonLogger.Msg("[WS Debug] Quest stage set to {0}.", stage);
        }
    }
}
