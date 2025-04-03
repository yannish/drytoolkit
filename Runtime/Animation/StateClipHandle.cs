using System;
using UnityEngine;

namespace drytoolkit.Runtime.Animation
{
    [Serializable]
    public class StateClipHandle : ClipHandle //... TODO: return a reference to this when it's created... & do stuff.
    {
        /*
     * What stuff...
     * ... cancel the anim?
     * ... reverse it?
     * ... scrub its timeline? (that would maybe be its own thing)
     * ...
     *
     *  Currently it's just a "blend to this state, then blend to this next state" kinda deal.
     *  Maybe proceed like that until it's a liability.
     *
     */

        public bool overrideBlendInTime = false;
        public float blendInOverrideTime = 0.1f;

        public float moveTowardsSpeed = 0f;
        public float startTime = 0f;
        public float playbackSpeed = 1f;
        public int index = -1;
    }
}
