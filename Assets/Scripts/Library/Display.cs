using System.Collections.Generic;
using UnityEngine;

namespace GameClassLibrary
{
    interface IDrawableObject
    {
        object GetGameObject();
        GameObject GetUnityObject();
        void UpdateDisplay(GameState state);
    }

    class DisplayManager
    {
        public Dictionary<object, IDrawableObject> drawableSet;

        public DisplayManager()
        {
            drawableSet = new Dictionary<object, IDrawableObject>();
        }

        public void Display(object gameObject)
        {
            if (drawableSet.TryGetValue(gameObject, out IDrawableObject drawable))
            {
                Debug.Log("Adding already contained game object to drawable set. " + gameObject.ToString());
            }
            else
            {
                switch (gameObject)
                {
                    case Character o:
                        drawableSet.Add(gameObject, new CharacterDrawable(o));
                        break;

                    case GroundTile o:
                        drawableSet.Add(gameObject, new GroundTileDrawable(o));
                        break;

                    default:
                        //Debug.Log("Trying to display game object without display handling. " + gameObject.ToString());
                        break;
                }
            }
        }

        public void Refresh(GameState gameState, GameRunner runner)
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

            foreach (var stateObject in gameState.Entities.Values)
            {
                // if stateObject is drawable at all?
                if (!drawableSet.ContainsKey(stateObject))
                {
                    Display(stateObject);
                    //Debug.Log("Display created for : " + stateObject.ToString());
                }
            }

            var disposedStateObjects = new List<object>();
            foreach (var kvp in drawableSet)
            {
                if (gameState.Entities.ContainsValue(kvp.Key))
                {
                    // if this is updatable at all?
                    kvp.Value.UpdateDisplay(gameState);
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
                drawableSet.Remove(removedState);
            }

            // set camera to follow local player location
            if (drawableSet.TryGetValue(gameState.Entities[runner.LocalPlayerID], out IDrawableObject playerTarget))
            {
                setCameraToObject(playerTarget);
            }
        }
    }

    public static class GameResources
    {
        public static ThirdPersonCamera Camera;

        public static UnityEngine.Object girl;
        public static UnityEngine.Object plane;

        public static UnityEngine.Object shard;

        public static AudioClip audioBeep1;

        public static void LoadAll()
        {
            var mainCamera = GameObject.FindWithTag("MainCamera");
            var cameraScript = mainCamera.GetComponent<ThirdPersonCamera>();
            Camera = cameraScript;

            audioBeep1 = Resources.Load<AudioClip>("Sounds\\beep1");

            plane = Resources.Load("plane");
            girl = Resources.Load("girl");
            shard = Resources.Load("Spells\\shard");
        }
    }

}
