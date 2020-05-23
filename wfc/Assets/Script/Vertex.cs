using UnityEngine;
using MIConvexHull;

class Vertex : IVertex
{
    public Vector3 Location;
    public double[] Position => new double[] { Location.x,Location.z};
    
}
