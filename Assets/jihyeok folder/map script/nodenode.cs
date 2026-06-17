using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;




public enum nodetype{
    startzone,monsterzone,rewardzone,safezone,elitezone,bosszone,questionmarkzone


}


public class nodenode : MonoBehaviour
{
    public int nodes;
    public int nodelines;
    public int nodelayer;
    public Vector2 nodevec2;

    public nodetype nodetype;
    public nodenode(int nodes,int nodelines,int nodelayer,Vector2 nodevec2,nodetype nodetype)
    {
        this.nodes = nodes;
        this.nodelines = nodelines;
        this.nodelayer = nodelayer;
        this.nodetype = nodetype;
        this.nodevec2 = nodevec2;

    }





}

