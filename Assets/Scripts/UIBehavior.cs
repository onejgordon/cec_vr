﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UIBehavior : MonoBehaviour
{
    public Canvas UIcanvas;
    public GameObject statusHUD;

    public GameObject screenText;
    public GameObject screenBG;

    private float countdown_secs = 0;
    private string countdown_message = null;

    void Start()
    {
        this.UIcanvas = GetComponent<Canvas>();

    }

    // Update is called once per frame
    void Update()
    {
        if (countdown_secs > 0) {
            int seconds_left = (int) (countdown_secs - Time.time);
            if (seconds_left < 0) countdown_secs = 0;
            string message = countdown_message + " - " + seconds_left.ToString();
            ShowHUDMessage(message);
        }
    }

    public void ClearCountdown() {
        countdown_secs = 0;
        countdown_message = null;
    }
    public void ShowHUDMessage(string message) {
        this.statusHUD.GetComponent<Text>().text = message;
    }

    public void ShowHUDCountdownMessage(int seconds, string message) {
        countdown_secs = Time.time + seconds;
        countdown_message = message;
    }

    public void ShowHUDScreen(string message, Color bgcolor) {
        this.screenBG.GetComponent<Image>().color = bgcolor;
        this.screenBG.SetActive(true);
        this.screenText.GetComponent<Text>().text = message;
        this.screenText.SetActive(true);
    }

    public void HideHUDScreen() {
        this.screenBG.SetActive(false);
        this.screenText.SetActive(false);
    }
}
