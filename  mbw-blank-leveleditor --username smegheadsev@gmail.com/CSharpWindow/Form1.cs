using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Windows;
using System.IO;
using CSharpWindow.Objects;

namespace CSharpWindow
{
    public partial class Form1 : Form
    {
        // Current save file version
        int VERSION_NUMBER = 3;
        enum VECTORENUM { VECTOR_NONE = 0, VECTOR_ROOM, VECTOR_BGOBJECT, VECTOR_MOVINGPLATFORM, VECTOR_GAMEOBJECT, VECTOR_WALL, VECTOR_ENEMY,
        VECTOR_LIGHT, VECTOR_DOOR, VECTOR_PICKUP, VECTOR_LASER, VECTOR_CRUSHER, VECTOR_WINDFAN, VECTOR_MAX };
        enum TOOLENUM { TOOL_NONE = -1, TOOL_TRANSLATE, TOOL_ROTATE, TOOL_SCALE, TOOL_MAX };
        /****************************************************************/
        #region DLL Functions
        /****************************************************************/

        [DllImport("PInvoke.dll")]
        public static extern void _clearAllData();

        [DllImport("PInvoke.dll")]
        public static extern void _updateSpawn(int _roomID, float _x, float _y);

        [DllImport("PInvoke.dll")]
        public static extern void _deleteObject(int _roomID, int _index, int _vectorID);
        
        [DllImport("PInvoke.dll")]
        public static extern void _objectPicking(float _x, float _y, ref int _vectorID, ref int _objectID);
        
        [DllImport("PInvoke.dll")]
        public static extern void _updateRoom(int _roomID, float _x, float _y, float _height, float _width, bool _cp, float _cpX, float _cpY, int _BGID);

        [DllImport("PInvoke.dll")]
        public static extern void _updateWallTile(int _roomID, int _index, float _x, float _y, bool _horizontal, int _tileSize);

        [DllImport("PInvoke.dll")]
        public static extern void _updateGameObject(int _roomID, int _index, float _x, float _y, int _rotation, int _objectID);

        [DllImport("PInvoke.dll")]
        public static extern void _updateLight(int _roomID, int _index, int _type, float _x, float _y, float _z, float _red, float _green, float _blue, float _radius, float _intensity);
       
        [DllImport("PInvoke.dll")]
        public static extern void _updateMovingPlatform(int _roomID, int _index, float _startX, float _startY, float _endX, float _endY, int _rotation, float _speed);
       
        [DllImport("PInvoke.dll")]
        public static extern void _updateDoor(int _roomID, int _index, int _spawnRoomID, bool _breakable, float _x, float _y, float _roomX, float _roomY, int _rotation);

        [DllImport("PInvoke.dll")]
        public static extern void _updateEnemy(int _roomID, int _index, int _type, float _startX, float _startY, float _endX, float _endY, float _speed);

        [DllImport("PInvoke.dll")]
        public static extern void _updateBackgroundObject(int _roomID, int _index, int _type, float _x, float _y, float _z, float _rotX, float _rotY, float _rotZ);

        [DllImport("PInvoke.dll")]
        public static extern void _updatePickup(int _roomID, int _index, float _x, float _y, float _z, int _pickupType);

        [DllImport("PInvoke.dll")]
        public static extern void _updateCrusher(int _roomID, int _index, float _x, float _y, float _z, float _height, float _speed);
        
        [DllImport("PInvoke.dll")]
        public static extern void _updateLaser(int _roomID, int _index, bool _horizontal, float _x, float _y, float _z, float _length, float _timeOn, float _timeOff);

        [DllImport("PInvoke.dll")]
        public static extern void _updateWindFan(int _roomID, int _index, float _x, float _y, float _z, float _rot, float _length, float _speed);

        /****************************************************************/
        #endregion 
        /****************************************************************/

        /****************************************************************/
        #region Needed Objects
        /****************************************************************/
        private static List<Room> roomList;
        private static int spawnRoomID;
        private static float spawnX, spawnY;
        private static RadioPlayer radioPlayer;
        private static Point lastCursorPos;
        private static bool lockToGrid;
        private static TOOLENUM selectedTool;
       
        /****************************************************************/
        #endregion
        /****************************************************************/

        /****************************************************************/
        #region Helper Functions
        /****************************************************************/

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch(keyData)
            {
                case Keys.Control | Keys.Shift | Keys.N:
                    if (RoomGroupBox.Contains(this.ActiveControl))
                        this.NewRoomButton.PerformClick();
                    else if (LightGroupBox.Contains(this.ActiveControl))
                        this.NewLightButton.PerformClick();
                    else if (WallGroupBox.Contains(this.ActiveControl))
                        this.AddWallButton.PerformClick();
                    else if (GameObjectGroupBox.Contains(this.ActiveControl))
                        this.AddGameObjectButton.PerformClick();
                    return true;

            }
            return false;
        }

        private void resetAllData()
        {
            _clearAllData();
            roomList.Clear();
            RoomListBox.Items.Clear();
            WallListBox.Items.Clear();
            GameObjectListBox.Items.Clear();
            DoorListBox.Items.Clear();
            MovingPlatformListBox.Items.Clear();
            RoomLightListBox.Items.Clear();
            EnemyListBox.Items.Clear();
            BackgroundObjectListBox.Items.Clear();
            LaserListBox.Items.Clear();
            CrusherListBox.Items.Clear();
            PickupListBox.Items.Clear();
        }

        private void saveFile(bool _txtFile)
        {
            SaveFileDialog output = new SaveFileDialog();
            output.AddExtension = true;
            output.DefaultExt = (_txtFile) ? ".txt" : ".lvl";
            output.Filter = (_txtFile) ? "Text file (*.txt)|*.txt" : "Level file (*.lvl)|*.lvl";
            if (DialogResult.OK == output.ShowDialog())
            {
                StreamWriter outFile = new StreamWriter(output.FileName.ToString());
                outFile.WriteLine(VERSION_NUMBER);
                outFile.WriteLine(spawnRoomID);
                outFile.WriteLine(spawnX);
                outFile.WriteLine(spawnY);
                // Export for each room
                outFile.WriteLine(roomList.Count);
                for (int roomIter = 0; roomIter < roomList.Count; roomIter++)
                {
                    outFile.WriteLine(roomList[roomIter].x);
                    outFile.WriteLine(roomList[roomIter].y);
                    outFile.WriteLine(roomList[roomIter].width);
                    outFile.WriteLine(roomList[roomIter].height);
                    outFile.WriteLine(roomList[roomIter].backgroundID);
                    outFile.WriteLine(Convert.ToInt32(roomList[roomIter].checkpoint));
                    outFile.WriteLine(roomList[roomIter].checkpointX);
                    outFile.WriteLine(roomList[roomIter].checkpointY);
                    if(_txtFile)
                        outFile.WriteLine(roomList[roomIter].name);

                    // In each room export every wall
                    outFile.WriteLine(roomList[roomIter].wallTiles.Count);
                    for (int wallIter = 0; wallIter < roomList[roomIter].wallTiles.Count; wallIter++)
                    {
                        outFile.WriteLine(roomList[roomIter].wallTiles[wallIter].x);
                        outFile.WriteLine(roomList[roomIter].wallTiles[wallIter].y);
                        outFile.WriteLine(roomList[roomIter].wallTiles[wallIter].size);
                        outFile.WriteLine(Convert.ToInt32(roomList[roomIter].wallTiles[wallIter].horizontal));
                    }
                    // In each room export every light
                    outFile.WriteLine(roomList[roomIter].lights.Count);
                    for (int lightIter = 0; lightIter < roomList[roomIter].lights.Count; lightIter++)
                    {
                        outFile.WriteLine(Convert.ToInt32(roomList[roomIter].lights[lightIter].type));
                        outFile.WriteLine(roomList[roomIter].lights[lightIter].x);
                        outFile.WriteLine(roomList[roomIter].lights[lightIter].y);
                        outFile.WriteLine(roomList[roomIter].lights[lightIter].z);
                        outFile.WriteLine(roomList[roomIter].lights[lightIter].red);
                        outFile.WriteLine(roomList[roomIter].lights[lightIter].green);
                        outFile.WriteLine(roomList[roomIter].lights[lightIter].blue);
                        outFile.WriteLine(roomList[roomIter].lights[lightIter].radius);
                        outFile.WriteLine(roomList[roomIter].lights[lightIter].intensity);
                    }
                    // In each room export every game object
                    outFile.WriteLine(roomList[roomIter].objects.Count);
                    for (int objectIter = 0; objectIter < roomList[roomIter].objects.Count; objectIter++)
                    {
                        outFile.WriteLine(roomList[roomIter].objects[objectIter].x);
                        outFile.WriteLine(roomList[roomIter].objects[objectIter].y);
                        outFile.WriteLine(roomList[roomIter].objects[objectIter].objectID);
                        outFile.WriteLine(roomList[roomIter].objects[objectIter].rotation);
                        outFile.WriteLine(roomList[roomIter].objects[objectIter].flag);
                    }
                    // In each room export every door
                    outFile.WriteLine(roomList[roomIter].doors.Count);
                    for (int doorIter = 0; doorIter < roomList[roomIter].doors.Count; doorIter++)
                    {
                        outFile.WriteLine(roomList[roomIter].doors[doorIter].x);
                        outFile.WriteLine(roomList[roomIter].doors[doorIter].y);
                        outFile.WriteLine(Convert.ToInt32(roomList[roomIter].doors[doorIter].breakable));
                        outFile.WriteLine(roomList[roomIter].doors[doorIter].rotation);
                        outFile.WriteLine(roomList[roomIter].doors[doorIter].roomID);
                        outFile.WriteLine(roomList[roomIter].doors[doorIter].roomX);
                        outFile.WriteLine(roomList[roomIter].doors[doorIter].roomY);
                    }
                    // In each room export every platform
                    outFile.WriteLine(roomList[roomIter].movingPlatforms.Count);
                    for (int platformIter = 0; platformIter < roomList[roomIter].movingPlatforms.Count; platformIter++)
                    {
                        outFile.WriteLine(roomList[roomIter].movingPlatforms[platformIter].waypoints.Count+1);
                        outFile.WriteLine(roomList[roomIter].movingPlatforms[platformIter].x);
                        outFile.WriteLine(roomList[roomIter].movingPlatforms[platformIter].y);
                        for (int waypointIter = 0; waypointIter < roomList[roomIter].movingPlatforms[platformIter].waypoints.Count; waypointIter++)
                        {
                            outFile.WriteLine(roomList[roomIter].movingPlatforms[platformIter].waypoints[waypointIter].x);
                            outFile.WriteLine(roomList[roomIter].movingPlatforms[platformIter].waypoints[waypointIter].y);
                        }
                        //outFile.WriteLine(roomList[roomIter].movingPlatforms[platformIter].endX);
                        //outFile.WriteLine(roomList[roomIter].movingPlatforms[platformIter].endY);
                        outFile.WriteLine(roomList[roomIter].movingPlatforms[platformIter].rotation);
                        outFile.WriteLine(roomList[roomIter].movingPlatforms[platformIter].speed);
                    }
                    // In each room export every enemy
                    outFile.WriteLine(roomList[roomIter].enemies.Count);
                    for (int enemyIter = 0; enemyIter < roomList[roomIter].enemies.Count; enemyIter++)
                    {
                        outFile.WriteLine(roomList[roomIter].enemies[enemyIter].x);
                        outFile.WriteLine(roomList[roomIter].enemies[enemyIter].y);
                        outFile.WriteLine(roomList[roomIter].enemies[enemyIter].endX);
                        outFile.WriteLine(roomList[roomIter].enemies[enemyIter].endY);
                        outFile.WriteLine((int)roomList[roomIter].enemies[enemyIter].type);
                        outFile.WriteLine(roomList[roomIter].enemies[enemyIter].speed);
                    }
                    // In each room export every background object
                    outFile.WriteLine(roomList[roomIter].backgroundObjects.Count);
                    for (int objectIter = 0; objectIter < roomList[roomIter].backgroundObjects.Count; objectIter++)
                    {
                        outFile.WriteLine(roomList[roomIter].backgroundObjects[objectIter].x);
                        outFile.WriteLine(roomList[roomIter].backgroundObjects[objectIter].y);
                        outFile.WriteLine(roomList[roomIter].backgroundObjects[objectIter].z);
                        outFile.WriteLine(roomList[roomIter].backgroundObjects[objectIter].rotation);
                        outFile.WriteLine(0);   // quick replacement. used to have roty and rotz but no longer using 
                        outFile.WriteLine(0);   // those variables need to update save files
                        outFile.WriteLine(roomList[roomIter].backgroundObjects[objectIter].objectID);
                    }
                    //
                    outFile.WriteLine(roomList[roomIter].crushers.Count);
                    for (int objectIter = 0; objectIter < roomList[roomIter].crushers.Count; objectIter++)
                    {
                        outFile.WriteLine(roomList[roomIter].crushers[objectIter].x);
                        outFile.WriteLine(roomList[roomIter].crushers[objectIter].y);
                        outFile.WriteLine(roomList[roomIter].crushers[objectIter].z);
                        outFile.WriteLine(roomList[roomIter].crushers[objectIter].height);
                        outFile.WriteLine(roomList[roomIter].crushers[objectIter].speed);
                    }
                    //
                    outFile.WriteLine(roomList[roomIter].lasers.Count);
                    for (int objectIter = 0; objectIter < roomList[roomIter].lasers.Count; objectIter++)
                    {
                        outFile.WriteLine(roomList[roomIter].lasers[objectIter].x);
                        outFile.WriteLine(roomList[roomIter].lasers[objectIter].y);
                        outFile.WriteLine(roomList[roomIter].lasers[objectIter].z);
                        outFile.WriteLine(roomList[roomIter].lasers[objectIter].length);
                        outFile.WriteLine(roomList[roomIter].lasers[objectIter].timeOn);
                        outFile.WriteLine(roomList[roomIter].lasers[objectIter].timeOff);
                        outFile.WriteLine(Convert.ToInt32(roomList[roomIter].lasers[objectIter].horizontal));
                    }
                    //
                    outFile.WriteLine(roomList[roomIter].pickups.Count);
                    for (int objectIter = 0; objectIter < roomList[roomIter].pickups.Count; objectIter++)
                    {
                        outFile.WriteLine(roomList[roomIter].pickups[objectIter].x);
                        outFile.WriteLine(roomList[roomIter].pickups[objectIter].y);
                        outFile.WriteLine(roomList[roomIter].pickups[objectIter].z);
                        outFile.WriteLine(Convert.ToInt32(roomList[roomIter].pickups[objectIter].type));
                    }
                    // 
                    outFile.WriteLine(roomList[roomIter].windFans.Count);
                    for (int objectIter = 0; objectIter < roomList[roomIter].windFans.Count; objectIter++)
                    {
                        outFile.WriteLine(roomList[roomIter].windFans[objectIter].x);
                        outFile.WriteLine(roomList[roomIter].windFans[objectIter].y);
                        outFile.WriteLine(roomList[roomIter].windFans[objectIter].z);
                        outFile.WriteLine(roomList[roomIter].windFans[objectIter].rotation);
                        outFile.WriteLine(roomList[roomIter].windFans[objectIter].speed);
                        outFile.WriteLine(roomList[roomIter].windFans[objectIter].length);
                    }

                }
                outFile.Close();
            }
        }

