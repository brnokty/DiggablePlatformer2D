using UnityEngine.Events;


public class EventHolder<T>
{
    public UnityAction<T> OnEvent;

    public EventHolder()
    {
    }
}