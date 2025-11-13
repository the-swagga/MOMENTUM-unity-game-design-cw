using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{
    [SerializeField] private float sensX = 5.0f;
    [SerializeField] private float sensY = 5.0f;

    [SerializeField] private Transform look;
    private float rotX;
    private float rotY;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensX * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensY * Time.deltaTime;

        rotY += mouseX;
        rotX -= mouseY;
        rotX = Mathf.Clamp(rotX, -90.0f, 90.0f);

        transform.rotation = Quaternion.Euler(rotX, rotY, 0);
        look.rotation = Quaternion.Euler(-0, rotY, 0);
    }
}
