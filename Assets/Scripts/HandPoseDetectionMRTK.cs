using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using ExitGames.Client.Photon;

public class HandPoseDetectionMRTK : MonoBehaviour,
    IMixedRealitySourceStateHandler, // Handle source detected and lost
    IMixedRealityHandJointHandler // handle joint position updates for hands
{
    [SerializeField]
    public Handedness HandHandednessForMoving;

    [SerializeField]
    public Handedness LeftHandedness;

    [SerializeField]
    public Handedness RightHandedness;

    private float palmRotation = 0f;

    private Vector3 indexTip = new Vector3();
    private Vector3 indexDistal = new Vector3();
    private Vector3 indexMiddle = new Vector3();

    private Vector3 middleTip = new Vector3();
    private Vector3 middleDistal = new Vector3();
    private Vector3 middleMiddle = new Vector3();

    private Vector3 ringTip = new Vector3();
    private Vector3 ringDistal = new Vector3();
    private Vector3 ringMiddle = new Vector3();

    private Vector3 pinkyTip = new Vector3();
    private Vector3 pinkyDistal = new Vector3();
    private Vector3 pinkyMiddle = new Vector3();

    static private Vector3 LeftPalmPosePosition;
    static private Quaternion LeftPalmPoseRotation;
    static private Vector3 RightPalmPosePosition;
    static private Quaternion RightPalmPoseRotation;

    private MixedRealityPose palmPose;
    private MixedRealityPose jointPose;

    
    private void OnEnable()
    {
        // Instruct Input System that we would like to receive all input events of type
        // IMixedRealitySourceStateHandler and IMixedRealityHandJointHandler
        CoreServices.InputSystem?.RegisterHandler<IMixedRealitySourceStateHandler>(this);
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityHandJointHandler>(this);

    }

    private void OnDisable()
    {
        // This component is being destroyed
        // Instruct the Input System to disregard us for input event handling
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealitySourceStateHandler>(this);
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityHandJointHandler>(this);
    }
    

    // IMixedRealitySourceStateHandler interface

    
    public void OnSourceDetected(SourceStateEventData eventData)
    {
        var hand = eventData.Controller as IMixedRealityHand;

        // Only react to articulated hand input sources
        if (hand != null)
        {
            //Debug.Log("Source detected: " + hand.ControllerHandedness);
        }
    }

    public void OnSourceLost(SourceStateEventData eventData)
    {
        var hand = eventData.Controller as IMixedRealityHand;

        // Only react to articulated hand input sources
        if (hand != null)
        {
            //Debug.Log("Source lost: " + hand.ControllerHandedness);

        }
    }

    
    void IMixedRealityHandJointHandler.OnHandJointsUpdated(InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> eventData)
    {

        //Debug.Log("Joints Update - EventH: " + eventData.Handedness);
        //Debug.Log("Joints Update - MyH: " + HandHandedness);

        if (eventData.Handedness == LeftHandedness)
        {
            if (eventData.InputData.TryGetValue(TrackedHandJoint.Palm, out palmPose))
            {
                LeftPalmPosePosition = palmPose.Position;
                LeftPalmPoseRotation = palmPose.Rotation;
            }
        }

        if (eventData.Handedness == RightHandedness)
        {
            if (eventData.InputData.TryGetValue(TrackedHandJoint.Palm, out palmPose))
            {
                RightPalmPosePosition = palmPose.Position;
                RightPalmPoseRotation = palmPose.Rotation;
            }
        }


        if (eventData.Handedness == HandHandednessForMoving)
        {

            //Debug.Log("INSIDE");

            //PALM
            if (eventData.InputData.TryGetValue(TrackedHandJoint.Palm, out palmPose))
            {
                //Debug.Log("Hand Joint Palm Rotation: " + palmPose.Rotation.eulerAngles.x + " ; " + palmPose.Rotation.eulerAngles.y + " ; " + palmPose.Rotation.eulerAngles.z);
                palmRotation = palmPose.Rotation.eulerAngles.z;
            }

            //INDEX
            if (eventData.InputData.TryGetValue(TrackedHandJoint.IndexTip, out jointPose))
            {

                //Debug.Log("Index Position: " + jointPose.Position);
                indexTip = jointPose.Position;

            }

            if (eventData.InputData.TryGetValue(TrackedHandJoint.IndexDistalJoint, out jointPose))
            {

                //Debug.Log("Distal Position: " + jointPose.Position);
                indexDistal = jointPose.Position;

            }

            if (eventData.InputData.TryGetValue(TrackedHandJoint.IndexMiddleJoint, out jointPose))
            {

                //Debug.Log("Index Middle Position: " + jointPose.Position);
                indexMiddle = jointPose.Position;

            }

            Vector3 indexTipDistal = indexTip - indexDistal;
            Vector3 indexDistalMid = indexDistal - indexMiddle;


            //MIDDLE
            if (eventData.InputData.TryGetValue(TrackedHandJoint.MiddleTip, out jointPose))
            {

                //Debug.Log("Middle Position: " + jointPose.Position);
                middleTip = jointPose.Position;

            }

            if (eventData.InputData.TryGetValue(TrackedHandJoint.MiddleDistalJoint, out jointPose))
            {

                //Debug.Log("Middle Position: " + jointPose.Position);
                middleDistal = jointPose.Position;

            }

            if (eventData.InputData.TryGetValue(TrackedHandJoint.MiddleMiddleJoint, out jointPose))
            {

                //Debug.Log("Middle Position: " + jointPose.Position);
                middleMiddle = jointPose.Position;

            }

            Vector3 middleTipDistal = middleTip - middleDistal;
            Vector3 middleDistalMid = middleDistal - middleMiddle;

            if (Vector3.Angle(indexTipDistal, indexDistalMid) <= 20f && Vector3.Angle(middleTipDistal, middleDistalMid) > 40f && 80f <= palmRotation && 100f >= palmRotation)
            {
                //Debug.Log("Pointing");
                this.GetComponent<MovePlayer>().MoveForward();
            }

        }

    }
    


    /*
    public static float DistanceLineSegmentPoint(Vector3 start, Vector3 end, Vector3 point)
    {
        var wander = point - start;
        var span = end - start;

        // Compute how far along the line is the closest approach to our point.
        float t = Vector3.Dot(wander, span) / span.sqrMagnitude;

        // Restrict this point to within the line segment from start to end.
        t = Mathf.Clamp01(t);

        Vector3 nearest = start + t * span;
        return (nearest - point).magnitude;
    }
    */

    static public Vector3 getLeftPalmPosition ()
    {
        return LeftPalmPosePosition;
    }

    static public Quaternion getLeftPalmRotation ()
    {
        return LeftPalmPoseRotation;
    }

    static public Vector3 getRightPalmPosition()
    {
        return RightPalmPosePosition;
    }

    static public Quaternion getRightPalmRotation()
    {
        return RightPalmPoseRotation;
    }

}