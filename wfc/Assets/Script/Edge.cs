using System;
using UnityEngine;
using QuickGraph;

// From Vicente's Graph scripts
class Edge : IEquatable<Edge>, IEdge<Vertex>
{
    public Vertex Source { get; set; }
    public Vertex Target { get; set; }
    public float Value = 1;

    public Vector3 Vector => Source.Location - Target.Location;
    public float Length => Vector.magnitude;
    public Vector3 Center => (Source.Location + Target.Location) * 0.5f;

    public bool Equals(Edge other)
    {
        if (other.Source == Source && other.Target == Target) return true;
        if (other.Source == Target && other.Target == Source) return true;
        return false;
    }

    public override int GetHashCode()
    {
        return Source.GetHashCode() + Target.GetHashCode();
    }
}
