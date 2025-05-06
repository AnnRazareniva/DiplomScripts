using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficStatistics : MonoBehaviour
{

    public static TrafficStatistics Instance { get; private set; }

    private class LaneData
    {
        public List<CarMove> cars = new List<CarMove>();
        public float totalWaitTime = 0f;
        public int passedCars = 0;
        public int queueCars = 0;
        public int carsInLastMinute = 0;
    }

    private Dictionary<string, LaneData> laneStats = new Dictionary<string, LaneData>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Инициализируем данные для всех полос
        laneStats["RL"] = new LaneData();
        laneStats["LR"] = new LaneData();
        laneStats["RR"] = new LaneData();
        laneStats["LL"] = new LaneData();
    }

    public void RegisterCar(CarMove car)//добавляем в словарь машину
    {
        string lane = car.GetCarType();
        if (laneStats.ContainsKey(lane))
        {
            laneStats[lane].cars.Add(car);
        }
        // Начинаем отсчёт времени при первом вызове метода
        if (!isCounting)
        {
            lastCheckTime = Time.time; // Начинаем отсчёт времени
            isCounting = true; // Устанавливаем флаг, что отсчёт времени уже начался
        }
    }

    public void UnregisterCar(CarMove car)//убираем из словаря когда проехали и добавляем что проехали
    {
        string lane = car.GetCarType();
        if (laneStats.ContainsKey(lane))
        {
            laneStats[lane].cars.Remove(car);
            laneStats[lane].passedCars++;

            
            // Передаем флаг о том, что машина прошла
            if (isCounting)
            {
                // Увеличиваем счётчик машин, прошедших за последний период
                laneStats[lane].carsInLastMinute++;
            }
        }
    }
    public void QueueCars(CarMove car)
    {
        string lane = car.GetCarType();
        if (!car.isCountedInQueue)
        {
            laneStats[lane].queueCars++;
            car.isCountedInQueue = true;
        }
    }

    public void AddWaitTime(CarMove car, float waitTime)//время ожидания на каждой полосе
    {
        string lane = car.GetCarType();
        if (laneStats.ContainsKey(lane))
        {
            laneStats[lane].totalWaitTime += waitTime;
        }
    }

    public float GetAverageQueueLength(string lane)//метод возвращает среднюю длину  очереди
    {
        if (!laneStats.ContainsKey(lane)) 
            return 0f;
        Debug.Log($" CarMove.trafficRedCount[lane]={Traffic_Lights.trafficRedCount[lane]}, [lane]={lane}");
        int averageQueueLength = laneStats[lane].queueCars / Traffic_Lights.trafficRedCount[lane];

        return averageQueueLength;
    }

    public float GetAverageWaitTime(string lane)//среднее время ожидания на каждой полосе
    {
        if (!laneStats.ContainsKey(lane)) 
            return 0f;
        LaneData data = laneStats[lane];
        if (data.passedCars == 0) 
            return 0f;

        return data.totalWaitTime / data.passedCars;
    }

    //public float GetCarsPerMinute(string lane)// среднее кол-во проехавших машин за минуту
    //{
    //    if (!laneStats.ContainsKey(lane)) 
    //        return 0f;
    //    // Предположим, что прошло N секунд с начала симуляции
    //    float minutesPassed = Time.time / 60f;
    //    if (minutesPassed == 0) 
    //        return 0f;

    //    return laneStats[lane].passedCars / minutesPassed;
    //}

    private float lastCheckTime = 0f;  // Время последней проверки
     // Количество машин за последнюю минуту
    private bool isCounting = false; // Флаг, чтобы отслеживать начало отсчёта

    public float GetCarsPerMinute(string lane)
    {
        float carsPerMinute;
        // Проверяем, есть ли статистика для полосы
        if (!laneStats.ContainsKey(lane))
            return 0f;

       

        // Получаем текущее время
        float currentTime = Time.time;

        // Если прошло 60 секунд, возвращаем результат и сбрасываем счётчик
        if (currentTime - lastCheckTime >= 60f)
        {
            carsPerMinute = laneStats[lane].carsInLastMinute; // Количество машин за последний минутный интервал

            // Сбрасываем отсчёт и счётчик
            lastCheckTime = 0f;
            isCounting = false; // Сбрасываем флаг, чтобы отсчёт можно было начать снова

            return carsPerMinute;
        }
        else
            return 0;
            
    }


    public float GetAverageIntersectionQueueLength()
    {
        float totalQueueLength = 0f;
        int laneCount = 0;

        foreach (var lane in laneStats)
        {
            totalQueueLength += GetAverageQueueLength(lane.Key);  
            laneCount++;
        }

        return totalQueueLength / laneCount;
    }

    public float GetAverageIntersectionWaitTime()
    {
        float totalWaitTime = 0f;
        int laneCount = 0;

        foreach (var lane in laneStats)
        {
            totalWaitTime += GetAverageWaitTime(lane.Key);
            laneCount++;
        }

        return totalWaitTime / laneCount;
    }

    public float GetCarsPerMinuteIntersection()
    {
        float totalCarsPerMinute = 0f;
        int laneCount = 0;

        foreach (var lane in laneStats)
        {
            totalCarsPerMinute += GetCarsPerMinute(lane.Key);  
            laneCount++;
        }

        return totalCarsPerMinute / laneCount;
    }
}
