using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// GameObjects with this behaviour record adjacency information for use with a <see cref="TesseraGenerator"/>.
    /// </summary>
    [AddComponentMenu("Tessera/Tile")]
    public class TesseraTile : MonoBehaviour
    {
        /// <summary>
        /// Set this to control the colors and names used for painting on the tile.
        /// Defaults to <see cref="TesseraPalette.defaultPalette"/>.
        /// </summary>
        public TesseraPalette palette;

        /// <summary>
        /// A list of outward facing faces.
        /// For a normal cube tile, there are 6 faces. Each face contains adjacency information that indicates what other tiles can connect to it.
        /// It is recommended you only edit this via the Unity Editor, or <see cref="Get(Vector3Int, FaceDir)"/> and <see cref="AddOffset(Vector3Int)"/>
        /// </summary>
        public List<OrientedFace> faceDetails = new List<OrientedFace>
        {
            new OrientedFace(Vector3Int.zero, FaceDir.Left, new FaceDetails() ),
            new OrientedFace(Vector3Int.zero, FaceDir.Right, new FaceDetails() ),
            new OrientedFace(Vector3Int.zero, FaceDir.Up, new FaceDetails() ),
            new OrientedFace(Vector3Int.zero, FaceDir.Down, new FaceDetails() ),
            new OrientedFace(Vector3Int.zero, FaceDir.Forward, new FaceDetails() ),
            new OrientedFace(Vector3Int.zero, FaceDir.Back, new FaceDetails() ),
        };

        /// <summary>
        /// A list of cells that this tile occupies.
        /// For a normal cube tile, this just contains Vector3Int.zero, but it will be more for "big" tiles.
        /// It is recommended you only edit this via the Unity Editor, or <see cref="AddOffset(Vector3Int)"/> and <see cref="RemoveOffset(Vector3Int)"/>
        /// </summary>
        public List<Vector3Int> offsets = new List<Vector3Int>()
        {
            Vector3Int.zero
        };

        /// <summary>
        /// Where the center of tile is.
        /// For big tils that occupy more than one cell, it's the center of the cell with offset (0, 0, 0).
        /// </summary>
        public Vector3 center = Vector3.zero;

        /// <summary>
        /// The size of one cell in the tile.
        /// NB: This field is only used in the Editor - you must set <see cref="TesseraGenerator.tileSize"/> to match.
        /// </summary>
        public Vector3 tileSize = Vector3.one;

        /// <summary>
        /// If true, when generating, all 4 rotations of the tile will be used.
        /// </summary>
        public bool rotatable = true;

        /// <summary>
        /// If true, when generating, reflections in the x-axis will be used.
        /// </summary>
        public bool reflectable = true;

        /// <summary>
        /// If set, when being instantiated by a Generator, only children will get constructed.
        /// If there are no children, then this effectively disables the tile from instantiation.
        /// </summary>
        public bool instantiateChildrenOnly = false;

        /// <summary>
        /// Finds the face details for a cell with a given offeset.
        /// </summary>
        public FaceDetails Get(Vector3Int offset, FaceDir faceDir)
        {
            return faceDetails.Single(x => x.offset == offset && x.faceDir == faceDir).faceDetails;
        }

        /// <summary>
        /// Finds the face details for a cell with a given offeset.
        /// </summary>
        public bool TryGet(Vector3Int offset, FaceDir faceDir, out FaceDetails details)
        {
            details = faceDetails.SingleOrDefault(x => x.offset == offset && x.faceDir == faceDir).faceDetails;
            return details != null;
        }

        /// <summary>
        /// Configures the tile as a "big" tile that occupies several cells.
        /// Keeps <see cref="offsets"/> and <see cref="faceDetails"/> in sync.
        /// </summary>
        public void AddOffset(Vector3Int o)
        {
            if (offsets.Contains(o))
                return;
            offsets.Add(o);
            foreach (FaceDir faceDir in Enum.GetValues(typeof(FaceDir)))
            {
                var o2 = o + faceDir.Forward();
                if (offsets.Contains(o2))
                {
                    faceDetails.RemoveAll(x => x.offset == o2 && x.faceDir == faceDir.Inverted());
                }
                else
                {
                    faceDetails.Add(new OrientedFace(o, faceDir, new FaceDetails()));
                }
            }
        }

        /// <summary>
        /// Configures the tile as a "big" tile that occupies several cells.
        /// Keeps <see cref="offsets"/> and <see cref="faceDetails"/> in sync.
        /// </summary>
        public void RemoveOffset(Vector3Int o)
        {
            if (!offsets.Contains(o))
                return;
            offsets.Remove(o);
            foreach (FaceDir faceDir in Enum.GetValues(typeof(FaceDir)))
            {
                var o2 = o + faceDir.Forward();
                if (offsets.Contains(o2))
                {
                    faceDetails.Add(new OrientedFace(o2, faceDir.Inverted(), new FaceDetails()));
                }
                else
                {
                    faceDetails.RemoveAll(x => x.offset == o && x.faceDir == faceDir);
                }
            }
        }

        public BoundsInt GetBounds()
        {
            var min = offsets[0];
            var max = min;
            foreach (var o in offsets)
            {
                min = Vector3Int.Min(min, o);
                max = Vector3Int.Max(max, o);
            }

            return new BoundsInt(min, max - min);
        }
    }
}