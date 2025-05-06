using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;

public class Car_Spawn : MonoBehaviour
{
    public GameObject leftLaneCarPrefab; // Префаб для левой полосы
    public GameObject rightLaneCarPrefab; // Префаб для правой полосы

    public Transform leftLaneSpawnPoint;  // Точка спавна на левой полосе
    public Transform rightLaneSpawnPoint; // Точка спавна на правой полосе
    public Transform leftLeftLaneSpawnPoint;  // Точка спавна для левой левой полосы (LL)
    public Transform rightRightLaneSpawnPoint; // Точка спавна для правой правой полосы (RR)

    public float carMaxSpeed; // Скорость машин
    public float carMinSpeed; // Скорость машин
    private float carAcceleration;
    public float carStartSpeed;
    public float carTimeAcceleration;


    public Transform stopPositionLeftRight;  
    public Transform stopPositionRightLeft;  
    public Transform stopPositionLeftLeft;  
    public Transform stopPositionRightRight;  

    //Точки к которым совершается поворот
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

    public void OwnSpeed()//метод тут скорость 40-50, рандомно для каждой машины
    {
        carMaxSpeed = UnityEngine.Random.Range(40f, 50f);

        carMaxSpeed = Mathf.Round((carMaxSpeed * 1000 / (60 * 60)) * 100) / 100;//перевод в м/с
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

    //Для каждой машины свой спавн т.к. вызов пуассоновского потока идёт для каждой полосы
    public void SpawnCar_LR() 
    {
        if (IsCarNearSpawnPoint(leftLaneSpawnPoint, 5f)) // Проверка на расстояние 10 единиц
        {
            Debug.Log("Машина слишком близко к точке спавна, спавн отклонен.");
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
                LR_Start); // Используем метод Initialize
            leftCarMove.Change();
        }
    }
    public void SpawnCar_RL() 
    {
        if (IsCarNearSpawnPoint(rightLaneSpawnPoint, 5f)) // Проверка на расстояние 10 единиц
        {
            Debug.Log("Машина слишком близко к точке спавна, спавн отклонен.");
            return;
        }

        // Спавн автомобиля на правой полосе
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
                RL_Start); // Используем метод Initialize
            rightCarMove.Change();
        }
    }
    public void SpawnCar_LL() 
    {
        if (IsCarNearSpawnPoint(leftLeftLaneSpawnPoint, 5f)) // Проверка на расстояние 10 единиц
        {
            Debug.Log("Машина слишком близко к точке спавна, спавн отклонен.");
            return;
        }
        // Спавн автомобиля на левой левой полосе
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
                LL_Start); // Используем метод Initialize
            leftLeftCarMove.Change();
        }

    }
    public void SpawnCar_RR() 
    {
        if (IsCarNearSpawnPoint(rightRightLaneSpawnPoint, 5f)) // Проверка на расстояние 10 единиц
        {
            Debug.Log("Машина слишком близко к точке спавна, спавн отклонен.");
            return;
        }
        // Спавн автомобиля на правой правой полосе
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
                RR_Start); // Используем метод Initialize
            rightRightCarMove.Change();
        }
    }

    public bool IsCarNearSpawnPoint(Transform spawnPoint, float minDistance)
    {
        // Находим все объекты, которые являются машинами
        GameObject[] allCars = GameObject.FindGameObjectsWithTag("Car");
        foreach (var car in allCars)
        {
            // Проверяем расстояние от точки спавна до машины
            float distance = Vector3.Distance(spawnPoint.position, car.transform.position);

            if (distance < minDistance)
            {
                return true; // Если машина слишком близка, возвращаем true
            }
        }
        return false; // Если нет машин рядом, возвращаем false
    }

    //public void SpawnCar()
    //{
    //    // Спавн автомобиля на левой полосе

    //    GameObject leftNewCar = Instantiate(leftLaneCarPrefab, leftLaneSpawnPoint.position, Quaternion.Euler(0, 180, 0));
    //    CarMove leftCarMove = leftNewCar.AddComponent<CarMove>();
    //    if (leftCarMove != null)
    //    {
    //        OwnSpeed();
    //        carType = "LR";
    //        leftCarMove.Initialize(carMaxSpeed, carAcceleration, carStartSpeed, stopPositionLeftRight, carType, 
    //            rightLR_leftRLTurn, leftLR_rightRLTurn, 
    //            leftLR_rightRLToward, rightLR_leftRLToward, 
    //            leftLR_rightRLCenter, rightLR_leftRLCenter); // Используем метод Initialize
    //        leftCarMove.Change();
    //    }

    //    // Спавн автомобиля на правой полосе
    //    GameObject rightNewCar = Instantiate(rightLaneCarPrefab, rightLaneSpawnPoint.position, Quaternion.identity);
    //    CarMove rightCarMove = rightNewCar.AddComponent<CarMove>();
    //    if (rightCarMove != null)
    //    {
    //        OwnSpeed();
    //        carType = "RL";
    //        rightCarMove.Initialize(carMaxSpeed, carAcceleration, carStartSpeed, stopPositionRightLeft, carType,
    //            leftLR_rightRLTurn, rightLR_leftRLTurn,
    //            rightLR_leftRLToward, leftLR_rightRLToward,
    //            rightLR_leftRLCenter, leftLR_rightRLCenter); // Используем метод Initialize
    //        rightCarMove.Change();
    //    }

    //    // Спавн автомобиля на левой левой полосе
    //    GameObject leftLeftNewCar = Instantiate(leftLaneCarPrefab, leftLeftLaneSpawnPoint.position, Quaternion.Euler(0, 270, 0));
    //    CarMove leftLeftCarMove = leftLeftNewCar.AddComponent<CarMove>();
    //    if (leftLeftCarMove != null)
    //    {
    //        OwnSpeed();
    //        carType = "LL";
    //        leftLeftCarMove.Initialize(carMaxSpeed, carAcceleration, carStartSpeed, stopPositionLeftLeft, carType,
    //            rightLL_leftRRTurn, leftLL_rightRRTurn,
    //            leftLL_rightRRToward, rightLL_leftRRToward,
    //            leftLL_rightRRCenter, rightLL_leftRRCenter); // Используем метод Initialize
    //        leftLeftCarMove.Change();
    //    }

    //    // Спавн автомобиля на правой правой полосе
    //    GameObject rightRightNewCar = Instantiate(rightLaneCarPrefab, rightRightLaneSpawnPoint.position, Quaternion.Euler(0, 90, 0));
    //    CarMove rightRightCarMove = rightRightNewCar.AddComponent<CarMove>();
    //    if (rightRightCarMove != null)
    //    {
    //        OwnSpeed();
    //        carType = "RR";
    //        rightRightCarMove.Initialize(carMaxSpeed, carAcceleration, carStartSpeed, stopPositionRightRight, carType,
    //            leftLL_rightRRTurn, rightLL_leftRRTurn,
    //            rightLL_leftRRToward, leftLL_rightRRToward,
    //            rightLL_leftRRCenter, leftLL_rightRRCenter); // Используем метод Initialize
    //        rightRightCarMove.Change();
    //    }

    //}

}
