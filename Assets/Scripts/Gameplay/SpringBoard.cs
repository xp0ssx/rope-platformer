using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(LinkableObject))]
public sealed class SpringBoard : MonoBehaviour
{
    [SerializeField] private float bounceImpulse = 15f;
    [SerializeField] private float minimumUpVelocity = 10f;
    [SerializeField] private Vector2 launchDirection = Vector2.up;
    [SerializeField] private float cooldown = 0.2f;
    [SerializeField] private bool launchOnStay = true;
    [FormerlySerializedAs("playerObjectName")]
    [SerializeField] private string targetObjectName = "Player";

    private LinkableObject linkableObject;
    private float lastBounceTime;

    private void Awake()
    {
        linkableObject = GetComponent<LinkableObject>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryBounce(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!launchOnStay)
        {
            return;
        }

        TryBounce(collision);
    }

    private void TryBounce(Collision2D collision)
    {
        if (!PhysicalLink.HasSpringLink(linkableObject))
        {
            return;
        }

        if (Time.time < lastBounceTime + cooldown)
        {
            return;
        }

        Rigidbody2D otherBody = collision.rigidbody;

        if (otherBody == null || otherBody.gameObject.name != targetObjectName)
        {
            return;
        }

        if (!IsContactFromAbove(collision))
        {
            return;
        }

        Vector2 direction = launchDirection.sqrMagnitude > 0.01f ? launchDirection.normalized : Vector2.up;
        Vector2 velocity = otherBody.linearVelocity;
        float velocityAlongLaunchDirection = Vector2.Dot(velocity, direction);

        if (velocityAlongLaunchDirection < minimumUpVelocity)
        {
            velocity += direction * (minimumUpVelocity - velocityAlongLaunchDirection);
        }

        otherBody.linearVelocity = velocity;
        otherBody.AddForce(direction * bounceImpulse, ForceMode2D.Impulse);

        lastBounceTime = Time.time;
    }

    private static bool IsContactFromAbove(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);

            if (contact.normal.y < -0.35f)
            {
                return true;
            }
        }

        return false;
    }
}
