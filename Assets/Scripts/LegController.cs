using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -Vector3.up, out hit)) {
            // offsetDistance = hit.distance;
            if (hit.transform.tag == "Walkable")
            {
                Debug.DrawLine (transform.position, hit.point, Color.cyan);
                transform.position = hit.point + new Vector3(0, 0.05f, 0);
            }
        }
    }
}
