using System.Collections.Generic;

public class GameSession
{
    private readonly List<IResettable> resetObjects = new List<IResettable>();

    // Registra objetos resetáveis
    public void RegisterResettable(IResettable obj)
    {
        if (!resetObjects.Contains(obj))
            resetObjects.Add(obj);
    }

    // Reseta todos os objetos registrados
    public void ResetSession()
    {
        foreach (var obj in resetObjects)
        {
            obj.ResetState();
        }
    }
}
