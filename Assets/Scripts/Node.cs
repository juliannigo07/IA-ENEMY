using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour, IWeighted
{
    public List<Node> neighbours = new List<Node>();
    public float Weight { get; set; }
    public Node previous;
    public LayerMask nodeMask;
    public float detectionRange = 5f;

    private void Awake()
    {
        Weight = 99999999999;

        var nodes = Physics.OverlapSphere(transform.position, detectionRange, nodeMask);

        foreach (var node in nodes)
        {
            var actualNode = node.GetComponent<Node>();

            if (actualNode == null || actualNode == this) continue;

            neighbours.Add(actualNode);
        }
    }

    public void OnResetWeight()
    {
        Weight = 99999999;
        previous = null;
    }

    private void OnDrawGizmos()
    {
        if (neighbours.Count > 0)
        {
            foreach (var node in neighbours)
            {
                Gizmos.DrawLine(transform.position, node.transform.position);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
