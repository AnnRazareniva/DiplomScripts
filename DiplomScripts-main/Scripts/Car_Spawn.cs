using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;

public class Car_Spawn : MonoBehaviour
{
    public GameObject leftLaneCarPrefab; // ������ ��� ����� ������
    public GameObject rightLaneCarPrefab; // ������ ��� ������ ������

    public Transform leftLaneSpawnPoint;  // ����� ������ �� ����� ������
    public Transform rightLaneSpawnPoint; // ����� ������ �� ������ ������
    public Transform leftLeftLaneSpawnPoint;  // ����� ������ ��� ����� ����� ������ (LL)
    public Transform rightRightLaneSpawnPoint; // ����� ������ ��� ������ ������ ������ (RR)

    public float carMaxSpeed; // �������� �����
    public float carMinSpeed; // �������� �����
    private float carAcceleration;
    public float carStartSpeed;
    public float carTimeAcceleration;


    public Transform stopPositionLeftRight;  
    public Transform stopPositionRightLeft;  
    public Transform stopPositionLeftLeft;  
    public Transform stopPositionRightRight;  

    //����� � ������� ����������� �������
    public Transform leftLR_rightRLTurn;  
    public Transform rightLR_leftRLTurn;  
    public Transform leftLL_rightRRTurn;  
    public Transform rightLL_leftRRTurn;  

    public Transform leftLR_rightRLToward;  
    public Transform rightLR_leftRLToward;  
    public Transform leftLL_rightRRToward;  
    public Transform rightLL_leftRRToward;  

    public Transform leftLR_rightRLCenter;  
    public Transform rightLR_leftRLCenter;  
    public Transform leftLL_rightRRCenter;  
    public Transform rightLL_leftRRCenter;
    
    public Transform LR_Start;
    public Transform RL_Start;
    public Transform LL_Start;
    public Transform RR_Start;
    private string turnDirection = "";

    private string carType;
    private void Start()
    {
        //Debug.Log(carMaxSpeed);

        //carMaxSpeed = UnityEngine.Random.Range(40f, 50f);

        //carMaxSpeed = Mathf.Round((carMaxSpeed * 1000 / (60 * 60)) * 100)/100;
        ////Debug.Log(carMaxSpeed);
        //carAcceleration = Mathf.Round(((carMaxSpeed - carStartSpeed) / carTimeAcceleration) * 100) / 100;

        //Debug.Log(carAcceleration);


    }

    public void OwnSpeed()//����� ��� �������� 40-50, �������� ��� ������ ������
    {
        carMaxSpeed = UnityEngine.Random.Range(40f, 50f);

        carMaxSpeed = Mathf.Round((carMaxSpeed * 1000 / (60 * 60)) * 100) / 100;//������� � �/�
        //Debug.Log(carMaxSpeed);
        carAcceleration = Mathf.Round(((carMaxSpeed - carStartSpeed) / carTimeAcceleration) * 100) / 100;
    }

    //public void TurnChoice()
    //{
    //    float rand = UnityEngine.Random.value;

    //    if (rand < 0.3f) turnDirection = "right";
    //    else if (rand < 0.8f) turnDirection = "forward";
    //    else turnDirection = "left";
    //}

