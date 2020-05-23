using UnityEngine;
using DeBroglie.Rot;

namespace Tessera
{
    // Actual tiles used internally by DeBroglie.
    // There's a many-to-one relationship between ModelTile and TesseraTile
    // due to rotations and "big" tile support.
    internal struct ModelTile
    {
        public ModelTile(TesseraTile tile, Rotation rotation, Vector3Int offset)
        {
            Tile = tile;
            Rotation = rotation;
            Offset = offset;
        }

        public TesseraTile Tile { get; set; }
        public Rotation Rotation { get; set; }
        public Vector3Int Offset { get; set; }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Rotation.GetHashCode();
                hash = hash * 23 + Tile.GetHashCode();
                hash = hash * 23 + Offset.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is ModelTile other)
            {
                return Rotation.Equals(other.Rotation) && Tile == other.Tile && Offset == other.Offset;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override string ToString()
        {
            return Tile.name.ToString() + Offset.ToString() + Rotation.ToString();
        }
    }
}