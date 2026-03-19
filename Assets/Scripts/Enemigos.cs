using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

//A*
//Theta*

public class Enemigos : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] private Vector3 initialPos;
    [SerializeField] Rigidbody rb;
    private bool isFollowing;
    private bool isWaitingForPath;
    [SerializeField] private List<Node> actualPath = new List<Node>();
    [SerializeField] private float speed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float obstacleRange;
    [SerializeField] private float viewRange;
    [SerializeField] private LayerMask _obstacleMask;
    private int _obstacleCount = 0;
    private Vector3 _obstacleDir;
    public bool isAlerted = false;
    public Vector3 alertPosition;
    private NPCStateMachine stateMachine;
    private Coroutine chaseTimerCoroutine;

    // Variables para patrullaje
    [SerializeField] private List<Transform> patrolPoints = new List<Transform>();
    [SerializeField] private float moveSpeed = 2f;
    private int currentPatrolPoint = 0;

    // Tiempo de espera configurable desde el Inspector
    [SerializeField] private float chaseTimeout = 2f;

    private void Awake()
    {
        initialPos = transform.position;
        stateMachine = GetComponent<NPCStateMachine>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) isFollowing = !isFollowing;

        if (isFollowing && !isWaitingForPath)
        {
            FollowPath();
        }
    }

    private void FollowPath()
    {
        if (_obstacleCount == 0)
        {
            _obstacleDir = ObstacleAvoidance();
        }

        if (actualPath.Count > 0)
        {
            Node targetNode = actualPath[0];
            Vector3 dir = targetNode.transform.position - transform.position;
            dir.y = 0;

            // Mueve al enemigo hacia el nodo actual
            Vector3 moveDir = dir.normalized + _obstacleDir;
            transform.forward = Vector3.Lerp(transform.forward, moveDir, rotationSpeed * Time.deltaTime);
            rb.velocity = transform.forward * speed * Time.deltaTime;

            // Si el enemigo está cerca del nodo actual, avanza al siguiente nodo
            if (Vector3.Distance(transform.position, targetNode.transform.position) < 1f)
            {
                actualPath.RemoveAt(0);
            }
        }
        else
        {
            isWaitingForPath = true;
            Pathfinding.Instance.RequestPath(transform.position, target.position, PathCallback, ErrorCallback);
        }

        _obstacleCount++;
        if (_obstacleCount > 2) _obstacleCount = 0;
    }

    private Vector3 ObstacleAvoidance()
    {
        var obstacles = Physics.OverlapSphere(transform.position, obstacleRange, _obstacleMask);
        Debug.Log(obstacles.Length);

        if (obstacles.Length <= 0) return Vector3.zero;

        var obstacleDir = Vector3.zero;

        foreach (var obstacle in obstacles)
        {
            Debug.Log(obstacle.gameObject.name);
            obstacleDir += (transform.position - obstacle.transform.position).normalized;
        }

        obstacleDir.y = 0f;

        return obstacleDir;
    }

    private void PathCallback(List<Node> path)
    {
        actualPath = path;
        isWaitingForPath = false;
        Debug.Log("Camino generado con éxito.");
        foreach (var node in actualPath)
        {
            Debug.Log("Nodo en el camino: " + node.name);
        }
    }

    private void ErrorCallback()
    {
        Debug.LogError("No se encontró ningún nodo");
    }

    public void Patrol()
    {
        if (patrolPoints.Count == 0) return;

        Transform target = patrolPoints[currentPatrolPoint];
        float step = moveSpeed * Time.deltaTime;
        Vector3 moveDir = (target.position - transform.position).normalized;
        transform.forward = Vector3.Lerp(transform.forward, moveDir, rotationSpeed * Time.deltaTime);
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentPatrolPoint = (currentPatrolPoint + 1) % patrolPoints.Count;
        }
    }

    public void ChasePlayer()
    {
        if (actualPath == null || actualPath.Count == 0)
        {
            Pathfinding.Instance.RequestPath(transform.position, target.position, PathCallback, ErrorCallback);
            return;
        }

        FollowPath();
    }

    public void AlertOtherAgents(Vector3 position)
    {
        Enemigos[] agents = FindObjectsOfType<Enemigos>();
        foreach (Enemigos agent in agents)
        {
            if (agent != this)
            {
                agent.isAlerted = true;
                agent.alertPosition = position;
                agent.stateMachine.TransitionToState(NPCStateMachine.State.Chase);
                Pathfinding.Instance.RequestPath(agent.transform.position, position, agent.PathCallback, agent.ErrorCallback);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, obstacleRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRange);

        if (actualPath.Count <= 0) return;

        foreach (Node node in actualPath)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(node.transform.position, 1.2f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            stateMachine.TransitionToState(NPCStateMachine.State.Chase);
            AlertOtherAgents(other.transform.position);
            if (chaseTimerCoroutine != null)
            {
                StopCoroutine(chaseTimerCoroutine);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (chaseTimerCoroutine != null)
            {
                StopCoroutine(chaseTimerCoroutine);
            }
            chaseTimerCoroutine = StartCoroutine(ChaseTimer());
        }
    }

    private IEnumerator ChaseTimer()
    {
        yield return new WaitForSeconds(chaseTimeout);
        stateMachine.TransitionToState(NPCStateMachine.State.Patrol);
        Enemigos[] agents = FindObjectsOfType<Enemigos>();
        foreach (Enemigos agent in agents)
        {
            if (agent != this)
            {
                agent.stateMachine.TransitionToState(NPCStateMachine.State.Patrol);
            }
        }
    }
}

