using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;


// todo:
// level generation (walking reveals)
// destroying drawable entities (empty tiles get disposed)


namespace GameClassLibrary
{
    public static class GameStatic
    {
        public static readonly float TimeStep = 1.0f / 25.0f;
        public static readonly float CameraFollowSpeed = 10.0f;
        public static readonly float CharacterTurnSpeed = 360.0f * 2.0f;
        public static Vector3 CursorTarget = Vector3.zero;
    }

    public struct PlayerMove
    {
        public Vector3 Offset;
        public ulong PlayerId;
        public int Frame;
    }

    public struct PlayerAbility
    {
        public Vector3 Destination;
        public ulong PlayerId;
        public int Frame;
        public int AbilityId;
    }

    abstract class Entity
    {
        public ulong Id;
        public abstract void Display(GameState gameState);

        public Entity(ulong id)
        {
            Id = id;
        }
    }

    abstract class MovableObject : Entity
    {
        //public ulong Id;

        public Vector3 Position;
        public Vector3 DestinationPosition;
        public Vector3 MovementVector;
        public Quaternion Rotation;
        public bool Moving;
        public float Speed = 5.0f;

        public MovableObject(ulong id) : base(id)
        {
            //this.Id = EntityId;
        }

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

        public void StopMovement()
        {
            Moving = false;
            DestinationPosition = Position;
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
                    //StopMovement();
                }
            }
        }
    }

    public enum CharacterAnimation
    {
        NONE,
        IDLE,
        IDLE1,
        MOVING,
        DIRECTED_CAST,
        OMNI_CAST,
        WEAPON_ATTACK,
        HURT,
        BLOCK,
        DIE,
    };

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
        void AddTile((int, int) tileIndex)
        {
            var (x, z) = tileIndex;
            var Position = new Vector3(x * tileSize, 0.0f, z * tileSize);
            var tile = new GroundTile(GetNewEntityId(), Position)
            {
                X = x,
                Y = z
            };
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

                    var minTilesToShow = 0.5f;
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
                            var player = Entities[o.PlayerId] as Character;
                            player.Move(o.Offset);

                            break;
                        case PlayerAbility o:
                            /*
                            foreach (var tile in GroundTiles)
                            {
                                Entities.Remove(tile.Id);
                                TileLookup.Remove((tile.X, tile.Y));
                            }
                            GroundTiles.Clear();*/

                            //AddCharacter();

                            // Get ability at index
                            // Call ability
                            // Ability checks and modifies player state - is it casting, reduce mana, change player state to casting
                            // If ability cast success or otherwise display local animations for local player

                            if (Entities.TryGetValue(o.PlayerId, out object callingPlayerEntity))
                            {
                                if (callingPlayerEntity is Character callingPlayer)
                                {
                                    if (o.AbilityId < callingPlayer.EquipedAbilities.Count && o.AbilityId >= 0)
                                    {
                                        var ability = callingPlayer.EquipedAbilities[o.AbilityId];
                                        var castResult = ability.Cast(callingPlayer, o.Destination);

                                        var str = "Ability" + castResult.ToString();
                                        GameDebugConsole.Log(str, 5.0f);
                                    }

                                }
                            }

                            // Ability can create ability entities in the world that can have some displayable object


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


}
