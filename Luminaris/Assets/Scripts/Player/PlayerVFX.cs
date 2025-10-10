using UnityEngine;

public class VisualEffects : MonoBehaviour
{
    public PlayerMovement pm; // Refer�ncia ao PlayerMovement
    [SerializeField]
    ParticleSystem mysticTrailPrefab;
    ParticleSystem mysticTrail;  // Prefab ou ParticleSystem j� na cena


    private void Start()
    {
        mysticTrail = Instantiate(mysticTrailPrefab, transform.position, Quaternion.identity);
        mysticTrail.transform.SetParent(transform);
    }

    //void Update()
    //{
    //    if (pm.horizontalInput == 0 || mysticTrail == null)
    //        return;



    //    // Verifica se o player est� se movendo (horizontalInput != 0) ou est� no ar
    //    bool isMoving = Mathf.Abs(pm.horizontalInput) > 0.1f;

    //    if (isMoving)
    //    {
    //        if (!mysticTrail.isPlaying)
    //            mysticTrail.Play();
    //    }
    //    else
    //    {
    //        if (mysticTrail.isPlaying)
    //            mysticTrail.Stop();
    //    }
    //}
}