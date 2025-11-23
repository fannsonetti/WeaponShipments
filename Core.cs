using MelonLoader;
using WeaponShipments.Services;
using WeaponShipments.Utils;
using WeaponShipments.UI;

[assembly: MelonInfo(typeof(WeaponShipments.Core), Constants.MOD_NAME, Constants.MOD_VERSION, Constants.MOD_AUTHOR)]
[assembly: MelonGame(Constants.Game.GAME_STUDIO, Constants.Game.GAME_NAME)]

namespace WeaponShipments
{
    public class Core : MelonMod
    {
        public static Core? Instance { get; private set; }

        public override void OnInitializeMelon()
        {
            var _ = WeaponShipments.Data.WeaponShipmentsPrefs.MaxSupplies.Value;

            MelonLogger.Msg("WeaponShipments initialized!");
        }

        public override void OnApplicationQuit()
        {
            Instance = null;
        }
    }
}