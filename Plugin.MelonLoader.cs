using MelonLoader;
using GorillaMusicPad;
using UnityEngine;

// This is used by MelonLoader to initialize your mod. Please put all of your mod code in Main.cs.

[assembly: MelonInfo(typeof(PluginMelonLoader), GorillaMusicPad.Constants.Name, GorillaMusicPad.Constants.Version, GorillaMusicPad.Constants.Author)]
[assembly: MelonGame("Another Axiom", "Gorilla Tag")]
[assembly: HarmonyDontPatchAll]

namespace GorillaMusicPad;

public class PluginMelonLoader : MelonMod
{
    public override void OnLateInitializeMelon()
    {
        GameObject obj = new GameObject(Constants.Guid);
        obj.AddComponent<Main>();
        Object.DontDestroyOnLoad(obj);
    }
}
