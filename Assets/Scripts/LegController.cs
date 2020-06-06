﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegController : MonoBehaviour
{
    [SerializeField]
    private Transform legTarget;

    [SerializeField]
    private float distanceTreshold = 1.5f;
    [SerializeField]
    private float speed = 5f;
    private float distance = 0;
    private bool distanceTresholdExceeded = false;
    private bool firstHalfOver = false;

    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
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

        distance = Vector3.Distance(transform.position, legTarget.position);

        if (distance > distanceTreshold)
        {
            distanceTresholdExceeded = true;
        }

        //move the leg
        if (distanceTresholdExceeded)
        {
            float step =  speed * Time.deltaTime; // calculate distance to move
            var targetPosition = Vector3.MoveTowards(legTarget.position, transform.position, step);

            //todo: add upwards vector for the first half

            legTarget.position = targetPosition;

            if (distance < 0.1f)
            {
                distanceTresholdExceeded = false;
                firstHalfOver = false;
            }
        }
    }
}
