using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Trackers;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Ensures that the generation is symmetric when x-axis mirrored.
    /// If there are any tile constraints, they will not be mirrored.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    [AddComponentMenu("Tessera/Mirror Constraint", 20)]
    [RequireComponent(typeof(TesseraGenerator))]
    public class MirrorConstraint : TesseraConstraint
    {
        /// <summary>
        /// If set, <see cref="symmetricTilesX"/> and <see cref="symmetricTilesZ"/> is used to determine symmetric tiles.
        /// Otherwise, they are automatically detected.
        /// </summary>
        public bool hasSymmetricTiles;

        /// <summary>
        /// If <see cref="hasSymmetricTiles"/>, this set specifies tiles that look the same before and after x-reflection.
        /// If <see cref="hasSymmetricTiles"/> is not set, this list is automatically inferred by inspecting the tile's paint.
        /// </summary>
        public List<TesseraTile> symmetricTilesX = new List<TesseraTile>();

        /// <summary>
        /// If <see cref="hasSymmetricTiles"/>, this set specifies tiles that look the same before and after z-reflection.
        /// If <see cref="hasSymmetricTiles"/> is not set, this list is automatically inferred by inspecting the tile's paint.
        /// </summary>
        public List<TesseraTile> symmetricTilesZ = new List<TesseraTile>();

        private bool IsSymmetricX(TesseraTile tile)
        {
            foreach (var of in tile.faceDetails)
            {
                var bounds = tile.GetBounds();
                var reflectedDir = of.faceDir == FaceDir.Left ? FaceDir.Right : of.faceDir == FaceDir.Right ? FaceDir.Left : of.faceDir;
                var reflectedOffset = new Vector3Int(bounds.xMin + bounds.xMax - of.offset.x, of.offset.y, of.offset.z);
                if (!tile.TryGet(reflectedOffset, reflectedDir, out var reflectedFaceDetails))
                    return false;
                if (!ReflectedEquals(of.faceDetails, reflectedFaceDetails))
                    return false;
            }
            return true;
        }

        private bool IsSymmetricZ(TesseraTile tile)
        {
            foreach (var of in tile.faceDetails)
            {
                var bounds = tile.GetBounds();
                var reflectedDir = of.faceDir == FaceDir.Forward ? FaceDir.Back: of.faceDir == FaceDir.Back ? FaceDir.Forward : of.faceDir;
                var reflectedOffset = new Vector3Int(of.offset.x, of.offset.y, bounds.zMin + bounds.zMax - of.offset.z);
                if (!tile.TryGet(reflectedOffset, reflectedDir, out var reflectedFaceDetails))
                    return false;
                if (!ReflectedEquals(of.faceDetails, reflectedFaceDetails))
                    return false;
            }
            return true;
        }

        public static bool ReflectedEquals(FaceDetails a, FaceDetails b)
        {
            return (a.topLeft == b.topRight) &&
                (a.top == b.top) &&
                (a.topRight == b.topLeft) &&
                (a.left == b.right) &&
                (a.center == b.center) &&
                (a.right == b.left) &&
                (a.bottomLeft == b.bottomRight) &&
                (a.bottom == b.bottom) &&
                (a.bottomRight == b.bottomLeft);
        }

        private IEnumerable<TesseraTile> GetSymmetricTilesX()
        {
            var generator = GetComponent<TesseraGenerator>();

            return generator.tiles.Select(x => x.tile).Where(IsSymmetricX).ToList();
        }

        private IEnumerable<TesseraTile> GetSymmetricTilesZ()
        {
            var generator = GetComponent<TesseraGenerator>();

            return generator.tiles.Select(x => x.tile).Where(IsSymmetricZ).ToList();
        }

        public void SetSymmetricTiles()
        {
            symmetricTilesX = GetSymmetricTilesX().ToList();
            symmetricTilesZ = GetSymmetricTilesZ().ToList();
        }

        internal override ITileConstraint GetTileConstraint(AdjacentModel model)
        {
            var actualSymmetricTilesX = new HashSet<TesseraTile>(hasSymmetricTiles ? symmetricTilesX : GetSymmetricTilesX());
            var actualSymmetricTilesZ = new HashSet<TesseraTile>(hasSymmetricTiles ? symmetricTilesZ : GetSymmetricTilesZ());

            // TODO: Not working in demo
            // TODO: Symmetric definition doesn't work with rotated tiles!

            var trb = new TileRotationBuilder(4, true, TileRotationTreatment.Missing);
            foreach (var tile in model.Tiles)
            {
                var modelTile = (ModelTile)tile.Value;
                if((modelTile.Rotation.RotateCw % 180 == 0 ? actualSymmetricTilesX :  actualSymmetricTilesZ).Contains(modelTile.Tile))
                {
                    var r = new Rotation(0, true);
                    var bounds = modelTile.Tile.GetBounds();
                    var modelTile2 = new ModelTile
                    {
                        Tile = modelTile.Tile,
                        Offset = modelTile.Offset,
                        Rotation = modelTile.Rotation,
                    };
                    trb.Add(tile, r, new Tile(modelTile2));
                }
                else if (modelTile.Tile.reflectable)
                {
                    var r = new Rotation(0, true);
                    var modelTile2 = new ModelTile
                    {
                        Tile = modelTile.Tile,
                        Offset = modelTile.Offset,
                        Rotation = modelTile.Rotation * r,
                    };
                    trb.Add(tile, r, new Tile(modelTile2));
                }
            }

            return new DeBroglie.Constraints.MirrorXConstraint
            {
                TileRotation = trb.Build(),
            };
        }
    }
}
