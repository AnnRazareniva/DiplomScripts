using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.Device;
using static Traffic_Lights;
using static UnityEngine.GraphicsBuffer;

public class CarMove : MonoBehaviour
{
    private float acceleration;
    private float maxSpeed;
    private float currentSpeed;

    private float frictionCoefficient = 0.8f; // Коэффициент трения (например, для сухого асфальта)
    private float g = 9.81f;

    private Transform stopLine;

    private GameObject raycastPoint;

    private string carType;  // Тип машины: 


    private bool isRed = false;
    private bool isYellow = false;
    private bool isGreen = false;
    private string trafficLightSide = "";

    private Transform rightTurnTarget;
    private Transform leftTurnTarget;
    Transform towardLeft;
    Transform towardRight;

    Transform centerLeft;
    Transform centerRight;

    Transform startTarget;

    private int turnPhase = 0; // 0 - нет поворота, 1 - к turnTarget, 2 - к toward

    private float seeCarDist = 20f;

    private bool passTraffic = false; // Проехала ли машина светофор
    
    private Rigidbody rb;

    void Start()
    {
        

        rb = GetComponent<Rigidbody>();

        // Создаём пустой объект для рейкаста
        if (raycastPoint == null)
        {
            raycastPoint = new GameObject("RaycastPoint");
            raycastPoint.transform.SetParent(transform);  // Делаем его дочерним объектом машины
            raycastPoint.transform.localPosition = new Vector3(0f, 0.2f, 0.5f);  // Позиционируем на передней оси машины
        }

        //if (turnProbability < 0f/* && isGreen*/)
        //{

        //определяем куда машина едет
            float rand = Random.value;
            turnProbability = rand;

            if (rand < 0.3f) turnDirection = "right";
            else if (rand < 0.8f) turnDirection = "forward";
            else turnDirection = "left";
        //}

    }

    public void Change()
    {
        Traffic_Lights trafficLights = FindObjectOfType<Traffic_Lights>();

        // Проверяем, есть ли объект светофора в сцене
        if (trafficLights != null)
        {
            // Подписываемся на событие только один раз
            if (carType == "LR" || carType == "RL")
            {
                trafficLightSide = "RL_LR";
                trafficLights.OnTrafficLightChanged += OnTrafficLightChanged;

                OnTrafficLightChanged(trafficLightSide, trafficLights.isRedLRRL, trafficLights.isYellowLRRL, trafficLights.isGreenLRRL);
            }
            else if (carType == "RR" || carType == "LL")
            {
                trafficLightSide = "RR_LL";
                trafficLights.OnTrafficLightChanged += OnTrafficLightChanged;

                OnTrafficLightChanged(trafficLightSide, trafficLights.isRedLLRR, trafficLights.isYellowLLRR, trafficLights.isGreenLLRR);
            }

            

        }
    }

    // Метод, который вызывается при изменении состояния светофора(собитие)
    private void OnTrafficLightChanged(string side, bool red, bool yellow, bool green)
    {
        if (side == trafficLightSide)
        {
            isRed = red;
            isYellow = yellow;
            isGreen = green;
                       
        }
    }

    // Метод для установки параметров машины(и точек для поворота)
    public void Initialize(float maxSpeed, float acceleration, float currentSpeed, Transform stopLine, string carType, 
        Transform rightTurnTarget, Transform leftTurnTarget, 
        Transform towardLeft, Transform towardRight, 
        Transform centerLeft, Transform centerRight,
        Transform startTarget)
    {
        this.maxSpeed = maxSpeed;
        this.acceleration = acceleration;
        this.currentSpeed = currentSpeed;
        this.stopLine = stopLine;
        //this.targetPositionRightLeft = targetPositionRight;
        this.towardLeft = towardLeft;
        this.towardRight = towardRight;
        this.centerLeft = centerLeft;
        this.centerRight = centerRight;
        this.carType = carType;
        this.rightTurnTarget = rightTurnTarget;
        this.leftTurnTarget = leftTurnTarget;
        this.startTarget = startTarget;
        TrafficStatistics.Instance.RegisterCar(this);//регистрируем машину
    }

    public string GetCarType()
    {
        return carType;
    }

    //НОВОЕ
    private bool isTurning = false;
    private string turnDirection = ""; // "right", "left", "forward"
    private Transform turnTarget; // Целевая точка поворота
    private Transform centerTarget;
    private Transform towardTarget;
    private float turnProbability = -1f; // чтобы выбрать направление только один раз
    //НОВОЕ

    private float halfCar = 4.3f / 2f;

    private bool hasStartTurn = false; // начал ли поворот при зелёном
    private bool turningCar = false;//машина едет прямо = false

    void Update()
    {
        // рейкаст для обнаружения машин перед собой
        RaycastHit[] hits;
        float raycastRange = 60f; // Дистанция, на которой проверяем наличие машин

        //точка рейкаста от пустого объекта, который движется с машиной(пустой объект у капота машины)
        Vector3 raycastOrigin = raycastPoint.transform.position;

        // Направление рейкаста 
        Vector3 raycastDirection = transform.forward;

        // Визуализация рейкаста
        //Debug.DrawRay(raycastOrigin, raycastDirection * raycastRange, Color.red, 0.1f);

        // Маска для  CarLayer (слой машин)
        int layerMask = LayerMask.GetMask(/*"WallLayer",*/ "CarLayer");

        // Выполнение рейкаста для получения всех объектов на пути
        hits = Physics.RaycastAll(raycastOrigin, raycastDirection, raycastRange, layerMask);

        RaycastHit nearestHit;

        //определение точки центра машины в зависимости от того куда едет
        //для определение далеко машина или нет
        if (turnDirection == "right")
        {
            AddCenter(centerRight);
        }
        else if (turnDirection == "left")
        {
            AddCenter(centerLeft);
        }
        else
        if (turnDirection == "forward")
        {
            AddCenter(centerRight);
        }

        //если поворачивает(и безопасно можно повернут), то машина "поворачивающая"
        if (!isTurning && turnDirection != "forward")
        {            
                if (turnDirection == "right")
                {
                    // Поворачиваем направо без условий
                    StartTurn(rightTurnTarget, /*centerRight,*/ towardRight);
                }
                else if (turnDirection == "left" && CanTurnLeftSafely())
                {
                    // Поворачиваем налево, если можно
                    StartTurn(leftTurnTarget, /*centerLeft,*/ towardLeft);
                }
        }

        //расстояние от капота машины до стоп-линии
        float distToStop = Mathf.Abs(Vector3.Dot(stopLine.position - transform.position, transform.forward) - halfCar);
        float distToCar = 100000f;

        //если впереди машина, то определяем расстояни едо неё(до ближайшей
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            nearestHit = hits[0];
            distToCar = nearestHit.distance;
        }

        // если  машина ближе чем стоп-линия то true 
        bool carIsNearest = distToCar <= distToStop;

        //проверяем проехала машина светофор или нет
        CheckPassTraffic();
        
        
        //логика вызова метода для движения машины
        if ((isRed || isYellow) && !passTraffic)  // Если сигнал красный или желтый и машина не проехала светофор то стоим в зависимости от ситуации
        {
            if (!hasStartTurn)  // Если поворот ещё не начат(машина не поворачивает (т.к. если на зелёно начала то должна доехать)
            {
                HandleCollision(carIsNearest, "red", distToCar, distToStop);
            }
        }
        else if (isGreen || hasStartTurn || passTraffic) //если проеахала, зелёный или поворачивает, то логика движения от ситуации
        {
            //Debug.Log($"  distToCar={distToCar}, distToStop={distToStop}");
            HandleCollision(carIsNearest, "green", distToCar, distToStop);
        }
        else
        {
            // Если нет столкновений, продолжаем движение
            ContinueMovement();
        }
        
        //считаем тормозной путь(реакция + само торможение)
        float brakingDistance = CalculateBrakingDistance(currentSpeed);

