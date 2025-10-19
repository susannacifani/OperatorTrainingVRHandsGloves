using Microsoft.MixedReality.Toolkit.UI;
using SG;
using UnityEngine;

public class MagneticSnap : MonoBehaviour
{
    // Assign these in the Inspector or dynamically
    public GameObject objectToSnap;    // The first object (that will be snapped)
    public GameObject targetObject;    // The second object (the target of the snap)

    // Position and rotation thresholds
    public float positionThreshold = 0.1f;   // The maximum distance for snapping to occur
    public float rotationThreshold = 5.0f;   // The maximum angle (in degrees) for snapping

    // Audio settings
    public AudioSource snapSound; // AudioSource component that will play the snap sound
    private bool hasSnapped = false; // To prevent playing the sound repeatedly

    // Update is called once per frame
    void Update()
    {
        // Check distance and angle
        if (IsWithinSnapThreshold() && !hasSnapped)
        {
            SnapObject();
            PlaySnapSound();
            hasSnapped = true; // Ensure the sound plays only once
            objectToSnap.GetComponent<SG_Grabable>().enabled = false;
            objectToSnap.GetComponent<ObjectManipulator>().enabled = false;
            objectToSnap.GetComponent<Rigidbody>().isKinematic = true;
        }

        // Reset if the object moves away from the snapping threshold
        if (!IsWithinSnapThreshold() && hasSnapped)
        {
            hasSnapped = false;
        }
    }

    // Check if the first object is within snapping thresholds
    bool IsWithinSnapThreshold()
    {
        // Calculate the distance between the two objects
        float distance = Vector3.Distance(objectToSnap.transform.position, targetObject.transform.position);

        // Calculate the angular difference between the two rotations
        float angleDifference = Quaternion.Angle(objectToSnap.transform.rotation, targetObject.transform.rotation);

        // Check if both the position and rotation are within the threshold
        return distance <= positionThreshold && angleDifference <= rotationThreshold;
    }

    // Snap the first object to the position and rotation of the second
    void SnapObject()
    {
        // Snap position
        objectToSnap.transform.position = targetObject.transform.position;

        // Snap rotation
        objectToSnap.transform.rotation = targetObject.transform.rotation;
    }

    // Play the snapping sound
    void PlaySnapSound()
    {
        if (snapSound != null)
        {
            snapSound.Play();
        }
        else
        {
            Debug.LogWarning("No snap sound assigned.");
        }
    }
}
