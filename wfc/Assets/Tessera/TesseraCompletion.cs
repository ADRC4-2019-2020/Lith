using System.Collections.Generic;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Returned by TesseraGenerator after generation finishes
    /// </summary>
    public class TesseraCompletion
    {
        /// <summary>
        /// True if all tiles were successfully found.
        /// </summary>
        public bool success { get; set; }

        /// <summary>
        /// The list of tiles to create.
        /// </summary>
        public IList<TesseraTileInstance> tileInstances { get; set; }

        /// <summary>
        /// The number of times the generation process was restarted.
        /// </summary>
        public int retries { get; set; }

        /// <summary>
        /// The number of times the generation process backtracked.
        /// </summary>
        public int backtrackCount { get; set; }

        /// <summary>
        /// If success is false, indicates where the generation failed.
        /// </summary>
        public Vector3Int? contradictionLocation { get; set; }
    }
}