using UnityEngine;

public class LavaRise : MonoBehaviour
{
    [Header("Velocidade Base da Lava")]
    [SerializeField] private float baseSpeed = 1f;

    [Header("Multiplicador Dinâmico")]
    [SerializeField] private float speedMultiplier = 1f;

    [Header("Jogadores")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    [Header("Acompanhamento")]
    [SerializeField] private float maxPlayerHeightOffset = 5f;

    private void Update()
    {
        if (player1 == null || player2 == null)
        {
            Debug.LogWarning("Player references not set in LavaRise script.");
            return; // Evita erros se os jogadores não estiverem atribuídos
        }

        // Calcula a altura mais alta entre os dois jogadores
        float highestY = Mathf.Max(player1.position.y, player2.position.y);

        // Define a altura alvo que a lava deve alcançar (um pouco abaixo do jogador mais alto)
        float targetY = highestY - maxPlayerHeightOffset;

        // Só sobe se a lava estiver abaixo do alvo
        if (transform.position.y < targetY)
        {
            // Calcula a velocidade dinâmica (base vezes multiplicador)
            float dynamicSpeed = baseSpeed * speedMultiplier;

            // Move a lava para cima suavemente
            transform.position += Vector3.up * dynamicSpeed * Time.deltaTime;

            // Evita ultrapassar o targetY
            if (transform.position.y > targetY)
            {
                transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
            }
        }
    }
}
