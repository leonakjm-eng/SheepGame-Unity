using System.Collections;
using System.Linq;
using UnityEngine;

public class WolfController : MonoBehaviour, IAgent
{
    [Header("Settings")]
    public float moveSpeed = 4f; // Faster than sheep
    public float panicDuration = 1f;
    public float detectionRadius = 20f; // Limit search? FDS says "closest".

    private Vector3 _direction;
    private bool _isPanic = false;
    private float _currentSpeed;

    private void Start()
    {
        _currentSpeed = moveSpeed;
        _direction = Vector3.forward; // Default
    }

    private void Update()
    {
        if (!_isPanic)
        {
            Hunt();
            AvoidZone();
        }

        transform.Translate(_direction * _currentSpeed * Time.deltaTime, Space.World);

        if (_direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(_direction);
        }
    }

    private void Hunt()
    {
        SheepController[] sheep = FindObjectsOfType<SheepController>();
        // Filter out dead or safe? FDS: "Closest Sheep". Doesn't specify safe ones are immune.
        // Assuming Safe sheep are still targets but protected by zone logic (Wolf avoids zone).

        if (sheep.Length == 0) return;

        SheepController nearest = null;
        float minDst = float.MaxValue;

        foreach (var s in sheep)
        {
            float dst = Vector3.Distance(transform.position, s.transform.position);
            if (dst < minDst)
            {
                minDst = dst;
                nearest = s;
            }
        }

        if (nearest != null)
        {
            Vector3 toSheep = (nearest.transform.position - transform.position).normalized;
            toSheep.y = 0;
            _direction = toSheep;
        }
    }

    private void AvoidZone()
    {
        // Check if Zone is ahead
        // Layer: Zone
        int zoneLayer = LayerMask.GetMask("Zone");
        // Or string
        // Raycast
        if (Physics.Raycast(transform.position, _direction, out RaycastHit hit, 5f, 1 << LayerMask.NameToLayer("Zone")))
        {
            // Tangent: Rotate 90 degrees?
            // Or use normal from hit? Sphere tangent.
            Vector3 normal = hit.normal;
            // Cross with Up to get tangent
            Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;

            // Choose tangent closer to current direction?
            if (Vector3.Dot(tangent, _direction) < 0)
            {
                tangent = -tangent;
            }

            _direction = Vector3.Lerp(_direction, tangent, Time.deltaTime * 10f); // Smooth turn
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Sheep"))
        {
            Destroy(collision.gameObject);
            GameManager.Instance.AddDeathCount();
        }
    }

    // IAgent Implementation
    public void OnDirectClick()
    {
        Vector2 rnd = Random.insideUnitCircle;
        _direction = new Vector3(rnd.x, 0, rnd.y).normalized;
        StartCoroutine(PanicRoutine());
    }

    public void OnNearClick(Vector3 point)
    {
        Vector3 away = transform.position - point;
        away.y = 0;
        _direction = away.normalized;
        StartCoroutine(PanicRoutine());
    }

    private IEnumerator PanicRoutine()
    {
        if (_isPanic) yield break;

        _isPanic = true;
        _currentSpeed = moveSpeed * 2f;

        yield return new WaitForSeconds(panicDuration);

        _currentSpeed = moveSpeed;
        _isPanic = false;
    }
}
