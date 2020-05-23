using System.Collections.Generic;
using UnityEngine;
using MIConvexHull;

// From Vicente's Graph scripts
class Face : TriangulationCell<Vertex, Face>
{
    public IEnumerable<Edge> GetEdges()
    {
        for (int i = 0; i < Vertices.Length; i++)
        {
            int j = i < Vertices.Length - 1 ? i + 1 : 0;
            yield return new Edge() { Source = Vertices[i], Target = Vertices[j] };
        }
    }

    public Vector3 GetCentroid()
    {
        Vector3 center = Vector3.zero;

        foreach (var vertex in Vertices)
            center += vertex.Location;

        center /= Vertices.Length;
        return center;
    }
}
