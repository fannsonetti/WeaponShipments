using MelonLoader;
using System.Collections;
using UnityEngine;
using WeaponShipments.Data;
using WeaponShipments.NPCs;
using WeaponShipments.Saveables;

namespace WeaponShipments.Quests
{
    public static class Act0DelayedStarter
    {
        public static void Start()
        {
            MelonCoroutines.Start(StartRoutine());
        }

        private static IEnumerator StartRoutine()
        {
            yield return new WaitForSeconds(20f);

            var data = WeaponShipmentsSaveData.Instance?.Data;
            if (data == null)
            {
                MelonLogger.Warning("[Act0] Save data not ready; aborting.");
                yield break;
            }

            if (data.Stats.Act0Started)
            {
                MelonLogger.Msg("[Act0] Already started; skipping.");
                yield break;
            }

            data.Stats.Act0Started = true;

            UnknownContact.Instance.SendIntro();

            MelonLogger.Msg("[Act0] Contact quest started after fixed delay.");
        }
    }
}
