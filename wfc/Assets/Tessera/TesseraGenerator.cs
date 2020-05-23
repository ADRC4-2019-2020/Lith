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

namespace Tessera
{

    /// <summary>
    /// GameObjects with this behaviour contain utilities to generate tile based levels using Wave Function Collapse (WFC).
    /// Call <see cref="Generate"/> or <see cref="StartGenerate"/> to run.
    /// The generation takes the following steps:
    /// * Inspect the tiles in <see cref="tiles"/> and work out how they rotate and connect to each other.
    /// * Setup any initial constraints that fix parts of the generation (<see cref="searchInitialConstraints"/> and <see cref="initialConstraints"/>).
    /// * Fix the boundary of the generation if <see cref="skyBox"/> is set.
    /// * Generate a set of tile instances that fits the above tiles and constraints.
    /// * Optionally <see cref="retries"/> or <see cref="backtrack"/>.
    /// * Instantiates the tile instances.
    /// </summary>
    [AddComponentMenu("Tessera/Generator")]
    public class TesseraGenerator : MonoBehaviour
    {


        [SerializeField]
        private Vector3Int m_size = new Vector3Int(10, 1, 10);

        [SerializeField]
        private Vector3 m_center = Vector3.zero;

        /// <summary>
        /// The size of the generator area, counting in cells each of size <see cref="tileSize"/>.
        /// </summary>
        public Vector3Int size
        {
            get { return m_size; }
            set
            {
                m_size = value;
            }
        }

        /// <summary>
        /// The local position of the center of the area to generate.
        /// </summary>
        public Vector3 center
        {
            get
            {
                return m_center;
            }
            set
            {
                m_center = value;
            }
        }

        /// <summary>
        /// The area of generation.
        /// Setting this will cause the size to be rounded to a multiple of <see cref="tileSize"/>
        /// </summary>
        public Bounds bounds
        {
            get
            {
                return new Bounds(m_center, Vector3.Scale(tileSize, m_size));
            }
            set
            {
                m_center = value.center;
                m_size = new Vector3Int(
                    Math.Max(1, (int)Math.Round(value.size.x / tileSize.x)),
                    Math.Max(1, (int)Math.Round(value.size.y / tileSize.y)),
                    Math.Max(1, (int)Math.Round(value.size.z / tileSize.z))
                    );
            }
        }

        /// <summary>
        /// The list of tiles eligable for generation.
        /// </summary>
        public List<TileEntry> tiles;

        /// <summary>
        /// The stride between each cell in the generation.
        /// "big" tiles may occupy a multiple of this tile size.
        /// </summary>
        public Vector3 tileSize = Vector3.one;

        /// <summary>
        /// If set, backtracking will be used during generation.
        /// Backtracking can find solutions that would otherwise be failures,
        /// but can take a long time.
        /// </summary>
        public bool backtrack = true;

        /// <summary>
        /// If backtracking is off, how many times to retry generation if a solution
        /// cannot be found.
        /// </summary>
        public int retries = 5;

        /// <summary>
        /// If set, this tile is used to define extra initial constraints for the boundary.
        /// </summary>
        public TesseraTile skyBox = null;

        /// <summary>
        /// If true, then active tiles in the scene will be taken as initial constraints.
        /// If false, then <see cref="initialConstraints"/> is used instead.
        /// </summary>
        public bool searchInitialConstraints = true;

        /// <summary>
        /// The initial constraints to be used, if <see cref="searchInitialConstraints"/> is false.
        /// This can be filled with objects returned from the GetInitialConstraint methods.
        /// </summary>
        public List<TesseraInitialConstraint> initialConstraints = null;

        /// <summary>
        /// Inherited from the first tile in <see cref="tiles"/>.
        /// </summary>
        public TesseraPalette palette => tiles.FirstOrDefault()?.tile?.palette ?? TesseraPalette.defaultPalette;


        /// <summary>
        /// Synchronously runs the generation process described in the class docs.
        /// </summary>
        /// <param name="onCreate">Called for each newly generated tile. By default, they are Instantiated in the scene.</param>
        public TesseraCompletion Generate(TesseraGenerateOptions options = null)
        {
            var e = StartGenerate(options);
            while (e.MoveNext())
            {
                var a = e.Current;
                if (a is TesseraCompletion tc)
                    return tc;
            }

            throw new Exception("Unreachable code.");
        }

