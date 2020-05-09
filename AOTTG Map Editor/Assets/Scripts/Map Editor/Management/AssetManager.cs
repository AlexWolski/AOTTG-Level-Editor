using UnityEngine;
using OutlineEffect;

namespace MapEditor
{
    public static class AssetManager
    {
        //The locations of the vanilla resources
        private readonly static string vanillaPrefabFolder = "Vanilla Resources/Vanilla Prefabs/";
        private readonly static string vanillaMaterialFolder = "Vanilla Resources/Vanilla Materials/";
        //The locations of the RC resources
        private readonly static string rcPrefabFolder = "RC Resources/RC Prefabs/";
        private readonly static string rcMaterialFolder = "RC Resources/RC Materials/";

        //Instantiate the vanilla object with the given name
        public static GameObject InstantiateVanillaObject(string objectName)
        {
            GameObject newObject = Object.Instantiate(Resources.Load<GameObject>(vanillaPrefabFolder + objectName));
            AddObjectToMap(newObject);

            return newObject;
        }

        //Instantiate the RC object with the given name
        public static GameObject InstantiateRcObject(string objectName)
        {
            GameObject newObject = Object.Instantiate(Resources.Load<GameObject>(rcPrefabFolder + objectName));
            AddObjectToMap(newObject);

            return newObject;
        }

        //Make the given object selectable and tag it as a map object
        private static void AddObjectToMap(GameObject newObject)
        {
            //If the game object has a mesh, add the outline script
            if (newObject.GetComponent<Renderer>() != null)
            {
                newObject.AddComponent<Outline>();
                newObject.gameObject.tag = "Selectable";
            }

            //Go through the children of the object and add the outline script if it has a mesh
            foreach (Transform child in newObject.transform)
            {
                //Only select objects with a renderer that aren't particle systems
                if (child.GetComponent<ParticleSystem>() == null && child.GetComponent<Renderer>() != null)
                {
                    child.gameObject.AddComponent<Outline>();
                    child.gameObject.tag = "Selectable";
                }
            }
        }

        //Load the given vanilla material
        public static Material LoadVanillaMaterial(string materialName)
        {
            return Resources.Load<Material>(vanillaMaterialFolder + materialName + "/" + materialName);
        }

        //Load the given RC material
        public static Material LoadRcMaterial(string materialName)
        {
            return Resources.Load<Material>(rcMaterialFolder + materialName + "/" + materialName);
        }
    }
}