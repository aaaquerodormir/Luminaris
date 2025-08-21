//using UnityEngine;

//public class LavaRise : MonoBehaviour
//{
//    [Header("Velocidade da Lava")]
//    [SerializeField] private float baseSpeed = 0.5f; // velocidade base lenta
//    [SerializeField] private float speedMultiplier = 1f; // multiplicador dinâmico

//    [Header("Jogadores")]
//    [SerializeField] private Transform player1;
//    [SerializeField] private Transform player2;

//    [Header("Acompanhamento")]
//    [SerializeField] private float maxPlayerHeightOffset = 5f;
//    // quanto a lava pode ficar atrás do jogador mais alto (se quiser limitar)

//    private Vector3 startPos; // posição inicial da lava

//    private void Start()
//    {
//         Salva posição inicial para reset
//        startPos = transform.position;
//    }

//     Reseta lava para posição inicial
//    public void ResetLava()
//    {
//        transform.position = startPos;
//    }

//    private void Update()
//    {
//        if (player1 == null || player2 == null) return;

//         Calcula altura do jogador mais alto
//        float highestY = Mathf.Max(player1.position.y, player2.position.y);

//         Define altura de referência (pode ser usado para limitar diferença)
//        float targetY = highestY - maxPlayerHeightOffset;

//         Lava sobe sempre, lentamente
//        float dynamicSpeed = baseSpeed * speedMultiplier;
//        transform.position += Vector3.up * dynamicSpeed * Time.deltaTime;

//         Se quiser impedir que a lava fique muito distante do jogador mais alto
//         pode limitar altura mínima com base no targetY
//        if (transform.position.y < targetY)
//        {
//            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
//        }
//    }
//}