        internal TesseraGeneratorHelper CreateTesseraGeneratorHelper(TesseraGenerateOptions options = null)
        {
            options = options ?? new TesseraGenerateOptions();
            var progress = options.progress;

            var seed = options.seed == 0 ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : options.seed;

            var xororng = new XoRoRNG(seed);

            Validate();

            TesseraGeneratorHelper.SetupModelFromTiles(tiles, out var allTiles, out var internalAdjacencies, out var tilesByFaceDir);

            var model = new AdjacentModel(DirectionSet.Cartesian3d);

            foreach (var (tile, frequency) in allTiles)
            {
                model.SetFrequency(tile, frequency);
            }

            foreach (var (tile1, tile2, d) in internalAdjacencies)
            {
                model.AddAdjacency(tile1, tile2, d);
            }

            // Generate adjacencies
            AddAdjacency(palette, model, Direction.XPlus, tilesByFaceDir[FaceDir.Right], tilesByFaceDir[FaceDir.Left]);
            AddAdjacency(palette, model, Direction.YPlus, tilesByFaceDir[FaceDir.Up], tilesByFaceDir[FaceDir.Down]);
            AddAdjacency(palette, model, Direction.ZPlus, tilesByFaceDir[FaceDir.Forward], tilesByFaceDir[FaceDir.Back]);

            var initialConstraints = searchInitialConstraints ? GetInitialConstraints() : this.initialConstraints;

            var constraints = GetTileConstraints(model);

            var actualSkyBox = skyBox == null ? null : new TesseraInitialConstraint
            {
                faceDetails = skyBox.faceDetails,
                offsets = skyBox.offsets,
            };

            return new TesseraGeneratorHelper(
                            palette,
                            model,
                            allTiles,
                            initialConstraints,
                            constraints,
                            tilesByFaceDir,
                            size,
                            backtrack,
                            actualSkyBox,
                            progress,
                            null,
                            xororng,
                            options.cancellationToken);
        }


        /// <summary>
        /// Asynchronously runs the generation process described in the class docs, for use with StartCoroutine.
        /// </summary>
        /// <remarks>The default instantiation is still synchronous, so this can still cause frame glitches unless you override onCreate.</remarks>
        /// <param name="onCreate"></param>
        public IEnumerator StartGenerate(TesseraGenerateOptions options = null)
        {
            options = options ?? new TesseraGenerateOptions();

            var coregenerator = CreateTesseraGeneratorHelper(options);


            for (var r = 0; r < retries; r++)
            {
                TilePropagator propagator;
                TilePropagator Run()
                {
                    coregenerator.SetupAndRun();
                    return coregenerator.Propagator;
                }

                if (options.multithreaded)
                {
                    var runTask = Task.Run(Run,  options.cancellationToken);

                    while (!runTask.IsCompleted)
                        yield return null;

                    options.cancellationToken.ThrowIfCancellationRequested();

                    propagator = runTask.Result;
                }
                else
                {
                    propagator = Run();
                }

                var status = propagator.Status;

                var contradictionTile = new ModelTile {};

                var result = propagator.ToValueArray<ModelTile?>(contradiction: contradictionTile);


                if (status == DeBroglie.Resolution.Contradiction)
                {
                    if (r < retries - 1)
                    {
                        continue;
                    }
                }


                var completion = new TesseraCompletion();
                completion.retries = r;
                completion.backtrackCount = propagator.BacktrackCount;
                completion.success = status == DeBroglie.Resolution.Decided;
                completion.tileInstances = GetTesseraTileInstances(result).ToList();
                completion.contradictionLocation = completion.success ? null : GetContradictionLocation(result);

                if (options.onComplete != null)
                {
                    options.onComplete(completion);
                }
                else
                {
                    HandleComplete(options, completion);
                }

                yield return completion;

                // Exit retries
                break;
            }
        }

        /// <summary>
        /// Checks tiles are consistently setup
        /// </summary>
        internal void Validate()
        {
            var allTiles = tiles.Select(x => x.tile).Where(x => x != null);
            var missized = allTiles.Where(x => x.tileSize != tileSize).ToList();
            if (missized.Count > 0)
            {
                Debug.LogWarning($"Some tiles do not have the same tileSize as the generator, {tileSize}, this can cause unexpected behaviour.\n" +
                    "NB: Big tiles should still share the same value of tileSize\n" +
                    "Affected tiles:\n" +
                    string.Join("\n", missized)
                    );
            }
        }

        /// <summary>
        /// Converts generator constraints into a format suitable for DeBroglie.
        /// </summary>
        private List<ITileConstraint> GetTileConstraints(AdjacentModel model)
        {
            var l = new List<ITileConstraint>();
            foreach (var constraintComponent in GetComponents<TesseraConstraint>())
            {
                var constraint = constraintComponent.GetTileConstraint(model);
                l.Add(constraint);
            }
            return l;
        }

