using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameClassLibrary
{
    class GameRunner
    {
        public GameState gameState;
        public DisplayManager displayManager;
        private List<object> inputCommands;

        public ulong LocalPlayerID;

        public float UpdateTime;

        public float TotalElapsedTime;

        public GameRunner()
        {
            // list of input commands from local player
            inputCommands = new List<object>();

            // if local player / spectator create drawable manager
            displayManager = new DisplayManager();

            // create empty game state
            gameState = new GameState(displayManager);

            // create local character
            var localCharacter = new Character(gameState.GetNewEntityId());
            gameState.Entities.Add(localCharacter.Id, localCharacter);
            gameState.Characters.Add(localCharacter);
            displayManager.Display(localCharacter);

            LocalPlayerID = localCharacter.Id;

            TotalElapsedTime = 0.0f;
        }

        // add command queue / filter
        void GetLocalCommands(List<object> obj)
        {
            var interactRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            var plane = new Plane(Vector3.up, 0.0f);
            if (plane.Raycast(interactRay, out float distance))
            {
                var targetHit = interactRay.GetPoint(distance);
                GameStatic.CursorTarget = targetHit;
            }


            if (Input.GetKey(KeyCode.Mouse0))
            {
                if (CursorOffsetFromCenterWorld(out Vector3 offset))
                {
                    var move = new PlayerMove
                    {
                        Offset = offset,
                        PlayerId = LocalPlayerID,
                        Frame = 0
                    };
                    inputCommands.Add(move);

                    if (inputCommands.Count > 1)
                        inputCommands.RemoveAt(0);
                }
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                var ability = new PlayerAbility
                {
                    Destination = GameStatic.CursorTarget,
                    PlayerId = LocalPlayerID
                };
                inputCommands.Add(ability);

                //AudioSource.PlayClipAtPoint(GameResources.audioBeep1, GameResources.Camera.transform.position);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                //inputCommands.Add(new PlayerAbility());
                //AudioSource.PlayClipAtPoint(GameResources.audioBeep1, GameResources.Camera.transform.position);
                GameDebugConsole.Log("E", 2.0f);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var currentCharacter = gameState.Characters.IndexOf(gameState.Entities[LocalPlayerID] as Character);
                var newCharacter = Math.Max(0, (currentCharacter - 1) % gameState.Characters.Count);

                LocalPlayerID = (gameState.Entities[gameState.Characters[newCharacter].Id] as Character).Id;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                var currentCharacter = gameState.Characters.IndexOf(gameState.Entities[LocalPlayerID] as Character);
                var newCharacter = Math.Max(0, (currentCharacter + 1) % gameState.Characters.Count);

                LocalPlayerID = (gameState.Entities[gameState.Characters[newCharacter].Id] as Character).Id;
            }
        }

        private bool CursorOffsetFromCenterWorld(out Vector3 offset)
        {
            var interactRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            var middleOfScreen = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0.0f);

            var middleRay = Camera.main.ScreenPointToRay(middleOfScreen);

            offset = Vector3.zero;

            var plane = new Plane(Vector3.up, 0.0f);
            if (plane.Raycast(interactRay, out float distance))
            {
                var targetHit = interactRay.GetPoint(distance);

                if (plane.Raycast(middleRay, out float middleDistance))
                {
                    var middleHit = middleRay.GetPoint(middleDistance);

                    if (gameState.Entities.TryGetValue(LocalPlayerID, out object character))
                    {
                        var playerCharacter = character as Character;
                        offset = targetHit - playerCharacter.Position;
                    }
                    else
                    {
                        offset = targetHit - middleHit;
                    }

                    return true;
                }
            }

            return false;
        }

        float lastElapsed = 0.0f;
        Queue<float> frameTimes = new Queue<float>();
        readonly int maxFrames = 120;
        public float averageFrameTime = 0.0f;
        public void Update(float elapsed)
        {
            lastElapsed = elapsed;
            var timeStart = Time.realtimeSinceStartup;

            //GetLocalCommands();
            gameState.Step(elapsed, inputCommands, GetLocalCommands);

            // refesh and update objects to display
            displayManager.Refresh(gameState, this);

            UpdateTime = Time.realtimeSinceStartup - timeStart;
            frameTimes.Enqueue(UpdateTime);
            if (frameTimes.Count >= maxFrames)
                frameTimes.Dequeue();
            averageFrameTime = frameTimes.Average();

            if (UpdateTime > averageFrameTime * 2.0f && (Application.isEditor ? (UpdateTime > (1.0f / 25.0f)) : (UpdateTime > (1.0f / 60.0f))))
            {
                GameDebugConsole.Log(((UpdateTime - averageFrameTime) * 1000.0f).ToString("0.0 ms SPIKE!!"), 10.0f);
                AudioSource.PlayClipAtPoint(GameResources.audioBeep1, GameResources.Camera.transform.position + GameResources.Camera.transform.forward, 0.2f);
            }

        }
    }

    public class GameMode : MonoBehaviour
    {
        private GameRunner gameRunner;

        public GameMode()
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
            GameDebugConsole.Log((gameRunner.gameState.GroundTiles.Count).ToString("0 tiles"), -1, "tiles");
            GameDebugConsole.Log((gameRunner.displayManager.drawableSet.Count).ToString("0 drawables"), -1, "drawables");

            GameDebugConsole.Draw(Time.deltaTime);
        }
    }
}