using UnityEngine;
using BayesServer;
using BayesServer.Inference.RelevanceTree;

public class BayesServerNet : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        Invoke("InitializeNetwork", 0);
        License.Validate("c2ca6130-86da-4a05-af35-2510567e1025");
    }
    public void InitializeNetwork()
    {
        Debug.Log("making network");
    }

}