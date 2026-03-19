using UnityEngine;

public class NPCStateMachine : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Chase,
        Alert
    }

    public State currentState;
    public Enemigos patrolAgent;

    void Update()
    {
        switch (currentState)
        {
            case State.Patrol:
                patrolAgent.Patrol();
                break;
            case State.Chase:
                patrolAgent.ChasePlayer();
                break;
            case State.Alert:
                patrolAgent.AlertOtherAgents(patrolAgent.alertPosition);
                break;
        }
    }

    public void TransitionToState(State newState)
    {
        currentState = newState;
    }
}
