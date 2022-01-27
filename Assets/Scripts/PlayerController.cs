/* Player controller class
 * Manage player movement and interaction with objects
 */

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]

public class PlayerController : MonoBehaviour {

	// movement
	[Range(0.0f, 30.0f)] public float movementSpeed = 10f;
	[Range(0.0f, 360.0f)] public float rotationSensitivity = 90f;

	// HUD management
	public static int score;
	[SerializeField] public Text scoreText;

	// GameObjects tags
	public static string targetTag = "Player"; 
	public static string poweruptag = "PowerUpTag";

	public static float reactionTime = 3f; // update time of FSMs
	public static float resampleTime = 5f; // update time of chase state
	public static float fleeResampleTime = .2f; // update time of flee state

	public static float powerUpDuration = 20.0f; // powerup duration

	void Start () {
		score = 0;
	}
	
	void FixedUpdate () {
		Rigidbody rb = GetComponent<Rigidbody> ();
		// gas and brake are converted into a translation forward/backward
		rb.MovePosition (transform.position
						 + transform.forward * movementSpeed * (Input.GetAxis ("Vertical") * Time.deltaTime));
		// steering is translated into a rotation
		rb.MoveRotation(Quaternion.Euler(0.0f, rotationSensitivity * (Input.GetAxis ("Horizontal") * Time.deltaTime), 0.0f)
			            * transform.rotation);
	}

    private void OnTriggerEnter(Collider other)
    {
		// Pickin up collectibles increase score 
		if (other.gameObject.tag == "Collectible") 
		{
			other.gameObject.SetActive(false); 
			score++;
			scoreText.text = score.ToString();
		}
	}

}
