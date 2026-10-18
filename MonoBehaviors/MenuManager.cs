using GorillaMusicPad.Classes;
using GorillaNetworking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR;
using Valve.VR;

namespace GorillaMusicPad.MonoBehaviors;

public class MenuManager : MonoBehaviour // this will be attached to the menu
{
    public GameObject offsetGO;

    private List<AudioClip> songs = new List<AudioClip>();

    public static MenuManager instance;

    public bool menuOpen = false;

    private float menuButtonCooldown = 0;

    private GameObject musicParticles;

    private GameObject background;

    private static MenuTheme[] menuThemes =
    [
        new MenuTheme
        {
            name = "Default",
            backColor = new Color(0.5f, 0.7f, 1f),
            buttonColor = new Color(0.25f, 0.5f, 1f)
        },
        new MenuTheme
        {
            name = "Dark",
            backColor = new Color(0.5f, 0.5f, 0.5f),
            buttonColor = new Color(0.25f, 0.25f, 0.25f)
        },
        new MenuTheme
        {
            name = "Green",
            backColor = new Color(0.5f, 1f, 0.5f),
            buttonColor = new Color(0f, 0.75f, 0f)
        },
        new MenuTheme
        {
            name = "Orange",
            backColor = new Color(1f, 0.9f, 0.55f),
            buttonColor = new Color(1f, 0.65f, 0f)
        },
        new MenuTheme
        {
            name = "Red",
            backColor = new Color(1f, 0.4f, 0.4f),
            buttonColor = new Color(1f, 0f, 0f)
        },
        new MenuTheme
        {
            name = "Pink",
            backColor = new Color(1f, 0.6f, 1f),
            buttonColor = new Color(1f, 0f, 1f)
        },
        new MenuTheme
        {
            name = "Lavendar",
            backColor = new Color(0.7f, 0.6f, 1f),
            buttonColor = new Color(0.5f, 0.2f, 1f)
        },
        new MenuTheme
        {
            name = "Cyan",
            backColor = new Color(0.4f, 1f, 1f),
            buttonColor = new Color(0f, 0.7f, 1f)
        },
        new MenuTheme
        {
            name = "Purple",
            backColor = new Color(0.5f, 0.4f, 1f),
            buttonColor = new Color(0.4f, 0f, 1f)
        },
    ];

    private int currentThemeIndex = 0;

    public MenuTheme currentTheme
    {
        get
        {
            return menuThemes[currentThemeIndex];
        }
    }

    public static bool showRoomCode = true;

    private static PressableButton pauseButton;

    private static readonly Dictionary<string, AudioType> audioTypes = new Dictionary<string, AudioType> // supported audio types
    {
        { ".mp3",  AudioType.MPEG },
        { ".wav",  AudioType.WAV },
        { ".ogg",  AudioType.OGGVORBIS },
        { ".aiff", AudioType.AIFF },
        { ".aif",  AudioType.AIFF },
    };

