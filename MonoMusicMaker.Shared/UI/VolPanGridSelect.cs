using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using NAudio.Midi;


namespace MonoMusicMaker
{
    public class VolPanGridSelect
    {
        public bool Visible { get; set; } = false;
        Vector2 mScreenPos;   //top left
        Vector2 mScreenPosDot;   //top left

        const int POS_GRID_X = 500;
        const int POS_GRID_Y = MelodyEditorInterface.TRACK_START_Y - PlayArea.GRAY_RECTANGLE_Y_LIFT;
        const int GRID_SQUARE_DIMENSION = PlayArea.NUM_DRAW_LINES* NoteLine.TRACK_HEIGHT +PlayArea.GRAY_RECTANGLE_Y_LIFT;
        const int SELECT_DOT_DIMENSION = 60;
        const int SELECT_DOT_DIMENSION_HALF = SELECT_DOT_DIMENSION/2;
        const int SELECT_DOT_RANGE = GRID_SQUARE_DIMENSION - SELECT_DOT_DIMENSION;
        const int OFFSET_CENTRE_GRID = GRID_SQUARE_DIMENSION / 2;
        const int OFFSET_CENTRE_DOT = OFFSET_CENTRE_GRID - SELECT_DOT_DIMENSION / 2;

        public Rectangle mSquareAreaRectangle;
        public Rectangle mSelectRectangle;
        Texture2D mSelectDotTexture;
        Texture2D mGridTexture;

        public VolPanGridSelect(Vector2 screenPos)
        {
            mScreenPos = screenPos;

            mSquareAreaRectangle = new Rectangle(POS_GRID_X, POS_GRID_Y , GRID_SQUARE_DIMENSION, GRID_SQUARE_DIMENSION);
            mSelectRectangle = new Rectangle(POS_GRID_X+ OFFSET_CENTRE_DOT, POS_GRID_Y+ OFFSET_CENTRE_DOT, SELECT_DOT_DIMENSION, SELECT_DOT_DIMENSION);
        }

        public void LoadContent(ContentManager contentMan)
        {
            mSelectDotTexture = contentMan.Load<Texture2D>("WhiteFilledCircle");
            mGridTexture = contentMan.Load<Texture2D>("grid"); ;
        }

        public void Start(MelodyEditorInterface.MEIState state)
        {
            System.Diagnostics.Debug.Assert(!Visible, string.Format(" Visible already!?"));

            Visible = true;

            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();

            float volRatio = 1f - (float)pa.mVolume / (float)MidiBase.MIDI_PARAM_RANGE;
            float panRatio = (float)pa.mPan / (float)MidiBase.MIDI_PARAM_RANGE;

            int selectX = POS_GRID_X + (int)(SELECT_DOT_RANGE * panRatio);
            int selectY = POS_GRID_Y + (int)(SELECT_DOT_RANGE * volRatio);

            mSelectRectangle = new Rectangle(selectX, selectY, SELECT_DOT_DIMENSION, SELECT_DOT_DIMENSION);
        }

        public void UpdateInput(MelodyEditorInterface.MEIState state)
        {
            if (state.input.Held)
            {
                if (state.input.mRectangle.Intersects(mSelectRectangle))
                {
                    if (Math.Abs(state.input.mDeltaX) > 0)
                    {
                        int startPosX = mSelectRectangle.X;
                        if (mSelectRectangle.Left + state.input.mDeltaX < mSquareAreaRectangle.Left)
                        {
                            mSelectRectangle.X = mSquareAreaRectangle.X;
                        }
                        else if (mSelectRectangle.Right + state.input.mDeltaX > mSquareAreaRectangle.Right)
                        {
                            mSelectRectangle.X = mSquareAreaRectangle.Right - mSelectRectangle.Width;
                        }
                        else
                        {
                            mSelectRectangle.X += state.input.mDeltaX;
                        }
                        if (mSelectRectangle.X != startPosX)
                        {
                            int centrePosDiffX = mSelectRectangle.X - mSquareAreaRectangle.Left;
                            float panRatio = (float)centrePosDiffX / (float)SELECT_DOT_RANGE;
                            //Set play area pan
                            //int beatPixDiff = mSelectRectangle.X - mSquareAreaTrackRectangle.X;
                            //float floatBeatDiff = BEAT_CHUNKS * beatPixDiff;

                            //state.mDebugPanel.SetFloat(panRatio, "panRatio");
                            state.mMeledInterf.SetCurrentAreaPan(panRatio);
                        }
                    }
                    if (Math.Abs(state.input.mDeltaY) > 0)
                    {
                        int startPosY = mSelectRectangle.Y;
                        if (mSelectRectangle.Top + state.input.mDeltaY < mSquareAreaRectangle.Top)
                        {
                            mSelectRectangle.Y = mSquareAreaRectangle.Y;
                        }
                        else if (mSelectRectangle.Bottom + state.input.mDeltaY > mSquareAreaRectangle.Bottom)
                        {
                            mSelectRectangle.Y = mSquareAreaRectangle.Bottom - mSelectRectangle.Height;
                        }
                        else
                        {
                            mSelectRectangle.Y += state.input.mDeltaY;
                        }

                        if (mSelectRectangle.Y != startPosY)
                        {
                            int centrePosDiffY = mSelectRectangle.Y - mSquareAreaRectangle.Top;
                            float volRatio = 1f - (float)centrePosDiffY / (float)SELECT_DOT_RANGE;
 
                            //Set play area volume
                            //int beatPixDiff = mSelectRectangle.X - mSquareAreaTrackRectangle.X;
                            //float floatBeatDiff = BEAT_CHUNKS * beatPixDiff;

                            //state.mDebugPanel.SetFloat(volRatio, "volRatio");
                            state.mMeledInterf.SetCurrentAreaVolume(volRatio);
                        }
                    }
                }
            }
        }

        public void Draw(SpriteBatch sb)
        {
            if (!Visible)
            {
                return;
            }

   
             sb.Begin();
             sb.Draw(mGridTexture,
                mSquareAreaRectangle,
                Color.LightBlue*0.6f);

            sb.Draw(mSelectDotTexture,
                mSelectRectangle,
                Color.Blue);

            sb.End();
        }
    }
}
