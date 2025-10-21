using UnityEngine;
using System.Collections;

public class PlataformaEspinhos : MonoBehaviour
{
    [SerializeField] private float cycleInterval = 2f;   // Tempo total do ciclo (sobe + desce)
    [SerializeField] private float initialDelay = 0f;    // Atraso inicial
    [SerializeField] private float detectionRadius = 6f; // Raio para ouvir o som
    [SerializeField] private LayerMask playerMask;       // Layer dos jogadores

    private Animator animator;
    private Transform espinhoTransform;

    private void Start()
    {
        animator = GetComponent<Animator>();
        espinhoTransform = transform;
        StartCoroutine(AnimationLoop());
    }

    private IEnumerator AnimationLoop()
    {
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            // dispara a animação dos espinhos
            animator.Play("Espinhos", 0, 0f);

            // sincroniza o som apenas quando começa a subir
            if (IsPlayerNearby())
            {
                AudioManager.Instance.PlaySound("Espinho");
            }

            // espera até o próximo ciclo
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
            //respawn.Die();
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
