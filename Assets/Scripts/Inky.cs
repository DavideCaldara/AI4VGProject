/* Inky Behavior
 * chase mode -> ??
 * flee mode (powerup active) -> patrol around bottom right corner of 
 * the maze (blue dots path)
*/

using System.Collections;
using UnityEngine.AI;
using UnityEngine;

[RequireComponent(typeof(NavMeshAgent))]

public class Inky : MonoBehaviour
{
    // Start is called before the first frame update

    public string targetTag = "Player";
    public string poweruptag = "PowerUpTag";

    private FSM fsm;
    public Transform destination;
    public float reactionTime = 3f;
    public float resampleTime = 5f;
    public float FleeTimer;

    private IEnumerator coroutine;

    Transform[] waypoints = new Transform[7];
    public float waypointTolerance = 1f;
    System.Random random = new System.Random();
    int nextWaypointIndex;
    private float fleeResampleTime = .2f;

    float timeLeft;

    private bool activePowerUp;
    private float powerUpDuration = 30.0f;

    private int totalActivePowerUps = 4;


    void Start()
    {
        FSMState chase = new FSMState();
        chase.enterActions.Add(InkyChase);

        FSMState flee = new FSMState();
        flee.enterActions.Add(InkyFlee);

        //define transitions
        FSMTransition t1 = new FSMTransition(PowerUp);
        FSMTransition t2 = new FSMTransition(Timer);

        // Link states to transitions
        chase.AddTransition(t1, flee);
        flee.AddTransition(t2, chase);

        //setup a FSA at ainitial state
        fsm = new FSM(chase);

        // Start Monitoring
        StartCoroutine(Patrol());

        // waypoints initialization
        int i = 0;
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("InkyWaypoints"))
        {
            waypoints[i] = go.transform;
            i++;
        }

        activePowerUp = false;

        nextWaypointIndex = random.Next(7);

    }

    // Periodic update, run forever
    public IEnumerator Patrol()
    {
        while (true)
        {
            fsm.Update();
            yield return new WaitForSeconds(reactionTime);
        }
    }

    // CONDITIONS

    public bool PowerUp()
    {
        // I check for the powerup to be collected, if one is missing fire transition
        int count = 0;
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(poweruptag))
        {
            if (go.activeSelf)
            { //count how many powerups are still active
                count++;
            }
        }
        if (totalActivePowerUps == count)
        { //stay in chase state 
            print("no powers up collected, keep chasing");
            return false;
        }
        else
        { // one has been collected
            totalActivePowerUps--; //update current active powerups
            print("power up collected, state transition to flee");
            StopCoroutine(coroutine);
            coroutine = null;
            return true;
        }


    }

    public bool Timer()
    {
        //print("condizione verificata, torno a stato chase");
        timeLeft -= (Time.deltaTime + reactionTime);
        print(timeLeft);
        if (timeLeft < 0)
        {
            StopCoroutine(coroutine);
            coroutine = null;
            return true;
        }
        return false;
    }


    // Behaviors Coroutines
    private IEnumerator GoChase()
    {
        while (true)
        {
            // TODO
            //GetComponent<NavMeshAgent>().destination = destination.position;
            //yield return new WaitForSeconds(resampleTime);
        }
    }
    private IEnumerator GoFlee()
    {
        while (true)
        {
            Vector3 nextWaypointPosition = waypoints[nextWaypointIndex].position;
            GetComponent<NavMeshAgent>().destination = nextWaypointPosition;
            CycleWaypointWhenClose(nextWaypointPosition);
            yield return new WaitForSeconds(fleeResampleTime);
        }
    }

    private void CycleWaypointWhenClose(Vector3 nextWaypointPosition)
    {
        if (Vector3.Distance(transform.position, nextWaypointPosition) <= waypointTolerance)
        {
            nextWaypointIndex++;
            if (nextWaypointIndex == 7)
                nextWaypointIndex = 0;
        }
    }


    // ACTIONS

    public void InkyChase()
    {
        //Clyde behavior, 
        print("entrato stato clydechase");
        activePowerUp = false;
        coroutine = GoChase();
        StartCoroutine(coroutine);
    }

    public void InkyFlee()
    {
        print("entrato stato clydeflee");
        activePowerUp = true;
        coroutine = GoFlee();
        StartCoroutine(coroutine); //Blinky Beahvior when fleeing, top right corner patrol
        timeLeft = powerUpDuration; //powerup 20 seconds
    }


}
