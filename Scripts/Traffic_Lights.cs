using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Traffic_Lights : MonoBehaviour
{
    // ������ �� �������� ���������� ��� ������� ���������
    public GameObject TrafficLightRL;
    public GameObject TrafficLightLR;
    public GameObject TrafficLightRR;
    public GameObject TrafficLightLL;

    // ������ �� �������� ���������� (�������, ������, �������) ��� ������� ���������
    private GameObject redLightRL, yellowLightRL, greenLightRL;
    private GameObject redLightLR, yellowLightLR, greenLightLR;
    private GameObject redLightRR, yellowLightRR, greenLightRR;
    private GameObject redLightLL, yellowLightLL, greenLightLL;

    // ����� ��������� ������� �������
    private float redTime;
    private float yellowTime = 3f;
    private float greenTime;

    // ��������� ���������
    public bool isRedLRRL = false;
    public bool isYellowLRRL = false;
    public bool isGreenLRRL = false;

    public bool isRedLLRR = false;
    public bool isYellowLLRR = false;
    public bool isGreenLLRR = false;

    // ������� ��� ����������� ����� � ����� ���������
    public delegate void TrafficLightChanged(string side, bool isRed, bool isYellow, bool isGreen);
    public event TrafficLightChanged OnTrafficLightChanged;

    public bool isTrafficLightActive = false;  // ����, ������� ����� �������������� ������ ���������

    private UI_Control uiControl;



    private void Start()
    {
        // ������������� �����������
        InitializeTrafficLights();

        uiControl = FindObjectOfType<UI_Control>();

        // ������ ����� ������ ����������
        //StartCoroutine(TrafficLightCycle());
    }

    // ����� ��� ��������� ���������
    public void ActivateTrafficLight()//��������� ���������
    {
        if (!isTrafficLightActive)
        {
            if (uiControl != null)//����� ����� ������ ��������
            {
                float redInput = uiControl.GetInputValues("RedTime");
                float greenInput = uiControl.GetInputValues("GreenTime");

                if (redInput > 0) 
                    redTime = redInput;
                if (greenInput > 0) 
                    greenTime = greenInput;
            }
            else
            {
                Debug.LogWarning("UI_Control not found, using default timings for traffic lights.");
            }

            isTrafficLightActive = true;
            StartCoroutine(TrafficLightCycle());
        }
    }

    private void InitializeTrafficLights()//����� ������� ����� ������ �� ������� ���������(������)
    {
        redLightRL = TrafficLightRL.transform.Find("Light0/Lights/redlight").gameObject;
        yellowLightRL = TrafficLightRL.transform.Find("Light0/Lights/yellowlight").gameObject;
        greenLightRL = TrafficLightRL.transform.Find("Light0/Lights/greenlight").gameObject;

        redLightLR = TrafficLightLR.transform.Find("Light0/Lights/redlight").gameObject;
        yellowLightLR = TrafficLightLR.transform.Find("Light0/Lights/yellowlight").gameObject;
        greenLightLR = TrafficLightLR.transform.Find("Light0/Lights/greenlight").gameObject;

        redLightRR = TrafficLightRR.transform.Find("Light0/Lights/redlight").gameObject;
        yellowLightRR = TrafficLightRR.transform.Find("Light0/Lights/yellowlight").gameObject;
        greenLightRR = TrafficLightRR.transform.Find("Light0/Lights/greenlight").gameObject;

        redLightLL = TrafficLightLL.transform.Find("Light0/Lights/redlight").gameObject;
        yellowLightLL = TrafficLightLL.transform.Find("Light0/Lights/yellowlight").gameObject;
        greenLightLL = TrafficLightLL.transform.Find("Light0/Lights/greenlight").gameObject;
    }
        
    private IEnumerator TrafficLightCycle()//������������ ���������
    {
        while (isTrafficLightActive)
        {
            // �������� ������ ��� RL � LR, ������� ��� RR � LL
            SetTrafficLightState(true, false, false, false, false, true); // RL � LR ������, RR � LL �������
            yield return new WaitForSeconds(greenTime); // ������� ����� ��� �������

            // �������� ������ ��� RL � LR, ������� ��� RR � LL
            SetTrafficLightState(false, true, false, false, false, true); // RL � LR ������, RR � LL �������
            yield return new WaitForSeconds(yellowTime); // ������� ����� ��� �������

            // �������� ������� ��� RL � LR, ������ ��� RR � LL
            SetTrafficLightState(false, false, true, true, false, false); // RL � LR �������, RR � LL ������
            yield return new WaitForSeconds(redTime); // ������� ����� ��� ��������

            // �������� ������ ��� RR � LL, ������� ��� RL � LR
            SetTrafficLightState(false, false, true, false, true, false); // RL � LR �������, RR � LL ������
            yield return new WaitForSeconds(yellowTime); // ������� ����� ��� �������
        }
    }

    static public Dictionary<string, int> trafficRedCount = new Dictionary<string, int>();

    // ����� ��� ���������/���������� ����� �� ������ ���������
    private void SetTrafficLightState(bool greenRL_LR, bool yellowRL_LR, bool redRL_LR,
                                       bool greenRR_LL, bool yellowRR_LL, bool redRR_LL)
    {
        // ��������/��������� ��������������� ������� ��� ������� ���������

        // ��� RL � LR
        redLightRL.SetActive(redRL_LR);
        yellowLightRL.SetActive(yellowRL_LR);
        greenLightRL.SetActive(greenRL_LR);
               

        redLightLR.SetActive(redRL_LR);
        yellowLightLR.SetActive(yellowRL_LR);
        greenLightLR.SetActive(greenRL_LR);
        
        isRedLRRL = redRL_LR;
        isYellowLRRL = yellowRL_LR;
        isGreenLRRL = greenRL_LR;
        if (redRL_LR)
        {
            CountTraffic("RL");
            CountTraffic("LR");
        }
        
        OnTrafficLightChanged.Invoke("RL_LR", redRL_LR, yellowRL_LR, greenRL_LR); //������� ������� �������
        
        // ��� RR � LL
        redLightRR.SetActive(redRR_LL);
        yellowLightRR.SetActive(yellowRR_LL);
        greenLightRR.SetActive(greenRR_LL);
        
        redLightLL.SetActive(redRR_LL);
        yellowLightLL.SetActive(yellowRR_LL);
        greenLightLL.SetActive(greenRR_LL);
        if (redRR_LL)
        {
            CountTraffic("RR");
            CountTraffic("LL");
        }

        isRedLLRR = redRR_LL;
        isYellowLLRR = yellowRR_LL;
        isGreenLLRR = greenRR_LL;

        OnTrafficLightChanged.Invoke("RR_LL", redRR_LL, yellowRR_LL, greenRR_LL); //������� ������� �������
    }
    private void CountTraffic(string type)
    {
        if (!trafficRedCount.ContainsKey(type))
        {
            trafficRedCount[type] = 0;
        }

        // ����������� ������� ��� ������� ���� ������
        trafficRedCount[type]++;
    }
}
