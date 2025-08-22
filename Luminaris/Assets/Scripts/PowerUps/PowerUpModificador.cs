using UnityEngine;

public abstract class PowerUpModificador : ScriptableObject
{
    [SerializeField] protected int durationTurns = 1; // duração em turnos

    // Ativa o efeito do PowerUp no target (Player ou Lava, por exemplo)
    public abstract void Activate(GameObject target);

    // Desativa (se necessário) — alguns PowerUps podem não precisar
    public abstract void Deactivate(GameObject target);
}
