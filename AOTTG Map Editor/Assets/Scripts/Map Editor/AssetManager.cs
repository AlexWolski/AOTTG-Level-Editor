using UnityEngine;
using OutlineEffect;

namespace MapEditor
{
    public static class AssetManager
    {
        //The locations of the prefabs and materials in the resources folder
        private readonly static string prefabFolder = "RC Resources/RC Prefabs/";
        private readonly static string materialFolder = "RC Resources/RC Materials/";

        //Instantiate the GameObject wtih the given name
        public static GameObject instantiateRcObject(string objectName)
        {
            //Instantiate the object
            GameObject newObject = Object.Instantiate(Resources.Load<GameObject>(prefabFolder + objectName));

            //If the gameobject has a mesh, add the outline script
            if (newObject.GetComponent<Renderer>() != null)
                newObject.AddComponent<Outline>();

            //Go through the children of the object and add the outline script if it has a mesh
            foreach (Transform child in newObject.transform)
            {
                if (child.GetComponent<Renderer>() != null)
                {
                    //Add the outline script to the object and give it a selectable tag
                    child.gameObject.AddComponent<Outline>();
                    child.gameObject.tag = "Selectable Object";
                }
            }

            //Add a Map Object tag to the object
            newObject.tag = "Map Object";

            return newObject;
        }

        //Load a material
        public static Material loadRcMaterial(string materialName)
        {
            return Resources.Load<Material>(materialFolder + materialName + "/" + materialName);
        }
    }
}