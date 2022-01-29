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
   
    private FSM fsm;
    public Transform destination;
    
    private IEnumerator coroutine;

    Transform[] waypoints = new Transform[6]; // Waypoints path management
    public float waypointTolerance = 1f;
    System.Random random = new System.Random();
    int nextWaypointIndex;
    
    private float runResampleTime = .2f;

    float timeLeft;
    
    private float maxDistance = 10f;

    NavMeshAgent agent;

    private float nextActionTime = 0.0f; // for mesh color changing effect
    private float period = 0.3f;

    private bool activePowerUp; // powerup collection management
    private int totalActivePowerUps = 4;

    Color temp;

    [SerializeField] private GameObject GameOverUI;

    void Start()
    {
        // Define FSM
        // Define States
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

        nextWaypointIndex = random.Next(6); // Start my flee route from a random waypoint
        agent = GetComponent<NavMeshAgent>();

        temp = GetComponent<Renderer>().material.color; // Save base color

        activePowerUp = false;

    }

    private void Update()
    {
        if (Time.time > nextActionTime)
        {
            nextActionTime += period;

            if (activePowerUp) // alarm effect on ghosts when powerup is active
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
        if (other.tag == "Player" && activePowerUp) // Eating ghost
        {
            this.gameObject.SetActive(false);
        }
        if (other.tag == "Player" && !activePowerUp) // GameOver
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        Time.timeScale = 0f; // pause the game show panel
        GameOverUI.SetActive(true);
    }

    // Periodic update FSM
    public IEnumerator Patrol()
    {
        while (true)
        {
            fsm.Update();
            yield return new WaitForSeconds(PlayerController.reactionTime);
        }
    }


    // CONDITIONS

    // If a powerUp has been collected I fire the transition to flee State
    public bool PowerUp()
    {
        // I check for the powerup to be collected, if one is missing fire transition
        int count = 0;
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(PlayerController.poweruptag))
        {
            if (go.activeSelf) { //count how many powerups are still active
                count++;
            }
        }
        if (totalActivePowerUps == count)
        { // stay in chase state 
            print("no powers up collected, keep chasing");
            return false;
        }
        else
        { // one has been collected
            print("power up collected, state transition to flee");
            totalActivePowerUps--;
            if (coroutine != null)
                StopCoroutine(coroutine);
            coroutine = null;
            return true;
        }
    }

    // After 30 seconds I fire the transition to Chase state
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

    public bool TooClose() // when i am too close to the player transition to run state
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

    public bool FarEnough() // when i am far enough i can go back chasing the player
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
            yield return new WaitForSeconds(PlayerController.resampleTime);
        }
    }
    private IEnumerator GoFlee() // run toward labyrinth corner
    {
        while (true)
        {
            Vector3 nextWaypointPosition = waypoints[nextWaypointIndex].position;
            GetComponent<NavMeshAgent>().destination = nextWaypointPosition;
            CycleWaypointWhenClose(nextWaypointPosition);
            yield return new WaitForSeconds(PlayerController.fleeResampleTime);
        }
    }

    private IEnumerator GoRun() // run away from the player
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


    private void CycleWaypointWhenClose(Vector3 nextWaypointPosition) // scan the vector for the next waypoint
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
        print("entered clydechase state");
        activePowerUp = false;
        coroutine = GoChase();
        StartCoroutine(coroutine);
    }

    public void ClydeFlee()
    {
        print("entered clydeflee state");
        activePowerUp = true;
        coroutine = GoFlee();
        StartCoroutine(coroutine);
        timeLeft = PlayerController.powerUpDuration; //powerup 20 seconds
    }

    public void ClydeRun()
    {
        print("entered clyderun state");
        coroutine = GoRun();
        StartCoroutine(coroutine);
    }


}
