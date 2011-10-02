using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class GameObject : Base
    {
        public enum GameObjectType { None = -1, Table, DNA_Chamber, Stationary_Platform, Moving_Platform, Spike, Max };
        public int objectID;
        public int rotation; // 0, 90, 180, 270
        public int flag;
        // Default Constructor
        public GameObject()
            : base()
        {
            flag = -1;
            objectID = 0;
            rotation = 0;
        }
        // Constructor
        public GameObject(float _x, float _y, float _z, string _name)
            : base(_x, _y, _z, _name)
        {

        }
        // Update Info String
        public new void UpdateInfo()
        {
            switch (objectID)
            {
                case 0:
                    info = "Table";
                    break;
                case 1:
                    info = "DNA Chamber";
                    break;
                case 2:
                    info = "Stationary Platform";
                    break;
                case 3:
                    info = "Moving Platform";
                    break;
                case 4:
                    info = "Spikes";
                    break;
            }
            info += " X:"; info += x;
            info += " Y:"; info += y;
        }
        // ToString override. Used for listboxes
        public override string ToString()
        {
            UpdateInfo();
            return info;
        }
    }
}