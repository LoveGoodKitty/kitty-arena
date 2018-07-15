using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;


// todo:
// level generation (walking reveals)
// destroying drawable entities (empty tiles get disposed)


namespace GameClassLibrary
{
    public static class GameStatic
    {
        public static readonly float TimeStep = 1.0f / 10.0f;
        public static readonly float CameraFollowSpeed = 10.0f;
        public static readonly float CharacterTurnSpeed = 360.0f * 4.0f;
    }

    struct PlayerMove
    {
        public Vector3 Offset;
        public ulong EntityId;
        public int Frame;
    }

    struct PlayerAbility
    {
        public Vector3 Destination;
        public ulong EntityId;
        public int Frame;
        public int AbilityId;
    }

    class MovableObject
    {
        public ulong Id;

        public Vector3 Position;
        public Vector3 StartPosition;
        public Vector3 DestinationPosition;
        public Vector3 MovementVector;
        public Quaternion Rotation;
        public bool Moving;
        public float Speed = 5.0f;

        public MovableObject(ulong EntityId)
        {
            this.Id = EntityId;
        }

        float moveStartTime = 0.0f;

        public void Move(Vector3 offset)
        {
            var destination = Position + offset;

            var distance = Vector3.Distance(Position, destination);

            if (distance < float.Epsilon)
                return;

            var minDistance = Speed * GameStatic.TimeStep;

            if (distance < minDistance)
                destination = (Position + (destination - Position).normalized * minDistance);

            Moving = true;
            DestinationPosition = destination;

            MovementVector = Vector3.Normalize(destination - Position);

            Rotation = Quaternion.LookRotation(MovementVector);
        }

        public void Step()
        {
            if (Moving)
            {
                Position = Position + MovementVector * Speed * GameStatic.TimeStep;
                //Rotation = Quaternion.RotateTowards(Rotation, destinationRotation, 360.0f * GameStatic.CharacterTurnSpeed * GameStatic.TimeStep);

                var vectorToTarget = (DestinationPosition - Position).normalized;

                var vectorReversed = Mathf.Sign(MovementVector.x) != Mathf.Sign(vectorToTarget.x) || Mathf.Sign(MovementVector.z) != Mathf.Sign(vectorToTarget.z);

                if (vectorReversed)
                {
                    Position = DestinationPosition;
                    Moving = false;
                }
            }
        }
    }

    class GroundTile : MovableObject
    {
        public int X;
        public int Y;
        public bool Blocked;
        public object Color;

        public GroundTile(ulong Id, Vector3 Position) : base(Id)
        {
            this.Position = Position;
        }
    }

    interface IDrawableObject
    {
        object GetGameObject();
        GameObject GetUnityObject();
        void UpdateUnityObject(GameState state);
    }

    class GroundTileDrawable : IDrawableObject
    {
        public UnityEngine.GameObject o;
        public GroundTile groundTile;

        public GroundTileDrawable(GroundTile groundPlane)
        {
            o = UnityEngine.Object.Instantiate(GameResources.groundPlane) as GameObject;
            this.groundTile = groundPlane;

            o.transform.position = groundTile.Position;
        }

        public GameObject GetUnityObject()
        {
            return o;
        }

        public object GetGameObject()
        {
            return groundTile;
        }

        public void UpdateUnityObject(GameState state)
        {

        }
    }

    class Character : MovableObject
    {
        public float Mana = 100.0f;

        public Character(ulong EntityId) : base(EntityId)
        {

        }
    }
    class CharacterDrawable : IDrawableObject
    {
        public GameObject o;
        public Character character;

        public Quaternion SmoothRotation = Quaternion.identity;

        Animator animator;
        private float animationSpellDuration = 1.0f;
        private float animationRunDuration = 1.0f;

        public CharacterDrawable(Character character)
        {
            this.character = character;
            o = GameObject.Instantiate(Resources.Load("GirlPrefab", typeof(GameObject))) as GameObject;
            animator = o.GetComponent<Animator>();

            var allClips = animator.runtimeAnimatorController.animationClips;
            foreach (var clip in allClips)
            {
                if (clip.name.StartsWith("spell"))
                {
                    animationSpellDuration = clip.length;
                }

                if (clip.name.StartsWith("run"))
                {
                    animationRunDuration = clip.length;
                    // add footprint sound events at contact frames..
                }
            }
        }

