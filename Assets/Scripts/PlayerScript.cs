using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class PlayerScript : MonoBehaviourPunCallbacks, IPunObservable
{
    public Rigidbody2D rb;
    public Animator anim;
    public SpriteRenderer sr;
    public PhotonView pv;
    public Text nickNameText;
    public Image healthImage;

    private bool isGround;
    private Vector3 curPos;

    public int speed = 4;
    public float initHealth = 100f;
    public float health = 100f;
    public float damage = 10f;

    private void Awake()
    {
        // 닉네임
        nickNameText.text = pv.IsMine ? PhotonNetwork.NickName : pv.Owner.NickName;
        nickNameText.color = pv.IsMine ? Color.green : Color.red;

        if (pv.IsMine)
        {
            // 2D 카메라
            var CM = GameObject.Find("CMCamera").GetComponent<CinemachineVirtualCamera>();
            CM.Follow = transform;
            CM.LookAt = transform;
        }
    }

    private void Update()
    {
        if (pv.IsMine)
        {
            // 이동
            float axis = Input.GetAxisRaw("Horizontal");
            rb.velocity = new Vector2(speed * axis, rb.velocity.y);

            if (axis != 0)
            {
                anim.SetBool("walk", true);
                pv.RPC("FlipXRPC", RpcTarget.AllBuffered, axis);
            }
            else anim.SetBool("walk", false);
            
            // 점프
            isGround = Physics2D.OverlapCircle(
                (Vector2) transform.position + new Vector2(0, -0.5f), 
                0.07f, 
                1 << LayerMask.NameToLayer("Ground"));
            
            anim.SetBool("jump", !isGround);
            if (Input.GetKeyDown(KeyCode.UpArrow) && isGround) pv.RPC("JumpRPC", RpcTarget.All);
            
            // 총 퓨퓨
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PhotonNetwork.Instantiate("Bullet", transform.position + new Vector3(sr.flipX ? -0.4f : 0.4f, -0.11f, 0), Quaternion.identity)
                    .GetComponent<PhotonView>().RPC("DirRPC", RpcTarget.All, sr.flipX ? -1 : 1);
                anim.SetTrigger("shoot");
            }
        }
        // IsMine이 아닌 것들 부드럽게 위치 동기화
        else if ((transform.position - curPos).sqrMagnitude >= 100) transform.position = curPos;
        else transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime * 10);
    }

    [PunRPC]
    private void FlipXRPC(float axis) => sr.flipX = axis == -1;

    [PunRPC]
    private void JumpRPC()
    {
        rb.velocity = Vector2.zero;
        rb.AddForce(Vector2.up * 700);
    }

    public void Hit(float d)
    {
        health -= d;
        healthImage.fillAmount = health / initHealth;
        if (health <= 0)
        {
            GameObject.Find("Canvas").transform.Find("RespawnPanel").gameObject.SetActive(true);
            pv.RPC("DestroyRPC", RpcTarget.AllBuffered); // AllBuffered => 버그 X
        }
    }
    
    [PunRPC]
    private void DestroyRPC() => Destroy(gameObject);

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(healthImage.fillAmount);
        }
        else
        {
            curPos = (Vector3) stream.ReceiveNext();
            healthImage.fillAmount = (float) stream.ReceiveNext();
        }
    }
}
