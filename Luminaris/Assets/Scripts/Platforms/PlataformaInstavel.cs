using System.Collections;
using UnityEngine;

public class PlataformaInstavel : MonoBehaviour, IResettable
{
    public float fallwait = 2f;      // tempo antes da queda
    public float destoryWait = 1f;   // tempo antes de destruir (não usado mais)

    private bool isfalling;
    private Rigidbody2D rb;
    private Vector3 startPos;
    private Quaternion startRot;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
        startRot = transform.rotation;

        // Registra no GameManager
        GameManager.Instance.RegisterResettable(this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isfalling && collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(Fall());
        }
    }

    private IEnumerator Fall()
    {
        isfalling = true;
        yield return new WaitForSeconds(fallwait);

        // Faz a plataforma cair
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    // Reset da plataforma para o estado inicial
    public void ResetState()
    {
        StopAllCoroutines();
        isfalling = false;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        transform.position = startPos;
        transform.rotation = startRot;

        gameObject.SetActive(true);
    }
}
