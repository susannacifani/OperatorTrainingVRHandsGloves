using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public Light Model1Light;
    public Light Model2Light;
    public GameObject Model1Text;
    public GameObject Model2Text;

    bool model1Turned = false;
    bool model2Turned = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void turnModel1()
    {
        if (model1Turned)
        {
            Model1Light.enabled = false;
            Model1Text.SetActive(false);
            model1Turned = false;
        }
        else
        {
            Model1Light.enabled = true;
            Model1Text.SetActive(true);
            model1Turned = true;
        }

    }

    public void turnModel2()
    {
        if (model2Turned)
        {
            Model2Light.enabled = false;
            Model2Text.SetActive(false);
            model2Turned = false;
        }
        else
        {
            Model2Light.enabled = true;
            Model2Text.SetActive(true);
            model2Turned = true;
        }

    }

    public void turnOff()
    {
        Model1Light.enabled = false;
        Model1Text.SetActive(false);
        Model2Light.enabled = false;
        Model2Text.SetActive(false);
        model1Turned = false;
        model2Turned = false;
    }


}
