using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Specialist_Dance
{
    public class TimePitch : MonoBehaviour
    {
        AudioSource source;
        void Awake()
        {
            source = GetComponent<AudioSource>();
        }

        void Update()
        {
            source.pitch = Time.timeScale;
        }
    }
}
