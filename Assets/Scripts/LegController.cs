using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegController : MonoBehaviour
{
    [SerializeField]
    private Transform legTarget;
    [SerializeField]
    private LegController oppositeLeg;
    [SerializeField]
    private float heightRise = 1f;

    [SerializeField]
    private float distanceTreshold = 1.5f;
    [SerializeField]
    private float speed = 5f;
    private float distance = 0;
    public bool DistanceTresholdExceeded = false;

    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -Vector3.up, out hit)) {
            if (hit.transform.tag == "Walkable")
            {
                Debug.DrawLine(transform.position, hit.point, Color.cyan);
                transform.position = hit.point + new Vector3(0, 0.2f, 0);
            }
        }

        distance = Vector3.Distance(transform.position, legTarget.position);

        if (distance > distanceTreshold && IsOppositeGrounded())
        {
            DistanceTresholdExceeded = true;
        }

        //move the leg
        if (DistanceTresholdExceeded)
        {
            float step =  speed * Time.deltaTime;
            var targetPosition = Vector3.MoveTowards(legTarget.position, transform.position + new Vector3(0,((distance / distanceTreshold)/2) * heightRise, 0), step);

            legTarget.position = targetPosition;

            if (distance < 0.1f)
            {
                DistanceTresholdExceeded = false;
            }
        }
    }

    private bool IsOppositeGrounded()
    {
        return !oppositeLeg.DistanceTresholdExceeded;
    }
}
