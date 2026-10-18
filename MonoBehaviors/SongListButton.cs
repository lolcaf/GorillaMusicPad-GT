using UnityEngine;

namespace GorillaMusicPad.MonoBehaviors;

public class SongListButton : PressableButton
{
    public AudioClip connectedClip;

    public override void OnButtonPress()
    {
        Main.Instance.SelectSong(connectedClip);
        MenuManager.MainScreen.Open();
    }
}
