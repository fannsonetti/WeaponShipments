using MelonLoader;
using S1API.GameTime;
using S1API.Quests;
using UnityEngine;
using WeaponShipments.Data;
using WeaponShipments.Services;
using CustomNPCTest.NPCs;

namespace WeaponShipments.Quests
{
    /// <summary>
    /// Act 3: Unpacking. Archie-led tutorial: supplies, production, selling.
    /// </summary>
    public class Act3UnpackingQuest : Quest
    {
        protected override string Title => "Unpacking";
        protected override string Description => "Archie shows you how to run the operation.";
        protected override bool AutoBegin => false;
        protected override Sprite? QuestIcon => WeaponShipments.Utils.QuestIconLoader.Load("quest_unpacking.png");

        private static readonly Vector3 ArchieWarehousePos = new Vector3(-30.9878f, -3.87f, 171.478f);

        private bool _tickHooked;
        private int _snapshotResupplyStarted;
        private int _snapshotResupplyCompleted;
        private QuestEntry _unpackingDeliveryEntry;

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
            AddEntry("Talk to Archie at the warehouse", ArchieWarehousePos);
            AddEntry("Use the app to steal supplies");
            AddEntry("Deliver stolen supplies to the warehouse");
            AddEntry("Wait for production to build stock");
            AddEntry("Talk to Archie about selling", ArchieWarehousePos);
            AddEntry("Sell stock and complete the delivery");
        }

        internal void EnsureTickHooked()
        {
            if (_tickHooked || Stage < 1) return;
            _tickHooked = true;
            TimeManager.OnTick += OnTick;
        }

        internal void OnQuestStarted()
        {
            _snapshotResupplyStarted = BusinessState.ResupplyJobsStarted;
            _snapshotResupplyCompleted = BusinessState.ResupplyJobsCompleted;
        }

        private void OnTick()
        {
            if (Stage < 2 || Stage > 5) return;

            // Stage 2->3: handled by OnUnpackingStealProximityReached when player approaches crate (not by ResupplyJobsStarted)

            if (Stage == 3 && BusinessState.ResupplyJobsCompleted > _snapshotResupplyCompleted)
            {
                AdvanceToStage(4);
                if (QuestEntries.Count >= 3) QuestEntries[2].Complete();
                _unpackingDeliveryEntry?.Complete();
                _unpackingDeliveryEntry = null;
                if (QuestEntries.Count >= 4) QuestEntries[3].Begin();
                return;
            }

            if (Stage == 4 && BusinessState.Stock > 0f)
            {
                // First stock during Unpacking: set to 5 for the tutorial sell
                BusinessState.SetStockForProperty(BusinessState.ActiveProperty, 5f);
                AdvanceToStage(5);
                if (QuestEntries.Count >= 4) QuestEntries[3].Complete();
                if (QuestEntries.Count >= 5) QuestEntries[4].Begin();
                Archie.SetDialogueFromUnpackingState();
                return;
            }
        }

        /// <summary>Start the steal flow inside Unpacking (no StealSuppliesQuest). Call when steal is pressed from app during Stage 2.</summary>
        internal void StartUnpackingSteal(string origin, string destination)
        {
            if (Stage != 2) return;

            _unpackingStealOrigin = origin;
            _unpackingStealDestination = destination ?? string.Empty;

            if (QuestEntries.Count >= 2) QuestEntries[1].Complete();
            var pickupPos = ShipmentSpawner.GetPickupPositionForOrigin(origin);
            AddEntry($"Pick up supplies at the {origin}", pickupPos);
            if (QuestEntries.Count >= 1) QuestEntries[QuestEntries.Count - 1].Begin();
        }

        private string _unpackingStealOrigin;
        private string _unpackingStealDestination;

        /// <summary>Call when player approaches the crate during Unpacking steal. Activates delivery step.</summary>
        internal void OnUnpackingStealProximityReached()
        {
            if (Stage != 2 || string.IsNullOrEmpty(_unpackingStealDestination)) return;

            var pickupIdx = QuestEntries.Count - 1;
            if (pickupIdx >= 0) QuestEntries[pickupIdx].Complete();

            AdvanceToStage(3);
            if (QuestEntries.Count >= 3) QuestEntries[2].Complete(); // old "Deliver supplies" placeholder
            var deliveryPos = ShipmentSpawner.GetDeliveryPositionForDestination(_unpackingStealDestination);
            AddEntry($"Deliver supplies to the {_unpackingStealDestination}", deliveryPos);
            if (QuestEntries.Count >= 1)
            {
                _unpackingDeliveryEntry = QuestEntries[QuestEntries.Count - 1];
                _unpackingDeliveryEntry.Begin();
            }
        }

        /// <summary>True if Unpacking is currently handling a steal (Stage 2 or 3).</summary>
        internal bool IsHandlingSteal => Stage == 2 || Stage == 3;

        /// <summary>True if Unpacking is currently handling the sell step (Stage 6).</summary>
        internal bool IsHandlingSell => Stage == 6;

        /// <summary>Called when player completes Archie's Unpacking intro dialogue.</summary>
        internal void AdvanceFromTalkToArchie()
        {
            if (Stage != 1) return;

            AdvanceToStage(2);
            if (QuestEntries.Count >= 1) QuestEntries[0].Complete();
            if (QuestEntries.Count >= 2) QuestEntries[1].Begin();
        }

        /// <summary>Called when player completes Archie's sell briefing dialogue.</summary>
        internal void AdvanceFromSellBriefing()
        {
            if (Stage != 5) return;

            AdvanceToStage(6);
            if (QuestEntries.Count >= 5) QuestEntries[4].Complete();
            if (QuestEntries.Count >= 6) QuestEntries[5].Begin();
            Archie.SetDialogueFromUnpackingState();
        }

        internal void OnSellJobCompleted()
        {
            if (Stage != 6) return;

            TimeManager.OnTick -= OnTick;
            _tickHooked = false;

            if (QuestEntries.Count >= 6) QuestEntries[5].Complete();
            Stage = 7;
            Complete();

            QuestManager.TryStartMovingUpOnZoneEntry();
            MelonLogger.Msg("[Act3] Unpacking complete.");
        }

        private void AdvanceToStage(int stage)
        {
            Stage = stage;
        }
    }
}
