using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

using static Tessera.GeometryUtils;

namespace Tessera
{
    /**
     * Holds the actual implementation of the genreation
     * In a format that is appropriate to run in threads.
     * I.e. all relevant GameObject properties have been copied into POCOs
     */
    internal class TesseraGeneratorHelper
    {
        // Configuration.
        // This is all loaded from TesseraGenerator
        TesseraPalette palette;
        AdjacentModel model;
        List<(Tile, float)> allTiles;
        List<TesseraInitialConstraint> initialConstraints;
        List<ITileConstraint> constraints;
        Dictionary<FaceDir, List<(FaceDetails, Tile)>> tilesByFaceDir;
        Vector3Int size;
        bool backtrack;
        TesseraInitialConstraint skyBox;
        Action<string, float> progress;
        Action<ITopoArray<ISet<ModelTile>>> progressTiles;
        XoRoRNG xororng;
        CancellationToken ct;

        // State. This is intialized in Setup()
        DeBroglie.Resolution lastStatus;
        TilePropagator propagator;

        // TODO: Record some timings here

        public TesseraGeneratorHelper(
            TesseraPalette palette,
            AdjacentModel model,
            List<(Tile, float)> allTiles,
            List<TesseraInitialConstraint> initialConstraints,
            List<ITileConstraint> constraints,
            Dictionary<FaceDir, List<(FaceDetails, Tile)>> tilesByFaceDir,
            Vector3Int size,
            bool backtrack,
            TesseraInitialConstraint skyBox,
            Action<string, float> progress,
            Action<ITopoArray<ISet<ModelTile>>> progressTiles,
            XoRoRNG xororng,
            CancellationToken ct)
        {
            this.palette = palette;
            this.model = model;
            this.allTiles = allTiles;
            this.initialConstraints = initialConstraints;
            this.constraints = constraints;
            this.tilesByFaceDir = tilesByFaceDir;
            this.size = size;
            this.backtrack = backtrack;
            this.skyBox = skyBox;
            this.progress = progress;
            this.progressTiles = progressTiles;
            this.xororng = xororng;
            this.ct = ct;
        }

        public TilePropagator Propagator => propagator;

        public void SetupAndRun()
        {
            Setup();
            Run();
        }


        // Construst the propagator
        internal void Setup()
        {
            progress?.Invoke("Initializing", 0.0f);

            var topology = GetTopology();

            var options = new TilePropagatorOptions
            {
                BackTrackDepth = backtrack ? -1 : 0,
                RandomDouble = xororng.NextDouble,
                Constraints = constraints.ToArray(),
            };
            propagator = new TilePropagator(model, topology, options);
            lastStatus = DeBroglie.Resolution.Undecided;

            CheckStatus("Failed to initialize propagator");

            ApplyInitialConstraintsAndSkybox();
            BanBigTiles();
        }

        GridTopology GetTopology()
        {
            // Use existing objects as mask
            var mask = Enumerable.Range(0, size.x * size.y * size.z).Select(x => true).ToArray();
            foreach (var ic in initialConstraints)
            {
                foreach (var offset in ic.offsets)
                {
                    var p2 = ic.cell + ic.rotator.Multiply(offset);
                    if (InBounds(p2, size))
                        mask[GetMaskIndex(p2, size)] = false;
                }
            }

            var t2 = DateTime.Now;

            return new GridTopology(size.x, size.y, size.z, false).WithMask(mask);
        }


        private void Run()
        {
            CheckStatus("Propagator is not ready to run");


            {
                var lastProgress = DateTime.Now;
                var progressResolution = TimeSpan.FromSeconds(0.1);
                while (propagator.Status == DeBroglie.Resolution.Undecided)
                {
                    ct.ThrowIfCancellationRequested();
                    if (lastProgress + progressResolution < DateTime.Now)
                    {
                        lastProgress = DateTime.Now;
                        if (progress != null)
                        {
                            progress("Generating", (float)propagator.GetProgress());
                        }
                        if (progressTiles != null)
                        {
                            progressTiles(propagator.ToValueSets<ModelTile>());
                        }
                    }
                    propagator.Step();
                }
            }
        }


