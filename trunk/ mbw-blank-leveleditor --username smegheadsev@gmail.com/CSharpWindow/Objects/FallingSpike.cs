using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class FallingSpike : Base
    {
        public float frequency, speed;

        // Default Constructor
        public FallingSpike()
        {
            frequency = speed = 5.0f;
            x = y = z = 0.0f;
            info = name = "";
            UpdateInfo();
        }

        // Update Info String
        public new void UpdateInfo()
        {
            info  = "X:";  info += x;
            info += " Y:"; info += y;
            info += " Frequency:"; info += frequency;
            info += " Speed:"; info += speed;
        }

        // ToString override. Used for listboxes
        public override string ToString()
        {
            return info;
        }
    }
}