        private void loadFile(bool _txtFile)
        {
            OpenFileDialog input = new OpenFileDialog();
            input.CheckFileExists = true;
            input.CheckPathExists = true;
            input.AddExtension = true;
            input.DefaultExt = (_txtFile) ? ".txt" : ".lvl";
            input.Filter = (_txtFile) ? "Text file (*.txt)|*.txt" : "Level file (*.lvl)|*.lvl";
            int tempSpawnID;
            float tempSpawnX, tempSpawnY;
            if (DialogResult.OK == input.ShowDialog())
            {
                resetAllData();
                StreamReader inFile = new StreamReader(input.FileName.ToString());
                int fileVersion = Convert.ToInt32(inFile.ReadLine());
                int numRooms = 0;

                tempSpawnID = Convert.ToInt32(inFile.ReadLine());
                tempSpawnX = Convert.ToInt32(inFile.ReadLine());
                tempSpawnY = Convert.ToInt32(inFile.ReadLine());

                // Import every room
                numRooms = Convert.ToInt32(inFile.ReadLine());
                SpawnRoomNUD.Maximum = (decimal)numRooms;
                for (int roomIter = 0; roomIter < numRooms; roomIter++)
                {
                    Room newRoom = new Room();
                    newRoom.x = Convert.ToInt32(inFile.ReadLine());
                    newRoom.y = Convert.ToInt32(inFile.ReadLine());
                    newRoom.width = Convert.ToInt32(inFile.ReadLine());
                    newRoom.height = Convert.ToInt32(inFile.ReadLine());
                    newRoom.backgroundID = Convert.ToInt32(inFile.ReadLine());
                    newRoom.checkpoint = Convert.ToBoolean(Convert.ToInt32(inFile.ReadLine()));
                    newRoom.checkpointX = Convert.ToInt32(inFile.ReadLine());
                    newRoom.checkpointY = (float)Convert.ToDecimal(inFile.ReadLine());
                    if(_txtFile)
                        newRoom.name = Convert.ToString(inFile.ReadLine());
                    roomList.Add(newRoom);
                    RoomListBox.Items.Add(newRoom);
                    _updateRoom(-1, newRoom.x, newRoom.y, newRoom.height, newRoom.width, newRoom.checkpoint, newRoom.checkpointX, newRoom.checkpointY, newRoom.backgroundID);

                    // Import every wall
                    int numWallTiles = Convert.ToInt32(inFile.ReadLine());
                    for (int wallIter = 0; wallIter < numWallTiles; wallIter++)
                    {
                        Wall newWall = new Wall();
                        newWall.x = Convert.ToInt32(inFile.ReadLine());
                        newWall.y = Convert.ToInt32(inFile.ReadLine());
                        newWall.size = Convert.ToInt32(inFile.ReadLine());
                        newWall.horizontal = Convert.ToBoolean(Convert.ToInt32(inFile.ReadLine()));
                        roomList[roomIter].wallTiles.Add(newWall);
                        //WallListBox.Items.Add(newRoom.info);
                        _updateWallTile(roomIter, -1, newWall.x, newWall.y, newWall.horizontal, newWall.size);
                    }

                    // Import every light
                    int numLights = Convert.ToInt32(inFile.ReadLine());
                    for (int lightIter = 0; lightIter < numLights; lightIter++)
                    {
                        Light newLight = new Light();
                        newLight.type = (Light.LightType)Convert.ToInt32(inFile.ReadLine());
                        newLight.x = (float)Convert.ToDecimal(inFile.ReadLine());
                        newLight.y = (float)Convert.ToDecimal(inFile.ReadLine());
                        newLight.z = (float)Convert.ToDecimal(inFile.ReadLine());
                        newLight.red = (float)Convert.ToDecimal(inFile.ReadLine());
                        newLight.green = (float)Convert.ToDecimal(inFile.ReadLine());
                        newLight.blue = (float)Convert.ToDecimal(inFile.ReadLine());
                        newLight.radius = (float)Convert.ToDecimal(inFile.ReadLine());
                        newLight.intensity = (float)Convert.ToDecimal(inFile.ReadLine());
                        roomList[roomIter].lights.Add(newLight);
                        //RoomLightListBox.Items.Add(newLight);
                        _updateLight(roomIter, -1, (int)newLight.type, newLight.x, newLight.y, newLight.z, newLight.red, newLight.green, newLight.blue, newLight.radius, newLight.intensity);
                    }

                    // Import every game object
                    int numGameObjects = Convert.ToInt32(inFile.ReadLine());
                    for (int objectIter = 0; objectIter < numGameObjects; objectIter++)
                    {
                        GameObject newObject = new GameObject();
                        newObject.x = Convert.ToInt32(inFile.ReadLine());
                        newObject.y = Convert.ToInt32(inFile.ReadLine());
                        newObject.objectID = Convert.ToInt32(inFile.ReadLine());
                        newObject.rotation = Convert.ToInt32(inFile.ReadLine());
                        newObject.flag = Convert.ToInt32(inFile.ReadLine());
                        roomList[roomIter].objects.Add(newObject);
                        //GameObjectListBox.Items.Add(newObject);
                        _updateGameObject(roomIter, -1, newObject.x, newObject.y, newObject.rotation, newObject.objectID);
                    }

                    // Import every door
                    int numDoors = Convert.ToInt32(inFile.ReadLine());
                    for (int doorIter = 0; doorIter < numDoors; doorIter++)
                    {
                        Door newDoor = new Door();
                        newDoor.x = Convert.ToInt32(inFile.ReadLine());
                        newDoor.y = Convert.ToInt32(inFile.ReadLine());
                        newDoor.breakable = Convert.ToBoolean(Convert.ToInt32(inFile.ReadLine()));
                        newDoor.rotation = Convert.ToInt32(inFile.ReadLine());
                        newDoor.roomID = Convert.ToInt32(inFile.ReadLine());
                        newDoor.roomX = (float)Convert.ToDecimal(inFile.ReadLine());
                        newDoor.roomY = (float)Convert.ToDecimal(inFile.ReadLine());
                        roomList[roomIter].doors.Add(newDoor);
                        //DoorListBox.Items.Add(newDoor.info);
                        _updateDoor(roomIter, -1, newDoor.roomID, newDoor.breakable, newDoor.x, newDoor.y, newDoor.roomX, newDoor.roomY, newDoor.rotation);
                    }

                    // Import every platform
                    int numPlatforms = Convert.ToInt32(inFile.ReadLine());
                    for (int platformIter = 0; platformIter < numPlatforms; platformIter++)
                    {
                        MovingPlatform newPlatform = new MovingPlatform();
                        int waypointCount = Convert.ToInt32(inFile.ReadLine());
                        newPlatform.x = (float)Convert.ToDecimal(inFile.ReadLine());
                        newPlatform.y = (float)Convert.ToDecimal(inFile.ReadLine());
                        for (int waypointIter = 0; waypointIter < waypointCount-1; waypointIter++)
                        {
                            Base newWaypoint = new Base();
                            newWaypoint.x = (float)Convert.ToDecimal(inFile.ReadLine());
                            newWaypoint.y = (float)Convert.ToDecimal(inFile.ReadLine());
                            newPlatform.waypoints.Add(newWaypoint);
                        }
                        newPlatform.rotation = Convert.ToInt32(inFile.ReadLine());
                        newPlatform.speed = Convert.ToInt32(inFile.ReadLine());
                        roomList[roomIter].movingPlatforms.Add(newPlatform);
                        //MovingPlatformListBox.Items.Add(newPlatform);
                        _updateMovingPlatform(roomIter, -1, newPlatform.x, newPlatform.y, newPlatform.endX, newPlatform.endY, newPlatform.rotation, newPlatform.speed);
                    }

                    // Import every
                    int numEnemies = Convert.ToInt32(inFile.ReadLine());
                    for (int enemyIter = 0; enemyIter < numEnemies; enemyIter++)
                    {
                        Enemy newEnemy = new Enemy();
                        newEnemy.x = (float)Convert.ToDecimal(inFile.ReadLine());
                        newEnemy.y = (float)Convert.ToDecimal(inFile.ReadLine());
                        newEnemy.endX = (float)Convert.ToDecimal(inFile.ReadLine());
                        newEnemy.endY = (float)Convert.ToDecimal(inFile.ReadLine());
                        newEnemy.type = (Enemy.EnemyType)Convert.ToInt32(inFile.ReadLine());
                        newEnemy.speed = Convert.ToInt32(inFile.ReadLine());
                        roomList[roomIter].enemies.Add(newEnemy);
                        //EnemyListBox.Items.Add(newEnemy);
                        _updateEnemy(roomIter, -1, (int)newEnemy.type, newEnemy.x, newEnemy.y, newEnemy.endX, newEnemy.endY, newEnemy.speed);
                    }

                    // Import every BG Object
                    int numBGObjects = Convert.ToInt32(inFile.ReadLine());
                    for (int BGOjectIter = 0; BGOjectIter < numBGObjects; BGOjectIter++)
                    {
                        BackgroundObject newBackgroundObject = new BackgroundObject();
                        newBackgroundObject.x = (float)Convert.ToDecimal(inFile.ReadLine());
                        newBackgroundObject.y = (float)Convert.ToDecimal(inFile.ReadLine());
                        newBackgroundObject.z = (float)Convert.ToDecimal(inFile.ReadLine());
                        newBackgroundObject.rotation = Convert.ToInt32(inFile.ReadLine());
                        Convert.ToInt32(inFile.ReadLine()); // quick replace. see save out for more comment
                        Convert.ToInt32(inFile.ReadLine());
                        newBackgroundObject.objectID = Convert.ToInt32(inFile.ReadLine());
                        roomList[roomIter].backgroundObjects.Add(newBackgroundObject);
                        //BackgroundObjectListBox.Items.Add(newBackgroundObject);
                        _updateBackgroundObject(roomIter, -1, newBackgroundObject.objectID, newBackgroundObject.x, newBackgroundObject.y, newBackgroundObject.z, newBackgroundObject.rotation, 0.0f, 0.0f);
                    }
                    int numCrushers = Convert.ToInt32(inFile.ReadLine());
                    for (int objectIter = 0; objectIter < numCrushers; objectIter++)
                    {
                        Crusher newCrusher = new Crusher();
                        newCrusher.x = (float)Convert.ToDecimal(inFile.ReadLine());
                        newCrusher.y = (float)Convert.ToDecimal(inFile.ReadLine());
                        newCrusher.z = (float)Convert.ToDecimal(inFile.ReadLine());
                        newCrusher.height = (float)Convert.ToDecimal(inFile.ReadLine());
                        newCrusher.speed = (float)Convert.ToDecimal(inFile.ReadLine());
                        roomList[roomIter].crushers.Add(newCrusher);
                        //CrusherListBox.Items.Add(newCrusher);
                        _updateCrusher(roomIter, -1, newCrusher.x, newCrusher.y, newCrusher.z, newCrusher.height, newCrusher.speed);
                    }
                    //
                    int numLasers = Convert.ToInt32(inFile.ReadLine());
                    for (int objectIter = 0; objectIter < numLasers; objectIter++)
                    {
                        Laser newLaser = new Laser();
                        newLaser.x = (float)Convert.ToDecimal(inFile.ReadLine());
                        newLaser.y = (float)Convert.ToDecimal(inFile.ReadLine());
                        newLaser.z = (float)Convert.ToDecimal(inFile.ReadLine());
                        newLaser.length = (float)Convert.ToDecimal(inFile.ReadLine());
                        newLaser.timeOn = (float)Convert.ToDecimal(inFile.ReadLine());
                        newLaser.timeOff = (float)Convert.ToDecimal(inFile.ReadLine());
                        newLaser.horizontal = Convert.ToBoolean(Convert.ToInt32(inFile.ReadLine()));
                        roomList[roomIter].lasers.Add(newLaser);
                        //LaserListBox.Items.Add(newLaser);
                        _updateLaser(roomIter, -1, newLaser.horizontal, newLaser.x, newLaser.y, newLaser.z, newLaser.length, newLaser.timeOn, newLaser.timeOff);
                    }
                    //
                    int numPickups = Convert.ToInt32(inFile.ReadLine());
                    for (int objectIter = 0; objectIter < numPickups; objectIter++)
                    {
                        Pickup newPickup = new Pickup();
                        newPickup.x = (float)Convert.ToDecimal(inFile.ReadLine());
                        newPickup.y = (float)Convert.ToDecimal(inFile.ReadLine());
                        newPickup.z = (float)Convert.ToDecimal(inFile.ReadLine());
                        newPickup.type = (Pickup.PickupTypes)Convert.ToInt32(inFile.ReadLine());
                        roomList[roomIter].pickups.Add(newPickup);
                        //PickupListBox.Items.Add(newPickup);
                        _updatePickup(roomIter, -1, newPickup.x, newPickup.y, newPickup.z, (int)newPickup.type);
                    }
                    int numFans = Convert.ToInt32(inFile.ReadLine());
                    for (int objectIter = 0; objectIter < numFans; objectIter++)
                    {
                        WindFan newFan = new WindFan();
                        newFan.x = (float)Convert.ToDecimal(inFile.ReadLine());
                        newFan.y = (float)Convert.ToDecimal(inFile.ReadLine());
                        newFan.z = (float)Convert.ToDecimal(inFile.ReadLine());
                        newFan.rotation = (float)Convert.ToDecimal(inFile.ReadLine());
                        newFan.speed = (float)Convert.ToDecimal(inFile.ReadLine());
                        newFan.length = (float)Convert.ToDecimal(inFile.ReadLine());
                        roomList[roomIter].windFans.Add(newFan);
                        //WindFanListBox.Items.Add(newFan);
                        _updateWindFan(roomIter, -1, newFan.x, newFan.y, newFan.z, newFan.rotation, newFan.length, newFan.speed);
                    }
                }
                SpawnRoomNUD.Value = tempSpawnID;
                SpawnRoomXNUD.Value = (decimal)tempSpawnX;
                SpawnRoomYNUD.Value = (decimal)tempSpawnY;
                
                inFile.Close();
            }
        }
        
        public Form1()
        { 
            InitializeComponent();
            roomList = new List<Room>();
            spawnRoomID = -1;
            spawnX = spawnY = 0.0f;
            radioPlayer = null;
            selectedTool = TOOLENUM.TOOL_NONE; 
        }

        /****************************************************************/
        #endregion
        /****************************************************************/

        /****************************************************************/
        #region Menu Strip Functions
        /****************************************************************/

