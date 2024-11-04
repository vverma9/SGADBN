using UnityEngine;
using BayesServer;
using BayesServer.Inference.RelevanceTree;
using Newtonsoft.Json;
using System;
using System.IO;

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
        string filePath = Path.Combine(Application.streamingAssetsPath, "Network.json");
        // var vec = JsonConvert.DeserializeObject<Vector2>("{'x':1.0,'y':0.0}");
        // Debug.Log(filePath);
    }

}