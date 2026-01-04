using MelonLoader;
using S1API.Internal.Abstraction;   // SaveableField
using S1API.Quests;
using S1API.Saveables;
using UnityEngine;
using WeaponShipments.NPCs;

public class Act0ContactQuest : Quest
{
    protected override string Title => "Act 0 — Contact";
    protected override string Description => "A new opportunity has surfaced.";
    protected override bool AutoBegin => false;

    [SaveableField("stage")]
    private int _stage = 0; // 0=not started, 1=started, 2=meet active, 3=met/completed

    private QuestEntry _meetEntry;
    private QuestEntry _HireEmployee;

    private static readonly Vector3 BlackMarketPos =
        new Vector3(-48.5173f, -2.1f, 40.4007f);

    protected override void OnCreated()
    {
        base.OnCreated();

        // Use the overload your project supports (positional args)
        _meetEntry = AddEntry("Meet the contact at the Black Market", BlackMarketPos);
        _HireEmployee = AddEntry("Hire an employee for the Warehouse", BlackMarketPos);
    }

    // Manager expects this name
    public void StartAct()
    {
        if (_stage != 0)
            return;

        Begin();

        _stage = 2;
        _meetEntry.Begin();

        // If you want Agent 28 warped at start of Act 0 meet stage:
        WarpAgent28();
    }

    // Manager expects this name
    public void OnMetAgent()
    {
        if (_stage >= 3)
            return;

        _stage = 3;
        _meetEntry.Complete();

        // Progress to next stage here (dialogue, unlocks, etc.)
        Complete();
    }
    private static void WarpAgent28()
    {
        Vector3 pos = new Vector3(-48.5173f, -2.1f, 40.4007f);
        Quaternion rot = Quaternion.Euler(0f, 200f, 0f);

        // Try to find the spawned GameObject by common names.
        // (Your prefab identity is "Agent28" in ConfigurePrefab, but scene object names can vary.)
        string[] candidates =
        {
        "Agent 28"
    };

        GameObject target = null;

        foreach (var name in candidates)
        {
            target = GameObject.Find(name);
            if (target != null)
                break;
        }

        // Fallback: search by contains to avoid name variance
        if (target == null)
        {
            foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (go == null) continue;
                if (go.name != null && go.name.ToLowerInvariant().Contains("agent28"))
                {
                    target = go;
                    break;
                }
                if (go.name != null && go.name.ToLowerInvariant().Contains("agent 28"))
                {
                    target = go;
                    break;
                }
            }
        }

        if (target == null)
        {
            MelonLogger.Warning("[Act0] WarpAgent28 failed: could not find Agent 28 GameObject in scene.");
            return;
        }

        target.transform.position = pos;
        target.transform.rotation = rot;

        MelonLogger.Msg($"[Act0] Warped Agent 28 GO '{target.name}' to {pos} rot(0,200,0).");
    }
}
