using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public InputField nickNameInput;
    public GameObject disconnectPanel;
    public GameObject respawnPanel;

    private void Awake()
    {
        // Screen.SetResolution(960, 540, false);
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 30;
    }

    public void Connect() { if (nickNameInput.text != String.Empty) PhotonNetwork.ConnectUsingSettings(); else Debug.LogWarning("Empty Nickname Field"); }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.LocalPlayer.NickName = nickNameInput.text;
        PhotonNetwork.JoinOrCreateRoom("Room", new RoomOptions {MaxPlayers = 6}, null);
    }

    public override void OnJoinedRoom()
    {
        disconnectPanel.SetActive(false);
        StartCoroutine("DestroyBullet");
        Spawn();
    }

    IEnumerator DestroyBullet()
    {
        yield return new WaitForSeconds(0.2f);
        foreach (GameObject GO in GameObject.FindGameObjectsWithTag("Bullet")) GO.GetComponent<PhotonView>().RPC("DestroyRPC", RpcTarget.All);
    }

    public void Spawn()
    {
        PhotonNetwork.Instantiate("Player", new Vector3(UnityEngine.Random.Range(-17f, 16f), 16.5f, 0), Quaternion.identity);
        respawnPanel.SetActive(false);
    }

    private void Update() { if (Input.GetKeyDown(KeyCode.Escape) && PhotonNetwork.IsConnected) { PhotonNetwork.Disconnect(); } }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        disconnectPanel.SetActive(true);
        respawnPanel.SetActive(false);
    }
}
