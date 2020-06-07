using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public enum LegSide
{
    Left,
    Right
}

[System.Serializable]
public class Leg
{
    public int id;
    public LegSide side;
    public Transform foot;
}

public class BodyController : MonoBehaviour
{
    [SerializeField]
    private Leg[] Legs;
    [SerializeField]
    private float speed; 
    [SerializeField]
    private float maxZAxisRotationOffset; 
    private Transform bodyTransform;
    private float startHeightAboveGround;
    private float currentHeightAboveGround;
    // Start is called before the first frame update
    void Start()
    {
        bodyTransform = transform.Find("Body");
        startHeightAboveGround = GetHeightAboveGround();
    }

    // Update is called once per frame
    void Update()
    {
        currentHeightAboveGround = GetHeightAboveGround();
        Move();
        BalanceRotation();
    }

    private float GetHeightAboveGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection (Vector3.down), out hit))
        {
            return hit.distance;
        }
        return 0;
    }

    private void Move()
    {
        Vector3 moveDirection = new Vector3(0f, 0f, Input.GetAxis("Vertical"));
        moveDirection *= speed;

        transform.position += moveDirection;
    }

    private void BalanceRotation()
    {
        // z axis
        var leftLegs = Legs.Where(leg => leg.side == LegSide.Left);
        var rightLegs = Legs.Where(leg => leg.side == LegSide.Right);

        float leftHeightSum = 0;
        foreach (var leg in leftLegs)
        {
            leftHeightSum += leg.foot.position.y;
        }

        float rightHeightSum = 0;
        foreach (var leg in rightLegs)
        {
            rightHeightSum += leg.foot.position.y;
        }

        float leftHeightAverage = leftHeightSum / leftLegs.Count();
        float rightHeightAverage = rightHeightSum / rightLegs.Count();

        float difference = rightHeightAverage - leftHeightAverage;
        float ratio = difference / rightHeightAverage;
        float angle = difference * maxZAxisRotationOffset;

        Vector3 currentRotation = transform.rotation.eulerAngles;
        bodyTransform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, angle);
    }
}
