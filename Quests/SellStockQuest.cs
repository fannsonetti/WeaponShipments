using MelonLoader;
using S1API.Quests;
using UnityEngine;

namespace WeaponShipments.Quests
{
    /// <summary>
    /// Two-step quest for each sell job. Pick up stock, deliver to dropoff.
    /// </summary>
    public class SellStockQuest : Quest
    {
        protected override string Title => "Sell Stock";
        protected override string Description => "Pick up and deliver stock.";
        protected override bool AutoBegin => false;
        protected override Sprite? QuestIcon => WeaponShipments.Utils.QuestIconLoader.Load("quest_sell.png");

        protected override void OnLoaded()
        {
            base.OnLoaded();
        }

        protected override void OnCreated()
        {
            base.OnCreated();
        }

        /// <summary>Start the sell run. Step 1: pickup, Step 2: delivery.</summary>
        public void StartWithPickupAt(Vector3 pickupPos, string pickupLabel, Vector3 deliveryPos, string deliveryLabel)
        {
            // Complete any existing entries from a previous run
            for (int i = QuestEntries.Count - 1; i >= 0; i--)
                QuestEntries[i]?.Complete();

            AddEntry($"Pick up stock at the {pickupLabel}", pickupPos);
            AddEntry($"Deliver stock to the {deliveryLabel}", deliveryPos);
            Begin();
            if (QuestEntries.Count >= 1)
                QuestEntries[0].Begin();
        }

        /// <summary>Call when the player delivers the stock.</summary>
        public void CompleteSellRun()
        {
            for (int i = 0; i < QuestEntries.Count; i++)
                QuestEntries[i]?.Complete();
            Complete();
        }
    }
}
