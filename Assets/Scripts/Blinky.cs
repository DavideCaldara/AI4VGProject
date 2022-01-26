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

public class Blinky : MonoBehaviour
{

    private FSM fsm;
    public Transform destination;
    
    private IEnumerator coroutine;

    Transform[] waypoints = new Transform[6];
    public float waypointTolerance = 1f;
    System.Random random = new System.Random();
    int nextWaypointIndex;
    
    float timeLeft;

    NavMeshAgent agent;
    private float speedCap = 5f;

    private float nextActionTime = 0.0f;
    private float period = 0.3f;

    private bool activePowerUp;
    private int totalActivePowerUps = 4;

    Color temp;

    [SerializeField] private GameObject GameOverUI;

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

        nextWaypointIndex = random.Next(6);

        //Blinky increase gradualy his speed during game execution until speed cap
        agent = GetComponent<NavMeshAgent>();
        StartCoroutine("IncreaseSpeedPerSecond", 1f); //SpeedUp coroutine

        temp = GetComponent<Renderer>().material.color;

        activePowerUp = false;

    }



    private void Update()
    {
        if (Time.time > nextActionTime) {

            nextActionTime += period;
            
            if (activePowerUp) // alarm effect on ghosts
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
        if(other.tag == "Player" && activePowerUp)
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

    // Periodic update of the FSM, runs forever
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
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(PlayerController.poweruptag)) {
            if (go.activeSelf) { // count how many powerups are still active
                count++;
            }
        }
        if (totalActivePowerUps == count) { // stay in chase state 
            print("no powers up collected, keep chasing");
            return false;
        }
        else { // one has been collected
            print("power up collected, state transition to flee");
            totalActivePowerUps--;
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
            yield return new WaitForSeconds(PlayerController.resampleTime);
        }
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
        timeLeft = PlayerController.powerUpDuration; //powerup 20 seconds
    }    
}
