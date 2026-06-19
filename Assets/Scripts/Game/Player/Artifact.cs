using UnityEngine;

public class Artifact : MonoBehaviour
{
    public ArtifactData data;
    
    private bool isCarried = false;
    private Transform originalParent;
    private Collider myCollider;
    private Rigidbody myRigidbody;

    void Awake()
    {
        myCollider = GetComponent<Collider>();
        myRigidbody = GetComponent<Rigidbody>();
        originalParent = transform.parent;
    }

    public void OnPickedUp(Transform handSocket)
    {
        isCarried = true;
        if (myCollider != null) myCollider.enabled = false;
        if (myRigidbody != null) myRigidbody.isKinematic = true;

        transform.SetParent(handSocket);
        transform.localPosition = data.HoldOffsetPosition;
        transform.localEulerAngles = data.HoldOffsetRotation;
    }

    public void OnDropped(Vector3 dropPosition, Vector3 surfaceNormal)
    {
        isCarried = false;
        transform.SetParent(originalParent);
        
        // Position on the surface of the planet
        transform.position = dropPosition;
        
        // Align up vector with gravity/surface normal
        transform.rotation = Quaternion.FromToRotation(transform.up, surfaceNormal) * transform.rotation;

        if (myCollider != null) myCollider.enabled = true;
        if (myRigidbody != null) myRigidbody.isKinematic = false;
    }

    public bool IsCarried()
    {
        return isCarried;
    }
}
