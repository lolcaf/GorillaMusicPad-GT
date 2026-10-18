using System;
using System.Collections.Generic;
using UnityEngine;

namespace GorillaMusicPad.MonoBehaviors;

public class PressableButton : MonoBehaviour
{
    private static List<PressableButton> instances = new List<PressableButton>();

    public static float time;

    public Action buttonPressed;

    private void Start()
    {
        instances.Add(this);
        gameObject.layer = 18; // the only layer buttons can be on
        gameObject.GetComponent<Renderer>().material.color = MenuManager.instance.currentTheme.buttonColor;
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }

    public virtual void OnButtonPress()
    {
        buttonPressed.Invoke();
    }

    public static void ChangeButtonColors(Color color)
    {
        foreach (PressableButton button in instances)
        {
            button.GetComponent<Renderer>().material.color = color;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (time < Time.time && other.gameObject == GorillaTagger.Instance.rightHandTriggerCollider)
        {
            time = Time.time + 0.4f;
            VRRig.LocalRig.PlayHandTapLocal(67, false, 0.2f);
            OnButtonPress();
        }
    }
}
