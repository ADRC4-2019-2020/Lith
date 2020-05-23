using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MIConvexHull;
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Algorithms.Search;
using System;
using System.Diagnostics;
using Random = UnityEngine.Random;
using static VoxelGrid;

public class RoomSpawning : MonoBehaviour
{
    GameObject SpaceAsVoxel;
    public List<SpatialVoidVoxel> ListOfSpaces = new List<SpatialVoidVoxel>();
    public SpatialVoidVoxel space;
    public List<GameObject> go = new List<GameObject>();

    string SpaceObject = "Room" ;
    public int InitialFitness;
    public bool IterationComplete;
    public bool Mesh = false;

    void Update()
    {
        Graphics.DrawMesh(_renderMesh, Matrix4x4.identity, _material, 0);
        if (Mesh==true)
        {
            SpaceAsMesh();
        }

        
    }

    void OnGUI()
    {
        int i = 1;
        int s = 25;

        if (GUI.Button(new Rect(s, s * i, 100, 20), "Parse Space"))
        {
            SpaceParser();

        }

        if (GUI.Toggle(new Rect(s + 110, s * i++, 100, 20), false, "On/Off"))
        {

            OnOFF();
        }



        if (GUI.Button(new Rect(s, s * i, 100, 20), "Optimize"))
        {
            StartCoroutine(HillClimbingOptmization());
        }
        GUI.Label(new Rect(s + 110, s * i++, 100, 20), "Score: " + InitialFitness.ToString());

      

        if (GUI.Button(new Rect(s, s * i++, 100, 20), "Generate Graph"))
        {
            Mesh = true;
        }

        if (GUI.Button(new Rect(s, s * i++, 100, 20), "Grid"))
        {
            var voids = go.Select(c => c.GetComponent<MeshCollider>());
            _grid = MakeGridWithVoids(voids, 1f);
            StartCoroutine(DisplayVoxel(_grid.Voxels));
        }
        if (IterationComplete == true)
        {
            GUI.Label(new Rect(s, s * i, 200, 20), "Iterations Complete");
        }
    }


    public void OnOFF()
    {
        foreach (var VisibleObject in go)
        {
            VisibleObject.GetComponent<MeshRenderer>().enabled = !VisibleObject.GetComponent<MeshRenderer>().enabled;
        }
    }

   


    // Take a list of users defining spacee usage for 12 hours in a day and generate co-living spaces from them.
    // USER: functions at these times{00-02,02-04,04-06,06-08,08,10,10-12,12-14,14-16,16-18,18-20,20-22,22-24} 
    // will probably make this 24 hour long instead of 12 and also make the input through the UI rather than here in a list

    List<string[]> Schedule = new List<string[]>
    {
      new string[]  { "Bed", "Bed", "Bed", "Bed", "Bath", "Kitchen", "LivingRoom", "LivingRoom", "WorkSpace", "Bath", "Bed",  "Bed"},
      new string[]  { "Bed", "Bed", "Bed", "Bed", "Bath", "LivingRoom", "LivingRoom", "LivingRoom", "Kitchen", "Kitchen", "Bed",  "Bed"},
      new string[]  { "Bed", "Bed", "Bed", "Bed", "Bath", "LivingRoom", "WorkSpace", "WorkSpace", "WorkSpace", "LivingRoom", "Bed",  "Bed"},
      new string[]  { "Bed", "Bed", "Bed", "Bed", "Bath", "Kitchen", "LivingRoom", "LivingRoom", "WorkSpace", "LivingRoom", "Bed",  "Bed"},
      new string[]  { "Bed", "Bed", "Bed", "Bed", "Bath", "Kitchen", "LivingRoom", "LivingRoom", "WorkSpace", "LivingRoom", "Bed",  "Bed"},
      new string[]  { "Bed", "Bed", "Bed", "Bed", "Bath", "Kitchen", "LivingRoom", "LivingRoom", "WorkSpace", "WorkSpace", "Bed",  "Bed"},
    };



