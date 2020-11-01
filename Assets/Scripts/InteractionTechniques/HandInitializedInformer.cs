using UnityEngine;

public class HandInitializedInformer : MonoBehaviour
{

    public event HandInitializedHandler onHandInitialized;

    public delegate void HandInitializedHandler();
    
    public void OnHandInitialized()
    {
        onHandInitialized?.Invoke();
    }
}
