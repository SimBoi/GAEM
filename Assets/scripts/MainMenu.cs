using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    const string gameScene = "Game";

    public void LoadGame()
    {
        SceneManager.LoadScene(gameScene);
    }
}