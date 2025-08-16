using System.Collections;
using UnityEngine;

public class PlataformaInstavel : MonoBehaviour
{
    public float fallwait = 2f;
    public float destoryWait = 1f;

    bool isfalling;
    Rigidbody2D rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();  
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(!isfalling && collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(Fall());
        }
    }

    private IEnumerator Fall()
    {
        isfalling = true;
        yield return new WaitForSeconds(fallwait);
        rb.bodyType = RigidbodyType2D.Dynamic;
        Destroy(gameObject, destoryWait);
    }
    void Update()
    {
        
    }
}
