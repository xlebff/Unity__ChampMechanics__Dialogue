using System.Collections.Generic;
using UnityEngine;

public interface ICustomStart
{
    public void CustomStart();
}

public class CustomStartController : MonoBehaviour
{
    [SerializeField] private List<MonoBehaviour> scriptsStartOrder;

    private void Start()
    {
        for (int i = 0; i < scriptsStartOrder.Count; ++i)
        {
            if (scriptsStartOrder[i] is ICustomStart custom)
                custom.CustomStart();
        }
    }
}
