using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private const string mainMenuScene = "MainMenu";

    public GameObject spawnUI;
    public GameObject respawnUI;
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    private GameObject activePlayer = null;

    private void Start()
    {
        spawnUI.SetActive(true);
    }

    private void Update()
    {
        if(activePlayer != null && activePlayer.transform.position.y < -5)
        {
            KillPlayer();
        }
    }

    public void Spawn()
    {
        int i = Random.Range(0, spawnPoints.Length);
        activePlayer = Instantiate(playerPrefab, spawnPoints[i].position, spawnPoints[i].rotation);
        spawnUI.SetActive(false);
        respawnUI.SetActive(false);
    }

    public void KillPlayer()
    {
        if (activePlayer != null)
        {
            activePlayer.GetComponent<CharacterController>().Die();
            activePlayer = null;
        }
        respawnUI.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        KillPlayer();
        SceneManager.LoadScene(mainMenuScene);
    }
}