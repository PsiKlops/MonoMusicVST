using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonoMusicMaker
{
    public class ColorGenerator
    {
        private int _colorIndex;
        public ColorGenerator()
        {

        }

        public Color Next()
        {
            _colorIndex++;
            byte byteIndex = (byte)(_colorIndex & 0x1F);
            int divider = byteIndex >> 3;


            byte baseByte = 0xFF;

            if(divider>0)
            {
                divider++;
                baseByte = (byte)(baseByte / divider);
            }

            byte colorByte = (byte)(baseByte);

            Color retCol = Color.Gray;
            if((byteIndex & 0x07) != 0)
            {
                byte red = (byteIndex & 0x01) == 1 ? (byte)0xff : (byte)0;
                byte green = (byteIndex & 0x02) == 2 ? (byte)0xff : (byte)0;
                byte blue = (byteIndex & 0x04) == 4 ? (byte)0xff : (byte)0;

                retCol =  new Color(red, green, blue);

            }
            return retCol;
        }
    }
}
