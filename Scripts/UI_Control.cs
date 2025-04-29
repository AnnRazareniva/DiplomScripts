using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

public class UI_Control : MonoBehaviour
{
    //public Button Button_Uniform;
    public Button Button_Poisson;
    public Button Button_Start;
    public Button Button_SpeedUp;

    public TMP_InputField inputFieldSpeed;


    public GameObject textPrefab; 
    public GameObject inputFieldPrefab; 

    public GameObject textPrefabOutput; 
    public GameObject inputFieldPrefabOutput; 

    public Transform container_TextInput; // Контейнер для текстов ВВОД
    public Transform container_FieldInput; // Контейнер для текстов и инпутов ВВОД
    
    public Transform container_TextOutput; // Контейнер для текстов ВЫВОД
    public Transform container_FieldOutput; // Контейнер для текстов и инпутов ВЫВОД

    public int numberGenerator;

    private Generator poissonGenerator;
    //private Generator uniformGenerator;

    public Car_Spawn carSpawner;

    private List<TMP_InputField> inputFieldsOutput = new List<TMP_InputField>();
    private List<TMP_InputField> inputFieldsInput = new List<TMP_InputField>();

    //public CarMove carMoveScript;
    private void Start()
    {
        poissonGenerator = new Generator(this);
        //uniformGenerator = new Generator(this);
        
        //Button_Uniform.onClick.AddListener(() => ShowFields(1));

        Button_Poisson.onClick.AddListener(() => ShowFields(2));
        Button_Start.gameObject.SetActive(false);

        Button_Start.onClick.AddListener(ShowStart);

        Button_SpeedUp.onClick.AddListener(SpeedUpGame);
    }

    private void Update()
    {
        if (simulationRunning)
        {
            elapsedTime += Time.deltaTime; // Считаем сколько прошло

            if (elapsedTime >= simulationTime && !fieldsCreated)
            {
                ShowLaneStatsFields(); // Показать все нужные поля
                fieldsCreated = true;                
                simulationRunning = false; 
            }
        }
    }

    private void ShowLaneStatsFields()
    {
        string[] lanes = { "LR", "RL", "LL", "RR" };

        foreach (string lane in lanes)
        { 
            string avgQueueLength = TrafficStatistics.Instance.GetAverageQueueLength(lane).ToString();
            string avgWaitTime = TrafficStatistics.Instance.GetAverageWaitTime(lane).ToString();
            string carsPerMinute = TrafficStatistics.Instance.GetCarsPerMinute(lane).ToString();
            
            Debug.Log($" lane={lane}, avgQueueLength={avgQueueLength}, avgWaitTime={avgWaitTime}, carsPerMinute={carsPerMinute}");

            //AddLaneStatsFields(lane);
        }

        AddLaneStatsFields("Общее");
        Time.timeScale = 0f;//остановка модели
    }

    private void AddLaneStatsFields(string laneName)//пока что для каждой полосы всё выводится
    {
        string avgQueueLength = TrafficStatistics.Instance.GetAverageIntersectionQueueLength().ToString();
        string avgWaitTime = TrafficStatistics.Instance.GetAverageIntersectionWaitTime().ToString();
        string carsPerMinute = TrafficStatistics.Instance.GetCarsPerMinuteIntersection().ToString();
        AddTextAndOutput($"[{laneName}] Средняя длина очереди", $"{laneName}_QueueLength", avgQueueLength);
        AddTextAndOutput($"[{laneName}] Среднее время ожидания", $"{laneName}_WaitTime", avgWaitTime);
        AddTextAndOutput($"[{laneName}] Кол-во машин за 1 минуту", $"{laneName}_CarsPerMinute", carsPerMinute);
        

    }

    //private Coroutine spawnCarCoroutine; // Для отслеживания текущей корутины
    //private Coroutine coroutineLR, coroutineRL, coroutineLL, coroutineRR;

    private List<Coroutine> spawnCarCoroutines = new List<Coroutine>();

