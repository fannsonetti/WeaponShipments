using MelonLoader;
using S1API.GameTime;
using S1API.Quests;
using System.Reflection;
using UnityEngine;
using WeaponShipments.Data;

namespace WeaponShipments.Quests
{
    /// <summary>
    /// Single-step quest for buy supplies. Step text updates based on minutes until delivery.
    /// </summary>
    public class BuySuppliesQuest : Quest
    {
        protected override string Title => "Buy Supplies";
        protected override string Description => "Supplies are on the way.";
        protected override bool AutoBegin => false;
        protected override Sprite? QuestIcon => WeaponShipments.Utils.QuestIconLoader.Load("quest_buy.png");

        private bool _tickHooked;

        protected override void OnLoaded()
        {
            base.OnLoaded();
        }

        protected override void OnCreated()
        {
            base.OnCreated();
        }

        /// <summary>Start the buy supplies quest. Call when player buys from the app.</summary>
        public void StartWithArrivalIn(float arrivesAtSeconds)
        {
            // Complete any existing entries from a previous run
            for (int i = QuestEntries.Count - 1; i >= 0; i--)
                QuestEntries[i]?.Complete();

            var text = FormatMinutesText(arrivesAtSeconds);
            AddEntry(text, Vector3.zero);
            Begin();
            if (QuestEntries.Count >= 1)
                QuestEntries[0].Begin();

            EnsureTickHooked();
        }

        private void EnsureTickHooked()
        {
            if (_tickHooked) return;
            _tickHooked = true;
            TimeManager.OnTick += OnTick;
        }

        private void OnTick()
        {
            if (QuestEntries.Count < 1) return;

            float secs = BusinessState.GetSecondsUntilNextBuyShipmentArrives();
            if (secs < 0f)
            {
                TimeManager.OnTick -= OnTick;
                _tickHooked = false;
                QuestEntries[0]?.Complete();
                Complete();
                return;
            }

            var text = FormatMinutesText(secs);
            SetEntryText(QuestEntries[0], text);
        }

        private static string FormatMinutesText(float seconds)
        {
            int mins = Mathf.Max(0, Mathf.CeilToInt(seconds / 60f));
            return mins <= 0 ? "Supplies arriving" : $"Supplies arriving in {mins} minute{(mins == 1 ? "" : "s")}";
        }

        private static void SetEntryText(QuestEntry entry, string text)
        {
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
                        return;
                    }
                }
            }
            catch { /* ignore */ }
        }

        /// <summary>Call when bought supplies arrive.</summary>
        public void CompleteBuyRun()
        {
            TimeManager.OnTick -= OnTick;
            _tickHooked = false;

            if (QuestEntries.Count >= 1)
            {
                QuestEntries[0]?.Complete();
                Complete();
            }
        }
    }
}
