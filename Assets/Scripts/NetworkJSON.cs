using System.Collections.Generic;
[System.Serializable]

public class NetworkJSON
{
    // Never use getter and setter otherwise Unity won't be able to serialize it
    public class Competency
    {
        public int id;
        public string name;
    }

    public class Evidence
    {
        public int id;
        public string name;
        public int parent;
    }

    public class Root
    {
        public string name;
    }

    public Root root;
    public List<Competency> competencies;
    public List<Evidence> evidences;
}