    private float currentInterval; // Для хранения текущего интервала спавна

    private void ShowFields(int buttonNumber)
    {
        // Очистка предыдущих полей
        foreach (Transform child in container_TextInput)//текст 
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in container_FieldInput)//инпутфилд
        {
            Destroy(child.gameObject);
        }


        Button_Start.gameObject.SetActive(true);

        //if (buttonNumber == 1)
        //{
        //    AddTextAndInput("Время(каждые ... секунд)", "Time");
        //    numberGenerator = 1;
        //}
        //else

        if (buttonNumber == 2)
        {
            AddTextAndInput("Количество машин в минуту", "Lambda");
            AddTextAndInput("Длителность работы модели(сек)", "Time");

            // Добавляем поля для светофора
            AddTextAndInput("Время красного сигнала (сек)", "RedTime");
            AddTextAndInput("Время зелёного сигнала (сек)", "GreenTime");

            numberGenerator = 2;
        }
    }

    private bool fieldsCreated = false; // Чтобы не создать их дважды
    private float simulationTime = 0f; // Сколько длится модель
    private float elapsedTime = 0f; // Сколько прошло времени
    private bool simulationRunning = false; // Флаг, идёт ли модель
    
    private void ShowStart()//спавн машин на основе выбора
    {
        Traffic_Lights trafficLights = FindObjectOfType<Traffic_Lights>();

        // Прекращаем спавн, если он уже был запущен
        foreach (var coroutine in spawnCarCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        spawnCarCoroutines.Clear();



        //if (numberGenerator == 1)
        //{
        //    float t = uniformGenerator.GenerateUniform();
        //    currentInterval = t; // Сохраняем текущий интервал
        //    spawnCarCoroutines.Add(StartCoroutine(SpawnCarRepeatedly(carSpawner.SpawnCar_LR, t)));
        //    spawnCarCoroutines.Add(StartCoroutine(SpawnCarRepeatedly(carSpawner.SpawnCar_RL, t)));
        //    spawnCarCoroutines.Add(StartCoroutine(SpawnCarRepeatedly(carSpawner.SpawnCar_LL, t)));
        //    spawnCarCoroutines.Add(StartCoroutine(SpawnCarRepeatedly(carSpawner.SpawnCar_RR, t)));

        //}

        if (numberGenerator == 2)
        {
            spawnCarCoroutines.Add(StartCoroutine(SpawnCarsWithPoisson(carSpawner.SpawnCar_LR)));
            spawnCarCoroutines.Add(StartCoroutine(SpawnCarsWithPoisson(carSpawner.SpawnCar_RL)));
            spawnCarCoroutines.Add(StartCoroutine(SpawnCarsWithPoisson(carSpawner.SpawnCar_LL)));
            spawnCarCoroutines.Add(StartCoroutine(SpawnCarsWithPoisson(carSpawner.SpawnCar_RR)));
            //spawnCarCoroutine = StartCoroutine(SpawnCarsWithPoisson());
            
        }
        // Активируем светофор
        if (trafficLights != null)
        {

            trafficLights.ActivateTrafficLight();
        }

        simulationTime = GetInputValues("Time"); // Берем время из инпутов
        elapsedTime = 0f;
        simulationRunning = true;
    }
       
    //private IEnumerator SpawnCarRepeatedly(Action spawnMethod, float interval)
    //{
    //    while (true)
    //    {
    //        spawnMethod?.Invoke(); 

    //        yield return new WaitForSeconds(interval);
    //    }
        
    //}

    private IEnumerator SpawnCarsWithPoisson(Action spawnMethod)//метод спавна машин по пуассоновскому потоку
    {
        foreach (float t in poissonGenerator.GeneratePoisson())
        {
            spawnMethod?.Invoke();//вызов метода спавна

            // Ждем интервал времени перед следующим спавном
            yield return new WaitForSeconds(t);
        }
    }

    private void SpeedUpGame()
    {
        // Считываем значение из inputFieldSpeed
        string inputText = inputFieldSpeed.text.Trim();

        // Проверяем, является ли введенный текст числом
        if (float.TryParse(inputText, out float speedFactor) && speedFactor > 0)
        {
            Time.timeScale = speedFactor;

            //Debug.Log($"Game speed increased to {Time.timeScale}x");
        }
        else
        {
            // Если введено неверное значение (не число или 0), предупреждение
            Debug.LogWarning("Invalid input for speed. Please enter a valid positive number.");
        }
    }

    private void AddTextAndInput(string text, string identifier) //для создания текст и инпутфилдов ВВОД
    {
        GameObject newText = Instantiate(textPrefab, container_TextInput);
        TMP_Text textComponent = newText.GetComponent<TMP_Text>(); 
        if (textComponent != null)
        {
            textComponent.text = text;
        }
        else
        {
            Debug.LogError("TMP_Text component not found in the instantiated textPrefab.");
        }

        GameObject newInputField = Instantiate(inputFieldPrefab, container_FieldInput);
        TMP_InputField inputFieldComponent = newInputField.GetComponent<TMP_InputField>(); 
        if (inputFieldComponent != null)
        {
            inputFieldComponent.name = identifier;
            inputFieldsInput.Add(inputFieldComponent); 
        }
        else
        {
            Debug.LogError("TMP_InputField component not found in the instantiated inputFieldPrefab.");
        }
    }

    private void AddTextAndOutput(string text, string identifier, string initialValue = "")//для создания текст и инпутфилдов ВЫВОД 
    {
        GameObject newText = Instantiate(textPrefab, container_TextOutput);
        TMP_Text textComponent = newText.GetComponent<TMP_Text>();
        if (textComponent != null)
        {
            textComponent.text = text;
        }
        else
        {
            Debug.LogError("TMP_Text component not found in the instantiated textPrefab.");
        }

        GameObject newInputField = Instantiate(inputFieldPrefab, container_FieldOutput);
        TMP_InputField inputFieldComponent = newInputField.GetComponent<TMP_InputField>();
        if (inputFieldComponent != null)
        {
            inputFieldComponent.name = identifier;
            inputFieldComponent.text = initialValue;
            inputFieldsOutput.Add(inputFieldComponent);
        }
        else
        {
            Debug.LogError("TMP_InputField component not found in the instantiated inputFieldPrefab.");
        }
    }

    public float GetInputValues(string value)//метод для получения значений из инпутфилдов
    {
        foreach (TMP_InputField inputField in inputFieldsInput)
        {
            // Проверяем значение в поле
            string inputText = inputField.text.Trim(); 

            if (string.IsNullOrEmpty(inputText))
            {
                Debug.LogWarning($"Input field {inputField.name} is empty.");
                return 0; // Если поле пустое, возвращаем 0
            }

            // Преобразуем строку в число с использованием TryParse
            if (float.TryParse(inputText, out float inputValue))
            {
                // Проверка на совпадение имени поля
                if (inputField.name == "Time" && value == "Time")
                {
                    //Debug.Log($"Time input: {inputValue}");
                    return inputValue;
                }
                else if (inputField.name == "Lambda" && value == "Lambda")
                {
                    //Debug.Log($"Lambda input: {inputValue}");
                    return inputValue;
                }
                else if (inputField.name == "RedTime" && value == "RedTime")
                {
                    //Debug.Log($"RedTime input: {inputValue}");
                    return inputValue;
                }
                else if (inputField.name == "GreenTime" && value == "GreenTime")
                {
                    //Debug.Log($"GreenTime input: {inputValue}");
                    return inputValue;
                }
            }
            else
            {
                Debug.LogWarning($"Invalid input in field {inputField.name}: {inputText}");
            }
        }

        // Если поле не найдено или значение невалидно, возвращаем 0
        return 0;
    }
}
