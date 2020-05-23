using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class VoxelGrid
{
    public Vector3Int Index;
    public Voxel[,,] Voxels;
    public Bounds BoundingBox;
    public Vector3Int Size;
    public float VoxelSize;

    public Vector3 Corner;

    public static VoxelGrid MakeGridWithVoids(IEnumerable<MeshCollider> voids, float voxelSize=1f)
    {
        var boundingBox = new Bounds();
        foreach (var v in voids.Select(v => v.bounds))
            boundingBox.Encapsulate(v);


        var grid = new VoxelGrid(boundingBox, voxelSize);

        foreach (var voxel in grid.Voxels)
        {
            voxel.There = !voxel.VoidVoxel(voids);
        }
        return grid;
    }

    public VoxelGrid(Bounds boundingBox, float voxelSize = 1.0f)
    {
        BoundingBox = boundingBox;
        VoxelSize = voxelSize;

        boundingBox.min = new Vector3(boundingBox.min.x, boundingBox.min.y, boundingBox.min.z);
        var sizeFloat = boundingBox.size / voxelSize;
       
        Size = new Vector3Int(Mathf.CeilToInt(sizeFloat.x), Mathf.CeilToInt(sizeFloat.y), Mathf.CeilToInt(sizeFloat.z));
        
        
        Corner = boundingBox.min ;

        Voxels = new Voxel[Size.x, Size.y, Size.z];

        for (int z = 0; z < Size.z; z++)
            for (int y = 0; y < Size.y; y++)
                for (int x = 0; x < Size.x; x++)
                {
                    Voxels[x, y, z] = new Voxel(new Vector3Int(x, y, z), this);
                    
                }
    }

   

}




