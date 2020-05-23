using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(AnimatedGenerator))]
    class AnimatedGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var ag = (AnimatedGenerator)target;
            if (ag.IsRunning)
            {
                if (GUILayout.Button("Pause"))
                {
                    ag.PauseGeneration();
                }
                if (GUILayout.Button("Stop"))
                {
                    ag.StopGeneration();
                }
            }
            else
            {
                if (ag.IsStarted)
                {
                    if (GUILayout.Button("Resume"))
                    {
                        ag.ResumeGeneration();
                    }
                }
                if (GUILayout.Button("Start"))
                {
                    ag.StartGeneration();
                }
            }
        }
    }
}
