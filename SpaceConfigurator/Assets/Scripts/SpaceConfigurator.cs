using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

using MIConvexHull;
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Algorithms.Search;

public class SpaceConfigurator : MonoBehaviour
{
    public Vector3Int bounds = new Vector3Int(10, 5, 20);

    public List<SpaceProperties> ListOfSpaces = new List<SpaceProperties>();
    public int ScheduleLength = 12;
    List<UserProperties> Schedule;

    void Start()
    {

        SpaceParser();
        StartCoroutine(Optmizer());
    }
    private void Update()
    {

    }
    public void SpaceParser()
    {
        Schedule = new List<UserProperties>(ScheduleLength);
        Scheduler(Schedule);
        for (int Index = 0; Index < ScheduleLength; Index++)
        {
            List<SpaceProperties> temp = new List<SpaceProperties>();
            foreach (var user in Schedule)
            {
                var function = user.DaySchedule[Index];
                temp.Add(new SpaceProperties()
                {
                    UserName = user.Name,
                    Function = function
                });
                if (function == "Private" && !ListOfSpaces.Any(s => s.UserName == user.Name))
                {
                    ListOfSpaces.Add(new SpaceProperties()
                    {
                        UserName = user.Name,
                        Function = function
                    });
                }
                else if (!ListOfSpaces.Any(s => s.Function == function && s.UserName == user.Name) && temp.Count(t => t.Function == function) > ListOfSpaces.Count(l => l.Function == function))
                {
                    ListOfSpaces.Add(new SpaceProperties()
                    {
                        UserName = user.Name,
                        Function = function

                    });
                }
            }
        }
        Populate();
    }
    public GameObject Object;
    public List<List<GameObject>> ConfigurationIndex = new List<List<GameObject>>();
    public void Populate()
    {
        foreach (var Space in ListOfSpaces)
        {
            List<GameObject> Configuration = new List<GameObject>();
            SpaceConfiguration _spaceConfiguration = new SpaceConfiguration();
            _spaceConfiguration.sequence(Space.Function);
            foreach (var V in _spaceConfiguration.Configuration)
            {
                Vector3Int Objectposition = Space.Location + V;
                GameObject NewObject = Instantiate(Object, Objectposition, Space.Rotation);
                NewObject.GetComponent<UserFunction>().Function = Space.Function;
                NewObject.GetComponent<UserFunction>().UserName = Space.UserName;
                Configuration.Add(NewObject);
            }

            ConfigurationIndex.Add(Configuration);
        }
    }
    public bool Conflict(List<List<GameObject>> ConfigurationList, List<GameObject> ConfigurationIndex,List<Vector3Int> ThisVector)
    {
        foreach (var _Configuration in ConfigurationList)
        {
            if (ThisVector.Any(v=> _Configuration.Except(ConfigurationIndex).Any(c=>c.transform.position == v)))
            {
                return true;
            }

        }

        return false;

    }


