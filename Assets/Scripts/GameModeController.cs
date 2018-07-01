using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using System.Collections;
using GameClassLibrary;

public class GameModeController : MonoBehaviour
{
    private GameMode gameMode;

    public GameModeController()
    {
        gameMode = new GameMode();
    }

    public void Start()
    {
        GraphicsResources.LoadResources();
    }

    private void Update()
    {
        gameMode.Update(Time.deltaTime);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 20), (Time.deltaTime * 1000.0f).ToString("0.ms"));
    }
}

