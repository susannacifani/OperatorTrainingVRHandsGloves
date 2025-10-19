using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SendJoints : MonoBehaviour
{
    public UDPReceiver udpReceiver;
    public GameObject handModel;
    public GameObject[] thumbPoints;
    public GameObject[] indexPoints;
    public GameObject[] middlePoints;
    public GameObject[] ringPoints;
    public GameObject[] pinkyPoints;

    public static Quaternion[][] kanevJoints;

    private string[] jointNames = { "thumb1", "index1", "index2", "middle1", "broken", "ring1", "ring2", "pinky1", "pinky2", "broken_thumb2", "thumb3" };

    // Valori di calibrazione per ogni joint
    private float[] calibratedMinValues = new float[11];
    private float[] calibratedMaxValues = new float[11];
    private List<float>[] jointSamplesOpen = new List<float>[11];
    private List<float>[] jointSamplesClosed = new List<float>[11];
    // Flag per verificare se la calibrazione è stata completata
    private bool isCalibrated = false;

    private float smoothSpeed = 15f; // Velocità di interpolazione
    private float[] currentJointAngles = new float[11]; // Stato attuale per ogni joint

    private Vector3[] initialRotations = new Vector3[5];

    private void Start()
    {

        // Inizializza kanevJoints con un array per ogni dito
        kanevJoints = new Quaternion[5][];
        for (int i = 0; i < 5; i++)
        {
            kanevJoints[i] = new Quaternion[4];
        }


        // Assegna direttamente le rotazioni iniziali dei punti principali
        initialRotations[0] = new Vector3(
            thumbPoints[0].transform.localRotation.eulerAngles.x,
            thumbPoints[0].transform.localRotation.eulerAngles.y,
            thumbPoints[0].transform.localRotation.eulerAngles.z
        );
        kanevJoints[0][0] = Quaternion.Euler(
            initialRotations[0].x,
            initialRotations[0].y,
            initialRotations[0].z
        );

        //Debug.Log($"Thumb Point X: {thumbPoints[0].transform.localRotation.eulerAngles.x}, " +
        //                      $"Y: {thumbPoints[0].transform.localRotation.eulerAngles.y}, " +
        //                      $"Z: {thumbPoints[0].transform.localRotation.eulerAngles.z}");
        //
        initialRotations[1] = new Vector3(
            indexPoints[0].transform.localRotation.eulerAngles.x,
            indexPoints[0].transform.localRotation.eulerAngles.y,
            indexPoints[0].transform.localRotation.eulerAngles.z
        );
        kanevJoints[1][0] = Quaternion.Euler(
            initialRotations[1].x,
            initialRotations[1].y,
            initialRotations[1].z
        );

        initialRotations[2] = new Vector3(
            middlePoints[0].transform.localRotation.eulerAngles.x,
            middlePoints[0].transform.localRotation.eulerAngles.y,
            middlePoints[0].transform.localRotation.eulerAngles.z
        );
        kanevJoints[2][0] = Quaternion.Euler(
            initialRotations[2].x,
            initialRotations[2].y,
            initialRotations[2].z
        );

        initialRotations[3] = new Vector3(
            ringPoints[0].transform.localRotation.eulerAngles.x,
            ringPoints[0].transform.localRotation.eulerAngles.y,
            ringPoints[0].transform.localRotation.eulerAngles.z
        );
        kanevJoints[3][0] = Quaternion.Euler(
            initialRotations[3].x,
            initialRotations[3].y,
            initialRotations[3].z
        );

        initialRotations[4] = new Vector3(
            pinkyPoints[0].transform.localRotation.eulerAngles.x,
            pinkyPoints[0].transform.localRotation.eulerAngles.y,
            pinkyPoints[0].transform.localRotation.eulerAngles.z
        );
        kanevJoints[4][0] = Quaternion.Euler(
            initialRotations[4].x,
            initialRotations[4].y,
            initialRotations[4].z
        );



        // Inizializza le liste per raccogliere i samples
        for (int i = 0; i < 11; i++)
        {
            jointSamplesOpen[i] = new List<float>();
            jointSamplesClosed[i] = new List<float>();
        }

        // Avvia la calibrazione
        StartCoroutine(CalibrateGlove());
    }

    void Update()
    {

        if (!isCalibrated)
            return;

        string data = udpReceiver.data;

        if (string.IsNullOrEmpty(data))
            return;

        data = data.Remove(0, 1);
        data = data.Remove(data.Length - 1, 1);

        string[] points = data.Split(',');

        float joint2 = float.Parse(points[points.Length - 18]); // thumb1
        float joint3 = float.Parse(points[points.Length - 17]); // index1
        float joint4 = float.Parse(points[points.Length - 16]); // index2
        float joint5 = float.Parse(points[points.Length - 15]); // middle1
        float joint6 = float.Parse(points[points.Length - 14]); // broken_middle2
        float joint7 = float.Parse(points[points.Length - 13]); // ring1
        float joint8 = float.Parse(points[points.Length - 12]); // ring2
        float joint9 = float.Parse(points[points.Length - 11]); // pinky1
        float joint10 = float.Parse(points[points.Length - 10]); // pinky2: maybe broken
        float joint11 = float.Parse(points[points.Length - 9]);  // broken_thumb2
        float joint12 = float.Parse(points[points.Length - 8]);  // thumb3


        // Calcolare l'angolo di rotazione per ciascun joint
        // thumb1 (calibratedMinValues[0])
        if (calibratedMinValues[0] == calibratedMaxValues[0]) {
            kanevJoints[0][0] = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            float joint2_norm = (joint2 - calibratedMinValues[0]) / (calibratedMaxValues[0] - calibratedMinValues[0]);
            float joint2_rotation_angle = joint2_norm * 90;
            joint2_rotation_angle = Mathf.Clamp(joint2_rotation_angle, 0, 90);
            currentJointAngles[0] = Mathf.Lerp(currentJointAngles[0], joint2_rotation_angle, Time.deltaTime * smoothSpeed);

            kanevJoints[0][0] = Quaternion.Euler(
                initialRotations[0].x,
                initialRotations[0].y,
                currentJointAngles[0]
            );
        }


        //Debug.Log($"kanevJoints[0][0]: {kanevJoints[0][0]}");

        // thumb2 broken (calibratedMinValues[9])
        if (calibratedMinValues[9] == calibratedMaxValues[9])
        {
            kanevJoints[0][1] = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            float joint11_norm = (joint11 - calibratedMinValues[9]) / (calibratedMaxValues[9] - calibratedMinValues[9]);
            float joint11_rotation_angle = joint11_norm * 90;
            joint11_rotation_angle = Mathf.Clamp(joint11_rotation_angle, 0, 90);
            currentJointAngles[9] = Mathf.Lerp(currentJointAngles[10], joint11_rotation_angle, Time.deltaTime * smoothSpeed);

            kanevJoints[0][1] = Quaternion.Euler(
                thumbPoints[1].transform.localRotation.eulerAngles.x,
                thumbPoints[1].transform.localRotation.eulerAngles.y,
                currentJointAngles[9]
            );
        }


        // thumb3 (calibratedMinValues[10])
        if (calibratedMinValues[10] == calibratedMaxValues[10])
        {
            kanevJoints[0][2] = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            float joint12_norm = (joint12 - calibratedMinValues[10]) / (calibratedMaxValues[10] - calibratedMinValues[10]);
            float joint12_rotation_angle = joint12_norm * 90;
            joint12_rotation_angle = Mathf.Clamp(joint12_rotation_angle, 0, 90);
            currentJointAngles[10] = Mathf.Lerp(currentJointAngles[10], joint12_rotation_angle, Time.deltaTime * smoothSpeed);

            kanevJoints[0][2] = Quaternion.Euler(
                thumbPoints[2].transform.localRotation.eulerAngles.x,
                thumbPoints[2].transform.localRotation.eulerAngles.y,
                currentJointAngles[10]
            );
        }

        // Non abbiamo proprio questo joint sul guanto di Kanev, quindi lo setto a 0
        kanevJoints[0][3] = Quaternion.Euler(0, 0, 0);



        // index1 (calibratedMinValues[1])
        if (calibratedMinValues[1] == calibratedMaxValues[1])
        {
            kanevJoints[1][0] = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            float joint3_norm = (joint3 - calibratedMinValues[1]) / (calibratedMaxValues[1] - calibratedMinValues[1]);
            float joint3_rotation_angle = joint3_norm * 90;
            joint3_rotation_angle = Mathf.Clamp(joint3_rotation_angle, 0, 120);
            currentJointAngles[1] = Mathf.Lerp(currentJointAngles[1], joint3_rotation_angle, Time.deltaTime * smoothSpeed);

            kanevJoints[1][0] = Quaternion.Euler(
                currentJointAngles[1],
                initialRotations[1].y,
                initialRotations[1].z
            );
        }


        // index2 (calibratedMinValues[2])
        if (calibratedMinValues[2] == calibratedMaxValues[2])
        {
            kanevJoints[1][1] = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            float joint4_norm = (joint4 - calibratedMinValues[2]) / (calibratedMaxValues[2] - calibratedMinValues[2]);
            float joint4_rotation_angle = joint4_norm * 90;
            joint4_rotation_angle = Mathf.Clamp(joint4_rotation_angle, 0, 90);
            currentJointAngles[2] = Mathf.Lerp(currentJointAngles[2], joint4_rotation_angle, Time.deltaTime * smoothSpeed);

            kanevJoints[1][1] = Quaternion.Euler(
                currentJointAngles[2],
                indexPoints[1].transform.localRotation.eulerAngles.y,
                indexPoints[1].transform.localRotation.eulerAngles.z
            );

        }

        // Non abbiamo proprio questi joint sul guanto di Kanev, quindi li setto a 0
        kanevJoints[1][2] = Quaternion.Euler(0, 0, 0);
        kanevJoints[1][3] = Quaternion.Euler(0, 0, 0);



        // middle1 (calibratedMinValues[3])
        if (calibratedMinValues[3] == calibratedMaxValues[3])
        {
            kanevJoints[2][0] = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            float joint5_norm = (joint5 - calibratedMinValues[3]) / (calibratedMaxValues[3] - calibratedMinValues[3]);
            float joint5_rotation_angle = joint5_norm * 90;
            joint5_rotation_angle = Mathf.Clamp(joint5_rotation_angle, 0, 120);
            currentJointAngles[3] = Mathf.Lerp(currentJointAngles[3], joint5_rotation_angle, Time.deltaTime * smoothSpeed);

            kanevJoints[2][0] = Quaternion.Euler(
                currentJointAngles[3],
                initialRotations[2].y,
                initialRotations[2].z
            );
        }

        // middle2 broken (calibratedMinValues[4])
        if (calibratedMinValues[4] == calibratedMaxValues[4])
        {
            kanevJoints[2][1] = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            float joint6_norm = (joint6 - calibratedMinValues[4]) / (calibratedMaxValues[4] - calibratedMinValues[4]);
            float joint6_rotation_angle = joint6_norm * 90;
            joint6_rotation_angle = Mathf.Clamp(joint6_rotation_angle, 0, 90);
            currentJointAngles[2] = Mathf.Lerp(currentJointAngles[2], joint6_rotation_angle, Time.deltaTime * smoothSpeed);

            kanevJoints[2][1] = Quaternion.Euler(
                currentJointAngles[4],
                middlePoints[1].transform.localRotation.eulerAngles.y,
                middlePoints[1].transform.localRotation.eulerAngles.z
            );

        }

        // ring1 (calibratedMinValues[5])
        if (calibratedMinValues[5] == calibratedMaxValues[5])
        {
            kanevJoints[3][0] = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            float joint7_norm = (joint7 - calibratedMinValues[5]) / (calibratedMaxValues[5] - calibratedMinValues[5]);
            float joint7_rotation_angle = joint7_norm * 90;
            joint7_rotation_angle = Mathf.Clamp(joint7_rotation_angle, 0, 120);
            currentJointAngles[5] = Mathf.Lerp(currentJointAngles[5], joint7_rotation_angle, Time.deltaTime * smoothSpeed);

            kanevJoints[3][0] = Quaternion.Euler(
                currentJointAngles[5],
                initialRotations[3].y,
                initialRotations[3].z
            );
        }


        // ring2 (calibratedMinValues[6])
        if (calibratedMinValues[6] == calibratedMaxValues[6])
        {
            kanevJoints[3][1] = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            float joint8_norm = (joint8 - calibratedMinValues[6]) / (calibratedMaxValues[6] - calibratedMinValues[6]);
            float joint8_rotation_angle = joint8_norm * 90;
            joint8_rotation_angle = Mathf.Clamp(joint8_rotation_angle, 0, 90);
            currentJointAngles[6] = Mathf.Lerp(currentJointAngles[6], joint8_rotation_angle, Time.deltaTime * smoothSpeed);

            kanevJoints[3][1] = Quaternion.Euler(
                currentJointAngles[6],
                ringPoints[1].transform.localRotation.eulerAngles.y,
                ringPoints[1].transform.localRotation.eulerAngles.z
            );
        }


        // pinky1 (calibratedMinValues[7])
        if (calibratedMinValues[7] == calibratedMaxValues[7])
        {
            kanevJoints[4][0] = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            float joint9_norm = (joint9 - calibratedMinValues[7]) / (calibratedMaxValues[7] - calibratedMinValues[7]);
            float joint9_rotation_angle = joint9_norm * 90;
            joint9_rotation_angle = Mathf.Clamp(joint9_rotation_angle, 0, 120);
            currentJointAngles[7] = Mathf.Lerp(currentJointAngles[7], joint9_rotation_angle, Time.deltaTime * smoothSpeed);

            kanevJoints[4][0] = Quaternion.Euler(
                currentJointAngles[7],
                initialRotations[4].y,
                initialRotations[4].z
            );
        }


        // pinky2 (calibratedMinValues[8])
        if (calibratedMinValues[8] == calibratedMaxValues[8])
        {
            kanevJoints[4][1] = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            float joint10_norm = (joint10 - calibratedMinValues[8]) / (calibratedMaxValues[8] - calibratedMinValues[8]);
            float joint10_rotation_angle = joint10_norm * 90;
            joint10_rotation_angle = Mathf.Clamp(joint10_rotation_angle, 0, 90);
            currentJointAngles[8] = Mathf.Lerp(currentJointAngles[8], joint10_rotation_angle, Time.deltaTime * smoothSpeed);

            kanevJoints[4][1] = Quaternion.Euler(
                currentJointAngles[8],
                pinkyPoints[1].transform.localRotation.eulerAngles.y,
                pinkyPoints[1].transform.localRotation.eulerAngles.z
            );
        }


    }

    public static Quaternion[][] GetKanevJoints()
    {
        return kanevJoints;
    }

    private IEnumerator CalibrateGlove()
    {
        Debug.Log("Calibration: keep your hand open for 3 seconds...");
        yield return CollectSamples(jointSamplesOpen, 3); // Raccogli samples per 3 secondi

        Debug.Log("Calibration: now close your hand for 3 seconds...");
        yield return CollectSamples(jointSamplesClosed, 3); // Raccogli samples per 3 secondi

        // Calcola i valori min e max filtrati
        CalculateCalibrationValues();

        isCalibrated = true;
        Debug.Log("Calibration complete!");
    }

    private IEnumerator CollectSamples(List<float>[] jointSamples, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            string data = udpReceiver.data;

            // Raccogli i sample
            if (!string.IsNullOrEmpty(data))
            {
                string[] points = data.Split(',');

                for (int i = 0; i < 11; i++)
                {
                    float jointValue = float.Parse(points[points.Length - 18 + i]);
                    jointSamples[i].Add(jointValue);
                }
            }

            elapsedTime += Time.deltaTime; // Incrementa il tempo trascorso

            // Mostra il tempo rimanente nella console
            Debug.Log($"Tempo rimanente: {Mathf.Max(0, duration - elapsedTime):F1} secondi");

            yield return null; // Aspetta il prossimo frame
        }
    }

    private void CalculateCalibrationValues()
    {
        for (int i = 0; i < 11; i++)
        {
            // Filtra i campioni per la mano distesa
            calibratedMinValues[i] = FilterSamples(jointSamplesOpen[i]);

            // Filtra i campioni per la mano chiusa
            calibratedMaxValues[i] = FilterSamples(jointSamplesClosed[i]);
        }

        //Debug.Log("Calibrazione completata. Valori minimi e massimi per ogni joint:");
        //for (int i = 0; i < 11; i++)
        //{
        //    Debug.Log($"{jointNames[i]}: Min = {calibratedMinValues[i]:F2}, Max = {calibratedMaxValues[i]:F2}");
        //}
    }

    private float FilterSamples(List<float> samples)
    {
        // Ordina i campioni
        samples.Sort();

        // Elimina il 10% dei valori estremi
        int removeCount = (int)(samples.Count * 0.1f); // 10% dei campioni
        samples = samples.GetRange(removeCount, samples.Count - removeCount * 2);

        // Calcola la media dei campioni rimasti
        float sum = 0f;
        foreach (float value in samples)
        {
            sum += value;
        }
        return sum / samples.Count;
    }

}
