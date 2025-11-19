using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Speedrun : MonoBehaviour
{
    private bool active = false;
    private float timer = 0.0f;

    private void Update()
    {
        if (active)
            timer += Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            active = true;
            timer = 0.0f;
        }
    }

    public string GetSpeedrunTime()
    {
        int minutes = Mathf.FloorToInt(timer / 60.0f);
        int seconds = Mathf.FloorToInt(timer % 60.0f);
        int mseconds = Mathf.FloorToInt((timer * 100.0f) % 100.0f);

        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, mseconds);
    }

    public bool GetActive()
    {
        return active;
    }
}
