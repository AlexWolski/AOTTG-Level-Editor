using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour
{
    public AssetManager assetManager;

	void Start ()
    {
        StartCoroutine(TestInstantiate());
    }

    IEnumerator TestInstantiate()
    {
        yield return new WaitForSeconds(1);
        assetManager.instantiateRCAsset("cuboid", "ice1", 1f, 1f, new Vector3(1, 1, 1), new Vector3(-10, 0, 0), Quaternion.Euler(0, 0, 0));
        assetManager.instantiateRCAsset("cuboid", "ice1", 1f, 1f, new Vector3(1, 1, 1), new Vector3(10, 0, 0), Quaternion.Euler(0, 0, 0));
    }
	
	void Update ()
    {
	
	}
}
