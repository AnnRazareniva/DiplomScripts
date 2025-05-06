using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generator
{

    public float time;
    public float lambda;
    private UI_Control value;

    //  онструктор принимает ссылку на UI_Control
    public Generator(UI_Control control)
    {
        value = control;
    }

    //ѕуассоновское распределение
    public IEnumerable<float> GeneratePoisson()
    {
        lambda = value.GetInputValues("Lambda");
        time = value.GetInputValues("Time");

        if (lambda <= 0 || time <= 0)
        {
            Debug.LogError("Lambda or Time is not valid. Please enter positive values.");
            Debug.LogError(lambda);
            Debug.LogError(time);
            yield break; // «авершаем выполнение, если значени€ некорректны
        }

        float t = 0;
        int N = 0;
        float T;

        //Debug.Log($"Starting Poisson generation with Lambda: {lambda} and Time: {time}");

        while (t <= time)
        {
            float r = UnityEngine.Random.value;
            T = -1 / lambda * (float)Math.Log(r);
            T *= 60;
            
            if (T < 4f)//если меньше 4 сек, то 4 сек, т.к. машины будут накладыватьс€ друг на друга
                T = 4f;

            yield return T;

            t += T;
            N++;
        }
    }

}
