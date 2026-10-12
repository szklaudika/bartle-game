using UnityEngine;

// Jednoduchá kódová animácia pre drevorubača poskladaného z primitív
// (chôdza, trasenie píly, výpady pri útoku, pád pri smrti).
// Pri použití skutočného modelu s Animatorom tento komponent stačí odstrániť.
[RequireComponent(typeof(TreeCutterAI))]
public class TreeCutterProceduralAnimation : MonoBehaviour
{
    public Transform body;        // všetko okrem koreňa – pri smrti padá
    public Transform leftLeg;
    public Transform rightLeg;
    public Transform arms;        // ruky + píla
    public Transform chainsaw;
    public Transform chain;       // reťaz na lište – posúva sa pri bežiacej píle

    public float strideAngle = 35f;
    public float strideFrequency = 1.6f;   // kroky za sekundu pri rýchlosti 1 m/s
    public float sawShake = 0.012f;
    public float chainToothSpacing = 0.05f;

    private TreeCutterAI _ai;
    private float _walkPhase;
    private float _deathTime = -1f;
    private Vector3 _bodyPos, _armsPos, _sawPos;
    private Quaternion _bodyRot, _armsRot;
    private Vector3 _chainBasePos;

    private void Awake()
    {
        _ai = GetComponent<TreeCutterAI>();
        if (body != null) { _bodyPos = body.localPosition; _bodyRot = body.localRotation; }
        if (arms != null) { _armsPos = arms.localPosition; _armsRot = arms.localRotation; }
        if (chainsaw != null) _sawPos = chainsaw.localPosition;
        if (chain != null) _chainBasePos = chain.localPosition;
    }

    private void LateUpdate()
    {
        if (_ai.CurrentState == TreeCutterAI.State.Dead)
        {
            AnimateDeath();
            return;
        }

        float speed = _ai.CurrentSpeed;
        _walkPhase += Time.deltaTime * speed * strideFrequency * Mathf.PI;
        float moving = Mathf.Clamp01(speed / 2f);
        float swing = Mathf.Sin(_walkPhase) * strideAngle * moving;

        if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
        if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);

        if (body != null)
        {
            float bob = Mathf.Abs(Mathf.Cos(_walkPhase)) * 0.05f * moving;
            body.localPosition = _bodyPos + Vector3.up * bob;
        }

        if (arms != null)
        {
            Quaternion armPose = _armsRot;
            Vector3 armOffset = Vector3.zero;

            switch (_ai.CurrentState)
            {
                case TreeCutterAI.State.Cutting:
                    // píla vodorovne do kmeňa, pomaly sa tlačí dopredu a späť
                    armPose *= Quaternion.Euler(10f, 0f, Mathf.Sin(Time.time * 2f) * 6f);
                    armOffset = Vector3.forward * (0.1f + Mathf.Sin(Time.time * 3f) * 0.05f);
                    break;
                case TreeCutterAI.State.Attacking:
                    // výpady pílou proti hráčovi
                    float thrust = Mathf.Abs(Mathf.Sin(Time.time * 7f));
                    armPose *= Quaternion.Euler(-25f * thrust, Mathf.Sin(Time.time * 4f) * 20f, 0f);
                    armOffset = Vector3.forward * (0.25f * thrust);
                    break;
                default:
                    armPose *= Quaternion.Euler(Mathf.Sin(_walkPhase * 2f) * 4f * moving, 0f, 0f);
                    break;
            }

            arms.localRotation = Quaternion.Slerp(arms.localRotation, armPose, Time.deltaTime * 12f);
            arms.localPosition = Vector3.Lerp(arms.localPosition, _armsPos + armOffset, Time.deltaTime * 12f);
        }

        if (chainsaw != null)
        {
            float shake = _ai.IsRevving ? sawShake * 2f : sawShake * 0.5f;
            chainsaw.localPosition = _sawPos + Random.insideUnitSphere * shake;
        }

        if (chain != null)
        {
            float chainSpeed = _ai.IsRevving ? 3f : 0.4f; // m/s
            float offset = Mathf.Repeat(Time.time * chainSpeed, chainToothSpacing);
            chain.localPosition = _chainBasePos + Vector3.forward * offset;
        }
    }

    private void AnimateDeath()
    {
        if (_deathTime < 0f) _deathTime = Time.time;
        if (body == null) return;

        // prevráti sa dozadu a píla mu vypadne z rúk
        float t = Mathf.Clamp01((Time.time - _deathTime) / 0.7f);
        float eased = t * t;
        body.localRotation = _bodyRot * Quaternion.Euler(-85f * eased, 0f, 0f);
        body.localPosition = _bodyPos + Vector3.back * 0.3f * eased + Vector3.up * 0.15f * eased;

        if (chainsaw != null)
            chainsaw.localPosition = _sawPos + Vector3.down * 0.4f * eased;
    }
}
