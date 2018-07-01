using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace GameClassLibrary
{
    struct PlayerMove
    {
        public Vector3 Destination;
        public int PlayerId;
        public int Frame;
    }

    struct PlayerAbility
    {
        public Vector3 Destination;
        public int PlayerId;
        public int Frame;
        public int AbilityId;
    }

    class MovableObject
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public bool Moving;

        private Quaternion startRotation;
        private Quaternion destinationRotation;

        private Vector3 startPosition;
        private Vector3 destinationPosition;

        private float distance;
        private float timeMoving;
        private float speed;

        private Vector3 vector;

        public void Move(Vector3 destination)
        {
            Moving = true;
            destinationPosition = destination;
            vector = Vector3.Normalize(destination - Position);
            speed = 5.0f;
            startRotation = Rotation;
            destinationRotation = Quaternion.LookRotation(vector);
            timeMoving = 0.0f;
        }

        public void UpdateMovement(float elapsed)
        {
            if (Moving)
            {
                timeMoving += elapsed;

                var distanceToTarget = Vector3.Distance(Position, destinationPosition);
                if (distanceToTarget >= 0.05f)
                {
                    //var destinationRotation = Quaternion.LookRotation(vector);

                    // handle overshooting
                    Position = Position + vector * speed * elapsed;
                    //Rotation = 
                    Rotation = Quaternion.Lerp(startRotation, destinationRotation, (timeMoving / (1.0f / 8.0f)));
                }
                else
                {
                    Moving = false;
                    timeMoving = 0.0f;
                }
            }
        }
    }

    class Instance
    {
        public int Id;
    }

    class Character : MovableObject
    {
        //public MovableObject movableObject;
        public int PlayerId = -1;
        public float Mana = 100.0f;

        public Character()
        {

        }
    }

    class GameState
    {
        public float Time;
        public List<Character> Characters;

        public GameState()
        {
            Characters = new List<Character>();
            Time = 0.0f;
        }

        public void Update(float elapsed)
        {
            foreach (var character in Characters)
            {
                character.UpdateMovement(elapsed);
            }
        }
    }

    class GameMode
    {
        public GameState gameState;
        private DrawableManager drawableManager;
        private List<object> inputCommands;

        public GameMode()
        {
            gameState = new GameState();
            gameState.Characters.Add(new Character());

            drawableManager = new DrawableManager();
            inputCommands = new List<object>();
        }

        void GetLocalCommands()
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                if (CursorHit(out RaycastHit hit))
                {
                    var move = new PlayerMove();
                    move.Destination = hit.point;
                    move.PlayerId = 0;
                    move.Frame = 0;

                    inputCommands.Add(move);
                }
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                inputCommands.Add(new PlayerAbility());
            }
        }

        private bool CursorHit(out RaycastHit hit)
        {
            var interactRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            var mask = LayerMask.GetMask("Ground");
            return Physics.Raycast(interactRay, out hit, Mathf.Infinity, mask);
        }

        public void Update(float elapsed)
        {
            GetLocalCommands();

            if (elapsed > (1.0 / 20.0f))
                Debug.Log("LAGGGGGGGGGGGGG + " + (elapsed * 1000.0f).ToString("0.ms"));

            foreach (var command in inputCommands)
            {
                switch (command)
                {
                    case PlayerMove o:
                        var player = gameState.Characters[o.PlayerId];
                        player.Move(o.Destination);
                        break;
                    case PlayerAbility o:
                        //gameState.Characters.Add(new Character());
                        break;
                    default:
                        Debug.LogError("Unsupported command");
                        break;
                }
            }

            inputCommands.Clear();

            gameState.Update(elapsed);

            drawableManager.Update(gameState);
        }

    }

    interface IDrawableObject
    {
        GameObject GetObject();
        void Update();
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

    class CharacterDrawable : IDrawableObject
    {
        public GameObject o;
        public Character character;

        Animator animator;

        public CharacterDrawable(Character character)
        {
            this.character = character;
            o = GameObject.Instantiate(Resources.Load("GirlPrefab", typeof(GameObject))) as GameObject;
            animator = o.GetComponent<Animator>();
        }

        public GameObject GetObject()
        {
            return o;
        }

        public void Update()
        {
            o.transform.position = character.Position;
            o.transform.rotation = character.Rotation;

            var isMoving = character.Moving;

            if (isMoving)
            {
                
            }

            animator.SetBool("Rest", !isMoving);
        }
    }

    class DrawableManager
    {
        public Dictionary<int, IDrawableObject> drawables;

        public DrawableManager()
        {
            drawables = new Dictionary<int, IDrawableObject>();
        }

        public void Update(GameState state)
        {
            void setCameraToObject(IDrawableObject target)
            {
                if (GraphicsResources.Camera != null)
                {
                    var o = target.GetObject();
                    if (o != null)
                    {
                        GraphicsResources.Camera.SetTarget(o);
                    }
                }
            }

            foreach (var player in state.Characters)
            {
                if (drawables.TryGetValue(player.PlayerId, out IDrawableObject drawableObject))
                {
                    drawableObject.Update();
                    setCameraToObject(drawableObject);
                }
                else
                {
                    var newDrawableObject = new CharacterDrawable(player);
                    drawables.Add(player.PlayerId, newDrawableObject);

                    newDrawableObject.Update();
                    setCameraToObject(newDrawableObject);

                }
            }


        }

    }
}
