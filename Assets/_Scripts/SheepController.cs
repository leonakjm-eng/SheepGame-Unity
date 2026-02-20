using System.Collections;
using UnityEngine;

public class SheepController : MonoBehaviour, IAgent
{
    [Header("Settings")]
    public float moveSpeed = 3f;
    public float panicDuration = 1f;

    private Vector3 _direction;
    private bool _isSafe = false;
    private bool _isPanic = false;
    private float _currentSpeed;
    private Transform _targetZoneTransform;
    private float _targetZoneRadius;
    private Animator _animator;

    public bool IsSafe => _isSafe;

    private void Start()
    {
        _currentSpeed = moveSpeed;
        Vector2 rnd = Random.insideUnitCircle;
        _direction = new Vector3(rnd.x, 0, rnd.y).normalized;

        // Requirement 3: Cache Animator
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (_isSafe && _targetZoneTransform != null)
        {
            // Clamp within zone
            float dist = Vector3.Distance(transform.position, _targetZoneTransform.position);
            if (dist > _targetZoneRadius)
            {
                Vector3 toCenter = (_targetZoneTransform.position - transform.position).normalized;
                _direction = Vector3.Lerp(_direction, toCenter, Time.deltaTime * 5f);
            }
        }

        transform.Translate(_direction * _currentSpeed * Time.deltaTime, Space.World);

        if (_direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        // Requirement 3: Update Animation
        if (_animator != null)
        {
            _animator.SetFloat("Vert", _currentSpeed);
            // Assuming State 1 is Move, 0 is Idle.
            _animator.SetInteger("State", _currentSpeed > 0.1f ? 1 : 0);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            if (collision.contacts.Length > 0)
            {
                Vector3 normal = collision.contacts[0].normal;
                _direction = Vector3.Reflect(_direction, normal).normalized;
                _direction.y = 0;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Detect Zone Layer
        if (other.gameObject.layer == LayerMask.NameToLayer("Zone"))
        {
            _isSafe = true;
            _targetZoneTransform = other.transform;
            SphereCollider sc = other.GetComponent<SphereCollider>();
            if (sc != null)
            {
                _targetZoneRadius = sc.radius * other.transform.lossyScale.x;
            }
        }
    }

    // Called by TargetZone.cs
    public void SetSafeState(bool safe)
    {
        _isSafe = safe;
        // Note: transform/radius might not be set yet if OnTriggerEnter hasn't fired.
        // But OnTriggerEnter will fire if physics is working.
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
        // FDS: Panic (1s) Speed 2x. End -> Return to normal.
        if (_isPanic) yield break; // Or reset? FDS implies state.

        _isPanic = true;
        _currentSpeed = moveSpeed * 2f;

        yield return new WaitForSeconds(panicDuration);

        _currentSpeed = moveSpeed;
        _isPanic = false;
    }
}