    private void Start()
    {
        instance = this;

        offsetGO = transform.Find("Offset").gameObject;
        offsetGO.transform.Find("VersionText").GetComponent<TextMeshPro>().text = "Version: " + Constants.Version;
        offsetGO.transform.localScale = Vector3.one * 1.5f; // fix the offset because I dont feel like rebuilding the asset bundle
        offsetGO.transform.localPosition = new Vector3(-0.2f, 0.15f, 0.15f);

        background = offsetGO.transform.Find("Back").gameObject;

        MainScreen.go = offsetGO.transform.Find("MainScreen").gameObject;
        NoSongsScreen.go = offsetGO.transform.Find("NoSongsScreen").gameObject;
        SettingsScreen.go = offsetGO.transform.Find("SettingScreen").gameObject;
        RoomScreen.go = offsetGO.transform.Find("RoomScreen").gameObject;
        SongListScreen.go = offsetGO.transform.Find("SongListScreen").gameObject;
        TimeScreen.go = offsetGO.transform.Find("TimeScreen").gameObject;

        musicParticles = offsetGO.transform.Find("Particles").gameObject;
        MainScreen.songNameText = MainScreen.go.transform.Find("SongName").gameObject.GetComponent<TextMeshPro>();
        MainScreen.volumeText = MainScreen.go.transform.Find("VolumeText").gameObject.GetComponent<TextMeshPro>();
        RoomScreen.roomCode = RoomScreen.go.transform.Find("RoomCode").GetComponent<TextMeshPro>();
        SongListScreen.Init();

        // setup the buttons
        NoSongsScreen.go.transform.Find("DownloadSongs").gameObject.AddComponent<PressableButton>().buttonPressed += NoSongsScreen.DownloadSongs;
        NoSongsScreen.go.transform.Find("OpenFolder").gameObject.AddComponent<PressableButton>().buttonPressed += NoSongsScreen.OpenSongFolder;
        NoSongsScreen.go.transform.Find("Continue").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.Open;

        pauseButton = MainScreen.go.transform.Find("Pause").AddComponent<PressableButton>();
        pauseButton.buttonPressed += MainScreen.Pause;
        MainScreen.go.transform.Find("Next").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.NextSong;
        MainScreen.go.transform.Find("Previous").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.PreviousSong;
        MainScreen.go.transform.Find("VolumeUp").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.TurnUpVolume;
        MainScreen.go.transform.Find("VolumeDown").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.TurnDownVolume;
        MainScreen.go.transform.Find("SettingsButton").gameObject.AddComponent<PressableButton>().buttonPressed += SettingsScreen.Open;
        MainScreen.go.transform.Find("RoomButton").gameObject.AddComponent<PressableButton>().buttonPressed += RoomScreen.Open;
        MainScreen.go.transform.Find("RefreshButton").gameObject.AddComponent<PressableButton>().buttonPressed += () => StartCoroutine(LoadSongs());
        MainScreen.go.transform.Find("Search").gameObject.AddComponent<PressableButton>().buttonPressed += () => StartCoroutine(SongListScreen.Open());
        MainScreen.go.transform.Find("TimeButton").gameObject.AddComponent<PressableButton>().buttonPressed += TimeScreen.Open;

        SettingsScreen.go.transform.Find("ExitButton").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.Open;
        SettingsScreen.go.transform.Find("SwitchTheme").gameObject.AddComponent<PressableButton>().buttonPressed += SettingsScreen.SwitchTheme;
        SettingsScreen.go.transform.Find("ShowRoomCode").gameObject.AddComponent<PressableButton>().buttonPressed += SettingsScreen.ShowRoomCode;

        RoomScreen.go.transform.Find("ExitButton").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.Open;
        RoomScreen.go.transform.Find("Disconnect").gameObject.AddComponent<PressableButton>().buttonPressed += () => StartCoroutine(RoomScreen.DelayDisconnect());
        RoomScreen.go.transform.Find("JoinPublic").gameObject.AddComponent<PressableButton>().buttonPressed += () => StartCoroutine(RoomScreen.JoinRandomDelay());

        SongListScreen.go.transform.Find("ExitButton").gameObject.AddComponent<PressableButton>().buttonPressed += SongListScreen.Close;
        SongListScreen.go.transform.Find("Previous").gameObject.AddComponent<PressableButton>().buttonPressed += SongListScreen.PrevPage;
        SongListScreen.go.transform.Find("Next").gameObject.AddComponent<PressableButton>().buttonPressed += SongListScreen.NextPage;

        TimeScreen.go.transform.Find("ExitButton").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.Open;
        TimeScreen.go.transform.Find("Morning").gameObject.AddComponent<PressableButton>().buttonPressed += () => TimeScreen.ChangeTime(1);
        TimeScreen.go.transform.Find("Day").gameObject.AddComponent<PressableButton>().buttonPressed += () => TimeScreen.ChangeTime(3);
        TimeScreen.go.transform.Find("Evening").gameObject.AddComponent<PressableButton>().buttonPressed += () => TimeScreen.ChangeTime(7);
        TimeScreen.go.transform.Find("Night").gameObject.AddComponent<PressableButton>().buttonPressed += () => TimeScreen.ChangeTime(0);
        TimeScreen.go.transform.Find("ToggleRain").gameObject.AddComponent<PressableButton>().buttonPressed += TimeScreen.ToggleRain;
        TimeScreen.go.transform.Find("Default").gameObject.AddComponent<PressableButton>().buttonPressed += TimeScreen.ResetTime;

        StartCoroutine(LoadSongs());
    }

