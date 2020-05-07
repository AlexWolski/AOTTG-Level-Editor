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
        private readonly static string RcPrefabFolder = "RC Resources/RC Prefabs/";
        private readonly static string RcMaterialFolder = "RC Resources/RC Materials/";

        //Instantiate the vanilla object wtih the given name
        public static GameObject instantiateVanillaObject(string objectName)
        {
            GameObject newObject = Object.Instantiate(Resources.Load<GameObject>(vanillaPrefabFolder + objectName));
            addObjectToMap(newObject);

            return newObject;
        }

        //Instantiate the RC object wtih the given name
        public static GameObject instantiateRcObject(string objectName)
        {
            GameObject newObject = Object.Instantiate(Resources.Load<GameObject>(RcPrefabFolder + objectName));
            addObjectToMap(newObject);

            return newObject;
        }

        //Make the given object selectable and tag it as a map object
        private static void addObjectToMap(GameObject newObject)
        {
            //If the gameobject has a mesh, add the outline script
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
        public static Material loadVanillaMaterial(string materialName)
        {
            return Resources.Load<Material>(vanillaMaterialFolder + materialName + "/" + materialName);
        }

        //Load the given RC material
        public static Material loadRcMaterial(string materialName)
        {
            return Resources.Load<Material>(RcMaterialFolder + materialName + "/" + materialName);
        }
    }
}