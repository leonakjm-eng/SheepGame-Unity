using UnityEngine;

public interface IAgent
{
    void OnDirectClick();
    void OnNearClick(Vector3 point);
}