        private void ApplyInitialConstraintsAndSkybox()
        {
            var topology = propagator.Topology;
            var mask = topology.Mask;

            // Use existing objects as initial constraint
            var isConstrained = new HashSet<(Vector3Int, FaceDir)>();
            void FaceConstrain(Vector3Int p, FaceDir faceDir, FaceDetails faceDetails)
            {
                var faceDirForward = faceDir.Forward();
                var p1 = p;
                var p2 = p1 + faceDirForward;
                if (InBounds(p2, size) && mask[GetMaskIndex(p2, size)] && isConstrained.Add((p2, faceDir.Inverted())))
                {
                    //Debug.Log(("face constraint", p, faceDir, faceDirForward, faceDetails));

                    var matchingTiles = tilesByFaceDir[faceDir.Inverted()]
                        .Where(x => palette.Match(faceDetails, x.Item1))
                        .Select(x => x.Item2)
                        .ToList();

                    propagator.Select(p2.x, p2.y, p2.z, matchingTiles);
                }
            }

            var t4 = DateTime.Now;

            foreach (var ic in initialConstraints)
            {
                foreach (var (offset, faceDir, faceDetails) in ic.faceDetails)
                {
                    var rotatedOffset = ic.rotator.Multiply(offset);
                    var (rotatedFaceDir, rotatedFaceDetails) = ApplyRotator(faceDir, faceDetails, ic.rotator);
                    FaceConstrain(ic.cell + rotatedOffset, rotatedFaceDir, rotatedFaceDetails);
                }
            }
            CheckStatus("Contradiction after setting initial constraints.");


            // Apply skybox (if any)
            if (skyBox != null)
            {
                var reflectX = new Rotation(0, true);
                for (var x = 0; x < size.x; x++)
                {
                    for (var y = 0; y < size.y; y++)
                    {
                        var forwardFaceDetails = skyBox.faceDetails.First(f => f.faceDir == FaceDir.Forward);
                        var backFaceDetails = skyBox.faceDetails.First(f => f.faceDir == FaceDir.Back);
                        FaceConstrain(new Vector3Int(x, y, -1), FaceDir.Forward, backFaceDetails.faceDetails.RotateBy(reflectX));
                        FaceConstrain(new Vector3Int(x, y, size.z), FaceDir.Back, forwardFaceDetails.faceDetails.RotateBy(reflectX));
                    }
                    for (var z = 0; z < size.z; z++)
                    {
                        var upFaceDetails = skyBox.faceDetails.First(f => f.faceDir == FaceDir.Up);
                        var downFaceDetails = skyBox.faceDetails.First(f => f.faceDir == FaceDir.Down);
                        FaceConstrain(new Vector3Int(x, -1, z), FaceDir.Up, downFaceDetails.faceDetails.RotateBy(reflectX));
                        FaceConstrain(new Vector3Int(x, size.y, z), FaceDir.Down, upFaceDetails.faceDetails.RotateBy(reflectX));
                    }
                }
                for (var y = 0; y < size.y; y++)
                {
                    for (var z = 0; z < size.z; z++)
                    {
                        var rightFaceDetails = skyBox.faceDetails.First(f => f.faceDir == FaceDir.Right);
                        var leftFaceDetails = skyBox.faceDetails.First(f => f.faceDir == FaceDir.Left);
                        FaceConstrain(new Vector3Int(-1, y, z), FaceDir.Right, leftFaceDetails.faceDetails.RotateBy(reflectX));
                        FaceConstrain(new Vector3Int(size.x, y, z), FaceDir.Left, rightFaceDetails.faceDetails.RotateBy(reflectX));
                    }
                }
            }
            CheckStatus("Contradiction after setting initial constraints and skybox.");
        }

