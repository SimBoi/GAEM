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
    public List<VoxelGrid> voxelGrids = new List<VoxelGrid>(); // voxel grids should add themselves to the list

    private GameObject activePlayer = null;

    private void Start()
    {
        spawnUI.SetActive(true);
    }

    public void Spawn()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        int i = Random.Range(0, spawnPoints.Length);
        activePlayer = Instantiate(playerPrefab, spawnPoints[i].position, spawnPoints[i].rotation);
        spawnUI.SetActive(false);
        respawnUI.SetActive(false);
    }

    public void KillPlayer()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (activePlayer != null)
        {
            activePlayer.GetComponent<CharacterController>().Die(gameObject);
        }
        activePlayer = null;
        respawnUI.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        KillPlayer();
        SceneManager.LoadScene(mainMenuScene);
    }
}