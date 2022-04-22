using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class MainMenu : MonoBehaviour
{
    const string gameScene = "Game";

    public void LoadGame()
    {
        SceneManager.LoadScene(gameScene);
    }
}