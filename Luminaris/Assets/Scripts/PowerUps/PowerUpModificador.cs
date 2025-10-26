using UnityEngine;

public abstract class PowerUpModificador : ScriptableObject
{
    [Header("Duração em turnos")]
    [Tooltip("Duração padrão em turnos do jogador (pode ser sobrescrita no inspector do asset)")]
    public int durationTurns = 3;
    public int DurationTurns => durationTurns;

    public abstract void Activate(GameObject target);
    //public abstract void Deactivate(GameObject target);
}
