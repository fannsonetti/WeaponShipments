// Act0ContactQuest.cs
using MelonLoader;
using S1API.GameTime;
using S1API.Quests;
using S1API.Saveables;
using UnityEngine;
using WeaponShipments.NPCs;
using WeaponShipments.Data;

public class Act0ContactQuest : Quest
{
    protected override string Title => "Act 0 — Contact";
    protected override string Description => "A new opportunity has surfaced.";
    protected override bool AutoBegin => false;

    // --- Persistence (WeaponShipmentsSaveData) ---
    // Quest state is NOT saved through S1API's modded quest saver. Everything we need lives in WeaponShipmentsSaveData.
    private WeaponShipmentsSaveData.SavedAct0ContactQuest Saved => WeaponShipmentsSaveData.Instance?.Act0Contact;

    // Local fallbacks (used only if SaveData is not yet available at runtime)
    private int _stageFallback = 0;
    private bool _awaitingWakeupFallback = false;
    private int _leadDayFallback = -1;
    private bool _sent1900Fallback = false;
    private bool _revealed2200Fallback = false;

    private int Stage
    {
        get => Saved != null ? Saved.Stage : _stageFallback;
        set
        {
            if (Saved != null) Saved.Stage = value;
            _stageFallback = value;
        }
    }

    // --- Time-gating state (Stage 2) ---
    private bool AwaitingWakeup
    {
        get => Saved != null ? Saved.AwaitingWakeup : _awaitingWakeupFallback;
        set
        {
            if (Saved != null) Saved.AwaitingWakeup = value;
            _awaitingWakeupFallback = value;
        }
    }

    private int LeadDay
    {
        get => Saved != null ? Saved.LeadDay : _leadDayFallback;
        set
        {
            if (Saved != null) Saved.LeadDay = value;
            _leadDayFallback = value;
        }
    }

    private bool Sent1900
    {
        get => Saved != null ? Saved.Sent1900 : _sent1900Fallback;
        set
        {
            if (Saved != null) Saved.Sent1900 = value;
            _sent1900Fallback = value;
        }
    }

    private bool Revealed2200
    {
        get => Saved != null ? Saved.Revealed2200 : _revealed2200Fallback;
        set
        {
            if (Saved != null) Saved.Revealed2200 = value;
            _revealed2200Fallback = value;
        }
    }

    private QuestEntry _agentMeetupEntry;
    private QuestEntry _waitForEmployeeEntry;
    private QuestEntry _mannyMeetupEntry;
    private QuestEntry _hireArchieEntry;
    private QuestEntry _equipmentSearchEntry;

    private static readonly Vector3 BlackMarketPos =
        new Vector3(-44.3456f, -1.135f, 23.4812f);

    private static readonly Vector3 DocksPos =
        new Vector3(-98.23f, -1.535f, -38.7985f);

    private static readonly Vector3 EquipmentPos =
        new Vector3(-48.5173f, -2.1f, 40.4007f);

    private bool _timeHooksAttached = false;

    // Important: do NOT SaveableField this.
    // We only need it for the load lifecycle decision.
    private bool _loadedFromSave = false;

    /// <summary>
    /// S1API load order (per your working quest pattern):
    /// - Loaded quests: OnLoaded() first (QuestEntries already exist), then OnCreated()
    /// - New quests: only OnCreated()
    ///
    /// Rule: OnLoaded should bind/create entries only. Do NOT call Begin/Complete/SetState here.
    /// Let S1API restore state, then apply side-effects later.
    /// </summary>
    protected override void OnLoaded()
    {
        base.OnLoaded();

        _loadedFromSave = true;

        // If entries already exist, just bind our fields to them.
        // If not, create them here so subsequent save/restore paths are consistent.
        if (QuestEntries.Count == 0)
        {
            CreateEntries();
        }

        RebindEntriesFromList();
        // Do NOT Begin/Complete here; S1API state restoration handles that.
    }

    protected override void OnCreated()
    {
        base.OnCreated();

        if (QuestEntries.Count == 0)
        {
            // New quest path: entries do not exist yet.
            CreateEntries();
        }

        // Loaded or new: ensure fields are bound.
        RebindEntriesFromList();

        AttachTimeHooksOnce();

        if (_loadedFromSave)
        {
            // Critical: do NOT mutate quest/entry state (Begin/Complete/Complete()) here for loaded quests.
            // Let S1API restore the states, then re-apply ONLY side-effects after restore finishes.
            MelonCoroutines.Start(ApplyStageSideEffectsNextFrame());
        }
        else
        {
            // New quest path: internal stage machine can initialize state as needed.
            RestoreStageStateForNewQuestOnly();
        }
    }

    private void CreateEntries()
    {
        // Entries match your naming and linear quest steps:
        // AgentMeetup -> WaitForEmployee -> MannyMeetup -> HireArchie -> EquipmentSearch -> FoundEquipment
        AddEntry("Agent meetup (Black Market)", BlackMarketPos);
        AddEntry("Wait for Agent 28 to find an employee");        // non-positional is OK in your environment
        AddEntry("Manny meetup (Docks)", DocksPos);
        AddEntry("Hire Archie");
        AddEntry("Search for equipment", EquipmentPos);
    }

