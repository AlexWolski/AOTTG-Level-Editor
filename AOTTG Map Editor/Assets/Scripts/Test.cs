using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour
{
    [SerializeField]
    private  AssetManager assetManager;

	void Start ()
    {
        StartCoroutine(TestInstantiate());
    }

    IEnumerator TestInstantiate()
    {
        yield return new WaitForSeconds(1);
        assetManager.instantiateRCAsset("cuboid", "earth1", 1f, 1f, new Vector3(2f, 2f, 2f), new Vector3(-10, 0, 0), Quaternion.Euler(0, 0, 0));
        assetManager.instantiateRCAsset("arena1", "earth1", 1f, 1f, new Vector3(0.1f, 0.1f, 0.1f), new Vector3(10, 0, 0), Quaternion.Euler(0, 0, 0));
    }
	
	void Update ()
    {
	
	}
}
