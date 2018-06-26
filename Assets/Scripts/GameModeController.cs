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
    public void Start()
    {

        this.gameObject.AddComponent(typeof(GameClassLibrary.Libray.UnityGameLink));
    }
}

