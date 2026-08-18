using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GorillaMusicPad;

// This is used by BepInEx to initialize your mod. Please put all of your mod code in Main.cs.

[BepInPlugin(Constants.Guid, Constants.Name, Constants.Version)]
public class PluginBepInEx : BaseUnityPlugin
{
    private void Start()
    {
        GameObject obj = new GameObject(Constants.Guid);
        obj.AddComponent<Main>();
        DontDestroyOnLoad(obj);
    }
}
