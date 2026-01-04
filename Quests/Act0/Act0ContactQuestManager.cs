using S1API.Quests;

public static class Act0ContactQuestManager
{
    public static Act0ContactQuest? Quest;

    public static void Initialize()
    {
        Quest = (Act0ContactQuest)
            QuestManager.CreateQuest<Act0ContactQuest>("ws_act0_contact");
    }

    public static void StartQuest()
    {
        Quest?.StartAct();
    }

    public static void MetAgent()
    {
        Quest?.OnMetAgent();
    }
}
