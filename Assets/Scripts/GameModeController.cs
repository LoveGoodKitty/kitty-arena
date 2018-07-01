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
    private GameRunner gameMode;

    public GameModeController()
    {
        gameMode = new GameRunner();
    }

    public void Start()
    {
        GraphicsResources.LoadResources();

        var vecMiddle = new Vector3(3.0f, 0.0f, 0.0f);
        var vecStart = new Vector3(1.0f, 0.0f, 0.0f);
        var vecEnd = new Vector3(5.0f, 0.0f, 0.0f);

        var angle1 = Vector3.Angle(vecStart, vecMiddle);
        var angle2 = Vector3.Angle(vecEnd, vecMiddle);

        var dot1 = Vector3.Dot(vecStart, vecMiddle);
        var dot2 = Vector3.Dot(vecEnd, vecMiddle);

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

