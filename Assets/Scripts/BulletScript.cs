using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class BulletScript : MonoBehaviourPunCallbacks
{
    public PhotonView pv;
    private int dir;

    private float destroyTime = 3.5f;
    private float speed = 7;

    private void Start() => Destroy(gameObject, destroyTime);

    private void Update() => transform.Translate(Vector3.right * speed * Time.deltaTime * dir);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Ground") pv.RPC("DestroyRPC", RpcTarget.AllBuffered);
        if (!pv.IsMine && other.tag == "Player" && other.GetComponent<PhotonView>().IsMine) // 느린쪽에 맟춰서 Hit 판정
        {
            other.GetComponent<PlayerScript>().Hit(other.GetComponent<PlayerScript>().damage);
            pv.RPC("DestroyRPC", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    private void DirRPC(int dir) => this.dir = dir;

    [PunRPC]
    private void DestroyRPC() => Destroy(gameObject);
}
