﻿using UnityEngine;
using System;
using System.Text;
using System.Globalization;

namespace MapEditor
{
    public abstract class MapObject : MonoBehaviour
    {
        #region Fields
        //The underlying values for properties and default values from the fbx prefab
        protected Vector3 defaultScale;
        protected Vector3 scaleFactor;
        #endregion

        #region Properties
        //The type of the object
        public ObjectType Type { get; set; }
        //The actual type name specified in the map script
        public string FullTypeName { get; set; }
        //The specific object
        public string ObjectName { get; set; }

        //Shorthand ways of accessing variables in the transform component
        public virtual Vector3 Scale
        {
            get { return scaleFactor; }
            set { scaleFactor = value; ScaleByFactor(value); }
        }

        public virtual Vector3 Position
        {
            get { return transform.position; }
            set { gameObject.transform.position = value; }
        }

        public virtual Quaternion Rotation
        {
            get { return transform.rotation; }
            set { gameObject.transform.rotation = value; }
        }
        #endregion

        #region Initialization
        //Copy the values from the given object
        public virtual void CopyValues(MapObject originalObject)
        {
            //Hidden data members
            defaultScale = originalObject.defaultScale;

            //MapObject properties
            Type = originalObject.Type;
            FullTypeName = originalObject.FullTypeName;
            ObjectName = originalObject.ObjectName;

            //GameObject properties
            scaleFactor = originalObject.Scale;
        }
        #endregion

        #region Setters
        //Change the scale factor of the length, width, or height of the object
        public void ScaleByFactor(Vector3 scaleFactor)
        {
            gameObject.transform.localScale = new Vector3(defaultScale.x * scaleFactor.x, defaultScale.y * scaleFactor.y, defaultScale.z * scaleFactor.z);
        }
        #endregion

        #region Parsing Utility Methods
        //An invariant version of ToSingle that always uses '.' as the decimal separator
        public static float ToSingleInvariant(string value)
        {
            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        //An invariant version of ToSTring that always uses the '.' as the decimal separator
        public static String ToStringInvariant(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        //Return the objectType associated with the given string
        public static ObjectType ParseType(string typeString)
        {
            //Make a string array containing the names of each type of object
            string[] objectTypes = Enum.GetNames(typeof(ObjectType));

            //Check if the string matches any of the types
            foreach (string objectType in objectTypes)
            {
                //If the string matches a type, return that type
                if (typeString.StartsWith(objectType))
                    return (ObjectType)Enum.Parse(typeof(ObjectType), objectType);
            }

            //If the object type is not valid, raise an error
            throw new Exception("The type '" + typeString + "' does not exist");
        }

        //Create a Color object with the three given color values. The opacity is always 1f.
        protected static Color ParseColorRGB(string r, string g, string b)
        {
            return new Color(ToSingleInvariant(r), ToSingleInvariant(g), ToSingleInvariant(b), 1f);
        }

        //Create a vector with the two given strings
        protected static Vector2 ParseVector2(string x, string y)
        {
            return new Vector2(ToSingleInvariant(x), ToSingleInvariant(y));
        }

        //Create a vector with the three given strings
        protected static Vector3 ParseVector3(string x, string y, string z)
        {
            return new Vector3(ToSingleInvariant(x), ToSingleInvariant(y), ToSingleInvariant(z));
        }

        //Create a quaternion with the three given strings
        protected static Quaternion ParseQuaternion(string x, string y, string z, string w)
        {
            return new Quaternion(ToSingleInvariant(x), ToSingleInvariant(y), ToSingleInvariant(z), ToSingleInvariant(w));
        }
        #endregion

        #region Exporting Utility Methods
        //Convert a boolean to the string 1 or 0
        protected string BoolToString(bool boolToStringify)
        {
            if (boolToStringify)
                return "1";

            return "0";
        }

        //Convert a color to a script friendly string
        protected string ColorToString(Color colorToStrinfigy)
        {
            return ToStringInvariant(colorToStrinfigy.r) + "," +
                   ToStringInvariant(colorToStrinfigy.g) + "," +
                   ToStringInvariant(colorToStrinfigy.b);
        }

        //Convert a vector2 to a script friendly string
        protected string Vector2ToString(Vector2 vectorToStringify)
        {
            return ToStringInvariant(vectorToStringify.x) + "," +
                   ToStringInvariant(vectorToStringify.y);
        }
        //Convert a vector2 to a script friendly string
        protected string Vector3ToString(Vector3 vectorToStringify)
        {
            return ToStringInvariant(vectorToStringify.x) + "," +
                   ToStringInvariant(vectorToStringify.y) + "," +
                   ToStringInvariant(vectorToStringify.z);
        }

        //Convert a vector2 to a script friendly string
        protected string QuaternionToString(Quaternion quatToStringify)
        {
            return ToStringInvariant(quatToStringify.x) + "," +
                   ToStringInvariant(quatToStringify.y) + "," +
                   ToStringInvariant(quatToStringify.z) + "," +
                   ToStringInvariant(quatToStringify.w);
        }
        #endregion

        #region Methods
        //Sets all of the object properties except for the type based on the parsed object script
        public virtual void LoadProperties(string[] properties)
        {
            //Store the full type
            FullTypeName = properties[0];
            //Store the object name
            ObjectName = properties[1];
            //Save the default scale of the object
            defaultScale = transform.localScale;
        }
        #endregion
    }
}