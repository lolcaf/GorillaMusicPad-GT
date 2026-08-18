using System;
using UnityEngine;

namespace GorillaMusicPad.MonoBehaviors
{
    public class PressableButton : MonoBehaviour
    {
        private float time;

        public Action buttonPressed;

        private void Start()
        {
            gameObject.layer = 18; // the only layer buttons can be on
        }

        public void OnButtonPress()
        {
            buttonPressed.Invoke();
            VRRig.LocalRig.PlayHandTapLocal(67, false, 0.2f);
        }

        private void OnTriggerEnter(Collider other)
        {
            Main.Log.WriteLine(other.gameObject.name);
            if (time < Time.time && other.gameObject == GorillaTagger.Instance.rightHandTriggerCollider)
            {
                time = Time.time + 0.3f;
                OnButtonPress();
            }
        }
    }
}
