using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tessera;
using System.Linq;
public class ADDParts : MonoBehaviour
{
    Vector3Int _currentPosition;
    public bool ObjectPLaced;
    TesseraTileInstance instance;
    private void Update()
    {
        AddShape();
    }
    public void AddShape()
    {
      
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit) && hit.transform.parent != null)
        {
            Debug.Log(hit.transform.parent.name);
            var tile = hit.collider.gameObject.GetComponentInParent<TesseraTile>();
            var fdetails = tile.faceDetails;
            var thisface = fdetails.Any(f=>f.faceDir==hit.normal);
            var _currentPosition = Vector3Int.RoundToInt(hit.transform.position) + Vector3Int.RoundToInt(hit.normal);

            _currentPosition = Vector3Int.RoundToInt(hit.transform.position) + Vector3Int.RoundToInt(hit.normal);
            ObjectPLaced = true;
        }
    }
}
