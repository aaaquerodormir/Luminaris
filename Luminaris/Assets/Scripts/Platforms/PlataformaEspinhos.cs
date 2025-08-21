using UnityEngine;

public class PlataformaEspinhos : MonoBehaviour
{
    public float bounceForce = 10f;
    public int damage = 1;

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

            rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
        }
    }
}
