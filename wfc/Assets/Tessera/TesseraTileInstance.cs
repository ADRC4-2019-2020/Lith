using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeBroglie.Rot;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Represents a request to instantiate a TesseraTile, post generation.
    /// </summary>
    public class TesseraTileInstance
    {
        public TesseraTile Tile { get; internal set; }
        public Vector3 Position { get; internal set; }
        public Quaternion Rotation { get; internal set; }
        public Vector3 LossyScale { get; internal set; }
        public Vector3 LocalPosition { get; internal set; }
        public Quaternion LocalRotation { get; internal set; }
        public Vector3 LocalScale { get; internal set; }
        [Obsolete("Use Cells")]
        public Vector3Int IntPosition { get; internal set; }

        public Vector3Int[] Cells { get; internal set; }
    }
}
