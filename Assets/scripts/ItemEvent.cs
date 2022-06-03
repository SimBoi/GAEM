using Unity.Netcode;
using UnityEngine;

public enum ItemEventType
{
    SinglePrimaryEvent,
    ContinuousPrimaryEvent,
    SingleSeconadryEvent,
    ContinuousSeconadryEvent
}

public class ItemEvent : NetworkBehaviour
{
    public ItemEventType eventType;
    private bool isEventActive = false;
    private bool wasEventActive = false;
    private GameObject lastEventCaller;

    private void LateUpdate()
    {
        if (wasEventActive && !isEventActive)
            CustomItemEventExit(lastEventCaller);

        wasEventActive = isEventActive;
        isEventActive = false;
    }

    public void CustomPrimaryItemEvent(GameObject eventCaller)
    {
        if (eventType == ItemEventType.SinglePrimaryEvent)
        {
            isEventActive = true;
            lastEventCaller = eventCaller;
            if (wasEventActive == false)
                CustomItemEvent(eventCaller);
        }
        else if (eventType == ItemEventType.ContinuousPrimaryEvent)
        {
            isEventActive = true;
            lastEventCaller = eventCaller;
            CustomItemEvent(eventCaller);
        }
    }

    public void CustomSecondaryItemEvent(GameObject eventCaller)
    {
        if (eventType == ItemEventType.SingleSeconadryEvent)
        {
            isEventActive = true;
            lastEventCaller = eventCaller;
            if (wasEventActive == false)
                CustomItemEvent(eventCaller);
        }
        else if (eventType == ItemEventType.ContinuousSeconadryEvent)
        {
            isEventActive = true;
            lastEventCaller = eventCaller;
            CustomItemEvent(eventCaller);
        }
    }

    public virtual void CustomItemEvent(GameObject eventCaller) { }

    public virtual void CustomItemEventExit(GameObject eventCaller) { }
}
