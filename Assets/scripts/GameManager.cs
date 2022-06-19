using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    private const string mainMenuScene = "MainMenu";

    public GameObject spawnUI;
    public GameObject respawnUI;
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    public GameObject activePlayer = null;

    private void Start()
    {
        spawnUI.SetActive(true);
    }

    public void Spawn()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SpawnServerRpc(NetworkManager.Singleton.LocalClientId);
        spawnUI.SetActive(false);
        respawnUI.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnServerRpc(ulong clientId)
    {
        int i = Random.Range(0, spawnPoints.Length);
        NetworkObject player = Instantiate(playerPrefab, spawnPoints[i].position, spawnPoints[i].rotation).GetComponent<NetworkObject>();
        player.SpawnWithOwnership(clientId);
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        SpawnClientRPC(player.NetworkObjectId, clientRpcParams);
    }

    [ClientRpc]
    private void SpawnClientRPC(ulong objectId, ClientRpcParams clientRpcParams)
    {
        activePlayer = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].gameObject;
    }

    public void KillPlayer()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (activePlayer != null)
        {
            activePlayer.GetComponent<CharacterController>().Die(false);
        }
        activePlayer = null;
        respawnUI.SetActive(true);
    }

    public void LeaveGame()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        KillPlayer();
        NetworkManager.Singleton.Shutdown();
        Destroy(NetworkManager.Singleton.gameObject);
        SceneManager.LoadScene(mainMenuScene);
    }
}