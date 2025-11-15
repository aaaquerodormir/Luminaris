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
    private NetworkTransform netTransform; // Componente para controle de sincronização

    // Posição Inicial
    private Vector3 startPos;
    private Quaternion startRot;

    private void Awake()
    {
        // Busca de Componentes
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        netTransform = GetComponent<NetworkTransform>(); // Obtém o NetworkTransform

        // A posição inicial é definida apenas uma vez.
        startPos = transform.position;
        startRot = transform.rotation;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Garante o estado inicial (Kinematic) em todos os lados.
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Garante que o estado de queda está em reset
        isFalling = false;
        col.enabled = true;
        sr.enabled = true;

        // Garante que o NetworkTransform está ligado
        if (netTransform != null) netTransform.enabled = true;
    }

    // O método OnCollisionEnter2D NÃO está aqui.
    // Ele foi movido para o PlayerMovement.cs, que chama este método abaixo via ServerRpc.

    // ==============================================================
    // PROBLEMA 2 RESOLVIDO: O Player (Client) notifica o Server, que chama este método.
    // ==============================================================
    public void ActivateFallFromServer()
    {
        // Apenas o Server pode iniciar a queda e se ela não estiver em progresso
        if (!IsServer || isFalling) return;

        // O servidor notifica todos para iniciar a sequência de queda (RPC)
        StartFallClientRpc();
    }

    // 1. ClientRpc para notificar todos os clientes sobre o início da queda.
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
        // PROBLEMA 1 RESOLVIDO: Desativar a sincronização durante o shake
        // para que o movimento local não seja sobrescrito pela rede.
        // ==============================================================
        if (netTransform != null) netTransform.enabled = false;

        // Efeito de shake
        yield return StartCoroutine(Shake(shakeDuration, shakeMagnitude, shakeFrequency));

        // Reativar a sincronização após o shake.
        if (netTransform != null) netTransform.enabled = true;

        yield return new WaitForSeconds(fallWait);

        // ==============================================================
        // CORREÇÃO DA QUEDA (Problema 2)
        // ==============================================================
        // A simulação de física (tornar Dynamic) deve ocorrer em TODOS os clientes.
        // A checagem "if (IsServer)" foi REMOVIDA daqui.
        rb.bodyType = RigidbodyType2D.Dynamic;
        // ==============================================================

        // Desativado em todos para evitar ativação dupla e novos toques
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
        // Guarda a posição original LOCAL
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Mathf.Sin(elapsed * frequency) * magnitude;
            float y = Mathf.Cos(elapsed * (frequency * 0.5f)) * magnitude * 0.5f;

            // O uso de localPosition é crucial para o shake
            transform.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Retorna a posição local ao original (para não afetar a posição global)
        transform.localPosition = originalPos;
    }

    // ClientRpc para notificar todos os clientes sobre o reset
    [ClientRpc]
    public void ResetStateClientRpc()
    {
        // Executa a lógica de reset em todos os clientes
        ResetState();
    }

    // Lógica de reset
    public void ResetState()
    {
        StopAllCoroutines();
        isFalling = false;

        // A manipulação do Rigidbody deve ser feita apenas no servidor.
        if (IsServer)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // ==============================================================
        // CORREÇÃO: A chamada netTransform.Teleport() só deve ser feita
        // no lado que possui a Autoridade (o Server, neste caso).
        // ==============================================================
        if (netTransform != null)
        {
            // Aplica a posição e rotação instantaneamente localmente em TODOS os lados
            transform.position = startPos;
            transform.rotation = startRot;

            // APENAS O SERVER/AUTHORITY CHAMA O TELEPORT()
            if (IsServer) // <--- Esta é a chave para corrigir o erro!
            {
                // netTransform.Teleport sincronizará a nova posição com todos os Clients.
                netTransform.Teleport(startPos, startRot, transform.localScale);
            }
        }
        else
        {
            // Fallback
            transform.position = startPos;
            transform.rotation = startRot;
        }

        // Ativar/desativar componentes visuais e de colisão para todos
        col.enabled = true;
        sr.enabled = true;
    }
}