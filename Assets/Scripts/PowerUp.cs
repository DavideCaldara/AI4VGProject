using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //rotate power up
        transform.Rotate(Vector3.up, 1f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        //if i collide with player deactivate me and launch flee status for 20 seconds
        if (other.gameObject.tag == "Player")
        {
            this.gameObject.SetActive(false);
        }
    }
}
