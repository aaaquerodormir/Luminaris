using UnityEngine;
using System.Collections;

public class PlataformaEspinhos : MonoBehaviour
{
    [SerializeField] private float cycleInterval = 2f; // Tempo total do ciclo (animação + pausa)
    [SerializeField] private float initialDelay = 0f;  // Pausa inicial para desincronizar

    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        StartCoroutine(AnimationLoop());
    }

    private IEnumerator AnimationLoop()
    {
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            animator.Play("Espinhos", 0, 0f);
            yield return new WaitForSeconds(cycleInterval);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var respawn = collision.GetComponentInParent<PlayerRespawn>();
        if (respawn != null)
        {
            respawn.Die();
        }

        HandlePlayerBounce(collision.attachedRigidbody);
    }

    private void HandlePlayerBounce(Rigidbody2D rb)
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
    }
}