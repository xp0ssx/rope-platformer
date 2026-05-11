using System.Collections.Generic;
using UnityEngine;

public sealed class PhysicalLink : MonoBehaviour
{
    private static readonly List<PhysicalLink> ActiveLinks = new();

    [Header("Objects")]
    [SerializeField] private LinkableObject first;
    [SerializeField] private LinkableObject second;
    [SerializeField] private LinkType linkType;

    [Header("Physics")]
    [SerializeField] private float targetLength = 2f;
    [SerializeField] private float minTargetLength = 0.45f;
    [SerializeField] private float maxTargetLength = 12f;
    [SerializeField] private float breakStretchMultiplier = 1.8f;
    [SerializeField] private float ropeMaxForceBeforeBreak = 900f;
    [SerializeField] private float springMaxForceBeforeBreak = 650f;
    [SerializeField] private float overloadBreakDelay = 0.35f;
    [SerializeField] private float springFrequency = 3f;
    [SerializeField] private float springDampingRatio = 0.35f;

    [Header("Visual")]
    [SerializeField] private float lineWidth = 0.06f;
    [SerializeField] private float overloadShake = 0.08f;
    [SerializeField] private float slackSag = 0.45f;

    private Joint2D joint;
    private LineRenderer line;
    private float load01;
    private float overloadTimer;
    private float currentLength;
    private bool isSlack;
    private bool isSelected;
    private float maxForceBeforeBreak;

    public LinkType Type => linkType;
    public float TargetLength => targetLength;
    public float CurrentLength => currentLength;
    public float Load01 => load01;

    public static bool TryFindNearest(Vector2 worldPosition, float maxDistance, out PhysicalLink nearestLink)
    {
        nearestLink = null;
        float bestDistance = maxDistance;

        foreach (PhysicalLink link in ActiveLinks)
        {
            if (link == null || link.first == null || link.second == null)
            {
                continue;
            }

            float distance = link.GetDistanceToVisualLine(worldPosition);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearestLink = link;
            }
        }

        return nearestLink != null;
    }

    public static void DestroyAllLinks()
    {
        PhysicalLink[] links = ActiveLinks.ToArray();

        foreach (PhysicalLink link in links)
        {
            if (link != null)
            {
                link.DestroyLink();
            }
        }
    }

    public void Initialize(LinkableObject firstObject, LinkableObject secondObject, LinkSettings settings)
    {
        first = firstObject;
        second = secondObject;
        linkType = settings.Type;
        maxForceBeforeBreak = linkType == LinkType.Rope ? ropeMaxForceBeforeBreak : springMaxForceBeforeBreak;
        SetTargetLength(Vector2.Distance(first.LinkPosition, second.LinkPosition));

        CreateJoint();
        CreateLine();
    }

    public void AdjustLength(float delta)
    {
        SetTargetLength(targetLength + delta);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateLineWidth();
    }

    public void DestroyLink()
    {
        Break();
    }

    private void OnEnable()
    {
        ActiveLinks.Add(this);
    }

    private void OnDisable()
    {
        ActiveLinks.Remove(this);
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
        if (linkType == LinkType.Rope)
        {
            DistanceJoint2D distanceJoint = first.gameObject.AddComponent<DistanceJoint2D>();
            distanceJoint.connectedBody = second.Body;
            distanceJoint.autoConfigureDistance = false;
            distanceJoint.distance = targetLength;
            distanceJoint.maxDistanceOnly = true;
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
        line.positionCount = 3;
        line.useWorldSpace = true;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.numCapVertices = 4;
        UpdateLineWidth();
    }

    private void UpdateLoad()
    {
        currentLength = Vector2.Distance(first.LinkPosition, second.LinkPosition);
        isSlack = linkType == LinkType.Rope && currentLength < targetLength * 0.98f;

        float lengthDelta = linkType == LinkType.Rope
            ? Mathf.Max(0f, currentLength - targetLength)
            : Mathf.Abs(currentLength - targetLength);

        float breakLengthDelta = Mathf.Max(targetLength * (breakStretchMultiplier - 1f), 0.01f);
        float stretchLoad = Mathf.InverseLerp(0f, breakLengthDelta, lengthDelta);

        float reactionForce = joint.GetReactionForce(Time.fixedDeltaTime).magnitude;
        float forceLoad = Mathf.InverseLerp(0f, maxForceBeforeBreak, reactionForce);

        load01 = Mathf.Clamp01(Mathf.Max(stretchLoad, forceLoad));
    }

    private void UpdateLine()
    {
        Vector3 start = first.LinkPosition;
        Vector3 end = second.LinkPosition;
        Vector3 middle = (start + end) * 0.5f;

        if (isSlack)
        {
            float slack01 = Mathf.Clamp01((targetLength - currentLength) / targetLength);
            middle += Vector3.down * slackSag * slack01;
        }

        if (load01 > 0.65f)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = new(-direction.y, direction.x, 0f);
            float shake = overloadShake * Mathf.InverseLerp(0.65f, 1f, load01);
            Vector3 offset = perpendicular * Random.Range(-shake, shake);
            start += offset;
            middle += offset;
            end -= offset;
        }

        line.SetPosition(0, start);
        line.SetPosition(1, middle);
        line.SetPosition(2, end);

        Color color = isSlack ? new Color(0.55f, 0.75f, 0.9f, 1f) : GetLoadColor(load01);
        line.startColor = color;
        line.endColor = color;
        UpdateLineWidth();
    }

    private void SetTargetLength(float length)
    {
        targetLength = Mathf.Clamp(length, minTargetLength, maxTargetLength);

        if (joint is DistanceJoint2D distanceJoint)
        {
            distanceJoint.distance = targetLength;
        }
        else if (joint is SpringJoint2D springJoint)
        {
            springJoint.distance = targetLength;
        }
    }

    private void UpdateLineWidth()
    {
        if (line == null)
        {
            return;
        }

        float width = isSelected ? lineWidth * 1.8f : lineWidth;
        line.startWidth = width;
        line.endWidth = width;
    }

    private float GetDistanceToVisualLine(Vector2 point)
    {
        if (line == null || line.positionCount < 2)
        {
            return float.PositiveInfinity;
        }

        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < line.positionCount - 1; i++)
        {
            Vector2 start = line.GetPosition(i);
            Vector2 end = line.GetPosition(i + 1);
            bestDistance = Mathf.Min(bestDistance, DistanceToSegment(point, start, end));
        }

        return bestDistance;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float segmentSqrLength = segment.sqrMagnitude;

        if (segmentSqrLength <= Mathf.Epsilon)
        {
            return Vector2.Distance(point, start);
        }

        float t = Vector2.Dot(point - start, segment) / segmentSqrLength;
        t = Mathf.Clamp01(t);
        Vector2 closestPoint = start + segment * t;
        return Vector2.Distance(point, closestPoint);
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
