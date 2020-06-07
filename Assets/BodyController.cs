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
    private float maxZAxisRotationOffset; 
    private float startHeightAboveGround;
    private float currentHeightAboveGround;
    // Start is called before the first frame update
    void Start()
    {
        startHeightAboveGround = GetHeightAboveGround();
    }

    // Update is called once per frame
    void Update()
    {
        currentHeightAboveGround = GetHeightAboveGround();

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
        float ratio = leftHeightAverage / rightHeightAverage;

        Vector3 rotationVector = new Vector3(transform.rotation.x, transform.rotation.y, difference * ratio * maxZAxisRotationOffset);
        transform.rotation = Quaternion.Euler(rotationVector);

        // float radians = Mathf.Atan2(leftHeightAverage, rightHeightAverage);
        // float angle = radians * (180/Mathf.PI);

    }
}
