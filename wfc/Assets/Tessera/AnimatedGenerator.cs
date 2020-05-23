using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeBroglie.Topo;
using UnityEditor;
using UnityEngine;


namespace Tessera
{
    /// <summary>
    /// Attach this to a TesseraGenerator to run the generator stepwise over several updates,
    /// displaying the changes so far.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    [RequireComponent(typeof(TesseraGenerator))]
    [AddComponentMenu("Tessera/AnimatedGenerator")]
    public class AnimatedGenerator : MonoBehaviour
    {
        private bool started = false;
        private bool running = false;

        private TesseraGeneratorHelper helper;

        private DeBroglie.Topo.ITopoArray<ISet<ModelTile>> lastTiles;

        private float lastStepTime = 0.0f;

        private ITesseraTileOutput tileOutput;

        private bool supportsCubes;

        private bool[] hasObject;
        private GameObject[] cubesByIndex;

        public float secondsPerStep = .0f;

        public bool IsRunning => running;

        public bool IsStarted => started;

        public GameObject uncertaintyTile;

        public void StartGeneration()
        {
            if (running)
                return;

            if (started)
                StopGeneration();

            tileOutput?.ClearTiles();

            var generator = GetComponent<TesseraGenerator>();
            helper = generator.CreateTesseraGeneratorHelper();
            helper.Setup();
            // TODO: Should be *all tiles* to start with
            lastTiles = helper.Propagator.ToValueSets<ModelTile>();
            lastStepTime = GetTime();
            tileOutput = generator.GetComponent<ITesseraTileOutput>() ?? new UpdatableInstantiateOutput(transform);
            if(!tileOutput.SupportsIncremental)
            {
                throw new Exception($"Output {tileOutput} does not support animations");
            }
            supportsCubes = generator.GetComponent<ITesseraTileOutput>() == null;
            hasObject = new bool[helper.Propagator.Topology.IndexCount];
            cubesByIndex = new GameObject[helper.Propagator.Topology.IndexCount];
            started = true;

            ResumeGeneration();
        }

        public void ResumeGeneration()
        {
            if (running)
                return;

            if (!started)
            {
                throw new Exception("Generation must be started first.");
            }

            running = true;
            if (!Application.isPlaying)
            {
                EditorApplication.update += Step;
            }
        }

        public void PauseGeneration()
        {
            if (!running)
                return;

            running = false;
            if (!Application.isPlaying)
            {
                EditorApplication.update -= Step;
            }
        }

        public void StopGeneration()
        {
            if (!started)
                return;

            PauseGeneration();
            tileOutput?.ClearTiles();
            tileOutput = null;
            if (helper != null)
            {
                foreach (var i in helper.Propagator.Topology.GetIndices())
                {
                    ClearCube(i);
                }
            }
            started = false;
        }

        private void Update()
        {
            Step();
        }

        void ClearCube(int i)
        {
            if (cubesByIndex[i] != null)
            {
                DestroyImmediate(cubesByIndex[i]);
            }
            cubesByIndex[i] = null;
        }

        void Step()
        {
            if (!running) return;

            if (gameObject == null)
            {
                StopGeneration();
            }

            if (GetTime() < lastStepTime + secondsPerStep) return;

            var generator = GetComponent<TesseraGenerator>();

            var propagator = helper.Propagator;
            var topology = propagator.Topology;

            propagator.Step();
            lastStepTime = GetTime();
            if(propagator.Status != DeBroglie.Resolution.Undecided)
            {
                started = false;
                PauseGeneration();
            }

            var tiles = propagator.ToValueSets<ModelTile>();

            var mask = topology.Mask;
            var maskOrProcessed = mask.ToArray();

            var updateInstances = new List<TesseraTileInstance>();

            var tileCount = helper.Propagator.TileModel.Tiles.Count();
            var minScale = 0.1f;
            var maxScale = 1.0f;

            foreach (var i in topology.GetIndices())
            {
                // Skip indices that are masked out or already processed
                if (!maskOrProcessed[i])
                    continue;

                var before = lastTiles.Get(i);
                var after = tiles.Get(i);

                // Skip if nothing has changed
                if (before.SetEquals(after))
                    continue;


                if (after.Count == 1)
                {
                    topology.GetCoord(i, out var x, out var y, out var z);
                    var p = new Vector3Int(x, y, z);

                    var modelTile = after.Single();
                    var ti = generator.GetTesseraTileInstance(x, y, z, modelTile);

                    foreach (var p2 in ti.Cells)
                    {
                        if (GeometryUtils.InBounds(p2, generator.size))
                        {
                            var i2 = topology.GetIndex(p2.x, p2.y, p2.z);
                            ClearCube(i2);
                            maskOrProcessed[i2] = true;
                            hasObject[i2] = true;
                        }
                    }

                    updateInstances.Add(ti);
                }
                else
                {
                    topology.GetCoord(i, out var x, out var y, out var z);

                    maskOrProcessed[i] = true;

                    // Draw cube
                    ClearCube(i);
                    if (uncertaintyTile != null && supportsCubes)
                    {
                        var p = new Vector3Int(x, y, z);

                        var c = cubesByIndex[i] = Instantiate(uncertaintyTile, generator.transform.TransformPoint(generator.GetLocalPosition(p)), Quaternion.identity, generator.transform);
                        var scale = (maxScale - minScale) * after.Count / tileCount + minScale;
                        c.transform.localScale = Vector3.one * scale;
                    }

                    // Remove object
                    if (hasObject[i])
                    {
                        updateInstances.Add(new TesseraTileInstance
                        {
                            Cells = new[] { new Vector3Int(x, y, z) }
                        });
                        hasObject[i] = false;
                    }
                    
                }
            }

            tileOutput.UpdateTiles(updateInstances);
            lastTiles = tiles;
        }

        private float GetTime()
        {
            if (Application.isPlaying)
            {
                return Time.time;
            }
            else
            {
                return (float)(DateTime.Now - new DateTime(2020, 1, 1)).TotalSeconds;
            }
        }
    }
}
