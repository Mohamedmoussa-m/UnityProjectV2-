using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuReturn : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "BasicScene";

    public void BackToMenu()
    {
        Debug.Log("Button clicked!");
    }

}
