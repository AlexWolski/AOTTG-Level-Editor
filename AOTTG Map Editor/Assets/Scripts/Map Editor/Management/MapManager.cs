using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MapEditor
{
    //A singleton class for creating and deleting map objects
    public class MapManager : MonoBehaviour
    {
        #region Data Members
        //A self-reference to the singleton instance of this script
        public static MapManager Instance { get; private set; }

        //A reference to the root for all copied objects
        [SerializeField] private GameObject copiedObjectsRoot;
        //Determines if there are copied objects saved
        private bool selectionCopied = false;
        //A reference to the root for all deleted objects
        [SerializeField] private GameObject deletedObjectsRoot;
        //A reference to the empty map to add objects to
        [SerializeField] private GameObject mapRoot;
        //A reference to the billboard prefab
        [SerializeField] private GameObject billboardPrefab;

        //References to the large and small map boundaries
        [SerializeField] private GameObject smallMapBounds;
        [SerializeField] private GameObject largeMapBounds;

        //The minimum in-game frame rate while loading or exporting
        [SerializeField] private float loadingFPS = 60f;
        //The maximum amount of time a coroutine can run before returning. Calculated from minimum loading frame rate
        private float loadingDelay;

        //A dictionary mapping gameobjects to MapObject scripts
        public Dictionary<GameObject, MapObject> objectScriptTable { get; private set; }
        //Determines if the small map bounds have been disabled or not
        private bool boundsDisabled;
        #endregion

        #region Delegates
        public delegate void OnImportEvent(HashSet<GameObject> imported);
        public event OnImportEvent OnImport;

        public delegate void OnPasteEvent(HashSet<GameObject> pastedObjects);
        public event OnPasteEvent OnPaste;

        public delegate void OnDeleteEvent(HashSet<GameObject> deletedObjects);
        public event OnDeleteEvent OnDelete;
        #endregion

        #region Properties
        //Static properties to access private instance data members
        public Dictionary<GameObject, MapObject> ObjectScriptTable
        {
            get { return Instance.objectScriptTable; }
            private set { Instance.objectScriptTable = value; }
        }

        private bool BoundsDisabled
        {
            get { return Instance.boundsDisabled; }
            set { Instance.boundsDisabled = value; }
        }
        #endregion

        #region Initialization
        //Set this script as the only instance of the MapManager script
        void Awake()
        {
            if (Instance == null)
                Instance = this;

            //Insantiate the script table
            ObjectScriptTable = new Dictionary<GameObject, MapObject>();
            //Calcualte the delay between each frame while loading in milliseconds
            loadingDelay = 1f / loadingFPS * 1000;
        }
        #endregion

        #region Update
        void Update()
        {
            //Stores the command that needs to be executed
            EditCommand editCommand = null;

            //If the game is in edit mode, check for keyboard shortcut inputs
            if (EditorManager.Instance.currentMode == EditorMode.Edit)
            {
                //Check the delete keys
                if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
                {
                    //Only create a delete command if there are any selected objects
                    if (ObjectSelection.Instance.getSelectionCount() > 0)
                    {
                        editCommand = new DeleteSelection();
                        editCommand.executeEdit();
                    }
                }
                //Check for copy & paste shortcuts
                else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                {
                    if (Input.GetKeyDown(KeyCode.C))
                        copySelection();
                    else if (Input.GetKeyDown(KeyCode.V))
                    {
                        //Only paste if there are any copied objects
                        if (selectionCopied)
                        {
                            pasteSelection();
                            editCommand = new PasteSelection();
                        }
                    }
                }

                //If a selection was made, add it to the history
                if (editCommand != null)
                    EditHistory.Instance.addCommand(editCommand);
            }
        }
        #endregion

        #region Edit Commands
        //Delete and undelete the pasted objects
        private class PasteSelection : EditCommand
        {
            private GameObject[] pastedObjects;

            //Store the pasted objects (must be called after the objects are pasted)
            public PasteSelection()
            {
                this.pastedObjects = ObjectSelection.Instance.getSelection().ToArray();
            }

            //Undelete the pasted objects (must have been deleted first)
            public override void executeEdit()
            {
                Instance.undeleteObjects(pastedObjects);
            }

            //Remove the pasted objects by deleting them
            public override void revertEdit()
            {
                Instance.deleteSelection();
            }
        }

        //Delete the selected objects
        private class DeleteSelection : EditCommand
        {
            private GameObject[] deletedObjects;
            private bool deleted;

            public DeleteSelection()
            {
                //Save the objects to be deleted
                deletedObjects = ObjectSelection.Instance.getSelection().ToArray();
                //The objects haven't been deleted yet
                deleted = false;
            }

            //Destructor to delete the saved deleted objects 
            ~DeleteSelection()
            {
                //Destroy the objects if they were deleted when the command instance was destroyed
               if (deleted)
                {
                    foreach (GameObject mapObject in deletedObjects)
                        Destroy(mapObject);
                }
            }

            //Delete the current selection
            public override void executeEdit()
            {
                Instance.deleteSelection();
                deleted = true;
            }

            //Undelete the objects that were deleted
            public override void revertEdit()
            {
                Instance.undeleteObjects(deletedObjects);
                deleted = false;
            }
        }
        #endregion

        #region Copy/Paste/Delete Methods
        //Copy a selection by cloning all of the selected objects and storing them
        private void copySelection()
        {
            //Get a reference to the list of selected objects
            HashSet<GameObject> selectedObjects = ObjectSelection.Instance.getSelection();

            //If there aren't any objects to copy, return
            if (selectedObjects.Count == 0)
                return;

            //Destroy any previously copied objects
            foreach (Transform copiedObject in Instance.copiedObjectsRoot.transform)
                Destroy(copiedObject.gameObject);

            //Temporary GameObject to disable cloned objects before storing them
            GameObject objectClone;

            //Clone each selected object and save it in the copied objects list
            foreach (GameObject mapObject in selectedObjects)
            {
                //Instantiate and disable the copied objects
                objectClone = Instantiate(mapObject);
                objectClone.SetActive(false);
                //Get a reference to the cloned object's MapObject script
                MapObject mapObjectScript = objectClone.GetComponent<MapObject>();
                //Copy the values of the original map object script
                mapObjectScript.copyValues(mapObject.GetComponent<MapObject>());
                //Set the object as the child of the copied objects root
                objectClone.transform.parent = Instance.copiedObjectsRoot.transform;
            }

            selectionCopied = true;
        }

        //Paste the copied objects by instantiating them
        private void pasteSelection()
        {
            //Temporary GameObject to enable cloned objects before storing them
            GameObject objectClone;
            //Reset the current selection
            ObjectSelection.Instance.deselectAll();

            //Loop through all of the copied objects
            foreach (Transform copiedObject in Instance.copiedObjectsRoot.transform)
            {
                //Instantiate and enable the cloned object
                objectClone = Instantiate(copiedObject.gameObject);
                objectClone.SetActive(true);
                //Get a reference to the cloned object's MapObject script
                MapObject mapObjectScript = objectClone.GetComponent<MapObject>();
                //Copy the values of the original map object script
                mapObjectScript.copyValues(copiedObject.GetComponent<MapObject>());
                //Add the object to the map and make it selectable
                addObjectToMap(objectClone, mapObjectScript);
                ObjectSelection.Instance.selectObject(objectClone);
            }

            //Once the selection is pasted, change the tool type to translate
            ToolButtonManager.setTool(Tool.Translate);

            //Notify listners that the copied objects were pasted at the end of the frame
            StartCoroutine(InvokeOnPaste());
        }

        //Delete the selected objects
        private void deleteSelection()
        {
            //Deselect the selection and get a reference
            HashSet<GameObject> selectedObjects = ObjectSelection.Instance.removeSelected();

            //Move the objects under the deleted objects root and hide them
            foreach (GameObject objectToDelete in selectedObjects)
            {
                objectToDelete.transform.parent = Instance.deletedObjectsRoot.transform;
                objectToDelete.SetActive(false);
            }

            //Notify listners that the selected objects were deleted
            OnDelete?.Invoke(selectedObjects);
        }

        //Add the given objects back into the game
        private void undeleteObjects(GameObject[] deletedObjects)
        {
            //Make all the deleted objects selectable
            foreach(GameObject gameObject in deletedObjects)
                ObjectSelection.Instance.addSelectable(gameObject);

            //Activate the object, move it back into the level, and select it
            foreach (GameObject mapObject in deletedObjects)
            {
                mapObject.SetActive(true);
                mapObject.transform.parent = Instance.mapRoot.transform;
                ObjectSelection.Instance.selectObject(mapObject);
            }
        }
        #endregion

        #region Event Invocation
        private IEnumerator InvokeOnImport()
        {
            //Wait until the pasted objects are rendered
            yield return new WaitForEndOfFrame();

            //Notify listners that the copied objects were pasted
            OnImport?.Invoke(ObjectSelection.Instance.getSelectable());
        }

        private IEnumerator InvokeOnPaste()
        {
            //Wait until the pasted objects are rendered
            yield return new WaitForEndOfFrame();

            //Notify listners that the copied objects were pasted
            OnPaste?.Invoke(ObjectSelection.Instance.getSelection());
        }
        #endregion

        #region Map Methods
        //Delete all of the map objects
        public void clearMap()
        {
            //Remove all deleted objects from the selection lists
            ObjectSelection.Instance.resetSelection();
            //Reset the hash table for MapObject scripts
            ObjectScriptTable = new Dictionary<GameObject, MapObject>();
            //Reset the boundaries disabled flag and activate the small bounds
            BoundsDisabled = false;
            enableLargeMapBounds(false);

            //Iterate over all children objects and delete them
            foreach (Transform child in Instance.mapRoot.GetComponentInChildren<Transform>())
                Destroy(child.gameObject);
        }

        //Parse the given map script and load the map
        //Accepts additional parameters for outputing loading progress, total amount of map objects, and a callback function
        public IEnumerator loadMap(string mapScript, Text progressText = null, Text totalText = null, Action callback = null)
        {
            //Used to keep track of how much time has elapsed between frames
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            //Remove all of the new lines and spaces in the script
            mapScript = mapScript.Replace("\n", "");
            mapScript = mapScript.Replace("\r", "");
            mapScript = mapScript.Replace("\t", "");
            mapScript = mapScript.Replace(" ", "");

            //Seperate the map by semicolon
            string[] parsedMap = mapScript.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            //Create each object and add it to the map
            for (int scriptIndex = 0; scriptIndex < parsedMap.Length; scriptIndex++)
            {
                try
                {
                    //Display how many objects have been loaded
                    progressText.text = scriptIndex.ToString();

                    //If the object script starts with '//' ignore it
                    if (parsedMap[scriptIndex].StartsWith("//"))
                        continue;

                    //Parse the object script and create a new map object
                    MapObject mapObjectScript;
                    GameObject newMapObject = loadObject(parsedMap[scriptIndex], out mapObjectScript);

                    //If the object is defined, add it to the map hierarchy and make it selectable
                    if (newMapObject)
                        addObjectToMap(newMapObject, mapObjectScript);
                }
                catch (Exception e)
                {
                    //If there was an issue parsing the object, log the error and skip it
                    Debug.Log("Skipping object on line " + scriptIndex + "\t(" + parsedMap[scriptIndex] + ")");
                    Debug.Log(e + ":\t" + e.Message);
                }

                //Get the time elapsed since last frame
                stopWatch.Stop();

                //Check if enough time has passed to start a new frame
                if (stopWatch.ElapsedMilliseconds > loadingDelay)
                {
                    //Update the total number of objects in the script
                    totalText.text = parsedMap.Length.ToString();
                    //Return from the corouting and render a frame
                    yield return null;
                    //Start counting the elapsed time from zero
                    stopWatch.Restart();
                }
                //Otherwise, resume measuring the elapsed time
                else
                    stopWatch.Start();
            }

            //Notify listners that a map was loaded at the end of the frame
            StartCoroutine(InvokeOnImport());
            //Run the callback method
            callback();
        }

        //Add the given object to the map hierarchy and make it selectable
        private void addObjectToMap(GameObject objectToAdd, MapObject objectScript)
        {
            //Make the new object a child of the map root.
            objectToAdd.transform.parent = Instance.mapRoot.transform;
            //Make the new object selectable
            ObjectSelection.Instance.addSelectable(objectToAdd);
            //Add the object and its MapObject script to the dictionary
            ObjectScriptTable.Add(objectToAdd, objectScript);
        }

        //Remove the given object to the map hierarchy and make object selection script
        private void removeObjectFromMap(GameObject objectToRemove)
        {
            //Remove the object from the object selection script
            ObjectSelection.Instance.removeSelectable(objectToRemove);
            //Remove the object from the script dictionary
            ObjectScriptTable.Remove(objectToRemove);
            //Delete the object itself
            Destroy(objectToRemove);
        }

        //Parse the given object script and instantiate a new GameObject with the data
        private GameObject loadObject(string objectScript, out MapObject mapObjectScript)
        {
            //Seperate the object script by comma
            string[] parsedObject = objectScript.Split(',');
            //The GameObject loaded from RCAssets corresponding to the object name
            GameObject newObject = null;
            //The type of the object
            objectType type;

            try
            {
                //If the script is "map,disableBounds" then set a flag to disable the map boundries and skip the object
                if (parsedObject[0].StartsWith("map") && parsedObject[1].StartsWith("disablebounds"))
                {
                    BoundsDisabled = true;
                    enableLargeMapBounds(true);
                    mapObjectScript = null;

                    return null;
                }

                //If the length of the string is too short, raise an error
                if (parsedObject.Length < 9)
                    throw new Exception("Too few elements in object script");

                //Parse the object type
                type = MapObject.parseType(parsedObject[0]);

                //Use the object name to load the asset
                newObject = createMapObject(type, parsedObject[1]);
                //Get the MapObject script attached to the new GameObject
                mapObjectScript = newObject.GetComponent<MapObject>();

                //Use the parsedObject array to set the reset of the properties of the object
                mapObjectScript.loadProperties(parsedObject);

                //Check if the object is a region
                if (type == objectType.misc && parsedObject[1] == "region")
                {
                    //Give the region a default rotation
                    mapObjectScript.Rotation = Quaternion.identity;

                    //intantiate a billboard and set it as a child of the region
                    GameObject billboard = Instantiate(Instance.billboardPrefab);
                    billboard.GetComponent<TextMesh>().text = mapObjectScript.RegionName;
                    billboard.transform.parent = newObject.transform;
                }

                return newObject;
            }
            //If there was an error converting an element to a float, destroy the object and pass a new exception to the caller
            catch (FormatException)
            {
                destroyObject(newObject);
                throw new Exception("Error conveting data");
            }
            //If there are any other errors, destroy the object and pass them back up to the caller
            catch (Exception e)
            {
                destroyObject(newObject);
                throw e;
            }
        }

        //Convert the map into a script
        public override string ToString()
        {
            //Create a string builder to efficiently construct the script
            //Initialize with a starting buffer with enough room to fit a large map script
            StringBuilder scriptBuilder = new StringBuilder(100000);

            //If bounds are disabled, append the bounds disable script
            if (BoundsDisabled)
                scriptBuilder.AppendLine("map,disablebounds;");

            //Append the script for each object to the map script
            foreach (MapObject objectScript in ObjectScriptTable.Values)
                scriptBuilder.AppendLine(objectScript.ToString());

            //Get the script string and return it
            return scriptBuilder.ToString();
        }
        #endregion

        #region Parser Helpers
        //If the object exists, disable and destroy it
        private static void destroyObject(GameObject objectToDestroy)
        {
            if (objectToDestroy)
            {
                objectToDestroy.SetActive(false);
                Destroy(objectToDestroy);
            }
        }

        //Toggle between the small and large map bounds being active
        private static void enableLargeMapBounds(bool enabled)
        {
            Instance.smallMapBounds.SetActive(!enabled);
            Instance.largeMapBounds.SetActive(enabled);
        }

        //Load the GameObject from RCAssets with the corresponding object name and attach a MapObject script to it
        private static GameObject createMapObject(objectType type, string objectName)
        {
            //The GameObject loaded from RCAssets corresponding to the object name
            GameObject newObject;

            //If the object is a vanilla object, instantiate it from the vanilla assets
            if (type == objectType.@base)
            {
                newObject = AssetManager.instantiateVanillaObject(objectName);
            }
            //If the object is a barrier or region, instantiate editor version
            else if (objectName == "barrier" || objectName == "region")
            {
                newObject = AssetManager.instantiateRcObject(objectName + "Editor");
            }
            //Otherwise, instantiate the object from teh RC assets
            else
                newObject = AssetManager.instantiateRcObject(objectName);

            //If the object name wasn't valid, raise an error
            if (!newObject)
                throw new Exception("The object '" + objectName + "' does not exist");

            //Attatch the MapObject script to the new object
            MapObject mapObjectScript = newObject.AddComponent<MapObject>();
            //Set the type of the mapObject
            mapObjectScript.Type = type;

            //Return the new object 
            return newObject;
        }
        #endregion
    }
}