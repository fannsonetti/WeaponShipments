using System.Collections;
using UnityEngine;
using S1API.Entities;
using MelonLoader;

namespace WeaponShipments
{
    public class BunkerTeleportTrigger : MonoBehaviour
    {
        public Vector3 teleportTo = new Vector3(280.25f, 21f, 268f);
        public string vehicleName = "PlayerPusher";

        private bool _busy;

        private void OnTriggerEnter(Collider other)
        {
            if (_busy) return;
            if (other == null || other.transform == null) return;

            // Never act on our own bunker/trigger colliders
            if (other.transform.IsChildOf(transform.root))
                return;

            // PLAYER: teleport player's root if the collider belongs to player hierarchy
            var p = Player.Local;
            if (p != null && p.Transform != null)
            {
                var playerRoot = p.Transform.root;
                if (other.transform.IsChildOf(playerRoot))
                {
                    MelonLogger.Msg($"[Bunker] Trigger hit by PLAYER: {playerRoot.name}");
                    StartCoroutine(TeleportNextFixed(playerRoot, teleportTo));
                    return;
                }
            }

            // VEHICLE: find the highest parent in the collider's ancestry that matches PlayerPusher,
            // then teleport THAT object's ROOT (the vehicle rig root you asked for).
            var pusherObj = FindTopmostParentByNameContains(other.transform, vehicleName);
            if (pusherObj != null)
            {
                var vehicleRoot = pusherObj.root;
                MelonLogger.Msg($"[Bunker] Trigger hit by VEHICLE: pusher={pusherObj.name}, root={vehicleRoot.name}");
                StartCoroutine(TeleportNextFixed(vehicleRoot, teleportTo));
            }
        }

        private static Transform FindTopmostParentByNameContains(Transform start, string needle)
        {
            if (start == null || string.IsNullOrEmpty(needle))
                return null;

            Transform highest = null;
            var t = start;

            while (t != null)
            {
                if (!string.IsNullOrEmpty(t.name) &&
                    t.name.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    highest = t;
                }
                t = t.parent;
            }

            return highest;
        }

        private IEnumerator TeleportNextFixed(Transform root, Vector3 targetPos)
        {
            _busy = true;

            yield return new WaitForFixedUpdate();

            Vector3 from = root.position;
            Vector3 delta = targetPos - from;

            var rbs = root.GetComponentsInChildren<Rigidbody>(true);

            // Freeze physics briefly so controllers don't fight the move
            bool[] wasKinematic = null;
            if (rbs != null && rbs.Length > 0)
            {
                wasKinematic = new bool[rbs.Length];
                for (int i = 0; i < rbs.Length; i++)
                {
                    var rb = rbs[i];
                    if (rb == null) continue;

                    wasKinematic[i] = rb.isKinematic;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
            }

            // Move root and rigidbodies consistently
            root.position = targetPos;

            if (rbs != null && rbs.Length > 0)
            {
                for (int i = 0; i < rbs.Length; i++)
                {
                    var rb = rbs[i];
                    if (rb == null) continue;

                    rb.position = rb.position + delta;
                    rb.transform.position = rb.position;
                }
            }

            Physics.SyncTransforms();

            yield return new WaitForFixedUpdate();

            // Restore kinematic state
            if (rbs != null && rbs.Length > 0 && wasKinematic != null)
            {
                for (int i = 0; i < rbs.Length; i++)
                {
                    var rb = rbs[i];
                    if (rb == null) continue;
                    rb.isKinematic = wasKinematic[i];
                }
            }

            Physics.SyncTransforms();

            MelonLogger.Msg($"[Bunker] Teleported '{root.name}' from {from} to {targetPos} (rbCount={(rbs?.Length ?? 0)})");

            // Cooldown to avoid repeated teleports while overlapping
            yield return new WaitForSeconds(1.0f);
            _busy = false;
        }
    }
}