    private void Update()
    {
        if (menuOpen)
        {
            transform.position = VRRig.LocalRig.leftHandTransform.position;
            transform.rotation = VRRig.LocalRig.leftHandTransform.rotation;
        }

        if (ControllerInputPoller.instance.leftControllerPrimaryButton && Time.time > menuButtonCooldown)
        {
            menuButtonCooldown = Time.time + 0.4f;
            menuOpen = !menuOpen;

            transform.position = Vector3.zero;

            musicParticles.SetActive(Main.Instance.musicPlayer.isPlaying);

            if (Main.Instance.musicPlayer.clip == null)
                Main.Instance.musicPlayer.clip = songs.FirstOrDefault();
            else
                MainScreen.songNameText.text = Main.Instance.musicPlayer.clip.name;
        }

        if (musicParticles.activeSelf != Main.Instance.musicPlayer.isPlaying)
            musicParticles.SetActive(Main.Instance.musicPlayer.isPlaying);

        if (RoomScreen.go.activeInHierarchy)
        {
            RoomScreen.roomCode.text = (showRoomCode && NetworkSystem.Instance.InRoom)
            ? NetworkSystem.Instance.RoomName
            : "----";
        }

        if (Time.time > PressableButton.time && GetJoystickClick())
        {
            pauseButton.OnButtonPress();
            PressableButton.time = Time.time + 1f;
        }
    }

    public static class MainScreen
    {
        public static GameObject go;

        public static TextMeshPro songNameText;

        public static TextMeshPro volumeText;

        public static void Open()
        {
            go.SetActive(true);
            NoSongsScreen.go.SetActive(false);
            SettingsScreen.go.SetActive(false);
            RoomScreen.go.SetActive(false);
            SongListScreen.go.SetActive(false);
            TimeScreen.go.SetActive(false);
            if (instance.songs.Count > 0)
            {
                if (Main.Instance.musicPlayer.clip == null) Main.Instance.musicPlayer.clip = instance.songs.FirstOrDefault();
                songNameText.text = Main.Instance.musicPlayer.clip.name;
            }
            else
            {
                songNameText.text = "Missing Song";
            }
        }

        public static void NextSong()
        {
            if (instance.songs.Count == 0) return;
            int songIndex = instance.songs.IndexOf(Main.Instance.musicPlayer.clip);
            if (instance.songs.Count > songIndex + 1)
            {
                songIndex++;
                Main.Instance.SelectSong(instance.songs[songIndex]);
            }
        }

        public static void PreviousSong()
        {
            if (instance.songs.Count == 0) return;
            int songIndex = instance.songs.IndexOf(Main.Instance.musicPlayer.clip);
            if (songIndex > 0)
            {
                songIndex--;
                Main.Instance.SelectSong(instance.songs[songIndex]);
            }
        }

        public static void Pause()
        {
            if (Main.Instance.musicPlayer.isPlaying) Main.Instance.musicPlayer.Pause();
            else Main.Instance.musicPlayer.Play();
        }

        public static void TurnUpVolume()
        {
            Main.Instance.musicPlayer.volume += 0.05f;
            volumeText.text = $"{Mathf.Round(Main.Instance.musicPlayer.volume * 100)}%";
        }

        public static void TurnDownVolume()
        {
            Main.Instance.musicPlayer.volume -= 0.05f;
            volumeText.text = $"{Mathf.Round(Main.Instance.musicPlayer.volume * 100)}%";
        }
    }

    private static class NoSongsScreen
    {
        public static GameObject go;

        public static void Open()
        {
            go.SetActive(true);
            MainScreen.go.SetActive(false);
            SettingsScreen.go.SetActive(false);
            RoomScreen.go.SetActive(false);
            SongListScreen.go.SetActive(false);
            TimeScreen.go.SetActive(false);
        }

        public static void DownloadSongs()
        {
            instance.StartCoroutine(DownloadSongsRoutine());
        }

