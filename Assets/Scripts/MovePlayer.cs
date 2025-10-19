using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MovePlayer : MonoBehaviour
{
    public float m_Speed;
    public GameObject XRPlayer;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void MoveForward()
    {
        //m_Rigidbody.velocity = transform.forward * m_Speed;
        //XRPlayer.transform.Translate(Vector3.forward * Time.deltaTime);
        XRPlayer.transform.Translate(Camera.main.transform.forward * m_Speed * Time.deltaTime);
    }

}
