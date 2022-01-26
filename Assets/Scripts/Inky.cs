/* Inky Behavior
 * chase mode -> His target is relative to both Blinky and Pac-Man. The destination is obtained 
 * rotating by 180 degress the vector from Blinky position to Pacman's FrontWaypoint.
 * flee mode (powerup active) -> patrol around bottom right corner of 
 * the maze (blue dots path)
*/

using System.Collections;
using UnityEngine.AI;
using UnityEngine;
using System;

[RequireComponent(typeof(NavMeshAgent))]

public class Inky : MonoBehaviour
{

    private FSM fsm;
    public Transform destination;

    private IEnumerator coroutine;

    Transform[] waypoints = new Transform[7];
    public float waypointTolerance = 1f;
    System.Random random = new System.Random();
    int nextWaypointIndex;

    float timeLeft;

    Color temp;

    private float nextActionTime = 0.0f;
    private float period = 0.3f;

    private bool activePowerUp;
    private int totalActivePowerUps = 4;

    [SerializeField] private GameObject GameOverUI;


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

        nextWaypointIndex = random.Next(7);

        temp = GetComponent<Renderer>().material.color;

        activePowerUp = false;

    }

    private void Update()
    {
        if (Time.time > nextActionTime)
        {
            nextActionTime += period;

            if (activePowerUp) // alarm effect
            {
                if (GetComponent<Renderer>().material.color == temp)
                    GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
                else
                    GetComponent<Renderer>().material.SetColor("_Color", temp);
            }
            else
            {
                GetComponent<Renderer>().material.SetColor("_Color", temp);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && activePowerUp)
        {
            this.gameObject.SetActive(false);
        }
        if (other.tag == "Player" && !activePowerUp)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        Time.timeScale = 0f;
        GameOverUI.SetActive(true);
    }

    // Periodic update, run forever
    public IEnumerator Patrol()
    {
        while (true)
        {
            fsm.Update();
            yield return new WaitForSeconds(PlayerController.reactionTime);
        }
    }

    // CONDITIONS

    public bool PowerUp()
    {
        // I check for the powerup to be collected, if one is missing fire transition
        int count = 0;
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(PlayerController.poweruptag))
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
            print("power up collected, state transition to flee");
            totalActivePowerUps--;
            StopCoroutine(coroutine);
            coroutine = null;
            return true;
        }


    }

    public bool Timer()
    {
        //print("condizione verificata, torno a stato chase");
        timeLeft -= (Time.deltaTime + PlayerController.reactionTime);
        print(timeLeft);
        if (timeLeft < 0)
        {
            StopCoroutine(coroutine);
            coroutine = null;
            return true;
        }
        return false;
    }

    Vector3 BlinkyPos;
    Vector3 WaypointPos;

    // Behaviors Coroutines
    private IEnumerator GoChase()
    {
        while (true)
        {
            BlinkyPos = GameObject.Find("Blinky").transform.position;
            WaypointPos = GameObject.Find("FrontWaypoint").transform.position;
            GetComponent<NavMeshAgent>().destination = CalculateInkyDestination(BlinkyPos, WaypointPos);
            yield return new WaitForSeconds(PlayerController.resampleTime);
        }
    }

    private Vector3 CalculateInkyDestination(Vector3 blinkyPos, Vector3 waypointPos)
    {
        //switch to 2D (x, z) cause player and ghosts are on the same plane
        Vector2 BlinkyPos = new Vector2(blinkyPos.x, blinkyPos.z);
        Vector2 WaypointPos = new Vector2(waypointPos.x, waypointPos.z); //center of mirroring

        float d = Vector2.Distance(BlinkyPos, WaypointPos);
        Vector2 versor = ((BlinkyPos - WaypointPos) / d).normalized;
        Vector2 result = WaypointPos - (versor * d);

        return new Vector3(result.x, GameObject.Find("Blinky").transform.position.y, result.y);
    }

    private IEnumerator GoFlee()
    {
        while (true)
        {
            Vector3 nextWaypointPosition = waypoints[nextWaypointIndex].position;
            GetComponent<NavMeshAgent>().destination = nextWaypointPosition;
            CycleWaypointWhenClose(nextWaypointPosition);
            yield return new WaitForSeconds(PlayerController.fleeResampleTime);
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
        timeLeft = PlayerController.powerUpDuration; //powerup 20 seconds
    }


}
