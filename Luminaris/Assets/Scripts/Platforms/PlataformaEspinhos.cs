using UnityEngine;
using System.Collections;

public class PlataformaEspinhos : MonoBehaviour
{
    public int damage = 1;
    public float animationInterval = 2f; // pausa
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        StartCoroutine(PlayAnimationWithPauseLoop());
    }

    private IEnumerator PlayAnimationWithPauseLoop()
    {
        while (true)
        {
            animator.Play("Espinhos", 0, 0f); // Toca a animacao do inicio
            yield return new WaitForSeconds(GetAnimationClipLength("Espinhos"));
            yield return new WaitForSeconds(animationInterval); // Espera entre as anima��es
        }
    }

    private float GetAnimationClipLength(string clipName)
    {
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
            {
                return clip.length;
            }
        }

        Debug.LogWarning("Animation clip not found: " + clipName);
        return 1f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            HandlePlayerBounce(collision.gameObject);
        }

        var respawn = collision.GetComponent<PlayerRespawn>();
        if (respawn != null)
        {
            respawn.Die();
        }
    }

    private void HandlePlayerBounce(GameObject player)
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }
    }
}