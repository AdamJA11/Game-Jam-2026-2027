using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moving_Hand : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform targetPosition; // The position to move towards
   public Transform mainTransform; // The transform of the object to move
   public Transform transorm1; // The transform of the object to move
   public Transform transorm2; // The transform of the object to move
   public float speed; // The speed at which to move the object
   public float stopDistance ;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(mainTransform.position, targetPosition.position) > stopDistance + 300)
        {
            mainTransform.position = Vector3.MoveTowards(mainTransform.position, targetPosition.position, speed * Time.deltaTime);
        }

        if (Vector3.Distance(transorm1.position, targetPosition.position) > stopDistance + 300)
        {
            transorm1.position = Vector3.MoveTowards(transorm1.position, targetPosition.position, speed * Time.deltaTime);
        }

        if (Vector3.Distance(transorm2.position, targetPosition.position) > stopDistance + 100)
        {
            transorm2.position = Vector3.MoveTowards(transorm2.position, targetPosition.position, speed * Time.deltaTime);
        }
       

        

        

        
        
        // Move the object towards the target position
        
        
        
    }
}
