using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoMusicMaker //.MEI
{
    public class PlayHead :  GridLine
    {
        public float mBeatPos =0f;
        bool mbInDrawRange = true; //start true so when playing after shifting draw area the transition will be seen
        float mLastMS_Per_Beat = 0;
        float mUsingMS_Per_Beat = 0;
        public bool mbLeftView = false;
        PlayArea mParent = null;

        const int PLAY_HEAD_BASE_RECT_SIZE = 10;
        const int PLAY_HEAD_BASE_RECT_SIZE_HALF = PLAY_HEAD_BASE_RECT_SIZE/2;

        Rectangle mBaseRect = new Rectangle(0,0, PLAY_HEAD_BASE_RECT_SIZE, PLAY_HEAD_BASE_RECT_SIZE); //need to see where the play head is!

        public PlayHead(PlayArea pa)
        {
            mParent = pa;
        }

        public void Init(PlayArea parentArea, Color color, int initX = -1)
        {
            base.Init(parentArea, color, initX);

            int rectX = (int)mScreenEnd.X - PLAY_HEAD_BASE_RECT_SIZE_HALF;

            mBaseRect = new Rectangle(rectX, (int)mScreenEnd.Y, PLAY_HEAD_BASE_RECT_SIZE, PLAY_HEAD_BASE_RECT_SIZE); //need to see where the play head is!
        }

        public void UpdateMillisecond(int ms)
        {
            bool bWasInViewAtStartUpdate = mbInDrawRange && mParentArea.HasCurrentView;

            if (mParentArea.EndBar == mParentArea.StartBar)
            {
                //Dont update and return
                SetBeatPos(0);
                return;
            }

            //if(mUsingMS_Per_Beat==0)
            {
                mUsingMS_Per_Beat = mParentArea.mMS_Per_Beat;
            }
            //if (mLastMS_Per_Beat != 0 && mLastMS_Per_Beat != mParentArea.mMS_Per_Beat)
            //{
            //    mUsingMS_Per_Beat = MathHelper.Lerp(mLastMS_Per_Beat, mParentArea.mMS_Per_Beat, 0.1f);
            //}
            //float beatWidth = mParentArea.

            int totalBarMs = (int)(PlayArea.MAX_BEATS * mUsingMS_Per_Beat); //TODO TAKE OUT FOR BELOW

            totalBarMs = (mParentArea.EndBar - mParentArea.StartBar)*(int)(PlayArea.BEATS_PER_BAR * mUsingMS_Per_Beat);

            int barPosMs = ms % totalBarMs;

            float fCurrentBeat = barPosMs / mUsingMS_Per_Beat;

            fCurrentBeat += (float)mParentArea.StartBar * PlayArea.BEATS_PER_BAR;

            mbInDrawRange = false;

            if (mParentArea.HasCurrentView && fCurrentBeat > mParentArea.StartBeat && fCurrentBeat < (mParentArea.StartBeat + PlayArea.NUM_DRAW_BEATS))
            {
                mbInDrawRange = true;
            }
            mLastMS_Per_Beat = mUsingMS_Per_Beat;
            SetBeatPos(fCurrentBeat);


            if(bWasInViewAtStartUpdate && !mbInDrawRange)
            {
                mbLeftView = true;
            }
        }

        //This allows grabbing the play head and moving and setting a new offset for this channel/play area - if global flag set then the global state ms is changed to suit new offset
        public void SetGrabbedPlayHead(MelodyEditorInterface.MEIState state, float newBeatPos, bool bGlobal = false)
        {
            if(bGlobal)
            {
                state.mCurentPlayingTimeMs = (int)(newBeatPos * mUsingMS_Per_Beat);
            }
            else
            {
                float playHeadBeatPosDIfference = newBeatPos - mBeatPos;
                int millisecDiff = (int)(mUsingMS_Per_Beat * playHeadBeatPosDIfference);
                mParent.mPlayAreaPlayingTimeOffsetTimeMs += millisecDiff;
            }

            SetBeatPos(newBeatPos);
        }

        public void SetBeatPos(float beatPos)
        {
            mBeatPos = beatPos;
            mbInDrawRange |= (mBeatPos == 0);
            mScreenStart.X = (beatPos - mParentArea.StartBeat) * mPixelsPerBeat + mfInitX;
            mScreenEnd.X = mScreenStart.X;

            int rectX = (int)mScreenEnd.X - PLAY_HEAD_BASE_RECT_SIZE_HALF;
            mBaseRect.X = rectX;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if(mbInDrawRange)
            {
                base.Draw(spriteBatch);

                spriteBatch.FillRectangle(mBaseRect, Color.Blue);
            }
        }
    }
}
