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
    private Rigidbody _rigidbody;

    public bool IsSafe => _isSafe;

    private void Start()
    {
        _currentSpeed = moveSpeed;
        Vector2 rnd = Random.insideUnitCircle;
        _direction = new Vector3(rnd.x, 0, rnd.y).normalized;

        _animator = GetComponentInChildren<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody != null) _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Update()
    {
        // Constraint: Update animation based on speed
        if (_animator != null)
        {
            _animator.SetFloat("Vert", _currentSpeed);
            _animator.SetInteger("State", _currentSpeed > 0.1f ? 1 : 0);
        }
    }

    private void FixedUpdate()
    {
        if (_rigidbody == null) return;

        if (_isSafe && _targetZoneTransform != null)
        {
            float dist = Vector3.Distance(_rigidbody.position, _targetZoneTransform.position);
            if (dist > _targetZoneRadius)
            {
                Vector3 toCenter = (_targetZoneTransform.position - _rigidbody.position).normalized;
                _direction = Vector3.Lerp(_direction, toCenter, Time.fixedDeltaTime * 5f).normalized;
            }
        }

        Vector3 nextPos = _rigidbody.position + (_direction * _currentSpeed * Time.fixedDeltaTime);
        _rigidbody.MovePosition(nextPos);

        if (_direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_direction);
            _rigidbody.MoveRotation(targetRotation);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Vector2 rnd = Random.insideUnitCircle;
            _direction = new Vector3(rnd.x, 0, rnd.y).normalized;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Zone"))
        {
            // Do not set _isSafe = true here. TargetZone script handles the counting and sets it.
            // Just cache the transform/radius.
            _targetZoneTransform = other.transform;
            SphereCollider sc = other.GetComponent<SphereCollider>();
            if (sc != null)
            {
                _targetZoneRadius = sc.radius * other.transform.lossyScale.x;
            }
        }
    }

    public void SetSafeState(bool safe)
    {
        _isSafe = safe;
    }

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
