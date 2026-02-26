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
        protected override Sprite? QuestIcon => WeaponShipments.Utils.QuestIconLoader.Load("quest_moving_up.png");

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
        private static readonly Vector3 Agent28WarehousePos = new Vector3(-23.0225f, -5f, 170.31f);
        private static readonly Vector3 ArchieWarehousePos = new Vector3(-30.9878f, -3.8f, 171.478f);

        /// <summary>Called when player buys garage from landlord.</summary>
        public void PurchaseGarage()
        {
            if (QuestEntries.Count >= 1) QuestEntries[0].Complete();
            if (Stage < 2) Stage = 2;
            if (QuestEntries.Count >= 2) QuestEntries[1].Begin();
            if (QuestEntries.Count >= 3) QuestEntries[2].Begin();
            WeaponShipments.NPCs.Agent28.SetMovingUpDialogueActive();
            CustomNPCTest.NPCs.Archie.SetMovingUpDialogueActive();
            MelonLogger.Msg("[Act3] Garage purchased; talk to Archie and Agent 28.");
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            if (QuestEntries.Count == 0)
                CreateEntries();
            if (Stage >= 2)
            {
                var entries = QuestEntries;
                if (entries != null && entries.Count >= 2)
                    WeaponShipments.NPCs.Agent28.SetMovingUpDialogueActive();
                if (entries != null && entries.Count >= 3)
                    CustomNPCTest.NPCs.Archie.SetMovingUpDialogueActive();
            }
        }

        protected override void OnCreated()
        {
            base.OnCreated();
            if (QuestEntries.Count == 0)
                CreateEntries();
        }

        private void CreateEntries()
        {
            AddEntry("Buy the garage from the landlord", LandlordPos);
            AddEntry("Talk to Agent 28", Agent28WarehousePos);
            AddEntry("Talk to Archie", ArchieWarehousePos);
        }
    }
}
