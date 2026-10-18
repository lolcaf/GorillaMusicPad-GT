using GorillaMusicPad.Classes;
using GorillaMusicPad.MonoBehaviors;
using GorillaMusicPad.Utilities;
using UnityEngine;

namespace GorillaMusicPad;

public class Main : MonoBehaviour
{
    public static Main? Instance;

    // Mod variables
    public AudioSource musicPlayer;

    // This is called when the mod initializes
    private void Start()
    {
        Instance = this;

        // Stops the OnPlayerSpawned method from creating unhandled errors, so other mods
        // can still work even if yours breaks.
        GorillaTagger.OnPlayerSpawned(OnPlayerSpawned);

        musicPlayer = new GameObject("MusicPlayer").AddComponent<AudioSource>();
        musicPlayer.loop = true;
        musicPlayer.volume = 0.2f;
        DontDestroyOnLoad(musicPlayer.gameObject);
    }

    // This is called when everything is ready in the game before the gorilla is spawned into the world.
    private void OnPlayerSpawned()
    {
        LoadAssetBundle();
    }

    private void OnApplicationQuit()
    {
        HarmonyPatches.Unpatch();
    }

    private void LoadAssetBundle()
    {
        GameObject menu = AssetBundleUtilities.Load("GorillaMusicPad.Resources.gorillamusicpad", "MusicPad");
        Instantiate(menu).AddComponent<MenuManager>();
    }

    public void SelectSong(AudioClip clip)
    {
        musicPlayer.clip = clip;
        MenuManager.MainScreen.songNameText.text = clip.name;
    }
}
