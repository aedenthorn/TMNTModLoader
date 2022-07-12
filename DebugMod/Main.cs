using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Paris.Engine;
using Paris.Engine.Scene;
using Paris.Engine.System;
using Paris.Game.Actor;
using Paris.Game.Damage;
using Paris.Game.Data;
using Paris.Game.System;
using Paris.System.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMNTModLoader;

namespace DebugMod
{
    public class ModEntry
    {
        public static ModConfig config;
        private void Main(ModHelper helper)
        {
            LoadConfig();
            var h = new Harmony("DebugMod");
            h.PatchAll();
            Console.WriteLine("Debug Mod Loaded");
        }
        public static void LoadConfig()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "modconfig.json");
            if (!File.Exists(path))
            {
                config = new ModConfig();
            }
            else
            {
                config = JsonConvert.DeserializeObject<ModConfig>(File.ReadAllText(path));
            }
            File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        public static void SaveConfig()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "modconfig.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        [HarmonyPatch(typeof(DamageInfoEx), nameof(DamageInfoEx.GetDamage))]
        static class DamageInfoEx_GetDamage_Patch
        {
            public static bool Prefix(GameObject2d victim, ref float __result)
            {
                if (god && victim is Player && (victim as Player).GamePInfo.PlayerID == PlayerManager.Singleton.LocalHostPlayer.PlayerID)
                {
                    __result = 0;
                    return false;
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(Player), nameof(Player.NinjaPowerStackCount))]
        [HarmonyPatch(MethodType.Getter)]
        static class Player_NinjaPowerStackCount_Patch
        {
            public static bool Prefix(Player __instance, ref int __result)
            {
                if (ninja && __instance is Player && (__instance as Player).GamePInfo.PlayerID == PlayerManager.Singleton.LocalHostPlayer.PlayerID)
                {
                    __result = GameSettings.Singleton.MaxNinjaPowerStacks;
                    return false;
                }
                return true;
            }
        }

        public static bool god;
        public static bool godPressed;

        public static bool ninja;
        public static bool ninjaPressed;

        public static bool switchPressed;

        public static bool healPressed;

        [HarmonyPatch(typeof(InputManagerBase), "Tick")]
        static class InputManager_Tick_Patch
        {
            public static void Prefix(KeyboardState ___keyboardState)
            {
                if (IsKeyDown(___keyboardState, config.godKey))
                {
                    if (!godPressed)
                    {
                        godPressed = true;
                        god = !god;
                        Console.WriteLine($"God mode: {god}");
                    }
                }
                else
                {
                    godPressed = false;
                }
                if (IsKeyDown(___keyboardState, config.ninjaKey))
                {
                    if (!ninjaPressed)
                    {
                        ninjaPressed = true;
                        ninja = !ninja;
                        Console.WriteLine($"Ninja mode: {ninja}");
                    }
                }
                else
                {
                    ninjaPressed = false;
                }

                if(Scene2d.Active != null)
                {
                    bool add = ___keyboardState.IsKeyDown(Keys.RightShift);
                    if (IsKeyDown(___keyboardState, config.healthKey))
                    {
                        if (!healPressed)
                        {
                            healPressed = true;
                            Console.WriteLine("Healing");
                            PlayerInfo localHostPlayer = PlayerManager.Singleton.LocalHostPlayer;
                            List<GameObject2d> players = Scene2d.Active.GetGameObjectsOfType<Player>();
                            for (int i = 0; i < players.Count; i++)
                            {
                                GamePlayerInfo p = (players[i] as Player).GamePInfo;
                                if (p.PlayerID != localHostPlayer.PlayerID)
                                    continue;
                                ((GamePlayerInfo)PlayerManager.Singleton.Players[i]).HP = ((GamePlayerInfo)PlayerManager.Singleton.Players[i]).MaxHP;
                            }
                        }
                    }
                    else
                    {
                        healPressed = false;
                    }
                    if (IsKeyDown(___keyboardState, config.switchKey))
                    {
                        SwitchCharacter(255, add);
                    }
                    else if (___keyboardState.IsKeyDown(Keys.D1))
                    {
                        SwitchCharacter(0, add);
                    }
                    else if (___keyboardState.IsKeyDown(Keys.D2))
                    {
                        SwitchCharacter(1, add);
                    }
                    else if (___keyboardState.IsKeyDown(Keys.D3))
                    {
                        SwitchCharacter(2, add);
                    }
                    else if (___keyboardState.IsKeyDown(Keys.D4))
                    {
                        SwitchCharacter(3, add);
                    }
                    else if (___keyboardState.IsKeyDown(Keys.D5))
                    {
                        SwitchCharacter(4, add);
                    }
                    else if (___keyboardState.IsKeyDown(Keys.D6))
                    {
                        SwitchCharacter(5, add);
                    }
                    else if (___keyboardState.IsKeyDown(Keys.D7))
                    {
                        SwitchCharacter(6, add);
                    }
                    else
                    {
                        switchPressed = false;
                    }
                }
            }

            private static bool IsKeyDown(KeyboardState state, string name)
            {
                if (Enum.TryParse<Keys>(name, out Keys key))
                    return state.IsKeyDown(key);
                return false;
            }

            private static void SwitchCharacter(byte v, bool add = false)
            {
                if (switchPressed)
                    return;
                switchPressed = true;
                Console.WriteLine($"switching characters");
                PlayerInfo localHostPlayer = PlayerManager.Singleton.LocalHostPlayer;
                List<GameObject2d> players = Scene2d.Active.GetGameObjectsOfType<Player>();
                for (int i = 0; i < players.Count; i++)
                {
                    GamePlayerInfo p = (players[i] as Player).GamePInfo;
                    if (p.PlayerID != localHostPlayer.PlayerID)
                        continue;
                    byte sc = p.SelectedCharacter;
                    if (sc == 255)
                        continue;
                    if (v == 255)
                    {
                        sc++;
                        sc %= 7;
                    }
                    else if (!config.enableNumKeys)
                    {
                        return;
                    }
                    else if(sc == v)
                    {
                        Console.WriteLine($"player already using character {p.SelectedCharacter}");
                        return;
                    }
                    else
                    {
                        sc = v;
                    }

                    Console.WriteLine($"player {p.PlayerID}: {p.SelectedCharacter}");
                    ((GamePlayerInfo)PlayerManager.Singleton.Players[i]).SelectedCharacter = sc;
                    Vector3 pos = players[i].Position;
                    if(!add)
                        Scene2d.Active.RemovePlayer(p);
                    Scene2d.Active.SpawnPlayer(p, false, pos, true);
                }
            }
        }
    }
}
