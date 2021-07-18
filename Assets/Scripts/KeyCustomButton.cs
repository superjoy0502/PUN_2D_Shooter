using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyCustomButton : MonoBehaviour
{
    public string customKeyName;
    private SettingsScript _settings;

    private void Start()
    {
        _settings = FindObjectOfType<SettingsScript>();
    }

    public void Interact()
    {
        _settings.SetCustomKey(this);
    }
}
