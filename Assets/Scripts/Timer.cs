using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public static Timer instance;
    public TextMeshProUGUI timerText;
    public float timer = 0f;

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            timerText = GetComponentInChildren<TextMeshProUGUI>();
        }
        else
            Destroy(gameObject);
    }

    public void UpdateTimer()
    {
        if (gameObject.activeSelf == false)
        {
            return;
        }
		//set timer UI
		timer += Time.deltaTime;
        timerText.text = "Time: " + (int)timer / 60 + ":" + (int)timer % 60;
		// timerText.text = hourCount +"h:"+ minuteCount +"m:"+(int)secondsCount + "s";
		// if(secondsCount >= 60){
		// 	minuteCount++;
		// 	secondsCount = 0;
		// }else if(minuteCount >= 60){
		// 	hourCount++;
		// 	minuteCount = 0;
		// }	
	}
}