        /// <summary>
        /// Converts from DeBroglie's array format back to Tessera's.
        /// </summary>
        internal IEnumerable<TesseraTileInstance> GetTesseraTileInstances(ITopoArray<ModelTile?> result)
        {
            var topology = result.Topology;
            var mask = topology.Mask;

            var empty = mask.ToArray();
            for (var x = 0; x < topology.Width; x++)
            {
                for (var y = 0; y < topology.Height; y++)
                {
                    for (var z = 0; z < topology.Depth; z++)
                    {
                        var p = new Vector3Int(x, y, z);
                        // Skip if already filled
                        if (!empty[GetMaskIndex(p)])
                            continue;
                        var modelTile = result.Get(x, y, z);
                        if (modelTile == null)
                            continue;
                        var rot = modelTile.Value.Rotation;
                        var tile = modelTile.Value.Tile;
                        if (tile == null)
                            continue;

                        var ti = GetTesseraTileInstance(x, y, z, modelTile.Value);

                        // Fill locations
                        foreach (var p2 in ti.Cells)
                        {
                            if (InBounds(p2))
                                empty[GetMaskIndex(p2)] = false;
                        }

                        if (ti != null)
                        {
                            yield return ti;
                        }
                    }
                }
            }
        }

        internal TesseraTileInstance GetTesseraTileInstance(int x, int y, int z, ModelTile modelTile)
        {
            var rot = modelTile.Rotation;
            var tile = modelTile.Tile;

            var p = new Vector3Int(x, y, z);

            var localRotation = Quaternion.Euler(0, -rot.RotateCw, 0);
            var localPosition = GetLocalPosition(p) - GeometryUtils.Rotate(rot, tile.center + Vector3.Scale(tileSize, modelTile.Offset));
            var localScale = rot.ReflectX ? new Vector3(-1, 1, 1) : new Vector3(1, 1, 1);

            var worldPosition = gameObject.transform.TransformPoint(localPosition);
            var worldRotation = gameObject.transform.rotation * localRotation;
            var worldScale = (gameObject.transform.localToWorldMatrix * Matrix4x4.TRS(localPosition, localRotation, localScale)).lossyScale;

            var newParent = gameObject.transform;

            var cells = tile.offsets.Select(offset => p + GeometryUtils.Rotate(rot, offset - modelTile.Offset)).ToArray();

            return new TesseraTileInstance
            {
                Tile = tile,
                Position = worldPosition,
                Rotation = worldRotation,
                LossyScale = worldScale,
                LocalPosition = localPosition,
                LocalRotation = localRotation,
                LocalScale = localScale,
#pragma warning disable CS0618 // Type or member is obsolete
                IntPosition = new Vector3Int(x, y, z),
#pragma warning restore CS0618 // Type or member is obsolete
                Cells = cells,
            };
        }


        private Vector3Int? GetContradictionLocation(ITopoArray<ModelTile?> result)
        {
            var topology = result.Topology;
            var mask = topology.Mask;

            var empty = mask.ToArray();
            for (var x = 0; x < topology.Width; x++)
            {
                for (var y = 0; y < topology.Height; y++)
                {
                    for (var z = 0; z < topology.Depth; z++)
                    {
                        var p = new Vector3Int(x, y, z);
                        // Skip if already filled
                        if (!empty[GetMaskIndex(p)])
                            continue;
                        var modelTile = result.Get(x, y, z);
                        if (modelTile == null)
                            continue;
                        var tile = modelTile.Value.Tile;
                        if (tile == null)
                        {
                            return new Vector3Int(x, y, z);
                        }
                    }
                }
            }

            return null;
        }

        private void HandleComplete(TesseraGenerateOptions options, TesseraCompletion completion)
        {
            if(!completion.success)
            {
                if (completion.contradictionLocation != null)
                {
                    var loc = completion.contradictionLocation;
                    Debug.LogError($"Failed to complete generation, issue at tile {loc}");
                }
                else
                {
                    Debug.LogError("Failed to complete generation");
                }
                return;
            }

            ITesseraTileOutput to = null;
            if(options.onCreate != null)
            {
                to = new ForEachOutput(options.onCreate);
            }
            else
            {
                to = GetComponent<ITesseraTileOutput>() ?? new InstantiateOutput(transform);
            }

            to.UpdateTiles(completion.tileInstances);
        }

        private int GetMaskIndex(Vector3Int p)
        {
            return GeometryUtils.GetMaskIndex(p, size);
        }

        /// <summary>
        /// Returns the cell, and a rotator for a tile placed near the generator
        /// A rotator takes vectors in the tile local space, and returns them in generator local space.
        /// It's always an distance presering transform (so it's always one of the symmetries of a cube).
        /// 
        /// NB: The cell returned corresponds to offset (0,0,0). The tile may not actually occupy that offset.
        /// </summary>
        internal bool GetCell(
            TesseraTile tile,
            Matrix4x4 tileLocalToWorldMatrix,
            out Vector3Int cell,
            out MatrixInt3x3 rotator)
        {
            return GeometryUtils.GetCell(
                transform,
                center,
                tileSize,
                size,
                tile, tileLocalToWorldMatrix,
                out cell,
                out rotator);
        }

