using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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
using ModLoader;
using Paris.Engine.Messaging;
using System.Linq;
using Paris.Game.HUD;
using Paris.Game;
using System.Reflection;

namespace DebugMod
{
    public class ModEntry
    {
        public ModConfig config;
        public IModHelper Helper;
        public static bool god;
        public static bool ninja;
        public Scene2d lastClearedStage;

        private void Main(IModHelper helper)
        {
            Helper = helper;
            config = helper.Config.LoadConfig<ModConfig>();
            helper.Events.UpdateTicked += Events_UpdateTicked;
            helper.Events.ContextSwitched += Events_ContextSwitched;
            var h = new Harmony("DebugMod");
            h.PatchAll();
        }

        private void Events_ContextSwitched(object sender, ModLoader.Events.ContextSwitchedEventArgs e)
        {
            lastClearedStage = null;
        }

        private bool TryParseKey(string name, out Keys key) => Enum.TryParse<Keys>(name, out key);

        private void Events_UpdateTicked(object sender, ModLoader.Events.UpdateTickEventArgs e)
        {
            if (TryParseKey(config.godKey, out Keys godKey) && InputManager.Singleton.IsKeyJustPressed(godKey))
            {
                god = !god;
                Helper.Console.Announcement($"God mode: {god}");
            }

            if (TryParseKey(config.ninjaKey, out Keys ninjaKey) && InputManager.Singleton.IsKeyJustPressed(ninjaKey))
            {
                ninja = !ninja;
                Helper.Console.Announcement($"Ninja mode: {ninja}");
            }

            if (Scene2d.Active != null)
            {
                if (TryParseKey(config.winKey, out Keys winKey) && InputManager.Singleton.IsKeyJustPressed(winKey))
                    ClearStage();

                if (TryParseKey(config.killKey, out Keys killKey) && InputManager.Singleton.IsKeyJustPressed(killKey))
                    KillAllEnemies();

                if (TryParseKey(config.whirlKey, out Keys whirlKey) && InputManager.Singleton.IsKeyJustPressed(whirlKey))
                {
                    Helper.Console.Announcement("Whirwind");
                    ApplyPlayerCheat((g,p) => p.AddStatusEffect(Player.StatusEffectTypes.Whirlwind, config.whirlwindDuration));
                }

                if (TryParseKey(config.lifeKey, out Keys lifeKey) && InputManager.Singleton.IsKeyJustPressed(lifeKey))
                {
                    Helper.Console.Announcement("Lifes + 1");
                    ApplyPlayerCheat((g,p) => g.Lives++);
                }

                if (TryParseKey(config.healthKey, out Keys healthKey) && InputManager.Singleton.IsKeyJustPressed(healthKey))
                {
                    Helper.Console.Announcement("Healing");
                    ApplyPlayerCheat((g, p) => g.HP = g.MaxHP);
                }

                bool add = InputManager.Singleton.IsKeyPressed(Keys.RightShift);
                if (TryParseKey(config.switchKey, out Keys switchKey) && InputManager.Singleton.IsKeyJustPressed(switchKey))
                    SwitchCharacter(255, add);
                else if (InputManager.Singleton.IsKeyJustPressed(Keys.D1))
                    SwitchCharacter(0, add);
                else if (InputManager.Singleton.IsKeyJustPressed(Keys.D2))
                    SwitchCharacter(1, add);
                else if (InputManager.Singleton.IsKeyJustPressed(Keys.D3))
                    SwitchCharacter(2, add);
                else if (InputManager.Singleton.IsKeyJustPressed(Keys.D4))
                    SwitchCharacter(3, add);
                else if (InputManager.Singleton.IsKeyJustPressed(Keys.D5))
                    SwitchCharacter(4, add);
                else if (InputManager.Singleton.IsKeyJustPressed(Keys.D6))
                    SwitchCharacter(5, add);
                else if (InputManager.Singleton.IsKeyJustPressed(Keys.D7))
                    SwitchCharacter(6, add);
            }
        }

        private void ApplyPlayerCheat(Action<GamePlayerInfo, Player> cheat)
        {
            PlayerInfo localHostPlayer = PlayerManager.Singleton.LocalHostPlayer;
            List<GameObject2d> players = Scene2d.Active.GetGameObjectsOfType<Player>();
            for (int i = 0; i < players.Count; i++)
            {
                GamePlayerInfo p = (players[i] as Player).GamePInfo;
                if (p.PlayerID != localHostPlayer.PlayerID)
                    continue;

                cheat(((GamePlayerInfo)PlayerManager.Singleton.Players[i]), players[i] as Player);
            }
        }

        private void SwitchCharacter(byte v, bool add = false)
        {
            Helper.Console.Announcement($"switching characters");
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
                    return;
                else if (sc == v)
                {
                    Helper.Console.Warn($"player already using character {p.SelectedCharacter}");
                    return;
                }
                else
                    sc = v;

                Helper.Console.Info($"player {p.PlayerID}: {p.SelectedCharacter}");
                ((GamePlayerInfo)PlayerManager.Singleton.Players[i]).SelectedCharacter = sc;
                Vector3 pos = players[i].Position;
                if (!add)
                    Scene2d.Active.RemovePlayer(p);
                Scene2d.Active.SpawnPlayer(p, false, pos, true);
            }
        }

        private void ClearStage()
        {
            if (Scene2d.Active != null && Scene2d.Active != lastClearedStage && Scene2d.Active.Players != null && Scene2d.Active.Players.Keys.Where(p => p.IsLocalPlayer).First() is PlayerInfo playerInfo && Scene2d.Active.Players[playerInfo] is ParisObject player)
            {
                MessageSystem.Singleton.SendMessage(player, 66, GameInfo.Singleton.CreateLevelCompleteInfo(), true, true);
                (Scene2d.Active.HUD as MainHUD).ShowCompletionHUD();

                Helper.Console.Announcement($"Clearing Stage");
                lastClearedStage = Scene2d.Active;
            }
        }

        private void KillAllEnemies()
        {
            Helper.Console.Announcement($"Killing");

            if (Scene2d.Active != null)
                foreach (Enemy enemy in Scene2d.Active.GetGameObjectsOfType<Enemy>().Where(enemy => enemy.Active && enemy.IsVisibleOnScreen).ToList())
                    enemy.Kill();
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

    }
}
