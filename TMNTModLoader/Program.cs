using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using Paris;
using Paris.Engine;
using Paris.Engine.Scene;
using Paris.Engine.System;
using Paris.Game.Actor;
using Paris.Game.Damage;
using Paris.Game.Data;
using Paris.Game.System;
using Paris.System.Input;

namespace TMNTModLoader
{
    public class Program
    {
        public static ModHelper helper;
        static void Main(string[] args)
        {
            var h = new Harmony("TMNTLoader");
            h.PatchAll();
            helper = new ModHelper();

            var modPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Mods");
            if (!Directory.Exists(modPath))
            {
                Console.WriteLine("Mods folder not found, creating...");
                Directory.CreateDirectory(modPath);
            }
            foreach(var file in Directory.GetFiles(modPath,"manifest.json", SearchOption.AllDirectories))
            {
                try
                {
                    ModManifest m = JsonConvert.DeserializeObject<ModManifest>(File.ReadAllText(file));
                    if (m == null)
                    {
                        Console.WriteLine($"Error loading manifest file {file}");
                        continue;
                    }
                    Console.WriteLine($"Loading {m.Name}");
                    var dllPath = Path.Combine(Path.GetDirectoryName(file), m.EntryFile);
                    Assembly a = Assembly.UnsafeLoadFrom(dllPath);
                    foreach (var t in a.GetTypes())
                    {
                        var e = t.GetMethod(m.EntryMethod, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                        if (e != null)
                        {
                            //Console.WriteLine($"Found entry method {m.EntryMethod}, invoking");
                            var o = Activator.CreateInstance(t);
                            e.Invoke(o, new object[] { helper });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading mod {file}:\n\n{ex}");
                }
            }
            var mi = AccessTools.Method("Paris.Program:Main");
            if (mi != null)
            {
                mi.Invoke(null, new object[] { new string[] { "-AllowMultiInstance" } });
                Console.WriteLine($"Starting game...");

            }
            else
            {
                Console.WriteLine($"Unable to start game");
            }
            Console.ReadLine();
        }

        [HarmonyPatch(typeof(DamageInfoEx), nameof(DamageInfoEx.GetDamage))]
        static class DamageInfoEx_GetDamage_Patch
        {
            public static bool Prefix()
            {
                return true;
            }
        }
    }
}
