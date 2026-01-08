using CustomNPCTest.NPCs;
using MelonLoader;
using S1API.GameTime;
using S1API.Quests;
using S1API.Saveables;
using UnityEngine;
using UnityEngine.AI;
using WeaponShipments.Data;
using WeaponShipments.NPCs;

namespace WeaponShipments.Quests
{
    public class Act0ContactQuest : Quest
    {
        protected override string Title => "Act 0 — Contact";
        protected override string Description => "A new opportunity has surfaced.";
        protected override bool AutoBegin => false;

        private WeaponShipmentsSaveData.SavedAct0ContactQuest Saved => WeaponShipmentsSaveData.Instance?.Act0Contact;

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

        private bool _loadedFromSave = false;

        protected override void OnLoaded()
        {
            base.OnLoaded();

            _loadedFromSave = true;

            if (QuestEntries.Count == 0)
            {
                CreateEntries();
            }

            RebindEntriesFromList();
        }

        protected override void OnCreated()
        {
            base.OnCreated();

            if (QuestEntries.Count == 0)
            {
                CreateEntries();
            }

            RebindEntriesFromList();

            AttachTimeHooksOnce();

            if (_loadedFromSave)
            {
                MelonCoroutines.Start(ApplyStageSideEffectsNextFrame());
            }
            else
            {
                RestoreStageStateForNewQuestOnly();
            }
        }

        private void CreateEntries()
        {
            AddEntry("Meet the Unknown Contact.", BlackMarketPos);
            AddEntry("Wait for Agent 28 to find an employee");
            AddEntry("Meet up with Manny", DocksPos);
            AddEntry("Hire Archie");
            AddEntry("Search for equipment", EquipmentPos);
        }

        private void RebindEntriesFromList()
        {
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

        private void RestoreStageStateForNewQuestOnly()
        {
            if (Stage == 0)
                return;

            if (_agentMeetupEntry == null || _waitForEmployeeEntry == null || _mannyMeetupEntry == null ||
                _hireArchieEntry == null || _equipmentSearchEntry == null)
            {
                MelonLogger.Warning("[Act0] RestoreStageStateForNewQuestOnly: one or more QuestEntry references are null.");
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

        private System.Collections.IEnumerator ApplyStageSideEffectsNextFrame()
        {
            yield return null;

            if (_agentMeetupEntry == null || _waitForEmployeeEntry == null || _mannyMeetupEntry == null ||
                _hireArchieEntry == null || _equipmentSearchEntry == null)
            {
                MelonLogger.Warning("[Act0] ApplyStageSideEffectsNextFrame: one or more QuestEntry references are null.");
                yield break;
            }

            switch (Stage)
            {
                case 0:
                    break;

                case 1:
                    WarpAgent28ToBlackMarket();
                    Agent28.SetMeetupDialogueActive();
                    break;

                case 2:
                    break;

                case 3:
                    TeleportMeetupNpcsToDocks();
                    break;

                case 4:
                case 5:
                    break;

                case 6:
                    Agent28.SetDefaultDialogueActive();
                    break;
            }
        }

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

        public void WaitForEmployee()
        {
            if (Stage != 1)
                return;

            _agentMeetupEntry.Complete();

            Stage = 2;
            _waitForEmployeeEntry.Begin();

            Agent28.Instance?.SendTextMessage("I'll ask around for employees.");

            AwaitingWakeup = true;
            LeadDay = -1;
            Sent1900 = false;
            Revealed2200 = false;
        }

        public void MannyMeetup()
        {
            if (Stage != 2)
                return;

            if (Revealed2200)
                return;

            Revealed2200 = true;
            DoMannyMeetupReveal();
            Manny.SetMeetupDialogueActive();
        }

        private void DoMannyMeetupReveal()
        {
            if (Stage != 2)
                return;

            _waitForEmployeeEntry.Complete();

            Stage = 3;
            _mannyMeetupEntry.Begin();

            Agent28.Instance?.SendTextMessage("Meet him behind Randy's shop.");
            Manny.SetMeetupDialogueActive();
            TeleportMeetupNpcsToDocks();
        }

        public void HireArchie()
        {
            if (Stage != 3)
                return;

            _mannyMeetupEntry.Complete();

            Stage = 4;
            _hireArchieEntry.Begin();

            Archie.SetMeetupDialogueActive();
        }

        public void EquipmentSearch()
        {
            if (Stage != 4)
                return;

            _hireArchieEntry.Complete();

            Stage = 5;
            _equipmentSearchEntry.Begin();
        }

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

            if (TimeManager.ElapsedDays != LeadDay)
                return;

            int t = TimeManager.CurrentTime;

            if (!Sent1900 && t >= 1900)
            {
                Sent1900 = true;
                Agent28.Instance?.SendTextMessage(
                    "Manny knows someone looking for work. I'll text you the location at 22:00. Be ready."
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
            Vector3 pos = BlackMarketPos;
            Quaternion rot = Quaternion.Euler(0f, 20f, 0f);

            WarpNpcGameObjectByName("Agent 28", pos, rot, logTag: "Agent28 warped");
        }

        private void TeleportMeetupNpcsToDocks()
        {
            Vector3 archiePos = new Vector3(-97.7585f, -2.5f, -37.1382f);
            Vector3 mannyPos = new Vector3(-98.5142f, -2.5f, -36.5701f);
            Vector3 igorPos = new Vector3(-98.8593f, -2.5f, -36f);
            Quaternion faceRot = Quaternion.Euler(0f, 200f, 0f);
            Quaternion archieRot = Quaternion.Euler(0f, 240f, 0f);

            WarpNpcGameObjectByName("ArchieWS", archiePos, archieRot, logTag: "Archie warped");
            WarpNpcGameObjectByName("MannyWS", mannyPos, faceRot, logTag: "Manny warped");
            WarpNpcGameObjectByName("IgorWS", igorPos, faceRot, logTag: "Igor warped");
        }

        private static void WarpNpcGameObjectByName(string exactName, Vector3 pos, Quaternion rot, string logTag)
        {
            var target = GameObject.Find(exactName);

            var agent = target.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
                agent.enabled = false;

            target.transform.position = pos;
            target.transform.rotation = rot;

            MelonLogger.Msg($"{logTag}: Warped '{target.name}' to {pos}.");
        }
    }
}
