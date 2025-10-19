using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Photon.Pun;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Management;
using System;
using UnityEngine.XR.Hands;

public class NetworkPlayer : MonoBehaviour

{



    public Transform head;

    public Transform leftHand;

    public Transform rightHand;

    private PhotonView photonView;



    // private OVRHand[] m_hands;

    private GameObject headController;

    private GameObject leftHandController;

    private GameObject rightHandController;

    public float xLeftR;
    public float yLeftR;
    public float zLeftR;

    public float xRightR;
    public float yRightR;
    public float zRightR;

    public float xLeftMR;
    public float yLeftMR;
    public float zLeftMR;

    public float xRightMR;
    public float yRightMR;
    public float zRightMR;


    // Start is called before the first frame update

    void Start()

    {

        photonView = GetComponent<PhotonView>();

        
        if (photonView.IsMine)

        {

            //headController = GameObject.Find("Main Camera");

            leftHandController = GameObject.Find("RiggedHandLeft");
            rightHandController = GameObject.Find("RiggedHandRight");

            //headController = GameObject.Find("OVRCameraRig");

            head.gameObject.SetActive(false);
            leftHand.gameObject.SetActive(false);
            rightHand.gameObject.SetActive(false);

        }
        


    }



    // Update is called once per frame

    [Obsolete]
    void Update()

    {

        if (photonView.IsMine)

        {


            //MapPosition(head, XRNode.Head);

            //MapPosition(leftHand, XRNode.LeftHand);

            //MapPosition(rightHand, XRNode.RightHand);


            //head.transform.position = headController.transform.position;
            //head.transform.rotation = headController.transform.rotation;


            head.transform.position = Camera.main.transform.position;
            head.transform.rotation = Camera.main.transform.rotation;

            /*
            leftHand.position = leftHandController.transform.position;
            rightHand.position = rightHandController.transform.position;
            
            leftHand.rotation = leftHandController.transform.rotation;
            rightHand.rotation = rightHandController.transform.rotation;
            

            rightHand.rotation = Quaternion.Euler(xRightMR * rightHandController.transform.rotation.eulerAngles.x + xRightR,
                                               yRightMR * rightHandController.transform.rotation.eulerAngles.y + yRightR,
                                               zRightMR * rightHandController.transform.rotation.eulerAngles.z + zRightR);

            leftHand.rotation = Quaternion.Euler(xLeftMR * leftHandController.transform.rotation.eulerAngles.x + xLeftR,
                                   yLeftMR * leftHandController.transform.rotation.eulerAngles.y + yLeftR,
                                   zLeftMR * leftHandController.transform.rotation.eulerAngles.z + zLeftR);

            */

            leftHand.position = HandPoseDetectionMRTK.getLeftPalmPosition();
            leftHand.rotation = HandPoseDetectionMRTK.getLeftPalmRotation();
            rightHand.position = HandPoseDetectionMRTK.getRightPalmPosition();
            rightHand.rotation = HandPoseDetectionMRTK.getRightPalmRotation();


        }



    }



    void MapPosition(Transform target, XRNode node)

    {

        InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);

        InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);



        target.position = position;

        target.rotation = rotation;



    }



    void SetTargetInvisible(GameObject Target)

    {

        foreach (Renderer r in Target.GetComponentsInChildren(typeof(Renderer)))

        {

            r.enabled = false;

        }

    }



    void SetTargetVisible(GameObject Target)

    {

        foreach (Renderer r in Target.GetComponentsInChildren(typeof(Renderer)))

        {

            r.enabled = true;

        }

    }



}

