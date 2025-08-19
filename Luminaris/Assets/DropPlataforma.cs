using UnityEngine;

public class DropPlataforma : MonoBehaviour
{
    private Collider2D _collider;
    private bool _playerOnPlatform;
    private void Start()
    {
        _collider = GetComponent<Collider2D>();
    }

    // Update is called once per frame
    private void SetPlayerOnPlatform(Collision2D other, bool value)
    {
        var player = other.gameObject.GetComponent<Player>();
        if (player != null)
        {
            _playerOnPlatform = value;
        }
    }
}
