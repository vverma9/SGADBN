using UnityEngine;
using BayesServer;
using BayesServer.Inference.RelevanceTree;
using Newtonsoft.Json;
using System.IO;

public class BayesServerNet : MonoBehaviour
{
    NetworkJSON networkData; // read from json file
    BayesServer.Network beliefNet; // variable that holds the entire DBN
    // Variables for L1 (root) node
    Node rootNode;
    Variable root;
    State Rtrue;
    State RFalse;

    // Variables for L2 (competency) nodes
    int cNum;
    Node[] competencyNodes;
    Variable[] competencies;
    State[] cTrue;
    State[] cFalse;

    // Variables for L3 (evidence) nodes
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
        beliefNet = new BayesServer.Network();

        // add a root node (blood transfusion knowledge) which is a latent variable (parameter to be learned from observed values)
        Rtrue = new State("True"); // Holds state for root node
        RFalse = new State("False"); // Holds state for root node
        root = new Variable(networkData.root.name, Rtrue, RFalse); // Creates root node variable that can have 2 states
        rootNode = new Node(root) // Created root node
        {
            TemporalType = TemporalType.Temporal // this is a time series node, hence re-used for each time slice
        };
        beliefNet.Nodes.Add(rootNode); // Add root node to the network
        beliefNet.Links.Add(new Link(rootNode, rootNode, 1)); // time series link with an order/lag 1

        // add competency nodes
        cNum = networkData.competencies.Count;
        cTrue = new State[cNum];
        cFalse = new State[cNum];
        competencies = new Variable[cNum];
        competencyNodes = new Node[cNum];
        int totalEvicenceCount = 0; // sets the total number of evidence in network
        for (int i = 0; i < cNum; i++)
        {
            totalEvicenceCount += networkData.competencies[i].evidences.Count;
        }
        eTrue = new State[totalEvicenceCount];
        eFalse = new State[totalEvicenceCount];
        evidences = new Variable[totalEvicenceCount];
        evidenceNodes = new Node[totalEvicenceCount];
        // iterate over all the competencies, create their nodes, add them to network and links them to root node
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

            // add evidence child nodes
            // iterate over all the child evidences, create their nodes, add them to network and links them to their parent competency node
            eNum = c.evidences.Count;
            for(int j=0; j < eNum; j++)
            {
                NetworkJSON.Evidence e = c.evidences[j];
                eTrue[i + j] = new State("True");
                eFalse[i + j] = new State("False");
                evidences[i + j] = new Variable(e.name, eTrue[i + j], eFalse[i + j]);
                evidenceNodes[i + j] = new Node(evidences[i + j])
                {
                    TemporalType = TemporalType.Temporal // this is a time series node, hence re-used for each time slice
                };
                beliefNet.Nodes.Add(evidenceNodes[i + j]);
                beliefNet.Links.Add(new Link(competencyNodes[i], evidenceNodes[i + j], 0));
            }
        }

        //Debug.Log(Application.persistentDataPath);
        InitializeNetworkDistribution();
        beliefNet.Save(Application.persistentDataPath + "/temp.bayes"); // doesn't save the state of evidences
    }
    public void InitializeNetworkDistribution()
    {
        // Probability distribution (Conditional Probability Tables, CPT) for root node at t=0
        StateContext rTrueContext = new StateContext(Rtrue, 0);
        StateContext rFalseContext = new StateContext(RFalse, 0);
        Table rootProb = rootNode.NewDistribution(0).Table;
        rootProb[rTrueContext] = 0;
        rootProb[rFalseContext] = 1;
        rootNode.Distribution = rootProb;

        // CPT for root node at t=1
        // when specifying temporal distributions, variables which belong to temporal nodes must have times associated
        // NOTE: Each time is specified relative to the current point in time which is defined as zero,
        // therefore the time for variables at the previous time step is -1
        StateContext rTrueTransitionContext = new StateContext(Rtrue, -1);
        StateContext rFalseTransitionContext = new StateContext(RFalse, -1);
        Table rootTransitionProb = rootNode.NewDistribution(1).Table;
        rootTransitionProb[rTrueContext, rTrueTransitionContext] = 1; // Knowledge learned will stay with you and can't be unlearned
        rootTransitionProb[rFalseContext, rTrueTransitionContext] = 0; // knowledge gained can't be unlearned
        rootTransitionProb[rTrueContext, rFalseTransitionContext] = 0.6; // Learn rate or transition rate
        rootTransitionProb[rFalseContext, rFalseTransitionContext] = 0.4;
        rootNode.Distributions[1] = rootTransitionProb;

        // CPT for Competency and Evidence nodes
    }
}