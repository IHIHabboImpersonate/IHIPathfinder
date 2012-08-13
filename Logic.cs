#region GPLv3

// 
// Copyright (C) 2012  Chris Chenery
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

#endregion

#region Usings

using System;
using System.Collections.Generic;
using IHI.Server.Rooms.RoomUnits;

#endregion

namespace IHI.Server.Plugins.Cecer1.IHIPathfinder
{
    /// <summary>
    ///   The logic of the pathfinder. This calculates all the paths.
    /// </summary>
    internal class Logic : IPathfinder
    {
        /// <summary>
        ///   Stores the state of the tiles.
        /// </summary>
        private byte[,] _collisionMap;

        /// <summary>
        ///   Stores the height of the tiles.
        /// </summary>
        private float[,] _height;

        #region IPathfinder Members

        /// <summary>
        ///   Set the tile map.
        /// </summary>
        /// <param name = "map">The state of the tiles.</param>
        /// <param name = "height">The height of the tiles.</param>
        public void ApplyCollisionMap(byte[,] map, float[,] height)
        {
            // Is this replacing an existing tile map
            if (_collisionMap != null)
                // Yes, ensure thread safety.
                lock (_collisionMap)
                {
                    // Set the tile states.
                    _collisionMap = map;
                    // Set the tile heights
                    _height = height;
                }
            else
            {
                // No, don't worry about other threads.

                // Set the tile states.
                _collisionMap = map;
                // Set the tile heights
                _height = height;
            }
        }

        /// <summary>
        ///   Get the next step on a path.
        /// </summary>
        /// <param name = "startX">PointA X</param>
        /// <param name = "startY">PointA Y</param>
        /// <param name = "endX">PointB X</param>
        /// <param name = "endY">PointB Y</param>
        /// <param name = "maxDrop">The Maximum height to drop in a single step.</param>
        /// <param name = "maxJump">The Maximum height to rise in a single step.</param>
        /// <returns></returns>
        public ICollection<byte[]> Path(byte startX, byte startY, byte endX, byte endY, float maxDrop, float maxJump)
        {
            Values values;
            lock (_collisionMap) // Thread Safety
            {
                values = new Values(_collisionMap, _height, maxDrop, maxJump);

                if (endX >= _collisionMap.GetLength(0) || // Is EndX outside the bounds of the collision map?
                    endY >= _collisionMap.GetLength(1) || // Is EndY outside the bounds of the collision map?
                    startX >= _collisionMap.GetLength(0) || // Is StartX outside the bounds of the collision map?
                    startY >= _collisionMap.GetLength(1) || // Is StartY outside the bounds of the collision map?
                    _collisionMap[endX, endY] == 0 || // Is the target blocked by the collision map?
                    (startX == endX && startY == endY)) // Is the start also the target?
                    
                    // If any of these are yes, no path can be made. Don't run the path finder.
                    return new byte[0][];

                #region Init
                values.Count++;
                values.BinaryHeap[values.Count] = values.LastID;
                values.X[values.LastID] = startX;
                values.Y[values.LastID] = startY;
                values.H[values.LastID] = (ushort) GetH(startX, startY, endX, endY);
                values.Parent[values.LastID] = 0;
                values.G[values.LastID] = 0;
                values.F[values.LastID] = (ushort) (values.G[values.LastID] + values.H[values.LastID]);

                #endregion

                while (values.Count != 0)
                {
                    values.Location = values.BinaryHeap[1];

                    if (values.X[values.Location] == endX && values.Y[values.Location] == endY)
                        break;

                    Move(values);

                    #region Add the surrounding tiles.
                    Add(-1, 0, endX, endY, values);
                    Add(0, -1, endX, endY, values);
                    Add(1, 0, endX, endY, values);
                    Add(0, 1, endX, endY, values);

                    Add(-1, -1, endX, endY, values);
                    Add(-1, 1, endX, endY, values);
                    Add(1, -1, endX, endY, values);
                    Add(1, 1, endX, endY, values);
                    #endregion
                }
            }

            // If no new tiles can be checked then the path must be impossible.
            if (values.Count == 0)
                return new byte[0][];

            List<byte[]> path = new List<byte[]>();

            while (values.X[values.Parent[values.Location]] != startX ||
                   values.Y[values.Parent[values.Location]] != startY)
            {
                path.Add(new[] {values.X[values.Location], values.Y[values.Location]});
                values.Location = values.Parent[values.Location];
            }
            path.Add(new[] {values.X[values.Location], values.Y[values.Location]});
            path.Reverse();

            return path;
        }

        #endregion

        /// <summary>
        ///   Estimate the cost from X,Y to EndX,EndY.
        /// </summary>
        /// <returns></returns>
        private static int GetH(int x, int y, int endX, int endY)
        {
            return (Math.Abs(x + endX) + Math.Abs(y + endY));
        }

