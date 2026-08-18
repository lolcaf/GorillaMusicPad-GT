using GorillaMusicPad.Classes;
using GorillaMusicPad.MonoBehaviors;
using GorillaMusicPad.Patches;
using GorillaMusicPad.Utilities;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

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
        StartCoroutine(LoadAssetBundle());
    }

    private IEnumerator LoadAssetBundle()
    {
        string weburl = "https://github.com/lolcaf/GorillaMusicPad-GT/raw/refs/heads/main/Resources/AssetBundle/gorillamusicpad";
        Log.WriteLine("Loading the assetbundle from the web");
        using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(weburl))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
                GameObject go = (GameObject)bundle.LoadAsset(bundle.GetAllAssetNames()[0]);
                if (go != null)
                {
                    theMenu = Instantiate(go);
                    theMenu.AddComponent<MenuManager>();
                    theMenu.transform.localScale = Vector3.one * 1.5f;
                    Log.WriteLine($"the menu is {theMenu}");
                }
                else
                {
                    Log.WriteLine("Asset bundle game object is null");
                }
                bundle.Unload(false);
                yield return new WaitForEndOfFrame();
            }
            else
            {
                Log.WriteLine($"Failed to load asset bundle from web: {www.error}");
            }
            www.Dispose();
        }
    }
}
