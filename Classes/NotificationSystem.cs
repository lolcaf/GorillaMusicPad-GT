using TMPro;
using UnityEngine;

namespace GorillaMusicPad.Classes
{
    public class NotificationSystem
    {
        public static void Send(string text)
        {
            if (GameObject.Find("NotificationText") != null)
            {
                Object.DestroyImmediate(GameObject.Find("NotificationText")); // destroy the overlapping notification text if there is one
            }
            GameObject txt = new GameObject("NotificationText");
            txt.transform.SetParent(Camera.main.transform, false);
            txt.transform.localPosition = new Vector3(0f, -0.3f, 0.8f);
            txt.transform.localRotation = Quaternion.identity;
            txt.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            TextMeshPro tmp = txt.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 4;
            tmp.richText = true;

            Object.Destroy(txt, 4);
        }
    }
}