    private void RebindEntriesFromList()
    {
        // Maintain the exact same order as CreateEntries()
        if (QuestEntries.Count >= 1) _agentMeetupEntry = QuestEntries[0];
        if (QuestEntries.Count >= 2) _waitForEmployeeEntry = QuestEntries[1];
        if (QuestEntries.Count >= 3) _mannyMeetupEntry = QuestEntries[2];
        if (QuestEntries.Count >= 4) _hireArchieEntry = QuestEntries[3];
        if (QuestEntries.Count >= 5) _equipmentSearchEntry = QuestEntries[4];
    }

    private void AttachTimeHooksOnce()
    {
        if (_timeHooksAttached)
            return;

        _timeHooksAttached = true;
        TimeManager.OnSleepEnd += OnSleepEnd;
        TimeManager.OnTick += OnTick;
    }

    /// <summary>
    /// For NEW quests only. This function can mutate quest/entry state.
    /// For LOADED quests, do not call this during the load lifecycle.
    /// </summary>
    private void RestoreStageStateForNewQuestOnly()
    {
        // For a brand-new quest, stage should normally be 0 and we do nothing.
        // Kept for completeness / dev tools if you ever set Stage via code.
        if (Stage == 0)
            return;

        // Defensive: if entries failed to bind for any reason, avoid nulls.
        if (_agentMeetupEntry == null || _waitForEmployeeEntry == null || _mannyMeetupEntry == null ||
            _hireArchieEntry == null || _equipmentSearchEntry == null)
        {
            MelonLogger.Warning("[Act0] RestoreStageStateForNewQuestOnly: one or more QuestEntry references are null.");
            return;
        }

        // If you ever force a non-zero stage on a newly created quest, this restores it.
        switch (Stage)
        {
            case 1:
                Begin();
                _agentMeetupEntry.Begin();
                WarpAgent28ToBlackMarket();
                Agent28.SetMeetupDialogueActive();
                break;

            case 2:
                Begin();
                _agentMeetupEntry.Complete();
                _waitForEmployeeEntry.Begin();
                break;

            case 3:
                Begin();
                _agentMeetupEntry.Complete();
                _waitForEmployeeEntry.Complete();
                _mannyMeetupEntry.Begin();
                break;

            case 4:
                Begin();
                _agentMeetupEntry.Complete();
                _waitForEmployeeEntry.Complete();
                _mannyMeetupEntry.Complete();
                _hireArchieEntry.Begin();
                break;

            case 5:
                Begin();
                _agentMeetupEntry.Complete();
                _waitForEmployeeEntry.Complete();
                _mannyMeetupEntry.Complete();
                _hireArchieEntry.Complete();
                _equipmentSearchEntry.Begin();
                break;

            case 6:
                Begin();
                _agentMeetupEntry.Complete();
                _waitForEmployeeEntry.Complete();
                _mannyMeetupEntry.Complete();
                _hireArchieEntry.Complete();
                _equipmentSearchEntry.Complete();
                Complete();
                Agent28.SetDefaultDialogueActive();
                break;
        }
    }

    /// <summary>
    /// For LOADED quests only: re-apply ONLY side-effects that are not entry/quest state mutations.
    /// We delay a frame so S1API's restore pass can complete before we touch anything.
    /// </summary>
    private System.Collections.IEnumerator ApplyStageSideEffectsNextFrame()
    {
        // allow S1API restore to finish
        yield return null;

        // Defensive
        if (_agentMeetupEntry == null || _waitForEmployeeEntry == null || _mannyMeetupEntry == null ||
            _hireArchieEntry == null || _equipmentSearchEntry == null)
        {
            MelonLogger.Warning("[Act0] ApplyStageSideEffectsNextFrame: one or more QuestEntry references are null.");
            yield break;
        }

        // Apply only "world" effects. Do NOT call Begin/Complete/Complete() here.
        switch (Stage)
        {
            case 0:
                // nothing
                break;

            case 1:
                WarpAgent28ToBlackMarket();
                Agent28.SetMeetupDialogueActive();
                break;

            case 2:
                // time hooks are already attached; leave the entries/state to S1API
                break;

            case 3:
                TeleportMeetupNpcsToDocks();
                break;

            case 4:
            case 5:
                // no special world effects required here
                break;

            case 6:
                Agent28.SetDefaultDialogueActive();
                break;
        }
    }

    // Step 1
    public void AgentMeetup()
    {
        if (Stage != 0)
            return;

        Begin();

        Stage = 1;
        _agentMeetupEntry.Begin();

        WarpAgent28ToBlackMarket();
        Agent28.SetMeetupDialogueActive();
    }

