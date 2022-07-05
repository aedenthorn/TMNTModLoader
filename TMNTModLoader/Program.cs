using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace TMNTModLoader
{
    public class Program
    {
        public static ModHelper helper;
        static void Main(string[] args)
        {
            helper = new ModHelper();

            var modPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Mods");
            if (!Directory.Exists(modPath))
            {
                Console.WriteLine("Mods folder not found, creating...");
                Directory.CreateDirectory(modPath);
                return;
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
                    Assembly a = Assembly.LoadFile(dllPath);
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
    }
}
