using System.Collections;
using UnityEngine;

public class Test : MonoBehaviour
{
    private MapManager mapManager;

	void Start ()
    {
        StartCoroutine(TestInstantiate());
        mapManager = gameObject.GetComponent<MapManager>();
    }

    IEnumerator TestInstantiate()
    {
        yield return new WaitForSeconds(1);
        mapManager.loadMap("custom,cuboid,default,1,1,1,0,1,1,1,1.0,1.0,0,0,0,0,1,0,-2.8213E-07;");
    }
	
	void Update ()
    {
	
	}
}