        private static IEnumerator DownloadSongsRoutine()
        {
            string musicPath = Path.Combine(Application.dataPath, "..", "GorillaMusicPad", "Music");
            musicPath = Path.GetFullPath(musicPath);
            string[] files = Directory.GetFiles(musicPath);
            go.SetActive(false);
            if (files.Length == 0)
            {
                NotificationSystem.Send("Downloading... Please Wait");
                instance.StartCoroutine(instance.LoadSongFromWeb(
                    "https://github.com/lolcaf/GorillaMusicPad-GT/raw/refs/heads/main/Resources/ExampleSongs/campfire.mp3",
                    GetAudioType("https://github.com/lolcaf/GorillaMusicPad-GT/raw/refs/heads/main/Resources/ExampleSongs/campfire.mp3"),
                    true,
                    clip =>
                    {
                        if (clip == null) return;
                        instance.songs.Add(clip);
                        Main.Instance.musicPlayer.clip = clip;
                    }
                ));
                yield return new WaitForSeconds(0.2f);
                instance.StartCoroutine(instance.LoadSongFromWeb(
                    "https://github.com/lolcaf/GorillaMusicPad-GT/raw/refs/heads/main/Resources/ExampleSongs/cave-wave.mp3",
                    GetAudioType("https://github.com/lolcaf/GorillaMusicPad-GT/raw/refs/heads/main/Resources/ExampleSongs/cave-wave.mp3"),
                    true,
                    clip =>
                    {
                        if (clip == null) return;
                        instance.songs.Add(clip);
                    }
                ));
                yield return new WaitForSeconds(0.2f);
                instance.StartCoroutine(instance.LoadSongFromWeb(
                    "https://github.com/lolcaf/GorillaMusicPad-GT/raw/refs/heads/main/Resources/ExampleSongs/monke-need-to-swing.mp3",
                    GetAudioType("https://github.com/lolcaf/GorillaMusicPad-GT/raw/refs/heads/main/Resources/ExampleSongs/monke-need-to-swing.mp3"),
                    true,
                    clip =>
                    {
                        if (clip == null) return;
                        instance.songs.Add(clip);
                    }
                ));
            }
            yield return new WaitForSeconds(4);
            MainScreen.Open();
        }

