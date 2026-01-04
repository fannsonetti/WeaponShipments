using MelonLoader;
using System.Collections;
using UnityEngine;
using WeaponShipments.Data;
using WeaponShipments.NPCs;
using WeaponShipments.Saveables;

public static class Act0DelayedStarter
{
    public static void Start()
    {
        MelonCoroutines.Start(StartRoutine());
    }

    private static IEnumerator StartRoutine()
    {
        // Hard wait: 30 seconds
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

        // Warp Agent 28 for the meet
        WarpAgent28GameObject();

        MelonLogger.Msg("[Act0] Contact quest started after fixed delay.");
    }

    private static void WarpAgent28GameObject()
    {
        Vector3 pos = new Vector3(-48.5173f, -2.1f, 40.4007f);
        Quaternion rot = Quaternion.Euler(0f, 200f, 0f);

        GameObject target = null;

        string[] candidates = { "Agent28", "Agent 28", "Agent28(Clone)" };
        foreach (var name in candidates)
        {
            target = GameObject.Find(name);
            if (target != null)
                break;
        }

        if (target == null)
        {
            foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (go == null) continue;
                var n = go.name?.ToLowerInvariant();
                if (n != null && (n.Contains("agent28") || n.Contains("agent 28")))
                {
                    target = go;
                    break;
                }
            }
        }

        if (target == null)
        {
            MelonLogger.Warning("[Act0] Could not find Agent 28 GameObject to warp.");
            return;
        }

        target.transform.position = pos;
        target.transform.rotation = rot;

        MelonLogger.Msg($"[Act0] Warped Agent 28 GO '{target.name}'.");
    }
}
