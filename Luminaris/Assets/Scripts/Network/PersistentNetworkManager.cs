using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PersistentNetworkManager : MonoBehaviour
{
    private static PersistentNetworkManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
