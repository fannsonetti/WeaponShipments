using System;
using System.Collections.Generic;
using MelonLoader;
using S1API.Entities;
using UnityEngine;
using UnityEngine.AI;
using ScheduleOne.PlayerScripts; // PlayerCrimeData
using WeaponShipments.Data;      // BusinessState
using WeaponShipments.Services;  // ShipmentManager

namespace WeaponShipments.NPCs
{
    public static class ShipmentBusts
    {
        // Hard-coded world spawn positions per Origin
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
                        new Vector3(72.6435f, -5.5350f, 22.9032f),
                        new Vector3(72.5811f, -5.5350f, 38.9382f),
                        new Vector3(79.9473f, -5.535f, 29.7755f)
                    }
                }
            };

        public static void TryTriggerBust(string shipmentId, Vector3 cratePosition)
        {
            var player = S1API.Entities.Player.Local;
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

            // No busts at Black Market
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

            // ---- Earnings-based difficulty ----
            float earnings = BusinessState.TotalEarnings;

            int minCops;
            int maxCops;
            PlayerCrimeData.EPursuitLevel targetPursuitLevel;
            float bustChance; // 0–1

            if (earnings < 25000f)
            {
                targetPursuitLevel = PlayerCrimeData.EPursuitLevel.NonLethal;
                minCops = 2;
                maxCops = 3;
                bustChance = 0.05f;
            }
            else if (earnings < 100000f)
            {
                targetPursuitLevel = PlayerCrimeData.EPursuitLevel.Lethal;
                minCops = 2;
                maxCops = 4;
                bustChance = 0.10f;
            }
            else
            {
                targetPursuitLevel = PlayerCrimeData.EPursuitLevel.Lethal;
                minCops = 4;
                maxCops = 7;
                bustChance = 0.15f;
            }

            // 🎲 Bust percentage roll
            float roll = UnityEngine.Random.value; // 0–1
            if (roll > bustChance)
            {
                MelonLogger.Msg($"[ShipmentBusts] Bust skipped (roll={roll:0.00}, chance={bustChance:0.00}, earnings={earnings}).");
                return;
            }

            // Apply pursuit level
            var crimeData = FindLocalPlayerCrimeData();
            if (crimeData != null)
            {
                crimeData.SetPursuitLevel(targetPursuitLevel);
                MelonLogger.Msg($"[ShipmentBusts] Set pursuit level to {targetPursuitLevel} (earnings={earnings}).");
            }
            else
            {
                MelonLogger.Warning("[ShipmentBusts] Could not find PlayerCrimeData; pursuit level not set.");
            }

            // Find all officer gameobjects
            List<GameObject> officers = FindAllOfficers();
            if (officers.Count == 0)
            {
                MelonLogger.Warning("[ShipmentBusts] No officers found in scene.");
                return;
            }

            // Cop count from configured min/max, but only clamp against number of officers
            int desired = UnityEngine.Random.Range(minCops, maxCops + 1); // inclusive max
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

                // Disable NavMeshAgent before teleport
                var agent = officer.GetComponent<NavMeshAgent>();
                if (agent != null && agent.enabled)
                    agent.enabled = false;

                // Multiple cops can use same spot: random spawn every time
                int spIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
                Vector3 targetPos = spawnPoints[spIndex];

                // Face the player
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

        private static PlayerCrimeData FindLocalPlayerCrimeData()
        {
            var allCrimeData = UnityEngine.Object.FindObjectsOfType<PlayerCrimeData>();
            if (allCrimeData == null || allCrimeData.Length == 0)
                return null;

            // Prefer the one owned by the local client
            foreach (var cd in allCrimeData)
            {
                if (cd == null || cd.Player == null || cd.Player.Owner == null)
                    continue;

                if (cd.Player.Owner.IsLocalClient)
                    return cd;
            }

            // Fallback: first one
            return allCrimeData[0];
        }

        /// <summary>
        /// Finds ALL GameObjects with "officer" in their name.
        /// </summary>
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
