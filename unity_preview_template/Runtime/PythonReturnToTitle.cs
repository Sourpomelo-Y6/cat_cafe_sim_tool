using UnityEngine;
using UnityEngine.SceneManagement;
namespace CatCafePreview
{
    public class PythonReturnToTitle : MonoBehaviour
    {
        public void Return() { SceneManager.LoadScene("CatCafeTitle"); }
    }
}
