using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class WindFan : Base
    {
        public float length;            // Length of the wind tunnel
        public float speed;            // Speed of the wind
        public float rotation;

        // Default Constructor
        public WindFan() : base()
        {
            length = speed = 1.0f;
            rotation = 0.0f;
        }
        // Constructor
        public WindFan(float _x, float _y, float _z, float _length, float _speed, float _rotation, string _name)
            : base(_x, _y, _z, _name)
        {
            speed = _speed;
            length = _length;
            rotation = _rotation;
        }
        // Update Info String
        public new void UpdateInfo()
        {
            info = " X:"; info += x;
            info += " Y:"; info += y;
            info += " Length:"; info += length;
            info += " Speed:"; info += speed;
        }
        // ToString override
        public override string ToString()
        {
            UpdateInfo();
            return info;
        }
    }
}