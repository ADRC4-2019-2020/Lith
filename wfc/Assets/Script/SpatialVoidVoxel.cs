using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialVoidVoxel 
{

    public string FunctionTypes;
    public string UserNumber;
    public Vector3Int Location;
    public Quaternion Rotation;
    

   public  SpatialVoidVoxel(int user,string functionTypes, Vector3Int location, Quaternion rotation)
    {
        FunctionTypes = functionTypes ;
        UserNumber = user.ToString();
        Location = location;
        Rotation = rotation;
       

    }
}
