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
    private Transform espinhoTransform;
    private NetworkAnimator networkAnimator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        espinhoTransform = transform;

        if (IsServer)
            StartCoroutine(AnimationLoop());
    }

    private IEnumerator AnimationLoop()
    {
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            // Dispara trigger para o NetworkAnimator sincronizar
            networkAnimator.SetTrigger("Activate");

            // Toca som apenas no servidor (evita duplicar áudio nos clients)
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
            // respawn.Die(); // mantenha desativado se o sistema de respawn ainda não estiver pronto
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
