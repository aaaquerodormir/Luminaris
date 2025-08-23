using UnityEngine;
using System.Collections;

public class DropPlataforma : MonoBehaviour
{
    [SerializeField] private float reenableDelay = 0.5f; // Tempo configurável no Inspector
    [SerializeField] private string playerTag = "Player"; // Tag do jogador

    private Collider2D _collider;
    private bool _playerOnPlatform;
    private bool _isCoroutineRunning;

    private void Start()
    {
        _collider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (_playerOnPlatform && Input.GetAxisRaw("Vertical") < 0 && !_isCoroutineRunning)
        {
            // Desabilita o collider para deixar o player cair
            _collider.enabled = false;
            StartCoroutine(EnableCollider());
        }
    }

    private IEnumerator EnableCollider()
    {
        _isCoroutineRunning = true;
        yield return new WaitForSeconds(reenableDelay);
        _collider.enabled = true;
        _isCoroutineRunning = false;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag(playerTag))
        {
            _playerOnPlatform = true;
        }
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag(playerTag))
        {
            _playerOnPlatform = false;
        }
    }
}