        public GameObject GetUnityObject()
        {
            return o;
        }

        public object GetGameObject()
        {
            return character;
        }

        Vector3 lastSeenPosition = Vector3.zero;
        public void UpdateUnityObject(GameState state)
        {
            Vector3 PositionInFuture(float timeInFuture)
            {
                var positionInFuture = (character.Position + character.MovementVector * character.Speed * state.timeSinceStep);

                // dont overshoot extrapolated position over destination position
                var vectorToTarget = (character.DestinationPosition - positionInFuture).normalized;
                var vectorReversed = Mathf.Sign(character.MovementVector.x) != Mathf.Sign(vectorToTarget.x) || Mathf.Sign(character.MovementVector.z) != Mathf.Sign(vectorToTarget.z);
                if (vectorReversed)
                {
                    return character.DestinationPosition;
                }
                else
                {
                    return positionInFuture;
                }
            }

            Quaternion RotationInFuture(float elapsedTime)
            {
                var r = Quaternion.RotateTowards(SmoothRotation, character.Rotation, GameStatic.CharacterTurnSpeed * Time.deltaTime);
                SmoothRotation = r;
                return r;
            }

            //o.transform.position = character.Position;
            var extrapolatedPosition = PositionInFuture(state.timeSinceStep);
            o.transform.position = extrapolatedPosition;

            //o.transform.rotation = character.Rotation;
            //o.transform.rotation = character.RotationInFuture(state.lastUpdateTotalTime);
            //o.transform.rotation = Quaternion.RotateTowards(o.transform.rotation, character.Rotation, GameStatic.CharacterTurnSpeed * Time.deltaTime);
            o.transform.rotation = RotationInFuture(state.lastUpdateTotalTime);

            //var isMoving = character.Moving;
            var isMoving = Vector3.Distance(extrapolatedPosition, lastSeenPosition) > 0.0f; // and is moving, not in kickback

            if (isMoving)
            {

            }

            animator.SetBool("Rest", !isMoving);
            animator.SetFloat("Speed", character.Speed * (0.25f / animationRunDuration)); // (1.0f / steps? / animDuration)

            lastSeenPosition = extrapolatedPosition;
        }
    }

    [Serializable]
    class GameState
    {
        public float Time;
        public ulong Frame;
        public Dictionary<ulong, object> Entities;

        public List<Character> Characters;
        public List<GroundTile> GroundTiles;

        public Dictionary<(int, int), GroundTile> TileLookup;

        private DisplayManager drawableManager;

        private ulong entityCounter;
        public ulong GetNewEntityId()
        {
            entityCounter = entityCounter + 1;
            return entityCounter;
        }

        public void AddEntity(ulong id, object entity)
        {
            Entities.Add(id, entity);
        }

        public GameState(DisplayManager drawableManager)
        {
            this.drawableManager = drawableManager;

            Time = 0.0f;
            Frame = 0;
            entityCounter = 0;

            Entities = new Dictionary<ulong, object>();

            Characters = new List<Character>();
            GroundTiles = new List<GroundTile>();

            TileLookup = new Dictionary<(int, int), GroundTile>();
        }

        readonly float tileSize = 10.0f;

        (int, int) PositionToTile(Vector3 Position)
        {
            return ((int)(Position.x / tileSize), (int)(Position.z / tileSize));
        }

        void AddTile((int, int) tileIndex)
        {
            var (x, z) = tileIndex;
            var Position = new Vector3(x * tileSize, 0.0f, z * tileSize);
            var tile = new GroundTile(GetNewEntityId(), Position);
            tile.X = x;
            tile.Y = z;
            Entities.Add(tile.Id, tile);
            GroundTiles.Add(tile);
            TileLookup.Add(tileIndex, tile);
        }
        void RemoveTile(GroundTile tile)
        {
            Entities.Remove(tile.Id);
            GroundTiles.Remove(tile);
            TileLookup.Remove((tile.X, tile.Y));
        }