        public static void OpenSongFolder()
        {
            string musicPath = Path.Combine(Application.dataPath, "..", "GorillaMusicPad", "Music");
            musicPath = Path.GetFullPath(musicPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = musicPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }

    private static class SettingsScreen
    {
        public static GameObject go;

        public static void Open()
        {
            go.SetActive(true);
            MainScreen.go.SetActive(false);
            NoSongsScreen.go.SetActive(false);
            RoomScreen.go.SetActive(false);
            SongListScreen.go.SetActive(false);
            TimeScreen.go.SetActive(false);
            go.transform.Find("ShowRoomCode").Find("Text (TMP)").GetComponent<TextMeshPro>().text = $"Show Room Code (<color=blue>{showRoomCode}</color>)";
        }

        public static void SwitchTheme()
        {
            instance.currentThemeIndex++;
            if (menuThemes.Length > instance.currentThemeIndex)
            {
                MenuTheme theme = menuThemes[instance.currentThemeIndex];
                instance.ApplyTheme(theme);
            }
            else
            {
                instance.currentThemeIndex = 0;
                instance.ApplyTheme(menuThemes.FirstOrDefault());
            }
        }

        public static void ShowRoomCode()
        {
            showRoomCode = !showRoomCode;
            go.transform.Find("ShowRoomCode").Find("Text (TMP)").GetComponent<TextMeshPro>().text = $"Show Room Code (<color=blue>{showRoomCode}</color>)";
        }
    }

    private static class RoomScreen
    {
        public static GameObject go;

        public static TextMeshPro roomCode;

        public static void Open()
        {
            go.SetActive(true);
            MainScreen.go.SetActive(false);
            NoSongsScreen.go.SetActive(false);
            SettingsScreen.go.SetActive(false);
            SongListScreen.go.SetActive(false);
            TimeScreen.go.SetActive(false);
        }

        public static IEnumerator DelayDisconnect()
        {
            yield return new WaitForSeconds(1.5f);
            NetworkSystem.Instance.ReturnToSinglePlayer();
        }

        private static void JoinRandom()
        {
            if (NetworkSystem.Instance.InRoom)
            {
                NetworkSystem.Instance.ReturnToSinglePlayer();
                instance.StartCoroutine(JoinRandomDelay());
                return;
            }

            GorillaNetworkJoinTrigger trigger = PhotonNetworkController.Instance.currentJoinTrigger;
            if (trigger == null) trigger = GorillaComputer.instance.GetJoinTriggerForZone("forest");
            if (trigger == null) return;
            PhotonNetworkController.Instance.AttemptToJoinPublicRoom(trigger);
        }

        public static IEnumerator JoinRandomDelay()
        {
            yield return new WaitForSeconds(1.5f);
            JoinRandom();
        }
    }

    public static class SongListScreen
    {
        public static GameObject go;

        private static GameObject templateButton;

        private static int buttonsPerPage = 4;

        private static float spacing = 0.03f;

        private static int currentPage;

        private static List<GameObject> slots = new List<GameObject>();

        private static int PageCount => Mathf.Max(1, Mathf.CeilToInt(instance.songs.Count / (float)buttonsPerPage));

        private static Transform container;

        public static IEnumerator Open()
        {
            MainScreen.go.SetActive(false);
            NoSongsScreen.go.SetActive(false);
            SettingsScreen.go.SetActive(false);
            RoomScreen.go.SetActive(false);
            TimeScreen.go.SetActive(false);
            yield return new WaitForSeconds(0.5f);
            go.SetActive(true);
            LoadButtons();
        }

        public static void Init()
        {
            templateButton = go.transform.Find("Example").gameObject;
            templateButton.SetActive(false);

            container = go.transform.Find("Buttons");
        }

        public static void Close()
        {
            MainScreen.Open();
            foreach (GameObject button in slots)
            {
                button.Destroy();
            }
            slots.Clear();
        }

        public static void LoadButtons()
        {
            currentPage = 0;

            foreach (GameObject s in slots) if (s != null) Destroy(s);
            slots.Clear();

            for (int i = 0; i < buttonsPerPage; i++)
            {
                GameObject clone = Instantiate(templateButton, container);
                clone.name = "ListButton_" + i;
                clone.transform.localPosition = templateButton.transform.localPosition + Vector3.down * spacing * i;
                clone.transform.localRotation = templateButton.transform.localRotation;
                clone.transform.localScale = templateButton.transform.localScale;
                clone.SetActive(true);
                slots.Add(clone);
            }

            RefreshPage();
        }

        private static void RefreshPage()
        {
            int start = currentPage * buttonsPerPage;

            for (int i = 0; i < slots.Count; i++)
            {
                int idx = start + i;
                bool has = idx < instance.songs.Count;
                slots[i].SetActive(has);
                if (!has) continue;

                SongListButton btn = slots[i].GetComponent<SongListButton>();
                if (btn == null) btn = slots[i].AddComponent<SongListButton>();
                btn.connectedClip = instance.songs[idx];
                TextMeshPro label = slots[i].GetComponentInChildren<TextMeshPro>();
                if (label != null) label.text = instance.songs[idx].name;
            }
        }

        public static void NextPage()
        {
            if (currentPage >= PageCount - 1) return;
            currentPage++;
            RefreshPage();
        }

        public static void PrevPage()
        {
            if (currentPage <= 0) return;
            currentPage--;
            RefreshPage();
        }
    }

    private static class TimeScreen
    {
        public static GameObject go;

        public static void Open()
        {
            go.SetActive(true);
            MainScreen.go.SetActive(false);
            NoSongsScreen.go.SetActive(false);
            SettingsScreen.go.SetActive(false);
            RoomScreen.go.SetActive(false);
            SongListScreen.go.SetActive(false);
        }

        public static void ToggleRain()
        {
            bool rain = BetterDayNightManager.instance.CurrentWeather() == BetterDayNightManager.WeatherType.None;

            for (int i = 0; i < BetterDayNightManager.instance.weatherCycle.Length; i++)
                BetterDayNightManager.instance.weatherCycle[i] = rain ? BetterDayNightManager.WeatherType.Raining : BetterDayNightManager.WeatherType.None;

            HarmonyPatches.DayNightManagerPatch.enabled = true;
        }

        public static void ChangeTime(int index)
        {
            BetterDayNightManager.instance.SetTimeOfDay(index);
            HarmonyPatches.DayNightManagerPatch.enabled = true;
        }

        public static void ResetTime()
        {
            BetterDayNightManager.instance.ClearTimeOfDay();
            BetterDayNightManager.instance.ClearFixedWeather();
            HarmonyPatches.DayNightManagerPatch.enabled = false;
        }
    }

    public void ApplyTheme(MenuTheme menuTheme)
    {
        background.GetComponent<Renderer>().material.color = menuTheme.backColor;
        offsetGO.transform.Find("Text (TMP)").GetComponent<TextMeshPro>().color = menuTheme.buttonColor; // set the menu title color
        SettingsScreen.go.transform.Find("SwitchTheme").Find("Text (TMP)").GetComponent<TextMeshPro>().text = $"Switch Theme (<color=#{ColorUtility.ToHtmlStringRGB(menuTheme.backColor)}>{menuTheme.name}</color>)";
        PressableButton.ChangeButtonColors(menuTheme.buttonColor);
    }

    public static MenuTheme GetThemeFromName(string name)
    {
        foreach (MenuTheme theme in menuThemes)
        {
            if (theme.name == name)
                return theme;
        }
        NotificationSystem.Send($"<color=red>Failed to find theme with name: {name}</color>");
        return null;
    }

    private static bool GetJoystickClick()
    {
        bool click = false;
        switch (PlayFabAuthenticator.instance.platform.PlatformTag)
        {
            case "Steam":
                click = SteamVR_Actions.gorillaTag_RightJoystickClick.state;
                break;
            default:
                ControllerInputPoller.instance.rightControllerDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out click);
                break;
        }
        return click;
    }

