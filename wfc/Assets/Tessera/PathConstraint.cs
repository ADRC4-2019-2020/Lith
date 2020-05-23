using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Forces a network of tiles to connect with each other, so there is always a complete path between them.
    /// Two tiles connect along the path if:
    /// * Both tiles are in <see cref="pathTiles"/> (if <see cref="hasPathTiles"/> set); and
    /// * The central color of the sides of the tiles leading to each other are in <see cref="pathColors"/> (if <see cref="pathColors"/> set)
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    [AddComponentMenu("Tessera/Path Constraint", 21)]
    [RequireComponent(typeof(TesseraGenerator))]
    public class PathConstraint : TesseraConstraint
    {
        /// <summary>
        /// If set, <see cref="pathColors"/> is used to determine path tiles and sides.
        /// </summary>
        public bool hasPathTiles;

        /// <summary>
        /// If <see cref="hasPathTiles"/>, this set filters tiles that the path can connect through.
        /// </summary>
        public List<TesseraTile> pathTiles = new List<TesseraTile>();

        /// <summary>
        /// If set, <see cref="pathColors"/> is used to determine path tiles and sides.
        /// </summary>
        public bool hasPathColors;

        /// <summary>
        /// If <see cref="hasPathColors"/>, this set filters tiles that the path can connect through.
        /// </summary>
        public List<int> pathColors = new List<int>();

        internal override ITileConstraint GetTileConstraint(AdjacentModel model)
        {
            if (hasPathColors)
            {
                var colorSet = new HashSet<int>(pathColors);
                var pathTilesSet = new HashSet<TesseraTile>(pathTiles);
                var generator = GetComponent<TesseraGenerator>();
                TesseraGeneratorHelper.SetupModelFromTiles(generator.tiles, out var allTiles, out var internalAdjacencies, out var tilesByFaceDir);
                // All internal connections are valid exits
                var internalDirs = internalAdjacencies
                    .Concat(internalAdjacencies.Select(t => (t.Item2, t.Item1, DirectionSet.Cartesian3d.Inverse(t.Item3))))
                    .Where(x => !hasPathTiles || pathTilesSet.Contains(((ModelTile)x.Item1.Value).Tile))
                    .ToLookup(x => x.Item1, x => x.Item3);
                // Extneral connections are valid exits only if the color in the center of the face matches
                var externalDirs = tilesByFaceDir
                    .SelectMany(kv => kv.Value.Select(t => new { FaceDir = kv.Key, FaceDetails = t.Item1, Tile = t.Item2 }))
                    .Where(x => !hasPathTiles || pathTilesSet.Contains(((ModelTile)x.Tile.Value).Tile))
                    .Where(x => colorSet.Contains(x.FaceDetails.center))
                    .ToLookup(x => x.Tile, x => x.FaceDir.ToDirection());
                var exits = internalDirs.Select(x => x.Key).Union(externalDirs.Select(x => x.Key))
                    .ToDictionary(x => x, x => (ISet<Direction>)new HashSet<Direction>(internalDirs[x].Concat(externalDirs[x])));
                return new DeBroglie.Constraints.EdgedPathConstraint(exits);
            }
            else if (hasPathTiles)
            {
                var actualPathTiles = new HashSet<Tile>(GetModelTiles(pathTiles).Select(x => new Tile(x)));
                return new DeBroglie.Constraints.PathConstraint(actualPathTiles);
            }
            else
            {
                throw new Exception("One of hasColors or hasPathTiles must be set for PathConstraints");
            }
        }
    }
}
