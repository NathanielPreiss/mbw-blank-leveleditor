using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class Pickup : Base
    {
        public enum PickupTypes { None = -1, Health, Max };
        public PickupTypes type;

        // Default Constructor
        public Pickup() : base()
        {
            type = PickupTypes.Health;
        }
        // Constructor
        public Pickup(float _x, float _y, float _z, PickupTypes _type, 
            string _name) : base(_x, _y, _z, _name)
        {
            type = _type;
        }
        // Update Info String
        public new void UpdateInfo()
        {
            info = "Type:"; info += type;
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