        private void BanBigTiles()
        {
            var topology = propagator.Topology;
            var mask = topology.Mask;

            // Ban big tiles from overlapping edges and placed tiles
            foreach (var modelTile in model.Tiles)
            {
                var mt = (ModelTile)modelTile.Value;
                foreach (FaceDir faceDir in Enum.GetValues(typeof(FaceDir)))
                {
                    var offset2 = mt.Offset + faceDir.Forward();
                    if (mt.Tile.offsets.Contains(offset2))
                    {
                        var worldOffset = Rotate(mt.Rotation, faceDir.Forward());
                        for (var x = 0; x < topology.Width; x++)
                        {
                            for (var y = 0; y < topology.Height; y++)
                            {
                                for (var z = 0; z < topology.Depth; z++)
                                {
                                    var p2 = new Vector3Int(x, y, z) + Rotate(mt.Rotation, faceDir.Forward());
                                    if (!InBounds(p2, size) || !mask[GetMaskIndex(p2, size)])
                                    {
                                        propagator.Ban(x, y, z, modelTile);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            CheckStatus("Contradiction after removing big tiles overlapping edges.");
        }

        // TODO: This should return via TesseraCompletion rather than logging
        private void CheckStatus(string s)
        {
            if (lastStatus != DeBroglie.Resolution.Contradiction && propagator.Status == DeBroglie.Resolution.Contradiction)
            {
                lastStatus = propagator.Status;
                Debug.LogWarning(s);
            }
        }

        /// <summary>
        /// Turns the generator's configuration into a format consumable by DeBroglie.
        /// </summary>
        /// <param name="allTiles">All the tiles in the model</param>
        /// <param name="internalAdjacencies">List of tile pairs needed for big tiles</param>
        /// <param name="tilesByFaceDir">All outward facing faces, grouped by face direction.</param>
        internal static void SetupModelFromTiles(
            List<TileEntry> tiles,
            out List<(Tile, float)> allTiles,
            out List<(Tile, Tile, Direction)> internalAdjacencies,
            out Dictionary<FaceDir, List<(FaceDetails, Tile)>> tilesByFaceDir)
        {
            allTiles = new List<(Tile, float)>();
            internalAdjacencies = new List<(Tile, Tile, Direction)>();

            tilesByFaceDir = new Dictionary<FaceDir, List<(FaceDetails, Tile)>>();
            tilesByFaceDir[FaceDir.Left] = new List<(FaceDetails, Tile)>();
            tilesByFaceDir[FaceDir.Right] = new List<(FaceDetails, Tile)>();
            tilesByFaceDir[FaceDir.Up] = new List<(FaceDetails, Tile)>();
            tilesByFaceDir[FaceDir.Down] = new List<(FaceDetails, Tile)>();
            tilesByFaceDir[FaceDir.Forward] = new List<(FaceDetails, Tile)>();
            tilesByFaceDir[FaceDir.Back] = new List<(FaceDetails, Tile)>();

            var tileCosts = new Dictionary<TesseraTile, int>();

            var rg = new RotationGroup(4, true);

            if (tiles == null || tiles.Count == 0)
            {
                throw new Exception("Cannot run generator with zero tiles configured.");
            }

            // Generate all tiles, and extract their face details
            foreach (var tileEntry in tiles)
            {
                var tile = tileEntry.tile;

                if (tile == null)
                    continue;
                if (!IsContiguous(tile))
                {
                    Debug.LogWarning($"Cannot use {tile} as it is not contiguous");
                    continue;
                }

                foreach (var rot in rg)
                {

                    if (!tile.rotatable && rot.RotateCw != 0)
                        continue;
                    if (!tile.reflectable && rot.ReflectX)
                        continue;

                    // Set up internal connections
                    foreach (var offset in tile.offsets)
                    {
                        var modelTile = new Tile(new ModelTile(tile, rot, offset));

                        allTiles.Add((modelTile, tileEntry.weight / tile.offsets.Count));

                        foreach (FaceDir faceDir in new[] { FaceDir.Right, FaceDir.Forward, FaceDir.Up })
                        {
                            var offset2 = offset + faceDir.Forward();
                            if (tile.offsets.Contains(offset2))
                            {
                                var modelTile2 = new Tile(new ModelTile(tile, rot, offset2));

                                var rdir = faceDir.RotateBy(rot);

                                internalAdjacencies.Add((modelTile, modelTile2, rdir.ToDirection()));
                                //Debug.Log(("internal", modelTile, modelTile2, rdir.ToDirection()));
                            }
                        }
                    }

                    // Set up external connections
                    foreach (var (offset, faceDir, faceDetails) in tile.faceDetails)
                    {
                        var modelTile = new Tile(new ModelTile(tile, rot, offset));

                        var rdir = faceDir.RotateBy(rot);
                        FaceDetails rFaceDetails = faceDetails.RotateBy(rdir, rot);
                        tilesByFaceDir[rdir].Add((rFaceDetails, modelTile));
                        //Debug.Log(("external", rdir, rFaceDetails, modelTile));
                    }
                }
            }
        }

        private static bool IsContiguous(TesseraTile tile)
        {
            if (tile.offsets.Count == 1)
                return true;

            // Floodfill offset
            var offsets = new HashSet<Vector3Int>(tile.offsets);
            var toRemove = new Stack<Vector3Int>();
            toRemove.Push(offsets.First());
            while (toRemove.Count > 0)
            {
                var o = toRemove.Pop();
                offsets.Remove(o);

                foreach (FaceDir faceDir in Enum.GetValues(typeof(FaceDir)))
                {
                    var o2 = o + faceDir.Forward();
                    if (offsets.Contains(o2))
                    {
                        toRemove.Push(o2);
                    }
                }
            }

            return offsets.Count == 0;
        }
    }
}
