using UnityEngine;

namespace MapEditor
{
    //A class for storing and retriving multiple speed values
    public class AdjustableSpeed
    {
        //The three speeds the camera can move at
        private float slowSpeed;
        private float normalSpeed;
        private float fastSpeed;

        //Calculate the three speeds based on a default speed and a multiplier
        public AdjustableSpeed(float defaultSpeed, float speedMultiplier)
        {
            normalSpeed = defaultSpeed;
            slowSpeed = defaultSpeed / speedMultiplier;
            fastSpeed = defaultSpeed * speedMultiplier;
        }

        //Set the speed based on if control or shift is held
        public float getSpeed()
        {
            if (Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl))
                return slowSpeed;
            else if (Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift))
                return fastSpeed;
            else
                return normalSpeed;
        }
    }
}