using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class Enemy : Base
    {
        public enum EnemyType { None = -1, Herp, Derp, Boss, Suicide_Herp, Max };
        
        public float endX, endY;
        public float speed;
        public EnemyType type;

        // Default Constructor
        public Enemy() : base()
        {
            endX = endY = speed = 0.0f;
            type = EnemyType.Herp;
        }
        // Constructor
        public Enemy(float _x, float _y, float _endX, float _endY, float _depth, float _speed, 
            EnemyType _type, string _name) : base(_x, _y, _depth, _name)
        {
            endX = _endX;
            endY = _endY;
            speed = _speed;
            type = _type;
        }
        // Update Info String
        public new void UpdateInfo()
        {
            info = ""; info += type;
            info += " Start X:"; info += x;
            info += " Start Y:"; info += y;
            info += " End X:"; info += endX;
            info += " End Y:"; info += endY;
        }
        // ToString override
        public override string ToString()
        {
            UpdateInfo();
            return info;
        }
    }
}
