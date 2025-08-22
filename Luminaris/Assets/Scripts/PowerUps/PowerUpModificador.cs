using UnityEngine;

public abstract class PowerUpModificador : ScriptableObject
{
    [SerializeField] protected int durationTurns = 1;

    public int DurationTurns => durationTurns;

    public abstract void Activate(GameObject target);
    public abstract void Deactivate(GameObject target);
}
