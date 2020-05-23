using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    public interface ITesseraTileOutput
    {
        /// <summary>
        /// Is this output safe to use with AnimatedGenerator
        /// </summary>
        bool SupportsIncremental { get; }

        /// <summary>
        /// Is the output currently empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Clear the output
        /// </summary>
        void ClearTiles();

        /// <summary>
        /// Update a chunk of tiles.
        /// If inremental updates are supported, then:
        ///  * Tiles can replace other tiles, as indicated by the <see cref="TesseraTileInstance.Cells"/> field.
        ///  * A tile of null indicates that the tile should be erased
        /// </summary>
        void UpdateTiles(IEnumerable<TesseraTileInstance> tileInstances);
    }

    internal class ForEachOutput : ITesseraTileOutput
    {
        private Action<TesseraTileInstance> onCreate;

        public ForEachOutput(Action<TesseraTileInstance> onCreate)
        {
            this.onCreate = onCreate;
        }

        public bool IsEmpty => throw new NotImplementedException();

        public bool SupportsIncremental => throw new NotImplementedException();

        public void ClearTiles()
        {
            throw new NotImplementedException();
        }

        public void UpdateTiles(IEnumerable<TesseraTileInstance> tileInstances)
        {
            foreach (var i in tileInstances)
            {
                onCreate(i);
            }
        }
    }

    internal class InstantiateOutput : ITesseraTileOutput
    {
        private Transform transform;

        public InstantiateOutput(Transform transform)
        {
            this.transform = transform;
        }

        public bool IsEmpty => throw new NotImplementedException();

        public bool SupportsIncremental => false;

        public void ClearTiles()
        {
            throw new NotImplementedException();
        }

        public void UpdateTiles(IEnumerable<TesseraTileInstance> tileInstances)
        {
            foreach (var i in tileInstances)
            {
                TesseraGenerator.Instantiate(i, transform);
            }
        }
    }


    internal class UpdatableInstantiateOutput : ITesseraTileOutput
    {
        private Dictionary<Vector3Int, GameObject[]> instantiated = new Dictionary<Vector3Int, GameObject[]>();

        private Transform transform;

        public UpdatableInstantiateOutput(Transform transform)
        {
            this.transform = transform;
        }

        public bool IsEmpty => transform.childCount == 0;

        public bool SupportsIncremental => true;

        private void Clear(Vector3Int p)
        {
            if (instantiated.TryGetValue(p, out var gos) && gos != null)
            {
                foreach (var go in gos)
                {
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(go);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(go);
                    }
                }
            }

            instantiated[p] = null;
        }

        public void ClearTiles()
        {
            foreach (var k in instantiated.Keys.ToList())
            {
                Clear(k);
            }
        }

        public void UpdateTiles(IEnumerable<TesseraTileInstance> tileInstances)
        {
            foreach (var i in tileInstances)
            {
                foreach (var p in i.Cells)
                {
                    Clear(p);
                }
                instantiated[i.Cells.First()] = TesseraGenerator.Instantiate(i, transform);
            }
        }
    }

}