        public void AddCharacter()
        {
            var character = new Character(GetNewEntityId());
            Entities.Add(character.Id, character);
            Characters.Add(character);
            drawableManager.Display(character);
        }

        public (int, int) PositionToCell(float x, float z, float cellSize)
        {
            //var somex = ((int)((x + cellSize / 2.0f) / cellSize)) * cellSize;
            //var somez = ((int)((z + cellSize / 2.0f) / cellSize)) * cellSize;
            int intx = Mathf.CeilToInt(x / cellSize);
            int intz = Mathf.CeilToInt(z / cellSize);
            return (intx, intz);
        }

        public float timeSinceStep = 0.0f;
        public float lastUpdateTotalTime = 0.0f;
        public void Step(float elapsed, List<object> commands, Action<List<object>> getLocalInput)
        {
            timeSinceStep += elapsed;
            lastUpdateTotalTime = timeSinceStep;

            // need to continue accumulating commands even if simulation step is not yet nessasery
            // move 
            getLocalInput(commands);

            while (timeSinceStep >= GameStatic.TimeStep)
            {
                timeSinceStep -= GameStatic.TimeStep;
                Time += GameStatic.TimeStep;

                GameDebugConsole.Log(Frame.ToString("0 frame"), -1, "frame");

                foreach (var character in Characters)
                {
                    character.Step();

                    // get and create tiles nearby player

                    var minTilesToShow = 9.5f;
                    var windowSize = tileSize * (minTilesToShow * 2.0f + 1.0f);
                    var halfWindow = windowSize / 2.0f;

                    //get start and end world coord, convert to tile index

                    var checkStartX = (character.Position.x - halfWindow);
                    var checkEndX = (character.Position.x + halfWindow);

                    var checkStartZ = (character.Position.z - halfWindow);
                    var checkEndZ = (character.Position.z + halfWindow);

                    var (sx, sz) = PositionToCell(checkStartX, checkStartZ, tileSize);
                    var (ex, ez) = PositionToCell(checkEndX, checkEndZ, tileSize);

                    // itterate over a window of tiles for each character...
                    for (int x = sx; x < ex; x++)
                    {
                        for (int z = sz; z < ez; z++)
                        {
                            var tileIndex = (x, z);

                            // check if tile at index exists
                            if (!TileLookup.TryGetValue(tileIndex, out GroundTile tile))
                            {
                                // create nonexistant tiles
                                AddTile(tileIndex);
                            }
                        }
                    }
                }
                
                // stream in new commands from local player
                if (commands.Count == 0)
                    getLocalInput(commands);

                // if networked - get network commands
                // if replay - stream in recorded commands

                foreach (var command in commands)
                {
                    // when was command called? (frame)
                    // who called command
                    // handle late commands (rewind, replay)
                    // keep list of incoming and processed commands
                    switch (command)
                    {
                        case PlayerMove o:
                            var player = Entities[o.EntityId] as Character;
                            player.Move(o.Offset);

                            break;
                        case PlayerAbility o:
                            foreach (var tile in GroundTiles)
                            {
                                Entities.Remove(tile.Id);
                                TileLookup.Remove((tile.X, tile.Y));
                            }
                            GroundTiles.Clear();

                            AddCharacter();

                            GameDebugConsole.Log("Ability!!", 5.0f);

                            //var caster = Entities[o.EntityId] as Character;
                            // determine spell to cast
                            // set character to casting state, update to play appropiate casting animation in drawable, 
                            // set animation position in time and speed based on character casting state
                            // spell and move durations / lenghts in full frames? use frame numbers for timing and duration
                            // create spell instance and drawable
                            // spell updates and affect game state


                            break;
                        default:
                            Debug.LogError("Unsupported command");
                            break;
                    }
                }

                commands.Clear();

                Frame += 1;
            }
        }
    }

    class GameRunner
    {
        public GameState gameState;
        public DisplayManager displayManager;
        private List<object> inputCommands;

        public ulong LocalPlayerID;

        public float UpdateTime;

        public float TotalElapsedTime;

        public float StateUpdateTime;