    //��� ������ ������ ���� ����� �.�. ����� �������������� ������ ��� ��� ������ ������
    public void SpawnCar_LR() 
    {
        if (IsCarNearSpawnPoint(leftLaneSpawnPoint, 5f)) // �������� �� ���������� 10 ������
        {
            Debug.Log("������ ������� ������ � ����� ������, ����� ��������.");
            return;
        }
        GameObject leftNewCar = Instantiate(leftLaneCarPrefab, leftLaneSpawnPoint.position, Quaternion.Euler(0, 180, 0));
        CarMove leftCarMove = leftNewCar.AddComponent<CarMove>();
        if (leftCarMove != null)
        {
            OwnSpeed();
            carType = "LR";
            leftCarMove.Initialize(carMaxSpeed, carAcceleration, carStartSpeed, stopPositionLeftRight, carType,
                rightLR_leftRLTurn, leftLR_rightRLTurn,
                leftLR_rightRLToward, rightLR_leftRLToward,
                leftLR_rightRLCenter, rightLR_leftRLCenter,
                LR_Start); // ���������� ����� Initialize
            leftCarMove.Change();
        }
    }
    public void SpawnCar_RL() 
    {
        if (IsCarNearSpawnPoint(rightLaneSpawnPoint, 5f)) // �������� �� ���������� 10 ������
        {
            Debug.Log("������ ������� ������ � ����� ������, ����� ��������.");
            return;
        }

        // ����� ���������� �� ������ ������
        GameObject rightNewCar = Instantiate(rightLaneCarPrefab, rightLaneSpawnPoint.position, Quaternion.identity);
        CarMove rightCarMove = rightNewCar.AddComponent<CarMove>();
        if (rightCarMove != null)
        {
            OwnSpeed();
            carType = "RL";
            rightCarMove.Initialize(carMaxSpeed, carAcceleration, carStartSpeed, stopPositionRightLeft, carType,
                leftLR_rightRLTurn, rightLR_leftRLTurn,
                rightLR_leftRLToward, leftLR_rightRLToward,
                rightLR_leftRLCenter, leftLR_rightRLCenter,
                RL_Start); // ���������� ����� Initialize
            rightCarMove.Change();
        }
    }
    public void SpawnCar_LL() 
    {
        if (IsCarNearSpawnPoint(leftLeftLaneSpawnPoint, 5f)) // �������� �� ���������� 10 ������
        {
            Debug.Log("������ ������� ������ � ����� ������, ����� ��������.");
            return;
        }
        // ����� ���������� �� ����� ����� ������
        GameObject leftLeftNewCar = Instantiate(leftLaneCarPrefab, leftLeftLaneSpawnPoint.position, Quaternion.Euler(0, 270, 0));
        CarMove leftLeftCarMove = leftLeftNewCar.AddComponent<CarMove>();
        if (leftLeftCarMove != null)
        {
            OwnSpeed();
            carType = "LL";
            leftLeftCarMove.Initialize(carMaxSpeed, carAcceleration, carStartSpeed, stopPositionLeftLeft, carType,
                rightLL_leftRRTurn, leftLL_rightRRTurn,
                leftLL_rightRRToward, rightLL_leftRRToward,
                leftLL_rightRRCenter, rightLL_leftRRCenter,
                LL_Start); // ���������� ����� Initialize
            leftLeftCarMove.Change();
        }

    }
    public void SpawnCar_RR() 
    {
        if (IsCarNearSpawnPoint(rightRightLaneSpawnPoint, 5f)) // �������� �� ���������� 10 ������
        {
            Debug.Log("������ ������� ������ � ����� ������, ����� ��������.");
            return;
        }
        // ����� ���������� �� ������ ������ ������
        GameObject rightRightNewCar = Instantiate(rightLaneCarPrefab, rightRightLaneSpawnPoint.position, Quaternion.Euler(0, 90, 0));
        CarMove rightRightCarMove = rightRightNewCar.AddComponent<CarMove>();
        if (rightRightCarMove != null)
        {
            OwnSpeed();
            carType = "RR";
            rightRightCarMove.Initialize(carMaxSpeed, carAcceleration, carStartSpeed, stopPositionRightRight, carType,
                leftLL_rightRRTurn, rightLL_leftRRTurn,
                rightLL_leftRRToward, leftLL_rightRRToward,
                rightLL_leftRRCenter, leftLL_rightRRCenter,
                RR_Start); // ���������� ����� Initialize
            rightRightCarMove.Change();
        }
    }

