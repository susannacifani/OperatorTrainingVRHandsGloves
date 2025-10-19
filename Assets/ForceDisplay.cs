using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public GameObject handModel;

    // Start is called before the first frame update
    void Start()
    {
        if (handModel != null)
        {
            handModel.SetActive(true);
        }
    }


}
