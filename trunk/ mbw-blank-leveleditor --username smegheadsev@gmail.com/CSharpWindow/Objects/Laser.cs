using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class Laser : Base
    {
        public bool horizontal;
        public float length;
        public float timeOn, timeOff;

        // Default Constructor
        public Laser()
        {
            horizontal = true;
            timeOn = timeOff = 1.0f;
            x = y = z = 0.0f;
            length = 1.0f;
            info = name = "";
            UpdateInfo();
        }

        // Update Info String
        public new void UpdateInfo()
        {
            info  = "X:";  info += x;
            info += " Y:"; info += y;
            info += " Length:"; info += length;
            info += " Time On/Off:"; info += timeOn;
            info += " / "; info += timeOff;
        }

        // ToString override. Used for listboxes
        public override string ToString()
        {
            return info;
        }
    }
}
