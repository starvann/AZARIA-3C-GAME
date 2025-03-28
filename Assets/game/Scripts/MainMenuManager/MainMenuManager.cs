using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField]
    private string _gameSceneName;
    // Start is called before the first frame update
    public void Play()
    {
        SceneManager.LoadScene(_gameSceneName);
    }

    // Update is called once per frame
    public void Exit()
    {
        Application.Quit();
    }
}