    // Step 2 (called when the Agent28 meetup dialogue completes)
    public void WaitForEmployee()
    {
        if (Stage != 1)
            return;

        _agentMeetupEntry.Complete();

        Stage = 2;
        _waitForEmployeeEntry.Begin();

        Agent28.Instance?.SendTextMessage("I'll ask around for employees. Get some sleep.");

        // Start the time-gated chain AFTER the next wakeup.
        AwaitingWakeup = true;
        LeadDay = -1;
        Sent1900 = false;
        Revealed2200 = false;
    }

    // Step 3 (typically triggered automatically at 22:00 via OnTick)
    public void MannyMeetup()
    {
        if (Stage != 2)
            return;

        // If you ever call this manually, ensure it doesn't double-trigger.
        if (Revealed2200)
            return;

        Revealed2200 = true;
        DoMannyMeetupReveal();
    }

    private void DoMannyMeetupReveal()
    {
        if (Stage != 2)
            return;

        _waitForEmployeeEntry.Complete();

        Stage = 3;
        _mannyMeetupEntry.Begin();

        // Clear, non-accusatory reveal.
        Agent28.Instance?.SendTextMessage("Location: the docks. Go now.");

        TeleportMeetupNpcsToDocks();
    }

    // Step 4 (call this when the player actually meets Manny / completes docks interaction)
    public void HireArchie()
    {
        if (Stage != 3)
            return;

        _mannyMeetupEntry.Complete();

        Stage = 4;
        _hireArchieEntry.Begin();
    }

    // Step 5 (call this when Archie is hired)
    public void EquipmentSearch()
    {
        if (Stage != 4)
            return;

        _hireArchieEntry.Complete();

        Stage = 5;
        _equipmentSearchEntry.Begin();
    }

    // Step 6 (final)
    public void FoundEquipment()
    {
        if (Stage != 5)
            return;

        _equipmentSearchEntry.Complete();

        Stage = 6;
        Complete();

        Agent28.SetDefaultDialogueActive();
    }

    private void OnSleepEnd(int minutesSkipped)
    {
        if (Stage != 2)
            return;

        if (!AwaitingWakeup)
            return;

        AwaitingWakeup = false;
        LeadDay = TimeManager.ElapsedDays;

        MelonLogger.Msg($"[Act0] Wakeup detected; scheduling Manny texts for day {LeadDay}.");
    }

    private void OnTick()
    {
        if (Stage != 2)
            return;

        if (LeadDay < 0)
            return;

        // Only run on the day we anchored to wakeup
        if (TimeManager.ElapsedDays != LeadDay)
            return;

        int t = TimeManager.CurrentTime; // e.g. 1900, 2200

        if (!Sent1900 && t >= 1900)
        {
            Sent1900 = true;
            Agent28.Instance?.SendTextMessage(
                "Manny’s set for 22:00. I’ll text you the location at 22:00. Be ready to move."
            );
            return;
        }

        if (Sent1900 && !Revealed2200 && t >= 2200)
        {
            Revealed2200 = true;
            DoMannyMeetupReveal();
        }
    }

    private static void WarpAgent28ToBlackMarket()
    {
        // IMPORTANT: This is an offset from BlackMarketPos (not absolute coords).
        // Tweak as needed for staging.
        Vector3 pos = BlackMarketPos;
        Quaternion rot = Quaternion.Euler(0f, 20f, 0f);

        WarpNpcGameObjectByName("Agent 28", pos, rot, logTag: "[Act0] Warp Agent28 -> BlackMarket");
    }

    private void TeleportMeetupNpcsToDocks()
    {
        Vector3 agentPos = DocksPos + new Vector3(0.6f, 0f, 0.2f);
        Vector3 mannyPos = DocksPos + new Vector3(-0.6f, 0f, -0.2f);
        Quaternion faceRot = Quaternion.Euler(0f, 180f, 0f);

        WarpNpcGameObjectByName("Archie", agentPos, faceRot, logTag: "[Act0] Warp Agent28 -> Docks");
        WarpNpcGameObjectByName("Manny", mannyPos, faceRot, logTag: "[Act0] Warp Manny -> Docks");
    }

    private static void WarpNpcGameObjectByName(string exactName, Vector3 pos, Quaternion rot, string logTag)
    {
        // Diagnostic so we can prove what name is being searched at runtime
        MelonLogger.Msg($"{logTag}: Looking for EXACT GameObject '{exactName}' (must have child 'Avatar')");

        var target = GameObject.Find(exactName);

        if (target == null)
        {
            MelonLogger.Warning($"{logTag} failed: GameObject '{exactName}' not found.");
            return;
        }

        if (!HasAvatarChild(target))
        {
            MelonLogger.Warning($"{logTag} failed: '{exactName}' found ('{target.name}') but has no child named 'Avatar'.");
            return;
        }

        target.transform.position = pos;
        target.transform.rotation = rot;

        MelonLogger.Msg($"{logTag}: Warped '{target.name}' to {pos}.");
    }

    private static bool HasAvatarChild(GameObject root)
    {
        var transforms = root.GetComponentsInChildren<Transform>(true); // includes inactive
        foreach (var t in transforms)
        {
            if (t != null && t.name == "Avatar")
                return true;
        }
        return false;
    }
}
