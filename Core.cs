using MelonLoader;
using WeaponShipments.Services;
using WeaponShipments.Utils;
using WeaponShipments.UI;
using UnityEngine.SceneManagement; // ADDED

[assembly: MelonInfo(typeof(WeaponShipments.Core), Constants.MOD_NAME, Constants.MOD_VERSION, Constants.MOD_AUTHOR)]
[assembly: MelonGame(Constants.Game.GAME_STUDIO, Constants.Game.GAME_NAME)]

namespace WeaponShipments
{
    public class Core : MelonMod
    {
        public static Core? Instance { get; private set; }

        private static bool _bunkerRequested; // ADDED

        public override void OnInitializeMelon()
        {
            Instance = this; // ADDED (your property existed but was never set)

            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            MelonLogger.Msg("[Bunker] Embedded resources in this DLL:");
            foreach (var n in asm.GetManifestResourceNames())
                MelonLogger.Msg(" - " + n);

            // keep your existing prefs warm-up
            var _ = WeaponShipments.Data.WeaponShipmentsPrefs.MaxSupplies.Value;

            // hook scene load
            SceneManager.sceneLoaded += OnSceneLoaded; // ADDED

            MelonLogger.Msg("WeaponShipments initialized!");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) // ADDED
        {
            // Helpful logging so you can discover the real "main" scene name
            MelonLogger.Msg($"[WeaponShipments] Scene loaded: {scene.name}");

            if (_bunkerRequested)
                return;

            // TODO: Replace "Main" with the actual gameplay scene name once you see it in logs.
            if (scene.name == "Main")
            {
                _bunkerRequested = true;

                MelonLogger.Msg("[WeaponShipments] Main scene detected; loading bunker (embedded bundle)...");
                AssetBundleLoader.LoadWarehouseAdditiveOnce(); // ADDED: calls your embedded resource loader
            }
        }

        public override void OnApplicationQuit()
        {
            // unhook to avoid dangling handlers
            SceneManager.sceneLoaded -= OnSceneLoaded; // ADDED

            Instance = null;
        }
    }
}
