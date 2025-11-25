using S1API.Entities;
using UnityEngine;
using WeaponShipments.NPCs;
using WeaponShipments.Services;

public class ShipmentProximityDetector : MonoBehaviour
{
    private bool _triggered;
    private string _dropoffLabel;
    private string _shipmentId;

    public void Init(string shipmentId, string dropoffLabel)
    {
        _shipmentId = shipmentId;
        _dropoffLabel = dropoffLabel;
    }

    private void Update()
    {
        if (_triggered)
            return;

        var player = Player.Local;
        if (player == null)
            return;

        var playerPos = player.Position;
        var cratePos = transform.position;

        float dist = Vector3.Distance(playerPos, cratePos);

        if (dist <= 5f)
        {
            _triggered = true;

            // Send dropoff message
            Agent28.NotifyStealDropoff(_dropoffLabel);

            // Spawn delivery area at this moment
            ShipmentSpawner.SpawnDeliveryArea(_shipmentId);

            // Trigger bust: we now pass shipmentId + crate position
            ShipmentBusts.TryTriggerBust(_shipmentId, cratePos);
        }
    }
}
