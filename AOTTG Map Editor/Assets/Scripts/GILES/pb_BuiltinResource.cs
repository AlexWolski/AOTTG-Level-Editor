using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GILES
{
	/**
	 *	Load resources that are in the Required folder.  Resources are loaded by name,
	 *	which are prefixed with their asset type (ex, Material is  mat_MyMaterial,
	 *	Texture2D is tex_MyTexture, etc).
	 *	
	 *	Resource objects are returned by instance.  You do not have to destroy returned
	 *	instances, as this class will keep a reference retained to be returned when next
	 *	requested.  However, if the asset is destroyed (in the event that you know it 
	 *	won't be used again, for example), that is accounted for.
	 */
	public static class pb_BuiltinResource
	{
		/// An opaque lit material used to shade handles with a faux lambert shadow.
		public const string mat_HandleOpaque 		= "Materials/GILES/HandleOpaqueMaterial";
		/// A transparent unlit shader that fades based on normal and camera angle.  Used to draw sphere gizmos.
		public const string mat_RotateHandle 		= "Materials/GILES/HandleRotateMaterial";
		/// A transparent unlit shader.
		public const string mat_HandleTransparent 	= "Materials/GILES/HandleTransparentMaterial";

		/// The cone mesh used in creating the position handles.
		public const string mesh_Cone 				= "Handle Meshes/ConeMesh.asset";

		static Dictionary<string, UnityEngine.Object> pool = null;

		static pb_BuiltinResource()
		{
			pool = new Dictionary<string, UnityEngine.Object>();
		}

		/**
		 *	Load a built-in material with path (relative to Resources/Required) and instantiate it.
		 *	If a material has already been loaded, a reference to that instance will be returned.
		 */
		public static Material GetMaterial(string materialPath)
		{
			return LoadResource<Material>(materialPath);
		}

		/**
		 *	Load a required resource with path (relative to Resources/Required folder).
		 */
		public static T LoadResource<T>(string path) where T : UnityEngine.Object
		{
			Object val = null;

			if(pool.TryGetValue(path, out val))
			{
				if( val != null && val is T )
					return (T) val;
			}

			T obj = Resources.Load<T>(path);

			if(obj == null)
			{
				Debug.LogWarning("Built-in resource \"" + (path) + "\" not found!");
			}
			else
			{
				T instance = (T)GameObject.Instantiate(obj);
				pool.Add(path, instance);
				return instance;
			}

			return null;
		}

		/**
		 *	Fetch a required resource with path (relative to Resources/Required folder).  Does not instantiate object (use for
		 *	Texture2D for example).
		 */
		public static T GetResource<T>(string path) where T : UnityEngine.Object
		{
			Object val = null;

			if(pool.TryGetValue(path, out val))
			{
				if( val != null && val is T )
					return (T) val;
			}

			T obj = Resources.Load<T>(path);

			if(obj == null)
			{
				Debug.LogWarning("Built-in resource \"" + (path) + "\" not found!");
			}
			else
			{
				pool.Add(path, obj);
				return obj;
			}

			return null;
		}
	}
}
