using System.CodeDom.Compiler;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class nodelines : MonoBehaviour
{
    public int maxnodelayers = 10;
    public int minnode = 1;    
    public int maxnode = 3;
    public int mapphase = 3;

    public float xnodesize = 2f;
    public float ynodesize = 3f;

    public GameObject nodeobject;
    public List<List<nodenode>> layers = new List<List<nodenode>>();
    public int nodecount;
    void Start()
    {
        Generate();

    }


    void Generate()
    {
        creatnode();
        connectnode();
        nodetype();
    }

    float getxpos(int nodeslot,int nodecount)
    {
        float totalwidth = (maxnode -1) * xnodesize;
        float startx = -totalwidth / 2f;



        return startx + nodeslot * xnodesize;
    }

    void creatnode()
    {

        layers.Clear();
        for (int x = 0;x < maxnodelayers; x++)
        {   

            List<nodenode> currentLayer = new List<nodenode>();

            if (x == 0 || x == maxnodelayers - 2 || x == maxnodelayers - 1)
            {
                nodecount = 1;


            }
            else
            {
                nodecount = Random.Range(minnode,maxnode+1);
            }
            List<int> xposindex = new List<int>();
            for (int i = 0; i < maxnode; i++)
            {
                xposindex.Add(i);

            }
            for (int i = 0; i < xposindex.Count; i++)
            {
                int wherenode = Random.Range(i,xposindex.Count);
                int temp = xposindex[i];
                xposindex[i] = xposindex[wherenode];
                xposindex[wherenode] = temp;
            }

            for (int y = 0;y < nodecount; y++)
            {
                int randomslot = xposindex[y];
                float xpos = getxpos(randomslot, nodecount);
                float ypos = x * ynodesize;
                Vector3 pos = new Vector3(xpos,ypos,0);
                GameObject newnode = Instantiate(nodeobject, pos, Quaternion.identity);
                nodenode nodeScript = newnode.GetComponent<nodenode>();
                currentLayer.Add(nodeScript);
            }
            layers.Add(currentLayer);
        }

    }


    void connectnode()
    {
        for(int p = 0;p < maxnode - 1; p++)
        {
            


        }
        


    }

    void nodetype()
    {
        



    }



    void Update()
    {
        



    }

}   

