using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Specialist_Dance
{
    public class TransformReplicator : MonoBehaviour
    {
        public Transform Replic;

        void Update()
        {
            transform.rotation = Replic.rotation;
            transform.localPosition = Replic.localPosition;
        }
    }
}
