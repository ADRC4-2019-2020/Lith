using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using UnityEngine;

namespace Tessera
{
    internal static class GeometryUtils
    {
        // Is p in a rect between the origin and size
        internal static bool InBounds(Vector3Int p, Vector3Int size)
        {
            if (p.x < 0) return false;
            if (p.x >= size.x) return false;
            if (p.y < 0) return false;
            if (p.y >= size.y) return false;
            if (p.z < 0) return false;
            if (p.z >= size.z) return false;

            return true;
        }

        internal static Matrix4x4 ToMatrix(Rotation r)
        {
            var q = Quaternion.Euler(0, -r.RotateCw, 0);
            return Matrix4x4.TRS(Vector3.zero, q, new Vector3(r.ReflectX ? -1 : 1, 1, 1));
        }

        /// <summary>
        /// Rotates v about the y-axis by r.
        /// </summary>
        internal static Vector3Int Rotate(Rotation r, Vector3Int v)
        {
            (v.x, v.z) = TopoArrayUtils.SquareRotateVector(v.x, v.z, r);
            return v;
        }

        /// <summary>
        /// Rotates v about the y-axis by r.
        /// </summary>
        internal static Vector3 Rotate(Rotation r, Vector3 v)
        {
            if (r.ReflectX)
            {
                v.x = -v.x;
            }
            switch (r.RotateCw)
            {
                case 0 * 90:
                    return new Vector3(v.x, v.y, v.z);
                case 1 * 90:
                    return new Vector3(-v.z, v.y, v.x);
                case 2 * 90:
                    return new Vector3(-v.x, v.y, -v.z);
                case 3 * 90:
                    return new Vector3(v.z, v.y, -v.x);
            }
            throw new Exception();
        }

        /// <summary>
        /// Given a cube normal vector, converts it to the FaceDir enum
        /// </summary>
        internal static FaceDir FromNormal(Vector3Int v)
        {
            if (v.x == 1) return FaceDir.Right;
            if (v.x == -1) return FaceDir.Left;
            if (v.y == 1) return FaceDir.Up;
            if (v.y == -1) return FaceDir.Down;
            if (v.z == 1) return FaceDir.Forward;
            if (v.z == -1) return FaceDir.Back;

            throw new Exception();
        }

        /// <summary>
        /// Given a FaceDetails on given face of the cube,
        /// rotates the cube, and returns the new face and correctly oriented FaceDetails
        /// </summary>
        internal static (FaceDir, FaceDetails) ApplyRotator(FaceDir faceDir, FaceDetails faceDetails, MatrixInt3x3 rotator)
        {
            var rotatedFaceDirForward = rotator.Multiply(faceDir.Forward());
            var rotatedFaceDirUp = rotator.Multiply(faceDir.Up());
            var rotatedFaceDirRight = rotator.Multiply(Vector3.Cross(faceDir.Forward(), faceDir.Up()));
            var rotatedFaceDir = FromNormal(rotatedFaceDirForward);
            var trueUp = rotatedFaceDir.Up();
            var trueForward = rotatedFaceDirForward; // =  rotatedFaceDir.Forward();
            var trueRight = Vector3.Cross(trueForward, trueUp);
            // Find the rotation that will map rotatedFaceDirUp to trueUp
            // and rotatedFaceDirRight to trueRight
            var dot = Vector3.Dot(rotatedFaceDirUp, trueUp);
            var cross = Vector3.Dot(rotatedFaceDirUp, trueRight);
            Rotation faceRot;
            if (dot == 1)
            {
                faceRot = new Rotation();
            }
            else if (dot == -1)
            {
                faceRot = new Rotation(180);
            }
            else if (cross == 1)
            {
                faceRot = new Rotation(270);
            }
            else if (cross == -1)
            {
                faceRot = new Rotation(90);
            }
            else
            {
                throw new Exception();
            }
            if (Vector3.Dot(Vector3.Cross(rotatedFaceDirForward, rotatedFaceDirUp), rotatedFaceDirRight) < 0)
            {
                faceRot = new Rotation(360 - faceRot.RotateCw, true);
            }


            var rotatedFaceDetails = faceDetails.RotateBy(faceRot);

            return (rotatedFaceDir, rotatedFaceDetails);
        }

        internal static int GetMaskIndex(Vector3Int p, Vector3Int size)
        {
            return p.x + p.y * size.x + p.z * size.x * size.y;
        }

        /// <summary>
        /// Returns the cell, and a rotator for a tile placed near the generator
        /// A rotator takes vectors in the tile local space, and returns them in generator local space.
        /// It's always an distance presering transform (so it's always one of the symmetries of a cube).
        /// 
        /// NB: The cell returned corresponds to offset (0,0,0). The tile may not actually occupy that offset.
        /// </summary>
        internal static bool GetCell(
            Transform transform,
            Vector3 center,
            Vector3 tileSize,
            Vector3Int size,
            TesseraTile tile, 
            Matrix4x4 tileLocalToWorldMatrix, 
            out Vector3Int cell, 
            out MatrixInt3x3 rotator)
        {
            var m = transform.worldToLocalMatrix * tileLocalToWorldMatrix;

            Vector3Int Rotate(Vector3Int v)
            {
                var v1 = m.MultiplyVector(v);
                var v2 = new Vector3Int((int)Math.Round(v1.x), (int)Math.Round(v1.y), (int)Math.Round(v1.z));

                return v2;
            }

            // True if v is a unit vector along an axis
            bool Ok(Vector3Int v)
            {
                return Math.Abs(v.x) + Math.Abs(v.y) + Math.Abs(v.z) == 1;
            }

            var rotatedRight = Rotate(Vector3Int.right);
            var rotatedUp = Rotate(Vector3Int.up);
            var rotatedForward = Rotate(new Vector3Int(0, 0, 1));

            if (Ok(rotatedRight) && Ok(rotatedUp) && Ok(rotatedForward))
            {
                var localPos = m.MultiplyPoint(tile.center);
                var min = center - Vector3.Scale(size - Vector3Int.one, tileSize) / 2.0f;
                localPos -= min;
                var x = (int)Mathf.Round(localPos.x / tileSize.x);
                var y = (int)Mathf.Round(localPos.y / tileSize.y);
                var z = (int)Mathf.Round(localPos.z / tileSize.z);
                cell = new Vector3Int(x, y, z);
                rotator = new MatrixInt3x3
                {
                    col1 = rotatedRight,
                    col2 = rotatedUp,
                    col3 = rotatedForward,
                };
                return true;
            }
            else
            {
                cell = default;
                rotator = default;
                return false;
            }
        }
    }
}
