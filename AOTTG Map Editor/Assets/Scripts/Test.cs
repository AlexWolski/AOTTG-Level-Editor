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
        mapManager.loadMap("");
    }
	
	void Update ()
    {
	
	}
}
