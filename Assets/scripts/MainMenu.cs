using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Netcode.Transports.UNET;

public class MainMenu : MonoBehaviour
{
    const string gameScene = "Game";

    public void JoinServer()
    {
        NetworkManager.Singleton.StartClient();
    }

    public void HostServer()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Single);
        NetworkManager.Singleton.SceneManager.LoadScene(gameScene, LoadSceneMode.Single);
    }

    public void SetHostPort(string port)
    {
        if (int.TryParse(port, out int portNum)) NetworkManager.Singleton.GetComponent<UNetTransport>().ServerListenPort = portNum;
    }
    
    public void SetJoinIP(string ip)
    {
        NetworkManager.Singleton.GetComponent<UNetTransport>().ConnectAddress = ip;
    }

    public void SetJoinPort(string port)
    {
        if (int.TryParse(port, out int portNum)) NetworkManager.Singleton.GetComponent<UNetTransport>().ConnectPort = portNum;
    }
}