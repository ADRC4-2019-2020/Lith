using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Rot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Abstract class for all generator constraint components.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    public abstract class TesseraConstraint : MonoBehaviour
    {
        internal abstract ITileConstraint GetTileConstraint(AdjacentModel model);

        internal IEnumerable<ModelTile> GetModelTiles(IEnumerable<TesseraTile> tiles)
        {
            var rg = new RotationGroup(4, true);
            foreach (var tile in tiles)
            {
                if (tile == null)
                    continue;

                foreach (var rot in rg)
                {
                    if (!tile.rotatable && rot.RotateCw != 0)
                        continue;
                    if (!tile.reflectable && rot.ReflectX)
                        continue;

                    foreach (var offset in tile.offsets)
                    {
                        var modelTile = new ModelTile(tile, rot, offset);
                        yield return modelTile;
                    }
                }
            }
        }
    }
}