    public int InceptiveAdequacy = 0;
    public int Adequacy;
    public bool EndIterationEffectuation;
    private IEnumerator Optmizer()
    {
        UnityEngine.Debug.Log(1);
        var ICount = 1000;
        var OrbitExtents = 5;
        for (var i = 0; i < ICount; i++)
        {
            var RIndex = Random.Range(0, ConfigurationIndex.Count);
            var RandomVector = Vector3Int.RoundToInt(Random.insideUnitSphere * OrbitExtents);
            List<Vector3Int> ThisVector = ConfigurationIndex[RIndex].Select(c => Vector3Int.RoundToInt(c.transform.position) + RandomVector).ToList();
            var LastVector = ListOfSpaces[RIndex].Location;
            Adequacy = Conflict(ConfigurationIndex,  ConfigurationIndex[RIndex], ThisVector) ? - 2 :  + 2;

            if (ThisVector.All(x => CheckBounds(x)))
            {
                foreach (var Center in ConfigurationIndex[RIndex])
                {
                    var BackTrackVector = Center.transform.position;
                    Center.transform.position = Center.transform.position + RandomVector;
                    UnityEngine.Debug.Log(Adequacy);
                    
                    if (Adequacy <= InceptiveAdequacy)
                    {
                        Center.transform.position = BackTrackVector;
                    }
                }
                yield return new WaitForSeconds(0.05f);
            }
            EndIterationEffectuation = i == ICount - 1 ? true : false;
        }
        yield return ConfigurationIndex;
    }
    bool CheckBounds(Vector3Int index)
    {
        if (index.x < -bounds.x / 2) return false;
        if (index.y < -bounds.y / 2) return false;
        if (index.z < -bounds.z / 2) return false;
        if (index.x >= bounds.x / 2) return false;
        if (index.y >= bounds.y / 2) return false;
        if (index.z >= bounds.z / 2) return false;
        return true;
    }
    public void Scheduler(List<UserProperties> Schedule)
    {

        Schedule.Add(new UserProperties()
        {
            Name = "Connie",
            DaySchedule = new string[] { "Private", "Private", "Private", "Private", "Private", "Kitchen", "LivingRoom", "LivingRoom", "WorkSpace", "Private", "Private", "Private" }
        });

        Schedule.Add(new UserProperties()
        {
            Name = "Adam",
            DaySchedule = new string[] { "Private", "Private", "Private", "Private", "Private", "LivingRoom", "LivingRoom", "LivingRoom", "Kitchen", "Kitchen", "Private", "Private" }
        });

        Schedule.Add(new UserProperties()
        {
            Name = "Steve",
            DaySchedule = new string[] { "Private", "Private", "Private", "Private", "Private", "LivingRoom", "WorkSpace", "WorkSpace", "WorkSpace", "LivingRoom", "Private", "Private" }
        });

        Schedule.Add(new UserProperties()
        {
            Name = "Ratchel",
            DaySchedule = new string[] { "Private", "Private", "Private", "Private", "Private", "Kitchen", "LivingRoom", "LivingRoom", "WorkSpace", "LivingRoom", "Private", "Private" }
        });

        Schedule.Add(new UserProperties()
        {
            Name = "Phoebe",
            DaySchedule = new string[] { "Private", "Private", "Private", "Private", "Private", "Kitchen", "LivingRoom", "LivingRoom", "WorkSpace", "LivingRoom", "Private", "Private" }
        });


    }

}

public class SpaceConfiguration
{
    public List<Vector3Int> Configuration;
    public List<Vector3Int> sequence(string function)
    {
        if (function == "Private")
        {
            Configuration = new List<Vector3Int>
            {
                new Vector3Int(0,0,0), new Vector3Int(1,0,0), new Vector3Int(0,0,1), new Vector3Int(1,0,1), new Vector3Int(0,0,0)
            };
        }

        if (function == "Kitchen")
        {
            Configuration = new List<Vector3Int>
            {
                new Vector3Int(0,0,0), new Vector3Int(1,0,0), new Vector3Int(0,0,1), new Vector3Int(1,0,1), new Vector3Int(0,0,0)
            };
        }

        if (function == "WorkSpace")
        {
            Configuration = new List<Vector3Int>
            {
                new Vector3Int(0,0,0), new Vector3Int(1,0,0), new Vector3Int(0,0,1), new Vector3Int(1,0,1), new Vector3Int(0,0,0)
            };
        }

        if (function == "LivingRoom")
        {
            Configuration = new List<Vector3Int>
            {
                new Vector3Int(0,0,0), new Vector3Int(1,0,0), new Vector3Int(0,0,1), new Vector3Int(1,0,1), new Vector3Int(0,0,0)
            };
        }
        return Configuration;
    }
}
public class SpaceProperties
{
    public string UserName { get; set; }
    public string Function { get; set; }
    public string SharingType { get; set; }
    public Vector3Int Location { get; set; }
    public Quaternion Rotation { get; set; }
}
public class UserProperties
{
    public string Name { get; set; }
    public string[] DaySchedule { get; set; }
}


public class Interspace
{
    
    public Interspace(Vector3Int InitialVector, Vector3Int UpdatedVector, Vector3Int TargetVector)
    {
        var interspace = Vector3Int.Distance(InitialVector, TargetVector) - Vector3Int.Distance(UpdatedVector, TargetVector);
    }
}
public class Contiguity
{
    public int Adequacy;
    public int GenerationAdequacy;
    public Interspace Key;
    public Contiguity(Vector3Int InitialVector, Vector3Int UpdatedVector, List<List<GameObject>> ConfigurationIndex)
    {
        foreach (var configuration in ConfigurationIndex)
        {
            foreach (var center in configuration)
            {
                Key = new Interspace(InitialVector, UpdatedVector,Vector3Int.RoundToInt(center.transform.position));
            }
        }
    }
}


