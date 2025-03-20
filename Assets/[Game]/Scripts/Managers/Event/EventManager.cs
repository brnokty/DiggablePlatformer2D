using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public enum EventTypes
{
    GameStart,
    GameFinish,
}

public class EventManager : MonoBehaviour
{
    #region Singleton

    public static EventManager Instance;

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

        Initialize();
    }

    #endregion

    public Dictionary<EventTypes, UnityAction<EventArgs>> events = new Dictionary<EventTypes, UnityAction<EventArgs>>();
    private UnityAction<EventArgs> onStationary;


    public void Initialize()
    {
        foreach (EventTypes foo in Enum.GetValues(typeof(EventTypes)))
        {
            EventHolder<EventArgs> eventHolder = new EventHolder<EventArgs>();
            events.Add(foo, eventHolder.OnEvent);
        }
    }

    public void Register(EventTypes eventType, UnityAction<EventArgs> callback)
    {
        events[eventType] += callback;
    }

    public void Unregister(EventTypes eventType, UnityAction<EventArgs> callback)
    {
        events[eventType] -= callback;
    }

    public void InvokeEvent(EventTypes eventType, EventArgs eventArgs = null)
    {
        events[eventType]?.Invoke(eventArgs);
    }
}