    void SpaceParser()
    {
        int Xoffset = 0;

        // For Private functions for each user check if the user has the spaceFunction Bed or Bath in his Schedule if he does then make that spaceFunction once for that user
        foreach (var User in Schedule)
        {
            List<string> Spacethere = new List<string> { };

            foreach (var spaceFunction in User)
            {
                

                if (spaceFunction == "Bath" || spaceFunction == "Bed")
                {
                    if (!Spacethere.Any(s => s == spaceFunction))
                    {
                        

                        space = new SpatialVoidVoxel(Schedule.IndexOf(User), SpaceObject, Vector3Int.zero + new Vector3Int(Random.Range(-8, 8), Random.Range(-3, 3), Random.Range(-10, 10)), Quaternion.identity);
                        ListOfSpaces.Add(space);
                        Spacethere.Add(spaceFunction);

                    }
                }
            }
        }

        // For public functions in the Schedule check which time has the largest need for that function then divide that need by sharing index(how many people share spaceFunction) to make the spaceFunction
        // create a list for requirement of each function
        List<string> NumKitchen = new List<string> { };
        List<string> NumWorkspace = new List<string> { };
        List<string> NumOut = new List<string> { };
        List<string> NumLivingRoom = new List<string> { };
      

        // use the logic of sorting through schedule to find how many spaceFunction 
        ParseSharedSpace LivingRoom = new ParseSharedSpace(SpaceObject, NumLivingRoom, Schedule, ListOfSpaces);
        ParseSharedSpace Workspace = new ParseSharedSpace(SpaceObject, NumWorkspace, Schedule, ListOfSpaces);
        ParseSharedSpace Kitchen = new ParseSharedSpace(SpaceObject, NumKitchen, Schedule, ListOfSpaces);
        ParseSharedSpace Out = new ParseSharedSpace(SpaceObject, NumOut, Schedule, ListOfSpaces);


        // Both Private and Public spaces which need to exist are instantiated as game objects and stored in gameobject list go
        // each function is right now a single voxel, but would make that into a bunch of voxels defined by their relation to each other later
        foreach (var Space in ListOfSpaces)
        {
            SpaceAsVoxel = Instantiate(Resources.Load<GameObject>(Space.FunctionTypes), Space.Location, Space.Rotation);
            go.Add(SpaceAsVoxel);

        }


    }

    //The class defines how public/shared functions are sorted
    public class ParseSharedSpace
    {
        public ParseSharedSpace(string spaceFunction, List<string> spaceRequired, List<string[]> timeSchedule, List<SpatialVoidVoxel> theseSpacesExist)
        {
            int SharingIndex = 2;

            // Since the schedule always has the same number of hours for all user( the schedule can have 24 hours rather than just 12, its easier to keep track right now)
            for (int index = 0; index < timeSchedule[0].Length; index++)
            {
                List<string> temp = new List<string>();

                foreach (var item in timeSchedule)
                {
                    temp.Add(item[index]);

                    if (item[index] == spaceFunction && temp.Count(t => t == spaceFunction) > spaceRequired.Count)
                    {
                        spaceRequired.Add(item[index]);
                    }
                }
            }

            for (int i = 0; i < spaceRequired.Count / SharingIndex; i++)
            {
                var Room = new SpatialVoidVoxel(1, spaceFunction, Vector3Int.zero + new Vector3Int(Random.Range(-5, 5), Random.Range(-3, 3), Random.Range(-8, 8)), Quaternion.identity);
                theseSpacesExist.Add(Room);
            }
        }
    }



