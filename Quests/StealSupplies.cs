// ===============================================
// Custom StealSupplies quest
// Mod: NPCPack v1.0.0 by FannsoNetti
// Game: TVGS - Schedule I
// ===============================================

using MelonLoader;
using S1API.Console;
using S1API.Entities;
using S1API.GameTime;
using S1API.Internal.Utils;
using S1API.Money;
using S1API.Quests;
using S1API.Quests.Constants;
using S1API.Saveables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MoreNPCs.Quests
{
    /// <summary>
    /// Steal supplies from a source location and deliver them to a destination.
    /// The "Warehouse" / "Bunker" text is fully dynamic based on what you pass in.
    /// </summary>
    public class StealSupplies : Quest
    {
        // Optional identifier if you ever want to look this quest up by name.
        public const string QuestIdentifier = "MoreNPCs.StealSupplies";

        // Dynamic location names. Defaults are just fallbacks.
        private string _sourceName = "Warehouse";
        private string _destinationName = "Bunker";

        // World positions for POIs
        private Vector3 _sourcePos;
        private Vector3 _destinationPos;

        // Quest entries / objectives
        private QuestEntry _goToSourceEntry;        // "Steal from Warehouse"
        private QuestEntry _deliverToDestinationEntry; // "Deliver to Bunker"

        // Quest title & description use the dynamic names
        protected override string Title =>
            $"Steal supplies from {_sourceName}";

        protected override string Description =>
            $"Steal a shipment of supplies from {_sourceName} and deliver it safely to {_destinationName}.";

        // We want to control when the quest starts ourselves
        protected override bool AutoBegin => false;

        /// <summary>
        /// Factory helper – call this from your mission / NPC logic when you
        /// generate a job. Pass in whatever you want shown instead of
        /// "Warehouse" and "Bunker" plus their POI positions.
        /// </summary>
        public static StealSupplies StartQuest(
            string sourceName,
            Vector3 sourcePos,
            string destinationName,
            Vector3 destinationPos)
        {
            // Create a new quest instance for the player
            var q = (StealSupplies)QuestManager.CreateQuest<StealSupplies>();

            // Configure with dynamic data and start it
            q.Configure(sourceName, sourcePos, destinationName, destinationPos);
            q.BeginQuest();

            return q;
        }

        /// <summary>
        /// Injects the current job's from/to data into the quest.
        /// </summary>
        private void Configure(
            string sourceName,
            Vector3 sourcePos,
            string destinationName,
            Vector3 destinationPos)
        {
            _sourceName = sourceName;
            _destinationName = destinationName;
            _sourcePos = sourcePos;
            _destinationPos = destinationPos;

            // If entries already exist (e.g. new job on an already-created quest instance),
            // update their titles + POIs to match the new locations.
            if (_goToSourceEntry != null)
            {
                _goToSourceEntry.Title = $"Steal supplies from {_sourceName}";
                _goToSourceEntry.POIPosition = _sourcePos;
            }

            if (_deliverToDestinationEntry != null)
            {
                _deliverToDestinationEntry.Title = $"Deliver supplies to {_destinationName}";
                _deliverToDestinationEntry.POIPosition = _destinationPos;
            }
        }

        /// <summary>
        /// Starts the quest + first objective, if not already started.
        /// </summary>
        private void BeginQuest()
        {
            if (QuestState == QuestState.Inactive)
                Begin(); // Starts the quest itself

            if (_goToSourceEntry != null &&
                _goToSourceEntry.State == QuestState.Inactive)
            {
                _goToSourceEntry.Begin(); // "Go steal from Warehouse"
            }
        }

        // ---------------- LIFECYCLE ----------------

        /// <summary>
        /// Called when the quest is first created for this save file.
        /// </summary>
        protected override void OnCreated()
        {
            base.OnCreated();

            // If this is a fresh quest, make the entries. If it's a loaded
            // one, wire up references to existing entries instead.
            if (QuestEntries.Count == 0)
            {
                _goToSourceEntry = AddEntry($"Steal supplies from {_sourceName}", _sourcePos);
                _deliverToDestinationEntry = AddEntry($"Deliver supplies to {_destinationName}", _destinationPos);

                // Second objective starts later, after you’ve stolen the supplies
                // so we leave _deliverToDestinationEntry in Inactive state.
            }
            else
            {
                // Loaded quest or somehow created earlier; reuse existing entries.
                if (QuestEntries.Count >= 1)
                    _goToSourceEntry = QuestEntries[0];
                if (QuestEntries.Count >= 2)
                    _deliverToDestinationEntry = QuestEntries[1];
            }

            SubscribeToTriggers();
        }

        /// <summary>
        /// Called after save data is loaded. We only need to re-hook references,
        /// not recreate or change states (the save system handles that).
        /// </summary>
        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (QuestEntries.Count >= 1)
                _goToSourceEntry = QuestEntries[0];
            if (QuestEntries.Count >= 2)
                _deliverToDestinationEntry = QuestEntries[1];
        }

        /// <summary>
        /// Subscribe to any global events you want this quest to react to.
        /// (You can hook game triggers here if you want it to be more automatic.)
        /// </summary>
        private void SubscribeToTriggers()
        {
            // Left empty for now – you’ll drive the quest from your own
            // trigger code using the public methods below.
        }

        // ---------------- GAMEPLAY HOOKS ----------------
        // These are what you call from your world triggers / NPC logic
        // to move the quest through its states.

        /// <summary>
        /// Call this when the player actually steals the supplies at the source
        /// (e.g. interact inside the warehouse).
        /// </summary>
        public void OnSuppliesStolen()
        {
            if (_goToSourceEntry == null)
                return;

            // Finish first objective if it’s active
            if (_goToSourceEntry.State == QuestState.Active)
            {
                _goToSourceEntry.Complete();
            }

            // Start the delivery objective
            if (_deliverToDestinationEntry != null &&
                _deliverToDestinationEntry.State == QuestState.Inactive)
            {
                _deliverToDestinationEntry.Begin();
            }
        }

        /// <summary>
        /// Call this when the player reaches the delivery location
        /// (your "Bunker" or whatever destination).
        /// </summary>
        public void OnSuppliesDelivered()
        {
            if (_deliverToDestinationEntry == null)
                return;

            if (_deliverToDestinationEntry.State == QuestState.Active)
            {
                _deliverToDestinationEntry.Complete();
            }

            // Quest is done once both objectives are complete.
            // (You could add more checks here if you ever add more entries.)
            if (QuestState == QuestState.Active)
            {
                Complete();
            }
        }

        /// <summary>
        /// Optional: If you want a simple "abort job" hook.
        /// </summary>
        public void OnJobCancelled()
        {
            if (QuestState == QuestState.Active ||
                QuestState == QuestState.Inactive)
            {
                Cancel();
            }
        }
    }
}
