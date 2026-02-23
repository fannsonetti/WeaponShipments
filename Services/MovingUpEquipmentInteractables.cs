using MelonLoader;
using UnityEngine;
using WeaponShipments.Data;

namespace WeaponShipments.Services
{
    /// <summary>
    /// Manages Moving Up quest equipment: Drill, Tool, Wire. Uses ScheduleOne.Interaction.InteractableObject.
    /// Persistent tags: DrillPlaced, ToolPlaced, WirePlaced (in WSPersistent.PersistedData).
    /// TODO: Add InteractableObject components when ScheduleOne.Interaction API is understood.
    /// </summary>
    public static class MovingUpEquipmentInteractables
    {
        public static bool DrillPlaced
        {
            get => WSPersistent.Instance?.Data?.DrillPlaced ?? false;
            set { if (WSPersistent.Instance?.Data != null) WSPersistent.Instance.Data.DrillPlaced = value; }
        }

        public static bool ToolPlaced
        {
            get => WSPersistent.Instance?.Data?.ToolPlaced ?? false;
            set { if (WSPersistent.Instance?.Data != null) WSPersistent.Instance.Data.ToolPlaced = value; }
        }

        public static bool WirePlaced
        {
            get => WSPersistent.Instance?.Data?.WirePlaced ?? false;
            set { if (WSPersistent.Instance?.Data != null) WSPersistent.Instance.Data.WirePlaced = value; }
        }

        /// <summary>Set up interactable equipment in warehouse when Moving Up starts.</summary>
        public static void SetupEquipmentInteractables()
        {
            // TODO: Use ScheduleOne.Interaction.InteractableObject to create
            // Drill, Tool, Wire interactables. On interact, set corresponding Placed = true.
            MelonLogger.Msg("[MovingUp] Equipment interactables placeholder – InteractableObject API TBD.");
        }
    }
}
