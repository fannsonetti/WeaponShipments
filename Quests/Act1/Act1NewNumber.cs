using MelonLoader;
using S1API.GameTime;
using S1API.Quests;
using UnityEngine;
using UnityEngine.AI;
using WeaponShipments.Data;
using WeaponShipments.NPCs;
using System.Collections;
using System.Reflection;
using S1API.Entities;

namespace WeaponShipments.Quests
{
    public class Act1NewNumberQuest : Quest
    {
        protected override string Title => "New Number, New Problems";
        protected override string Description => "A new opportunity has surfaced.";
        protected override bool AutoBegin => false;
        protected override Sprite? QuestIcon => WeaponShipments.Utils.QuestIconLoader.Load("quest_new_number.png");

        private const int RequiredLevel = 10;
        private const float PollInterval = 2f;
        private const float FallbackDelaySeconds = 20f;

        private WSSaveData.SavedNewNumberQuest Saved => WSSaveData.Instance?.NewNumberQuest;

        private int _stageFallback = 0;
        private bool _sleepHookAttached = false;

        private int Stage
        {
            get => Saved != null ? Saved.Stage : _stageFallback;
            set
            {
                if (Saved != null) Saved.Stage = value;
                _stageFallback = value;
            }
        }

        private QuestEntry _agentMeetupEntry;
        private QuestEntry _goToSleepEntry;
        private QuestEntry _warehouseTalkEntry;

        private static readonly Vector3 BlackMarketPos =
            new Vector3(-44.3456f, -2.1f, 23.4812f);

        private bool _loadedFromSave = false;

        public static void StartWhenReady()
        {
            MelonCoroutines.Start(StartWhenReadyRoutine());
        }

        private static IEnumerator StartWhenReadyRoutine()
        {
            float fallbackTimer = FallbackDelaySeconds;

            while (fallbackTimer > 0f)
            {
                var persistent = WSPersistent.Instance?.Data;
                if (persistent != null && persistent.Act0Started)
                {
                    MelonLogger.Msg("[Act1] Quest already started; skipping.");
                    yield break;
                }

                int level = TryGetPlayerLevel();
                if (level >= RequiredLevel)
                {
                    MelonLogger.Msg($"[Act1] Player level {level} >= {RequiredLevel}; starting Quest 1.");
                    StartQuest1();
                    yield break;
                }

                fallbackTimer -= PollInterval;
                yield return new WaitForSeconds(PollInterval);
            }

            var persistentFinal = WSPersistent.Instance?.Data;
            if (persistentFinal == null)
            {
                MelonLogger.Warning("[Act1] Persistent save not ready; aborting.");
                yield break;
            }

            if (persistentFinal.Act0Started)
            {
                MelonLogger.Msg("[Act1] Already started; skipping.");
                yield break;
            }

            MelonLogger.Msg("[Act1] Fallback delay reached; starting Quest 1.");
            StartQuest1();
        }

        private static void StartQuest1()
        {
            var persistent = WSPersistent.Instance?.Data;
            if (persistent == null)
                return;

            persistent.Act0Started = true;
            UnknownContact.Instance?.SendIntro();
        }

        private static int TryGetPlayerLevel()
        {
            try
            {
                var player = Player.Local;
                if (player == null)
                    return -1;

                var leveling = Type.GetType("S1API.Leveling.LevelManager, S1API.Forked");
                if (leveling != null)
                {
                    var method = leveling.GetMethod("GetLevel", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Player) }, null);
                    if (method != null)
                    {
                        var result = method.Invoke(null, new object[] { player });
                        if (result is int l)
                            return l;
                    }
                }

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            _loadedFromSave = true;

            if (QuestEntries.Count == 0)
                CreateEntries();

            RebindEntriesFromList();
        }

        protected override void OnCreated()
        {
            base.OnCreated();

            if (QuestEntries.Count == 0)
                CreateEntries();

            RebindEntriesFromList();
            AttachSleepHookOnce();

            if (_loadedFromSave || (Saved != null && Saved.Stage > 0))
                MelonCoroutines.Start(ApplyStageSideEffectsNextFrame());
            else
                RestoreStageStateForNewQuestOnly();
        }

        private static readonly Vector3 Agent28WarehousePos = new Vector3(-23.0225f, -5f, 170.31f);

        private void CreateEntries()
        {
            AddEntry("Meet the Unknown Contact.", BlackMarketPos);
            AddEntry("Meet Agent 28 in the black market and complete the deal.", BlackMarketPos);
            AddEntry("Go to sleep.");
            AddEntry("Go to the warehouse and talk to Agent 28.", Agent28WarehousePos);
        }

        private void RebindEntriesFromList()
        {
            if (QuestEntries.Count >= 1) _agentMeetupEntry = QuestEntries[0];
            if (QuestEntries.Count >= 3) _goToSleepEntry = QuestEntries[2];
            if (QuestEntries.Count >= 4) _warehouseTalkEntry = QuestEntries[3];
        }

