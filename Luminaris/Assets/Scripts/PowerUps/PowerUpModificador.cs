using UnityEngine;

public abstract class PowerUpModificador : ScriptableObject
{
    [SerializeField] protected int durationTurns = 1; // dura��o em turnos

    // Ativa o efeito do PowerUp no target (Player ou Lava, por exemplo)
    public abstract void Activate(GameObject target);

    // Desativa (se necess�rio) � alguns PowerUps podem n�o precisar
    public abstract void Deactivate(GameObject target);
}
