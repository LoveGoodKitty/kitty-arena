using UnityEngine;

namespace GameClassLibrary
{
    class GroundTile : MovableObject
    {
        public int X;
        public int Y;
        public bool Blocked;
        public object Color;

        public GroundTile(ulong id, Vector3 Position) : base(id)
        {
            this.Position = Position;
        }

        public override void Display(GameState gameState)
        {
            throw new System.NotImplementedException();
        }
    }

    class GroundTileDrawable : IDrawableObject
    {
        public UnityEngine.GameObject o;
        public GroundTile groundTile;

        public GroundTileDrawable(GroundTile groundPlane)
        {
            o = UnityEngine.Object.Instantiate(GameResources.plane) as GameObject;
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

        public void UpdateDisplay(GameState state)
        {

        }
    }
}