    public bool IsCarNearSpawnPoint(Transform spawnPoint, float minDistance)
    {
        // ������� ��� �������, ������� �������� ��������
        GameObject[] allCars = GameObject.FindGameObjectsWithTag("Car");
        foreach (var car in allCars)
        {
            // ��������� ���������� �� ����� ������ �� ������
            float distance = Vector3.Distance(spawnPoint.position, car.transform.position);

            if (distance < minDistance)
            {
                return true; // ���� ������ ������� ������, ���������� true
            }
        }
        return false; // ���� ��� ����� �����, ���������� false
    }

    //public void SpawnCar()
    //{
    //    // ����� ���������� �� ����� ������

    //    GameObject leftNewCar = Instantiate(leftLaneCarPrefab, leftLaneSpawnPoint.position, Quaternion.Euler(0, 180, 0));
    //    CarMove leftCarMove = leftNewCar.AddComponent<CarMove>();
    //    if (leftCarMove != null)
    //    {
    //        OwnSpeed();
    //        carType = "LR";
    //        leftCarMove.Initialize(carMaxSpeed, carAcceleration, carStartSpeed, stopPositionLeftRight, carType, 
    //            rightLR_leftRLTurn, leftLR_rightRLTurn, 
    //            leftLR_rightRLToward, rightLR_leftRLToward, 
    //            leftLR_rightRLCenter, rightLR_leftRLCenter); // ���������� ����� Initialize
    //        leftCarMove.Change();
    //    }

    //    // ����� ���������� �� ������ ������
    //    GameObject rightNewCar = Instantiate(rightLaneCarPrefab, rightLaneSpawnPoint.position, Quaternion.identity);
    //    CarMove rightCarMove = rightNewCar.AddComponent<CarMove>();
    //    if (rightCarMove != null)
    //    {
    //        OwnSpeed();
    //        carType = "RL";
    //        rightCarMove.Initialize(carMaxSpeed, carAcceleration, carStartSpeed, stopPositionRightLeft, carType,
    //            leftLR_rightRLTurn, rightLR_leftRLTurn,
    //            rightLR_leftRLToward, leftLR_rightRLToward,
    //            rightLR_leftRLCenter, leftLR_rightRLCenter); // ���������� ����� Initialize
    //        rightCarMove.Change();
    //    }

    //    // ����� ���������� �� ����� ����� ������
    //    GameObject leftLeftNewCar = Instantiate(leftLaneCarPrefab, leftLeftLaneSpawnPoint.position, Quaternion.Euler(0, 270, 0));
    //    CarMove leftLeftCarMove = leftLeftNewCar.AddComponent<CarMove>();
    //    if (leftLeftCarMove != null)
    //    {
    //        OwnSpeed();
    //        carType = "LL";
    //        leftLeftCarMove.Initialize(carMaxSpeed, carAcceleration, carStartSpeed, stopPositionLeftLeft, carType,
    //            rightLL_leftRRTurn, leftLL_rightRRTurn,
    //            leftLL_rightRRToward, rightLL_leftRRToward,
    //            leftLL_rightRRCenter, rightLL_leftRRCenter); // ���������� ����� Initialize
    //        leftLeftCarMove.Change();
    //    }

    //    // ����� ���������� �� ������ ������ ������
    //    GameObject rightRightNewCar = Instantiate(rightLaneCarPrefab, rightRightLaneSpawnPoint.position, Quaternion.Euler(0, 90, 0));
    //    CarMove rightRightCarMove = rightRightNewCar.AddComponent<CarMove>();
    //    if (rightRightCarMove != null)
    //    {
    //        OwnSpeed();
    //        carType = "RR";
    //        rightRightCarMove.Initialize(carMaxSpeed, carAcceleration, carStartSpeed, stopPositionRightRight, carType,
    //            leftLL_rightRRTurn, rightLL_leftRRTurn,
    //            rightLL_leftRRToward, leftLL_rightRRToward,
    //            rightLL_leftRRCenter, leftLL_rightRRCenter); // ���������� ����� Initialize
    //        rightRightCarMove.Change();
    //    }

    //}

}
