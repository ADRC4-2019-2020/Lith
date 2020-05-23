using System;
using System.Threading;

namespace Tessera
{

    /// <summary>
    /// Additional settings to customize the generation at runtime.
    /// </summary>
    public class TesseraGenerateOptions
    {
        /// <summary>
        /// Called for each newly generated tile. By default, <see cref="TesseraGenerator.Instantiate(TesseraTileInstance)"/> is used.
        /// </summary>
        public Action<TesseraTileInstance> onCreate;

        /// <summary>
        /// Called when the generation is complete. By default, checks for success then invokes <see cref="onCreate"/> on each instance.
        /// </summary>
        public Action<TesseraCompletion> onComplete;

        /// <summary>
        /// Called with a string describing the current phase of the calculations, and the progress from 0 to 1.
        /// Progress can move backwards for retries or backtracing.
        /// Note progress can be called from threads other than the main thread.
        /// </summary>
        public Action<string, float> progress;

        /// <summary>
        /// Allows interuption of the calculations
        /// </summary>
        public CancellationToken cancellationToken;

        /// <summary>
        /// Fixes the seed for random number generator.
        /// If the value is zero, the seed is taken from Unity.Random 
        /// </summary>
        public int seed { get; set; }

        /// <summary>
        /// If set, then generation is offloaded to another thread
        /// stopping Unity from freezing.
        /// Requires you to use StartGenerate in a coroutine.
        /// </summary>
        public bool multithreaded { get; set; } = true;
    }
}