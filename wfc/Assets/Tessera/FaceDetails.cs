using DeBroglie.Rot;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Records the painted colors for a single face of one cube in a <see cref="TesseraTile"/>
    /// </summary>
    [Serializable]
    public class FaceDetails : IEnumerable<(Vector2Int, int)>
    {
        public int topLeft;
        public int top;
        public int topRight;
        public int left;
        public int center;
        public int right;
        public int bottomLeft;
        public int bottom;
        public int bottomRight;

        public int this[Vector2Int p]
        {
            get
            {
                switch (p.x + p.y * 3 + 4)
                {
                    case 0: return bottomLeft;
                    case 1: return bottom;
                    case 2: return bottomRight;
                    case 3: return left;
                    case 4: return center;
                    case 5: return right;
                    case 6: return topLeft;
                    case 7: return top;
                    case 8: return topRight;
                }
                return 0;
            }

            set
            {
                switch (p.x + p.y * 3 + 4)
                {
                    case 0: bottomLeft = value; break;
                    case 1: bottom = value; break;
                    case 2: bottomRight = value; break;
                    case 3: left = value; break;
                    case 4: center = value; break;
                    case 5: right = value; break;
                    case 6: topLeft = value; break;
                    case 7: top = value; break;
                    case 8: topRight = value; break;
                }
            }
        }

        /// <summary>
        /// Returns a new FaceDetails with the paint shuffled around.
        /// Assumes the rotation is about the normal of the face
        /// </summary>
        public FaceDetails RotateBy(Rotation r)
        {
            var c = (FaceDetails)MemberwiseClone();
            if (r.ReflectX) c.ReflectX();
            for (var i = 0; i < r.RotateCw / 90; i++) c.RotateCw();
            return c;
        }

        /// <summary>
        /// Returns a new FaceDetails with the paint shuffled around.
        /// Assumes the rotation is about the y-axis, and the this
        /// face has the given facing.
        /// </summary>
        public FaceDetails RotateBy(FaceDir detailsFaceDir, Rotation rot)
        {
            if (detailsFaceDir == FaceDir.Up)
            {
                return RotateBy(rot);
            }
            else if (detailsFaceDir == FaceDir.Down)
            {
                return RotateBy(new Rotation(360 - rot.RotateCw, rot.ReflectX));
            }
            else
            {
                if (rot.ReflectX)
                    return RotateBy(new Rotation(0, true));
                else
                    return this;
            }
        }


        private void ReflectX()
        {
            (topLeft, topRight) = (topRight, topLeft);
            (left, right) = (right, left);
            (bottomLeft, bottomRight) = (bottomRight, bottomLeft);
        }

        private void RotateCw()
        {
            (topLeft, topRight, bottomRight, bottomLeft) = (topRight, bottomRight, bottomLeft, topLeft);
            (top, right, bottom, left) = (right, bottom, left, top);
        }


        /// <summary>
        /// Returns an enumerator of length 9 with the position and color index
        /// </summary>
        public IEnumerator<(Vector2Int, int)> GetEnumerator()
        {
            // TODO: Match this to unity's conventions with regard to top/bottom
            yield return (new Vector2Int(-1, 1), topLeft);
            yield return (new Vector2Int(0, 1), top);
            yield return (new Vector2Int(1, 1), topRight);
            yield return (new Vector2Int(-1, 0), left);
            yield return (new Vector2Int(0, 0), center);
            yield return (new Vector2Int(1, 0), right);
            yield return (new Vector2Int(-1, -1), bottomLeft);
            yield return (new Vector2Int(0, -1), bottom);
            yield return (new Vector2Int(1, -1), bottomRight);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"({topLeft},{top},{topRight};{left},{center},{right};{bottomLeft},{bottom},{bottomRight})";
        }
    }
}