using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlataformaInstavel : NetworkBehaviour, IResettable
{
    [Header("Timers")]
    [SerializeField] private float fallWait = 2f;
    [SerializeField] private float respawnWait = 2f;
    [SerializeField] private float invisibleWait = 1f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.05f;
    [SerializeField] private float shakeFrequency = 40f;

    private bool isFalling = false;

    // Componentes
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private NetworkTransform netTransform; // Componente para controle de sincroniza��o

    // Posi��o Inicial
    private Vector3 startPos;
    private Quaternion startRot;

    private void Awake()
    {
        // Busca de Componentes
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        netTransform = GetComponent<NetworkTransform>(); // Obt�m o NetworkTransform

        // A posi��o inicial � definida apenas uma vez.
        startPos = transform.position;
        startRot = transform.rotation;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Garante o estado inicial (Kinematic) em todos os lados.
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Garante que o estado de queda est� em reset
        isFalling = false;
        col.enabled = true;
        sr.enabled = true;

        // Garante que o NetworkTransform est� ligado
        if (netTransform != null) netTransform.enabled = true;
    }

    // O m�todo OnCollisionEnter2D N�O est� aqui.
    // Ele foi movido para o PlayerMovement.cs, que chama este m�todo abaixo via ServerRpc.

    // ==============================================================
    // PROBLEMA 2 RESOLVIDO: O Player (Client) notifica o Server, que chama este m�todo.
    // ==============================================================
    public void ActivateFallFromServer()
    {
        // Apenas o Server pode iniciar a queda e se ela n�o estiver em progresso
        if (!IsServer || isFalling) return;

        // O servidor notifica todos para iniciar a sequ�ncia de queda (RPC)
        StartFallClientRpc();
    }

    // 1. ClientRpc para notificar todos os clientes sobre o in�cio da queda.
    [ClientRpc]
    private void StartFallClientRpc()
    {
        // Todos iniciam a coroutine localmente
        StartCoroutine(FallSequence());
    }

    private IEnumerator FallSequence()
    {
        if (isFalling) yield break;
        isFalling = true;

        // ==============================================================
        // PROBLEMA 1 RESOLVIDO: Desativar a sincroniza��o durante o shake
        // para que o movimento local n�o seja sobrescrito pela rede.
        // ==============================================================
        if (netTransform != null) netTransform.enabled = false;

        // Efeito de shake
        yield return StartCoroutine(Shake(shakeDuration, shakeMagnitude, shakeFrequency));

        // Reativar a sincroniza��o ap�s o shake.
        if (netTransform != null) netTransform.enabled = true;

        yield return new WaitForSeconds(fallWait);

        // A simula��o de f�sica (tornar Dynamic) � APENAS no servidor.
        if (IsServer)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        // Desativado em todos para evitar ativa��o dupla e novos toques
        col.enabled = false;

        yield return new WaitForSeconds(respawnWait);

        // Esconder visualmente (em todos)
        sr.enabled = false;

        yield return new WaitForSeconds(invisibleWait);

        // O Server chama o ResetStateClientRpc para sincronizar o respawn
        if (IsServer)
        {
            ResetStateClientRpc();
        }
    }

    private IEnumerator Shake(float duration, float magnitude, float frequency)
    {
        // Guarda a posi��o original LOCAL
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Mathf.Sin(elapsed * frequency) * magnitude;
            float y = Mathf.Cos(elapsed * (frequency * 0.5f)) * magnitude * 0.5f;

            // O uso de localPosition � crucial para o shake
            transform.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Retorna a posi��o local ao original (para n�o afetar a posi��o global)
        transform.localPosition = originalPos;
    }

    // ClientRpc para notificar todos os clientes sobre o reset
    [ClientRpc]
    public void ResetStateClientRpc()
    {
        // Executa a l�gica de reset em todos os clientes
        ResetState();
    }

    // L�gica de reset
    public void ResetState()
    {
        StopAllCoroutines();
        isFalling = false;

        // A manipula��o do Rigidbody deve ser feita apenas no servidor.
        if (IsServer)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // ==============================================================
        // CORRE��O: A chamada netTransform.Teleport() s� deve ser feita
        // no lado que possui a Autoridade (o Server, neste caso).
        // ==============================================================
        if (netTransform != null)
        {
            // Aplica a posi��o e rota��o instantaneamente localmente em TODOS os lados
            transform.position = startPos;
            transform.rotation = startRot;

            // APENAS O SERVER/AUTHORITY CHAMA O TELEPORT()
            if (IsServer) // <--- Esta � a chave para corrigir o erro!
            {
                // netTransform.Teleport sincronizar� a nova posi��o com todos os Clients.
                netTransform.Teleport(startPos, startRot, transform.localScale);
            }
        }
        else
        {
            // Fallback
            transform.position = startPos;
            transform.rotation = startRot;
        }

        // Ativar/desativar componentes visuais e de colis�o para todos
        col.enabled = true;
        sr.enabled = true;
    }
}