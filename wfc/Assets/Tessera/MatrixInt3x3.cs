using System;
using UnityEngine;

namespace Tessera
{
    [Serializable]
    internal class MatrixInt3x3
    {
        public Vector3Int col1;
        public Vector3Int col2;
        public Vector3Int col3;

        public Vector3Int Multiply(Vector3Int v)
        {
            return col1 * v.x + col2 * v.y + col3 * v.z;
        }

        public Vector3 Multiply(Vector3 v)
        {
            return (Vector3)col1 * v.x + (Vector3)col2 * v.y + (Vector3)col3 * v.z;
        }
    }
}