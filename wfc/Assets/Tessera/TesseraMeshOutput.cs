using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Attach this to a TesseraGenerator to output the tiles to a single mesh instead of instantiating them.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    [RequireComponent(typeof(TesseraGenerator))]
    [AddComponentMenu("Tessera/TesseraMeshOutput")]
    public class TesseraMeshOutput : MonoBehaviour, ITesseraTileOutput
    {
        public MeshFilter targetMeshFilter;

        public bool IsEmpty => targetMeshFilter == null || targetMeshFilter.sharedMesh == null;

        public bool SupportsIncremental => false;

        public void ClearTiles()
        {
            targetMeshFilter.mesh = null;
        }

        private object CanonicalizeMaterial(Material m)
        {
            return m.ToString();
        }

        public void UpdateTiles(IEnumerable<TesseraTileInstance> tileInstances)
        {
            // Work out material indices
            var allMaterials = new HashSet<object>();
            foreach (var i in tileInstances)
            {
                void GetMaterials(GameObject subObject)
                {
                    var meshFilter = subObject.GetComponent<MeshFilter>();
                    var meshRenderer = subObject.GetComponent<MeshRenderer>();
                    if (meshFilter == null)
                    {
                        return;
                    }
                    if (meshRenderer == null)
                    {
                        throw new Exception($"Expected MeshRenderer to accompany MeshFilter on {subObject}");
                    }
                    var materials = meshRenderer.sharedMaterials;
                    foreach (var m in materials)
                    {
                        allMaterials.Add(CanonicalizeMaterial(m));
                    }

                }
                if(i.Tile.instantiateChildrenOnly)
                {
                    foreach(Transform child in i.Tile.transform)
                    {
                        GetMaterials(child.gameObject);
                    }
                }
                else
                {
                    GetMaterials(i.Tile.gameObject);
                }
            }
            var allMaterialsList = allMaterials.ToList();
            var targetRenderer = targetMeshFilter.GetComponent<MeshRenderer>();
            if(targetRenderer)
            {
                foreach(var material in targetRenderer.sharedMaterials.Reverse())
                {
                    var m = CanonicalizeMaterial(material);
                    var i = allMaterialsList.IndexOf(m);
                    if(i >= 0)
                    {
                        allMaterialsList.RemoveAt(i);
                        allMaterialsList.Insert(0, m);
                    }
                }
            }

            // For each material, make a mesh
            var allMeshes = new List<Mesh>();
            var allCombine = new List<CombineInstance>();
            foreach (var material in allMaterialsList)
            {
                var combineInstances = new List<CombineInstance>();
                foreach (var i in tileInstances)
                {
                    void HandleSubObject(GameObject subObject, Matrix4x4 transform)
                    {
                        var meshFilter = subObject.GetComponent<MeshFilter>();
                        var meshRenderer = subObject.GetComponent<MeshRenderer>();
                        if(meshFilter == null)
                        {
                            return;
                        }
                        if(meshRenderer == null)
                        {
                            throw new Exception($"Expected MeshRenderer to accompany MeshFilter on {subObject}");
                        }
                        var mesh = meshFilter.sharedMesh;
                        var materials = meshRenderer.sharedMaterials.Select(CanonicalizeMaterial).ToList();
                        var subMeshIndex = materials.ToList().IndexOf(material);
                        if (subMeshIndex >= 0)
                        {
                            combineInstances.Add(new CombineInstance
                            {
                                mesh = mesh,
                                transform = targetMeshFilter.transform.worldToLocalMatrix * Matrix4x4.TRS(i.Position, i.Rotation, i.LocalScale) * transform,
                                subMeshIndex = subMeshIndex,
                            });
                        }
                    }

                    if (i.Tile.instantiateChildrenOnly)
                    {
                        foreach(Transform child in i.Tile.transform)
                        {
                            HandleSubObject(child.gameObject, Matrix4x4.TRS(child.localPosition, child.localRotation, child.localScale));
                        }
                    }
                    else
                    {
                        HandleSubObject(i.Tile.gameObject, Matrix4x4.identity);
                    }
                }
                var outputMesh = new Mesh();
                outputMesh.CombineMeshes(combineInstances.ToArray(), true);
                allMeshes.Add(outputMesh);
                allCombine.Add(new CombineInstance
                {
                    mesh = outputMesh,
                    transform = Matrix4x4.identity,
                });
            }
            var finalMesh = new Mesh();
            finalMesh.CombineMeshes(allCombine.ToArray(), false);
            targetMeshFilter.mesh = finalMesh;
        }
    }
}
