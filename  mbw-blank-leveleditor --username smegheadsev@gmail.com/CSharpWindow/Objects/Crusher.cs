using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class Crusher : Base
    {
        public float height, speed;

        // Default Constructor
        public Crusher()
        {
            speed = height = 1.0f;
            x = y = z = 0.0f;
            info = name = "";
            UpdateInfo();
        }

        // Update Info String
        public new void UpdateInfo()
        {
            info  = "X:";  info += x;
            info += " Y:"; info += y;
            info += " Height:"; info += height;
            info += " Speed:"; info += speed;
        }

        // ToString override. Used for listboxes
        public override string ToString()
        {
            UpdateInfo();
            return info;
        }
    }
}