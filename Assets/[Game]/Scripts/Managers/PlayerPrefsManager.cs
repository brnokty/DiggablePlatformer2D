using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPrefsManager : MonoBehaviour
{
    #region Singleton

    public static PlayerPrefsManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    private string playerNameKey = "PlayerName";
    private string soundKey = "Sound";
    private string mirrorKey = "Mirror";
    private string webCamDeviceKey = "WebCamDevice";

    //Player Name
    public void SetPlayerName(string name)
    {
        PlayerPrefs.SetString(playerNameKey, name);
    }

    public string GetPlayerName()
    {
        return PlayerPrefs.GetString(playerNameKey);
    }

    //Sound
    public void SetSound(bool value)
    {
        PlayerPrefs.SetInt(soundKey, value ? 1 : 0);
    }

    public bool GetSound()
    {
        return PlayerPrefs.GetInt(soundKey, 1) == 1;
    }

    //Mirror
    public void SetMirror(bool value)
    {
        PlayerPrefs.SetInt(mirrorKey, value ? 1 : 0);
    }

    public bool GetMirror()
    {
        return PlayerPrefs.GetInt(mirrorKey) == 1;
    }

    //WebCamDevice
    public void SetWebCamDevice(int value)
    {
        PlayerPrefs.SetInt(webCamDeviceKey, value);
    }

    public int GetWebCamDevice()
    {
        return PlayerPrefs.GetInt(webCamDeviceKey);
    }
}