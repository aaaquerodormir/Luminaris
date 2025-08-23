using UnityEngine;

public class LavaRise : MonoBehaviour
{
    [Header("Velocidade da Lava")]
    [SerializeField] private float baseSpeed = 0.5f; // velocidade base lenta
    [SerializeField] private float speedMultiplier = 1f; // multiplicador dinâmico

    [Header("Jogadores")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    [Header("Acompanhamento")]
    [SerializeField] private float maxPlayerHeightOffset = 5f;
    // quanto a lava pode ficar atrás do jogador mais alto (se quiser limitar)

    private float speedModifier = 1f;
    private int turnsLeft = 0;


    [SerializeField] private Vector3 startPos;

    private void Awake()
    {
        // Se não setar manualmente no Inspector, pega a posição atual da cena
        if (startPos == Vector3.zero)
            startPos = transform.position;
    }

    //private void Start()
    //{
    //    //Mudança: salvar a posição inicial e forçar a lava a começar exatamente dali

    //    startPos = transform.position;
    //    transform.position = startPos;
    //}

    //Reseta lava para posição inicial
    public void ResetLava()
    {
        transform.position = startPos;
        speedModifier = 1f;
        turnsLeft = 0;
        //transform.position = startPos;
    }

    private void Update()
    {
        if (player1 == null || player2 == null) return;

        //Calcula altura do jogador mais alto
        float highestY = Mathf.Max(player1.position.y, player2.position.y);

        //Define altura de referência(pode ser usado para limitar diferença)
        float targetY = highestY - maxPlayerHeightOffset;

        //Lava sobe sempre, lentamente
        float dynamicSpeed = baseSpeed * speedMultiplier * speedModifier;
        transform.position += Vector3.up * dynamicSpeed * Time.deltaTime;

        //Se quiser impedir que a lava fique muito distante do jogador mais alto
         //pode limitar altura mínima com base no targetY
        if (transform.position.y < targetY)
        {
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        }
    }
    public void AddSpeedModifier(float modifier, int durationTurns)
    {
        speedModifier = modifier;
        turnsLeft = durationTurns;
        Debug.Log($" Lava modificada! Novo multiplicador: {speedModifier}, duração: {turnsLeft} turnos");
    }

    public void ConsumeTurn()
    {
        if (turnsLeft > 0)
        {
            turnsLeft--;
            if (turnsLeft <= 0)
            {
                speedModifier = 1f;
                Debug.Log(" Lava voltou à velocidade normal");
            }
        }
    }
}
