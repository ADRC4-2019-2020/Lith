using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using System.Linq;


public enum VoxelType { Basic, Propogable, Collapsed };


public class Voxel 
{
    

    public Vector3Int Index;
    public VoxelType Type;
    public Vector3Int Orientation;
    public bool There;
    private VoxelGrid _grid;
    public Vector3 Center;


    public Voxel(int x, int y, int z, VoxelType type, Vector3Int orientation)
    {

        Index = new Vector3Int(x, y, z);
        Type = type;
        Orientation = orientation;
        There = true;

    }
    public Voxel(int x, int y, int z)
    {
        Index = new Vector3Int(x, y, z);
    }




    public Voxel(Vector3Int index, VoxelGrid grid)
    {
        _grid = grid;
        Index = index;
        Center =  new Vector3(index.x , index.y, index.z) * grid.VoxelSize;
    }

    public bool VoidVoxel(IEnumerable<MeshCollider> colliders)
    {
        Physics.queriesHitBackfaces = true;

        var point = Center;
        var collisionSurfaces = new Dictionary<Collider, int>();

        foreach (var collider in colliders)
            collisionSurfaces.Add(collider, 0);

        

        while (Physics.Raycast(new Ray(point, Vector3.forward), out RaycastHit hit))
        {
            Debug.DrawRay(point, Vector3.forward, Color.green, 1000);

            var collider = hit.collider;

            if (collisionSurfaces.ContainsKey(collider))
                collisionSurfaces[collider]++;

            point = hit.point + Vector3.forward * 0.00001f;
        }

        bool voidVoxel = collisionSurfaces.Any(c => c.Value % 2 != 0);

        

        return voidVoxel;

       
    }
}
