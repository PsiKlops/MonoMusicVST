using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace MonoMusicMaker
{
    public class DebugPanel
    {
        public bool Visible { get; set; } = false;

        class FloatData
        {
            public String mName = "";
            public String mStr="";
            public float mValue = 0;
        };

        //TODO var map = new Dictionary<string, string>();
        List<FloatData> mFloatData = new List<FloatData>();
        Vector2 mvScreenPos;
        SpriteFont mFont;

        public void LoadContent(ContentManager contentMan, SpriteFont font)
        {
            mFont = font;
        }

        public void Init()
        {
        }

        public void SetPos(int x , int y)
        {
            mvScreenPos = new Vector2(x , y);
        }

        FloatData GetFD(String name)
        {
            FloatData rfd = null;
            foreach (FloatData fd in mFloatData)
            {
                if(fd.mName == name)
                {
                    return fd;
                }
            }
            return rfd;
        }

        public void SetFloat(float fValue, String name  ="")
        {
            FloatData fd = GetFD(name);

            if (fd == null)
            {
                FloatData newFdata = new FloatData();
                newFdata.mValue = fValue;
                newFdata.mName = name;
                newFdata.mStr = string.Format("{0} float {1}", name, fValue);
                mFloatData.Add(newFdata);
                return;
            }

            fd.mValue = fValue;
            fd.mStr = string.Format("{0} {1}", fd.mName, fValue);
        }

        public void Draw(SpriteBatch sb )
        {
            if (!Visible)
            {
                return;
            }

            sb.Begin();
            Vector2 screenPos = mvScreenPos;
            foreach (FloatData fd in mFloatData)
            {
                int stringHeight = (int)mFont.MeasureString(fd.mStr).Y;
                sb.DrawString(mFont, fd.mStr, screenPos, Color.Black);
                screenPos.Y = screenPos.Y + stringHeight;
            }
            sb.End();
        }
    }
}
