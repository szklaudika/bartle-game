using UnityEngine;

// Zrezaný strom: nakloní sa, zrýchľuje pád, po dopade chvíľu leží a potom sa prepadne do zeme.
public class FallingTree : MonoBehaviour
{
    public float fallAngle = 86f;
    public float angularAcceleration = 70f;  // stupne / s²
    public float lieTime = 6f;
    public float sinkTime = 3f;
    public float sinkDepth = 3f;

    private Vector3 _pivot;
    private Vector3 _axis;
    private float _angle;
    private float _angularSpeed = 4f;
    private float _lieTimer;
    private float _sinkTimer;

    public void Begin(Vector3 fallDirection)
    {
        fallDirection.y = 0f;
        if (fallDirection.sqrMagnitude < 0.001f) fallDirection = Random.insideUnitSphere;
        fallDirection.y = 0f;

        _pivot = transform.position;
        _axis = Vector3.Cross(Vector3.up, fallDirection.normalized);
    }

    private void Update()
    {
        if (_angle < fallAngle)
        {
            _angularSpeed += angularAcceleration * Time.deltaTime;
            float step = Mathf.Min(_angularSpeed * Time.deltaTime, fallAngle - _angle);
            transform.RotateAround(_pivot, _axis, step);
            _angle += step;
            return;
        }

        if (_lieTimer < lieTime)
        {
            _lieTimer += Time.deltaTime;
            return;
        }

        _sinkTimer += Time.deltaTime;
        transform.position += Vector3.down * (sinkDepth / sinkTime) * Time.deltaTime;
        if (_sinkTimer >= sinkTime) Destroy(gameObject);
    }
}
