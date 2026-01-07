using S1API.Quests;
using MelonLoader;
using System.Collections.Generic;
using System.Reflection;

public static class Act0ContactQuestManager
{
    private const string QUEST_GUID = "ws_act0_contact";
    private const string QUEST_NAME = "Act 0 — Contact"; // must match Act0ContactQuest.Title exactly

    private static Act0ContactQuest? _cachedQuest;

    public static Act0ContactQuest? Quest => GetOrCreate();

    public static void Initialize()
    {
        _ = GetOrCreate();
    }

    private static Act0ContactQuest? GetOrCreate()
    {
        // If cached, make sure it is still registered
        if (_cachedQuest != null)
        {
            if (QuestManagerQuests.Contains(_cachedQuest))
                return _cachedQuest;

            _cachedQuest = null;
        }

        // Try to find existing quest by name first
        var questByName = QuestManager.GetQuestByName(QUEST_NAME);
        if (questByName is Act0ContactQuest foundByName)
        {
            _cachedQuest = foundByName;
            return _cachedQuest;
        }

        // Fallback: linear scan (critical to prevent duplicates if name lookup fails)
        for (int i = 0; i < QuestManagerQuests.Count; i++)
        {
            if (QuestManagerQuests[i] is Act0ContactQuest q)
            {
                _cachedQuest = q;
                return _cachedQuest;
            }
        }

        // Create only if not found
        var created = QuestManager.CreateQuest<Act0ContactQuest>(QUEST_GUID);
        if (created is Act0ContactQuest act0)
        {
            _cachedQuest = act0;
            return _cachedQuest;
        }

        MelonLogger.Error("[Act0ContactQuestManager] Failed to create Act0ContactQuest - wrong type returned");
        return null;
    }

    public static void ClearCache() => _cachedQuest = null;
    public static void AgentMeetup() => Quest?.AgentMeetup();
    public static void WaitForEmployee() => Quest?.WaitForEmployee();
    public static void MannyMeetup() => Quest?.MannyMeetup();
    public static void HireArchie() => Quest?.HireArchie();
    public static void EquipmentSearch() => Quest?.EquipmentSearch();
    public static void FoundEquipment() => Quest?.FoundEquipment();

    private static List<Quest> QuestManagerQuests => (List<Quest>)typeof(QuestManager)
        .GetField("Quests", BindingFlags.NonPublic | BindingFlags.Static)
        .GetValue(null);
}
