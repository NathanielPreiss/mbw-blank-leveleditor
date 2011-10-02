using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class MovingPlatform : Base
    {
        public float endX, endY;
        public float speed;
        public int rotation;
        public List<Base> waypoints;
        // Default Constructor
        public MovingPlatform() : base()
        {
            waypoints = new List<Base>();
            speed = 0.0f;
            rotation = 0;
        }
        // Constructor
        public MovingPlatform(float _x, float _y, float _depth, float _speed, 
            int _rotation, string _name) : base(_x, _y, _depth, _name)
        {
            speed = _speed;
            rotation = _rotation;
        }
        // Update Info String
        public new void UpdateInfo()
        {
            info  = "Start X:";  info += x;
            info += " Start Y:"; info += y;
        }
        // ToString override
        public override string ToString()
        {
            UpdateInfo();
            return info;
        }
    }
}