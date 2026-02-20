using UnityEngine;

public class TargetZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Sheep"))
        {
            SheepController sheep = other.GetComponentInParent<SheepController>();
            // If the collider is on the child model, we might need to get component in parent.
            // FDS 4.1 says Sheep_Container (Root) has Rigidbody/Collider.
            // So 'other' should be the root's collider.

            if (sheep == null)
            {
                sheep = other.GetComponent<SheepController>();
            }

            if (sheep != null && !sheep.IsSafe)
            {
                sheep.SetSafeState(true);
                GameManager.Instance.AddLiveCount();
            }
        }
    }
}
