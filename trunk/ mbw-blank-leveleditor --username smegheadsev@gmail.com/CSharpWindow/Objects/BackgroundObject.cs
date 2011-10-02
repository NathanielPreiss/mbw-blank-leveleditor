using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class BackgroundObject : Base
    {
        public int objectID;
        public int rotation;
        public int rotationy;

        // Default Constructor
        public BackgroundObject() : base()
        {
            objectID = 0;
            rotation = 0;
        }
        // Constructor
        public BackgroundObject(float _x, float _y, float _z, int _objectID, int _rotation, string _name) : base(_x, _y, _z, _name)
        {
            objectID = _objectID;
            rotation = _rotation;
        }
        // Update Info String
        public new void UpdateInfo()
        {
            info  = "X:";  info += x;
            info += " Y:"; info += y;
            info += " Z:"; info += z;
        }
        // ToString override
        public override string ToString()
        {
            UpdateInfo();
            return info;
        }
    }
}
