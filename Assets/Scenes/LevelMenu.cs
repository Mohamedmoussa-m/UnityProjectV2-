using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMenu : MonoBehaviour
{
    public void LoadLevel1() { SceneManager.LoadScene("Level_1"); }
    public void LoadLevel2() { SceneManager.LoadScene("Level_2"); }
}
