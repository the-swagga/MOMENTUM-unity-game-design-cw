using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchPad : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private PlayerMovement pm;

    private void Start()
    {
        pm = player.GetComponent<PlayerMovement>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (pm != null && other.CompareTag("Player"))
            pm.LaunchPad();
    }
}
