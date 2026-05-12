using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new(0f, 2f, -12f);
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float verticalDeadZone = 1.3f;
    [SerializeField] private bool enableLookUp = true;
    [SerializeField] private float lookUpOffset = 2.8f;
    [SerializeField] private float lookUpSmoothTime = 0.12f;
    [SerializeField] private bool enableMouseLook = true;
    [SerializeField] private Vector2 maxMouseLookOffset = new(5.5f, 2.2f);
    [SerializeField] private float mouseLookDeadZone = 0.55f;
    [SerializeField] private float mouseLookFullOffsetAt = 0.95f;
    [SerializeField] private float mouseLookPower = 1.4f;
    [SerializeField] private float mouseLookSmoothTime = 0.12f;

    [Header("World Bounds")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 worldMin = new(-10f, -7f);
    [SerializeField] private Vector2 worldMax = new(36f, 8f);

    private Camera viewCamera;
    private Vector3 velocity;
    private float verticalFocusY;
    private float lookUpCurrentOffset;
    private float lookUpVelocity;
    private Vector2 mouseLookCurrentOffset;
    private Vector2 mouseLookVelocity;
    private bool hasVerticalFocus;

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float playerTargetY = target.position.y + offset.y;
        UpdateVerticalFocus(playerTargetY);

        Vector2 mouseLookOffset = ReadMouseLookOffset();

        Vector3 desiredPosition = new(
            target.position.x + offset.x + mouseLookOffset.x,
            verticalFocusY + ReadLookUpOffset() + mouseLookOffset.y,
            target.position.z + offset.z);

        if (useBounds)
        {
            desiredPosition = ClampToWorldBounds(desiredPosition);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }

    private void UpdateVerticalFocus(float playerTargetY)
    {
        if (!hasVerticalFocus)
        {
            verticalFocusY = playerTargetY;
            hasVerticalFocus = true;
            return;
        }

        float delta = playerTargetY - verticalFocusY;

        if (delta > verticalDeadZone)
        {
            verticalFocusY = playerTargetY - verticalDeadZone;
        }
        else if (delta < -verticalDeadZone)
        {
            verticalFocusY = playerTargetY + verticalDeadZone;
        }
    }

    private float ReadLookUpOffset()
    {
        float targetLookUpOffset = 0f;

        if (enableLookUp && Keyboard.current != null && Keyboard.current.tabKey.isPressed)
        {
            targetLookUpOffset = lookUpOffset;
        }

        lookUpCurrentOffset = Mathf.SmoothDamp(
            lookUpCurrentOffset,
            targetLookUpOffset,
            ref lookUpVelocity,
            lookUpSmoothTime);

        return lookUpCurrentOffset;
    }

    private Vector2 ReadMouseLookOffset()
    {
        Vector2 targetOffset = Vector2.zero;

        if (enableMouseLook && Mouse.current != null && viewCamera != null)
        {
            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Vector3 viewportPosition = viewCamera.ScreenToViewportPoint(screenPosition);
            Vector2 normalizedMousePosition = new(
                Mathf.Clamp01(viewportPosition.x) * 2f - 1f,
                Mathf.Clamp01(viewportPosition.y) * 2f - 1f);

            normalizedMousePosition.x = ApplyEdgeLookCurve(normalizedMousePosition.x);
            normalizedMousePosition.y = ApplyEdgeLookCurve(normalizedMousePosition.y);

            targetOffset = new Vector2(
                normalizedMousePosition.x * maxMouseLookOffset.x,
                normalizedMousePosition.y * maxMouseLookOffset.y);
        }

        mouseLookCurrentOffset = Vector2.SmoothDamp(
            mouseLookCurrentOffset,
            targetOffset,
            ref mouseLookVelocity,
            mouseLookSmoothTime);

        return mouseLookCurrentOffset;
    }

    private Vector3 ClampToWorldBounds(Vector3 desiredPosition)
    {
        if (viewCamera == null || !viewCamera.orthographic)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, worldMin.x, worldMax.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, worldMin.y, worldMax.y);
            return desiredPosition;
        }

        float halfHeight = viewCamera.orthographicSize;
        float halfWidth = halfHeight * viewCamera.aspect;

        desiredPosition.x = ClampCenterAxis(desiredPosition.x, worldMin.x, worldMax.x, halfWidth);
        desiredPosition.y = ClampCenterAxis(desiredPosition.y, worldMin.y, worldMax.y, halfHeight);
        return desiredPosition;
    }

    private static float ClampCenterAxis(float value, float minEdge, float maxEdge, float halfViewSize)
    {
        float minCenter = minEdge + halfViewSize;
        float maxCenter = maxEdge - halfViewSize;

        if (minCenter > maxCenter)
        {
            return (minEdge + maxEdge) * 0.5f;
        }

        return Mathf.Clamp(value, minCenter, maxCenter);
    }

    private float ApplyEdgeLookCurve(float value)
    {
        float absoluteValue = Mathf.Abs(value);

        if (absoluteValue <= mouseLookDeadZone)
        {
            return 0f;
        }

        float sign = Mathf.Sign(value);
        float fullOffsetAt = Mathf.Max(mouseLookFullOffsetAt, mouseLookDeadZone + 0.01f);
        float edge01 = Mathf.InverseLerp(mouseLookDeadZone, fullOffsetAt, absoluteValue);
        return sign * Mathf.Pow(edge01, mouseLookPower);
    }
}
