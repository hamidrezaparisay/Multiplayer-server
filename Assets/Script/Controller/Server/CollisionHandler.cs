using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    Rigidbody carRB;
    public static CollisionHandler AddThisComponent(GameObject myObject)
    {
        CollisionHandler result=myObject.AddComponent<CollisionHandler>();
        result.Awake();
        return result;
    }
    void Awake()
    {
        carRB=GetComponent<Rigidbody>();
    }
    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            carRB.AddForceAtPosition(transform.right*10-transform.forward*1,contact.point,ForceMode.VelocityChange);
        }
    }
    void OnCollisionExit(Collision collision)
    {
        carRB.velocity*=0.4f;
    }
}
