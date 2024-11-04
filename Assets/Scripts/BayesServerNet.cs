using UnityEngine;
using BayesServer;
using BayesServer.Inference.RelevanceTree;
using Newtonsoft.Json;
using System.IO;

public class BayesServerNet : MonoBehaviour
{
    NetworkJSON networkData;
    BayesServer.Network beliefNet;
    Node rootNode;
    Variable root;
    State Rtrue;
    State RFalse;
    int cNum;
    Node[] competencyNodes;
    Variable[] competencies;
    State[] cTrue;
    State[] cFalse;
    int eNum;
    Node[] evidenceNodes;
    Variable[] evidences;
    State[] eTrue;
    State[] eFalse;
    // Use this for initialization
    void Awake()
    {
        License.Validate("c2ca6130-86da-4a05-af35-2510567e1025");
        Invoke("InitializeNetwork", 0);
    }
    public void InitializeNetwork()
    {
        //Debug.Log("making network");
        string filePath = Path.Combine(Application.streamingAssetsPath, "Network.json");
        networkData = JsonConvert.DeserializeObject<NetworkJSON>(File.ReadAllText(filePath));
        cNum = networkData.competencies.Count;
        eNum = networkData.evidences.Count;
        //Debug.Log(networkData.competencies.Count);
        beliefNet = new BayesServer.Network();

        // add a root node (blood transfusion knowledge) which is a latent variable (parameter to be learned from observed values)
        Rtrue = new State("True");
        RFalse = new State("False");
        root = new Variable(networkData.root.name, Rtrue, RFalse);
        rootNode = new Node(root)
        {
            TemporalType = TemporalType.Temporal // this is a time series node, hence re-used for each time slice
        };
        beliefNet.Nodes.Add(rootNode);

        // add competency nodes
        cTrue = new State[cNum];
        cFalse = new State[cNum];
        competencies = new Variable[cNum];
        competencyNodes = new Node[cNum];
        for (int i=0; i< cNum; i++)
        {
            NetworkJSON.Competency c = networkData.competencies[i];
            cTrue[i] = new State("True");
            cFalse[i] = new State("False");
            competencies[i] = new Variable(c.name, cTrue[i], cFalse[i]);
            competencyNodes[i] = new Node(competencies[i])
            {
                TemporalType = TemporalType.Temporal // this is a time series node, hence re-used for each time slice
            };
            beliefNet.Nodes.Add(competencyNodes[i]);
            beliefNet.Links.Add(new Link(rootNode, competencyNodes[i], 0));
        }
        //Debug.Log(Application.persistentDataPath);
        beliefNet.Save(Application.persistentDataPath + "/temp.bayes"); // doesn't save the state of evidences
    }

}