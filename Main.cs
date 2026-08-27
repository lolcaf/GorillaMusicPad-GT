using GorillaMapTeleporter.Utilities;
using GorillaMusicPad.Classes;
using GorillaMusicPad.MonoBehaviors;
using GorillaMusicPad.Patches;
using GorillaMusicPad.Utilities;
using UnityEngine;

namespace GorillaMusicPad;

public class Main : MonoBehaviour
{
    public static Main? Instance;

    // This is a log, used for writing information for debug purposes
    public static GorillaLog Log = new GorillaLog();

    // Mod variables
    private GameObject theMenu;

    public AudioSource musicPlayer;

    // This is called when the mod initializes
    private void Start()
    {
        Instance = this;

        HarmonyPatches.Patch(); // Patch the game

        // Stops the OnPlayerSpawned method from creating unhandled errors, so other mods
        // can still work even if yours breaks.
        GorillaTagger.OnPlayerSpawned(() => MethodUtilities.Attempt(OnPlayerSpawned));

        musicPlayer = new GameObject("GorillaMusicPad-MusicPlayer").AddComponent<AudioSource>();
        musicPlayer.loop = true;
        musicPlayer.volume = 0.2f;
        DontDestroyOnLoad(musicPlayer.gameObject);
    }

    // This is called when everything is ready in the game before the gorilla is spawned into the world.
    private void OnPlayerSpawned()
    {
        LoadAssetBundle();
    }

    private void LoadAssetBundle()
    {
        GameObject menu = AssetBundleUtilities.Load("GorillaMusicPad.Resources.gorillamusicpad", "MusicPad");
        Instantiate(menu).AddComponent<MenuManager>();
    }
}
