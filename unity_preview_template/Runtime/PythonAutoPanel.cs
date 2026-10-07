using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonAutoPanel : MonoBehaviour
    {
        public Button toggle, speed;
        public void Toggle() { GetComponent<PythonStateClient>().ToggleAutomatic(); }
        public void ChangeSpeed() { GetComponent<PythonStateClient>().ToggleSpeed(); }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();
            toggle.interactable=client.Automatic || client.CanStartAutomatic;
            toggle.GetComponentInChildren<Text>().text=client.Automatic ? "自動進行を停止" : "自動進行を開始";
            speed.GetComponentInChildren<Text>().text=client.Fast ? "速度：高速" : "速度：通常";
        }
    }
}
