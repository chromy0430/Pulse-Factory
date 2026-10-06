using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PulseEventChannel",
    menuName = "Pulse Factory/Events/Pulse Event Channel")]
public class PulseEventChannelSO : ScriptableObject
{
    private event Action<PulseData> listeners;

    [Title(
        "Pulse Event Channel",
        "전역 Pulse 이벤트를 전달합니다.")]

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [ReadOnly]
    [LabelText("Subscriber Count")]
    public int SubscriberCount =>
        listeners?.GetInvocationList().Length ?? 0;

    [BoxGroup("Runtime")]
    [ShowInInspector]
    [TableList(
        AlwaysExpanded = true,
        DrawScrollView = false)]
    [LabelText("Subscribers")]
    private List<SubscriberInfo> SubscriberList
    {
        get
        {
            var result = new List<SubscriberInfo>();

            if (listeners == null)
                return result;

            foreach (Delegate listener in listeners.GetInvocationList())
            {
                string objectName = "-";
                string componentName = "Unknown";

                if (listener.Target is Component component)
                {
                    objectName = component.gameObject.name;
                    componentName = component.GetType().Name;
                }
                else if (listener.Target is ScriptableObject so)
                {
                    objectName = so.name;
                    componentName = so.GetType().Name;
                }
                else if (listener.Target != null)
                {
                    componentName =
                        listener.Target.GetType().Name;
                }
                else
                {
                    objectName = "Static";

                    componentName =
                        listener.Method.DeclaringType?.Name
                        ?? "Unknown";
                }

                result.Add(new SubscriberInfo
                {
                    GameObject = objectName,
                    Component = componentName,
                    Method = listener.Method.Name
                });
            }

            return result;
        }
    }

    public void Subscribe(Action<PulseData> listener)
    {
        if (listener == null)
            return;

        // 중복 구독 방지
        listeners -= listener;
        listeners += listener;
    }

    public void Unsubscribe(Action<PulseData> listener)
    {
        if (listener == null)
            return;

        listeners -= listener;
    }

    public void Raise(PulseData pulse)
    {
        if (listeners == null)
            return;

        Delegate[] invocationList =
            listeners.GetInvocationList();

        foreach (Delegate listener in invocationList)
        {
            try
            {
                ((Action<PulseData>)listener).Invoke(pulse);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    [BoxGroup("Debug")]
    [Button("Clear Runtime Subscribers")]
    private void ClearSubscribers()
    {
        listeners = null;
    }

    [Serializable]
    private sealed class SubscriberInfo
    {
        [ReadOnly]
        [TableColumnWidth(140)]
        public string GameObject;

        [ReadOnly]
        [TableColumnWidth(180)]
        public string Component;

        [ReadOnly]
        public string Method;
    }
}