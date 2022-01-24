/* Clyde Behavior
 * chase mode -> Follows pacman keeping staying always at least 8 meters away 
 * from him
 * flee mode (powerup active) -> patrol around bottom left corner of 
 * the maze (orange dots path)
*/

using System.Collections;
using UnityEngine.AI;
using UnityEngine;

[RequireComponent(typeof(NavMeshAgent))]

public class Clyde : MonoBehaviour
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

    Transform[] waypoints = new Transform[6];
    public float waypointTolerance = 1f;
    System.Random random = new System.Random();
    int nextWaypointIndex;
    private float fleeResampleTime = .2f;
    private float runResampleTime = .2f;

    float timeLeft;

    private bool activePowerUp;
    private float powerUpDuration = 30.0f;

    private int totalActivePowerUps = 4;
    private float maxDistance = 10f;

    NavMeshAgent agent;

    void Start()
    {
        FSMState chase = new FSMState();
        chase.enterActions.Add(ClydeChase);

        FSMState flee = new FSMState();
        flee.enterActions.Add(ClydeFlee);

        FSMState run = new FSMState();
        run.enterActions.Add(ClydeRun);

        //define transitions
        FSMTransition t1 = new FSMTransition(PowerUp);
        FSMTransition t2 = new FSMTransition(Timer);
        FSMTransition t3 = new FSMTransition(TooClose);
        FSMTransition t4 = new FSMTransition(FarEnough);

        // Link states to transitions
        chase.AddTransition(t1, flee);
        chase.AddTransition(t3, run);
        flee.AddTransition(t2, chase);
        run.AddTransition(t4,chase);
        run.AddTransition(t1, flee);

        //setup a FSA at ainitial state
        fsm = new FSM(chase);

        // Start Monitoring
        StartCoroutine(Patrol());

        // waypoints initialization
        int i = 0;
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("ClydeWaypoints"))
        {
            waypoints[i] = go.transform;
            i++;
        }

        activePowerUp = false;

        nextWaypointIndex = random.Next(6);
        agent = GetComponent<NavMeshAgent>();

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
            if (coroutine != null)
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

    public bool TooClose()
    {
        //print(Vector3.Distance(transform.position, GameObject.Find("Player").transform.position));
        if (Vector3.Distance(transform.position, GameObject.Find("PacMan").transform.position) < maxDistance)
        {
            StopCoroutine(coroutine);
            coroutine = null;
            return true;
        }
        return false;
    }

    public bool FarEnough()
    {
        if (Vector3.Distance(transform.position, GameObject.Find("PacMan").transform.position) > maxDistance)
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
            // Follows pacman keeping staying always at least 8 units away from him,
            // I need a coroutine that always check for the distance from Clyde to pacman to be minimum 8 units, otherwise stops clyde
            GetComponent<NavMeshAgent>().destination = destination.position;
            yield return new WaitForSeconds(resampleTime);
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

    private IEnumerator GoRun()
    {
        while (true)
        {
            float distance = Vector3.Distance(transform.position, GameObject.Find("PacMan").transform.position);
            
            if(distance < maxDistance)
            {
                Vector3 dirToPlayer = transform.position - GameObject.Find("PacMan").transform.position;
                Vector3 newPos = transform.position + dirToPlayer;
                agent.destination = newPos;
            }
            yield return new WaitForSeconds(runResampleTime);
        }
    }


    private void CycleWaypointWhenClose(Vector3 nextWaypointPosition)
    {
        if (Vector3.Distance(transform.position, nextWaypointPosition) <= waypointTolerance)
        {
            nextWaypointIndex++;
            if (nextWaypointIndex == 6)
                nextWaypointIndex = 0;
        }
    }


    // ACTIONS

    public void ClydeChase()
    {
        print("entrato stato clydechase");
        activePowerUp = false;
        coroutine = GoChase();
        StartCoroutine(coroutine);
    }

    public void ClydeFlee()
    {
        print("entrato stato clydeflee");
        activePowerUp = true;
        coroutine = GoFlee();
        StartCoroutine(coroutine); //Blinky Beahvior when fleeing, top right corner patrol
        timeLeft = powerUpDuration; //powerup 20 seconds
    }

    public void ClydeRun()
    {
        print("entrato stato clyderun");
        coroutine = GoRun();
        StartCoroutine(coroutine);
    }


}
