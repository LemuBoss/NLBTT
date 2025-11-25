using UnityEngine;

/// <summary>
/// Base class for simple event cards that trigger once
/// </summary>
public abstract class SimpleEventCard : Card
{
    protected bool hasTriggered = false;
    protected string eventMessage;

    public override void OnPlayerEnter()
    {
        if (!hasTriggered)
        {
            TriggerEvent();
            hasTriggered = true;
        }
    }

    // Override this in specific card implementations
    protected abstract void TriggerEvent();
}