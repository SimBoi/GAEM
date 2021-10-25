using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemEventType
{
    SinglePrimaryEvent,
    ContinuousPrimaryEvent,
    SingleSeconadryEvent,
    ContinuousSeconadryEvent
}

public class ItemEvent : MonoBehaviour
{
    public ItemEventType eventType;
    private bool isActive = false;
    private bool wasActive = false;
    private GameObject lastEventCaller;

    private void LateUpdate()
    {
        if (wasActive && !isActive)
            CustomEventExit(lastEventCaller);

        wasActive = isActive;
        isActive = false;
    }

    public void CustomPrimaryEvent(GameObject eventCaller)
    {
        if (eventType == ItemEventType.SinglePrimaryEvent)
        {
            isActive = true;
            lastEventCaller = eventCaller;
            if (wasActive == false)
                CustomEvent(eventCaller);
        }
        else if (eventType == ItemEventType.ContinuousPrimaryEvent)
        {
            isActive = true;
            lastEventCaller = eventCaller;
            CustomEvent(eventCaller);
        }
    }

    public void CustomSecondaryEvent(GameObject eventCaller)
    {
        if (eventType == ItemEventType.SingleSeconadryEvent)
        {
            isActive = true;
            lastEventCaller = eventCaller;
            if (wasActive == false)
                CustomEvent(eventCaller);
        }
        else if (eventType == ItemEventType.ContinuousSeconadryEvent)
        {
            isActive = true;
            lastEventCaller = eventCaller;
            CustomEvent(eventCaller);
        }
    }

    public virtual void CustomEvent(GameObject eventCaller)
    {

    }

    public virtual void CustomEventExit(GameObject eventCaller)
    {

    }
}
