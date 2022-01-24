/* Blinky Behavior
 * chase mode -> follows pacman directly, increase his speed overtime 
 * during game execution
 * flee mode (powerup active) -> patrol around upper right corner of 
 * the maze (red dots path)
*/


using System.Collections;
using UnityEngine.AI;
using UnityEngine;

[RequireComponent(typeof(NavMeshAgent))]

public class Blinky : MonoBehaviour {

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

    float timeLeft;

    private bool activePowerUp;
    private float powerUpDuration = 20.0f;

    private int totalActivePowerUps = 4;

    NavMeshAgent agent;
    private float speedCap = 5f;

    void Start()
    {
        // Define FSM
        // Define States
        FSMState chase = new FSMState();
        chase.enterActions.Add(BlinkyChase);

        FSMState flee = new FSMState();
        flee.enterActions.Add(BlinkyFlee);

        // Define transitions
        FSMTransition t1 = new FSMTransition(PowerUp);
        FSMTransition t2 = new FSMTransition(Timer);

        // Link states to transitions
        chase.AddTransition(t1, flee);
        flee.AddTransition(t2, chase);

        //setup initial state
        fsm = new FSM(chase);

        // Start Monitoring
        StartCoroutine(Patrol());

        // waypoints vector initialization
        int i = 0;
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("BlinkyWaypoints")) {
            waypoints[i] = go.transform;
            i++;
        }

        activePowerUp = false;

        nextWaypointIndex = random.Next(6);

        //Blinky increase gradualy his speed during game execution until speed cap
        agent = GetComponent<NavMeshAgent>();
        StartCoroutine("IncreaseSpeedPerSecond", 1f); //SpeedUp coroutine

    }

    // Periodic update of the FSM, runs forever
    public IEnumerator Patrol()
    {
        while (true)
        {
            fsm.Update();
            yield return new WaitForSeconds(reactionTime);
        }
    }

    // CONDITIONS

    // If a powerUp has been collected I fire the transition to flee State
    public bool PowerUp()
    {
        // I check for the powerup to be collected, if one is missing fire transition
        int count = 0;
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(poweruptag)) {
            if (go.activeSelf) { // count how many powerups are still active
                count++;
            }
        }
        if (totalActivePowerUps == count) { // stay in chase state 
            print("no powers up collected, keep chasing");
            return false;
        }
        else { // one has been collected
            totalActivePowerUps--; //update current active powerups
            print("power up collected, state transition to flee");
            StopCoroutine(coroutine);
            coroutine = null;
            return true;
        }        
    }

    // After 30 seconds I fire the transition to Chase state
    public bool Timer()
    {
        //print("condizione verificata, torno a stato chase");
        timeLeft -= (Time.deltaTime + reactionTime);
        print(timeLeft);
        if (timeLeft < 0) {
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

    private void CycleWaypointWhenClose(Vector3 nextWaypointPosition)
    {
        if (Vector3.Distance(transform.position, nextWaypointPosition) <= waypointTolerance)
        {
            nextWaypointIndex++;
            if (nextWaypointIndex == 6)
                nextWaypointIndex = 0;
        }
    }

    private IEnumerator IncreaseSpeedPerSecond(float waitTime)
    {
        //while agent's speed is less than the speedCap
        while (agent.speed < speedCap)
        {
            //wait "waitTime"
            yield return new WaitForSeconds(waitTime);
            //add 0.01f to currentSpeed every loop 
            agent.speed += 0.01f;
        }
    }

    // ACTIONS

    public void BlinkyChase()
    {
        //Blinky behavior, chase pacman directly
        print("entrato stato blinkchase");
        activePowerUp = false;
        coroutine = GoChase();
        StartCoroutine(coroutine); 
    }

    public void BlinkyFlee()
    {
        print("entrato stato blinkflee");
        activePowerUp = true;
        coroutine = GoFlee();
        StartCoroutine(coroutine); //Blinky Beahvior when fleeing, top right corner patrol
        timeLeft = powerUpDuration; //powerup 20 seconds
    }    
}
