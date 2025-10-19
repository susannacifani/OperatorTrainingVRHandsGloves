using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class HandMovement : MonoBehaviour
{
    public UDPReceiver udpReceiver;
    public GameObject handModel;
    public GameObject[] thumbPoints;
    public GameObject[] indexPoints;
    public GameObject[] middlePoints;
    public GameObject[] ringPoints;
    public GameObject[] pinkyPoints;

    // Fattore di scala per il giroscopio (ipotizzando ±250°/s)
    private float gyroscopeScaleFactor = 0.00763f;

    // Variabili di accumulo per la rotazione (in gradi)
    private float accumulatedRotationX = 0f;
    private float accumulatedRotationY = 0f;
    private float accumulatedRotationZ = 0f;

    private void Start()
    {
        if (handModel != null)
        {
            handModel.SetActive(true);
        }
    }

    void Update()
    {
        string data = udpReceiver.data;

        // Verifica se i dati non sono nulli o vuoti
        if (string.IsNullOrEmpty(data))
            return;

        // Rimuovere i caratteri iniziali e finali (assumendo che siano parentesi o altri caratteri)
        data = data.Remove(0, 1);
        data = data.Remove(data.Length - 1, 1);

        // Dividere i dati in base alla virgola
        string[] points = data.Split(',');



        // Leggere i valori grezzi del giroscopio (Gx, Gy, Gz)
        float Gx = float.Parse(points[points.Length - 3]);
        float Gy = float.Parse(points[points.Length - 2]);
        float Gz = float.Parse(points[points.Length - 1]);

        
        // Convertire i valori del giroscopio in gradi per secondo
        float gyroX = -Gy * gyroscopeScaleFactor;
        float gyroY = -Gz * gyroscopeScaleFactor;
        float gyroZ = -Gx * gyroscopeScaleFactor;

        // Integrare i valori del giroscopio per ottenere l'angolo di rotazione cumulativo (in gradi)
        accumulatedRotationX += gyroX * Time.deltaTime;
        accumulatedRotationY += gyroY * Time.deltaTime;
        accumulatedRotationZ += gyroZ * Time.deltaTime;

        // Creare la rotazione target usando i valori integrati
        Quaternion targetRotation = Quaternion.Euler(accumulatedRotationX, accumulatedRotationY, accumulatedRotationZ);

        // Applicare la rotazione al cubo
        handModel.transform.rotation = targetRotation;
        

        float joint2 = float.Parse(points[points.Length - 18]); //thumb1
        float joint3 = float.Parse(points[points.Length - 17]); //index1
        float joint4 = float.Parse(points[points.Length - 16]); //index2
        float joint5 = float.Parse(points[points.Length - 15]);
        float joint6 = float.Parse(points[points.Length - 14]); //broken
        float joint7 = float.Parse(points[points.Length - 13]);
        float joint8 = float.Parse(points[points.Length - 12]);
        float joint9 = float.Parse(points[points.Length - 11]);
        float joint10 = float.Parse(points[points.Length - 10]);
        float joint11 = float.Parse(points[points.Length - 9]); //broken thumb2
        float joint12 = float.Parse(points[points.Length - 8]); //thumb3


        //I must normalize manually each joint like normalized_value = (sensor_value - min_value) / (max_value - min_value)
        //thumb1
        float joint2_norm = (joint2 - 630) / (1500 - 630);
        float joint2_rotation_angle = joint2_norm * 90;
        //thumb3 (thumb2 broken)
        float joint12_norm = (joint12 - 870) / (1500 - 870);
        float joint12_rotation_angle = joint12_norm * 90;
        //aggiorno solo Z
        thumbPoints[0].transform.localRotation = Quaternion.Euler(thumbPoints[0].transform.localRotation.eulerAngles.x, thumbPoints[0].transform.localRotation.eulerAngles.y, joint2_rotation_angle);
        thumbPoints[1].transform.localRotation = Quaternion.Euler(thumbPoints[1].transform.localRotation.eulerAngles.x, thumbPoints[1].transform.localRotation.eulerAngles.y, joint12_rotation_angle);

        //index1
        float joint3_norm = (joint3 - 800) / (1500 - 800);
        float joint3_rotation_angle = joint3_norm * 90;
        //index2
        float joint4_norm = (joint4 - 900) / (1500 - 900);
        float joint4_rotation_angle = joint4_norm * 90;
        indexPoints[0].transform.localRotation = Quaternion.Euler(joint3_rotation_angle, indexPoints[0].transform.localRotation.eulerAngles.y, indexPoints[0].transform.localRotation.eulerAngles.z);
        indexPoints[1].transform.localRotation = Quaternion.Euler(joint4_rotation_angle, indexPoints[1].transform.localRotation.eulerAngles.y, indexPoints[1].transform.localRotation.eulerAngles.z);

        //middle1
        float joint5_norm = (joint5 - 560) / (1500 - 560);
        float joint5_rotation_angle = joint5_norm * 90;
        middlePoints[0].transform.localRotation = Quaternion.Euler(joint5_rotation_angle, middlePoints[0].transform.localRotation.eulerAngles.y, middlePoints[0].transform.localRotation.eulerAngles.z);


        //ring1
        float joint7_norm = (joint7 - 610) / (1500 - 610);
        float joint7_rotation_angle = joint7_norm * 90;
        //ring2
        float joint8_norm = (joint8 - 1190) / (1500 - 1190);
        float joint8_rotation_angle = joint8_norm * 90;
        ringPoints[0].transform.localRotation = Quaternion.Euler(joint7_rotation_angle, ringPoints[0].transform.localRotation.eulerAngles.y, ringPoints[0].transform.localRotation.eulerAngles.z);
        ringPoints[1].transform.localRotation = Quaternion.Euler(joint8_rotation_angle, ringPoints[1].transform.localRotation.eulerAngles.y, ringPoints[1].transform.localRotation.eulerAngles.z);

        //pinky1
        float joint9_norm = (joint9 - 300) / (1500 - 300);
        float joint9_rotation_angle = joint9_norm * 90;
        pinkyPoints[0].transform.localRotation = Quaternion.Euler(joint9_rotation_angle, pinkyPoints[0].transform.localRotation.eulerAngles.y, pinkyPoints[0].transform.localRotation.eulerAngles.z);

        /*
        //pinky2
        float joint10_norm = (joint10 - 1000) / (1500 - 1000);
        float joint10_rotation_angle = joint10_norm * 90;
        pinkyPoints[0].transform.localRotation = Quaternion.Euler(joint9_rotation_angle, pinkyPoints[0].transform.localRotation.eulerAngles.y, pinkyPoints[0].transform.localRotation.eulerAngles.z);
        pinkyPoints[1].transform.localRotation = Quaternion.Euler(joint10_rotation_angle, pinkyPoints[1].transform.localRotation.eulerAngles.y, pinkyPoints[1].transform.localRotation.eulerAngles.z);
        */


    }

}