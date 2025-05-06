using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car_Destroy : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // если машина столкнулась со стеной
        if (other.gameObject.CompareTag("Car"))
        {
            Destroy(other.gameObject); // Уничтожаем машину
        }
    }
}
