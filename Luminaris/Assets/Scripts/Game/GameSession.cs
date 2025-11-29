using System.Collections.Generic;

public class GameSession
{
    private readonly List<IResettable> resetObjects = new List<IResettable>();

    public void RegisterResettable(IResettable obj)
    {
        if (!resetObjects.Contains(obj))
            resetObjects.Add(obj);
    }
    public void ResetSession()
    {
        foreach (var obj in resetObjects)
        {
            obj.ResetState();
        }
    }
}
