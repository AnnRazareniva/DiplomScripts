using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car_Destroy : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // ���� ������ ����������� �� ������
        if (other.gameObject.CompareTag("Car"))
        {
            Destroy(other.gameObject); // ���������� ������
        }
    }
}
