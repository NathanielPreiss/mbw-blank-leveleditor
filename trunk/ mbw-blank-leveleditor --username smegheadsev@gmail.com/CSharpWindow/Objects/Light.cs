using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class Light : Base
    {
        public enum LightType { None = -1, Point, Max };
        public LightType type;
        public float red, green, blue;
        public float intensity, radius;

        // Default Constructor
        public Light() : base()
        {
            red = green = blue = 255.0f;
            intensity = radius = 0.0f;
            type = LightType.Point;
        }
        // Constructor
        public Light(float _x, float _y, float _z, float _red, float _green, float _blue, float _intensity, float _radius, 
            LightType _type, string _name) : base(_x, _y, _z, _name)
        {
            red = _red;
            green = _green;
            blue = _blue;
            intensity = _intensity;
            radius = _radius;
            type = _type;
        }
        // Update Info String
        public new void UpdateInfo()
        {
            info = "";
            info += type; info += " Light";
            info += " X:"; info += x;
            info += " Y:"; info += y;
            info += " Z:"; info += z;
            info += " Intensity:"; info += intensity;
            info += " Radius:"; info += radius;
        }
        // ToString override
        public override string ToString()
        {
            UpdateInfo();
            return info;
        }
    }
}
