using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private PlayerMovement pm;
    [SerializeField] private TextMeshProUGUI xSpeedText;

    void Update()
    {
        if (pm != null && xSpeedText != null)
        {
            int speed = Mathf.RoundToInt(pm.GetXSpeed() * 100);
            int exclN = Mathf.Clamp(speed / 1000, 0, 3);
            string excl = new string('!', exclN);

            if (speed > 5000)
            {
                xSpeedText.text = "SPEED: HOW?!";
                return;
            }

            xSpeedText.text = "SPEED: " + Mathf.RoundToInt(speed).ToString() + excl;
        }
        else if (xSpeedText != null)
        {
            xSpeedText.text = "SPEED: N/A";
        }
    }
}
