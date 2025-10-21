using System.Collections;
using UnityEngine;

public class PlataformaInstavel : MonoBehaviour, IResettable
{
    [SerializeField] private float fallWait = 2f;
    [SerializeField] private float respawnWait = 2f;
    [SerializeField] private float invisibleWait = 1f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.05f;
    [SerializeField] private float shakeFrequency = 40f;

    private bool isFalling;
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private Vector3 startPos;
    private Quaternion startRot;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        startPos = transform.position;
        startRot = transform.rotation;

        //GameManager.Instance.RegisterResettable(this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isFalling && collision.gameObject.CompareTag("Player"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // Apenas ativa se o Player encostou por cima
                if (contact.normal.y < -0.5f)
                {
                    StartCoroutine(Fall());
                    break;
                }
            }
        }
    }

    private IEnumerator Fall()
    {
        isFalling = true;
        yield return StartCoroutine(Shake(shakeDuration, shakeMagnitude, shakeFrequency));
        yield return new WaitForSeconds(fallWait);

        rb.bodyType = RigidbodyType2D.Dynamic;
        col.enabled = false;

        yield return new WaitForSeconds(respawnWait);
        sr.enabled = false;

        yield return new WaitForSeconds(invisibleWait);
        ResetState();
    }

    private IEnumerator Shake(float duration, float magnitude, float frequency)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Mathf.Sin(elapsed * frequency) * magnitude;
            float y = Mathf.Cos(elapsed * (frequency * 0.5f)) * magnitude * 0.5f;

            transform.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }

    public void ResetState()
    {
        StopAllCoroutines();
        isFalling = false;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        transform.position = startPos;
        transform.rotation = startRot;

        col.enabled = true;
        sr.enabled = true;
    }
}