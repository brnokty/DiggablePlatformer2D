using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    #region Singleton

    public static UIManager Instance;

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

    public InGamePanel inGamePanel;

    private void Start()
    {
        ShowInGamePanel();
    }

    public void ShowInGamePanel()
    {
        inGamePanel.Appear();

        //disable other panels
    }

    //hide all panels
    public void HideAllPanels()
    {
        inGamePanel.Disappear();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}