    private IEnumerator LoadSongs()
    {
        songs.Clear();
        Main.Instance.musicPlayer.clip = null;
        string musicPath = Path.Combine(Application.dataPath, "..", "GorillaMusicPad", "Music");
        MainScreen.go.SetActive(false);
        yield return StartCoroutine(LoadAudioFile(musicPath, clips =>
        {
            if (clips.Count == 0)
            {
                NoSongsScreen.Open();
            }
            else
            {
                songs = clips;
                Main.Instance.musicPlayer.clip = clips.FirstOrDefault();
            }
        }));
        if (!NoSongsScreen.go.activeSelf)
            MainScreen.Open(); // also refreshes the song name text
    }

    public IEnumerator LoadSongFromWeb(string url, AudioType audioType, bool downloadFile, Action<AudioClip> onComplete)
    {
        string musicDir = Path.Combine(Application.dataPath, "..", "GorillaMusicPad", "Music");
        musicDir = Path.GetFullPath(musicDir);

        if (!Directory.Exists(musicDir))
            Directory.CreateDirectory(musicDir);

        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
        yield return request.SendWebRequest();

        string saveFile = Path.Combine(musicDir, Path.GetFileName(url));

        if (request.result != UnityWebRequest.Result.Success)
        {
            NotificationSystem.Send($"Failed to download audio from {url}: {request.error}");
            onComplete.Invoke(null);
            yield break;
        }

        File.WriteAllBytes(saveFile, request.downloadHandler.data);

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        clip.name = Path.GetFileNameWithoutExtension(url);
        onComplete.Invoke(clip);
        request.Dispose();
    }

    public IEnumerator LoadAudioFile(string directory, Action<List<AudioClip>> onComplete)
    {
        List<AudioClip> clips = new List<AudioClip>();
        string path = Path.GetFullPath(directory);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            onComplete.Invoke(clips);
            yield break;
        }

        string[] files = Directory.GetFiles(path);

        foreach (string file in files)
        {
            string ext = Path.GetExtension(file).ToLower();

            if (!audioTypes.TryGetValue(ext, out AudioType audioType)) continue;

            string url = "file://" + file;

            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                NotificationSystem.Send($"Failed to load audio file {file}: {request.error}");
                continue;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            clip.name = Path.GetFileNameWithoutExtension(file);
            clips.Add(clip);

            request.Dispose();
        }

        NotificationSystem.Send($"Loaded {clips.Count} audio clips");
        onComplete.Invoke(clips);
    }

    public static AudioType GetAudioType(string url)
    {
        string ext = Path.GetExtension(url).ToLower();
        return ext switch
        {
            ".mp3" => AudioType.MPEG,
            ".wav" => AudioType.WAV,
            ".ogg" => AudioType.OGGVORBIS,
            ".aiff" => AudioType.AIFF,
            ".aif" => AudioType.AIFF,
            _ => AudioType.UNKNOWN
        };
    }

    public class MenuTheme
    {
        public string name;

        public Color backColor;

        public Color buttonColor;
    }
}
