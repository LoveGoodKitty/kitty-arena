using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameClassLibrary
{
    enum SpellCastResult
    {
        SUCCESS,
        NO_RESOURCE,
        COOLDOWN,
        PLAYER_BUSY,
        FAIL
    }

    interface ISpell
    {
        SpellCastResult Cast(Character callingPlayer, Vector3 destination);
    }

    class ShardSpell : ISpell
    {
        public CharacterAnimation Animation = CharacterAnimation.DIRECTED_CAST;
        public float CastTime = 1.0f;

        public SpellCastResult Cast(Character callingPlayer, Vector3 destination)
        {

            // check character status to cast

            // set player to casting state


            // create projectile
            var o = UnityEngine.Object.Instantiate(GameResources.shard, destination, Quaternion.identity) as GameObject;


            return SpellCastResult.SUCCESS;
        }
    }

    class ShardProjectile
    {
        public Vector3 Destination;
        public Character Caster;

        


    }
}