        private void SaveLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFile(true);
        }
       
        private void OpenLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadFile(true);
        }
        
        private void exportLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFile(false);
        }
        
        private void importLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadFile(false);
        }
        
        /****************************************************************/
        #endregion
        /****************************************************************/

        /****************************************************************/
        #region Room Setting Functions
        /****************************************************************/

        private void RoomListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;

            // Set All
            RoomXNUD.ValueChanged -= Room_Update;
            RoomYNUD.ValueChanged -= Room_Update;
            RoomWidthNUD.ValueChanged -= Room_Update;
            RoomHeightNUD.ValueChanged -= Room_Update;
            RoomBackgroundComboBox.SelectedIndexChanged -= Room_Update;
            RoomCheckpoint.CheckedChanged -= Room_Update;
            CheckpointXNUD.ValueChanged -= Room_Update;
            CheckpointYNUD.ValueChanged -= Room_Update;
            RoomNameTextBox.TextChanged -= Room_Update;
            
            RoomWidthNUD.Value = (decimal)roomList[selectedRoom].width;
            RoomHeightNUD.Value = (decimal)roomList[selectedRoom].height;
            RoomXNUD.Value = (decimal)roomList[selectedRoom].x;
            RoomYNUD.Value = (decimal)roomList[selectedRoom].y;
            RoomBackgroundComboBox.SelectedIndex = roomList[selectedRoom].backgroundID;
            RoomCheckpoint.Checked = roomList[selectedRoom].checkpoint;
            CheckpointXNUD.Value = (decimal)roomList[selectedRoom].checkpointX;
            CheckpointYNUD.Value = (decimal)roomList[selectedRoom].checkpointY;
            RoomNameTextBox.Text = roomList[selectedRoom].name;
            
            RoomXNUD.ValueChanged += Room_Update;
            RoomYNUD.ValueChanged += Room_Update;
            RoomWidthNUD.ValueChanged += Room_Update;
            RoomHeightNUD.ValueChanged += Room_Update;
            RoomBackgroundComboBox.SelectedIndexChanged += Room_Update;
            RoomCheckpoint.CheckedChanged += Room_Update;
            CheckpointXNUD.ValueChanged += Room_Update;
            CheckpointYNUD.ValueChanged += Room_Update;
            RoomNameTextBox.TextChanged += Room_Update;
            
            // Load in rooms lights
            RoomLightListBox.Items.Clear();
            for (int i = 0; i < roomList[selectedRoom].lights.Count; i++)
                RoomLightListBox.Items.Add(roomList[selectedRoom].lights[i]);
           
            // Load in rooms wall tiles
            WallListBox.Items.Clear();
            for (int i = 0; i < roomList[selectedRoom].wallTiles.Count; i++)
                WallListBox.Items.Add(roomList[selectedRoom].wallTiles[i]);
            
            // Load in rooms doors
            DoorListBox.Items.Clear();
            for (int i = 0; i < roomList[selectedRoom].doors.Count; i++)
                DoorListBox.Items.Add(roomList[selectedRoom].doors[i]);
            
            // Load in rooms game objects
            GameObjectListBox.Items.Clear();
            for (int i = 0; i < roomList[selectedRoom].objects.Count; i++)
                GameObjectListBox.Items.Add(roomList[selectedRoom].objects[i]);
            
            // Load in rooms enemies
            EnemyListBox.Items.Clear();
            for (int i = 0; i < roomList[selectedRoom].enemies.Count; i++)
                EnemyListBox.Items.Add(roomList[selectedRoom].enemies[i]);
            
            // Load in rooms moving platforms
            MovingPlatformListBox.Items.Clear();
            MovingPlatformWaypointListBox.Items.Clear();
            for (int i = 0; i < roomList[selectedRoom].movingPlatforms.Count; i++)
                MovingPlatformListBox.Items.Add(roomList[selectedRoom].movingPlatforms[i]);
            
            // Load in rooms background objects
            BackgroundObjectListBox.Items.Clear();
            for (int i = 0; i < roomList[selectedRoom].backgroundObjects.Count; i++)
                BackgroundObjectListBox.Items.Add(roomList[selectedRoom].backgroundObjects[i]);
            
            // Load in rooms pickups
            PickupListBox.Items.Clear();
            for (int i = 0; i < roomList[selectedRoom].pickups.Count; i++)
                PickupListBox.Items.Add(roomList[selectedRoom].pickups[i]);
            
            // Load in rooms lasers
            LaserListBox.Items.Clear();
            for (int i = 0; i < roomList[selectedRoom].lasers.Count; i++)
                LaserListBox.Items.Add(roomList[selectedRoom].lasers[i]);
           
            // Load in rooms crushers
            CrusherListBox.Items.Clear();
            for (int i = 0; i < roomList[selectedRoom].crushers.Count; i++)
                CrusherListBox.Items.Add(roomList[selectedRoom].crushers[i]);
            
            // Load in rooms wind fans
            WindFanListBox.Items.Clear();
            for (int i = 0; i < roomList[selectedRoom].windFans.Count; i++)
                WindFanListBox.Items.Add(roomList[selectedRoom].windFans[i]);
        }

        private void NewRoomButton_Click(object sender, EventArgs e)
        {
            // Create a new room
            Room newRoom = new Room();
            // Add to C#
            roomList.Add(newRoom);
            // Add to list box
            RoomListBox.Items.Add(newRoom);
            // Add to C++
            _updateRoom(-1, newRoom.x, newRoom.y, newRoom.height, newRoom.width, newRoom.checkpoint, 
                newRoom.checkpointX, newRoom.checkpointY, newRoom.backgroundID);
            // Update the cap on list boxes
            SpawnRoomNUD.Maximum  = roomList.Count - 1;
            DoorRoomIDNUD.Maximum = roomList.Count - 1;
        }   
        
        private void DeleteRoomButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            // Remove from C#
            roomList.RemoveAt(selectedRoom);
            // Remove from list box
            RoomListBox.Items.RemoveAt(selectedRoom);
            // Remove from C++
            _deleteObject(selectedRoom, -1, (int)VECTORENUM.VECTOR_ROOM);
            // Clear our list boxes
            WallListBox.Items.Clear();
            DoorListBox.Items.Clear();
            RoomLightListBox.Items.Clear();
            GameObjectListBox.Items.Clear();
            MovingPlatformListBox.Items.Clear();
            // Update and check Room IDs
            if (SpawnRoomNUD.Value == roomList.Count)
                SpawnRoomNUD.Value = -1;

            for (int roomIter = 0; roomIter < roomList.Count; roomIter++ )
            {
                for (int doorIter = 0; doorIter < roomList[roomIter].doors.Count; doorIter++ )
                {
                    if (roomList[roomIter].doors[doorIter].roomID == roomList.Count)
                        roomList[roomIter].doors[doorIter].roomID = -1;
                }
            }
            // Reduce the available room IDs
            SpawnRoomNUD.Maximum = roomList.Count - 1;
            DoorRoomIDNUD.Maximum = roomList.Count - 1;
        }

        private void SpawnRoomNUD_ValueChanged(object sender, EventArgs e)
        {
            // Update spawn room ID
            spawnRoomID = (int)SpawnRoomNUD.Value;
            _updateSpawn(spawnRoomID, spawnX, spawnY);
        }

        private void SpawnRoomXNUD_ValueChanged(object sender, EventArgs e)
        {
            // Update spawn room x
            spawnX = (float)SpawnRoomXNUD.Value;
            _updateSpawn(spawnRoomID, spawnX, spawnY);
        }

        private void SpawnRoomYNUD_ValueChanged(object sender, EventArgs e)
        {
            // Update spawn room y
            spawnY = (float)SpawnRoomYNUD.Value;
            _updateSpawn(spawnRoomID, spawnX, spawnY);
        }
        
        private void Room_Update(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            Room currRoom = roomList[selectedRoom];
            // Update C#

            currRoom.x = (float)RoomXNUD.Value;
            currRoom.y = (float)RoomYNUD.Value;
            currRoom.z = (float)0.0f;
            currRoom.width = (float)RoomWidthNUD.Value;
            currRoom.height = (float)RoomHeightNUD.Value;
            currRoom.backgroundID = RoomBackgroundComboBox.SelectedIndex;
            currRoom.checkpoint = RoomCheckpoint.Checked;
            currRoom.checkpointX = (float)CheckpointXNUD.Value;
            currRoom.checkpointY = (float)CheckpointYNUD.Value;
            currRoom.name = RoomNameTextBox.Text;

            // Update list box
            RoomListBox.Items[selectedRoom] = currRoom;
            // Update C++
            _updateRoom(selectedRoom, currRoom.x, currRoom.y, currRoom.height, currRoom.width, currRoom.checkpoint,
                currRoom.checkpointX, currRoom.checkpointY, currRoom.backgroundID);
        }
        
        /****************************************************************/
        #endregion
        /****************************************************************/

        /****************************************************************/
        #region Lighting Setting Functions
        /****************************************************************/
        
        private void RoomLightListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedLight = RoomLightListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedLight == -1)
                return;
            // Set All
            LightTypeComboBox.SelectedIndexChanged -= Light_Update;
            LightPosXNUD.ValueChanged -= Light_Update;
            LightPosYNUD.ValueChanged -= Light_Update;
            LightPosZNUD.ValueChanged -= Light_Update;
            LightRedNUD.ValueChanged -= Light_Update;
            LightGreenNUD.ValueChanged -= Light_Update;
            LightBlueNUD.ValueChanged -= Light_Update;
            LightBrightnessNUD.ValueChanged -= Light_Update;

            LightTypeComboBox.SelectedIndex = (int)roomList[selectedRoom].lights[selectedLight].type;
            LightPosXNUD.Value = (decimal)roomList[selectedRoom].lights[selectedLight].x;
            LightPosYNUD.Value = (decimal)roomList[selectedRoom].lights[selectedLight].y;
            LightPosZNUD.Value = (decimal)roomList[selectedRoom].lights[selectedLight].z;
            LightRedNUD.Value = (decimal)roomList[selectedRoom].lights[selectedLight].red;
            LightGreenNUD.Value = (decimal)roomList[selectedRoom].lights[selectedLight].green;
            LightBlueNUD.Value = (decimal)roomList[selectedRoom].lights[selectedLight].blue;
            LightBrightnessNUD.Value = (decimal)roomList[selectedRoom].lights[selectedLight].intensity;

            LightTypeComboBox.SelectedIndexChanged += Light_Update;
            LightPosXNUD.ValueChanged += Light_Update;
            LightPosYNUD.ValueChanged += Light_Update;
            LightPosZNUD.ValueChanged += Light_Update;
            LightRedNUD.ValueChanged += Light_Update;
            LightGreenNUD.ValueChanged += Light_Update;
            LightBlueNUD.ValueChanged += Light_Update;
            LightBrightnessNUD.ValueChanged += Light_Update;
        }

        private void NewLightButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            // Create a new light
            Light newLight = new Light();
            // Add to C#
            roomList[selectedRoom].lights.Add(newLight);
            // Add to list box
            RoomLightListBox.Items.Add(newLight);
            // Add to C++
            _updateLight(selectedRoom, -1, (int)newLight.type, newLight.x, newLight.y, newLight.z, newLight.red, newLight.green, 
                newLight.blue, newLight.radius, newLight.intensity);
        }

        private void DeleteLightButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedLight = RoomLightListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedLight == -1)
                return;
            // Remove from C#
            roomList[selectedRoom].lights.RemoveAt(selectedLight);
            // Remove from list box
            RoomLightListBox.Items.RemoveAt(selectedLight);
            // Remove from C++
            _deleteObject(selectedRoom, selectedLight, (int)VECTORENUM.VECTOR_LIGHT);
        }
        
        private void Light_Update(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedLight = RoomLightListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedLight == -1)
                return;
            Light currLight = roomList[selectedRoom].lights[selectedLight];
            // Update C#
            currLight.x = (float)LightPosXNUD.Value;
            currLight.y = (float)LightPosYNUD.Value;
            currLight.z = (float)LightPosZNUD.Value;
            currLight.red = (float)LightRedNUD.Value;
            currLight.green = (float)LightGreenNUD.Value;
            currLight.blue = (float)LightBlueNUD.Value;
            currLight.type = (Light.LightType)LightTypeComboBox.SelectedIndex;
            currLight.radius = (float)LightRadiusNUD.Value;
            currLight.intensity = (float)LightBrightnessNUD.Value;
            // Update list box
            RoomLightListBox.Items[selectedLight] = currLight;
            // Update C++
            _updateLight(selectedRoom, selectedLight, (int)currLight.type, currLight.x, currLight.y, currLight.z, currLight.red, currLight.green,
                currLight.blue, currLight.radius, currLight.intensity);
        }

        /****************************************************************/
        #endregion
        /****************************************************************/

        /****************************************************************/
        #region Spotlighting Setting Functions
        /****************************************************************/

        private void SpotlightListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedSpotlight = spotlightListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedSpotlight == -1)
                return;
            // Set All
            spotlightBrightnessNUD.ValueChanged -= Spotlight_Update;
            spotlightRadiusNUD.ValueChanged -= Spotlight_Update;
            spotlightGreenNUD.ValueChanged -= Spotlight_Update;
            spotlightInnerNUD.ValueChanged -= Spotlight_Update;
            spotlightOuterNUD.ValueChanged -= Spotlight_Update;
            spotlightPosXNUD.ValueChanged -= Spotlight_Update;
            spotlightPosYNUD.ValueChanged -= Spotlight_Update;
            spotlightPosZNUD.ValueChanged -= Spotlight_Update;
            spotlightDirXNUD.ValueChanged -= Spotlight_Update;
            spotlightDirYNUD.ValueChanged -= Spotlight_Update;
            spotlightDirZNUD.ValueChanged -= Spotlight_Update;
            spotlightBlueNUD.ValueChanged -= Spotlight_Update;
            spotlightRedNUD.ValueChanged -= Spotlight_Update;


            spotlightBrightnessNUD.ValueChanged += Spotlight_Update;
            spotlightRadiusNUD.ValueChanged += Spotlight_Update;
            spotlightGreenNUD.ValueChanged += Spotlight_Update;
            spotlightInnerNUD.ValueChanged += Spotlight_Update;
            spotlightOuterNUD.ValueChanged += Spotlight_Update;
            spotlightPosXNUD.ValueChanged += Spotlight_Update;
            spotlightPosYNUD.ValueChanged += Spotlight_Update;
            spotlightPosZNUD.ValueChanged += Spotlight_Update;
            spotlightDirXNUD.ValueChanged += Spotlight_Update;
            spotlightDirYNUD.ValueChanged += Spotlight_Update;
            spotlightDirZNUD.ValueChanged += Spotlight_Update;
            spotlightBlueNUD.ValueChanged += Spotlight_Update;
            spotlightRedNUD.ValueChanged += Spotlight_Update;
        }

        private void NewspotLightButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            // Create a new light
            Light newLight = new Light();
            // Add to C#
            roomList[selectedRoom].lights.Add(newLight);
            // Add to list box
            RoomLightListBox.Items.Add(newLight);
            // Add to C++
            _updateLight(selectedRoom, -1, (int)newLight.type, newLight.x, newLight.y, newLight.z, newLight.red, newLight.green,
                newLight.blue, newLight.radius, newLight.intensity);
        }

        private void DeleteSpotlightButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedLight = RoomLightListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedLight == -1)
                return;
            // Remove from C#
            roomList[selectedRoom].lights.RemoveAt(selectedLight);
            // Remove from list box
            RoomLightListBox.Items.RemoveAt(selectedLight);
            // Remove from C++
            _deleteObject(selectedRoom, selectedLight, (int)VECTORENUM.VECTOR_LIGHT);
        }

        private void Spotlight_Update(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedLight = RoomLightListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedLight == -1)
                return;
            Light currLight = roomList[selectedRoom].lights[selectedLight];
            // Update C#
            currLight.x = (float)LightPosXNUD.Value;
            currLight.y = (float)LightPosYNUD.Value;
            currLight.z = (float)LightPosZNUD.Value;
            currLight.red = (float)LightRedNUD.Value;
            currLight.green = (float)LightGreenNUD.Value;
            currLight.blue = (float)LightBlueNUD.Value;
            currLight.type = (Light.LightType)LightTypeComboBox.SelectedIndex;
            currLight.radius = (float)LightRadiusNUD.Value;
            currLight.intensity = (float)LightBrightnessNUD.Value;
            // Update list box
            RoomLightListBox.Items[selectedLight] = currLight;
            // Update C++
            _updateLight(selectedRoom, selectedLight, (int)currLight.type, currLight.x, currLight.y, currLight.z, currLight.red, currLight.green,
                currLight.blue, currLight.radius, currLight.intensity);
        }

        /****************************************************************/
        #endregion
        /****************************************************************/

        /****************************************************************/
        #region Door Functions
        /****************************************************************/

        private void DoorListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedDoor = DoorListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedDoor == -1)
                return;
            // Set All
            DoorPosXNUD.Value   = (decimal)roomList[selectedRoom].doors[selectedDoor].x;
            DoorPosYNUD.Value = (decimal)roomList[selectedRoom].doors[selectedDoor].y;
            DoorRoomIDNUD.Value = (int)roomList[selectedRoom].doors[selectedDoor].roomID;
            DoorSpawnXNUD.Value = (decimal)roomList[selectedRoom].doors[selectedDoor].roomX;
            DoorSpawnYNUD.Value = (decimal)roomList[selectedRoom].doors[selectedDoor].roomY;
            DoorBreakableCheckbox.Checked = roomList[selectedRoom].doors[selectedDoor].breakable;
            switch (roomList[selectedRoom].doors[selectedDoor].rotation)
            {
                case 0:
                    DoorFacingComboBox.SelectedIndex = 0;
                    break;
                case 90:
                    DoorFacingComboBox.SelectedIndex = 1;
                    break;
                case 180:
                    DoorFacingComboBox.SelectedIndex = 2;
                    break;
                case 270:
                    DoorFacingComboBox.SelectedIndex = 3;
                    break;
            }
        }

        private void AddDoorButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            // Create a new door
            Door newDoor = new Door();
            // Add to C#
            roomList[selectedRoom].doors.Add(newDoor);
            // Add to list box
            DoorListBox.Items.Add(newDoor.info);
            // Add to C++
            _updateDoor(selectedRoom, -1, newDoor.roomID, newDoor.breakable, newDoor.x, newDoor.y, newDoor.roomX, newDoor.roomY, newDoor.rotation);
        }

        private void DeleteDoorButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedDoor = DoorListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedDoor == -1)
                return;
            // Remove from C#
            roomList[selectedRoom].doors.RemoveAt(selectedDoor);
            // Remove from list box
            DoorListBox.Items.RemoveAt(selectedDoor);
            // Remove from C++
            _deleteObject(selectedRoom, selectedDoor, (int)VECTORENUM.VECTOR_DOOR);
        }
       
        private void DoorPosXNUD_ValueChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedDoor = DoorListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedDoor == -1)
                return;
            Door currDoor = roomList[selectedRoom].doors[selectedDoor];
            // Update C#
            currDoor.x = (float)DoorPosXNUD.Value;
            // Update list box
            roomList[selectedRoom].doors[selectedDoor].UpdateInfo();
            DoorListBox.Items.RemoveAt(selectedDoor);
            DoorListBox.Items.Insert(selectedDoor, roomList[selectedRoom].doors[selectedDoor].info);
            DoorListBox.SelectedIndex = selectedDoor;
            // Update C++
            _updateDoor(selectedRoom, selectedDoor, currDoor.roomID, currDoor.breakable, currDoor.x, currDoor.y, currDoor.roomX, currDoor.roomY, currDoor.rotation);
        }

        private void DoorPosYNUD_ValueChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedDoor = DoorListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedDoor == -1)
                return;
            Door currDoor = roomList[selectedRoom].doors[selectedDoor];
            // Update C#
            currDoor.y = (float)DoorPosYNUD.Value;
            // Update list box
            roomList[selectedRoom].doors[selectedDoor].UpdateInfo();
            DoorListBox.Items.RemoveAt(selectedDoor);
            DoorListBox.Items.Insert(selectedDoor, roomList[selectedRoom].doors[selectedDoor].info);
            DoorListBox.SelectedIndex = selectedDoor;
            // Update C++
            _updateDoor(selectedRoom, selectedDoor, currDoor.roomID, currDoor.breakable, currDoor.x, currDoor.y, currDoor.roomX, currDoor.roomY, currDoor.rotation);
        }

        private void DoorFacingComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedDoor = DoorListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedDoor == -1)
                return;
            Door currDoor = roomList[selectedRoom].doors[selectedDoor];
            // Update C#
            switch (DoorFacingComboBox.SelectedIndex)
            {
                case 0:
                    roomList[selectedRoom].doors[selectedDoor].rotation = 0;
                    break;
                case 1:
                    roomList[selectedRoom].doors[selectedDoor].rotation = 90;
                    break;
                case 2:
                    roomList[selectedRoom].doors[selectedDoor].rotation = 180;
                    break;
                case 3:
                    roomList[selectedRoom].doors[selectedDoor].rotation = 270;
                    break;
            }
            // Update list box
            roomList[selectedRoom].doors[selectedDoor].UpdateInfo();
            DoorListBox.Items.RemoveAt(selectedDoor);
            DoorListBox.Items.Insert(selectedDoor, roomList[selectedRoom].doors[selectedDoor].info);
            DoorListBox.SelectedIndex = selectedDoor;
            // Update C++
            _updateDoor(selectedRoom, selectedDoor, currDoor.roomID, currDoor.breakable, currDoor.x, currDoor.y, currDoor.roomX, currDoor.roomY, currDoor.rotation);
        }

        private void DoorRoomIDNUD_ValueChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedDoor = DoorListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedDoor == -1)
                return;
            Door currDoor = roomList[selectedRoom].doors[selectedDoor];
            // Update C#
            currDoor.roomID = (int)DoorRoomIDNUD.Value;
            // Update list box
            roomList[selectedRoom].doors[selectedDoor].UpdateInfo();
            DoorListBox.Items.RemoveAt(selectedDoor);
            DoorListBox.Items.Insert(selectedDoor, roomList[selectedRoom].doors[selectedDoor].info);
            DoorListBox.SelectedIndex = selectedDoor;
            // Update C++
            _updateDoor(selectedRoom, selectedDoor, currDoor.roomID, currDoor.breakable, currDoor.x, currDoor.y, currDoor.roomX, currDoor.roomY, currDoor.rotation);
        }

        private void DoorSpawnXNUD_ValueChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedDoor = DoorListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedDoor == -1)
                return;
            Door currDoor = roomList[selectedRoom].doors[selectedDoor];
            // Update C#
            currDoor.roomX = (float)DoorSpawnXNUD.Value;
            // Update list box
            roomList[selectedRoom].doors[selectedDoor].UpdateInfo();
            DoorListBox.Items.RemoveAt(selectedDoor);
            DoorListBox.Items.Insert(selectedDoor, roomList[selectedRoom].doors[selectedDoor].info);
            DoorListBox.SelectedIndex = selectedDoor;
            // Update C++
            _updateDoor(selectedRoom, selectedDoor, currDoor.roomID, currDoor.breakable, currDoor.x, currDoor.y, currDoor.roomX, currDoor.roomY, currDoor.rotation);
        }

        private void DoorSpawnYNUD_ValueChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedDoor = DoorListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedDoor == -1)
                return;
            Door currDoor = roomList[selectedRoom].doors[selectedDoor];
            // Update C#
            currDoor.roomY = (float)DoorSpawnYNUD.Value;
            // Update list box
            roomList[selectedRoom].doors[selectedDoor].UpdateInfo();
            DoorListBox.Items.RemoveAt(selectedDoor);
            DoorListBox.Items.Insert(selectedDoor, roomList[selectedRoom].doors[selectedDoor].info);
            DoorListBox.SelectedIndex = selectedDoor;
            // Update C++
            _updateDoor(selectedRoom, selectedDoor, currDoor.roomID, currDoor.breakable, currDoor.x, currDoor.y, currDoor.roomX, currDoor.roomY, currDoor.rotation);
        }

        private void DoorBreakableCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedDoor = DoorListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedDoor == -1)
                return;
            Door currDoor = roomList[selectedRoom].doors[selectedDoor];
            // Update C#
            currDoor.breakable = DoorBreakableCheckbox.Checked;
            // Update list box
            roomList[selectedRoom].doors[selectedDoor].UpdateInfo();
            DoorListBox.Items.RemoveAt(selectedDoor);
            DoorListBox.Items.Insert(selectedDoor, roomList[selectedRoom].doors[selectedDoor].info);
            DoorListBox.SelectedIndex = selectedDoor;
            // Update C++
            _updateDoor(selectedRoom, selectedDoor, currDoor.roomID, currDoor.breakable, currDoor.x, currDoor.y, currDoor.roomX, currDoor.roomY, currDoor.rotation);
        }
       
        /****************************************************************/
        #endregion
        /****************************************************************/

        /****************************************************************/
        #region Wall Tile Functions
        /****************************************************************/

        private void WallListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedWall = WallListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedWall == -1)
                return;
            // Set All
            WallSizeNUD.ValueChanged -= Wall_Update;
            WallXNUD.ValueChanged -= Wall_Update;
            WallYNUD.ValueChanged -= Wall_Update;
            WallOrientationComboBox.SelectedIndexChanged -= Wall_Update;

            WallSizeNUD.Value = roomList[selectedRoom].wallTiles[selectedWall].size;
            WallXNUD.Value = (decimal)roomList[selectedRoom].wallTiles[selectedWall].x;
            WallYNUD.Value = (decimal)roomList[selectedRoom].wallTiles[selectedWall].y;
            if(roomList[selectedRoom].wallTiles[selectedWall].horizontal)
                WallOrientationComboBox.SelectedIndex = 1;
            else
                WallOrientationComboBox.SelectedIndex = 0;

            WallSizeNUD.ValueChanged += Wall_Update;
            WallXNUD.ValueChanged += Wall_Update;
            WallYNUD.ValueChanged += Wall_Update;
            WallOrientationComboBox.SelectedIndexChanged += Wall_Update;
        }
        
        private void AddWallButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            // Create a new light
            Wall newWall = new Wall();
            // Add to C#
            roomList[selectedRoom].wallTiles.Add(newWall);
            // Add to list box
            WallListBox.Items.Add(newWall);
            // Add to C++
            _updateWallTile(selectedRoom, -1, newWall.x, newWall.y, newWall.horizontal, newWall.size);
        }
       
        private void DeleteWallButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedWall = WallListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedWall == -1)
                return;
            // Remove from C#
            roomList[selectedRoom].wallTiles.RemoveAt(selectedWall);
            // Remove from list box
            WallListBox.Items.RemoveAt(selectedWall);
            // Remove from C++
            _deleteObject(selectedRoom, selectedWall, (int)VECTORENUM.VECTOR_WALL);
        }
        
        private void Wall_Update(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedWall = WallListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedWall == -1)
                return;
            Wall currWall = roomList[selectedRoom].wallTiles[selectedWall];
            // Update C#
            currWall.x = (float)WallXNUD.Value;
            currWall.y = (float)WallYNUD.Value;
            currWall.size = (int)WallSizeNUD.Value;
            currWall.horizontal = (WallOrientationComboBox.SelectedIndex == 0) ? false : true;
            // Update list box
            WallListBox.Items[selectedWall] = roomList[selectedRoom].wallTiles[selectedWall];
            // Update C++
            _updateWallTile(selectedRoom, selectedWall, currWall.x, currWall.y, currWall.horizontal, currWall.size);
        }

        /****************************************************************/
        #endregion
        /****************************************************************/

        /****************************************************************/
        #region Gameplay Object Functions
        /****************************************************************/

        private void GameObjectListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedObject = GameObjectListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedObject == -1)
                return;
            // Set All
            GameObjectTypeComboBox.SelectedIndex = roomList[selectedRoom].objects[selectedObject].objectID;
            GameObjectXNUD.Value = (decimal)roomList[selectedRoom].objects[selectedObject].x;
            GameObjectYNUD.Value = (decimal)roomList[selectedRoom].objects[selectedObject].y;
            switch (roomList[selectedRoom].objects[selectedObject].rotation)
            {
                case 0:
                    GameObjectFacingComboBox.SelectedIndex = 0;
                    break;
                case 90:
                    GameObjectFacingComboBox.SelectedIndex = 1;
                    break;
                case 180:
                    GameObjectFacingComboBox.SelectedIndex = 2;
                    break;
                case 270:
                    GameObjectFacingComboBox.SelectedIndex = 3;
                    break;
            }
            ObjectFlagComboBox.Items.Clear();
            if (roomList[selectedRoom].objects[selectedObject].objectID == 1)
            {
                ObjectFlagComboBox.Items.Add("Spawn");
                ObjectFlagComboBox.Items.Add("Gorilla");
                ObjectFlagComboBox.Items.Add("Frog");
                ObjectFlagComboBox.Items.Add("Hawk");
                ObjectFlagComboBox.Items.Add("Cheetah");
            }
            else if (roomList[selectedRoom].objects[selectedObject].objectID == 4)
            {
                ObjectFlagComboBox.Items.Add("Stationary");
                ObjectFlagComboBox.Items.Add("Falling");
            }
            ObjectFlagComboBox.SelectedIndex = roomList[selectedRoom].objects[selectedObject].flag;
        }
        
        private void AddGameObjectButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            // Create a new light
            GameObject newObject = new GameObject();
            // Add to C#
            roomList[selectedRoom].objects.Add(newObject);
            // Add to list box
            GameObjectListBox.Items.Add(newObject);
            // Add to C++
            _updateGameObject(selectedRoom, -1, newObject.x, newObject.y, newObject.rotation, newObject.objectID);
        }

        private void DeleteGameObjectButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedObject = GameObjectListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedObject == -1)
                return;
            // Remove from C#
            roomList[selectedRoom].objects.RemoveAt(selectedObject);
            // Remove from list box
            GameObjectListBox.Items.RemoveAt(selectedObject);
            // Remove from C++
            _deleteObject(selectedRoom, selectedObject, (int)VECTORENUM.VECTOR_GAMEOBJECT);
        }

        private void GameObjectTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedObject = GameObjectListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedObject == -1)
                return;
            GameObject currObject = roomList[selectedRoom].objects[selectedObject];
            // Update C#
            currObject.objectID = GameObjectTypeComboBox.SelectedIndex;
            currObject.flag = -1;
            // Update list box
            roomList[selectedRoom].objects[selectedObject].UpdateInfo();
            GameObjectListBox.Items[selectedObject] = roomList[selectedRoom].objects[selectedObject].info;
            // Update C++
            _updateGameObject(selectedRoom, selectedObject, currObject.x, currObject.y, currObject.rotation, currObject.objectID);
            ObjectFlagComboBox.Items.Clear();
            if (currObject.objectID == 1)
            {
                ObjectFlagComboBox.Items.Add("Spawn");
                ObjectFlagComboBox.Items.Add("Gorilla");
                ObjectFlagComboBox.Items.Add("Frog");
                ObjectFlagComboBox.Items.Add("Hawk");
                ObjectFlagComboBox.Items.Add("Cheetah");
            }
            else if (currObject.objectID == 4)
            {
                ObjectFlagComboBox.Items.Add("Stationary");
                ObjectFlagComboBox.Items.Add("Falling");                   
            }
            ObjectFlagComboBox.SelectedIndex = currObject.flag;
        }

        private void ObjectFlagComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedObject = GameObjectListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedObject == -1)
                return;
            GameObject currObject = roomList[selectedRoom].objects[selectedObject];
            // Update C#
            currObject.flag = ObjectFlagComboBox.SelectedIndex;
        }

        private void GameObjectFacingComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {          
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedObject = GameObjectListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedObject == -1)
                return;
            GameObject currObject = roomList[selectedRoom].objects[selectedObject];
            // Update C#
            switch (GameObjectFacingComboBox.SelectedIndex)
            {
                case 0:
                    roomList[selectedRoom].objects[selectedObject].rotation = 0;
                    break;
                case 1:
                    roomList[selectedRoom].objects[selectedObject].rotation = 90;
                    break;
                case 2:
                    roomList[selectedRoom].objects[selectedObject].rotation = 180;
                    break;
                case 3:
                    roomList[selectedRoom].objects[selectedObject].rotation = 270;
                    break;
            }
            // Update list box
            roomList[selectedRoom].objects[selectedObject].UpdateInfo();
            GameObjectListBox.Items[selectedObject] = roomList[selectedRoom].objects[selectedObject].info;
            // Update C++
            _updateGameObject(selectedRoom, selectedObject, currObject.x, currObject.y, currObject.rotation, currObject.objectID);
        }

        private void GameObjectXNUD_ValueChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedObject = GameObjectListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedObject == -1)
                return;
            GameObject currObject = roomList[selectedRoom].objects[selectedObject];
            // Update C#
            currObject.x = (float)GameObjectXNUD.Value;
            // Update list box
            roomList[selectedRoom].objects[selectedObject].UpdateInfo();
            GameObjectListBox.Items[selectedObject] = roomList[selectedRoom].objects[selectedObject].info;
            // Update C++
            _updateGameObject(selectedRoom, selectedObject, currObject.x, currObject.y, currObject.rotation, currObject.objectID);
        }

        private void GameObjectYNUD_ValueChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedObject = GameObjectListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedObject == -1)
                return;
            GameObject currObject = roomList[selectedRoom].objects[selectedObject];
            // Update C#
            currObject.y = (float)GameObjectYNUD.Value;
            // Update list box
            roomList[selectedRoom].objects[selectedObject].UpdateInfo();
            GameObjectListBox.Items[selectedObject] = roomList[selectedRoom].objects[selectedObject].info;
            // Update C++
            _updateGameObject(selectedRoom, selectedObject, currObject.x, currObject.y, currObject.rotation, currObject.objectID);
        }

        /****************************************************************/
        #endregion
        /****************************************************************/
       
        /****************************************************************/
        #region Pickup Functions
        /****************************************************************/
      
        private void NewPickupButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            // Create a new light
            Pickup newPickup = new Pickup();
            // Add to C#
            roomList[selectedRoom].pickups.Add(newPickup);
            // Add to list box
            PickupListBox.Items.Add(newPickup);
            // Add to C++
            _updatePickup(selectedRoom, -1, newPickup.x, newPickup.y, newPickup.z, (int)newPickup.type);
        }
        
        private void DeletePickupButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPickup = PickupListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPickup == -1)
                return;
            // Remove from C#
            roomList[selectedRoom].pickups.RemoveAt(selectedPickup);
            // Remove from list box
            PickupListBox.Items.RemoveAt(selectedPickup);
            // Remove from C++
            _deleteObject(selectedRoom, selectedPickup, (int)VECTORENUM.VECTOR_PICKUP);
        }
      
        private void PickupListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPickup = PickupListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPickup == -1)
                return;
            // Set All

            PickupXNUD.ValueChanged -= Pickup_Update;
            PickupYNUD.ValueChanged -= Pickup_Update;
            PickupZNUD.ValueChanged -= Pickup_Update;
            PickupTypeComboBox.SelectedIndexChanged -= Pickup_Update;

            PickupXNUD.Value = (decimal)roomList[selectedRoom].pickups[selectedPickup].x;
            PickupYNUD.Value = (decimal)roomList[selectedRoom].pickups[selectedPickup].y;
            PickupZNUD.Value = (decimal)roomList[selectedRoom].pickups[selectedPickup].z;
            PickupTypeComboBox.SelectedIndex = (int)roomList[selectedRoom].pickups[selectedPickup].type;
        
            PickupXNUD.ValueChanged += Pickup_Update;
            PickupYNUD.ValueChanged += Pickup_Update;
            PickupZNUD.ValueChanged += Pickup_Update;
            PickupTypeComboBox.SelectedIndexChanged += Pickup_Update;
        
        }

        private void Pickup_Update(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPickup = PickupListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPickup == -1)
                return;
            Pickup currPickup = roomList[selectedRoom].pickups[selectedPickup];
            // Update C#
            currPickup.x = (float)PickupXNUD.Value;
            currPickup.y = (float)PickupYNUD.Value;
            currPickup.z = (float)PickupZNUD.Value;
            currPickup.type = (Pickup.PickupTypes)PickupTypeComboBox.SelectedIndex;
            // Update list box
            PickupListBox.Items[selectedPickup] = currPickup;
            // Update C++
            _updatePickup(selectedRoom, selectedPickup, currPickup.x, currPickup.y, currPickup.z, (int)currPickup.type);
        }

        /****************************************************************/
        #endregion
        /****************************************************************/
       
        /****************************************************************/
        #region Moving Platform Functions
        /****************************************************************/

        private void MovingPlatformListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPlatform = MovingPlatformListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPlatform == -1)
                return;
            // Set All
            MovingPlatformStartXNUD.Value = (decimal)roomList[selectedRoom].movingPlatforms[selectedPlatform].x;
            MovingPlatformStartYNUD.Value = (decimal)roomList[selectedRoom].movingPlatforms[selectedPlatform].y;
            //MovingPlatformEndXNUD.Value = (decimal)roomList[selectedRoom].movingPlatforms[selectedPlatform].endX;
            //MovingPlatformEndYNUD.Value = (decimal)roomList[selectedRoom].movingPlatforms[selectedPlatform].endY;
            MovingPlatformSpeedNUD.Value = (decimal)roomList[selectedRoom].movingPlatforms[selectedPlatform].speed;
            switch (roomList[selectedRoom].movingPlatforms[selectedPlatform].rotation)
            {
                case 0:
                    MovingPlatformFacingComboBox.SelectedIndex = 0;
                    break;
                case 90:
                    MovingPlatformFacingComboBox.SelectedIndex = 1;
                    break;
                case 180:
                    MovingPlatformFacingComboBox.SelectedIndex = 2;
                    break;
                case 270:
                    MovingPlatformFacingComboBox.SelectedIndex = 3;
                    break;
            }
            MovingPlatformWaypointListBox.Items.Clear();
            for (int i = 0; i < roomList[selectedRoom].movingPlatforms[selectedPlatform].waypoints.Count; i++)
                MovingPlatformWaypointListBox.Items.Add(roomList[selectedRoom].movingPlatforms[selectedPlatform].waypoints[i]);

        }

        private void AddMovingPlatformButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            // Create a new light
            MovingPlatform newPlatform = new MovingPlatform();
            // Add to C#
            roomList[selectedRoom].movingPlatforms.Add(newPlatform);
            // Add to list box
            MovingPlatformListBox.Items.Add(newPlatform);
            // Add to C++
            _updateMovingPlatform(selectedRoom, -1, newPlatform.x, newPlatform.y, newPlatform.endX, newPlatform.endY, 
                newPlatform.rotation, newPlatform.speed);
        }

        private void DeleteMovingPlatformButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPlatform = MovingPlatformListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPlatform == -1)
                return;
            // Remove from C#
            roomList[selectedRoom].movingPlatforms.RemoveAt(selectedPlatform);
            // Remove from list box
            MovingPlatformListBox.Items.RemoveAt(selectedPlatform);
            MovingPlatformWaypointListBox.Items.Clear();
            // Remove from C++
            _deleteObject(selectedRoom, selectedPlatform, (int)VECTORENUM.VECTOR_MOVINGPLATFORM);
        }

        private void AddWaypointButton_Click(object sender, EventArgs e)
        {
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPlatform = MovingPlatformListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPlatform == -1)
                return;
            Base newWaypoint = new Base();
            roomList[selectedRoom].movingPlatforms[selectedPlatform].waypoints.Add(newWaypoint);
            MovingPlatformWaypointListBox.Items.Add(newWaypoint);
        }

        private void DeleteWaypointButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPlatform = MovingPlatformListBox.SelectedIndex;
            int selectedWaypoint = MovingPlatformWaypointListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPlatform == -1 || selectedWaypoint == -1)
                return;
            // Remove from C#
            roomList[selectedRoom].movingPlatforms[selectedPlatform].waypoints.RemoveAt(selectedWaypoint);
            // Remove from list box
            MovingPlatformWaypointListBox.Items.RemoveAt(selectedWaypoint);
            // Remove from C++
            //_deleteObject(selectedRoom, selectedPlatform, (int)VECTORENUM.VECTOR_MOVINGPLATFORM);
        }

        private void MovingPlatformWaypointListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPlatform = MovingPlatformListBox.SelectedIndex;
            int selectedWaypoint = MovingPlatformWaypointListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPlatform == -1 || selectedWaypoint == -1)
                return;

            MovingPlatform currPlatform = roomList[selectedRoom].movingPlatforms[selectedPlatform];
            MovingPlatformEndXNUD.Value = (decimal)currPlatform.waypoints[selectedWaypoint].x;
            MovingPlatformEndYNUD.Value = (decimal)currPlatform.waypoints[selectedWaypoint].y;
        }

        private void MovingPlatformStartXNUD_ValueChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPlatform = MovingPlatformListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPlatform == -1)
                return;
            MovingPlatform currPlatform = roomList[selectedRoom].movingPlatforms[selectedPlatform];
            // Update C#
            currPlatform.x = (float)MovingPlatformStartXNUD.Value;
            // Update list box
            roomList[selectedRoom].movingPlatforms[selectedPlatform].UpdateInfo();
            MovingPlatformListBox.Items[selectedPlatform] = roomList[selectedRoom].movingPlatforms[selectedPlatform].info;
            // Update C++
            _updateMovingPlatform(selectedRoom, selectedPlatform, currPlatform.x, currPlatform.y, currPlatform.endX, currPlatform.endY,
                currPlatform.rotation, currPlatform.speed);
        }

        private void MovingPlatformStartYNUD_ValueChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPlatform = MovingPlatformListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPlatform == -1)
                return;
            MovingPlatform currPlatform = roomList[selectedRoom].movingPlatforms[selectedPlatform];
            // Update C#
            currPlatform.y = (float)MovingPlatformStartYNUD.Value;
            // Update list box
            roomList[selectedRoom].movingPlatforms[selectedPlatform].UpdateInfo();
            MovingPlatformListBox.Items[selectedPlatform] = roomList[selectedRoom].movingPlatforms[selectedPlatform].info;
            // Update C++
            _updateMovingPlatform(selectedRoom, selectedPlatform, currPlatform.x, currPlatform.y, currPlatform.endX, currPlatform.endY,
                currPlatform.rotation, currPlatform.speed);
        }
        
        private void MovingPlatformEndXNUD_ValueChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPlatform = MovingPlatformListBox.SelectedIndex;
            int selectedWaypoint = MovingPlatformWaypointListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPlatform == -1 || selectedWaypoint == -1)
                return;
            MovingPlatform currPlatform = roomList[selectedRoom].movingPlatforms[selectedPlatform];
            // Update C#
            currPlatform.waypoints[selectedWaypoint].x = (float)MovingPlatformEndXNUD.Value;
            // Update list box
            //MovingPlatformListBox.Items[selectedPlatform] = roomList[selectedRoom].movingPlatforms[selectedPlatform];
            MovingPlatformWaypointListBox.Items[selectedWaypoint] = roomList[selectedRoom].movingPlatforms[selectedPlatform].waypoints[selectedWaypoint];
            // Update C++
            //_updateMovingPlatform(selectedRoom, selectedPlatform, currPlatform.x, currPlatform.y, currPlatform.endX, currPlatform.endY,
            //   currPlatform.rotation, currPlatform.speed);
        }

        private void MovingPlatformEndYNUD_ValueChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPlatform = MovingPlatformListBox.SelectedIndex;
            int selectedWaypoint = MovingPlatformWaypointListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPlatform == -1 || selectedWaypoint == -1)
                return;
            MovingPlatform currPlatform = roomList[selectedRoom].movingPlatforms[selectedPlatform];
            // Update C#
            currPlatform.waypoints[selectedWaypoint].y = (float)MovingPlatformEndYNUD.Value;
            // Update list box
            //MovingPlatformListBox.Items[selectedPlatform] = roomList[selectedRoom].movingPlatforms[selectedPlatform];
            MovingPlatformWaypointListBox.Items[selectedWaypoint] = roomList[selectedRoom].movingPlatforms[selectedPlatform].waypoints[selectedWaypoint];
            // Update C++
            //_updateMovingPlatform(selectedRoom, selectedPlatform, currPlatform.x, currPlatform.y, currPlatform.endX, currPlatform.endY,
            //   currPlatform.rotation, currPlatform.speed);
        }

        private void MovingPlatformSpeedNUD_ValueChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPlatform = MovingPlatformListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPlatform == -1)
                return;
            MovingPlatform currPlatform = roomList[selectedRoom].movingPlatforms[selectedPlatform];
            // Update C#
            currPlatform.speed = (float)MovingPlatformSpeedNUD.Value;
            // Update list box
            roomList[selectedRoom].movingPlatforms[selectedPlatform].UpdateInfo();
            MovingPlatformListBox.Items[selectedPlatform] = roomList[selectedRoom].movingPlatforms[selectedPlatform].info;
            // Update C++
            _updateMovingPlatform(selectedRoom, selectedPlatform, currPlatform.x, currPlatform.y, currPlatform.endX, currPlatform.endY,
                currPlatform.rotation, currPlatform.speed);
        }
       
        private void MovingPlatformFacingComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedPlatform = MovingPlatformListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedPlatform == -1)
                return;
            MovingPlatform currPlatform = roomList[selectedRoom].movingPlatforms[selectedPlatform];
            // Update C#
            switch (MovingPlatformFacingComboBox.SelectedIndex)
            {
                case 0:
                    roomList[selectedRoom].movingPlatforms[selectedPlatform].rotation = 0;
                    break;
                case 1:
                    roomList[selectedRoom].movingPlatforms[selectedPlatform].rotation = 90;
                    break;
                case 2:
                    roomList[selectedRoom].movingPlatforms[selectedPlatform].rotation = 180;
                    break;
                case 3:
                    roomList[selectedRoom].movingPlatforms[selectedPlatform].rotation = 270;
                    break;
            }
            // Update list box
            roomList[selectedRoom].movingPlatforms[selectedPlatform].UpdateInfo();
            MovingPlatformListBox.Items[selectedPlatform] = roomList[selectedRoom].movingPlatforms[selectedPlatform].info;
            // Update C++
            _updateMovingPlatform(selectedRoom, selectedPlatform, currPlatform.x, currPlatform.y, currPlatform.endX, currPlatform.endY,
                currPlatform.rotation, currPlatform.speed);
        }

        /****************************************************************/
        #endregion
        /****************************************************************/

        /****************************************************************/
        #region Background Object Functions
        /****************************************************************/

        private void BackgroundObjectListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedObject = BackgroundObjectListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedObject == -1)
                return;
            // Set All
            BackgroundObjectXNUD.ValueChanged -= BackgroundObject_Update;
            BackgroundObjectYNUD.ValueChanged -= BackgroundObject_Update;
            BackgroundObjectZNUD.ValueChanged -= BackgroundObject_Update;
            BackgroundObjectRotationXNUD.ValueChanged -= BackgroundObject_Update;
            BackgroundObjectRotationYNUD.ValueChanged -= BackgroundObject_Update;
            BackgroundObjectTypeComboBox.SelectedIndexChanged -= BackgroundObject_Update;

            BackgroundObjectXNUD.Value = (decimal)roomList[selectedRoom].backgroundObjects[selectedObject].x;
            BackgroundObjectYNUD.Value = (decimal)roomList[selectedRoom].backgroundObjects[selectedObject].y; 
            BackgroundObjectZNUD.Value = (decimal)roomList[selectedRoom].backgroundObjects[selectedObject].z;
            BackgroundObjectRotationXNUD.Value = roomList[selectedRoom].backgroundObjects[selectedObject].rotation;
            BackgroundObjectRotationYNUD.Value = roomList[selectedRoom].backgroundObjects[selectedObject].rotationy;
            BackgroundObjectTypeComboBox.SelectedIndex = roomList[selectedRoom].backgroundObjects[selectedObject].objectID;

            BackgroundObjectXNUD.ValueChanged += BackgroundObject_Update;
            BackgroundObjectYNUD.ValueChanged += BackgroundObject_Update;
            BackgroundObjectZNUD.ValueChanged += BackgroundObject_Update;
            BackgroundObjectRotationXNUD.ValueChanged += BackgroundObject_Update;
            BackgroundObjectRotationYNUD.ValueChanged += BackgroundObject_Update;
            BackgroundObjectTypeComboBox.SelectedIndexChanged += BackgroundObject_Update;
        }

        private void AddBackgroundObjectButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            // Create a new light
            BackgroundObject newObject = new BackgroundObject();
            // Add to C#
            roomList[selectedRoom].backgroundObjects.Add(newObject);
            // Add to list box
            BackgroundObjectListBox.Items.Add(newObject);
            // Add to C++
            _updateBackgroundObject(selectedRoom, -1, newObject.objectID, newObject.x, newObject.y, newObject.z, newObject.rotation, 0.0f, 0.0f );
        }

        private void DeleteBackgroundObjectButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedObject = BackgroundObjectListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedObject == -1)
                return;
            // Remove from C#
            roomList[selectedRoom].backgroundObjects.RemoveAt(selectedObject);
            // Remove from list box
            BackgroundObjectListBox.Items.RemoveAt(selectedObject);
            // Remove from C++
            _deleteObject(selectedRoom, selectedObject, (int)VECTORENUM.VECTOR_BGOBJECT);
        }

        private void BackgroundObject_Update(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedObject = BackgroundObjectListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedObject == -1)
                return;
            BackgroundObject currObject = roomList[selectedRoom].backgroundObjects[selectedObject];
            // Update C#
            currObject.x = (float)BackgroundObjectXNUD.Value;
            currObject.y = (float)BackgroundObjectYNUD.Value;
            currObject.z = (float)BackgroundObjectZNUD.Value;
            currObject.rotation = (int)BackgroundObjectRotationXNUD.Value;
            currObject.rotationy = (int)BackgroundObjectRotationYNUD.Value;
            currObject.objectID = BackgroundObjectTypeComboBox.SelectedIndex;
            // Update list box
            BackgroundObjectListBox.Items[selectedObject] = currObject;
            // Update C++
            _updateBackgroundObject(selectedRoom, selectedObject, currObject.objectID, currObject.x, currObject.y, currObject.z, currObject.rotation, currObject.rotationy, 0.0f);
        }

        /****************************************************************/
        #endregion
        /****************************************************************/
        
        /****************************************************************/
        #region Laser Stuff
        /****************************************************************/
       
        private void LaserListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedLaser = LaserListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedLaser == -1)
                return;
            // Set All
            LaserXNUD.ValueChanged -= Laser_Update;
            LaserYNUD.ValueChanged -= Laser_Update;
            LaserZNUD.ValueChanged -= Laser_Update;
            LaserLengthNUD.ValueChanged -= Laser_Update;
            LaserTimeOnNUD.ValueChanged -= Laser_Update;
            LaserTimeOffNUD.ValueChanged -= Laser_Update;
            LaserOrientationComboBox.SelectedIndexChanged -= Laser_Update;

            LaserXNUD.Value = (decimal)roomList[selectedRoom].lasers[selectedLaser].x;
            LaserYNUD.Value = (decimal)roomList[selectedRoom].lasers[selectedLaser].y;
            LaserZNUD.Value = (decimal)roomList[selectedRoom].lasers[selectedLaser].z;
            LaserLengthNUD.Value = (decimal)roomList[selectedRoom].lasers[selectedLaser].length;
            LaserTimeOnNUD.Value = (decimal)roomList[selectedRoom].lasers[selectedLaser].timeOn;
            LaserTimeOffNUD.Value = (decimal)roomList[selectedRoom].lasers[selectedLaser].timeOff;
            if(roomList[selectedRoom].lasers[selectedLaser].horizontal)
                LaserOrientationComboBox.SelectedIndex = 0;
            else
                LaserOrientationComboBox.SelectedIndex = 1;
           
            LaserXNUD.ValueChanged += Laser_Update;
            LaserYNUD.ValueChanged += Laser_Update;
            LaserZNUD.ValueChanged += Laser_Update;
            LaserLengthNUD.ValueChanged += Laser_Update;
            LaserTimeOnNUD.ValueChanged += Laser_Update;
            LaserTimeOffNUD.ValueChanged += Laser_Update;
            LaserOrientationComboBox.SelectedIndexChanged += Laser_Update;
        }
        
        private void AddLaserButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            // Create a new laser
            Laser newLaser = new Laser();
            // Add to C#
            roomList[selectedRoom].lasers.Add(newLaser);
            // Add to list box
            LaserListBox.Items.Add(newLaser);
            // Add to C++
            _updateLaser(selectedRoom, -1, newLaser.horizontal, newLaser.x, newLaser.y, newLaser.z, newLaser.length, newLaser.timeOn, newLaser.timeOff );
        }

        private void DeleteLaserButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedLaser = LaserListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedLaser == -1)
                return;
            // Remove from C#
            roomList[selectedRoom].lasers.RemoveAt(selectedLaser);
            // Remove from list box
            LaserListBox.Items.RemoveAt(selectedLaser);
            // Remove from C++
            _deleteObject(selectedRoom, selectedLaser, (int)VECTORENUM.VECTOR_LASER);
        }

        private void Laser_Update(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedLaser = LaserListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedLaser == -1)
                return;
            Laser currLaser = roomList[selectedRoom].lasers[selectedLaser];
            // Update C#
            currLaser.x = (float)LaserXNUD.Value;
            currLaser.y = (float)LaserYNUD.Value;
            currLaser.z = (float)LaserZNUD.Value;
            currLaser.length = (float)LaserLengthNUD.Value;
            currLaser.timeOn = (float)LaserTimeOnNUD.Value;
            currLaser.timeOff = (float)LaserTimeOffNUD.Value;
            currLaser.horizontal = (LaserOrientationComboBox.SelectedIndex == 0) ? true: false;
            // Update list box
            LaserListBox.Items[selectedLaser] = currLaser;
            // Update C++
            _updateLaser(selectedRoom, selectedLaser, currLaser.horizontal, currLaser.x, currLaser.y, currLaser.z, 
                currLaser.length, currLaser.timeOn, currLaser.timeOff);
        }

        /****************************************************************/
        #endregion
        /****************************************************************/

        /****************************************************************/
        #region crusher
        /****************************************************************/
        private void CrusherListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedCrusher = CrusherListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedCrusher == -1)
                return;
            // Set All
            CrusherXNUD.ValueChanged -= Update_Crusher;
            CrusherYNUD.ValueChanged -= Update_Crusher;
            CrusherZNUD.ValueChanged -= Update_Crusher;
            CrusherSpeedNUD.ValueChanged -= Update_Crusher;
            CrusherHeightNUD.ValueChanged -= Update_Crusher;

            CrusherXNUD.Value = (decimal)roomList[selectedRoom].crushers[selectedCrusher].x;
            CrusherYNUD.Value = (decimal)roomList[selectedRoom].crushers[selectedCrusher].y;
            CrusherZNUD.Value = (decimal)roomList[selectedRoom].crushers[selectedCrusher].z;
            CrusherSpeedNUD.Value = (decimal)roomList[selectedRoom].crushers[selectedCrusher].speed;
            CrusherHeightNUD.Value = (decimal)roomList[selectedRoom].crushers[selectedCrusher].height;

            CrusherXNUD.ValueChanged += Update_Crusher;
            CrusherYNUD.ValueChanged += Update_Crusher;
            CrusherZNUD.ValueChanged += Update_Crusher;
            CrusherSpeedNUD.ValueChanged += Update_Crusher;
            CrusherHeightNUD.ValueChanged += Update_Crusher;
        }

        private void AddCrusherButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            // Create a new light
            Crusher newCrusher = new Crusher();
            // Add to C#
            roomList[selectedRoom].crushers.Add(newCrusher);
            // Add to list box
            CrusherListBox.Items.Add(newCrusher);
            // Add to C++
            _updateCrusher(selectedRoom, -1, newCrusher.x, newCrusher.y, newCrusher.z, newCrusher.height, newCrusher.speed);
        }

        private void DeleteCrusherButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedCrusher = CrusherListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedCrusher == -1)
                return;
            // Remove from C#
            roomList[selectedRoom].crushers.RemoveAt(selectedCrusher);
            // Remove from list box
            CrusherListBox.Items.RemoveAt(selectedCrusher);
            // Remove from C++
            _deleteObject(selectedRoom, selectedCrusher, (int)VECTORENUM.VECTOR_CRUSHER);
        }

        private void Update_Crusher(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedCrusher = CrusherListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedCrusher == -1)
                return;
            Crusher currCrusher = roomList[selectedRoom].crushers[selectedCrusher];
            // Update C#
            currCrusher.x = (float)CrusherXNUD.Value;
            currCrusher.y = (float)CrusherYNUD.Value;
            currCrusher.z = (float)CrusherZNUD.Value;
            currCrusher.height = (float)CrusherHeightNUD.Value;
            currCrusher.speed = (float)CrusherSpeedNUD.Value;
            // Update list box
            CrusherListBox.Items[selectedCrusher] = currCrusher;
            // Update C++
            _updateCrusher(selectedRoom, selectedCrusher, currCrusher.x, currCrusher.y, currCrusher.z, currCrusher.height, currCrusher.speed);
        }

        /****************************************************************/
        #endregion
        /****************************************************************/
        
        /****************************************************************/
        #region Wind Fans
        /****************************************************************/
        private void WindFanListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedFan = WindFanListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedFan == -1)
                return;
            // Set All
            WindFanXNUD.ValueChanged -= Update_WindFan;
            WindFanYNUD.ValueChanged -= Update_WindFan;
            WindFanLengthNUD.ValueChanged -= Update_WindFan;
            WindFanSpeedNUD.ValueChanged -= Update_WindFan;
            WindFanFacingComboBox.SelectedIndexChanged -= Update_WindFan;

            WindFanXNUD.Value = (decimal)roomList[selectedRoom].windFans[selectedFan].x;
            WindFanYNUD.Value = (decimal)roomList[selectedRoom].windFans[selectedFan].y;
            WindFanLengthNUD.Value = (decimal)roomList[selectedRoom].windFans[selectedFan].length;
            WindFanSpeedNUD.Value = (decimal)roomList[selectedRoom].windFans[selectedFan].speed;
            switch((int)roomList[selectedRoom].windFans[selectedFan].rotation)
            {
                case 0:
                    WindFanFacingComboBox.SelectedIndex = 0;
                    break;
                case 90:
                    WindFanFacingComboBox.SelectedIndex = 1;
                    break;
                case 180:
                    WindFanFacingComboBox.SelectedIndex = 2;
                    break;
                case 270:
                    WindFanFacingComboBox.SelectedIndex = 3;
                    break;
            };
            WindFanXNUD.ValueChanged += Update_WindFan;
            WindFanYNUD.ValueChanged += Update_WindFan;
            WindFanLengthNUD.ValueChanged += Update_WindFan;
            WindFanSpeedNUD.ValueChanged += Update_WindFan;
            WindFanFacingComboBox.SelectedIndexChanged += Update_WindFan;
        }

        private void WindFanAddButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            // Create a new light
            WindFan newFan = new WindFan();
            // Add to C#
            roomList[selectedRoom].windFans.Add(newFan);
            // Add to list box
            WindFanListBox.Items.Add(newFan);
            // Add to C++
            _updateWindFan(selectedRoom, -1, newFan.x, newFan.y, newFan.z, newFan.rotation, newFan.length, newFan.speed);
        }

        private void WindFanDeleteButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedFan = WindFanListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedFan == -1)
                return;
            // Remove from C#
            roomList[selectedRoom].windFans.RemoveAt(selectedFan);
            // Remove from list box
            WindFanListBox.Items.RemoveAt(selectedFan);
            // Remove from C++
            _deleteObject(selectedRoom, selectedFan, (int)VECTORENUM.VECTOR_WINDFAN);
        }

        private void Update_WindFan(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedFan = WindFanListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedFan == -1)
                return;
            WindFan currFan = roomList[selectedRoom].windFans[selectedFan];
            // Update C#
            currFan.x = (float)WindFanXNUD.Value;
            currFan.y = (float)WindFanYNUD.Value;
            currFan.speed = (float)WindFanSpeedNUD.Value;
            currFan.length = (float)WindFanLengthNUD.Value;
            switch(WindFanFacingComboBox.SelectedIndex)
            {
                case 0:
                    currFan.rotation = 0.0f;
                    break;
                case 1:
                    currFan.rotation = 90.0f;
                    break;
                case 2:
                    currFan.rotation = 180.0f;
                    break;
                case 3: 
                    currFan.rotation = 270.0f;
                    break;
            }
            // Update list box
            WindFanListBox.Items[selectedFan] = currFan;
            // Update C++
            _updateWindFan(selectedRoom, selectedFan, currFan.x, currFan.y, currFan.z, currFan.rotation, currFan.length, currFan.speed);
        }

        /****************************************************************/
        #endregion
        /****************************************************************/

        /****************************************************************/
        #region Enemy Functions
        /****************************************************************/

        private void EnemyListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedEnemy = EnemyListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedEnemy == -1)
                return;
            Enemy currEnemy = roomList[selectedRoom].enemies[selectedEnemy];
            // Set All

            EnemyStartXNUD.ValueChanged -= Enemy_Update;
            EnemyStartYNUD.ValueChanged -= Enemy_Update;
            EnemyEndXNUD.ValueChanged   -= Enemy_Update;
            EnemyEndYNUD.ValueChanged   -= Enemy_Update;
            EnemySpeedNUD.ValueChanged  -= Enemy_Update;
            EnemyTypeComboBox.SelectedIndexChanged -= Enemy_Update;

            EnemyStartXNUD.Value = (decimal)currEnemy.x;
            EnemyStartYNUD.Value = (decimal)currEnemy.y;
            EnemyEndXNUD.Value = (decimal)currEnemy.endX;
            EnemyEndYNUD.Value = (decimal)currEnemy.endY;
            EnemySpeedNUD.Value = (decimal)currEnemy.speed;
            EnemyTypeComboBox.SelectedIndex = (int)currEnemy.type;

            EnemyStartXNUD.ValueChanged += Enemy_Update;
            EnemyStartYNUD.ValueChanged += Enemy_Update;
            EnemyEndXNUD.ValueChanged   += Enemy_Update;
            EnemyEndYNUD.ValueChanged   += Enemy_Update;
            EnemySpeedNUD.ValueChanged  += Enemy_Update;
            EnemyTypeComboBox.SelectedIndexChanged += Enemy_Update;
        }
        
        private void NewEnemyButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1)
                return;
            // Create a new light
            Enemy newEnemy = new Enemy();
            // Add to C#
            roomList[selectedRoom].enemies.Add(newEnemy);
            // Add to list box
            EnemyListBox.Items.Add(newEnemy);
            // Add to C++
            _updateEnemy(selectedRoom, -1, (int)newEnemy.type, newEnemy.x, newEnemy.y, newEnemy.endX, newEnemy.endY, newEnemy.speed);
        }

        private void DeleteEnemyButton_Click(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedEnemy = EnemyListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedEnemy == -1)
                return;
            // Remove from C#
            roomList[selectedRoom].enemies.RemoveAt(selectedEnemy);
            // Remove from list box
            EnemyListBox.Items.RemoveAt(selectedEnemy);
            // Remove from C++
            _deleteObject(selectedRoom, selectedEnemy, (int)VECTORENUM.VECTOR_ENEMY);
        }        
      
        private void Enemy_Update(object sender, EventArgs e)
        {
            // If not valid index return
            int selectedRoom = RoomListBox.SelectedIndex;
            int selectedEnemy = EnemyListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedEnemy == -1)
                return;
            Enemy currEnemy = roomList[selectedRoom].enemies[selectedEnemy];
            // Update C#
            currEnemy.x = (float)EnemyStartXNUD.Value;
            currEnemy.y = (float)EnemyStartYNUD.Value;
            currEnemy.endX = (float)EnemyEndXNUD.Value;
            currEnemy.endY = (float)EnemyEndYNUD.Value;
            currEnemy.speed = (float)EnemySpeedNUD.Value;
            currEnemy.type = (Enemy.EnemyType)EnemyTypeComboBox.SelectedIndex;
            // Update list box
            EnemyListBox.Items[selectedEnemy] = currEnemy;
            // Update C++
            _updateEnemy(selectedRoom, selectedEnemy, (int)currEnemy.type, currEnemy.x, currEnemy.y, currEnemy.endX, currEnemy.endY,
                currEnemy.speed);
        }

        /****************************************************************/
        #endregion
        /****************************************************************/

        /****************************************************************/
        #region Unknown Functions
        /****************************************************************/

        private void numericUpDown32_ValueChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
        }
       
        private void CollisionBoxHeightNUD_ValueChanged(object sender, EventArgs e)
        {

        }

        private void CollisionBoxWidthNUD_ValueChanged(object sender, EventArgs e)
        {
        }
    
        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
  
        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
      
        private void GameObjectListBox_SelectedIndexChanged1(object sender, EventArgs e)
        {

        }

        private void BackgroundListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void MovingPlatformRotNUD_ValueChanged(object sender, EventArgs e)
        {

        }

        private void GameObjectRotNUD_ValueChanged(object sender, EventArgs e)
        {
            if (RoomListBox.SelectedIndex == -1 || GameObjectListBox.SelectedIndex == -1)
                return;
            GameObject currObject = roomList[RoomListBox.SelectedIndex].objects[GameObjectListBox.SelectedIndex];
            //currObject.rotation = (int)GameObjectFacingComboBox.Value;
            _updateGameObject(RoomListBox.SelectedIndex, GameObjectListBox.SelectedIndex, currObject.x,
                currObject.y, currObject.rotation, currObject.objectID);
        }

        private void groupBox8_Enter(object sender, EventArgs e)
        {

        }

        private void label38_Click(object sender, EventArgs e)
        {

        }

        private void LightBrightnessNUD_ValueChangedee(object sender, EventArgs e)
        {

        }

        private void MovingPlatformEndYNUD_ValueChangedbull(object sender, EventArgs e)
        {

        }

        private void MovingPlatformEndYNUD_ValueChanged11(object sender, EventArgs e)
        {

        }

        private void MovingPlatformSpeedNUD_ValueChanged111(object sender, EventArgs e)
        {

        }
     
        private void MovingPlatformSpeedNUD_ValueChanged1(object sender, EventArgs e)
        {
        }

        private void label42_Click(object sender, EventArgs e)
        {

        }

        private void label51_Click(object sender, EventArgs e)
        {

        }

        private void editorTipsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void showUIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Main_UI_TabControl.Visible = showUIToolStripMenuItem.Checked;
        }

        private void constrainToRoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int roomIter = 0; roomIter < roomList.Count; roomIter++)
            {
                float minX = 1000000;  float minY = 1000000;
                float maxX = -1000000; float maxY = -1000000;

                for (int iter = 0; iter < roomList[roomIter].wallTiles.Count; iter++)
                {
                    if (roomList[roomIter].wallTiles[iter].x < minX)
                        minX = roomList[roomIter].wallTiles[iter].x;
                    if (roomList[roomIter].wallTiles[iter].y < minY)
                        minY = roomList[roomIter].wallTiles[iter].y;
                    if (roomList[roomIter].wallTiles[iter].x > maxX)
                        maxX = roomList[roomIter].wallTiles[iter].x;
                    if (roomList[roomIter].wallTiles[iter].y > maxY)
                        maxY = roomList[roomIter].wallTiles[iter].y;
                }
                for (int iter = 0; iter < roomList[roomIter].objects.Count; iter++)
                {
                    if (roomList[roomIter].objects[iter].x < minX)
                        minX = roomList[roomIter].objects[iter].x;
                    if (roomList[roomIter].objects[iter].y < minY)
                        minY = roomList[roomIter].objects[iter].y;
                    if (roomList[roomIter].objects[iter].x > maxX)
                        maxX = roomList[roomIter].objects[iter].x;
                    if (roomList[roomIter].objects[iter].y > maxY)
                        maxY = roomList[roomIter].objects[iter].y;
                }
                for (int iter = 0; iter < roomList[roomIter].backgroundObjects.Count; iter++)
                {
                    if (roomList[roomIter].backgroundObjects[iter].x < minX)
                        minX = (int)roomList[roomIter].backgroundObjects[iter].x;
                    if (roomList[roomIter].backgroundObjects[iter].y < minY)
                        minY = (int)roomList[roomIter].backgroundObjects[iter].y;
                    if (roomList[roomIter].backgroundObjects[iter].x > maxX)
                        maxX = (int)roomList[roomIter].backgroundObjects[iter].x;
                    if (roomList[roomIter].backgroundObjects[iter].y > maxY)
                        maxY = (int)roomList[roomIter].backgroundObjects[iter].y;
                }
                for (int iter = 0; iter < roomList[roomIter].doors.Count; iter++)
                {
                    if (roomList[roomIter].doors[iter].x < minX)
                        minX = roomList[roomIter].doors[iter].x;
                    if (roomList[roomIter].doors[iter].y < minY)
                        minY = roomList[roomIter].doors[iter].y;
                    if (roomList[roomIter].doors[iter].x > maxX)
                        maxX = roomList[roomIter].doors[iter].x;
                    if (roomList[roomIter].doors[iter].y > maxY)
                        maxY = roomList[roomIter].doors[iter].y;
                }
                for (int iter = 0; iter < roomList[roomIter].enemies.Count; iter++)
                {
                    if (roomList[roomIter].enemies[iter].x < minX)
                        minX = (int)roomList[roomIter].enemies[iter].x;
                    if (roomList[roomIter].enemies[iter].y < minY)
                        minY = (int)roomList[roomIter].enemies[iter].y;
                    if (roomList[roomIter].enemies[iter].x > maxX)
                        maxX = (int)roomList[roomIter].enemies[iter].x;
                    if (roomList[roomIter].enemies[iter].y > maxY)
                        maxY = (int)roomList[roomIter].enemies[iter].y;
                    if (roomList[roomIter].enemies[iter].endX < minX)
                        minX = (int)roomList[roomIter].enemies[iter].endX;
                    if (roomList[roomIter].enemies[iter].endY < minY)
                        minY = (int)roomList[roomIter].enemies[iter].endY;
                    if (roomList[roomIter].enemies[iter].endX > maxX)
                        maxX = (int)roomList[roomIter].enemies[iter].endX;
                    if (roomList[roomIter].enemies[iter].endY > maxY)
                        maxY = (int)roomList[roomIter].enemies[iter].endY;
                }
                for (int iter = 0; iter < roomList[roomIter].movingPlatforms.Count; iter++)
                {
                    if (roomList[roomIter].movingPlatforms[iter].x < minX)
                        minX = (int)roomList[roomIter].movingPlatforms[iter].x;
                    if (roomList[roomIter].movingPlatforms[iter].y < minY)
                        minY = (int)roomList[roomIter].movingPlatforms[iter].y;
                    if (roomList[roomIter].movingPlatforms[iter].x > maxX)
                        maxX = (int)roomList[roomIter].movingPlatforms[iter].x;
                    if (roomList[roomIter].movingPlatforms[iter].y > maxY)
                        maxY = (int)roomList[roomIter].movingPlatforms[iter].y;
                    if (roomList[roomIter].movingPlatforms[iter].endX < minX)
                        minX = (int)roomList[roomIter].movingPlatforms[iter].endX;
                    if (roomList[roomIter].movingPlatforms[iter].endY < minY)
                        minY = (int)roomList[roomIter].movingPlatforms[iter].endY;
                    if (roomList[roomIter].movingPlatforms[iter].endX > maxX)
                        maxX = (int)roomList[roomIter].movingPlatforms[iter].endX;
                    if (roomList[roomIter].movingPlatforms[iter].endY > maxY)
                        maxY = (int)roomList[roomIter].movingPlatforms[iter].endY;
                }

                if(((maxX - minX) < 1) || ((maxY - minY) < 1))
                    continue;

                RoomListBox.SelectedIndex = roomIter;
                RoomWidthNUD.Value  = (decimal)(maxX - minX);
                RoomHeightNUD.Value = (decimal)(maxY - minY);

                for (int iter = 0; iter < roomList[roomIter].wallTiles.Count; iter++)
                {
                    WallListBox.SelectedIndex = iter;
                    WallXNUD.Value -= (decimal)minX;
                    WallYNUD.Value -= (decimal)minY;
                }
                for (int iter = 0; iter < roomList[roomIter].objects.Count; iter++)
                {
                    GameObjectListBox.SelectedIndex = iter;
                    GameObjectXNUD.Value -= (decimal)minX;
                    GameObjectYNUD.Value -= (decimal)minY;
                }
                for (int iter = 0; iter < roomList[roomIter].backgroundObjects.Count; iter++)
                {
                    BackgroundObjectListBox.SelectedIndex = iter;
                    BackgroundObjectXNUD.Value -= (decimal)minX;
                    BackgroundObjectYNUD.Value -= (decimal)minY;
                }
                for (int iter = 0; iter < roomList[roomIter].doors.Count; iter++)
                {
                    DoorListBox.SelectedIndex = iter;
                    DoorPosXNUD.Value -= (decimal)minX;
                    DoorPosYNUD.Value -= (decimal)minY;
                }
                for (int iter = 0; iter < roomList[roomIter].enemies.Count; iter++)
                {
                    EnemyListBox.SelectedIndex = iter;
                    EnemyStartXNUD.Value -= (decimal)minX;
                    EnemyStartYNUD.Value -= (decimal)minY;
                    EnemyEndXNUD.Value -= (decimal)minX;
                    EnemyEndYNUD.Value -= (decimal)minY;
                }
                for (int iter = 0; iter < roomList[roomIter].movingPlatforms.Count; iter++)
                {
                    MovingPlatformListBox.SelectedIndex = iter;
                    MovingPlatformStartXNUD.Value -= (decimal)minX;
                    MovingPlatformStartYNUD.Value -= (decimal)minY;
                    MovingPlatformEndXNUD.Value -= (decimal)minX;
                    MovingPlatformEndYNUD.Value -= (decimal)minY;
                }
                for (int iter = 0; iter < roomList[roomIter].lights.Count; iter++)
                {
                    RoomLightListBox.SelectedIndex = iter;
                    LightPosXNUD.Value -= (decimal)minX;
                    LightPosYNUD.Value -= (decimal)minY;
                }
            }
        }

        private void blankOnlineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.facebook.com/BlankVideoGame");
        }

        private void mysteriousBlueWombatsOnlineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.facebook.com/groups/195640103815270");
        }

        private void RoomWidthNUD_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
        }

        private void radioPlayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(radioPlayer == null || radioPlayer.IsDisposed) 
                radioPlayer = new RadioPlayer();
            radioPlayer.Show();
            radioPlayer.Focus();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void menuToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
            if (e.KeyChar == 13 || e.KeyChar == 27 || this.ActiveControl == null)
            {
                this.ActiveControl = null;
                return;
            }
            if (this.ActiveControl.GetType() == (new NumericUpDown()).GetType() && e.KeyChar >= 48 && e.KeyChar <= 57)
                e.Handled = false;
            if (this.ActiveControl.GetType() == (new TextBox()).GetType())
                e.Handled = false;
            if (this.ActiveControl.GetType() == (new ListBox()).GetType())
                e.Handled = false;

            switch (e.KeyChar)
            {
                case 'w':
                    selectedTool = TOOLENUM.TOOL_TRANSLATE;
                    break;
                case 'e':
                    selectedTool = TOOLENUM.TOOL_ROTATE;
                    break;
                case 'r':
                    selectedTool = TOOLENUM.TOOL_SCALE;
                    break;
                case (char)33:
                    selectedTool = TOOLENUM.TOOL_NONE;
                    break;
            }

        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void RoomAmbientRedNUD_ValueChanged(object sender, EventArgs e)
        {

        }

        private void RoomAmbientGreenNUD_ValueChanged(object sender, EventArgs e)
        {

        }

        private void RoomAmbientBlueNUD_ValueChanged(object sender, EventArgs e)
        {

        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            //this.Cursor = new Cursor(Cursor.Current.Handle);
            //Cursor.Position = new Point(Cursor.Position.X + 5, Cursor.Position.Y);
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            Point cursorChange = new Point(e.X - lastCursorPos.X, e.Y - lastCursorPos.Y);
            lastCursorPos = new Point(e.X, e.Y);

            int selectedRoom = RoomListBox.SelectedIndex;
            if (selectedRoom == -1 || selectedTool == TOOLENUM.TOOL_NONE)
                return;

            Base currObject = new Base();
            NumericUpDown currXNUD = new NumericUpDown();
            NumericUpDown currYNUD = new NumericUpDown();

            if (RoomGroupBox.Contains(this.ActiveControl))
            {
                currObject = roomList[selectedRoom];
                currXNUD = RoomXNUD; currYNUD = RoomYNUD;
            }
            else if (LightGroupBox.Contains(this.ActiveControl))
            {
                if (RoomLightListBox.SelectedIndex == -1)
                    return;
                currObject = roomList[selectedRoom].lights[RoomLightListBox.SelectedIndex];
                currXNUD = LightPosXNUD; currYNUD = LightPosYNUD;
            }
            else if (WallGroupBox.Contains(this.ActiveControl))
            {
                if (WallListBox.SelectedIndex == -1)
                    return;
                currObject = roomList[selectedRoom].wallTiles[WallListBox.SelectedIndex];
                currXNUD = WallXNUD; currYNUD = WallYNUD;
            }
            else if (GameObjectGroupBox.Contains(this.ActiveControl))
            {
                if (GameObjectListBox.SelectedIndex == -1)
                    return;
                currObject = roomList[selectedRoom].objects[GameObjectListBox.SelectedIndex];
                currXNUD = GameObjectXNUD; currYNUD = GameObjectYNUD;
            }
            else if (DoorGroupBox.Contains(this.ActiveControl))
            {
                if (DoorListBox.SelectedIndex == -1)
                    return;
                currObject = roomList[selectedRoom].doors[DoorListBox.SelectedIndex];
                currXNUD = DoorPosXNUD; currYNUD = DoorPosYNUD;
            }
            else if (PlatformGroupBox.Contains(this.ActiveControl))
            {
                if (MovingPlatformListBox.SelectedIndex == -1)
                    return;
                currObject = roomList[selectedRoom].movingPlatforms[MovingPlatformListBox.SelectedIndex];
                currXNUD = MovingPlatformStartXNUD; currYNUD = MovingPlatformStartYNUD;
            }
            else if (BackgroundObjectGroupBox.Contains(this.ActiveControl))
            {
                if (BackgroundObjectListBox.SelectedIndex == -1)
                    return;
                currObject = roomList[selectedRoom].backgroundObjects[BackgroundObjectListBox.SelectedIndex];
                switch(selectedTool)
                {
                    case TOOLENUM.TOOL_TRANSLATE:
                    currXNUD = BackgroundObjectXNUD; currYNUD = BackgroundObjectYNUD;
                    break;
                    case TOOLENUM.TOOL_ROTATE:
                    currXNUD = BackgroundObjectRotationYNUD; currYNUD = BackgroundObjectRotationXNUD;
                    break;
                    case TOOLENUM.TOOL_SCALE:
                    break;
                }
            }
            else if (EnemyGroupBox.Contains(this.ActiveControl))
            {
                if (EnemyListBox.SelectedIndex == -1)
                    return;
                currObject = roomList[selectedRoom].enemies[EnemyListBox.SelectedIndex];
                currXNUD = EnemyStartXNUD; currYNUD = EnemyStartYNUD;
            }
            else if (PickupGroupBox.Contains(this.ActiveControl))
            {
                if (PickupListBox.SelectedIndex == -1)
                    return;
                currObject = roomList[selectedRoom].pickups[PickupListBox.SelectedIndex];
                currXNUD = PickupXNUD; currYNUD = PickupYNUD;
            }
            else if (LaserGroupBox.Contains(this.ActiveControl))
            {
                if (LaserListBox.SelectedIndex == -1)
                    return;
                currObject = roomList[selectedRoom].lasers[LaserListBox.SelectedIndex];
                currXNUD = LaserXNUD; currYNUD = LaserYNUD;
            }
            else if (CrusherGroupBox.Contains(this.ActiveControl))
            {
                if (CrusherListBox.SelectedIndex == -1)
                    return;
                currObject = roomList[selectedRoom].crushers[CrusherListBox.SelectedIndex];
                currXNUD = CrusherXNUD; currYNUD = CrusherYNUD;
            }
            else
                return;
            if (e.Button == MouseButtons.Left)
            {
                if (lockToGrid)
                {
                    if ((currXNUD.Value + cursorChange.X) <= currXNUD.Maximum && (currXNUD.Value + cursorChange.X) >= currXNUD.Minimum)
                        currXNUD.Value = (int)(currXNUD.Value + cursorChange.X);

                    if ((currYNUD.Value - cursorChange.Y) <= currYNUD.Maximum && (currYNUD.Value - cursorChange.Y) >= currYNUD.Minimum)
                        currYNUD.Value = (int)(currYNUD.Value - cursorChange.Y);
                }
                else
                {
                    if ((currXNUD.Value + cursorChange.X) <= currXNUD.Maximum && (currXNUD.Value + cursorChange.X) >= currXNUD.Minimum)
                        currXNUD.Value += (decimal)(cursorChange.X * 0.05f);
                    if ((currYNUD.Value - cursorChange.Y) <= currYNUD.Maximum && (currYNUD.Value - cursorChange.Y) >= currYNUD.Minimum)
                        currYNUD.Value -= (decimal)(cursorChange.Y * 0.05f);
                }
            }
            //
            //

        }

        private void groupBox7_Enter(object sender, EventArgs e)
        {

        }

        private void panel1_DoubleClick(object sender, EventArgs e)
        {
            int selectedRoom = RoomListBox.SelectedIndex;
            int vectorID = selectedRoom, objectID = -1;
            if (selectedRoom == -1)
                return;

            _objectPicking(this.Left + panel1.Left, this.Top + panel1.Top, ref vectorID, ref objectID);
            switch (vectorID)
            {
                case (int)VECTORENUM.VECTOR_WALL:
                    this.ActiveControl = WallListBox;
                    WallListBox.SelectedIndex = objectID;
                    break;
                case (int)VECTORENUM.VECTOR_ENEMY:
                    this.ActiveControl = EnemyListBox;
                    EnemyListBox.SelectedIndex = objectID;
                    break;
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Shift)
                lockToGrid = true;
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (!e.Shift)
                lockToGrid = false;
        }


        private void numericUpDown9_ValueChanged(object sender, EventArgs e)
        {

        }

        private void Tool_ExportRoomLights_Click(object sender, EventArgs e)
        {
            Room selectedRoom = roomList[RoomListBox.SelectedIndex];
            if (RoomListBox.SelectedIndex == -1 || selectedRoom.lights.Count == 0)
                return;

            SaveFileDialog output = new SaveFileDialog();
            output.AddExtension = true;
            output.DefaultExt = ".lights";
            output.Filter =  "Room Lights Data (*.lights)|*.lights";
            if (DialogResult.OK == output.ShowDialog())
            {
                StreamWriter outFile = new StreamWriter(output.FileName.ToString());
                outFile.WriteLine(selectedRoom.lights.Count);
                for (int lightIter = 0; lightIter < selectedRoom.lights.Count; lightIter++)
                {
                    outFile.WriteLine(Convert.ToInt32(selectedRoom.lights[lightIter].type));
                    outFile.WriteLine(selectedRoom.lights[lightIter].x);
                    outFile.WriteLine(selectedRoom.lights[lightIter].y);
                    outFile.WriteLine(selectedRoom.lights[lightIter].z);
                    outFile.WriteLine(selectedRoom.lights[lightIter].red);
                    outFile.WriteLine(selectedRoom.lights[lightIter].green);
                    outFile.WriteLine(selectedRoom.lights[lightIter].blue);
                    outFile.WriteLine(selectedRoom.lights[lightIter].radius);
                    outFile.WriteLine(selectedRoom.lights[lightIter].intensity);
                }
                outFile.Close();
            }
        }

        private void Tool_ImportRoomLights_Click(object sender, EventArgs e)
        {
            Room selectedRoom = roomList[RoomListBox.SelectedIndex];
            if (RoomListBox.SelectedIndex == -1)
                return;

            OpenFileDialog input = new OpenFileDialog();
            input.AddExtension = true;
            input.DefaultExt = ".lights";
            input.Filter = "Room Lights Data (*.lights)|*.lights";
            if (DialogResult.OK == input.ShowDialog())
            {
                StreamReader inFile = new StreamReader(input.FileName.ToString());
                int numLights = Convert.ToInt32(inFile.ReadLine());
                for (int lightIter = 0; lightIter < numLights; lightIter++)
                {
                    Light newLight = new Light();
                    newLight.type = (Light.LightType)Convert.ToInt32(inFile.ReadLine());
                    newLight.x = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.y = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.z = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.red = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.green = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.blue = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.radius = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.intensity = (float)Convert.ToDecimal(inFile.ReadLine());
                    selectedRoom.lights.Add(newLight);
                    RoomLightListBox.Items.Add(newLight);
                    _updateLight(RoomListBox.SelectedIndex, -1, (int)newLight.type, newLight.x, newLight.y, newLight.z, 
                        newLight.red, newLight.green, newLight.blue, newLight.radius, newLight.intensity);
                }
                inFile.Close();
            }
        }

        private void numericUpDown13_ValueChanged(object sender, EventArgs e)
        {

        }

        private void Tool_ImportRoom_Click(object sender, EventArgs e)
        {
            int selectedRoom = RoomListBox.Items.Count;
            OpenFileDialog input = new OpenFileDialog();
            input.CheckFileExists = true;
            input.CheckPathExists = true;
            input.AddExtension = true;
            input.DefaultExt = ".rm";
            input.Filter = "Room file (*.rm)|*.rm";
            if (DialogResult.OK == input.ShowDialog())
            {
                StreamReader inFile = new StreamReader(input.FileName.ToString());

                // Import every room
                Room newRoom = new Room();
                newRoom.x = Convert.ToInt32(inFile.ReadLine());
                newRoom.y = Convert.ToInt32(inFile.ReadLine());
                newRoom.width = Convert.ToInt32(inFile.ReadLine());
                newRoom.height = Convert.ToInt32(inFile.ReadLine());
                newRoom.backgroundID = Convert.ToInt32(inFile.ReadLine());
                newRoom.checkpoint = Convert.ToBoolean(Convert.ToInt32(inFile.ReadLine()));
                newRoom.checkpointX = Convert.ToInt32(inFile.ReadLine());
                newRoom.checkpointY = (float)Convert.ToDecimal(inFile.ReadLine());
                roomList.Add(newRoom);
                RoomListBox.Items.Add(newRoom);
                _updateRoom(-1, newRoom.x, newRoom.y, newRoom.height, newRoom.width, newRoom.checkpoint, newRoom.checkpointX, newRoom.checkpointY, newRoom.backgroundID);

                // Import every wall
                int numWallTiles = Convert.ToInt32(inFile.ReadLine());
                for (int wallIter = 0; wallIter < numWallTiles; wallIter++)
                {
                    Wall newWall = new Wall();
                    newWall.x = Convert.ToInt32(inFile.ReadLine());
                    newWall.y = Convert.ToInt32(inFile.ReadLine());
                    newWall.size = Convert.ToInt32(inFile.ReadLine());
                    newWall.horizontal = Convert.ToBoolean(Convert.ToInt32(inFile.ReadLine()));
                    roomList[selectedRoom].wallTiles.Add(newWall);
                    //WallListBox.Items.Add(newRoom.info);
                    _updateWallTile(selectedRoom, -1, newWall.x, newWall.y, newWall.horizontal, newWall.size);
                }

                // Import every light
                int numLights = Convert.ToInt32(inFile.ReadLine());
                for (int lightIter = 0; lightIter < numLights; lightIter++)
                {
                    Light newLight = new Light();
                    newLight.type = (Light.LightType)Convert.ToInt32(inFile.ReadLine());
                    newLight.x = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.y = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.z = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.red = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.green = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.blue = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.radius = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLight.intensity = (float)Convert.ToDecimal(inFile.ReadLine());
                    roomList[selectedRoom].lights.Add(newLight);
                    //RoomLightListBox.Items.Add(newLight);
                    _updateLight(selectedRoom, -1, (int)newLight.type, newLight.x, newLight.y, newLight.z, newLight.red, newLight.green, newLight.blue, newLight.radius, newLight.intensity);
                }

                // Import every game object
                int numGameObjects = Convert.ToInt32(inFile.ReadLine());
                for (int objectIter = 0; objectIter < numGameObjects; objectIter++)
                {
                    GameObject newObject = new GameObject();
                    newObject.x = Convert.ToInt32(inFile.ReadLine());
                    newObject.y = Convert.ToInt32(inFile.ReadLine());
                    newObject.objectID = Convert.ToInt32(inFile.ReadLine());
                    newObject.rotation = Convert.ToInt32(inFile.ReadLine());
                    newObject.flag = Convert.ToInt32(inFile.ReadLine());
                    roomList[selectedRoom].objects.Add(newObject);
                    //GameObjectListBox.Items.Add(newObject);
                    _updateGameObject(selectedRoom, -1, newObject.x, newObject.y, newObject.rotation, newObject.objectID);
                }

                // Import every door
                int numDoors = Convert.ToInt32(inFile.ReadLine());
                for (int doorIter = 0; doorIter < numDoors; doorIter++)
                {
                    Door newDoor = new Door();
                    newDoor.x = Convert.ToInt32(inFile.ReadLine());
                    newDoor.y = Convert.ToInt32(inFile.ReadLine());
                    newDoor.breakable = Convert.ToBoolean(Convert.ToInt32(inFile.ReadLine()));
                    newDoor.rotation = Convert.ToInt32(inFile.ReadLine());
                    newDoor.roomID = Convert.ToInt32(inFile.ReadLine());
                    newDoor.roomX = (float)Convert.ToDecimal(inFile.ReadLine());
                    newDoor.roomY = (float)Convert.ToDecimal(inFile.ReadLine());
                    roomList[selectedRoom].doors.Add(newDoor);
                    //DoorListBox.Items.Add(newDoor.info);
                    _updateDoor(selectedRoom, -1, newDoor.roomID, newDoor.breakable, newDoor.x, newDoor.y, newDoor.roomX, newDoor.roomY, newDoor.rotation);
                }

                // Import every platform
                int numPlatforms = Convert.ToInt32(inFile.ReadLine());
                for (int platformIter = 0; platformIter < numPlatforms; platformIter++)
                {
                    MovingPlatform newPlatform = new MovingPlatform();
                    int waypointCount = Convert.ToInt32(inFile.ReadLine());
                    newPlatform.x = (float)Convert.ToDecimal(inFile.ReadLine());
                    newPlatform.y = (float)Convert.ToDecimal(inFile.ReadLine());
                    for (int waypointIter = 0; waypointIter < waypointCount - 1; waypointIter++)
                    {
                        Base newWaypoint = new Base();
                        newWaypoint.x = (float)Convert.ToDecimal(inFile.ReadLine());
                        newWaypoint.y = (float)Convert.ToDecimal(inFile.ReadLine());
                        newPlatform.waypoints.Add(newWaypoint);
                    }
                    newPlatform.rotation = Convert.ToInt32(inFile.ReadLine());
                    newPlatform.speed = Convert.ToInt32(inFile.ReadLine());
                    roomList[selectedRoom].movingPlatforms.Add(newPlatform);
                    //MovingPlatformListBox.Items.Add(newPlatform);
                    _updateMovingPlatform(selectedRoom, -1, newPlatform.x, newPlatform.y, newPlatform.endX, newPlatform.endY, newPlatform.rotation, newPlatform.speed);
                }

                // Import every
                int numEnemies = Convert.ToInt32(inFile.ReadLine());
                for (int enemyIter = 0; enemyIter < numEnemies; enemyIter++)
                {
                    Enemy newEnemy = new Enemy();
                    newEnemy.x = (float)Convert.ToDecimal(inFile.ReadLine());
                    newEnemy.y = (float)Convert.ToDecimal(inFile.ReadLine());
                    newEnemy.endX = (float)Convert.ToDecimal(inFile.ReadLine());
                    newEnemy.endY = (float)Convert.ToDecimal(inFile.ReadLine());
                    newEnemy.type = (Enemy.EnemyType)Convert.ToInt32(inFile.ReadLine());
                    newEnemy.speed = Convert.ToInt32(inFile.ReadLine());
                    roomList[selectedRoom].enemies.Add(newEnemy);
                    //EnemyListBox.Items.Add(newEnemy);
                    _updateEnemy(selectedRoom, -1, (int)newEnemy.type, newEnemy.x, newEnemy.y, newEnemy.endX, newEnemy.endY, newEnemy.speed);
                }

                // Import every BG Object
                int numBGObjects = Convert.ToInt32(inFile.ReadLine());
                for (int BGOjectIter = 0; BGOjectIter < numBGObjects; BGOjectIter++)
                {
                    BackgroundObject newBackgroundObject = new BackgroundObject();
                    newBackgroundObject.x = (float)Convert.ToDecimal(inFile.ReadLine());
                    newBackgroundObject.y = (float)Convert.ToDecimal(inFile.ReadLine());
                    newBackgroundObject.z = (float)Convert.ToDecimal(inFile.ReadLine());
                    newBackgroundObject.rotation = Convert.ToInt32(inFile.ReadLine());
                    Convert.ToInt32(inFile.ReadLine()); // quick replace. see save out for more comment
                    Convert.ToInt32(inFile.ReadLine());
                    newBackgroundObject.objectID = Convert.ToInt32(inFile.ReadLine());
                    roomList[selectedRoom].backgroundObjects.Add(newBackgroundObject);
                    //BackgroundObjectListBox.Items.Add(newBackgroundObject);
                    _updateBackgroundObject(selectedRoom, -1, newBackgroundObject.objectID, newBackgroundObject.x, newBackgroundObject.y, newBackgroundObject.z, newBackgroundObject.rotation, 0.0f, 0.0f);
                }
                int numCrushers = Convert.ToInt32(inFile.ReadLine());
                for (int objectIter = 0; objectIter < numCrushers; objectIter++)
                {
                    Crusher newCrusher = new Crusher();
                    newCrusher.x = (float)Convert.ToDecimal(inFile.ReadLine());
                    newCrusher.y = (float)Convert.ToDecimal(inFile.ReadLine());
                    newCrusher.z = (float)Convert.ToDecimal(inFile.ReadLine());
                    newCrusher.height = (float)Convert.ToDecimal(inFile.ReadLine());
                    newCrusher.speed = (float)Convert.ToDecimal(inFile.ReadLine());
                    roomList[selectedRoom].crushers.Add(newCrusher);
                    //CrusherListBox.Items.Add(newCrusher);
                    _updateCrusher(selectedRoom, -1, newCrusher.x, newCrusher.y, newCrusher.z, newCrusher.height, newCrusher.speed);
                }
                //
                int numLasers = Convert.ToInt32(inFile.ReadLine());
                for (int objectIter = 0; objectIter < numLasers; objectIter++)
                {
                    Laser newLaser = new Laser();
                    newLaser.x = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLaser.y = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLaser.z = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLaser.length = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLaser.timeOn = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLaser.timeOff = (float)Convert.ToDecimal(inFile.ReadLine());
                    newLaser.horizontal = Convert.ToBoolean(Convert.ToInt32(inFile.ReadLine()));
                    roomList[selectedRoom].lasers.Add(newLaser);
                    //LaserListBox.Items.Add(newLaser);
                    _updateLaser(selectedRoom, -1, newLaser.horizontal, newLaser.x, newLaser.y, newLaser.z, newLaser.length, newLaser.timeOn, newLaser.timeOff);
                }
                //
                int numPickups = Convert.ToInt32(inFile.ReadLine());
                for (int objectIter = 0; objectIter < numPickups; objectIter++)
                {
                    Pickup newPickup = new Pickup();
                    newPickup.x = (float)Convert.ToDecimal(inFile.ReadLine());
                    newPickup.y = (float)Convert.ToDecimal(inFile.ReadLine());
                    newPickup.z = (float)Convert.ToDecimal(inFile.ReadLine());
                    newPickup.type = (Pickup.PickupTypes)Convert.ToInt32(inFile.ReadLine());
                    roomList[selectedRoom].pickups.Add(newPickup);
                    //PickupListBox.Items.Add(newPickup);
                    _updatePickup(selectedRoom, -1, newPickup.x, newPickup.y, newPickup.z, (int)newPickup.type);
                }
                int numFans = Convert.ToInt32(inFile.ReadLine());
                for (int objectIter = 0; objectIter < numFans; objectIter++)
                {
                    WindFan newFan = new WindFan();
                    newFan.x = (float)Convert.ToDecimal(inFile.ReadLine());
                    newFan.y = (float)Convert.ToDecimal(inFile.ReadLine());
                    newFan.z = (float)Convert.ToDecimal(inFile.ReadLine());
                    newFan.rotation = (float)Convert.ToDecimal(inFile.ReadLine());
                    newFan.speed = (float)Convert.ToDecimal(inFile.ReadLine());
                    newFan.length = (float)Convert.ToDecimal(inFile.ReadLine());
                    roomList[selectedRoom].windFans.Add(newFan);
                    //WindFanListBox.Items.Add(newFan);
                    _updateWindFan(selectedRoom, -1, newFan.x, newFan.y, newFan.z, newFan.rotation, newFan.length, newFan.speed);
                }
                inFile.Close();
            }
        }

        private void Tool_ExportRoom_Click(object sender, EventArgs e)
        {
            int selectedRoom = RoomListBox.SelectedIndex;
            if(selectedRoom == -1)
                return;
            SaveFileDialog output = new SaveFileDialog();
            output.AddExtension = true;
            output.DefaultExt = ".txt";
            output.Filter = "Room file (*.rm)|*.rm";
            if (DialogResult.OK == output.ShowDialog())
            {
                StreamWriter outFile = new StreamWriter(output.FileName.ToString());
                outFile.WriteLine(roomList[selectedRoom].x);
                outFile.WriteLine(roomList[selectedRoom].y);
                outFile.WriteLine(roomList[selectedRoom].width);
                outFile.WriteLine(roomList[selectedRoom].height);
                outFile.WriteLine(roomList[selectedRoom].backgroundID);
                outFile.WriteLine(Convert.ToInt32(roomList[selectedRoom].checkpoint));
                outFile.WriteLine(roomList[selectedRoom].checkpointX);
                outFile.WriteLine(roomList[selectedRoom].checkpointY);

                // In each room export every wall
                outFile.WriteLine(roomList[selectedRoom].wallTiles.Count);
                for (int wallIter = 0; wallIter < roomList[selectedRoom].wallTiles.Count; wallIter++)
                {
                    outFile.WriteLine(roomList[selectedRoom].wallTiles[wallIter].x);
                    outFile.WriteLine(roomList[selectedRoom].wallTiles[wallIter].y);
                    outFile.WriteLine(roomList[selectedRoom].wallTiles[wallIter].size);
                    outFile.WriteLine(Convert.ToInt32(roomList[selectedRoom].wallTiles[wallIter].horizontal));
                }
                // In each room export every light
                outFile.WriteLine(roomList[selectedRoom].lights.Count);
                for (int lightIter = 0; lightIter < roomList[selectedRoom].lights.Count; lightIter++)
                {
                    outFile.WriteLine(Convert.ToInt32(roomList[selectedRoom].lights[lightIter].type));
                    outFile.WriteLine(roomList[selectedRoom].lights[lightIter].x);
                    outFile.WriteLine(roomList[selectedRoom].lights[lightIter].y);
                    outFile.WriteLine(roomList[selectedRoom].lights[lightIter].z);
                    outFile.WriteLine(roomList[selectedRoom].lights[lightIter].red);
                    outFile.WriteLine(roomList[selectedRoom].lights[lightIter].green);
                    outFile.WriteLine(roomList[selectedRoom].lights[lightIter].blue);
                    outFile.WriteLine(roomList[selectedRoom].lights[lightIter].radius);
                    outFile.WriteLine(roomList[selectedRoom].lights[lightIter].intensity);
                }
                // In each room export every game object
                outFile.WriteLine(roomList[selectedRoom].objects.Count);
                for (int objectIter = 0; objectIter < roomList[selectedRoom].objects.Count; objectIter++)
                {
                    outFile.WriteLine(roomList[selectedRoom].objects[objectIter].x);
                    outFile.WriteLine(roomList[selectedRoom].objects[objectIter].y);
                    outFile.WriteLine(roomList[selectedRoom].objects[objectIter].objectID);
                    outFile.WriteLine(roomList[selectedRoom].objects[objectIter].rotation);
                    outFile.WriteLine(roomList[selectedRoom].objects[objectIter].flag);
                }
                // In each room export every door
                outFile.WriteLine(roomList[selectedRoom].doors.Count);
                for (int doorIter = 0; doorIter < roomList[selectedRoom].doors.Count; doorIter++)
                {
                    outFile.WriteLine(roomList[selectedRoom].doors[doorIter].x);
                    outFile.WriteLine(roomList[selectedRoom].doors[doorIter].y);
                    outFile.WriteLine(Convert.ToInt32(roomList[selectedRoom].doors[doorIter].breakable));
                    outFile.WriteLine(roomList[selectedRoom].doors[doorIter].rotation);
                    outFile.WriteLine(roomList[selectedRoom].doors[doorIter].roomID);
                    outFile.WriteLine(roomList[selectedRoom].doors[doorIter].roomX);
                    outFile.WriteLine(roomList[selectedRoom].doors[doorIter].roomY);
                }
                // In each room export every platform
                outFile.WriteLine(roomList[selectedRoom].movingPlatforms.Count);
                for (int platformIter = 0; platformIter < roomList[selectedRoom].movingPlatforms.Count; platformIter++)
                {
                    outFile.WriteLine(roomList[selectedRoom].movingPlatforms[platformIter].waypoints.Count + 1);
                    outFile.WriteLine(roomList[selectedRoom].movingPlatforms[platformIter].x);
                    outFile.WriteLine(roomList[selectedRoom].movingPlatforms[platformIter].y);
                    for (int waypointIter = 0; waypointIter < roomList[selectedRoom].movingPlatforms[platformIter].waypoints.Count; waypointIter++)
                    {
                        outFile.WriteLine(roomList[selectedRoom].movingPlatforms[platformIter].waypoints[waypointIter].x);
                        outFile.WriteLine(roomList[selectedRoom].movingPlatforms[platformIter].waypoints[waypointIter].y);
                    }
                    //outFile.WriteLine(roomList[selectedRoom].movingPlatforms[platformIter].endX);
                    //outFile.WriteLine(roomList[selectedRoom].movingPlatforms[platformIter].endY);
                    outFile.WriteLine(roomList[selectedRoom].movingPlatforms[platformIter].rotation);
                    outFile.WriteLine(roomList[selectedRoom].movingPlatforms[platformIter].speed);
                }
                // In each room export every enemy
                outFile.WriteLine(roomList[selectedRoom].enemies.Count);
                for (int enemyIter = 0; enemyIter < roomList[selectedRoom].enemies.Count; enemyIter++)
                {
                    outFile.WriteLine(roomList[selectedRoom].enemies[enemyIter].x);
                    outFile.WriteLine(roomList[selectedRoom].enemies[enemyIter].y);
                    outFile.WriteLine(roomList[selectedRoom].enemies[enemyIter].endX);
                    outFile.WriteLine(roomList[selectedRoom].enemies[enemyIter].endY);
                    outFile.WriteLine((int)roomList[selectedRoom].enemies[enemyIter].type);
                    outFile.WriteLine(roomList[selectedRoom].enemies[enemyIter].speed);
                }
                // In each room export every background object
                outFile.WriteLine(roomList[selectedRoom].backgroundObjects.Count);
                for (int objectIter = 0; objectIter < roomList[selectedRoom].backgroundObjects.Count; objectIter++)
                {
                    outFile.WriteLine(roomList[selectedRoom].backgroundObjects[objectIter].x);
                    outFile.WriteLine(roomList[selectedRoom].backgroundObjects[objectIter].y);
                    outFile.WriteLine(roomList[selectedRoom].backgroundObjects[objectIter].z);
                    outFile.WriteLine(roomList[selectedRoom].backgroundObjects[objectIter].rotation);
                    outFile.WriteLine(0);   // quick replacement. used to have roty and rotz but no longer using 
                    outFile.WriteLine(0);   // those variables need to update save files
                    outFile.WriteLine(roomList[selectedRoom].backgroundObjects[objectIter].objectID);
                }
                //
                outFile.WriteLine(roomList[selectedRoom].crushers.Count);
                for (int objectIter = 0; objectIter < roomList[selectedRoom].crushers.Count; objectIter++)
                {
                    outFile.WriteLine(roomList[selectedRoom].crushers[objectIter].x);
                    outFile.WriteLine(roomList[selectedRoom].crushers[objectIter].y);
                    outFile.WriteLine(roomList[selectedRoom].crushers[objectIter].z);
                    outFile.WriteLine(roomList[selectedRoom].crushers[objectIter].height);
                    outFile.WriteLine(roomList[selectedRoom].crushers[objectIter].speed);
                }
                //
                outFile.WriteLine(roomList[selectedRoom].lasers.Count);
                for (int objectIter = 0; objectIter < roomList[selectedRoom].lasers.Count; objectIter++)
                {
                    outFile.WriteLine(roomList[selectedRoom].lasers[objectIter].x);
                    outFile.WriteLine(roomList[selectedRoom].lasers[objectIter].y);
                    outFile.WriteLine(roomList[selectedRoom].lasers[objectIter].z);
                    outFile.WriteLine(roomList[selectedRoom].lasers[objectIter].length);
                    outFile.WriteLine(roomList[selectedRoom].lasers[objectIter].timeOn);
                    outFile.WriteLine(roomList[selectedRoom].lasers[objectIter].timeOff);
                    outFile.WriteLine(Convert.ToInt32(roomList[selectedRoom].lasers[objectIter].horizontal));
                }
                //
                outFile.WriteLine(roomList[selectedRoom].pickups.Count);
                for (int objectIter = 0; objectIter < roomList[selectedRoom].pickups.Count; objectIter++)
                {
                    outFile.WriteLine(roomList[selectedRoom].pickups[objectIter].x);
                    outFile.WriteLine(roomList[selectedRoom].pickups[objectIter].y);
                    outFile.WriteLine(roomList[selectedRoom].pickups[objectIter].z);
                    outFile.WriteLine(Convert.ToInt32(roomList[selectedRoom].pickups[objectIter].type));
                }
                // 
                outFile.WriteLine(roomList[selectedRoom].windFans.Count);
                for (int objectIter = 0; objectIter < roomList[selectedRoom].windFans.Count; objectIter++)
                {
                    outFile.WriteLine(roomList[selectedRoom].windFans[objectIter].x);
                    outFile.WriteLine(roomList[selectedRoom].windFans[objectIter].y);
                    outFile.WriteLine(roomList[selectedRoom].windFans[objectIter].z);
                    outFile.WriteLine(roomList[selectedRoom].windFans[objectIter].rotation);
                    outFile.WriteLine(roomList[selectedRoom].windFans[objectIter].speed);
                    outFile.WriteLine(roomList[selectedRoom].windFans[objectIter].length);
                }
                outFile.Close();
            }
        }

        /****************************************************************/
        #endregion
        /****************************************************************/
    }
}