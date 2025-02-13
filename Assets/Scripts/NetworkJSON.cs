using System.Collections.Generic;
[System.Serializable]

public class NetworkJSON
{
    // Never use getter and setter otherwise Unity won't be able to serialize it
    public class Competency
    {
        public int id;
        public string name;
        public double guessRate;
        public double slipRate;
        public int parent;
    }

    public class Evidence
    {
        public string name;
        public float[] probabilities;
        public int[] parents;
    }

    public class Root
    {
        public string name;
    }

    public Root root;
    public List<Competency> l1competencies;
    public List<Competency> l2competencies;
    public List<Competency> l3competencies;
    public List<Evidence> evidences;
}