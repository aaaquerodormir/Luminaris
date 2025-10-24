using UnityEngine;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlataformaEspinhos : NetworkBehaviour
{
    [SerializeField] private float cycleInterval = 2f;   // Tempo total do ciclo (sobe + desce)
    [SerializeField] private float initialDelay = 0f;    // Atraso inicial
    [SerializeField] private float detectionRadius = 6f; // Raio para ouvir o som
    [SerializeField] private LayerMask playerMask;       // Layer dos jogadores

    private Animator animator;
    private NetworkAnimator netAnimator;
    private Transform espinhoTransform;
    private static readonly int ActivateTrigger = Animator.StringToHash("Activate");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        netAnimator = GetComponent<NetworkAnimator>();
        espinhoTransform = transform;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            StartCoroutine(AnimationLoop());
    }

    private IEnumerator AnimationLoop()
    {
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            netAnimator.SetTrigger(ActivateTrigger);

            if (IsPlayerNearby())
                AudioManager.Instance.PlaySound("Espinho");

            yield return new WaitForSeconds(cycleInterval);
        }
    }

    private bool IsPlayerNearby()
    {
        Collider2D playerNearby = Physics2D.OverlapCircle(
            espinhoTransform.position,
            detectionRadius,
            playerMask
        );
        return playerNearby != null;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var respawn = collision.GetComponentInParent<PlayerRespawn>();
        if (respawn != null)
        {
            // respawn.Die();
        }

        HandlePlayerBounce(collision.attachedRigidbody);
    }

    private void HandlePlayerBounce(Rigidbody2D rb)
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
