using UnityEngine;
//Akhona Khoali
//Old script from class but I'm scared of deleting it so I will keep it here for now.
//I didnt wire the controls so it doesnt affect my other scripts. I will delete it later if I dont need it.

public class PickUpObject : MonoBehaviour
{
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void PickUp(Transform holdPoint)
    {
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
    }

    public void Drop()
    {
        rb.useGravity = true;
        transform.SetParent(null);
    }

    public void MoveToHoldPoint(Vector3 targetPosition)
    {
        rb.MovePosition(targetPosition);
    }

    public void Throw(Vector3 impulse)
    {
        transform.SetParent(null);
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(impulse, ForceMode.Impulse);
    }
}