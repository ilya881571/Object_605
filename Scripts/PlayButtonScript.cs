using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButtonScript : MonoBehaviour
{
    public void LoadLevel0()
    {
        SceneManager.LoadScene("Level0");
    }
}
