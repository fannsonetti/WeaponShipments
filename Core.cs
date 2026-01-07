using MelonLoader;
using S1API.Entities;
using S1API.Quests;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using WeaponShipments.Services;
using WeaponShipments.Utils;

[assembly: MelonInfo(typeof(WeaponShipments.Core), Constants.MOD_NAME, Constants.MOD_VERSION, Constants.MOD_AUTHOR)]
[assembly: MelonGame(Constants.Game.GAME_STUDIO, Constants.Game.GAME_NAME)]

namespace WeaponShipments
{
    public class Core : MelonMod
    {
        private static bool _bunkerRequested;
        private static bool _act0Hooked;

        public override void OnInitializeMelon()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            MelonLogger.Msg("WeaponShipments initialized!");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_bunkerRequested)
                return;

            if (scene.name == "Main")
            {
                _bunkerRequested = true;
                WarehouseLoader.LoadWarehouseAdditiveOnce();
                GarageLoader.LoadGarageAdditiveOnce();
                TerrainLoader.LoadTerrainAdditiveOnce();
            }
        }
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "Main")
                return;

            if (_act0Hooked)
                return;

            _act0Hooked = true;

            MelonLogger.Msg("[Act0] Main scene loaded. Deferring quest init until S1API is ready...");
            MelonCoroutines.Start(InitAct0WhenReady());
        }

        private static IEnumerator InitAct0WhenReady()
        {
            float timeout = 30f;
            while (Player.Local == null && timeout > 0f)
            {
                timeout -= UnityEngine.Time.deltaTime;
                yield return null;
            }

            if (Player.Local == null)
            {
                MelonLogger.Warning("[Act0] Player.Local never became available; cannot init Act0 quests.");
                yield break;
            }

            yield return null;

            MelonLogger.Msg("[Act0] S1API appears ready. Creating quest + starting delayed Act0.");

            Act0ContactQuestManager.Initialize();

            Act0DelayedStarter.Start();
        }

        public override void OnApplicationQuit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
