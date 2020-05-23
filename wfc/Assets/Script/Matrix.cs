using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;

public class Matrix
{
    public SpatialVoidVoxel SVoidVoxel;
    public List<SpatialVoidVoxel> MBuiltSpaces;
    public int fitness = 0;
    public int fitnessSum = 0;

   // Defining what score each space gets to move closer to every other space, 
   // would have to localize its effects as optimization halts currently
   // Also I know this is probably a terrible way of defining these. Couldn't figure out a better way yet, but will soon.

    public Matrix(SpatialVoidVoxel sVVoxel, List<SpatialVoidVoxel> mBuiltSpaces, Vector3Int changedPos)
    {
        SVoidVoxel = sVVoxel;
        MBuiltSpaces = mBuiltSpaces;

        
        foreach (var item in mBuiltSpaces)
        {
            var initialDist = item.Location - sVVoxel.Location;
            var changedDist = item.Location - changedPos;


            if (sVVoxel.FunctionTypes == "Bed")
            {
                // 
                if (item.FunctionTypes == "Bed")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 2 : 0;
                    fitnessSum += fitness;

                }
                if (item.FunctionTypes == "Bath")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 6;
                    fitnessSum += fitness;
                }
                if (item.FunctionTypes == "LivingRoom")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 2;
                    fitnessSum += fitness;
                }
                if (item.FunctionTypes == "WorkSpace")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum += fitness;
                }
                if (item.FunctionTypes == "Kitchen")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 1;
                    fitnessSum += fitness;
                }
                if (item.FunctionTypes == "Out")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum += fitness;
                }


            }


            if (sVVoxel.FunctionTypes == "Bath")
            {
                if (item.FunctionTypes == "Bed")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0: 4;
                    fitnessSum += fitness;
                }
                if (item.FunctionTypes == "Bath")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 4 : 0;
                    fitnessSum += fitness;

                }
                if (item.FunctionTypes == "LivingRoom")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum += fitness;
                }
                if (item.FunctionTypes == "WorkSpace")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum += fitness;
                }
                if (item.FunctionTypes == "Kitchen")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum += fitness;
                }
                if (item.FunctionTypes == "Out")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum += fitness;
                }
            }


            if (sVVoxel.FunctionTypes == "LivingRoom")
            {
                if (item.FunctionTypes == "Bed")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 3;
                    fitnessSum += fitness;
                }


                if (item.FunctionTypes == "Bath")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 1;
                    fitnessSum += fitness;

                }


                if (item.FunctionTypes == "LivingRoom")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 6 : 0;
                    fitnessSum += fitness;
                }


                if (item.FunctionTypes == "WorkSpace")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 2;
                    fitnessSum += fitness;
                }


                if (item.FunctionTypes == "Kitchen")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 3;
                    fitnessSum += fitness;
                }


                if (item.FunctionTypes == "Out")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 2;
                    fitnessSum += fitness;
                }
            }


            if (sVVoxel.FunctionTypes == "WorkSpace")
            {
                if (item.FunctionTypes == "Bed")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 2;
                    fitnessSum += fitness;
                }


                if (item.FunctionTypes == "Bath")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum += fitness;

                }


                if (item.FunctionTypes == "LivingRoom")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 4;
                    fitnessSum += fitness;
                }


                if (item.FunctionTypes == "WorkSpace")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 6 : 0;
                    fitnessSum += fitness;
                }


                if (item.FunctionTypes == "Kitchen")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 2;
                    fitnessSum += fitness;
                }


                if (item.FunctionTypes == "Out")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum += fitness;
                }
            }



            if (sVVoxel.FunctionTypes == "Kitchen")
            {
                if (item.FunctionTypes == "Bed")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 2;
                    fitnessSum += fitness;
                }
                if (item.FunctionTypes == "Bath")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum += fitness;

                }
                if (item.FunctionTypes == "LivingRoom")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 4;
                    fitnessSum += fitness;
                }
                if (item.FunctionTypes == "WorkSpace")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 3;
                    fitnessSum += fitness;
                }
                if (item.FunctionTypes == "Kitchen")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 6 : 0;
                    fitnessSum += fitness;
                }
                if (item.FunctionTypes == "Out")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum += fitness;
                }

            }



            if (sVVoxel.FunctionTypes == "Out")
            {
                if (item.FunctionTypes == "Bed")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum = fitnessSum + fitness;
                }
                else if (item.FunctionTypes == "Bath")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum = fitnessSum + fitness;

                }
                else if (item.FunctionTypes == "LivingRoom")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 3;
                    fitnessSum = fitnessSum + fitness;
                }
                else if (item.FunctionTypes == "WorkSpace")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum = fitnessSum + fitness;
                }
                else if (item.FunctionTypes == "Kitchen")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 0 : 0;
                    fitnessSum = fitnessSum + fitness;
                }
                else if (item.FunctionTypes == "Out")
                {
                    fitness = initialDist.magnitude < changedDist.magnitude ? 1 : 0;
                    fitnessSum = fitnessSum + fitness;
                }
            }

        }
    }





}
