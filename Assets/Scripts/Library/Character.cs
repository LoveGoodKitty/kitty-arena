using System.Collections.Generic;
using UnityEngine;

namespace GameClassLibrary
{
    class Character : MovableObject
    {
        public float Mana = 100.0f;

        public List<ISpell> EquipedAbilities;

        public Character(ulong id) : base(id)
        {
            EquipedAbilities = new List<ISpell>();

            EquipedAbilities.Add(new ShardSpell());
        }

        public override void Display(GameState gameState)
        {
            throw new System.NotImplementedException();
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
            //o = GameObject.Instantiate(Resources.Load("girl", typeof(GameObject))) as GameObject;
            o = UnityEngine.Object.Instantiate(GameResources.girl) as GameObject;
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
        public void UpdateDisplay(GameState state)
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
}
