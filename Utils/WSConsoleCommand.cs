using System;
using System.Collections.Generic;
using System.Globalization;
using MelonLoader;
using S1API.Console;
using WeaponShipments.Data;

namespace WeaponShipments.Utils
{
    /// <summary>
    /// In-game console command via S1API. Type "ws GarageStock 12" etc. in the game console.
    /// </summary>
    public class WSConsoleCommand : BaseConsoleCommand
    {
        public override string CommandWord => "ws";
        public override string CommandDescription => "WeaponShipments: set stock/supplies/stats, property owned, or open debug menu.";
        public override string ExampleUsage => "ws GarageStock 12 | ws SetOwned Warehouse | ws menu";

        public override void ExecuteCommand(List<string> args)
        {
            try
            {
                if (args == null || args.Count < 1)
                {
                    PrintHelp();
                    return;
                }

                var key = args[0];

                if (key.Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp();
                    return;
                }

                if (key.Equals("menu", StringComparison.OrdinalIgnoreCase))
                {
                    WSDebugMenu.Visible = !WSDebugMenu.Visible;
                    MelonLogger.Msg("[WS] Debug menu {0}.", WSDebugMenu.Visible ? "open" : "closed");
                    return;
                }

                if (key.Equals("SetOwned", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Count < 2) { MelonLogger.Warning("[WS] SetOwned requires property name."); return; }
                    if (Enum.TryParse<BusinessState.PropertyType>(args[1], true, out var p))
                    {
                        BusinessState.SetPropertyOwned(p, true);
                    }
                    else
                        MelonLogger.Warning("[WS] Invalid property. Use Warehouse, Garage, or Bunker.");
                    return;
                }

                if (key.Equals("ActiveProperty", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Count < 2) { MelonLogger.Warning("[WS] ActiveProperty requires value."); return; }
                    if (Enum.TryParse<BusinessState.PropertyType>(args[1], true, out var p))
                    {
                        BusinessState.SetActiveProperty(p);
                        MelonLogger.Msg("[WS] ActiveProperty = {0}", p);
                    }
                    else
                        MelonLogger.Warning("[WS] Invalid property. Use Warehouse, Garage, or Bunker.");
                    return;
                }

                if (args.Count < 2) { PrintHelp(); return; }
                var valueStr = args[1];

                if (!float.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    MelonLogger.Warning("[WS] Invalid numeric value: {0}", valueStr);
                    return;
                }

                var k = key.ToLowerInvariant();

                var pt = k switch
                {
                    "garagestock" => (BusinessState.PropertyType.Garage, true),
                    "garagesupplies" => (BusinessState.PropertyType.Garage, false),
                    "warehousestock" => (BusinessState.PropertyType.Warehouse, true),
                    "warehousesupplies" => (BusinessState.PropertyType.Warehouse, false),
                    "bunkerstock" => (BusinessState.PropertyType.Bunker, true),
                    "bunkersupplies" => (BusinessState.PropertyType.Bunker, false),
                    _ => ((BusinessState.PropertyType?)null, false)
                };

                if (pt.Item1 != null)
                {
                    if (pt.Item2)
                        BusinessState.SetStockForProperty(pt.Item1.Value, value);
                    else
                        BusinessState.SetSuppliesForProperty(pt.Item1.Value, value);
                    MelonLogger.Msg("[WS] {0} = {1}", key, value);
                    return;
                }

                switch (k)
                {
                    case "totalearnings": BusinessState.SetTotalEarnings(value); MelonLogger.Msg("[WS] TotalEarnings = {0}", value); return;
                    case "totalsalescount": BusinessState.SetTotalSalesCount((int)value); MelonLogger.Msg("[WS] TotalSalesCount = {0}", value); return;
                    case "totalstockproduced": BusinessState.SetTotalStockProduced(value); MelonLogger.Msg("[WS] TotalStockProduced = {0}", value); return;
                    case "resupplyjobsstarted": BusinessState.SetResupplyJobsStarted((int)value); MelonLogger.Msg("[WS] ResupplyJobsStarted = {0}", value); return;
                    case "resupplyjobscompleted": BusinessState.SetResupplyJobsCompleted((int)value); MelonLogger.Msg("[WS] ResupplyJobsCompleted = {0}", value); return;
                    case "hylandsellattempts": BusinessState.SetHylandSellAttempts((int)value); MelonLogger.Msg("[WS] HylandSellAttempts = {0}", value); return;
                    case "hylandsellsuccesses": BusinessState.SetHylandSellSuccesses((int)value); MelonLogger.Msg("[WS] HylandSellSuccesses = {0}", value); return;
                }

                MelonLogger.Warning("[WS] Unknown key: {0}", key);
                PrintHelp();
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WS] Command failed: {0}", ex);
            }
        }

        private static void PrintHelp()
        {
            MelonLogger.Msg("WS commands: ws <Key> <Value>");
            MelonLogger.Msg("  Stock/Supplies: GarageStock, GarageSupplies, WarehouseStock, WarehouseSupplies, BunkerStock, BunkerSupplies");
            MelonLogger.Msg("  Stats: TotalEarnings, TotalSalesCount, TotalStockProduced, ResupplyJobsStarted, ResupplyJobsCompleted, HylandSellAttempts, HylandSellSuccesses");
            MelonLogger.Msg("  SetOwned <Warehouse|Garage|Bunker>, ActiveProperty <Warehouse|Garage|Bunker>");
            MelonLogger.Msg("  menu - toggle debug panel");
        }
    }
}
