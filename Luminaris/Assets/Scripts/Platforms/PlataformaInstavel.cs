using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlataformaInstavel : NetworkBehaviour, IResettable
{
    [Header("Timers")]
    [SerializeField] private float fallWait = 2f;
    [SerializeField] private float respawnWait = 2f;
    [SerializeField] private float invisibleWait = 1f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.05f;
    [SerializeField] private float shakeFrequency = 40f;

    private bool isFalling = false;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private NetworkTransform netTransform;

    private Vector3 startPos;
    private Quaternion startRot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        netTransform = GetComponent<NetworkTransform>();

        startPos = transform.position;
        startRot = transform.rotation;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ResetState();
    }

    public void ActivateFallFromServer()
    {
        if (!IsServer || isFalling) return;
        StartFallClientRpc();
    }

    [ClientRpc]
    private void StartFallClientRpc()
    {
        StartCoroutine(FallSequence());
    }

    private IEnumerator FallSequence()
    {
        if (isFalling) yield break;
        isFalling = true;


        yield return StartCoroutine(Shake(shakeDuration, shakeMagnitude, shakeFrequency));

        yield return new WaitForSeconds(fallWait);

        rb.bodyType = RigidbodyType2D.Dynamic;

        col.enabled = false;

        yield return new WaitForSeconds(respawnWait);

        sr.enabled = false;

        yield return new WaitForSeconds(invisibleWait);

        if (IsServer)
        {
            ResetStateClientRpc();
        }
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

    [ClientRpc]
    public void ResetStateClientRpc()
    {
        ResetState();
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

        if (IsServer && netTransform != null)
        {
            netTransform.Teleport(startPos, startRot, transform.localScale);
        }

        col.enabled = true;
        sr.enabled = true;
    }
}