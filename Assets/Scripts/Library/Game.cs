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
        public static readonly float TimeStep = 1.0f / 5.0f;
        public static readonly float CameraFollowSpeed = 15.0f;
        public static readonly float CharacterTurnSpeed = 0.2f;
    }

    struct PlayerMove
    {
        public Vector3 Destination;
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
        public ulong EntityId;

        public Vector3 Position;
        public Quaternion Rotation;
        public bool Moving;

        private Quaternion startRotation;
        private Quaternion destinationRotation;

        private Vector3 startPosition;
        private Vector3 destinationPosition;

        private float distance;
        private float timeMoving;

        public float Speed = 5.0f;

        private float RotationTime = 0.16f;

        private Vector3 vector;

        public MovableObject(ulong EntityId)
        {
            this.EntityId = EntityId;
        }

        float moveStartTime = 0.0f;

        public void Move(Vector3 destination)
        {
            // make it so that character does not jump back and forward when issuing move command close to character

            // when command is issued to move little distance, and it finishes moving before new frame is issued it creates jerking



            var distance = Vector3.Distance(Position, destination);

            var minDistance = Speed * GameStatic.TimeStep;

            if (distance <= minDistance / 2.0f)
                return;

            if (distance < minDistance)
                destination = (Position + (destination - Position).normalized * minDistance);

            //destination.y = 0.0f;

            //Debug.Log((destination - Position).normalized);

            Moving = true;
            destinationPosition = destination;

            vector = Vector3.Normalize(destination - Position);

            //Rotation = Quaternion.Lerp(Rotation, destinationRotation, ((timeMoving) / RotationTime));
            Rotation = lastSeenRotation;
            startRotation = lastSeenRotation;
            destinationRotation = Quaternion.LookRotation(vector);

            timeMoving = 0.0f;
        }

        // smooth out position
        // move to visual part
        public Vector3 PositionInFuture(float timeInFuture)
        {
            var positionInFuture = (Position + vector * Speed * timeInFuture);

            var vectorToTarget = (destinationPosition - positionInFuture).normalized;

            var vectorReversed = Mathf.Sign(vector.x) != Mathf.Sign(vectorToTarget.x) || Mathf.Sign(vector.z) != Mathf.Sign(vectorToTarget.z);

            if (vectorReversed)
            {
                return destinationPosition;
            }
            else
            {
                return positionInFuture;
            }
        }

        // move to visual part
        Quaternion lastSeenRotation = Quaternion.identity;
        public Quaternion RotationInFuture(float updateTime)
        {
            // time infuuture -> time since last update
            //return (Quaternion.Lerp(Rotation, destinationRotation, ((timeMoving + timeInFuture) / RotationTime)));
            // last seen rotation... destination rotation... elapsed time since last seen
            var r = Quaternion.RotateTowards(lastSeenRotation, destinationRotation, 360.0f * GameStatic.CharacterTurnSpeed * updateTime);
            lastSeenRotation = r;
            return r;

        }

        public void StepMovement()
        {
            if (Moving)
            {
                timeMoving += GameStatic.TimeStep;

                //Position = Position + vector * Speed * (timeNow - moveStartTime);
                Position = Position + vector * Speed * GameStatic.TimeStep;
                //Rotation = Quaternion.RotateTowards(Rotation, destinationRotation, 360.0f * 1.0f * elapsed);

                var vectorToTarget = (destinationPosition - Position).normalized;

                var vectorReversed = Mathf.Sign(vector.x) != Mathf.Sign(vectorToTarget.x) || Mathf.Sign(vector.z) != Mathf.Sign(vectorToTarget.z);

                if (vectorReversed)
                {
                    //var distanceToTarget = Vector3.Magnitude(Position - destinationPosition);
                    //Debug.Log("Overshot Distance = " + distance.ToString("0.000 000"));

                    Position = destinationPosition;
                    startPosition = Position;

                    Moving = false;
                    vector = Vector3.zero;
                    //Position = destinationPosition;

                    //startRotation = Rotation;
                    //destinationRotation = Rotation;
                }
            }

            if (!Moving)
            {
                //startRotation = Rotation;
                //destinationRotation = Rotation;
                timeMoving = 0.0f;
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

    class GroundPlaneDrawable : IDrawableObject
    {
        public UnityEngine.GameObject o;
        public GroundTile GroundPlane;

        public GroundPlaneDrawable(GroundTile groundPlane)
        {
            o = UnityEngine.Object.Instantiate(Resources.Load("Plane", typeof(GameObject))) as GameObject;
            this.GroundPlane = groundPlane;
            o.transform.position = GroundPlane.Position;
        }

        public GameObject GetObject()
        {
            return o;
        }

        public void UpdateUnityObject(GameState state)
        {
            o.transform.position = GroundPlane.Position;
        }
    }

    internal class Character : MovableObject
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
                }
            }
        }

        public GameObject GetObject()
        {
            return o;
        }

        public void UpdateUnityObject(GameState state)
        {
            //o.transform.position = character.Position;
            o.transform.position = character.PositionInFuture(state.remainingTime);
            //o.transform.rotation = character.Rotation;
            o.transform.rotation = character.RotationInFuture(state.lastUpdateTotalTime);

            var isMoving = character.Moving;

            if (isMoving)
            {

            }

            animator.SetBool("Rest", !isMoving);
            animator.SetFloat("Speed", character.Speed * (0.25f / animationRunDuration)); // (1.0f / steps? / animDuration)

        }
    }

    [Serializable]
    class GameState
    {
        public float Time;
        public Dictionary<ulong, object> Entities;

        public List<Character> Characters;
        public List<GroundTile> GroundTiles;

        public Dictionary<(int, int), GroundTile> TileLookup;

        private ulong entityCounter;
        public ulong GetNewEntityId()
        {
            entityCounter = entityCounter + 1;
            return entityCounter;
        }

        public GameState()
        {
            Time = 0.0f;
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
            Entities.Add(tile.EntityId, tile);
            GroundTiles.Add(tile);
            TileLookup.Add(tileIndex, tile);
        }

        public (int, int) PositionToCell(float x, float z, float cellSize)
        {
            //var somex = ((int)((x + cellSize / 2.0f) / cellSize)) * cellSize;
            //var somez = ((int)((z + cellSize / 2.0f) / cellSize)) * cellSize;
            int intx = Mathf.CeilToInt(x / cellSize);
            int intz = Mathf.CeilToInt(z / cellSize);
            return (intx, intz);
        }

        public float remainingTime = 0.0f;
        public float lastUpdateTotalTime = 0.0f;
        public void Step(float elapsed, List<object> commands)
        {
            remainingTime += elapsed;
            lastUpdateTotalTime = remainingTime;

            while (remainingTime >= GameStatic.TimeStep)
            {
                remainingTime -= GameStatic.TimeStep;
                Time += GameStatic.TimeStep;

                foreach (var character in Characters)
                {
                    character.StepMovement();

                    // get and create tiles nearby player

                    var minTilesToShow = 1.5f;
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

                if (commands.Count > 0)
                    Debug.Log(commands.Count.ToString() + " commands in queue");

                foreach (var command in commands)
                {
                    switch (command)
                    {
                        case PlayerMove o:
                            var player = Entities[o.EntityId] as Character;
                            player.Move(o.Destination);

                            break;
                        case PlayerAbility o:
                            var caster = Entities[o.EntityId] as Character;
                            // set character to casting state
                            // create spell instance

                            var newCharacter = new Character(GetNewEntityId());
                            Entities.Add(newCharacter.EntityId, newCharacter);
                            Characters.Add(newCharacter);
                            break;
                        default:
                            Debug.LogError("Unsupported command");
                            break;
                    }
                }
                commands.Clear();
            }
        }
    }

    class GameRunner
    {
        public GameState gameState;
        private DrawableManager drawableManager;
        private List<object> inputCommands;
        public ulong LocalPlayerID;

        public float UpdateTime;

        public GameRunner()
        {
            // create empty game state
            gameState = new GameState();

            /*
            // create level
            void createGroundPlane(Vector3 Position)
            {
                var groundPlane = new GroundTile(gameState.GetNewEntityId(), Position);
                gameState.Entities.Add(groundPlane.EntityId, groundPlane);
                gameState.GroundTiles.Add(groundPlane);
            }

            createGroundPlane(Vector3.zero);
            createGroundPlane(new Vector3(10.0f, 0.0f, 0.0f));
            createGroundPlane(new Vector3(0.0f, 0.0f, 10.0f)); */

            // create local character
            var localCharacter = new Character(gameState.GetNewEntityId());
            gameState.Entities.Add(localCharacter.EntityId, localCharacter);
            gameState.Characters.Add(localCharacter);

            // set local player id for camera and input delegation
            LocalPlayerID = localCharacter.EntityId;

            inputCommands = new List<object>();

            drawableManager = new DrawableManager();
        }

        // add command queue / filter
        void GetLocalCommands()
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                if (CursorHit(out Vector3 hit))
                {
                    var move = new PlayerMove();
                    move.Destination = hit;
                    move.EntityId = LocalPlayerID;
                    move.Frame = 0;
                    inputCommands.Add(move);
                }
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                inputCommands.Add(new PlayerAbility());
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var currentCharacter = gameState.Characters.IndexOf(gameState.Entities[LocalPlayerID] as Character);
                var newCharacter = Math.Max(0, (currentCharacter - 1) % gameState.Characters.Count);

                LocalPlayerID = gameState.Characters[newCharacter].EntityId;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                var currentCharacter = gameState.Characters.IndexOf(gameState.Entities[LocalPlayerID] as Character);
                var newCharacter = Math.Max(0, (currentCharacter + 1) % gameState.Characters.Count);

                LocalPlayerID = gameState.Characters[newCharacter].EntityId;
            }
        }

        private bool CursorHit(out Vector3 hit)
        {
            var interactRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            //Physics.
            //var mask = LayerMask.GetMask("Ground");
            //return Physics.Raycast(interactRay, out hit, Mathf.Infinity, mask);
            //return Physics.Raycast(interactRay, out hit);
            var plane = new Plane(Vector3.up, 0.0f);
            if (plane.Raycast(interactRay, out float distance))
            {
                hit = interactRay.GetPoint(distance);
                return true;
            }
            else
            {
                hit = Vector3.zero;
                return false;
            }
        }

        float lastElapsed = 0.0f;
        Queue<float> frameTimes = new Queue<float>();
        int maxFrames = 120;
        public float averageFrameTime = 0.0f;
        public void Update(float elapsed)
        {
            var timeStart = Time.realtimeSinceStartup;

            frameTimes.Enqueue(elapsed);
            if (frameTimes.Count >= maxFrames)
            {
                frameTimes.Dequeue();
            }
            averageFrameTime = frameTimes.Average();

            if (elapsed > (1.0 / 20.0f))
                Debug.Log("LAGGGGGGGGGGGGG + " + (elapsed * 1000.0f).ToString("0.ms"));

            if (elapsed > averageFrameTime * 5.0f)
                Debug.Log("SPIKEEEEEEEEEEE + " + (elapsed * 1000.0f).ToString("0.ms"));

            lastElapsed = elapsed;

            GetLocalCommands();

            gameState.Step(elapsed, inputCommands);
            //inputCommands.Clear();

            drawableManager.Update(gameState, this);

            UpdateTime = Time.realtimeSinceStartup - timeStart;
        }

    }

    interface IDrawableObject
    {
        GameObject GetObject();
        void UpdateUnityObject(GameState state);
    }

    class DrawableManager
    {
        public Dictionary<ulong, IDrawableObject> drawables;

        public DrawableManager()
        {
            drawables = new Dictionary<ulong, IDrawableObject>();
        }

        public void Update(GameState state, GameRunner runner)
        {
            void setCameraToObject(IDrawableObject target)
            {
                if (target != null && GraphicsResources.Camera != null)
                {
                    var o = target.GetObject();
                    if (o != null)
                    {
                        GraphicsResources.Camera.SetTarget(o);
                    }
                }
            }

            foreach (var character in state.Characters)
            {
                if (drawables.TryGetValue(character.EntityId, out IDrawableObject drawableObject))
                {
                    drawableObject.UpdateUnityObject(state);
                }
                else
                {
                    var newDrawableObject = new CharacterDrawable(character);
                    drawables.Add(character.EntityId, newDrawableObject);

                    newDrawableObject.UpdateUnityObject(state);

                }
            }

            foreach (var groundPlane in state.GroundTiles)
            {
                if (drawables.TryGetValue(groundPlane.EntityId, out IDrawableObject drawableObject))
                {
                    drawableObject.UpdateUnityObject(state);
                }
                else
                {
                    var newDrawableObject = new GroundPlaneDrawable(groundPlane);
                    drawables.Add(groundPlane.EntityId, newDrawableObject);

                    newDrawableObject.UpdateUnityObject(state);
                }
            }

            if (drawables.TryGetValue(runner.LocalPlayerID, out IDrawableObject playerTarget))
            {
                setCameraToObject(playerTarget);
            }


        }

    }

    public static class GraphicsResources
    {
        public static ThirdPersonCamera Camera;
        public static UnityEngine.Object girlPrefab;
        public static UnityEngine.Object sphere;

        public static void LoadResources()
        {
            var mainCamera = GameObject.FindWithTag("MainCamera");
            var cameraScript = mainCamera.GetComponent<ThirdPersonCamera>();
            Camera = cameraScript;

            //sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere) as GameObject;


            //girlPrefab = GameObject.Instantiate(Resources.Load("GirlPrefab", typeof(GameObject))) as GameObject;
        }
    }
}
