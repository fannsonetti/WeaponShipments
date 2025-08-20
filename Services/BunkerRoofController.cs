using UnityEngine;
using S1API.Entities;

namespace WeaponShipments
{
    public class BunkerRoofController : MonoBehaviour
    {
        // Local rotation values
        public Vector3 closedEuler = new Vector3(270f, 0f, 0f);
        public Vector3 openEuler = new Vector3(290f, 180f, 180f);

        // Behavior tuning
        public float openDistance = 15f;
        public float smoothSpeed = 0.3f;

        // Vehicle identifier
        public string vehicleName = "PlayerPusher";

        private Quaternion _closedRot;
        private Quaternion _openRot;
        private float _t;

        private static readonly Collider[] _hits = new Collider[96];

        private void Awake()
        {
            _closedRot = Quaternion.Euler(closedEuler);
            _openRot = Quaternion.Euler(openEuler);

            transform.localRotation = _closedRot;
            _t = 0f;
        }

        private void Update()
        {
            bool shouldOpen = IsPlayerNear() || IsVehicleNear();

            float target = shouldOpen ? 1f : 0f;
            _t = Mathf.MoveTowards(_t, target, Time.deltaTime * smoothSpeed);

            transform.localRotation = Quaternion.Slerp(_closedRot, _openRot, _t);
        }

        private bool IsPlayerNear()
        {
            var p = Player.Local;
            if (p == null) return false;

            return Vector3.Distance(p.Position, transform.position) <= openDistance;
        }

        private bool IsVehicleNear()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                openDistance,
                _hits,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                var c = _hits[i];
                if (c == null) continue;

                // Walk up the hierarchy to find PlayerPusher
                var t = c.transform;
                while (t != null)
                {
                    if (!string.IsNullOrEmpty(t.name) &&
                        t.name.IndexOf(vehicleName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                    t = t.parent;
                }
            }

            return false;
        }
    }
}
