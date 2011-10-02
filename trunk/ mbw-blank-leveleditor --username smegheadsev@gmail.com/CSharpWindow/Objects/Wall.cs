using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class Wall : Base
    {
        public int size;            // Length of the wall
        public bool horizontal;     // Flag for horizontal or vertical

        // Default Constructor
        public Wall() : base()
        {
            size = 1;
            horizontal = true;
        }
        // Constructor
        public Wall(float _x, float _y, float _z, int _size, bool _horizontal, string _name) : base(_x, _y, _z, _name)
        {
            size = _size;
            horizontal = _horizontal;
        }
        // Update Info String
        public new void UpdateInfo()
        {
            info = "Size:"; info += size;
            info += " X:"; info += x;
            info += " Y:"; info += y;
        }
        // ToString override
        public override string ToString()
        {
            UpdateInfo();
            return info;
        }
    }
}