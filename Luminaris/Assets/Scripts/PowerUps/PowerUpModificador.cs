using UnityEngine;

public abstract class PowerUpModificador : ScriptableObject
{
    [Header("Dura��o em turnos")]
    [Tooltip("Dura��o padr�o em turnos do jogador (pode ser sobrescrita no inspector do asset)")]
    public int durationTurns = 3;
    public int DurationTurns => durationTurns;

    public abstract void Activate(GameObject target);
    //public abstract void Deactivate(GameObject target);
}
