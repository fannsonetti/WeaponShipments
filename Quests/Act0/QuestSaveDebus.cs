using MelonLoader;
using S1API.Quests;
using System.Collections.Generic;
using System.Reflection;

public static class QuestSaveDebug
{
    public static void Dump()
    {
        var quests = (List<Quest>)typeof(QuestManager)
            .GetField("Quests", BindingFlags.NonPublic | BindingFlags.Static)
            .GetValue(null);

        MelonLogger.Msg($"[QuestSaveDebug] Quest count = {quests.Count}");

        var countsByType = new Dictionary<string, int>();

        for (int i = 0; i < quests.Count; i++)
        {
            var q = quests[i];
            if (q == null)
            {
                MelonLogger.Warning($"[QuestSaveDebug] [{i}] NULL quest instance");
                continue;
            }

            string type = q.GetType().FullName ?? q.GetType().Name;
            countsByType[type] = countsByType.TryGetValue(type, out var c) ? c + 1 : 1;

            int entriesCount = -999;
            try { entriesCount = q.QuestEntries?.Count ?? -1; } catch { entriesCount = -2; }

            MelonLogger.Msg(
                $"[QuestSaveDebug] [{i}] {q.GetType().Name} | QuestEntries={entriesCount}"
            );
        }

        foreach (var kv in countsByType)
        {
            if (kv.Value > 1)
                MelonLogger.Warning($"[QuestSaveDebug] DUPLICATE TYPE: {kv.Key} x{kv.Value}");
        }
    }
}
