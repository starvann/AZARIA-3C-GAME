using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayManager : MonoBehaviour
{
    [SerializeField]
    private string _mainMenuSceneName;

    [SerializeField]
    private InputManager _inputManager;

    private void Start()
    {
        _inputManager.OnMainMenuInput += BackToMainMenu;
    }

    private void ODestroy()
    {
        _inputManager.OnMainMenuInput -= BackToMainMenu;
    }
    private void BackToMainMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(_mainMenuSceneName);
    }
}
