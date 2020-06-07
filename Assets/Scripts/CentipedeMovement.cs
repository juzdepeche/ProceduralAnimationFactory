using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentipedeMovement : MonoBehaviour
{
    public List<Transform> BodyParts = new List<Transform>();

    [SerializeField]
    private int bodyCount = 6;
    [SerializeField]
    private float minDistance = 1f;
    [SerializeField]
    private float speed = 1;
    [SerializeField]
    private float rotationSpeed = 50;

    [SerializeField]
    private GameObject bodyPrefab;

    private float distance;
    private Transform currentBodyPart;
    private Transform previousBodyPart;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < bodyCount - 1; i++)
        {
            AddBodyPart();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }

    private void Move() {
        BodyParts[0].Translate(BodyParts[0].forward * speed * Time.smoothDeltaTime, Space.World);

        if (Input.GetAxis("Horizontal") != 0)
        {
            BodyParts[0].Rotate(Vector3.up * rotationSpeed * Time.deltaTime * Input.GetAxis("Horizontal"));
        }

        for (int i = 1; i < BodyParts.Count; i++)
        {
            currentBodyPart = BodyParts[i];
            previousBodyPart = BodyParts[i - 1];

            distance = Vector3.Distance(previousBodyPart.position, currentBodyPart.position);

            Vector3 newPosition = previousBodyPart.position;

            //flag hum
            newPosition.y = BodyParts[0].position.y;

            float T = Time.deltaTime * distance / minDistance * speed;

            if (T > 0.5f) T = 0.5f;

            currentBodyPart.position = Vector3.Slerp(currentBodyPart.position, newPosition, T);
            currentBodyPart.rotation = Quaternion.Slerp(currentBodyPart.rotation, previousBodyPart.rotation, T);
        }
    }

    public void AddBodyPart()
    {
        Transform newPart = Instantiate(bodyPrefab, BodyParts[BodyParts.Count - 1].position, BodyParts[BodyParts.Count - 1].rotation).transform.Find("BodyPartController");

        newPart.SetParent(transform);
        BodyParts.Add(newPart);
    }
}
