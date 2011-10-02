using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class Door : Base
    {
        public int rotation;
        public int roomID;
        public float roomX, roomY;
        public bool breakable;

        // Default Constructor
        public Door()
        {
            x = y = 0;
            rotation = 0;
            roomID = -1;
            roomX = roomY = 0.0f;
            breakable = false;
            info = "";
            UpdateInfo();
        }

        // Update Info String
        public new void UpdateInfo()
        {
            if (breakable)
                info = "Breakable ";
            else
                info = "Unbreakable ";
            info += " X:"; info += x;
            info += " Y:"; info += y;
            info += " Connecting Room ID:"; info += roomID;
        }
        public override string ToString()
        {
            UpdateInfo();
            return info;
        }
    }
}
