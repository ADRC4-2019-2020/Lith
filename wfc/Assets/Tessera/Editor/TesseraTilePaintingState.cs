using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    public class TesseraTilePaintingState : ScriptableSingleton<TesseraTilePaintingState>
    {
        [SerializeField]
        private bool m_showBackface = false;

        [SerializeField]
        private float m_opacity = 0.8f;

        [SerializeField]
        private bool m_showAll = false;

        [SerializeField]
        private int m_paintIndex = 1;

        public static bool showBackface
        {
            get { return instance.m_showBackface; }
            set { instance.m_showBackface = value; }
        }

        public static float opacity
        {
            get { return instance.m_opacity; }
            set { instance.m_opacity = value; }
        }

        public static bool showAll
        {
            get { return instance.m_showAll; }
            set { instance.m_showAll = value; }
        }

        public static int paintIndex
        {
            get { return instance.m_paintIndex; }
            set { instance.m_paintIndex = value; }
        }
    }
}