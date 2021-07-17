using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsScript : MonoBehaviour
{

    public KeyCode shoot = KeyCode.Space;
    public KeyCode left = KeyCode.LeftArrow;
    public KeyCode right = KeyCode.RightArrow;
    public KeyCode jump = KeyCode.UpArrow;
    public KeyCode shootAlt;
    public KeyCode leftAlt = KeyCode.A;
    public KeyCode rightAlt = KeyCode.D;
    public KeyCode jumpAlt;
    public KeyCode reload = KeyCode.R;
    public KeyCode reloadAlt;

    public Text shootCustomText;
    public Text leftCustomText;
    public Text rightCustomText;
    public Text jumpCustomText;
    public Text reloadCustomText;
    public Text shootAltCustomText;
    public Text leftAltCustomText;
    public Text rightAltCustomText;
    public Text jumpAltCustomText;
    public Text reloadAltCustomText;

    public Text settingsStatusText;

    private bool _waiting = false;
    private KeyCustomButton _targetButton;

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        if (_waiting)
        {
            foreach(KeyCode vKey in Enum.GetValues(typeof(KeyCode))){
                if(Input.GetKey(vKey)){
                    if (vKey == shoot || vKey == left || vKey == right || vKey == jump || vKey == reload || vKey == shootAlt || vKey == leftAlt || vKey == rightAlt || vKey == jumpAlt || vKey == reloadAlt)
                    {
                        _waiting = false;
                        settingsStatusText.text = "중복되는 조작키입니다!";
                        return;
                    }
                    switch (_targetButton.customKeyName)
                    {
                        case "shoot":
                            shoot = vKey;
                            settingsStatusText.text = _targetButton.gameObject.name + "의 조작키를 " + vKey + "로 지정했습니다!";
                            break;
                        case "left":
                            left = vKey;
                            settingsStatusText.text = _targetButton.gameObject.name + "의 조작키를 " + vKey + "로 지정했습니다!";
                            break;
                        case "right":
                            right = vKey;
                            settingsStatusText.text = _targetButton.gameObject.name + "의 조작키를 " + vKey + "로 지정했습니다!";
                            break;
                        case "jump":
                            jump = vKey;
                            settingsStatusText.text = _targetButton.gameObject.name + "의 조작키를 " + vKey + "로 지정했습니다!";
                            break;
                        case "reload":
                            reload = vKey;
                            settingsStatusText.text = _targetButton.gameObject.name + "의 조작키를 " + vKey + "로 지정했습니다!";
                            break;
                        case "shootAlt":
                            shootAlt = vKey;
                            settingsStatusText.text = _targetButton.gameObject.name + "의 조작키를 " + vKey + "로 지정했습니다!";
                            break;
                        case "leftAlt":
                            leftAlt = vKey;
                            settingsStatusText.text = _targetButton.gameObject.name + "의 조작키를 " + vKey + "로 지정했습니다!";
                            break;
                        case "rightAlt":
                            rightAlt = vKey;
                            settingsStatusText.text = _targetButton.gameObject.name + "의 조작키를 " + vKey + "로 지정했습니다!";
                            break;
                        case "jumpAlt":
                            jumpAlt = vKey;
                            settingsStatusText.text = _targetButton.gameObject.name + "의 조작키를 " + vKey + "로 지정했습니다!";
                            break;
                        case "reloadAlt":
                            reloadAlt = vKey;
                            settingsStatusText.text = _targetButton.gameObject.name + "의 조작키를 " + vKey + "로 지정했습니다!";
                            break;
                    }
                    UpdateUI();
                    _waiting = false;
                }
            }
        }
    }

    public void UpdateUI()
    {
        shootCustomText.text = shoot.ToString();
        leftCustomText.text = left.ToString();
        rightCustomText.text = right.ToString();
        jumpCustomText.text = jump.ToString();
        reloadCustomText.text = reload.ToString();
        shootAltCustomText.text = shootAlt.ToString();
        leftAltCustomText.text = leftAlt.ToString();
        rightAltCustomText.text = rightAlt.ToString();
        jumpAltCustomText.text = jumpAlt.ToString();
        reloadAltCustomText.text = reloadAlt.ToString();
    }

    public void SetCustomKey(KeyCustomButton button)
    {
        _waiting = true;
        _targetButton = button;
        settingsStatusText.text = _targetButton.gameObject.name + "의 커스텀 조작키를 설정중...";
    }
}
