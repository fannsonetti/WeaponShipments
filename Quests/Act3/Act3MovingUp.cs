using MelonLoader;
using S1API.Quests;
using UnityEngine;
using WeaponShipments.Data;

namespace WeaponShipments.Quests
{
    /// <summary>
    /// Act 3: Moving Up. Foundation - triggers at 10k total sold.
    /// Steps: Talk to landlord of north warehouse, buy garage space, buy equipment, hire more people.
    /// </summary>
    public class Act3MovingUpQuest : Quest
    {
        protected override string Title => "Moving Up";
        protected override string Description => "Expand your operation.";
        protected override bool AutoBegin => false;

        private WSSaveData.SavedMovingUpQuest Saved => WSSaveData.Instance?.MovingUpQuest;

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

        private static readonly Vector3 LandlordPos = new Vector3(-35f, -3.5f, 168f);

        /// <summary>Called when player buys garage from landlord.</summary>
        public void PurchaseGarage()
        {
            if (QuestEntries.Count >= 1) QuestEntries[0].Complete();
            if (QuestEntries.Count >= 2) QuestEntries[1].Complete();
            if (Stage < 2) Stage = 2;
            if (QuestEntries.Count >= 3) QuestEntries[2].Begin();
            MelonLogger.Msg("[Act3] Garage purchased; advancing to equipment step.");
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            if (QuestEntries.Count == 0)
                CreateEntries();
        }

        protected override void OnCreated()
        {
            base.OnCreated();
            if (QuestEntries.Count == 0)
                CreateEntries();
        }

        private void CreateEntries()
        {
            AddEntry("Talk to the landlord of the north warehouse building", LandlordPos);
            AddEntry("Buy the unused garage space", LandlordPos);
            AddEntry("Buy equipment for the garage", LandlordPos);
            AddEntry("Hire more people", LandlordPos);
        }
    }
}
