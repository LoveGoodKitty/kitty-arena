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
    private GameRunner gameRunner;

    public GameModeController()
    {
    }

    public void Start()
    {
        //System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;

        GameResources.LoadAll();

        gameRunner = new GameRunner();

        /*
        try
        {
            var priority = System.Diagnostics.ProcessPriorityClass.High;
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = priority;
            GameDebugConsole.Log(priority.ToString(), 10.0f);
        }
        catch (Exception e)
        {
            GameDebugConsole.Log("Failed to set thread priority." + e.Message, 10.0f);
        }*/
    }

    private void FixedUpdate()
    {
        //gameMode.Update(Time.deltaTime);
    }

    private void Update()
    {
        gameRunner.Update(Time.deltaTime);
    }

    private void OnGUI()
    {
        //GUI.Label(new Rect(10, 10, 100, 20), (Time.deltaTime * 1000.0f).ToString("00.00ms Total"));
        //GUI.Label(new Rect(10, 30, 100, 20), (gameRunner.averageFrameTime * 1000.0f).ToString("00.00ms Run"));
        //GUI.Label(new Rect(10, 50, 100, 20), (gameRunner.gameState.GroundTiles.Count).ToString("0 tiles"));
        //GUI.Label(new Rect(10, 70, 100, 20), (gameRunner.drawableManager.set.Count).ToString("0 drawables"));

        GameDebugConsole.Log((Time.deltaTime * 1000.0f).ToString("0.0 ms total"), -1, "total");
        GameDebugConsole.Log((gameRunner.averageFrameTime * 1000.0f).ToString("0.0 ms run"), -1, "run");
        GameDebugConsole.Log((gameRunner.gameState.GroundTiles.Count).ToString("0 tiles"), - 1, "tiles");
        GameDebugConsole.Log((gameRunner.displayManager.set.Count).ToString("0 drawables"), -1, "drawables");

        GameDebugConsole.Draw(Time.deltaTime);
    }
}