        /// <summary>
        /// Returns the center of the cell
        /// </summary>
        internal Vector3 GetLocalPosition(Vector3Int cell)
        {
            var min = m_center - Vector3.Scale(m_size - Vector3Int.one, tileSize) / 2.0f;
            return min + Vector3.Scale(tileSize, cell);
        }

        private bool InBounds(Vector3Int p)
        {
            return GeometryUtils.InBounds(p, size);
        }

        private static void AddAdjacency(TesseraPalette palette, AdjacentModel model, Direction d, List<(FaceDetails, Tile)> tiles1, List<(FaceDetails, Tile)> tiles2)
        {
            foreach (var (fd1, t1) in tiles1)
            {
                foreach (var (fd2, t2) in tiles2)
                {
                    if (palette.Match(fd1, fd2))
                    {
                        model.AddAdjacency(t1, t2, d);
                    }
                }
            }
        }

#region InitialConstraintUtilities

        /// <summary>
        /// Utility function that represents what <see cref="searchInitialConstraints"/> does.
        /// </summary>
        /// <returns>Initial constraints for use with <see cref="initialConstraints"/></returns>
        public List<TesseraInitialConstraint> GetInitialConstraints()
        {
            return FindObjectsOfType(typeof(TesseraTile))
                .Cast<TesseraTile>()
                .Select(GetInitialConstraint)
                .Where(x => x != null)
                .ToList();
        }

        /// <summary>
        /// Utility function that gets the initial constraint from a given tile.
        /// The tile should be aligned with the grid defined by this generator.
        /// </summary>
        /// <param name="tile">The tile to inspect</param>
        /// <returns>Initial constraint for use with <see cref="initialConstraints"/></returns>
        public TesseraInitialConstraint GetInitialConstraint(TesseraTile tile)
        {
            return GetInitialConstraint(tile, tile.transform.localToWorldMatrix);
        }

        /// <summary>
        /// Utility function that gets the initial constraint from a given tile at a given position.
        /// The tile should be aligned with the grid defined by this generator.
        /// </summary>
        /// <param name="tile">The tile to inspect</param>
        /// <param name="localToWorldMatrix">The matrix indicating the position and rotation of the tile</param>
        /// <returns>Initial constraint for use with <see cref="initialConstraints"/></returns>
        public TesseraInitialConstraint GetInitialConstraint(TesseraTile tile, Matrix4x4 localToWorldMatrix)
        {
            if (!GetCell(tile, localToWorldMatrix, out var cell, out var rotator))
            {
                return null;
            }
            // TODO: Needs support for big tiles
            return new TesseraInitialConstraint
            {
                faceDetails = tile.faceDetails, // TODO: Need to copy for multithreading?
                offsets = tile.offsets, // TODO: Need to copy for multithreading?
                cell = cell,
                rotator = rotator,
            };
        }
#endregion

#region Creation utilities
        public GameObject[] Instantiate(TesseraTileInstance instance)
        {
            return Instantiate(instance, transform);
        }

        /// <summary>
        /// Utility function that instantiates a tile instance in the scene.
        /// This is the default function used when you do not pass <c>onCreate</c> to the Generate method.
        /// It is iessentially the same as Unity's normal Instantiate method, but it respects <see cref="TesseraTile.instantiateChildrenOnly"/>.
        /// </summary>
        /// <param name="instance">The instance being created.</param>
        /// <param name="parent">The game object to parent the new game object to.</param>
        /// <returns>The game objects created.</returns>
        public static GameObject[] Instantiate(TesseraTileInstance instance, Transform parent)
        {
            if (instance.Tile.instantiateChildrenOnly)
            {
                var m = Matrix4x4.TRS(instance.Position, instance.Rotation, instance.LocalScale);
                return instance.Tile.gameObject.transform.Cast<Transform>().Select(child =>
                {
                    var go = GameObject.Instantiate(child.gameObject, m.MultiplyPoint(child.localPosition), instance.Rotation * child.localRotation, parent);
                    go.transform.localScale = Vector3.Scale(child.localScale, instance.LocalScale);
                    return go;
                }).ToArray();
            }
            else
            {
                var go = GameObject.Instantiate(instance.Tile.gameObject, instance.Position, instance.Rotation, parent);
                go.transform.localScale = instance.LocalScale;
                return new[] { go };
            }
        }
        #endregion
    }
}