using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class Base
    {
        public float x, y, z;
        public string name;
        public string info;
        public Base()
        {
            name = "Untitled";
            info = "";
            x = y = z = 0.0f;
        }
        public Base(float _x, float _y, float _z, string _name)
        {
            x = _x;
            y = _y;
            z = _z;
            name = _name;
        }
        public void UpdateInfo()
        {
            info = name;
            info += " X:" + x;
            info += " Y:" + y;
            info += " Z:" + z;
        }
        public override string ToString()
        {
            UpdateInfo();
            return info;
        }
    }
}