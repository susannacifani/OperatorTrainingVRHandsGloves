using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class NetworkPlayerSpawner : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update

    [Space]
    public Transform spawnPoint;

    private int counter;

    void Start()
    {
        //GameObject np = GameObject.Find("NetworkPlayer");
        //SetTargetInvisible(np);

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private GameObject spawnedPlayerPrefab;

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        spawnedPlayerPrefab = PhotonNetwork.Instantiate("NetworkPlayer", spawnPoint.position, Quaternion.identity);
        

        //SetTargetVisible(spawnedPlayerPrefab);

    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        PhotonNetwork.Destroy(spawnedPlayerPrefab);
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
