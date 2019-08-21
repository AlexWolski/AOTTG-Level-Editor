using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour
{
    private LevelManager levelManager;

	void Start ()
    {
        StartCoroutine(TestInstantiate());
        levelManager = gameObject.GetComponent<LevelManager>();
    }

    IEnumerator TestInstantiate()
    {
        yield return new WaitForSeconds(1);
        levelManager.loadLevel("");
        //AssetManager.instantiateRCAsset("cuboid", "earth1", new Vector3(1f, 1f, 1f), new Color(0, 1, 0), new Vector2(0.5f, 0.5f), new Vector3(-10, 0, 0), Quaternion.Euler(0, 0, 0));
        //AssetManager.instantiateRCAsset("arena1", "ice1", new Vector3(0.1f, 0.1f, 0.1f), null, null, new Vector3(10, 0, 0), Quaternion.Euler(0, 0, 0));
    }
	
	void Update ()
    {
	
	}
}