        public float DrawUpdateTime;

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
            if (Input.GetKey(KeyCode.Mouse0))
            {
                if (CursorOffsetFromCenterWorld(out Vector3 hit))
                {
                    var move = new PlayerMove();
                    move.Offset = hit;
                    move.EntityId = LocalPlayerID;
                    move.Frame = 0;
                    inputCommands.Add(move);

                    if (inputCommands.Count > 1)
                        inputCommands.RemoveAt(0);
                }
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                inputCommands.Add(new PlayerAbility());

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

                    offset = targetHit - middleHit;

                    return true;
                }
            }

            return false;
        }

        float lastElapsed = 0.0f;
        Queue<float> frameTimes = new Queue<float>();
        int maxFrames = 120;
        public float averageFrameTime = 0.0f;
        public void Update(float elapsed)
        {
            var timeStart = Time.realtimeSinceStartup;
            lastElapsed = elapsed;

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

    class DisplayManager
    {
        public Dictionary<object, IDrawableObject> set;

        public DisplayManager()
        {
            set = new Dictionary<object, IDrawableObject>();
        }

        public void Display(object gameObject)
        {
            if (set.TryGetValue(gameObject, out IDrawableObject drawable))
            {
                Debug.Log("Adding already contained game object to drawable set. " + gameObject.ToString());
            }
            else
            {
                switch (gameObject)
                {
                    case Character o:
                        set.Add(gameObject, new CharacterDrawable(o));
                        break;

                    case GroundTile o:
                        set.Add(gameObject, new GroundTileDrawable(o));
                        break;

                    default:
                        //Debug.Log("Trying to display game object without display handling. " + gameObject.ToString());
                        break;
                }
            }
        }

        public void Refresh(GameState state, GameRunner runner)
        {
            void setCameraToObject(IDrawableObject target)
            {
                if (target != null && GameResources.Camera != null)
                {
                    var o = target.GetUnityObject();
                    if (o != null)
                    {
                        GameResources.Camera.SetTarget(o);
                    }
                }
            }

            // in drawable each frame:
            // go over game state entity(drawable?) list by game object hash
            // try find existing associated drawable object - drawable
            // create a new darawable in switch if not exists
            // go over each game object in drawables and dispose unity object if object does not exist in game state
            // update existing drawables(updatables?)
            // remove disposed unity objects from drawable dictionary

            foreach (var stateObject in state.Entities.Values)
            {
                // if stateObject is drawable at all?
                if (!set.ContainsKey(stateObject))
                {
                    Display(stateObject);
                    //Debug.Log("Display created for : " + stateObject.ToString());
                }
            }

            var disposedStateObjects = new List<object>();
            foreach (var kvp in set)
            {
                if (state.Entities.ContainsValue(kvp.Key))
                {
                    // if this is updatable at all?
                    kvp.Value.UpdateUnityObject(state);
                }
                else
                {
                    disposedStateObjects.Add(kvp.Key);

                    var o = kvp.Value.GetUnityObject();

                    if (Application.isEditor)
                        UnityEngine.Object.DestroyImmediate(o);
                    else
                        UnityEngine.Object.Destroy(o);
                }
            }
            foreach (var removedState in disposedStateObjects)
            {
                set.Remove(removedState);
            }

            // set camera to follow local player location
            if (set.TryGetValue(state.Entities[runner.LocalPlayerID], out IDrawableObject playerTarget))
            {
                setCameraToObject(playerTarget);
            }
        }
    }

    public static class GameResources
    {
        public static ThirdPersonCamera Camera;
        public static UnityEngine.Object girlPrefab;
        public static UnityEngine.Object sphere;

        public static UnityEngine.Object groundPlane;
        public static UnityEngine.Object playerPrefab;

        public static AudioClip audioBeep1;

        public static void LoadAll()
        {
            var mainCamera = GameObject.FindWithTag("MainCamera");
            var cameraScript = mainCamera.GetComponent<ThirdPersonCamera>();
            Camera = cameraScript;

            audioBeep1 = Resources.Load<AudioClip>("Sounds\\beep1");

            groundPlane = Resources.Load("Plane");

            //sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere) as GameObject;


            //girlPrefab = GameObject.Instantiate(Resources.Load("GirlPrefab", typeof(GameObject))) as GameObject;
        }
    }
}
