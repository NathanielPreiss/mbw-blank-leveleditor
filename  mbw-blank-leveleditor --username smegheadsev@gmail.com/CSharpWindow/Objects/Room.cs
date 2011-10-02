using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpWindow.Objects
{
    class Room : Base
    {
        public float height, width;
        public int backgroundID;
        public bool checkpoint;
        public float checkpointX, checkpointY;
        public int ambientRed, ambientGreen, ambientBlue;
        public List<Light> lights;
        public List<Wall> wallTiles;
        public List<GameObject> objects;
        public List<BackgroundObject> backgroundObjects;
        public List<Door> doors;
        public List<Enemy> enemies;
        public List<MovingPlatform> movingPlatforms;
        public List<Pickup> pickups;
        public List<Laser> lasers;
        public List<Crusher> crushers;
        public List<FallingSpike> fallingSpikes;
        public List<WindFan> windFans;
        
        // Default Constructor
        public Room()
        {
            height = width = 10;
            ambientRed = ambientGreen = ambientBlue = 255;
            backgroundID = -1;
            checkpoint = false;
            checkpointX = checkpointY = 0.0f;
            lights = new List<Light>();
            wallTiles = new List<Wall>();
            objects = new List<GameObject>();
            doors = new List<Door>();
            enemies = new List<Enemy>();
            movingPlatforms = new List<MovingPlatform>();
            backgroundObjects = new List<BackgroundObject>();
            pickups = new List<Pickup>();
            lasers = new List<Laser>();
            crushers = new List<Crusher>();
            fallingSpikes = new List<FallingSpike>();
            windFans = new List<WindFan>();
            UpdateInfo();
        }
        
        // Updates Info String
        public new void UpdateInfo()
        {
            info = name;
            info += " X:"; info += x;
            info += " Y:"; info += y;
            info += " Width:"; info += width;
            info += " Height:"; info += height;
        }
        // ToString override. Used for listboxes
        public override string ToString()
        {
            UpdateInfo();
            return info;
        }
    }
}
