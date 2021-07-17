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
    public AudioClip gunShot;
    public AudioClip reload;
    public Text statusText;
    private SettingsScript _settings;

    private bool _isGround;
    private Vector3 _curPos;

    public int speed = 4;
    public float initHealth = 100f;
    private float _health;
    private float _currentHealth;
    public float damage = 10f;
    public int initMag = 12;
    private int _mag = 12;
    private bool _canShoot = true;
    public float healthRegenTime = 10f;
    private float _timer = 0f;

    float axis = 0f;

    private void Awake()
    {
        // 닉네임
        nickNameText.text = pv.IsMine ? PhotonNetwork.NickName : pv.Owner.NickName;
        nickNameText.color = pv.IsMine ? Color.green : Color.red;
        statusText.text = _mag + " / " + initMag;

        _health = initHealth;
        _currentHealth = initHealth;

        _settings = FindObjectOfType<SettingsScript>();

        StartCoroutine("CompareHealth");

        if (pv.IsMine)
        {
            // 2D 카메라
            var CM = GameObject.Find("CMCamera").GetComponent<CinemachineVirtualCamera>();
            CM.Follow = transform;
            CM.LookAt = transform;
            statusText.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (pv.IsMine)
        {

            // 이동
            
            if (Input.GetKey(_settings.left) || Input.GetKey(_settings.leftAlt))
            {
                axis = -1f;
            }
            else if (Input.GetKey(_settings.right) || Input.GetKey(_settings.rightAlt))
            {
                axis = 1f;
            }
            else if (Input.GetKeyUp(_settings.left) || Input.GetKeyUp(_settings.leftAlt) || Input.GetKeyUp(_settings.right) || Input.GetKeyUp(_settings.rightAlt))
            {
                axis = 0f;
            }

            rb.velocity = new Vector2(speed * axis, rb.velocity.y);
            
            if (axis != 0)
            {
                anim.SetBool("walk", true);
                pv.RPC("FlipXRPC", RpcTarget.AllBuffered, axis);
            }
            else anim.SetBool("walk", false);
            
            // 점프
            _isGround = Physics2D.OverlapCircle(
                (Vector2) transform.position + new Vector2(0, -0.5f), 
                0.07f, 
                1 << LayerMask.NameToLayer("Ground"));
            
            anim.SetBool("jump", !_isGround);
            if ((Input.GetKeyDown(_settings.jump) && _isGround) || (Input.GetKeyDown(_settings.jumpAlt) && _isGround)) pv.RPC("JumpRPC", RpcTarget.All);
            
            // 총 퓨퓨
            if (Input.GetKeyDown(_settings.shoot) || Input.GetKeyDown(_settings.shootAlt))
            {
                StartCoroutine("Shoot");
            }
            if (Input.GetKeyDown(_settings.reload) || Input.GetKeyDown(_settings.reloadAlt) || _mag <= 0)
            {
                if (_mag == initMag) return;
                    if (_canShoot)
                {
                    StartCoroutine("Reload");
                }
            }

            if (Math.Abs(_health - _currentHealth) < 0.1f)
            {
                _timer += Time.deltaTime;
                if (_timer >= healthRegenTime)
                {
                    _health = initHealth;
                    healthImage.fillAmount = _health / initHealth;
                }
            }
            else
            {
                _timer = 0f;
            }

            
        }
        // IsMine이 아닌 것들 부드럽게 위치 동기화
        else if ((transform.position - _curPos).sqrMagnitude >= 100) transform.position = _curPos;
        else transform.position = Vector3.Lerp(transform.position, _curPos, Time.deltaTime * 10);
    }

    private IEnumerator Shoot()
    {
        if (!_canShoot) yield break;
        _mag -= 1;
        statusText.text = _mag + " / " + initMag;
        pv.RPC("ShootSound", RpcTarget.All);
        PhotonNetwork.Instantiate("Bullet", transform.position + new Vector3(sr.flipX ? -0.4f : 0.4f, -0.11f, 0), Quaternion.identity)
            .GetComponent<PhotonView>().RPC("DirRPC", RpcTarget.All, sr.flipX ? -1 : 1);
        anim.SetTrigger("shoot");
    }

    private IEnumerator Reload()
    {
        _canShoot = false;
        statusText.text = "재장전 중...";
        pv.RPC("ReloadSound", RpcTarget.All);
        yield return new WaitForSeconds(reload.length);
        _mag = initMag;
        _canShoot = true;
        statusText.text = _mag + " / " + initMag;
    }

    private IEnumerator CompareHealth()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            _currentHealth = _health;
        }
    }

    [PunRPC]
    private void FlipXRPC(float axis) => sr.flipX = axis == -1;

    [PunRPC]
    private void JumpRPC()
    {
        rb.velocity = Vector2.zero;
        rb.AddForce(Vector2.up * 700);
    }

    [PunRPC]
    private void ShootSound()
    {
        AudioSource audioRPC = gameObject.GetComponent<AudioSource>();
        audioRPC.spatialBlend = 1;
        audioRPC.minDistance = 1;
        audioRPC.maxDistance = 200;
        audioRPC.PlayOneShot(gunShot);
    }
    
    [PunRPC]
    private void ReloadSound()
    {
        AudioSource audioRPC = gameObject.GetComponent<AudioSource>();
        audioRPC.spatialBlend = 1;
        audioRPC.minDistance = 1;
        audioRPC.maxDistance = 1;
        audioRPC.PlayOneShot(reload);
    }

    public void Hit(float d)
    {
        _health -= d;
        healthImage.fillAmount = _health / initHealth;
        if (_health <= 0)
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
            _curPos = (Vector3) stream.ReceiveNext();
            healthImage.fillAmount = (float) stream.ReceiveNext();
        }
    }
}
