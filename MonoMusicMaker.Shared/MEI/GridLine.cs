using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoMusicMaker //.MEI
{
    public class GridLine
    {
        protected float mfInitX;
        protected Vector2 mScreenStart;
        protected Vector2 mScreenEnd;
        protected float mPixelsPerBeat = MelodyEditorInterface.BEAT_PIXEL_WIDTH;
        protected  PlayArea mParentArea;
        protected Color mColor = Color.LightBlue;
        public String TopText { get; set; } = "";
        public SpriteFont mFont;
        public bool mBarLine = false;

        public void Init(PlayArea parentArea, Color color, int initX = -1 )
        {
            mColor = color;
            mParentArea = parentArea;
            if(initX==-1)
            {
                mfInitX = mParentArea.mAreaRectangle.X;
            }
            else
            {
                mfInitX = initX;
            }
            mScreenStart.X = mfInitX;
            mScreenEnd.X = mfInitX;
            mScreenStart.Y = mParentArea.mAreaRectangle.Y;
            mScreenEnd.Y = mParentArea.mAreaRectangle.Y + mParentArea.mAreaRectangle.Height;
        }

        public void UpdateSCreenX(int x)
        {
            mScreenStart.X = x;
            mScreenEnd.X = x;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawLine(mScreenStart, mScreenEnd, mBarLine ? mColor : Color.Black, mBarLine ? 3:1);
            if (!string.IsNullOrEmpty(TopText))
            {
                Vector2 topTextPos = new Vector2(mScreenStart.X-30, mScreenStart.Y);

                spriteBatch.DrawString(mFont, TopText, topTextPos, Color.Black);
            }
        }
    }
}
