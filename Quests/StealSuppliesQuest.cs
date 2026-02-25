using MelonLoader;
using S1API.Quests;
using UnityEngine;
using WeaponShipments.Services;

namespace WeaponShipments.Quests
{
    /// <summary>
    /// Two-step quest for each steal run. Step 1: pickup waypoint. Step 2: delivery waypoint (activates when player approaches crate).
    /// </summary>
    public class StealSuppliesQuest : Quest
    {
        protected override string Title => "Steal Supplies";
        protected override string Description => "Pick up and deliver supplies.";
        protected override bool AutoBegin => false;
        protected override Sprite? QuestIcon => WeaponShipments.Utils.QuestIconLoader.Load("quest_steal.png");

        private string _currentDestination;

        protected override void OnLoaded()
        {
            base.OnLoaded();
        }

        protected override void OnCreated()
        {
            base.OnCreated();
        }

        /// <summary>Start the steal run. Step 1: pickup. Step 2 activates when player approaches crate.</summary>
        public void StartWithPickupAt(string origin, string destination)
        {
            _currentDestination = destination ?? string.Empty;
            var pickupPos = ShipmentSpawner.GetPickupPositionForOrigin(origin);

            // Complete any existing entries from a previous run
            for (int i = QuestEntries.Count - 1; i >= 0; i--)
                QuestEntries[i]?.Complete();

            AddEntry($"Pick up supplies at the {origin}", pickupPos);
            Begin();
            if (QuestEntries.Count >= 1)
                QuestEntries[QuestEntries.Count - 1].Begin();
        }

        /// <summary>Call when player approaches the crate (Agent 28 sends dropoff text). Activates step 2.</summary>
        public void ActivateDeliveryStep()
        {
            if (string.IsNullOrEmpty(_currentDestination)) return;
            if (QuestEntries.Count < 1) return;

            // Complete step 1
            QuestEntries[0]?.Complete();

            var deliveryPos = ShipmentSpawner.GetDeliveryPositionForDestination(_currentDestination);
            AddEntry($"Deliver supplies to the {_currentDestination}", deliveryPos);
            if (QuestEntries.Count >= 2)
                QuestEntries[1].Begin();
        }

        /// <summary>Call when the player delivers the stolen supplies.</summary>
        public void CompleteStealRun()
        {
            if (QuestEntries.Count >= 1)
            {
                var last = QuestEntries.Count - 1;
                QuestEntries[last]?.Complete();
                Complete();
            }
        }
    }
}
