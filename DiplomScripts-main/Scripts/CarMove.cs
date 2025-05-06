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

    private float frictionCoefficient = 0.8f; // ����������� ������ (��������, ��� ������ ��������)
    private float g = 9.81f;

    private Transform stopLine;

    private GameObject raycastPoint;

    private string carType;  // ��� ������: 


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

    private int turnPhase = 0; // 0 - ��� ��������, 1 - � turnTarget, 2 - � toward

    private float seeCarDist = 20f;

    private bool passTraffic = false; // �������� �� ������ ��������
    
    private Rigidbody rb;

    void Start()
    {
        

        rb = GetComponent<Rigidbody>();

        // ������ ������ ������ ��� ��������
        if (raycastPoint == null)
        {
            raycastPoint = new GameObject("RaycastPoint");
            raycastPoint.transform.SetParent(transform);  // ������ ��� �������� �������� ������
            raycastPoint.transform.localPosition = new Vector3(0f, 0.2f, 0.5f);  // ������������� �� �������� ��� ������
        }

        //if (turnProbability < 0f/* && isGreen*/)
        //{

        //���������� ���� ������ ����
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

        // ���������, ���� �� ������ ��������� � �����
        if (trafficLights != null)
        {
            // ������������� �� ������� ������ ���� ���
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

    // �����, ������� ���������� ��� ��������� ��������� ���������(�������)
    private void OnTrafficLightChanged(string side, bool red, bool yellow, bool green)
    {
        if (side == trafficLightSide)
        {
            isRed = red;
            isYellow = yellow;
            isGreen = green;
                       
        }
    }

    // ����� ��� ��������� ���������� ������(� ����� ��� ��������)
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
        TrafficStatistics.Instance.RegisterCar(this);//������������ ������
    }

    public string GetCarType()
    {
        return carType;
    }

    //�����
    private bool isTurning = false;
    private string turnDirection = ""; // "right", "left", "forward"
    private Transform turnTarget; // ������� ����� ��������
    private Transform centerTarget;
    private Transform towardTarget;
    private float turnProbability = -1f; // ����� ������� ����������� ������ ���� ���
    //�����

    private float halfCar = 4.3f / 2f;

    private bool hasStartTurn = false; // ����� �� ������� ��� ������
    private bool turningCar = false;//������ ���� ����� = false

    void Update()
    {
        // ������� ��� ����������� ����� ����� �����
        RaycastHit[] hits;
        float raycastRange = 60f; // ���������, �� ������� ��������� ������� �����

        //����� �������� �� ������� �������, ������� �������� � �������(������ ������ � ������ ������)
        Vector3 raycastOrigin = raycastPoint.transform.position;

        // ����������� �������� 
        Vector3 raycastDirection = transform.forward;

        // ������������ ��������
        //Debug.DrawRay(raycastOrigin, raycastDirection * raycastRange, Color.red, 0.1f);

        // ����� ���  CarLayer (���� �����)
        int layerMask = LayerMask.GetMask(/*"WallLayer",*/ "CarLayer");

        // ���������� �������� ��� ��������� ���� �������� �� ����
        hits = Physics.RaycastAll(raycastOrigin, raycastDirection, raycastRange, layerMask);

        RaycastHit nearestHit;

        //����������� ����� ������ ������ � ����������� �� ���� ���� ����
        //��� ����������� ������ ������ ��� ���
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

        //���� ������������(� ��������� ����� ��������), �� ������ "��������������"
        if (!isTurning && turnDirection != "forward")
        {            
                if (turnDirection == "right")
                {
                    // ������������ ������� ��� �������
                    StartTurn(rightTurnTarget, /*centerRight,*/ towardRight);
                }
                else if (turnDirection == "left" && CanTurnLeftSafely())
                {
                    // ������������ ������, ���� �����
                    StartTurn(leftTurnTarget, /*centerLeft,*/ towardLeft);
                }
        }

        //���������� �� ������ ������ �� ����-�����
        float distToStop = Mathf.Abs(Vector3.Dot(stopLine.position - transform.position, transform.forward) - halfCar);
        float distToCar = 100000f;

        //���� ������� ������, �� ���������� ��������� ��� ��(�� ���������
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            nearestHit = hits[0];
            distToCar = nearestHit.distance;
        }

        // ����  ������ ����� ��� ����-����� �� true 
        bool carIsNearest = distToCar <= distToStop;

        //��������� �������� ������ �������� ��� ���
        CheckPassTraffic();
        
        
        //������ ������ ������ ��� �������� ������
        if ((isRed || isYellow) && !passTraffic)  // ���� ������ ������� ��� ������ � ������ �� �������� �������� �� ����� � ����������� �� ��������
        {
            if (!hasStartTurn)  // ���� ������� ��� �� �����(������ �� ������������ (�.�. ���� �� ����� ������ �� ������ �������)
            {
                HandleCollision(carIsNearest, "red", distToCar, distToStop);
            }
        }
        else if (isGreen || hasStartTurn || passTraffic) //���� ���������, ������ ��� ������������, �� ������ �������� �� ��������
        {
            //Debug.Log($"  distToCar={distToCar}, distToStop={distToStop}");
            HandleCollision(carIsNearest, "green", distToCar, distToStop);
        }
        else
        {
            // ���� ��� ������������, ���������� ��������
            ContinueMovement();
        }
        
        //������� ��������� ����(������� + ���� ����������)
        float brakingDistance = CalculateBrakingDistance(currentSpeed);

        //���� �� ������ ����� ���������� � ������������, � �� ����� �������, �� ��������
        if (!carIsNearest && distToStop <= brakingDistance || distToStop < 1.55)
        {
            if (turnDirection == "left" && !CanTurnLeftSafely() && !passTraffic)
            {
                ApplyBrakesDot(distToStop, 1.5f);
            }
        }

        //���������� �� ����� ������ ��������
        float distToStart = Vector3.Distance(transform.position, startTarget.position)/* / 2*/ /* - halfCar*/;

        //���� ������(1 �) �� ������ ������������
        if (distToStart <= 1f && turningCar)
        {
            hasStartTurn = true;
            isTurning = true;
        }

        //���� �� ��, �� �������� �������)
        if (isTurning && turnTarget != null/* && distToStart <= 2f*/)
        {
            //hasStartTurn = true;
            TurnCar();
            return; // �� ���������� ������� ��������
        }

    }
    private Vector3 turnStartPos;
    void CheckPassTraffic()
    {
        float threshold = 0.5f;

        if (!passTraffic)//���� ������ ��� �� �������� ��������
        {
            //���������� �� ����-�����
            float dist = Vector3.Distance(transform.position, stopLine.position) - halfCar;
            if ((turnDirection == "forward" || turnDirection == "right") && centerTarget != null)//���� ����� ��� �������, �� � ��� ���� ����� �� ������� ��������� ������� ��� ���)
            {

                if (dist < threshold)
                {
                    passTraffic = true;//�������� ��������
                    TrafficStatistics.Instance.UnregisterCar(this);
                    isCountedInQueue = true;
                }
            }
            else if (turnDirection == "left" && turnTarget != null)//���� ������ �� � �� ������ ����� ��������
            {
                //float dist = Vector3.Distance(transform.position, stopLine.position) - halfCar;
                if (dist < threshold)
                {
                    passTraffic = true;//�������� ��������
                    TrafficStatistics.Instance.UnregisterCar(this);
                    isCountedInQueue = true;
                }
            }
            
        }
    }

    void StartTurn(Transform target, /*Transform center,*/ Transform toward)//����� ��� ������ ������������ � ��������� �������� �� �����-�����
    {
        turningCar = true;
        if (centerTarget == null)
        {
            //Debug.LogError("centerTarget == null � StartTurnPhase!");
            isTurning = false;
            return;
        }
        turnStartPos = startTarget.position;

        turnTarget = target;
        towardTarget = toward;

        turnPhase = 1;//���� ��������
        StartTurnPhase();
        //Debug.Log($" StartTurn: target={target?.name}, center={centerTarget?.name}, toward={towardTarget?.name}");
    }

    void AddCenter(Transform center)
    {
        centerTarget = center;

        //Debug.Log($"����� ����� ��������: {center?.name}");
    }

    float t = 0f; // �������� �� ����
    private Vector3 bezierP0, bezierP1, bezierP2;
    //private float turnSpeed = 1.3f;
    private Quaternion smoothedRotation;

    void TurnCar()
    {
        if (turnPhase == 1)//�� ��������� ������ �� �����
        {
            Vector3 flatCurrent = transform.position;
            flatCurrent.y = 0f;

            Vector3 flatTarget = turnStartPos;
            flatTarget.y = 0f;

            Vector3 direction = (flatTarget - flatCurrent).normalized;

            currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

            // ������� ������ � ������� ��������
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            // ������ � ������ ��������, �� ������� �� ���� 2
            if (Vector3.Distance(flatCurrent, flatTarget) < 0.1f)
            {
                turnPhase = 2;
                t = 0f; 
            }

            return;
        }
        else if (turnPhase == 2)//������ �����
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
            //�������� ����������� ������
            if (bezierTangent.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(bezierTangent.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }

            transform.position = bezierPos;

            if (t >= 1f || Vector3.Distance(transform.position, bezierP2) < 0.05f)//������ � ����� ���������� �����, �� ��������� �� 3 ����
            {
                t = 0f;
                turnPhase = 3;
            }

            return;
        }
        else if (turnPhase == 3)
        {
            // ����/������������� �� ��������� ������ �� ����� ������������

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

    void StartTurnPhase()// ����� ��������� ��� �������� �����
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

            //��������� ��������
            currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
        }
    }

    bool CanTurnLeftSafely()//�������� ����� ������ ������������ ������ ��� ���
    {
        GameObject[] allCars = GameObject.FindGameObjectsWithTag("Car");//��� ������� ������

        List<(GameObject car, float distance)> oppositeCars = new List<(GameObject, float)>();//������ ��������� �����
        Transform center = centerTarget;//����� �� ������� ������������� ���������� �� ������

        foreach (GameObject car in allCars)//���� ���������� ������ ��������� �����
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

        if (oppositeCars.Count == 0) //���� ��� ���������, ������ ����� ������������
            return true;

        // ��������� �� ����������
        oppositeCars.Sort((a, b) => a.distance.CompareTo(b.distance));

        // ��������� ����� ��������� ��������� ������
        var (nearestCar, nearestDistance) = oppositeCars[0];
        CarMove nearestMove = nearestCar.GetComponent<CarMove>();

        // ���� ��� ������ ������������ ������(���� � ���������), ������ ����� ������������
        if (turnDirection == "left" && nearestMove.turnDirection == "left")
        {
            return true;
        }

        // ��������� ������ 3 ������(����������)
        int checkCount = Mathf.Min(oppositeCars.Count, 3);
        for (int i = 0; i < checkCount; i++)
        {
            GameObject car = oppositeCars[i].car;
            float distance = oppositeCars[i].distance;

            CarMove otherCar = car.GetComponent<CarMove>();
            string direction = otherCar.turnDirection;

            if (direction == "left")
            {
                continue; // ������������ ������, �� ������
            }

            if ((direction == "forward" || direction == "right") && distance < seeCarDist)
            {
                return false; // ������
            }
        }

        return true;
    }

    public bool isCountedInQueue = false; // ���� �� ��� ������ � �������
    private bool isWaiting = false;
    private float waitStartTime = 0f;
    void HandleCollision(bool carIsNearest, string color, float distToCar, float distToStop)//����� ������ ���� ������
    {
        float brakingDistance = CalculateBrakingDistance(currentSpeed);
        float distanceToTarget = carIsNearest ? distToCar : distToStop;
        //float waitingTime = Time.deltaTime;

        if (color == "red")
        {
            //Debug.Log($"[HANDLE] red: carIsNearest={carIsNearest}, distToCar={distToCar}, isTurning={isTurning}, currentSpeed={currentSpeed}");
            if (carIsNearest && (distanceToTarget < brakingDistance /*&& brakingDistance > 4*/ || distanceToTarget <= 3.55f))
            {
                // ���� ������ ������� ������, �������� ���������
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
            // ���� ��� ������


            if (carIsNearest)
            {
                
                //����� ���
                if (distanceToTarget < 0.5f && isTurning)//���� ������������, ����� �� ���� ��������� ��� ��������, ������� 1
                {
                    // ���� ������ ������� ������, �������� ���������
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
                if (distanceToTarget <= 2.55 && !isTurning)//���� �� ������������, ������ ����
                {
                    // ���� ������ ������� ������, �������� 
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
                if ((distanceToTarget < brakingDistance || distanceToTarget <= 2.55f/* + 1.5f*/) && !isTurning) //�������� 1.5 ��� ������
                {
                    // ���� ������ ������� ������, �������� 
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
                    // ���� ���������� �����������, ���������� ��������
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
                if (distanceToTarget < brakingDistance || distanceToTarget <= 2.55f/* + 1.5f*/)//�������� 1.5 ��� ������
                {
                    // ���� ������ ������� ������, ��������
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
                    // ���� ���������� �����������, ���������� ��������
                    ContinueMovement();
                }
            }
            else
            {
                // ���� ���������� �����������, ���������� ��������
                ContinueMovement();
            }

        }
    }

    void ContinueMovement()//������ ���� �����/����������
    {
        if (currentSpeed < maxSpeed)
        {
            currentSpeed += acceleration * Time.deltaTime;
        }
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    void ApplyBrakesDot(float distanceToTarget, float distance)
    {
        // ������������ ��������� ������ � ����������� �� ����������� ����������
        float brakeFactor = Mathf.Clamp01(distanceToTarget / CalculateBrakingDistance(currentSpeed));

        // ���������� ���������� �� ��������� ������
        if (distanceToTarget < 7f) 
        {
            // ������ ����������� ��������� ������, ����� ��� ����� ����� ������� ��� ���������� ����������
            float emergencyBrakeFactor = Mathf.Lerp(50f, 200f, Mathf.Clamp01((5f - distanceToTarget) / 5f));  // ����������� ���������� �� ���� ���������� ���������
            currentSpeed -= (currentSpeed * emergencyBrakeFactor) * Time.deltaTime;
        }
        else
        {            
            currentSpeed -= (currentSpeed * brakeFactor * 0.8f) * Time.deltaTime;// ������� ���������� �� ������� ����������
        }
                
        currentSpeed = Mathf.Max(currentSpeed, 0f);// ����� �������� �� ����������� �������������

        // ���� �������� ����� ���� � �� ���� �������� ���� �����, ������������� ������������� ������
        if (currentSpeed < 1f && distanceToTarget < distance)
        {
            currentSpeed = 0f;
            distanceToTarget = 0f;
        }
    }
 
    float CalculateBrakingDistance(float speed)//������������ ����
    {
        return speed*2 + Mathf.Pow(speed, 2) / (2 * frictionCoefficient * g);
    }    
}      
//void TurnCar()
    //{
    //    if (turnPhase == 1)
    //    {
    //        // ������ ���� � ������ �������� � ����� ������ �������� (turnStartPos)
    //        Vector3 flatCurrent = transform.position;
    //        flatCurrent.y = 0f;

    //        Vector3 flatTarget = turnStartPos;
    //        flatTarget.y = 0f;

    //        Vector3 direction = (flatTarget - flatCurrent).normalized;

    //        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

    //        //transform.position += direction * Time.deltaTime * currentSpeed;

    //        // ������� ������ � ������� ��������
    //        if (direction.sqrMagnitude > 0.001f)
    //        {
    //            Quaternion targetRotation = Quaternion.LookRotation(direction);
    //            targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
    //            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    //        }

    //        // ���� ������ � turnStartPos � ������� �� ���� 2
    //        if (Vector3.Distance(flatCurrent, flatTarget) < 0.1f)
    //        {
    //            turnPhase = 2;
    //            t = 0f; // �������� �������� ������
    //        }

    //        return;
    //    }
    //    else if (turnPhase == 2)
    //    {
    //        // ������ ���� � ��� ���� ������ �����
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

    //            //// ��� ����� �� ���� ��� ���������� ���������� �����������!
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
    //        // ���� ������ ���� 3
    //        Vector3 flatCurrent = transform.position;
    //        flatCurrent.y = 0f;

    //        //currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

    //        //if (fixedDirection.sqrMagnitude > 0.001f)
    //        //{
    //        //    Quaternion targetRotation = Quaternion.LookRotation(fixedDirection);
    //        //    targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
    //        //    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
    //        //}

    //        //// �������� ������ ��� �� ������ �������� ���������, ���� ����
    //        //// ��������, �������� �����
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

    //        //    ������ ������� � ����
    //        //    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
    //        //}

    //        //���������, ��� �� ����� �������� ����(���� ����� ��������� �������)
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
    //        // ������ ��������� ������
    //        float estimatedLength = (bezierP0 - bezierP1).magnitude + (bezierP1 - bezierP2).magnitude;
    //        float deltaT = (currentSpeed / estimatedLength) * Time.deltaTime;

    //        t += deltaT; // t ������� �� ��������
    //        t = Mathf.Clamp01(t);

    //        // ������� �� ������
    //        bezierPos = Mathf.Pow(1 - t, 2) * bezierP0 +
    //                    2 * (1 - t) * t * bezierP1 +
    //                    Mathf.Pow(t, 2) * bezierP2;

    //        // �������� (����������� ������ �����)
    //        bezierTangent = 2 * (1 - t) * (bezierP1 - bezierP0) +
    //                        2 * t * (bezierP2 - bezierP1);

    //        bezierTangent.y = 0f;

    //        if (bezierTangent.sqrMagnitude > 0.001f)
    //        {
    //            Quaternion targetRot = Quaternion.LookRotation(bezierTangent.normalized);
    //            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
    //        }

    //        transform.position = bezierPos;

    //        // ������� ����� ������
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
    //    // ������� ������� ������ ������
    //    Vector3 frontPosition = transform.position + transform.forward * halfCar;

    //    if (turnPhase == 1 || turnPhase == 2)
    //    {
    //        // �������� �����������!
    //        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

    //        Vector3 bezierPos, bezierTangent;

    //        //t += Time.deltaTime * turnSpeed;
    //        // ������ ��������� ������
    //        float estimatedLength = (bezierP0 - bezierP1).magnitude + (bezierP1 - bezierP2).magnitude;
    //        float deltaT = (currentSpeed / estimatedLength) * Time.deltaTime;

    //        t += deltaT; // t ������� �� ��������
    //        t = Mathf.Clamp01(t);

    //        // ������� �� ������ �����
    //        bezierPos = Mathf.Pow(1 - t, 2) * bezierP0 +
    //                    2 * (1 - t) * t * bezierP1 +
    //                    Mathf.Pow(t, 2) * bezierP2;

    //        // �������� (����������� ������ �����)
    //        bezierTangent = 2 * (1 - t) * (bezierP1 - bezierP0) +
    //                        2 * t * (bezierP2 - bezierP1);

    //        bezierTangent.y = 0f;

    //        if (bezierTangent.sqrMagnitude > 0.001f)
    //        {
    //            Quaternion targetRot = Quaternion.LookRotation(bezierTangent.normalized);
    //            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
    //        }

    //        transform.position = bezierPos;

    //        // ������� ����� ������
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

    //        // ��������� frontPosition ����� ��������
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
//2. �����
        //if (!isTurning && isGreen && turnDirection != "forward")
        //{
        //    foreach (RaycastHit hit in hits)
        //    {
        //        if (hit.collider.CompareTag("wall") && hit.distance <= turnStartDistance)
        //        {
        //            if (turnDirection == "right")
        //            {
        //                // ������������ ������� ��� �������
        //                StartTurn(rightTurnTarget, /*centerRight,*/ towardRight);
        //            }
        //            else if (turnDirection == "left" && CanTurnLeftSafely())
        //            {
        //                // ������������ ������, ���� �����
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
        //    // ��������� ������� �� ���������� (��������� ������ ����� ������ � ������)
        //    System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        //    // �������� ��������� ������
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
        //    // ���� ��� ������������, ���������� ��������
        //    ContinueMovement();
        //}
//// ���� ��� ������
            //if (hit.collider.CompareTag("Car"))
            //{                
            //    if (distanceToTarget < brakingDistance/* + 1.5f*/)//�������� 1.5 ��� ������
            //    {
            //        // ���� ������ ������� ������, �������� ���������
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
            //        {// ���� ���������� �����������, ���������� ��������
            //            ContinueMovement();
            //        }
            //    }
            //}
            //else if (hit.collider.CompareTag("wall"))
            //{
            //    if (distanceToTarget < brakingDistance/* + 1.5f*/)//�������� 1.5 ��� ������
            //    {
            //        // ���� ������ ������� ������, �������� ���������
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
            //        {// ���� ���������� �����������, ���������� ��������
            //            ContinueMovement();
            //        }
            //    }
            //}
            // ���� ��� ������

            //if (distanceToTarget < brakingDistance/* + 1.5f*/)//�������� 1.5 ��� ������
            //{
            //    // ���� ������ ������� ������, �������� ���������
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
            //    {// ���� ���������� �����������, ���������� ��������
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


//// ���� ������ ������� ������, �������� ���������
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
    //            // ���� ��������� ������ ���� ������������ ������ � �� ������ ���, ����������
    //            if (otherCar.turnDirection == "left")
    //                continue;

    //            // ���� ��������� ������ ���� ����� ��� ������� � ��������� ����������
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
    //                Vector3 intersectionPoint = transform.position + transform.forward * turnStartDistance; // �������������� ����� ��������
    //                float distanceToIntersection = Vector3.Distance(otherCar.transform.position, intersectionPoint);
    //                float timeToReach = distanceToIntersection / otherSpeed;

    //                float timeToTurn = 1.5f; // ������� ������� �������� �������

    //                if (timeToReach < timeToTurn + 0.5f) // ���� ��� ����� ������������ ����� �� ����������
    //                {
    //                    return false;
    //                }
    //            }
    //        }
    //    }

    //    return true;
    //}

    //7. �����

    //void ApplyBrakesDot(float distanceToTarget, float distance)
    //{
    //    float brakingDistance = CalculateBrakingDistance(currentSpeed);

    //    // ���� �� ������� �����, ��� ��������� ���� � �������� ����������
    //    if (distanceToTarget < brakingDistance)
    //    {
    //        // ��������� ������ � ��� �� ���������, ��� � ������, �� � �������� �������
    //        currentSpeed -= acceleration * Time.deltaTime;
    //    }

    //    // �������� �� ���������� �������������
    //    currentSpeed = Mathf.Max(currentSpeed, 0f);

    //    // ���� �������� ����� ���� � �� ���� �������� ���� �����, ������������� ������������� ������
    //    if (currentSpeed < 0.5f && distanceToTarget < distance)
    //    {
    //        currentSpeed = 0f;
    //        distanceToTarget = 0f;
    //    }
    //}
//void TurnTowardsTarget()
    //{
    //    // �������� ������� ������� ����� (������������� ��� �������, ����� � ����� "toward")
    //    Transform currentTarget = (turnPhase == 1) ? turnTarget : toward;
    //    if (currentTarget == null) return;

    //    // ����������� � ������� ������� �����
    //    Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;

    //    // ������������ ���� ����� ������� ������������ ������ � ������������ � ����
    //    float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

    //    // ������� ������� � ������������ ���� ��������, ����� �� �� ��� ������� ������
    //    float rotationStep = Mathf.Sign(angleToTarget) * Mathf.Min(Mathf.Abs(angleToTarget), turnAnglePerSecond * Time.deltaTime);
    //    transform.Rotate(0, rotationStep, 0);

    //    //// ��������� ����� (� ������ ������� ��������)
    //    //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

    //    // ���������, �������� �� �� ������� �����
    //    if (Vector3.Distance(transform.position, currentTarget.position) < 0.5f)
    //    {
    //        // ���� ���� � ����� ��������, ��������� � ����� "toward"
    //        if (turnPhase == 1)
    //        {
    //            turnPhase = 2; // ������ ���� � ����� "toward"
    //        }
    //        // ���� ���� � ����� "toward", ������������ � ������� ��������
    //        else if (turnPhase == 2)
    //        {
    //            isTurning = false;
    //            turnTarget = null;
    //            turnDirection = "";
    //            turnPhase = 0; // ��������� �������
    //            turnProbability = -1f;
    //        }
    //    }
    //}
    //void TurnTowardsTarget()
    //{
    //    // �������� ������� ������� ����� (������� ����� ��������, ����� ����� "toward")
    //    Transform currentTarget = (turnPhase == 1) ? turnTarget : toward;
    //    if (currentTarget == null) return;

    //    // ����������� � ������� ����� (���������� ��� Y, ����� �������� ���� �� �����������)
    //    Vector3 directionToTarget = currentTarget.position - transform.position;
    //    directionToTarget.y = 0;  // ���������� ��� Y, ����� �������� ���� � �������������� ���������
    //    float distanceToTarget = directionToTarget.magnitude;

    //    // ����������� ������, ����� ��� ��� ��������� ������
    //    directionToTarget.Normalize();

    //    // ������� ������������ ������ �� ����������� � ������� �����
    //    // �� ������������ ������ ������ �� ����������� � ������� �����
    //    if (distanceToTarget > 0.1f)  // ���� �� �� �������� ����
    //    {
    //        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
    //        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnAnglePerSecond * Time.deltaTime);
    //    }

    //    // ��������� ����� � ����������� ������� ����� � ������� ���������
    //    //transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

    //    // �������� �� ���������� ����
    //    if (distanceToTarget < 0.1f)
    //    {
    //        // ���� ��� ����� ��������, ��������� � ����� "toward"
    //        if (turnPhase == 1)
    //        {
    //            turnPhase = 2;  // ������ ���� � ����� "toward"
    //        }
    //        // ���� ��� ����� "toward", ��������� ��������
    //        else if (turnPhase == 2)
    //        {
    //            isTurning = false;
    //            turnTarget = null;
    //            turnDirection = "";
    //            turnPhase = 0;  // ��������� �������
    //            turnProbability = -1f;
    //        }
    //    }
    //}







    //5. �����


    //6. �����
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

//    private float frictionCoefficient = 0.8f; // ����������� ������ (��������, ��� ������ ��������)
//    private float g = 9.81f;

//    private Transform targetPosition;

//    private GameObject raycastPoint;

//    private static string carType;  // ��� ������: 


//    private bool isRed = false;
//    private bool isYellow = false;
//    private bool isGreen = false;
//    private string trafficLightSide = "";



//    private Transform rightTurnTarget;
//    private Transform leftTurnTarget;

//    void Start()
//    {
//        // ������ ������ ������ ��� ��������, ���� ��� ���
//        if (raycastPoint == null)
//        {
//            raycastPoint = new GameObject("RaycastPoint");
//            raycastPoint.transform.SetParent(transform);  // ������ ��� �������� �������� ������
//            raycastPoint.transform.localPosition = new Vector3(0f, 0.5f, 0.5f);  // ������������� �� �������� ��� ������
//        }        
//    }

//    public void Change()
//    {
//        Traffic_Lights trafficLights = FindObjectOfType<Traffic_Lights>();

//        // ���������, ���� �� ������ ��������� � �����
//        if (trafficLights != null)
//        {
//            // ������������� �� ������� ������ ���� ���
//            if (carType == "LR" || carType == "RL")
//                trafficLightSide = "RL_LR";
//            else if (carType == "RR" || carType == "LL")
//                trafficLightSide = "RR_LL";


//            trafficLights.OnTrafficLightChanged += OnTrafficLightChanged;

//            OnTrafficLightChanged(trafficLightSide, trafficLights.isRed, trafficLights.isYellow, trafficLights.isGreen);

//        }
//    }


//    // �����, ������� ����� ������ ��� ��������� ��������� ���������
//    private void OnTrafficLightChanged(string side, bool red, bool yellow, bool green)
//    {
//        // ������, ������� ����������� ��� ��������� ��������� ���������
//        Debug.Log($"Traffic light changed for {side}: Red={isRed}, Yellow={isYellow}, Green={isGreen}");

//        if (side == trafficLightSide)
//        {
//            isRed = red;
//            isYellow = yellow;
//            isGreen = green;
//        }
//    }

//    // ����� ��� ��������� ����������
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

//        // ����� ���������� ������� (������ ��� �����)
//        RaycastHit[] hits;
//        float raycastRange = 30f; // ���������, �� ������� ��������� ������� ��������

//        // �������� ����� �������� �� ������� �������, ������� �������� � �������
//        Vector3 raycastOrigin = raycastPoint.transform.position;

//        // ����������� �������� � �������� ��� ������
//        Vector3 raycastDirection = transform.forward * raycastRange;

//        // ������������ �������� � ���������
//        Debug.DrawRay(raycastOrigin, raycastDirection, Color.red, 0.1f);

//        // ����� ��� ����� WallLayer � CarLayer (��� ����� ������ ���� ��� �����)
//        int layerMask = LayerMask.GetMask("WallLayer", "CarLayer");

//        // ���������� �������� ��� ��������� ���� �������� �� ����
//        hits = Physics.RaycastAll(raycastOrigin, transform.forward, raycastRange, layerMask);

//        if (hits.Length > 0)
//        {
//            // ��������� ������� �� ���������� (��������� ������ ����� ������ � ������)
//            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

//            // �������� ��������� ������
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
//            // ���� ��� ������������, ���������� ��������
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
//            // ���� ��� ������
//            else if (hit.collider.CompareTag("Car"))
//            {
//                //����� ���
//                if (distanceToTarget < brakingDistance)
//                {
//                    // ���� ������ ������� ������, �������� ���������
//                    ApplyBrakesDot(distanceToTarget, 2f);
//                }
//                else
//                {
//                    // ���� ���������� �����������, ���������� ��������
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
//        // ������������ ��������� ������ � ����������� �� ����������� ����������
//        float brakeFactor = Mathf.Clamp01(distanceToTarget / CalculateBrakingDistance(currentSpeed));

//        // ���������� ���������� �� ��������� ������
//        if (distanceToTarget < 10f)  // ������ 5 ������ - ���������� ����������
//        {
//            // ������ ����������� ��������� ������, ����� ��� ����� ����� ������� ��� ���������� ����������
//            float emergencyBrakeFactor = Mathf.Lerp(50f, 200f, Mathf.Clamp01((5f - distanceToTarget) / 5f));  // ����������� ���������� �� ���� ���������� ���������
//            currentSpeed -= (currentSpeed * emergencyBrakeFactor) * Time.deltaTime;
//        }
//        else
//        {
//            // ������� ���������� �� ������� ����������
//            currentSpeed -= (currentSpeed * brakeFactor * 0.8f) * Time.deltaTime;
//        }

//        // �������� �� ���������� �������������
//        currentSpeed = Mathf.Max(currentSpeed, 0f);

//        // ���� �������� ����� ���� � �� ���� �������� ���� �����, ������������� ������������� ������
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

////    // ����� ��������� ������ ����� �������
////    RaycastHit hit;
////    float raycastRange = 30f; // ���������, �� ������� �� ��������� ������� ������

////    // �������� ����� �������� �� ������� �������, ������� �������� � �������
////    Vector3 raycastOrigin = raycastPoint.transform.position;

////    // ����������� �������� � �������� ��� ������
////    Vector3 raycastDirection = transform.forward * raycastRange;

////    // ������������ �������� � ���������
////    Debug.DrawRay(raycastOrigin, raycastDirection, Color.red, 0.1f);

////    // ����� ��� ����� WallLayer � CarLayer (��� ����� ������ ���� ��� �����)
////    int layerMask = LayerMask.GetMask("WallLayer", "CarLayer");


////    if (isRed || isYellow)
////    {
////        if (Physics.Raycast(raycastOrigin, transform.forward, out hit, raycastRange, layerMask))
////        {
////            // ���� �� ����� ������
////            GameObject hitObject = hit.collider.gameObject;

////            // ���� ����� ���� ����� (�������� �� ��� �������)
////            if (hit.collider.CompareTag("wall"))  // ����� ������ ���� �������� ����� "Wall"
////            {
////                //������� �����)
////                float distanceToTarget = (float)(Vector3.Distance(transform.position, targetPos) - 4.3/* / 2*/);

////                // ������������ ��������� ���� ��� ������� ��������
////                float brakingDistance = CalculateBrakingDistance(currentSpeed);


////                // ���� ��������� �� ���� ������ ���������� ����, �������� ���������
////                if (distanceToTarget < brakingDistance || distanceToTarget == 0)
////                {
////                    // �������� ��������� ������ (��������� ��������)
////                    ApplyBrakesDot(distanceToTarget, 2f);
////                }
////                else
////                {
////                    // ���������� ��������, �� � ������ ����������
////                    if (currentSpeed < maxSpeed)
////                    {
////                        currentSpeed += acceleration * Time.deltaTime;
////                    }
////                }
////                // ���������� ���������� ������ � ������� ���������
////                transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////            }
////            else
////            if (hit.collider.CompareTag("Car"))
////                {
////                    // ������������ ���������� �� ��������� ������
////                    float distanceToNextCar = (float)(hit.distance - 4.3/* / 2*/);  // ��� ��������� ������� ������

////                    // ������������ ��������� ���� ��� ������� ��������
////                    float brakingDistance = CalculateBrakingDistance(currentSpeed);

////                    // ���� ���������� �� ��������� ������ ������ ���������� ����, �������� ���������
////                    if (distanceToNextCar < brakingDistance)
////                    {
////                        // �������� ������
////                        ApplyBrakesDot(distanceToNextCar, 2f);
////                        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////                    }
////                    else
////                    {
////                        // ���� ��������� ����������, ���������� ��������
////                        if (currentSpeed < maxSpeed)
////                        {
////                            currentSpeed += acceleration * Time.deltaTime;
////                        }
////                        // ���������� ���������� ������ � ������� ���������
////                        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////                    }



////            } 


////        }else
////                {
////                    // ���� ��������� ����������, ���������� ��������
////                    if (currentSpeed < maxSpeed)
////                    {
////                        currentSpeed += acceleration * Time.deltaTime;
////                    }
////                    // ���������� ���������� ������ � ������� ���������
////                    transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////                }


////    }
////    else if (isGreen)
////    {
////        // ���� �� �� � ���� ����������, ����������
////        if (currentSpeed < maxSpeed)
////        {
////            currentSpeed += acceleration * Time.deltaTime;
////        }
////        // ���������� ���������� ������ � ������� ���������
////        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////    }

////    //// ����������� �������� � ����� ����� �������
////    //if (Physics.Raycast(raycastOrigin, transform.forward, out hit, raycastRange))
////    //{
////    //    // ���� �� ����� ������
////    //    GameObject hitObject = hit.collider.gameObject;
////    //    // �������� ��������� CarMove ��� ��������� ������
////    //    CarMove nearestCarScript = hitObject.GetComponent<CarMove>();
////    //    if (nearestCarScript != null)
////    //    {
////    //        // ������������ ���������� �� ��������� ������
////    //        float distanceToNextCar = (float)(hit.distance - 4.3 / 2);  // ��� ��������� ������� ������

////    //        // ������������ ��������� ���� ��� ������� ��������
////    //        float brakingDistance = CalculateBrakingDistance(currentSpeed);

////    //        // ���� ���������� �� ��������� ������ ������ ���������� ����, �������� ���������
////    //        if (distanceToNextCar < brakingDistance)
////    //        {
////    //            // �������� ������
////    //            ApplyBrakesDot(distanceToNextCar, 2f);
////    //            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////    //        }
////    //        else
////    //        {
////    //            // ���� ��������� ����������, ���������� ��������
////    //            if (currentSpeed < maxSpeed)
////    //            {
////    //                currentSpeed += acceleration * Time.deltaTime;
////    //            }
////    //            // ���������� ���������� ������ � ������� ���������
////    //            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
////    //        }


////    //    }
////    //}
////    //else
////    //{
////    //    if (isRed || isYellow)
////    //    {
////    //        // ���� ����� ��� ������ �� �������, �������, ��� ����� ���� ����� (������� �����)
////    //        float distanceToTarget = (float)(Vector3.Distance(transform.position, targetPos) - 4.3 / 2);

////    //        // ������������ ��������� ���� ��� ������� ��������
////    //        float brakingDistance = CalculateBrakingDistance(currentSpeed);


////    //        // ���� ��������� �� ���� ������ ���������� ����, �������� ���������
////    //        if (distanceToTarget < brakingDistance || distanceToTarget == 0)
////    //        {
////    //            // �������� ��������� ������ (��������� ��������)
////    //            ApplyBrakesDot(distanceToTarget, 2f);
////    //        }
////    //        else
////    //        {
////    //            // ���������� ��������, �� � ������ ����������
////    //            if (currentSpeed < maxSpeed)
////    //            {
////    //                currentSpeed += acceleration * Time.deltaTime;
////    //            }
////    //        }
////    //        // ���������� ���������� ������ � ������� ���������
////    //        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

////    //    }
////    //    else if (isGreen)
////    //    {

////    //        // ���� �� �� � ���� ����������, ����������
////    //        if (currentSpeed < maxSpeed)
////    //        {
////    //            currentSpeed += acceleration * Time.deltaTime;
////    //        }
////    //        // ���������� ���������� ������ � ������� ���������
////    //        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

////    //    }


////    //}

////}