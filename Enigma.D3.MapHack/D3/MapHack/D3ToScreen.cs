﻿using Enigma.D3;
using Enigma.Memory;
using Enigma.D3.DataTypes;
using Enigma.Wpf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Enigma.D3.MemoryModel;
using Enigma.D3.MemoryModel.Core;

namespace Enigma.D3.MapHack
{
    public class D3ToScreen
    {
     

        //public static System.Drawing.Point FromD3toScreenCoords(SharpDX.Vector3 objectGameLocation)
        //{
        //    Enigma.D3.DataTypes.Vector3 vec = new Enigma.D3.DataTypes.Vector3() { X = objectGameLocation.X, Y = objectGameLocation.Y, Z = objectGameLocation.Z };
        //    return FromD3toScreenCoords(vec);
        //}

        public static System.Drawing.Point FromD3toScreenCoords(Vector3 objectGameLocation, Vector3 currentCharGameLoc)
        {
            

        //    var localAcd = ActorCommonDataHelper.GetLocalAcd();
        //    Vector3 currentCharGameLoc = new Vector3() { X = localAcd.x0D0_WorldPosX, Y = localAcd.x0D4_WorldPosY, Z = localAcd.x0D8_WorldPosZ };

            double xd = objectGameLocation.X - currentCharGameLoc.X;
            double yd = objectGameLocation.Y - currentCharGameLoc.Y;
            double zd = objectGameLocation.Z - currentCharGameLoc.Z;

            double w = -0.515 * xd + -0.514 * yd + -0.686 * zd + 97.985;
            double X = (-1.682 * xd + 1.683 * yd + 0 * zd + 7.045e-3) / w;
            double Y = (-1.54 * xd + -1.539 * yd + 2.307 * zd + 6.161) / w;

            double width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width; 
            double height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

            double aspectChange = (double)((double)width / (double)height) / (double)(4.0f / 3.0f); // 4:3 = default aspect ratio

            X /= aspectChange;

            float rX = (float)((X + 1) / 2 * width);
            float rY = (float)((1 - Y) / 2 * height);

            if ((uint)rX > width || (uint)rY > height)
            {


            }

            return new System.Drawing.Point((int)rX, (int)rY);
        }
    }
}

