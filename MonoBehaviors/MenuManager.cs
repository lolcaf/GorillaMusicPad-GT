using GorillaMusicPad.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace GorillaMusicPad.MonoBehaviors
{
    public class MenuManager : MonoBehaviour // this will be attached to the menu
    {
        public GameObject offsetGO;

        private List<AudioClip> songs = new List<AudioClip>();

        public static MenuManager instance;

        public bool menuOpen = false;

        private float menuButtonCooldown = 0;

        private static readonly Dictionary<string, AudioType> audioTypes = new Dictionary<string, AudioType> // supported audio types
        {
            { ".mp3",  AudioType.MPEG    },
            { ".wav",  AudioType.WAV     },
            { ".ogg",  AudioType.OGGVORBIS },
            { ".aiff", AudioType.AIFF    },
            { ".aif",  AudioType.AIFF    },
        };

        private void Start()
        {
            instance = this;

            offsetGO = transform.Find("Offset").gameObject;
            offsetGO.transform.Find("VersionText").GetComponent<TextMeshPro>().text = "Version: " + Constants.Version;
            offsetGO.transform.localScale = Vector3.one * 1.5f; // fix the offset because I dont feel like rebuilding the asset bundle
            offsetGO.transform.localPosition = new Vector3(-0.2f, 0.15f, 0.15f);

            MainScreen.go = offsetGO.transform.Find("MainScreen").gameObject;
            NoSongsScreen.go = offsetGO.transform.Find("NoSongsScreen").gameObject;

            // setup the buttons
            NoSongsScreen.go.transform.Find("DownloadSongs").gameObject.AddComponent<PressableButton>().buttonPressed += NoSongsScreen.DownloadSongs;
            NoSongsScreen.go.transform.Find("OpenFolder").gameObject.AddComponent<PressableButton>().buttonPressed += NoSongsScreen.OpenSongFolder;
            NoSongsScreen.go.transform.Find("Continue").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.Open;

            MainScreen.go.transform.Find("Pause").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.Pause;
            MainScreen.go.transform.Find("Next").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.NextSong;
            MainScreen.go.transform.Find("Previous").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.PreviousSong;
            MainScreen.go.transform.Find("VolumeUp").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.TurnUpVolume;
            MainScreen.go.transform.Find("VolumeDown").gameObject.AddComponent<PressableButton>().buttonPressed += MainScreen.TurnDownVolume;

            string musicPath = Path.Combine(Application.dataPath, "..", "GorillaMusicPad", "Music");
            StartCoroutine(LoadAudioFile(musicPath, clips =>
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
                if (Main.Instance.musicPlayer.clip == null) Main.Instance.musicPlayer.clip = songs.FirstOrDefault();
                MainScreen.go.transform.Find("SongName").gameObject.GetComponent<TextMeshPro>().text = Main.Instance.musicPlayer.clip.name;
            }

            offsetGO.transform.Find("Particles").gameObject.SetActive(Main.Instance.musicPlayer.isPlaying);
        }

        private class MainScreen
        {
            public static GameObject go;

            public static void Open()
            {
                go.SetActive(true);
                NoSongsScreen.go.SetActive(false);
                if (instance.songs.Count > 0)
                {
                    if (Main.Instance.musicPlayer.clip == null) Main.Instance.musicPlayer.clip = instance.songs.FirstOrDefault();
                    go.transform.Find("SongName").gameObject.GetComponent<TextMeshPro>().text = Main.Instance.musicPlayer.clip.name;
                }
                else
                {
                    go.transform.Find("SongName").gameObject.GetComponent<TextMeshPro>().text = "Missing Song";
                }
            }

            public static void NextSong()
            {
                if (instance.songs.Count == 0) return;
                int songIndex = instance.songs.IndexOf(Main.Instance.musicPlayer.clip);
                if (instance.songs.Count > songIndex + 1)
                {
                    songIndex++;
                    Main.Instance.musicPlayer.clip = instance.songs[songIndex];
                    go.transform.Find("SongName").GetComponent<TextMeshPro>().text = instance.songs[songIndex].name;
                }
            }

            public static void PreviousSong()
            {
                if (instance.songs.Count == 0) return;
                int songIndex = instance.songs.IndexOf(Main.Instance.musicPlayer.clip);
                if (songIndex > 0)
                {
                    songIndex--;
                    Main.Instance.musicPlayer.clip = instance.songs[songIndex];
                    go.transform.Find("SongName").GetComponent<TextMeshPro>().text = instance.songs[songIndex].name;
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
                go.transform.Find("VolumeText").gameObject.GetComponent<TextMeshPro>().text = $"{Mathf.Round(Main.Instance.musicPlayer.volume * 100)}%";
            }

            public static void TurnDownVolume()
            {
                Main.Instance.musicPlayer.volume -= 0.05f;
                go.transform.Find("VolumeText").gameObject.GetComponent<TextMeshPro>().text = $"{Mathf.Round(Main.Instance.musicPlayer.volume * 100)}%";
            }
        }

        private class NoSongsScreen
        {
            public static GameObject go;

            public static void Open()
            {
                go.SetActive(true);
                MainScreen.go.SetActive(false);
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
                yield return new WaitForSeconds(3);
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
                Main.Log.WriteLine($"Failed to download audio from {url}: {request.error}");
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
                    Main.Log.WriteLine($"Failed to load audio file {file}: {request.error}");
                    continue;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                clip.name = Path.GetFileNameWithoutExtension(file);
                clips.Add(clip);

                Main.Log.WriteLine($"Loaded: {clip.name}");
                request.Dispose();
            }

            Main.Log.WriteLine($"Loaded {clips.Count} audio clips from {path}");
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
    }
}
