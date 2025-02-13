using UnityEngine;
using BayesServer;
using BayesServer.Inference.RelevanceTree;
using Newtonsoft.Json;
using System.IO;

public class BayesServerNet : MonoBehaviour
{
    NetworkJSON networkData; // read from json file
    BayesServer.Network beliefNet; // variable that holds the entire DBN
    // Variables for root node
    Node rootNode;
    State Rtrue;
    State RFalse;

    // Variables for L1 (competency) nodes
    int l1cNum;
    Node[] l1competencyNodes;
    State[] l1cTrue;
    State[] l1cFalse;

    // Variables for L2 (competency) nodes
    int l2cNum;
    Node[] l2competencyNodes;
    State[] l2cTrue;
    State[] l2cFalse;

    // Variables for L3 (competency) nodes
    int l3cNum;
    Node[] l3competencyNodes;
    State[] l3cTrue;
    State[] l3cFalse;

    // Variables for L4 (evidence) nodes
    int eNum; // count of evidences
    Node[] evidenceNodes;
    State[] eTrue;
    State[] eFalse;

    // Use this for initialization
    void Awake()
    {
        // This as an academic trial license for bayesserver 8 released years ago
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
        rootNode = new Node(networkData.root.name, Rtrue, RFalse) // Created root node
        {
            TemporalType = TemporalType.Contemporal // this is a not a time series node
        };
        beliefNet.Nodes.Add(rootNode); // Add root node to the network

        // add l1 competency nodes
        l1cNum = networkData.l1competencies.Count;
        l1cTrue = new State[l1cNum];
        l1cFalse = new State[l1cNum];
        l1competencyNodes = new Node[l1cNum];

        // iterate over all the level 1 competencies, create their nodes, add them to network and links them to root node
        for (int i=0; i< l1cNum; i++)
        {
            NetworkJSON.Competency l1c = networkData.l1competencies[i];
            l1cTrue[i] = new State("True");
            l1cFalse[i] = new State("False");
            l1competencyNodes[i] = new Node(l1c.name, l1cTrue[i], l1cFalse[i])
            {
                TemporalType = TemporalType.Contemporal// this is not a time series node
            };
            beliefNet.Nodes.Add(l1competencyNodes[i]);
            beliefNet.Links.Add(new Link(rootNode, l1competencyNodes[i]));
        }

        // add l2 competency nodes
        l2cNum = networkData.l2competencies.Count;
        l2cTrue = new State[l2cNum];
        l2cFalse = new State[l2cNum];
        l2competencyNodes = new Node[l2cNum];

        // iterate over all the level 1 competencies, create their nodes, add them to network and links them to root node
        for (int i = 0; i < l2cNum; i++)
        {
            NetworkJSON.Competency l2c = networkData.l2competencies[i];
            l2cTrue[i] = new State("True");
            l2cFalse[i] = new State("False");
            l2competencyNodes[i] = new Node(l2c.name, l2cTrue[i], l2cFalse[i])
            {
                TemporalType = TemporalType.Contemporal// this is not a time series node
            };
            beliefNet.Nodes.Add(l2competencyNodes[i]);
            beliefNet.Links.Add(new Link(l1competencyNodes[l2c.parent], l2competencyNodes[i]));
        }

        // add l3 competency nodes
        l3cNum = networkData.l3competencies.Count;
        l3cTrue = new State[l3cNum];
        l3cFalse = new State[l3cNum];
        l3competencyNodes = new Node[l3cNum];

        // iterate over all the level 1 competencies, create their nodes, add them to network and links them to root node
        for (int i = 0; i < l3cNum; i++)
        {
            NetworkJSON.Competency l3c = networkData.l3competencies[i];
            l3cTrue[i] = new State("True");
            l3cFalse[i] = new State("False");
            l3competencyNodes[i] = new Node(l3c.name, l3cTrue[i], l3cFalse[i])
            {
                TemporalType = TemporalType.Contemporal// this is not a time series node
            };
            beliefNet.Nodes.Add(l3competencyNodes[i]);
            beliefNet.Links.Add(new Link(l2competencyNodes[l3c.parent], l3competencyNodes[i]));
        }

        eNum = networkData.evidences.Count;
        eTrue = new State[eNum];
        eFalse = new State[eNum];
        evidenceNodes = new Node[eNum];
        // iterate over all the evidences, create their nodes, add them to network and links them to their parent competency node
        for (int i = 0; i < eNum; i++)
        {
            NetworkJSON.Evidence e = networkData.evidences[i];
            eTrue[i] = new State("True");
            eFalse[i] = new State("False");
            evidenceNodes[i] = new Node(e.name, eTrue[i], eFalse[i])
            {
                TemporalType = TemporalType.Contemporal// this is a time series node, hence re-used for each time slice
            };
            beliefNet.Nodes.Add(evidenceNodes[i]);
            int[] parents = e.parents;
            foreach(int parent in parents)
                beliefNet.Links.Add(new Link(l3competencyNodes[parent], evidenceNodes[i], 0));
        }

        //Debug.Log(Application.persistentDataPath);
        InitializeNetworkDistribution();
        beliefNet.Save(Application.persistentDataPath + "/temp.bayes"); // doesn't save the state of evidences
    }
    public void InitializeNetworkDistribution()
    {
        // Probability distribution (Conditional Probability Tables, CPT) for root node at t=0
        Table rootProb = rootNode.NewDistribution().Table;
        rootProb[Rtrue] = 0;
        rootProb[RFalse] = 1;
        rootNode.Distribution = rootProb;

        // CPT for L1 Competency nodes
        Table cProb;
        for (int i = 0; i < l1cNum; i++)
        {
            NetworkJSON.Competency l1c = networkData.l1competencies[i];
            cProb = l1competencyNodes[i].NewDistribution().Table;
            cProb[Rtrue, l1cTrue[i]] = 1 - l1c.slipRate;
            cProb[Rtrue, l1cFalse[i]] = l1c.slipRate;
            cProb[RFalse, l1cTrue[i]] = l1c.guessRate;
            cProb[RFalse, l1cFalse[i]] = 1 - l1c.guessRate;
            l1competencyNodes[i].Distribution = cProb;
        }

        // CPT for L2 Competency nodes
        for (int i = 0; i < l2cNum; i++)
        {
            NetworkJSON.Competency l2c = networkData.l2competencies[i];
            cProb = l2competencyNodes[i].NewDistribution().Table;
            cProb[l1cTrue[l2c.parent], l2cTrue[i]] = 1 - l2c.slipRate;
            cProb[l1cTrue[l2c.parent], l2cFalse[i]] = l2c.slipRate;
            cProb[l1cFalse[l2c.parent], l2cTrue[i]] = l2c.guessRate;
            cProb[l1cFalse[l2c.parent], l2cFalse[i]] = 1 - l2c.guessRate;
            l2competencyNodes[i].Distribution = cProb;
        }

        // CPT for L3 Competency nodes
        for (int i = 0; i < l3cNum; i++)
        {
            NetworkJSON.Competency l3c = networkData.l3competencies[i];
            cProb = l3competencyNodes[i].NewDistribution().Table;
            cProb[l2cTrue[l3c.parent], l3cTrue[i]] = 1 - l3c.slipRate;
            cProb[l2cTrue[l3c.parent], l3cFalse[i]] = l3c.slipRate;
            cProb[l2cFalse[l3c.parent], l3cTrue[i]] = l3c.guessRate;
            cProb[l2cFalse[l3c.parent], l3cFalse[i]] = 1 - l3c.guessRate;
            l3competencyNodes[i].Distribution = cProb;
        }

        // CPT for Evidence nodes
        StateContext eTrueContext, eFalseContext;
        Table eProb;
        for (int i = 0; i < eNum; i++)
        {
            NetworkJSON.Evidence e = networkData.evidences[i];
            int[] parents = e.parents;

            // Assign probability values
            int numCombinations = (int)System.Math.Pow(2, parents.Length);
            double[] probabilities = new double[numCombinations * 2]; // *2 for both states of L3
            for (int k = 0; k < numCombinations; k++)
            {
                probabilities[k * 2] = 0.75; // Probability of "true"
                probabilities[k * 2 + 1] = 0.25; // Probability of "false"
            }

            // refer to the documentaion for adding distribution for nodes that may have multiple parents
            // https://www.bayesserver.com/code/csharp/construction-inference-cs
            eProb = evidenceNodes[i].NewDistribution().Table;
            Node[] currentAndParents = new Node[parents.Length + 1];
            int j = 0;
            while (j < parents.Length)
            {
                currentAndParents[j] = l3competencyNodes[parents[j]];
                j++;
            }
            currentAndParents[j] = evidenceNodes[i];

            TableIterator tabIter = new TableIterator(eProb, currentAndParents);
            // Copy the generated probabilities into the table
            tabIter.CopyFrom(probabilities);
            evidenceNodes[i].Distribution = eProb;
        }
    }
}