        //если мы первые перед светофором и приближаемся, и не можем поехать, то тормозим
        if (!carIsNearest && distToStop <= brakingDistance || distToStop < 1.55)
        {
            if (turnDirection == "left" && !CanTurnLeftSafely() && !passTraffic)
            {
                ApplyBrakesDot(distToStop, 1.5f);
            }
        }

        //расстояние до точки начала поворота
        float distToStart = Vector3.Distance(transform.position, startTarget.position)/* / 2*/ /* - halfCar*/;

        //если близки(1 м) то машина поворачивает
        if (distToStart <= 1f && turningCar)
        {
            hasStartTurn = true;
            isTurning = true;
        }

        //если всё ок, то начинаем поворот)
        if (isTurning && turnTarget != null/* && distToStart <= 2f*/)
        {
            //hasStartTurn = true;
            TurnCar();
            return; // Не продолжаем обычное движение
        }

    }
    private Vector3 turnStartPos;
    void CheckPassTraffic()
    {
        float threshold = 0.5f;

        if (!passTraffic)//если машина ещё не проехала светофор
        {
            //расстояние до стоп-линии
            float dist = Vector3.Distance(transform.position, stopLine.position) - halfCar;
            if ((turnDirection == "forward" || turnDirection == "right") && centerTarget != null)//если прямо или направо, то у них одна точка по которой проверяют поехала или нет)
            {

                if (dist < threshold)
                {
                    passTraffic = true;//проехала светофор
                    TrafficStatistics.Instance.UnregisterCar(this);
                    isCountedInQueue = true;
                }
            }
            else if (turnDirection == "left" && turnTarget != null)//если налево то у неё другая точка проверки
            {
                //float dist = Vector3.Distance(transform.position, stopLine.position) - halfCar;
                if (dist < threshold)
                {
                    passTraffic = true;//проехала светофор
                    TrafficStatistics.Instance.UnregisterCar(this);
                    isCountedInQueue = true;
                }
            }
            
        }
    }

    void StartTurn(Transform target, /*Transform center,*/ Transform toward)//задаём что машина поворачивает и проверяем задалась ли точка-центр
    {
        turningCar = true;
        if (centerTarget == null)
        {
            //Debug.LogError("centerTarget == null в StartTurnPhase!");
            isTurning = false;
            return;
        }
        turnStartPos = startTarget.position;

        turnTarget = target;
        towardTarget = toward;

        turnPhase = 1;//фаза поворота
        StartTurnPhase();
        //Debug.Log($" StartTurn: target={target?.name}, center={centerTarget?.name}, toward={towardTarget?.name}");
    }

    void AddCenter(Transform center)
    {
        centerTarget = center;

        //Debug.Log($"Задан центр поворота: {center?.name}");
    }

    float t = 0f; // прогресс по дуге
    private Vector3 bezierP0, bezierP1, bezierP2;
    //private float turnSpeed = 1.3f;
    private Quaternion smoothedRotation;

    void TurnCar()
    {
        if (turnPhase == 1)//от положения машины до точки
        {
            Vector3 flatCurrent = transform.position;
            flatCurrent.y = 0f;

            Vector3 flatTarget = turnStartPos;
            flatTarget.y = 0f;

            Vector3 direction = (flatTarget - flatCurrent).normalized;

            currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

            // Поворот машины в сторону движения
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            // близки к начале поворота, то переход на фазу 2
            if (Vector3.Distance(flatCurrent, flatTarget) < 0.1f)
            {
                turnPhase = 2;
                t = 0f; 
            }

            return;
        }
        else if (turnPhase == 2)//кривая Безье
        {
            Vector3 bezierPos, bezierTangent;

            currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

            //float estimatedLength = (bezierP0 - bezierP1).magnitude + (bezierP1 - bezierP2).magnitude;
            //float deltaT = (currentSpeed / estimatedLength) * Time.deltaTime;

            //t += deltaT;
            //t = Mathf.Clamp01(t);

            bezierPos = Mathf.Pow(1 - t, 2) * bezierP0 +
                        2 * (1 - t) * t * bezierP1 +
                        Mathf.Pow(t, 2) * bezierP2;

            bezierTangent = 2 * (1 - t) * (bezierP1 - bezierP0) +
                            2 * t * (bezierP2 - bezierP1);

            //float estimatedLength = (bezierP0 - bezierP1).magnitude + (bezierP1 - bezierP2).magnitude;

            float tangentMagnitude = bezierTangent.magnitude;

            float deltaT = (currentSpeed / tangentMagnitude) * Time.deltaTime;

            t += deltaT;
            t = Mathf.Clamp01(t);


            bezierTangent.y = 0f;
            //реальное направление машины
            if (bezierTangent.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(bezierTangent.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }

            transform.position = bezierPos;

            if (t >= 1f || Vector3.Distance(transform.position, bezierP2) < 0.05f)//близки к точке завершения безье, то переходим на 3 фазу
            {
                t = 0f;
                turnPhase = 3;
            }

            return;
        }
        else if (turnPhase == 3)
        {
            // едем/выравниваемся от положения машины до точки выравнивания

            //Vector3 flatPos = turnTarget.position;
            Vector3 flatPos = transform.position;            
            flatPos.y = 0f;

            Vector3 flatTarget = towardTarget.position;
            flatTarget.y = 0f;
            currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

            Vector3 direction = (flatTarget - flatPos);

            if (Vector3.Distance(transform.position, towardTarget.position) < 1f)
            {
                //transform.position = towardTarget.position;
                isTurning = false;
                turnTarget = null;
                turnDirection = "";
                turnPhase = 0;
                turnProbability = -1f;
                return;
            }

            
            direction.Normalize();

            Vector3 alignedDirection = Vector3.zero;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
            {
                alignedDirection = new Vector3(Mathf.Sign(direction.x), 0f, 0f);
            }
            else
            {
                alignedDirection = new Vector3(0f, 0f, Mathf.Sign(direction.z));
            }

            if (alignedDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(alignedDirection);
                targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
                transform.rotation = targetRotation;
            }
            
        }
    }

    void StartTurnPhase()// задаём параметры для поворота безье
    {
        smoothedRotation = transform.rotation;
        float height = transform.position.y;
                
        if (turnPhase == 1)
        {
            bezierP0 = turnStartPos;
            bezierP1 = centerTarget.position;
            bezierP2 = turnTarget.position;

            bezierP0.y = height;
            bezierP1.y = height;
            bezierP2.y = height;

            t = 0f;

            //сохраняем скорость
            currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
        }
    }

    bool CanTurnLeftSafely()//проверка может машина поворачивать налево или нет
    {
        GameObject[] allCars = GameObject.FindGameObjectsWithTag("Car");//все объекты машины

        List<(GameObject car, float distance)> oppositeCars = new List<(GameObject, float)>();//список встречных машин
        Transform center = centerTarget;//точка до которой расчитывается расстояние от машины

        foreach (GameObject car in allCars)//цикл заполнения списка встречных машин
        {
            if (car == gameObject) continue;

            CarMove otherCar = car.GetComponent<CarMove>();
            if (otherCar == null) continue;

            string otherType = otherCar.GetCarType();

            bool isOnOppositePath =
                (carType == "LR" && otherType == "RL") ||
                (carType == "RL" && otherType == "LR") ||
                (carType == "LL" && otherType == "RR") ||
                (carType == "RR" && otherType == "LL");

            if (!isOnOppositePath) continue;

            float distanceToCenter = Vector3.Distance(car.transform.position, center.position);
            oppositeCars.Add((car, distanceToCenter));
        }

        if (oppositeCars.Count == 0) //если нет встречных, машина может поворачивать
            return true;

        // сортируем по расстоянию
        oppositeCars.Sort((a, b) => a.distance.CompareTo(b.distance));

        // проверяем самую ближайшую встречную машину
        var (nearestCar, nearestDistance) = oppositeCars[0];
        CarMove nearestMove = nearestCar.GetComponent<CarMove>();

        // если обе машины поворачивают налево(наша и встречная), машина может поворачивать
        if (turnDirection == "left" && nearestMove.turnDirection == "left")
        {
            return true;
        }

        // проверяем первые 3 машины(пропускаем)
        int checkCount = Mathf.Min(oppositeCars.Count, 3);
        for (int i = 0; i < checkCount; i++)
        {
            GameObject car = oppositeCars[i].car;
            float distance = oppositeCars[i].distance;

            CarMove otherCar = car.GetComponent<CarMove>();
            string direction = otherCar.turnDirection;

            if (direction == "left")
            {
                continue; // поворачивает налево, не мешает
            }

            if ((direction == "forward" || direction == "right") && distance < seeCarDist)
            {
                return false; // мешает
            }
        }

        return true;
    }

    public bool isCountedInQueue = false; // была ли уже учтена в очереди
    private bool isWaiting = false;
    private float waitStartTime = 0f;
    void HandleCollision(bool carIsNearest, string color, float distToCar, float distToStop)//метод логики езды машины
    {
        float brakingDistance = CalculateBrakingDistance(currentSpeed);
        float distanceToTarget = carIsNearest ? distToCar : distToStop;
        //float waitingTime = Time.deltaTime;

        if (color == "red")
        {
            //Debug.Log($"[HANDLE] red: carIsNearest={carIsNearest}, distToCar={distToCar}, isTurning={isTurning}, currentSpeed={currentSpeed}");
            if (carIsNearest && (distanceToTarget < brakingDistance /*&& brakingDistance > 4*/ || distanceToTarget <= 3.55f))
            {
                // Если машина слишком близко, начинаем тормозить
                ApplyBrakesDot(distanceToTarget, 3.5f);
                if (currentSpeed <= 1.1f) 
                {
                    if (!isWaiting)
                    {
                        isWaiting = true;
                        waitStartTime = Time.time;
                        TrafficStatistics.Instance.QueueCars(this);
                    }
                }
                else
                {
                    if (isWaiting)
                    {
                        float waitDuration = Time.time - waitStartTime;
                        TrafficStatistics.Instance.AddWaitTime(this, waitDuration);
                        isWaiting = false;
                    }
                }
            }
            else if(!carIsNearest && (distanceToTarget < brakingDistance /*&& brakingDistance > 4*/ || distanceToTarget <= 1.55f))
            {
                ApplyBrakesDot(distanceToTarget, 1.5f);

                //if (currentSpeed <= 1f)
                //{
                //    TrafficStatistics.Instance.QueueCars(this);
                //    TrafficStatistics.Instance.AddWaitTime(this, waitingTime);
                //}
                if (currentSpeed <= 1f)
                {
                    if (!isWaiting)
                    {
                        isWaiting = true;
                        waitStartTime = Time.time;
                        TrafficStatistics.Instance.QueueCars(this);
                    }
                }
                else
                {
                    if (isWaiting)
                    {
                        float waitDuration = Time.time - waitStartTime;
                        TrafficStatistics.Instance.AddWaitTime(this, waitDuration);
                        isWaiting = false;
                    }
                }
            }

            else
            {
                ContinueMovement();
            }
            
            //Debug.Log($"[HANDLE] RED: carIsNearest={carIsNearest}, distToCar={distToCar}, distTostop={distToStop}, isTurning={isTurning}, currentSpeed={currentSpeed}, " +
            //    $"passTraffic ={passTraffic}, carType ={carType}, turnDirection = {turnDirection}, distanceToTarget = {distanceToTarget}");
        }
        else
        if (color == "green")
        {
            //Debug.Log($"[HANDLE] GREEN: carIsNearest={carIsNearest}, distToCar={distToCar}, distTostop={distToStop}, isTurning={isTurning}, currentSpeed={currentSpeed}, " +
            //    $"passTraffic ={ passTraffic}, carType ={carType}, turnDirection = {turnDirection}, distanceToTarget = {distanceToTarget}");
            // Если это машина


            if (carIsNearest)
            {
                
                //нужен код
                if (distanceToTarget < 0.5f && isTurning)//если поворачиваем, чтобы не было конфликта при повороте, поэтому 1
                {
                    // Если машина слишком близко, начинаем тормозить
                    ApplyBrakesDot(distanceToTarget, 0.5f);
                    //if (currentSpeed <= 1f)
                    //{
                    //    TrafficStatistics.Instance.AddWaitTime(this, waitingTime);

                    //}
                    if (currentSpeed <= 1f)
                    {
                        if (!isWaiting)
                        {
                            isWaiting = true;
                            waitStartTime = Time.time;
                        }
                    }
                    else
                    {
                        if (isWaiting)
                        {
                            float waitDuration = Time.time - waitStartTime;
                            TrafficStatistics.Instance.AddWaitTime(this, waitDuration);
                            isWaiting = false;
                        }
                    }
                }
                else
                if (distanceToTarget <= 2.55 && !isTurning)//если не поворачиваем, просто едем
                {
                    // Если машина слишком близко, тормозим 
                    ApplyBrakesDot(distanceToTarget, 2.5f);
                    //if (currentSpeed <= 1f)
                    //{
                    //    TrafficStatistics.Instance.AddWaitTime(this, waitingTime);
                    //}
                    if (currentSpeed <= 1f)
                    {
                        if (!isWaiting)
                        {
                            isWaiting = true;
                            waitStartTime = Time.time;
                        }
                    }
                    else
                    {
                        if (isWaiting)
                        {
                            float waitDuration = Time.time - waitStartTime;
                            TrafficStatistics.Instance.AddWaitTime(this, waitDuration);
                            isWaiting = false;
                        }
                    }
                }
                else
                if ((distanceToTarget < brakingDistance || distanceToTarget <= 2.55f/* + 1.5f*/) && !isTurning) //добавила 1.5 для кадров
                {
                    // Если машина слишком близко, тормозим 
                    ApplyBrakesDot(distanceToTarget, 2.5f);
                    //if (currentSpeed <= 1f)
                    //{
                    //    TrafficStatistics.Instance.AddWaitTime(this, waitingTime);
                    //}
                    if (currentSpeed <= 1f)
                    {
                        if (!isWaiting)
                        {
                            isWaiting = true;
                            waitStartTime = Time.time;
                        }
                    }
                    else
                    {
                        if (isWaiting)
                        {
                            float waitDuration = Time.time - waitStartTime;
                            TrafficStatistics.Instance.AddWaitTime(this, waitDuration);
                            isWaiting = false;
                        }
                    }
                }
                else
                {
                    // Если расстояние достаточное, продолжаем движение
                    ContinueMovement();
                }
            }
            else if (!carIsNearest && !passTraffic)
            {
                ContinueMovement();
            }
            else if (!carIsNearest && passTraffic)
            {
                distanceToTarget = distToCar;
                if (distanceToTarget < brakingDistance || distanceToTarget <= 2.55f/* + 1.5f*/)//добавила 1.5 для кадров
                {
                    // Если машина слишком близко, тормозим
                    ApplyBrakesDot(distanceToTarget, 2.5f);
                    //if (currentSpeed <= 1f)
                    //{
                    //    TrafficStatistics.Instance.AddWaitTime(this, waitingTime);
                    //}
                    if (currentSpeed <= 1f)
                    {
                        if (!isWaiting)
                        {
                            isWaiting = true;
                            waitStartTime = Time.time;
                        }
                    }
                    else
                    {
                        if (isWaiting)
                        {
                            float waitDuration = Time.time - waitStartTime;
                            TrafficStatistics.Instance.AddWaitTime(this, waitDuration);
                            isWaiting = false;
                        }
                    }
                }
                else
                {
                    // Если расстояние достаточное, продолжаем движение
                    ContinueMovement();
                }
            }
            else
            {
                // Если расстояние достаточное, продолжаем движение
                ContinueMovement();
            }

        }
    }

    void ContinueMovement()//машина едет прямо/ускоряется
    {
        if (currentSpeed < maxSpeed)
        {
            currentSpeed += acceleration * Time.deltaTime;
        }
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    void ApplyBrakesDot(float distanceToTarget, float distance)
    {
        // Рассчитываем тормозное усилие в зависимости от оставшегося расстояния
        float brakeFactor = Mathf.Clamp01(distanceToTarget / CalculateBrakingDistance(currentSpeed));

        // Экстренное торможение на последних метрах
        if (distanceToTarget < 7f) 
        {
            // Плавно увеличиваем тормозное усилие, чтобы оно стало очень сильным при уменьшении расстояния
            float emergencyBrakeFactor = Mathf.Lerp(50f, 200f, Mathf.Clamp01((5f - distanceToTarget) / 5f));  // Увеличиваем торможение по мере уменьшения дистанции
            currentSpeed -= (currentSpeed * emergencyBrakeFactor) * Time.deltaTime;
        }
        else
        {            
            currentSpeed -= (currentSpeed * brakeFactor * 0.8f) * Time.deltaTime;// Плавное торможение на большом расстоянии
        }
                
        currentSpeed = Mathf.Max(currentSpeed, 0f);// чтобы скорость не становилась отрицательной

        // Если скорость очень мала и до цели осталось мало места, принудительно останавливаем машину
        if (currentSpeed < 1f && distanceToTarget < distance)
        {
            currentSpeed = 0f;
            distanceToTarget = 0f;
        }
    }
 
    float CalculateBrakingDistance(float speed)//Остановочный путь
    {
        return speed*2 + Mathf.Pow(speed, 2) / (2 * frictionCoefficient * g);
    }    
}      
//void TurnCar()
    //{
    //    if (turnPhase == 1)
    //    {
    //        // Первая фаза — прямое движение к точке начала поворота (turnStartPos)
    //        Vector3 flatCurrent = transform.position;
    //        flatCurrent.y = 0f;

    //        Vector3 flatTarget = turnStartPos;
    //        flatTarget.y = 0f;

    //        Vector3 direction = (flatTarget - flatCurrent).normalized;

    //        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

    //        //transform.position += direction * Time.deltaTime * currentSpeed;

    //        // Поворот машины в сторону движения
    //        if (direction.sqrMagnitude > 0.001f)
    //        {
    //            Quaternion targetRotation = Quaternion.LookRotation(direction);
    //            targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
    //            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    //        }

    //        // Если близко к turnStartPos — переход на фазу 2
    //        if (Vector3.Distance(flatCurrent, flatTarget) < 0.1f)
    //        {
    //            turnPhase = 2;
    //            t = 0f; // сбросить прогресс кривой
    //        }

    //        return;
    //    }
    //    else if (turnPhase == 2)
    //    {
    //        // Вторая фаза — уже твоя кривая Безье
    //        Vector3 bezierPos, bezierTangent;

    //        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

    //        //float estimatedLength = (bezierP0 - bezierP1).magnitude + (bezierP1 - bezierP2).magnitude;
    //        //float deltaT = (currentSpeed / estimatedLength) * Time.deltaTime;

    //        //t += deltaT;
    //        //t = Mathf.Clamp01(t);

    //        bezierPos = Mathf.Pow(1 - t, 2) * bezierP0 +
    //                    2 * (1 - t) * t * bezierP1 +
    //                    Mathf.Pow(t, 2) * bezierP2;

    //        bezierTangent = 2 * (1 - t) * (bezierP1 - bezierP0) +
    //                        2 * t * (bezierP2 - bezierP1);

    //        //float estimatedLength = (bezierP0 - bezierP1).magnitude + (bezierP1 - bezierP2).magnitude;

    //        //float tangentMagnitude = bezierTangent.magnitude;
    //        float tangentMagnitude = Mathf.Max(bezierTangent.magnitude, 0.01f);
    //        float deltaT = (currentSpeed / tangentMagnitude) * Time.deltaTime;

    //        t += deltaT;
    //        t = Mathf.Clamp01(t);

    //        bezierTangent.y = 0f;

    //        if (bezierTangent.sqrMagnitude > 0.001f)
    //        {
    //            Quaternion targetRot = Quaternion.LookRotation(bezierTangent.normalized);
    //            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
    //        }

    //        transform.position = bezierPos;

    //        if (t >= 1f || Vector3.Distance(transform.position, bezierP2) < 0.05f)
    //        {
    //            //t = 0f;
    //            //turnPhase = 3;
    //            t = 0f;
    //            turnPhase = 3;

    //            //// Вот здесь мы ОДИН РАЗ запоминаем правильное направление!
    //            //Vector3 from = turnTarget.position;
    //            //from.y = 0f;
    //            //Vector3 to = towardTarget.position;
    //            //to.y = 0f;

    //            //fixedDirection = (to - from).normalized;
    //        }

    //        return;
    //    }
    //    else if (turnPhase == 3)
    //    {
    //        // Твоя старая фаза 3
    //        Vector3 flatCurrent = transform.position;
    //        flatCurrent.y = 0f;

    //        //currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

    //        //if (fixedDirection.sqrMagnitude > 0.001f)
    //        //{
    //        //    Quaternion targetRotation = Quaternion.LookRotation(fixedDirection);
    //        //    targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
    //        //    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
    //        //}

    //        //// Движение машины тут ты можешь отдельно прописать, если надо
    //        //// Например, движемся вперёд
    //        ////transform.position += fixedDirection * currentSpeed * Time.deltaTime;

    //        //if (Vector3.Distance(transform.position, towardTarget.position) < 0.05f)
    //        //{
    //        //    isTurning = false;
    //        //    turnTarget = null;
    //        //    turnDirection = "";
    //        //    turnPhase = 0;
    //        //    turnProbability = -1f;
    //        //}


    //        //Vector3 flatTarget = towardTarget.position;
    //        //flatTarget.y = 0f;

    //        //Vector3 direction = (flatTarget - flatCurrent);

    //        //if (direction.sqrMagnitude > 0.001f)
    //        //{
    //        //    direction.Normalize();

    //        //    Quaternion targetRotation = Quaternion.LookRotation(direction);
    //        //    targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);

    //        //    Мягкий поворот к цели
    //        //    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
    //        //}

    //        //Проверяем, что мы почти достигли цели(если нужно завершить поворот)
    //        //if (Vector3.Distance(flatCurrent, flatTarget) < 0.05f)
    //        //{
    //        //    isTurning = false;
    //        //    turnTarget = null;
    //        //    turnDirection = "";
    //        //    turnPhase = 0;
    //        //    turnProbability = -1f;
    //        //}

    //        //if (Vector3.Distance(transform.position, towardTarget.position) < 0.05f)
    //        //{
    //        //    isTurning = false;
    //        //    turnTarget = null;
    //        //    turnDirection = "";
    //        //    turnPhase = 0;
    //        //    turnProbability = -1f;
    //        //    return;
    //        //}

    //        //direction.Normalize();

    //        //Quaternion targetRotation = Quaternion.LookRotation(direction);
    //        //targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
    //        //transform.rotation = targetRotation;

    //        //transform.position += direction * Time.deltaTime * currentSpeed;
    //    }
    //}

    //void TurnCar()
    //{
    //    if (turnPhase == 1 || turnPhase == 2)
    //    {
    //        Vector3 bezierPos, bezierTangent;

    //        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

    //        //t += Time.deltaTime * turnSpeed;
    //        // Расчёт дистанции кривой
    //        float estimatedLength = (bezierP0 - bezierP1).magnitude + (bezierP1 - bezierP2).magnitude;
    //        float deltaT = (currentSpeed / estimatedLength) * Time.deltaTime;

    //        t += deltaT; // t зависит от скорости
    //        t = Mathf.Clamp01(t);

    //        // Позиция на кривой
    //        bezierPos = Mathf.Pow(1 - t, 2) * bezierP0 +
    //                    2 * (1 - t) * t * bezierP1 +
    //                    Mathf.Pow(t, 2) * bezierP2;

    //        // Тангента (производная кривой Безье)
    //        bezierTangent = 2 * (1 - t) * (bezierP1 - bezierP0) +
    //                        2 * t * (bezierP2 - bezierP1);

    //        bezierTangent.y = 0f;

    //        if (bezierTangent.sqrMagnitude > 0.001f)
    //        {
    //            Quaternion targetRot = Quaternion.LookRotation(bezierTangent.normalized);
    //            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
    //        }

    //        transform.position = bezierPos;

    //        // Переход между фазами
    //        if (t >= 1f || Vector3.Distance(transform.position, bezierP2) < 0.05f)
    //        {
    //            t = 0f;
    //            turnPhase = 3;
    //            //hasStartTurn = false;
    //        }

    //        return;
    //    }
    //    else if (turnPhase == 3)
    //    {
    //        Vector3 flatPos = turnTarget.position;
    //        flatPos.y = 0f;

    //        Vector3 flatTarget = towardTarget.position;
    //        flatTarget.y = 0f;

    //        Vector3 direction = (flatTarget - flatPos);

    //        if (Vector3.Distance(transform.position, towardTarget.position) < 0.05f)
    //        {
    //            isTurning = false;
    //            //hasStartTurn = false;
    //            turnTarget = null;
    //            turnDirection = "";
    //            turnPhase = 0;
    //            turnProbability = -1f;
    //            return;
    //        }

    //        direction.Normalize();


    //        Quaternion targetRotation = Quaternion.LookRotation(direction);
    //        targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
    //        transform.rotation = targetRotation;

    //        //if (currentSpeed < maxSpeed)
    //        //{
    //        //    currentSpeed += acceleration * Time.deltaTime;
    //        //}

    //        transform.position += direction * Time.deltaTime * currentSpeed/*currentSpeed*/;
    //    }
    //}

    //void TurnCar()
    //{
    //    // Считаем позицию переда машины
    //    Vector3 frontPosition = transform.position + transform.forward * halfCar;

    //    if (turnPhase == 1 || turnPhase == 2)
    //    {
    //        // Скорость сохраняется!
    //        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

    //        Vector3 bezierPos, bezierTangent;

    //        //t += Time.deltaTime * turnSpeed;
    //        // Расчёт дистанции кривой
    //        float estimatedLength = (bezierP0 - bezierP1).magnitude + (bezierP1 - bezierP2).magnitude;
    //        float deltaT = (currentSpeed / estimatedLength) * Time.deltaTime;

    //        t += deltaT; // t зависит от скорости
    //        t = Mathf.Clamp01(t);

    //        // Позиция на кривой Безье
    //        bezierPos = Mathf.Pow(1 - t, 2) * bezierP0 +
    //                    2 * (1 - t) * t * bezierP1 +
    //                    Mathf.Pow(t, 2) * bezierP2;

    //        // Тангента (производная кривой Безье)
    //        bezierTangent = 2 * (1 - t) * (bezierP1 - bezierP0) +
    //                        2 * t * (bezierP2 - bezierP1);

    //        bezierTangent.y = 0f;

    //        if (bezierTangent.sqrMagnitude > 0.001f)
    //        {
    //            Quaternion targetRot = Quaternion.LookRotation(bezierTangent.normalized);
    //            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
    //        }

    //        transform.position = bezierPos;

    //        // Переход между фазами
    //        if (t >= 1f || Vector3.Distance(frontPosition, bezierP2) < 0.05f)
    //        {
    //            t = 0f;
    //            turnPhase = 3;
    //            //hasStartTurn = false;
    //        }

    //        return;
    //    }
    //    else if (turnPhase == 3)
    //    {
    //        Vector3 flatPos = turnTarget.position;
    //        flatPos.y = 0f;

    //        Vector3 flatTarget = towardTarget.position;
    //        flatTarget.y = 0f;

    //        Vector3 direction = (flatTarget - flatPos);

    //        direction.Normalize();

    //        Quaternion targetRotation = Quaternion.LookRotation(direction);
    //        targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
    //        transform.rotation = targetRotation;

    //        transform.position += direction * Time.deltaTime * currentSpeed/*currentSpeed*/;

    //        // Обновляем frontPosition после движения
    //        frontPosition = transform.position + transform.forward * halfCar;

    //        if (Vector3.Distance(frontPosition, towardTarget.position) < 0.05f)
    //        {
    //            isTurning = false;
    //            //hasStartTurn = false;
    //            turnTarget = null;
    //            turnDirection = "";
    //            turnPhase = 0;
    //            turnProbability = -1f;
    //            return;
    //        }
    //    }
    //}
//2. НОВОЕ
        //if (!isTurning && isGreen && turnDirection != "forward")
        //{
        //    foreach (RaycastHit hit in hits)
        //    {
        //        if (hit.collider.CompareTag("wall") && hit.distance <= turnStartDistance)
        //        {
        //            if (turnDirection == "right")
        //            {
        //                // Поворачиваем направо без условий
        //                StartTurn(rightTurnTarget, /*centerRight,*/ towardRight);
        //            }
        //            else if (turnDirection == "left" && CanTurnLeftSafely())
        //            {
        //                // Поворачиваем налево, если можно
        //                StartTurn(leftTurnTarget, /*centerLeft,*/ towardLeft);
        //            }
        //            else if (turnDirection == "left" && !CanTurnLeftSafely())
        //            {
        //                if (hit.collider.CompareTag("wall") && hit.distance <= turnStartDistance + 1.5f)
        //                {
        //                    float stopDistance = hit.distance;
        //                    ApplyBrakesDot(stopDistance, 1.5f);
        //                    //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
        //                    return;
        //                }

        //            }
        //            break;
        //        }
        //    }
        //}

        //if (hits.Length > 0)
        //{
        //    // Сортируем объекты по расстоянию (ближайший объект будет первым в списке)
        //    System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        //    // Получаем ближайший объект
        //    RaycastHit nearestHit = hits[0];
        //    GameObject hitObject = nearestHit.collider.gameObject;

        //    if ((isRed || isYellow) && !hasCommittedTurn)
        //    {
        //        HandleCollision(nearestHit, "red");
        //    }
        //    else if (isGreen || hasCommittedTurn)
        //    {
        //        HandleCollision(nearestHit, "green");
        //    }
        //}
        //else
        //{
        //    // Если нет столкновений, продолжаем движение
        //    ContinueMovement();
        //}
//// Если это машина
            //if (hit.collider.CompareTag("Car"))
            //{                
            //    if (distanceToTarget < brakingDistance/* + 1.5f*/)//добавила 1.5 для кадров
            //    {
            //        // Если машина слишком близко, начинаем тормозить
            //        ApplyBrakesDot(distanceToTarget, 2.5f);
            //        //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
            //    }
            //    else
            //    {
            //        if (distanceToTarget <= 2.5f)
            //        {
            //            ApplyBrakesDot(distanceToTarget, 2.5f);
            //        }
            //        else
            //        {// Если расстояние достаточное, продолжаем движение
            //            ContinueMovement();
            //        }
            //    }
            //}
            //else if (hit.collider.CompareTag("wall"))
            //{
            //    if (distanceToTarget < brakingDistance/* + 1.5f*/)//добавила 1.5 для кадров
            //    {
            //        // Если машина слишком близко, начинаем тормозить
            //        ApplyBrakesDot(distanceToTarget, 2.5f);
            //        //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
            //    }
            //    else
            //    {
            //        if (distanceToTarget <= 2.5f)
            //        {
            //            ApplyBrakesDot(distanceToTarget, 2.5f);
            //        }
            //        else
            //        {// Если расстояние достаточное, продолжаем движение
            //            ContinueMovement();
            //        }
            //    }
            //}
            // Если это машина

            //if (distanceToTarget < brakingDistance/* + 1.5f*/)//добавила 1.5 для кадров
            //{
            //    // Если машина слишком близко, начинаем тормозить
            //    ApplyBrakesDot(distanceToTarget, 2.5f);
            //    //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
            //}
            //else
            //{
            //    if (distanceToTarget <= 2.5f)
            //    {
            //        ApplyBrakesDot(distanceToTarget, 2.5f);
            //    }
            //    else
            //    {// Если расстояние достаточное, продолжаем движение
            //        ContinueMovement();
            //    }
            //}   //            if (color == "red")
//        {
//            if (hit.collider.CompareTag("Car"))
//            {
//                if (distanceToTarget <= 4.5f)
//                {
//                    ApplyBrakesDot(distanceToTarget, 4f);
//}
//                else
//                    if (distanceToTarget < brakingDistance)
//{
//    ApplyBrakesDot(distanceToTarget, 7f);
//}


//// Если машина слишком близко, начинаем тормозить
//ApplyBrakesDot(distanceToTarget, 4f);
//                //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
//            }
//            else
//                if (hit.collider.CompareTag("wall"))
//{
//    if (distanceToTarget <= 4.5f)
//    {
//        ApplyBrakesDot(distanceToTarget, 3f);
//    }
//    else
//        if (distanceToTarget < brakingDistance)
//    {
//        ApplyBrakesDot(distanceToTarget, 7f);
//    }

//    ApplyBrakesDot(distanceToTarget, 3f);
//    //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
//}


//if (distanceToTarget < brakingDistance || distanceToTarget == 0)
//{


//}
//else
//{
//    if (currentSpeed < maxSpeed)
//    {
//        currentSpeed += acceleration * Time.deltaTime;
//    }
//    transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
//}
//            //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
//        }

//bool CanTurnLeftSafely()
    //{
    //    GameObject[] allCars = GameObject.FindGameObjectsWithTag("Car");

    //    foreach (GameObject car in allCars)
    //    {
    //        if (car == gameObject) continue;

    //        CarMove otherCar = car.GetComponent<CarMove>();
    //        if (otherCar == null) continue;

    //        string otherType = otherCar.GetCarType();

    //        bool isOnOppositePath =
    //            (carType == "LR" && otherType == "RL") ||
    //            (carType == "RL" && otherType == "LR") ||
    //            (carType == "LL" && otherType == "RR") ||
    //            (carType == "RR" && otherType == "LL");

    //        if (isOnOppositePath)
    //        {
    //            // Если встречная машина тоже поворачивает налево — не мешает нам, продолжаем
    //            if (otherCar.turnDirection == "left")
    //                continue;

    //            // Если встречная машина едет прямо или направо — проверяем расстояние
    //            bool isGoingStraightOrRight =
    //                otherCar.turnDirection == "forward" ||
    //                otherCar.turnDirection == "right";

    //            if (isGoingStraightOrRight)
    //            {
    //                float distanceToCar = Vector3.Distance(transform.position, otherCar.transform.position);
    //                if (distanceToCar < 50f)
    //                {
    //                    return false;
    //                }
    //            }
    //        }
    //    }

    //    return true;
    //}

    //bool CanTurnLeftSafely()
    //{
    //    GameObject[] allCars = GameObject.FindGameObjectsWithTag("Car");

    //    foreach (GameObject car in allCars)
    //    {
    //        if (car == gameObject) continue;

    //        CarMove otherCar = car.GetComponent<CarMove>();
    //        if (otherCar == null) continue;

    //        string otherType = otherCar.GetCarType();
    //        bool isOnOppositePath = (carType == "LR" && otherType == "RL") || (carType == "LL" && otherType == "RR");

    //        if (isOnOppositePath)
    //        {
    //            bool isGoingStraightOrRight = otherCar.turnDirection == "forward" || otherCar.turnDirection == "right" || string.IsNullOrEmpty(otherCar.turnDirection);

    //            if (isGoingStraightOrRight)
    //            {
    //                float otherSpeed = Mathf.Max(otherCar.currentSpeed, 0.1f);
    //                Vector3 intersectionPoint = transform.position + transform.forward * turnStartDistance; // приблизительно точка поворота
    //                float distanceToIntersection = Vector3.Distance(otherCar.transform.position, intersectionPoint);
    //                float timeToReach = distanceToIntersection / otherSpeed;

    //                float timeToTurn = 1.5f; // Сколько времени занимает поворот

    //                if (timeToReach < timeToTurn + 0.5f) // Если они почти одновременно будут на перекрёстке
    //                {
    //                    return false;
    //                }
    //            }
    //        }
    //    }

    //    return true;
    //}

    //7. НОВОЕ

    //void ApplyBrakesDot(float distanceToTarget, float distance)
    //{
    //    float brakingDistance = CalculateBrakingDistance(currentSpeed);

    //    // Если до объекта ближе, чем тормозной путь — начинаем торможение
    //    if (distanceToTarget < brakingDistance)
    //    {
    //        // Замедляем машину с той же скоростью, что и разгон, но в обратную сторону
    //        currentSpeed -= acceleration * Time.deltaTime;
    //    }

    //    // скорость не становится отрицательной
    //    currentSpeed = Mathf.Max(currentSpeed, 0f);

    //    // Если скорость очень мала и до цели осталось мало места, принудительно останавливаем машину
    //    if (currentSpeed < 0.5f && distanceToTarget < distance)
    //    {
    //        currentSpeed = 0f;
    //        distanceToTarget = 0f;
    //    }
    //}
//void TurnTowardsTarget()
    //{
    //    // Выбираем текущую целевую точку (первоначально это поворот, затем к точке "toward")
    //    Transform currentTarget = (turnPhase == 1) ? turnTarget : toward;
    //    if (currentTarget == null) return;

    //    // Направление к текущей целевой точке
    //    Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;

    //    // Рассчитываем угол между текущим направлением машины и направлением к цели
    //    float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

    //    // Плавный поворот — ограничиваем угол поворота, чтобы он не был слишком резким
    //    float rotationStep = Mathf.Sign(angleToTarget) * Mathf.Min(Mathf.Abs(angleToTarget), turnAnglePerSecond * Time.deltaTime);
    //    transform.Rotate(0, rotationStep, 0);

    //    //// Двигаемся вперёд (с учетом текущей скорости)
    //    //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

    //    // Проверяем, достигли ли мы целевой точки
    //    if (Vector3.Distance(transform.position, currentTarget.position) < 0.5f)
    //    {
    //        // Если цель — точка поворота, переходим к точке "toward"
    //        if (turnPhase == 1)
    //        {
    //            turnPhase = 2; // Теперь едем к точке "toward"
    //        }
    //        // Если цель — точка "toward", возвращаемся к прямому движению
    //        else if (turnPhase == 2)
    //        {
    //            isTurning = false;
    //            turnTarget = null;
    //            turnDirection = "";
    //            turnPhase = 0; // Завершаем поворот
    //            turnProbability = -1f;
    //        }
    //    }
    //}
    //void TurnTowardsTarget()
    //{
    //    // Выбираем текущую целевую точку (сначала точка поворота, потом точка "toward")
    //    Transform currentTarget = (turnPhase == 1) ? turnTarget : toward;
    //    if (currentTarget == null) return;

    //    // Направление к целевой точке (игнорируем ось Y, чтобы движение было по горизонтали)
    //    Vector3 directionToTarget = currentTarget.position - transform.position;
    //    directionToTarget.y = 0;  // Сбрасываем ось Y, чтобы движение было в горизонтальной плоскости
    //    float distanceToTarget = directionToTarget.magnitude;

    //    // Нормализуем вектор, чтобы это был единичный вектор
    //    directionToTarget.Normalize();

    //    // Плавное выравнивание машины по направлению к целевой точке
    //    // Мы поворачиваем машину плавно по направлению в целевую точку
    //    if (distanceToTarget > 0.1f)  // Если мы не достигли цели
    //    {
    //        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
    //        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnAnglePerSecond * Time.deltaTime);
    //    }

    //    // Двигаемся вперёд в направлении целевой точки с текущей скоростью
    //    //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

    //    // Проверка на достижение цели
    //    if (distanceToTarget < 0.1f)
    //    {
    //        // Если это точка поворота, переходим к точке "toward"
    //        if (turnPhase == 1)
    //        {
    //            turnPhase = 2;  // Теперь едем к точке "toward"
    //        }
    //        // Если это точка "toward", завершаем движение
    //        else if (turnPhase == 2)
    //        {
    //            isTurning = false;
    //            turnTarget = null;
    //            turnDirection = "";
    //            turnPhase = 0;  // Завершаем поворот
    //            turnProbability = -1f;
    //        }
    //    }
    //}







    //5. НОВОЕ


    //6. НОВОЕ
//using System.Collections;
//using System.Collections.Generic;
//using Unity.VisualScripting;
//using Unity.VisualScripting.Antlr3.Runtime.Tree;
//using UnityEngine;
//using UnityEngine.Device;
//using static Traffic_Lights;

//public class CarMove : MonoBehaviour
//{
//    private float acceleration;
//    private float maxSpeed;
//    private float currentSpeed;

//    private float frictionCoefficient = 0.8f; // Коэффициент трения (например, для сухого асфальта)
//    private float g = 9.81f;

//    private Transform targetPosition;

//    private GameObject raycastPoint;

//    private static string carType;  // Тип машины: 


//    private bool isRed = false;
//    private bool isYellow = false;
//    private bool isGreen = false;
//    private string trafficLightSide = "";



//    private Transform rightTurnTarget;
//    private Transform leftTurnTarget;

//    void Start()
//    {
//        // Создаём пустой объект для рейкаста, если его нет
//        if (raycastPoint == null)
//        {
//            raycastPoint = new GameObject("RaycastPoint");
//            raycastPoint.transform.SetParent(transform);  // Делаем его дочерним объектом машины
//            raycastPoint.transform.localPosition = new Vector3(0f, 0.5f, 0.5f);  // Позиционируем на передней оси машины
//        }        
//    }

//    public void Change()
//    {
//        Traffic_Lights trafficLights = FindObjectOfType<Traffic_Lights>();

//        // Проверяем, есть ли объект светофора в сцене
//        if (trafficLights != null)
//        {
//            // Подписываемся на событие только один раз
//            if (carType == "LR" || carType == "RL")
//                trafficLightSide = "RL_LR";
//            else if (carType == "RR" || carType == "LL")
//                trafficLightSide = "RR_LL";


//            trafficLights.OnTrafficLightChanged += OnTrafficLightChanged;

//            OnTrafficLightChanged(trafficLightSide, trafficLights.isRed, trafficLights.isYellow, trafficLights.isGreen);

//        }
//    }


//    // Метод, который будет вызван при изменении состояния светофора
//    private void OnTrafficLightChanged(string side, bool red, bool yellow, bool green)
//    {
//        // Логика, которая выполняется при изменении состояния светофора
//        Debug.Log($"Traffic light changed for {side}: Red={isRed}, Yellow={isYellow}, Green={isGreen}");

//        if (side == trafficLightSide)
//        {
//            isRed = red;
//            isYellow = yellow;
//            isGreen = green;
//        }
//    }

//    // Метод для установки параметров
//    public void Initialize(float maxSpeed, float acceleration, float currentSpeed, Transform targetPosition, string carTypee, Transform rightTurnTarget, Transform leftTurnTarget)
//    {
//        this.maxSpeed = maxSpeed;
//        this.acceleration = acceleration;
//        this.currentSpeed = currentSpeed;
//        this.targetPosition = targetPosition;
//        //this.targetPositionRightLeft = targetPositionRight;
//        carType = carTypee;
//        this.rightTurnTarget = rightTurnTarget;
//        this.leftTurnTarget = leftTurnTarget;
//    }

//    public string GetCarType()
//    {
//        return carType;
//    }

//    void Update()
//    {
//        Vector3 targetPos = targetPosition.position;

//        // Поиск ближайшего объекта (машина или стена)
//        RaycastHit[] hits;
//        float raycastRange = 30f; // Дистанция, на которой проверяем наличие объектов

//        // Получаем точку рейкаста от пустого объекта, который движется с машиной
//        Vector3 raycastOrigin = raycastPoint.transform.position;

//        // Направление рейкаста — передняя ось машины
//        Vector3 raycastDirection = transform.forward * raycastRange;

//        // Визуализация рейкаста в редакторе
//        Debug.DrawRay(raycastOrigin, raycastDirection, Color.red, 0.1f);

//        // Маска для слоев WallLayer и CarLayer (или любой другой слой для машин)
//        int layerMask = LayerMask.GetMask("WallLayer", "CarLayer");

//        // Выполнение рейкаста для получения всех объектов на пути
//        hits = Physics.RaycastAll(raycastOrigin, transform.forward, raycastRange, layerMask);

//        if (hits.Length > 0)
//        {
//            // Сортируем объекты по расстоянию (ближайший объект будет первым в списке)
//            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

//            // Получаем ближайший объект
//            RaycastHit nearestHit = hits[0];
//            GameObject hitObject = nearestHit.collider.gameObject;

//            if (isRed || isYellow)
//            {
//                HandleCollision(nearestHit, "red");
//            }
//            else if(isGreen)
//            {
//                HandleCollision(nearestHit, "green");
//            }
//        }
//        else
//        {
//            // Если нет столкновений, продолжаем движение
//            ContinueMovement();
//        }
//    }

//    void HandleCollision(RaycastHit hit, string color)
//    {
//        float distanceToTarget = hit.distance - 4.3f / 2;
//        float brakingDistance = CalculateBrakingDistance(currentSpeed);

//        if (color == "red")
//        {
//            if (distanceToTarget < brakingDistance || distanceToTarget == 0)
//            {
//                ApplyBrakesDot(distanceToTarget, 2f);
//            }
//            else
//            {
//                if (currentSpeed < maxSpeed)
//                {
//                    currentSpeed += acceleration * Time.deltaTime;
//                }
//            }
//            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
//        }
//        else
//        if(color == "green")
//        {
//            if (hit.collider.CompareTag("wall"))
//            {
//                ContinueMovement();
//            }
//            // Если это машина
//            else if (hit.collider.CompareTag("Car"))
//            {
//                //нужен код
//                if (distanceToTarget < brakingDistance)
//                {
//                    // Если машина слишком близко, начинаем тормозить
//                    ApplyBrakesDot(distanceToTarget, 2f);
//                }
//                else
//                {
//                    // Если расстояние достаточное, продолжаем движение
//                    ContinueMovement();
//                }
//            }

//        }
//    }


//    void ContinueMovement()
//    {
//        if (currentSpeed < maxSpeed)
//        {
//            currentSpeed += acceleration * Time.deltaTime;
//        }
//        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
//    }

//    void ApplyBrakesDot(float distanceToTarget, float distance)
//    {
//        // Рассчитываем тормозное усилие в зависимости от оставшегося расстояния
//        float brakeFactor = Mathf.Clamp01(distanceToTarget / CalculateBrakingDistance(currentSpeed));

//        // Экстренное торможение на последних метрах
//        if (distanceToTarget < 10f)  // Меньше 5 метров - экстренное торможение
//        {
//            // Плавно увеличиваем тормозное усилие, чтобы оно стало очень сильным при уменьшении расстояния
//            float emergencyBrakeFactor = Mathf.Lerp(50f, 200f, Mathf.Clamp01((5f - distanceToTarget) / 5f));  // Увеличиваем торможение по мере уменьшения дистанции
//            currentSpeed -= (currentSpeed * emergencyBrakeFactor) * Time.deltaTime;
//        }
//        else
//        {
//            // Плавное торможение на большом расстоянии
//            currentSpeed -= (currentSpeed * brakeFactor * 0.8f) * Time.deltaTime;
//        }

//        // скорость не становится отрицательной
//        currentSpeed = Mathf.Max(currentSpeed, 0f);

//        // Если скорость очень мала и до цели осталось мало места, принудительно останавливаем машину
//        if (currentSpeed < 0.5f && distanceToTarget < distance)
//        {
//            currentSpeed = 0f;
//            distanceToTarget = 0f;

//        }
//    }



//    float CalculateBrakingDistance(float speed)
//    {
//        return Mathf.Pow(speed, 2) / (2 * frictionCoefficient * g);
//    } 

//}

////void Update()
////{
////    Vector3 targetPos = targetPosition.position;

////    // Поиск ближайшей машины через рейкаст
////    RaycastHit hit;
////    float raycastRange = 30f; // Дистанция, на которой мы проверяем наличие машины

////    // Получаем точку рейкаста от пустого объекта, который движется с машиной
////    Vector3 raycastOrigin = raycastPoint.transform.position;

////    // Направление рейкаста — передняя ось машины
////    Vector3 raycastDirection = transform.forward * raycastRange;

////    // Визуализация рейкаста в редакторе
////    Debug.DrawRay(raycastOrigin, raycastDirection, Color.red, 0.1f);

////    // Маска для слоев WallLayer и CarLayer (или любой другой слой для машин)
////    int layerMask = LayerMask.GetMask("WallLayer", "CarLayer");


////    if (isRed || isYellow)
////    {
////        if (Physics.Raycast(raycastOrigin, transform.forward, out hit, raycastRange, layerMask))
////        {
////            // Если мы нашли машину
////            GameObject hitObject = hit.collider.gameObject;

////            // Если перед нами стена (проверка на тип объекта)
////            if (hit.collider.CompareTag("wall"))  // Стена должна быть помечена тегом "Wall"
////            {
////                //целевая точка)
////                float distanceToTarget = (float)(Vector3.Distance(transform.position, targetPos) - 4.3/* / 2*/);

////                // Рассчитываем тормозной путь для текущей скорости
////                float brakingDistance = CalculateBrakingDistance(currentSpeed);


////                // Если дистанция до цели меньше тормозного пути, начинаем тормозить
////                if (distanceToTarget < brakingDistance || distanceToTarget == 0)
////                {
////                    // Начинаем тормозить плавно (уменьшаем скорость)
////                    ApplyBrakesDot(distanceToTarget, 2f);
////                }
////                else
////                {
////                    // Продолжаем движение, но с учетом торможения
////                    if (currentSpeed < maxSpeed)
////                    {
////                        currentSpeed += acceleration * Time.deltaTime;
////                    }
////                }
////                // Перемещаем автомобиль вперед с текущей скоростью
////                transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////            }
////            else
////            if (hit.collider.CompareTag("Car"))
////                {
////                    // Рассчитываем расстояние до ближайшей машины
////                    float distanceToNextCar = (float)(hit.distance - 4.3/* / 2*/);  // Без вычитания размера машины

////                    // Рассчитываем тормозной путь для текущей скорости
////                    float brakingDistance = CalculateBrakingDistance(currentSpeed);

////                    // Если расстояние до следующей машины меньше тормозного пути, начинаем тормозить
////                    if (distanceToNextCar < brakingDistance)
////                    {
////                        // Тормозим плавно
////                        ApplyBrakesDot(distanceToNextCar, 2f);
////                        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////                    }
////                    else
////                    {
////                        // Если дистанция достаточна, продолжаем движение
////                        if (currentSpeed < maxSpeed)
////                        {
////                            currentSpeed += acceleration * Time.deltaTime;
////                        }
////                        // Перемещаем автомобиль вперед с текущей скоростью
////                        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////                    }



////            } 


////        }else
////                {
////                    // Если дистанция достаточна, продолжаем движение
////                    if (currentSpeed < maxSpeed)
////                    {
////                        currentSpeed += acceleration * Time.deltaTime;
////                    }
////                    // Перемещаем автомобиль вперед с текущей скоростью
////                    transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////                }


////    }
////    else if (isGreen)
////    {
////        // Если мы не в зоне торможения, ускоряемся
////        if (currentSpeed < maxSpeed)
////        {
////            currentSpeed += acceleration * Time.deltaTime;
////        }
////        // Перемещаем автомобиль вперед с текущей скоростью
////        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////    }

////    //// Направление рейкаста — прямо перед машиной
////    //if (Physics.Raycast(raycastOrigin, transform.forward, out hit, raycastRange))
////    //{
////    //    // Если мы нашли машину
////    //    GameObject hitObject = hit.collider.gameObject;
////    //    // Получаем компонент CarMove для ближайшей машины
////    //    CarMove nearestCarScript = hitObject.GetComponent<CarMove>();
////    //    if (nearestCarScript != null)
////    //    {
////    //        // Рассчитываем расстояние до ближайшей машины
////    //        float distanceToNextCar = (float)(hit.distance - 4.3 / 2);  // Без вычитания размера машины

////    //        // Рассчитываем тормозной путь для текущей скорости
////    //        float brakingDistance = CalculateBrakingDistance(currentSpeed);

////    //        // Если расстояние до следующей машины меньше тормозного пути, начинаем тормозить
////    //        if (distanceToNextCar < brakingDistance)
////    //        {
////    //            // Тормозим плавно
////    //            ApplyBrakesDot(distanceToNextCar, 2f);
////    //            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////    //        }
////    //        else
////    //        {
////    //            // Если дистанция достаточна, продолжаем движение
////    //            if (currentSpeed < maxSpeed)
////    //            {
////    //                currentSpeed += acceleration * Time.deltaTime;
////    //            }
////    //            // Перемещаем автомобиль вперед с текущей скоростью
////    //            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////    //        }


////    //    }
////    //}
////    //else
////    //{
////    //    if (isRed || isYellow)
////    //    {
////    //        // Если стены или машины не найдено, считаем, что перед нами стена (целевая точка)
////    //        float distanceToTarget = (float)(Vector3.Distance(transform.position, targetPos) - 4.3 / 2);

////    //        // Рассчитываем тормозной путь для текущей скорости
////    //        float brakingDistance = CalculateBrakingDistance(currentSpeed);


////    //        // Если дистанция до цели меньше тормозного пути, начинаем тормозить
////    //        if (distanceToTarget < brakingDistance || distanceToTarget == 0)
////    //        {
////    //            // Начинаем тормозить плавно (уменьшаем скорость)
////    //            ApplyBrakesDot(distanceToTarget, 2f);
////    //        }
////    //        else
////    //        {
////    //            // Продолжаем движение, но с учетом торможения
////    //            if (currentSpeed < maxSpeed)
////    //            {
////    //                currentSpeed += acceleration * Time.deltaTime;
////    //            }
////    //        }
////    //        // Перемещаем автомобиль вперед с текущей скоростью
////    //        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

////    //    }
////    //    else if (isGreen)
////    //    {

////    //        // Если мы не в зоне торможения, ускоряемся
////    //        if (currentSpeed < maxSpeed)
////    //        {
////    //            currentSpeed += acceleration * Time.deltaTime;
////    //        }
////    //        // Перемещаем автомобиль вперед с текущей скоростью
////    //        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

////    //    }


////    //}

////}