        private void AttachSleepHookOnce()
        {
            if (_sleepHookAttached) return;
            _sleepHookAttached = true;
            TimeManager.OnSleepEnd += OnSleepEnd;
        }

        private void OnSleepEnd(int minutesSkipped)
        {
            var p = WSPersistent.Instance?.Data;
            if (p != null && p.DealCompleteAwaitingSleep)
            {
                p.DealCompleteAwaitingSleep = false;
                p.AwaitingWarehouseTalk = true;
                Stage = 3;
                _goToSleepEntry?.Complete();
                _warehouseTalkEntry?.Begin();
                MelonCoroutines.Start(WarpAgent28WhenReady());
                Agent28.SetDialogueFromWarehouseState();
                Agent28.Instance?.SendTextMessage("Come talk to me in the warehouse when you're ready.");
                MelonLogger.Msg("[NewNumber] Woke: Agent 28 warp scheduled, warehouse POI set.");
            }
        }

        private void RestoreStageStateForNewQuestOnly()
        {
            if (Stage == 0)
                return;

            if (_agentMeetupEntry == null)
            {
                MelonLogger.Warning("[NewNumber] RestoreStageState: agentMeetupEntry is null.");
                return;
            }

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
                    _agentMeetupEntry?.Complete();
                    if (QuestEntries.Count >= 2) QuestEntries[1].Complete();
                    _goToSleepEntry?.Begin();
                    Agent28.SetDialogueFromWarehouseState();
                    break;

                case 3:
                    Begin();
                    _agentMeetupEntry?.Complete();
                    if (QuestEntries.Count >= 2) QuestEntries[1].Complete();
                    _goToSleepEntry?.Complete();
                    _warehouseTalkEntry?.Begin();
                    WarpAgent28ToWarehouse();
                    Agent28.SetDialogueFromWarehouseState();
                    break;
            }
        }

        private IEnumerator ApplyStageSideEffectsNextFrame()
        {
            yield return null;
            yield return new WaitForSeconds(1f);

            if (_agentMeetupEntry == null)
                yield break;

            switch (Stage)
            {
                case 1:
                    WarpAgent28ToBlackMarket();
                    Agent28.SetMeetupDialogueActive();
                    break;
                case 2:
                    Agent28.SetDialogueFromWarehouseState();
                    break;
                case 3:
                    WarpAgent28ToWarehouse();
                    Agent28.SetDialogueFromWarehouseState();
                    break;
            }
        }

        public void AgentMeetup()
        {
            if (Stage != 0)
                return;

            Begin();
            Stage = 1;
            _agentMeetupEntry?.Begin();

            WarpAgent28ToBlackMarket();
            Agent28.SetMeetupDialogueActive();
        }

        public void CompleteDialogueAndUnlockProperty()
        {
            if (Stage != 1)
                return;

            _agentMeetupEntry?.Complete();
            if (QuestEntries.Count >= 2) QuestEntries[1].Complete();

            Stage = 2;
            var p = WSPersistent.Instance?.Data;
            if (p != null) p.DealCompleteAwaitingSleep = true;

            _goToSleepEntry?.Begin();
            Agent28.SetDialogueFromWarehouseState();
            MelonLogger.Msg("[NewNumber] Deal done. Go to sleep, then warehouse talk.");
        }

        private static readonly Quaternion Agent28WarehouseRot = Quaternion.Euler(0f, 310f, 0f);

        private static IEnumerator WarpAgent28WhenReady()
        {
            while (!WarpNpcOnce("Agent 28", Agent28WarehousePos, Agent28WarehouseRot, "Agent28 warehouse"))
                yield return new WaitForSeconds(1f);
        }

        private static void WarpAgent28ToBlackMarket()
        {
            MelonCoroutines.Start(WarpAgent28ToBlackMarketWhenReady());
        }

        private static IEnumerator WarpAgent28ToBlackMarketWhenReady()
        {
            Vector3 pos = BlackMarketPos;
            Quaternion rot = Quaternion.Euler(0f, 20f, 0f);
            while (!WarpNpcOnce("Agent 28", pos, rot, "Agent28 warped"))
                yield return new WaitForSeconds(1f);
        }

        private static void WarpAgent28ToWarehouse()
        {
            MelonCoroutines.Start(WarpAgent28WhenReady());
        }

        private static bool WarpNpcOnce(string exactName, Vector3 pos, Quaternion rot, string logTag)
        {
            var target = GameObject.Find(exactName);
            if (target == null)
                return false;

            var agent = target.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
                agent.enabled = false;

            target.transform.position = pos;
            target.transform.rotation = rot;
            MelonLogger.Msg($"{logTag}: Warped '{target.name}' to {pos}.");
            return true;
        }

        private static void WarpNpcGameObjectByName(string exactName, Vector3 pos, Quaternion rot, string logTag)
        {
            if (!WarpNpcOnce(exactName, pos, rot, logTag))
                MelonLogger.Warning($"[NewNumber] {logTag}: object '{exactName}' not found.");
        }
    }
}
