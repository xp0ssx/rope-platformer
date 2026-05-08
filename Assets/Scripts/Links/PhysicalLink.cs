using UnityEngine;

public sealed class PhysicalLink : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private LinkableObject first;
    [SerializeField] private LinkableObject second;
    [SerializeField] private LinkType linkType;

    [Header("Physics")]
    [SerializeField] private float targetLength = 2f;
    [SerializeField] private float breakStretchMultiplier = 1.8f;
    [SerializeField] private float maxForceBeforeBreak = 800f;
    [SerializeField] private float overloadBreakDelay = 0.35f;
    [SerializeField] private float springFrequency = 3f;
    [SerializeField] private float springDampingRatio = 0.35f;

    [Header("Visual")]
    [SerializeField] private float lineWidth = 0.06f;
    [SerializeField] private float overloadShake = 0.08f;

    private Joint2D joint;
    private LineRenderer line;
    private float load01;
    private float overloadTimer;

    public void Initialize(LinkableObject firstObject, LinkableObject secondObject, LinkType type)
    {
        first = firstObject;
        second = secondObject;
        linkType = type;
        targetLength = Vector2.Distance(first.LinkPosition, second.LinkPosition);

        CreateJoint();
        CreateLine();
    }

    private void Update()
    {
        if (first == null || second == null || joint == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdateLoad();
        UpdateLine();

        if (load01 >= 1f)
        {
            overloadTimer += Time.deltaTime;
        }
        else
        {
            overloadTimer = 0f;
        }

        if (overloadTimer >= overloadBreakDelay)
        {
            Break();
        }
    }

    private void CreateJoint()
    {
        if (linkType == LinkType.Rigid)
        {
            DistanceJoint2D distanceJoint = first.gameObject.AddComponent<DistanceJoint2D>();
            distanceJoint.connectedBody = second.Body;
            distanceJoint.autoConfigureDistance = false;
            distanceJoint.distance = targetLength;
            distanceJoint.enableCollision = true;
            joint = distanceJoint;
            return;
        }

        SpringJoint2D springJoint = first.gameObject.AddComponent<SpringJoint2D>();
        springJoint.connectedBody = second.Body;
        springJoint.autoConfigureDistance = false;
        springJoint.distance = targetLength;
        springJoint.frequency = springFrequency;
        springJoint.dampingRatio = springDampingRatio;
        springJoint.enableCollision = true;
        joint = springJoint;
    }

    private void CreateLine()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.numCapVertices = 4;
    }

    private void UpdateLoad()
    {
        float currentLength = Vector2.Distance(first.LinkPosition, second.LinkPosition);
        float stretchLimit = Mathf.Max(targetLength * breakStretchMultiplier, targetLength + 0.01f);
        float stretchLoad = Mathf.InverseLerp(targetLength, stretchLimit, currentLength);

        float reactionForce = joint.GetReactionForce(Time.fixedDeltaTime).magnitude;
        float forceLoad = Mathf.InverseLerp(0f, maxForceBeforeBreak, reactionForce);

        load01 = Mathf.Clamp01(Mathf.Max(stretchLoad, forceLoad));
    }

    private void UpdateLine()
    {
        Vector3 start = first.LinkPosition;
        Vector3 end = second.LinkPosition;

        if (load01 > 0.65f)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = new(-direction.y, direction.x, 0f);
            float shake = overloadShake * Mathf.InverseLerp(0.65f, 1f, load01);
            Vector3 offset = perpendicular * Random.Range(-shake, shake);
            start += offset;
            end -= offset;
        }

        line.SetPosition(0, start);
        line.SetPosition(1, end);

        Color color = GetLoadColor(load01);
        line.startColor = color;
        line.endColor = color;
    }

    private static Color GetLoadColor(float load)
    {
        if (load < 0.5f)
        {
            return Color.Lerp(Color.green, Color.yellow, load / 0.5f);
        }

        return Color.Lerp(Color.yellow, Color.red, (load - 0.5f) / 0.5f);
    }

    private void Break()
    {
        if (joint != null)
        {
            Destroy(joint);
        }

        Destroy(gameObject);
    }
}
