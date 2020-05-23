using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Initial constraint objects fix parts of the generation process in places.
    /// Use the utility methods on <see cref="TesseraGenerator"/> to create these objects.
    /// </summary>
    [Serializable]
    public class TesseraInitialConstraint
    {
        internal List<OrientedFace> faceDetails;

        internal List<Vector3Int> offsets;

        internal MatrixInt3x3 rotator;

        internal Vector3Int cell;
    }
}