        /// <summary>
        /// </summary>
        private void Add(sbyte x, sbyte y, byte endX, byte endY, Values values)
        {
            byte x2 = (byte) (values.X[values.Location] + x);
            byte y2 = (byte) (values.Y[values.Location] + y);
            ushort parent = values.Location;

            #region PATHFINDER RULE: Disallow (non-)tiles beyond the map
            if (x2 >= _collisionMap.GetLengt/h(0) || y2 >= _collisionMap.GetLength(1))
                return;
            #endregion

            #region PATHFINDER RULE: Disallow tiles that make up the current path.
            if (values.Tiles[x2, y2] == 2)
                return;
            #endregion
            #region PATHFINDER RULE: Disallow closed tiles
            if (_collisionMap[x2, y2] == 0)
                return;
            #endregion
            #region PATHFINDER RULE: Disallow interactive tiles EXCEPT for the destination tile.
            if (_collisionMap[x2, y2] == 2 && (x2 != endX || y2 != endY))
                return;
            #endregion

            float z = values.Z[x2, y2];
            float z2 = values.Z[values.X[parent], values.Y[parent]];

            #region PATHFINDER RULE: Disallow height changes beyond the limit
            if (z > z2 + values.MaxJump || z < z2 - values.MaxDrop)
                return;
            #endregion

            if (parent > 0)
            {
                #region PATHFINDER RULE: Disallow parernt tile (backtracking)
                if (values.X[parent] == x2 && values.Y[parent] == y2)
                    return;
                #endregion
                #region PATHFINDER RULE: Disallow diagonals when walking though solid/interactive corners
                if (_collisionMap[x2, values.Y[parent]] == 0 || _collisionMap[x2, values.Y[parent]] == 2)
                    return;
                if (_collisionMap[values.X[parent], y2] == 0 || _collisionMap[values.X[parent], y2] == 2)
                    return;
                #endregion
            }


            if (values.Tiles[x2, y2] == 1)
            {
                ushort i = 1;
                for (; i <= values.Count; i++)
                {
                    if (values.X[i] == x2 && values.Y[i] == y2)
                        break;
                }

                if (values.X[i] == endX || values.Y[i] == endY)
                {
                    if (10 + values.G[parent] < values.G[i])
                        values.Parent[i] = parent;
                }
                else if (14 + values.G[parent] < values.G[i])
                    values.Parent[i] = parent;
                return;
            }

            values.LastID++;
            values.Count++;
            values.BinaryHeap[values.Count] = values.LastID;
            values.X[values.LastID] = x2;
            values.Y[values.LastID] = y2;
            values.H[values.LastID] = (ushort) GetH(x2, y2, endX, endY);
            values.Parent[values.LastID] = parent;

            if (x2 == values.X[parent] || y2 == values.Y[parent])
                values.G[values.LastID] = (ushort) (10 + values.G[parent]);
            else
                values.G[values.LastID] = (ushort) (14 + values.G[parent]);
            values.F[values.LastID] = (ushort) (values.G[values.LastID] + values.H[values.LastID]);

            for (ushort c = values.Count; c != 1; c /= 2)
            {
                if (values.F[values.BinaryHeap[c]] > values.F[values.BinaryHeap[c/2]])
                    break;
                ushort temp = values.BinaryHeap[c/2];
                values.BinaryHeap[c/2] = values.BinaryHeap[c];
                values.BinaryHeap[c] = temp;
            }
            values.Tiles[x2, y2] = 1;
        }

        private static void Move(Values values)
        {
            values.Tiles[values.X[values.BinaryHeap[1]], values.Y[values.BinaryHeap[1]]] = 2;


            values.BinaryHeap[1] = values.BinaryHeap[values.Count];
            values.Count--;

            ushort location = 1;
            while (true)
            {
                ushort high = location;
                if (2*high + 1 <= values.Count)
                {
                    if (values.F[values.BinaryHeap[high]] >= values.F[values.BinaryHeap[2*high]])
                        location = (ushort) (2*high);
                    if (values.F[values.BinaryHeap[location]] >= values.F[values.BinaryHeap[2*high + 1]])
                        location = (ushort) (2*high + 1);
                }
                else if (2*high <= values.Count)
                {
                    if (values.F[values.BinaryHeap[high]] >= values.F[values.BinaryHeap[2*high]])
                        location = (ushort) (2*high);
                }

                if (high == location)
                    break;
                ushort temp = values.BinaryHeap[high];
                values.BinaryHeap[high] = values.BinaryHeap[location];
                values.BinaryHeap[location] = temp;
            }
        }
    }
}