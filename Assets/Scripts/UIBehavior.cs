﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;



public class UIBehavior : MonoBehaviour
{
    public Canvas UIcanvas;
    public GameObject statusHUD; // Countdown message overlay, etc
    public GameObject screenText;
    public GameObject screenBG; // Panel (background)
    public GameObject uiImage; // Image
    public GameObject room;
    private float countdown_secs = 0;
    private string countdown_message = null;

    public SteamVR_Action_Boolean grabAction;
    
    // State for confirmation screen
    private bool waitingForTrigger = false;
    private string invokeOnCallback;

    void Start()
    {
        this.UIcanvas = GetComponent<Canvas>();

    }

    // Update is called once per frame
    void Update()
    {
        if (countdown_secs > 0) {
            int seconds_left = Mathf.RoundToInt(countdown_secs - Time.time);
            if (seconds_left < 0) countdown_secs = 0;
            string message = countdown_message + " " + seconds_left.ToString();
            ShowHUDMessage(message);
        }

        // Check for trigger
        if (waitingForTrigger) {
            if (grabAction.GetLastStateDown(SteamVR_Input_Sources.Any)) {
                DismissConfirmationScreen();
            }
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
        this.room.SetActive(false);
        this.statusHUD.SetActive(false);
    }

    public void ShowHUDScreenWithConfirm(string message, Color bgcolor,  string callback) {
        invokeOnCallback = callback;
        waitingForTrigger = true;
        ShowHUDScreen(message, bgcolor);
    }

    public void ShowHUDImage(string image_path) {
        this.screenBG.GetComponent<Image>().color = Color.black;
        this.screenBG.SetActive(true);
        this.uiImage.SetActive(true);
        Util.SetImage(this.uiImage, image_path);
        this.screenText.SetActive(false);
        this.room.SetActive(false);
        this.statusHUD.SetActive(false);
    }

    public void ShowHUDImageWithDelayedConfirm(string image_path, string callback) {
        invokeOnCallback = callback;
        waitingForTrigger = true;
        waitingForTrigger = false;
        Invoke("AllowTrigger", 10); // 10 s
        ShowHUDImage(image_path);
    }

    public void ShowHUDScreenWithDelayedConfirm(string message, Color bgcolor,  string callback) {
        // Same as ShowHUDScreenWithConfirm but disallows confirm until X seconds
        // have passed (e.g. to allow experimenter intervention)
        invokeOnCallback = callback;
        waitingForTrigger = false;
        Invoke("AllowTrigger", 10); // 10 s
        ShowHUDScreen(message, bgcolor);
    }

    public void AllowTrigger() {
        waitingForTrigger = true;
    }

    public void DismissConfirmationScreen() {
        HideHUDScreen();
        ExperimentRunner exp = GameObject.Find("Camera").GetComponent<ExperimentRunner>();
        exp.Invoke(invokeOnCallback, 0);
        invokeOnCallback = null;
        waitingForTrigger = false;
    }

    public void HideHUDScreen() {
        this.uiImage.SetActive(false);
        this.screenBG.SetActive(false);
        this.screenText.SetActive(false);
        this.room.SetActive(true);
        this.statusHUD.SetActive(true);
    }
}
