using System;
using System.Collections.Generic;
using MelonLoader;
using S1API.Entities;
using S1API.Law;
using UnityEngine;
using UnityEngine.AI;
using WeaponShipments.Data;
using WeaponShipments.Services;

namespace WeaponShipments.NPCs
{
    public static class ShipmentBusts
    {
        private static readonly Dictionary<string, Vector3[]> SpawnPositionsByOrigin =
            new Dictionary<string, Vector3[]>
            {
                {
                    "RV",
                    new[]
                    {
                        new Vector3( 4.9481f,  1.2986f, -70.2113f),
                        new Vector3(21.1344f,  1.2909f, -66.8579f),
                        new Vector3(29.6539f,  2.4616f, -88.6574f)
                    }
                },
                {
                    "Gazebo",
                    new[]
                    {
                        new Vector3(91.8661f,  4.9643f, -112.3821f),
                        new Vector3(89.7267f,  5.0127f, -134.6288f)
                    }
                },
                {
                    "Sewer Market",
                    new[]
                    {
                        new Vector3(79.7539f, -5.535f, 28.9794f),
                        new Vector3(80.4278f, -5.535f, 31.4929f),
                        new Vector3(78.6597f, -5.535f, 32.7226f)
                    }
                }
            };

        public static void TryTriggerBust(string shipmentId, Vector3 cratePosition)
        {
            var player = Player.Local;
            if (player == null)
            {
                MelonLogger.Warning("[ShipmentBusts] No local S1API player; cannot trigger bust.");
                return;
            }

            var shipment = ShipmentManager.Instance.GetShipment(shipmentId);
            if (shipment == null)
            {
                MelonLogger.Warning($"[ShipmentBusts] No shipment found for id '{shipmentId}'.");
                return;
            }

            string origin = shipment.Origin;

            if (origin == "Black Market")
            {
                MelonLogger.Msg("[ShipmentBusts] Busts are disabled for Black Market shipments.");
                return;
            }

            if (!SpawnPositionsByOrigin.TryGetValue(origin, out var spawnPoints) ||
                spawnPoints == null || spawnPoints.Length == 0)
            {
                MelonLogger.Warning($"[ShipmentBusts] No spawn points configured for origin '{origin}'.");
                return;
            }

            float earnings = BusinessState.TotalEarnings;

            int minCops;
            int maxCops;
            PursuitLevel targetPursuitLevel;
            float bustChance;

            if (earnings < WeaponShipmentsPrefs.BuyBustTier1MaxEarnings.Value)
            {
                targetPursuitLevel = PursuitLevel.NonLethal;
                minCops = 2;
                maxCops = 3;
                bustChance = WeaponShipmentsPrefs.BuyBustChanceTier1.Value;
            }
            else if (earnings < WeaponShipmentsPrefs.BuyBustTier2MaxEarnings.Value)
            {
                targetPursuitLevel = PursuitLevel.Lethal;
                minCops = 2;
                maxCops = 4;
                bustChance = WeaponShipmentsPrefs.BuyBustChanceTier2.Value;
            }
            else
            {
                targetPursuitLevel = PursuitLevel.Lethal;
                minCops = 4;
                maxCops = 7;
                bustChance = WeaponShipmentsPrefs.BuyBustChanceTier3.Value;
            }

            float roll = UnityEngine.Random.value; // 0–1
            if (roll > bustChance)
            {
                MelonLogger.Msg($"[ShipmentBusts] Bust skipped (roll={roll:0.00}, chance={bustChance:0.00}, earnings={earnings}).");
                return;
            }

            LawManager.SetWantedLevel(player, targetPursuitLevel);
            player.CrimeData.RecordLastKnownPosition(resetTimeSinceSighted: true);
            LawManager.CallPolice(player);

            MelonLogger.Msg($"[ShipmentBusts] Set wanted level to {targetPursuitLevel} (earnings={earnings}).");

            List<GameObject> officers = FindAllOfficers();
            if (officers.Count == 0)
            {
                MelonLogger.Warning("[ShipmentBusts] No officers found in scene.");
                return;
            }

            int desired = UnityEngine.Random.Range(minCops, maxCops + 1);
            int numToSpawn = Mathf.Min(desired, officers.Count);

            if (numToSpawn <= 0)
            {
                MelonLogger.Warning("[ShipmentBusts] Not enough officers to spawn.");
                return;
            }

            Vector3 playerPos = player.Position;

            for (int i = 0; i < numToSpawn; i++)
            {
                GameObject officer = officers[i];
                if (officer == null)
                    continue;

                var agent = officer.GetComponent<NavMeshAgent>();
                if (agent != null && agent.enabled)
                    agent.enabled = false;

                int spIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
                Vector3 targetPos = spawnPoints[spIndex];

                Vector3 toPlayer = playerPos - targetPos;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude < 0.001f)
                    toPlayer = Vector3.forward;

                Quaternion rot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);

                officer.transform.position = targetPos;
                officer.transform.rotation = rot;

                MelonLogger.Msg($"[ShipmentBusts] Teleported '{officer.name}' to {targetPos} (Origin: {origin}, slot {spIndex}).");
            }

            MelonLogger.Msg($"[ShipmentBusts] Spawned {numToSpawn} officers (desired {desired}) for earnings={earnings}.");
        }

        private static List<GameObject> FindAllOfficers()
        {
            var list = new List<GameObject>();
            var all = UnityEngine.Object.FindObjectsOfType<GameObject>();

            foreach (var go in all)
            {
                if (!go || !go.activeInHierarchy)
                    continue;

                string name = go.name;
                if (string.IsNullOrEmpty(name))
                    continue;

                if (name.IndexOf("officer", StringComparison.OrdinalIgnoreCase) >= 0)
                    list.Add(go);
            }

            return list;
        }
    }
}
