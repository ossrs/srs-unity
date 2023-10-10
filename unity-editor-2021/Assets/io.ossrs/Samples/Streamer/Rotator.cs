using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    void Start()
    {
    }

    void Update()
    {
        transform.Rotate(30 * Time.deltaTime, 60 * Time.deltaTime, 120 * Time.deltaTime);
    }
}
