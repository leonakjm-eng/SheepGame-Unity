using System.Collections;
using UnityEngine;

public class WolfController : MonoBehaviour, IAgent
{
    [Header("Settings")]
    public float moveSpeed = 4f;
    public float panicDuration = 1f;

    private Vector3 _direction;
    private bool _isPanic = false;
    private float _currentSpeed;
    private Animator _animator;
    private Rigidbody _rigidbody;
    private float _wanderTimer;

    private void Start()
    {
        _currentSpeed = moveSpeed;
        Vector2 rnd = Random.insideUnitCircle;
        _direction = new Vector3(rnd.x, 0, rnd.y).normalized;
        _wanderTimer = Random.Range(1.0f, 2.0f);

        _animator = GetComponentInChildren<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody != null) _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Update()
    {
        // Wandering Logic
        if (!_isPanic)
        {
            _wanderTimer -= Time.deltaTime;
            if (_wanderTimer <= 0)
            {
                Vector2 rnd = Random.insideUnitCircle;
                _direction = new Vector3(rnd.x, 0, rnd.y).normalized;
                _wanderTimer = Random.Range(1.0f, 2.0f);
            }
        }

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

        if (!_isPanic)
        {
            AvoidZone();
        }

        Vector3 nextPos = _rigidbody.position + (_direction * _currentSpeed * Time.fixedDeltaTime);
        _rigidbody.MovePosition(nextPos);

        if (_direction != Vector3.zero)
        {
            Vector3 lookDir = new Vector3(_direction.x, 0, _direction.z).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        }
    }

    private void AvoidZone()
    {
        if (Physics.Raycast(transform.position, _direction, out RaycastHit hit, 5f, 1 << LayerMask.NameToLayer("Zone")))
        {
            Vector3 normal = hit.normal;
            Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;

            if (Vector3.Dot(tangent, _direction) < 0)
            {
                tangent = -tangent;
            }

            _direction = Vector3.Lerp(_direction, tangent, Time.fixedDeltaTime * 10f).normalized;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Sheep"))
        {
            SheepController sheep = collision.gameObject.GetComponent<SheepController>();

            if (sheep != null && !sheep.IsSafe)
            {
                Destroy(collision.gameObject);
                GameManager.Instance.AddDeathCount();
            }
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            Vector2 rnd = Random.insideUnitCircle;
            _direction = new Vector3(rnd.x, 0, rnd.y).normalized;
        }
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