    // trying to rearrange the spaces so certain spaces are closer to each other than other spaces
    // essentialy trying to control the closeness of spaces to every other space
    private IEnumerator HillClimbingOptmization()
    {
        var IterationCount = 1000;
        for (var i = 0; i < IterationCount; i++)
        {
            //pick any space and try to move it 5 voxels in any direction
            var randomspace = Random.Range(0, ListOfSpaces.Count);
            var stepsize = 5;
            var thispos = ListOfSpaces[randomspace].Location + new Vector3Int(Random.Range(-stepsize, stepsize), Random.Range(-stepsize, stepsize), Random.Range(-stepsize, stepsize));
            var lastpos = go[randomspace].transform.position;
            var dia = 20;

            if (ListOfSpaces.All(l => l.Location != thispos)
                && thispos.x > Vector3Int.zero.x
                && thispos.y > Vector3Int.zero.y
                && thispos.z > Vector3Int.zero.z
            )

            {// If the space gives a better score through the matrix that defines the score between each space then let the change happen 
                if (Mathf.Abs(thispos.x) < dia && Mathf.Abs(thispos.y) < dia && Mathf.Abs(thispos.z) < dia)
                {
                    go[randomspace].transform.position = thispos;
                    yield return new WaitForSeconds(0.05f);
                    var connectionadjecency = new Matrix(ListOfSpaces[randomspace], ListOfSpaces, thispos);
                    if (connectionadjecency.fitnessSum >= InitialFitness)
                    {
                        ListOfSpaces[randomspace].Location = thispos;
                        go[randomspace].transform.position = thispos;
                        InitialFitness = connectionadjecency.fitnessSum;
                    }
                    else go[randomspace].transform.position = lastpos;
                }
            }
           
            IterationComplete = i == IterationCount-1 ? true : false;
            UnityEngine.Debug.Log(InitialFitness);
            yield return new WaitForSeconds(0.05f);

        }
        yield return go;
    }



    //Vicente's Graph Script
    // Used here for visualisation of connections between spaces, would want to use the graph library to use for adjacency optimization(Don't know how yet.)

    [SerializeField] Mesh _mesh = null;
    [SerializeField] Material _material = null;
    Mesh _renderMesh;
    Color32[] _colors;


    
    IEnumerable<Edge> DelaunayEdges()
    {
        var points = go.Select(l => l.transform.position).ToList();
        var vertices = points.Select(p => new Vertex() { Location = p }).ToList();

        var delaunay = Triangulation.CreateDelaunay<Vertex, Face>(vertices);
        var edges = delaunay.Cells
            .SelectMany(c => c.GetEdges())
            .Distinct();

        return edges;
    }
    void SpaceAsMesh()
    {
        var edges = DelaunayEdges().ToList();
        MeshFromEdges(edges);
    }

    void MeshFromEdges(IEnumerable<Edge> edges)
    {
        SetMesh(edges, e =>
        {
            var rotation = Quaternion.LookRotation(e.Vector, Vector3.up);
            var scale = new Vector3(0.02f, 0.02f, e.Length);
            return Matrix4x4.TRS(e.Center, rotation, scale);
        });
    }

    void SetMesh<T>(IEnumerable<T> elements, Func<T, Matrix4x4> projection)
    {
        var matrices = elements.Select(projection);
        _renderMesh = CombineMesh(_mesh, matrices);
        _colors = new Color32[_renderMesh.vertexCount];
        for (int i = 0; i < _renderMesh.vertexCount; i++)
            _colors[i] = Color.white;
    }

    Mesh CombineMesh(Mesh instance, IEnumerable<Matrix4x4> matrices)
    {
        var instances = matrices
            .Select(m => new CombineInstance() { mesh = instance, transform = m });

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.CombineMeshes(instances.ToArray());
        return mesh;
    }



    // Part of Vicente's Voxel Script to generate the building around the functions spaces or voids( which are singe voxels right now )
    

    public GameObject VoxelCube;
    VoxelGrid _grid;
    IEnumerator DisplayVoxel(Voxel[,,] voxels)
    {
        foreach (var voxel in voxels)
        {
            if (voxel.There)
                Instantiate(VoxelCube, voxel.Index, Quaternion.identity, transform);
        }
        yield return new WaitForSeconds(0);
    }

}









