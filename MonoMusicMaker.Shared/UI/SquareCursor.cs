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


//Used to allow moving the interact window over whole range of play area
namespace MonoMusicMaker
{
    public class SquareCursor
    {
        public delegate bool IsQuarterNoteHereDel(float pos);

        float mMainSelectNoteOffset = 0f;
        float mSelectAreaNoteOffset = 0f;
        public SpriteFont mFont;

        const int POINTING_HELD_SCROLL_RATE = 9;
        const int TEXT_GAP = 40;
        const int PLAY_AREA_GAP = 10; 
        public const int SC_XPOS = MelodyEditorInterface.TRACK_START_X + PlayArea.WIDTH_DRAW_AREA + TEXT_GAP + PLAY_AREA_GAP;

        public const int SC_YPOS = 304; // OLD with 8 Notes less 434;
        public const int WIDTH_RANGE_RECT  = PIX_PER_BEAT*6;
        public const int HEIGHT_RANGE_RECT = 100;

        public const int SC_TEXT_XPOS = SC_XPOS - TEXT_GAP;

        public const int SC_TEXT_MAIN_YPOS = 434;
        public const int SC_TEXT_RANGE_YPOS = 464;

#if WINDOWS
        const float BEATS_SHOWN = 64;
#else
        const float BEATS_SHOWN = 32;
#endif
        const int PIX_PER_BEAT = 12;
        const float BEAT_CHUNKS = 1f / (float)PIX_PER_BEAT;
        const int PIX_PER_TRACK = 7;
        const float TRACK_CHUNKS = 1f / (float)PIX_PER_TRACK;

        const int ABUT_BAR_WIDTH = 10;


        int mPlayHeadX = 0;

        Rectangle mSquareAreaTrackRectangle; //use to cover actual selectable trask area
        public Rectangle mSquareAreaRectangle;
        public Rectangle mSelectRectangle;
        Texture2D mCustomTexture;

        Texture2D mLeftTexture;
        Texture2D mRightTexture;

        public Rectangle mLeftPointingRectangle;
        public Rectangle mRightPointingRectangle;

        Rectangle mLeftStartBarIndicator; //only drawn to show area is at start
        Rectangle mRightEndBarIndicator; //only drawn to show area is abutting end
        bool mbDrawLeftStartBarIndicator = true;
        bool mbDrawRightEndBarIndicator = true;
        bool mbAllTrackAdjust = false; //WHen adjusting all tracks to same range will be used by draw to highlight buttons red

        void RefreshSideIndicators()
        {
            mbDrawLeftStartBarIndicator = false;
            mbDrawRightEndBarIndicator = false;

            if(mMainSelectNoteOffset==0f)
            {
                mbDrawLeftStartBarIndicator = true;
            }
            if ((mMainSelectNoteOffset + BEATS_SHOWN) >=PlayArea.MAX_BEATS)
            {
                mbDrawRightEndBarIndicator = true;
            }
        }

        const int AIREGION_TRACKS_TOP = 3;
        const int AIREGION_TRACK_HEIGHT = PIX_PER_TRACK* AIREGION_TRACKS_TOP;


        const int mWidthTex = PIX_PER_BEAT * (int)BEATS_SHOWN; // (int)PlayArea.MAX_BEATS;
        const int mHeightTex = PIX_PER_TRACK * PlayArea.FULL_RANGE + AIREGION_TRACK_HEIGHT;
        const int mSize = mWidthTex * mHeightTex;
        bool mbWaitForSelectRectTransitionToUp = false;
        bool mbWaitForMainRectTransitionToUp = false;
        bool mbWaitForRangeTransitionToUp = false;
        bool mbLeftRangeRectHeld = false;

        bool mbRightPointingHeldLeft = false;
        bool mbLeftPointingHeldRight = false;

        Color mDarkLine = new Color(.7f, .7f, .6f);

        Color mDarkerLine = new Color(.4f, .4f, .3f);
        Color mDarkerBeige = new Color(.7f, .7f, .6f);

        Color[] mTextureColors;
        public void BlankTexture()
        {
            mTextureColors = new Color[mSize];
            for (var i = 0; i < mSize; i++)
            {
                if (i < mSize / 2)
                {
                    mTextureColors[i] = Color.Silver;
                }
                else
                {
                    mTextureColors[i] = Color.Gold;
                }
            }
        }

        Texture2D tex;

        public SquareCursor(SpriteBatch spriteBatch, ContentManager contentMan, SpriteFont font)
        {
            mFont = font;
            mLeftTexture = contentMan.Load<Texture2D>("LeftArrow");
            mRightTexture = contentMan.Load<Texture2D>("RightArrow");

            BlankTexture();
            tex = new Texture2D(spriteBatch.GraphicsDevice, mWidthTex, mHeightTex, false, SurfaceFormat.Color);
            tex.SetData(mTextureColors);
            mCustomTexture = tex;

            float windowRatio = PlayArea.NUM_DRAW_BEATS / BEATS_SHOWN; // PlayArea.MAX_BEATS;
            mSquareAreaRectangle = new Rectangle(SC_XPOS, SC_YPOS, mWidthTex, mHeightTex);
            mSquareAreaTrackRectangle = new Rectangle(SC_XPOS, SC_YPOS + AIREGION_TRACK_HEIGHT, mWidthTex, mHeightTex- AIREGION_TRACK_HEIGHT);
            mSelectRectangle = new Rectangle(SC_XPOS, SC_YPOS + AIREGION_TRACK_HEIGHT, (int)(windowRatio*mWidthTex), PlayArea.NUM_DRAW_LINES * PIX_PER_TRACK);

            mScreenStart.Y = mSquareAreaRectangle.Y;
            mScreenEnd.Y = mSquareAreaRectangle.Y+ mSquareAreaRectangle.Height;

            int leftStartX = SC_XPOS;
            int rightStartX = SC_XPOS + mWidthTex - WIDTH_RANGE_RECT;
            int leftRangeY = SC_YPOS - HEIGHT_RANGE_RECT;
            int rightRangeY = SC_YPOS + mSquareAreaRectangle.Height;

            mRightPointingRectangle = new Rectangle(leftStartX, leftRangeY, WIDTH_RANGE_RECT, HEIGHT_RANGE_RECT);
            mLeftPointingRectangle = new Rectangle(rightStartX, rightRangeY, WIDTH_RANGE_RECT, HEIGHT_RANGE_RECT);

            int leftStartIndicatorX = SC_XPOS - ABUT_BAR_WIDTH;
            mLeftStartBarIndicator = new Rectangle(leftStartIndicatorX, SC_YPOS, ABUT_BAR_WIDTH, mHeightTex);
            mRightEndBarIndicator = new Rectangle(SC_XPOS + mWidthTex, SC_YPOS, ABUT_BAR_WIDTH, mHeightTex); //leave width at NoteLine.mTrackWidth for now since notes could be wide
        }

        public void SetToNewPlayHeadPosition(ref float fPlayHeadPos)
        {
            mMainSelectNoteOffset = fPlayHeadPos;
            mSelectAreaNoteOffset = 0f;

            if((mMainSelectNoteOffset + BEATS_SHOWN) > PlayArea.MAX_BEATS)
            {
                mMainSelectNoteOffset = PlayArea.MAX_BEATS - BEATS_SHOWN;

                //see if need move select too
                float newSelectRange = mMainSelectNoteOffset + PlayArea.NUM_DRAW_BEATS; //start beat of select window if moved to far right
                if (fPlayHeadPos >= newSelectRange)
                {
                    mSelectAreaNoteOffset = BEATS_SHOWN - PlayArea.NUM_DRAW_BEATS; //move select to far right to cover this
                }
                fPlayHeadPos = mSelectAreaNoteOffset + mMainSelectNoteOffset;
            }

            //Make sure to set select rectangle to new pos, abuttting  LHS of new dsiplay area
                int newX = SC_XPOS + (int)((float)mWidthTex * (float)mSelectAreaNoteOffset / (float)BEATS_SHOWN); // (float)PlayArea.MAX_BEATS);
            mSelectRectangle.X = newX;

            RefreshSideIndicators();
        }

        public void SetFromPlayArea(PlayArea pa)
        {
            int startBeat = (int)pa.StartBeat;
            int startLine = pa.StartDrawLine;

            if(startBeat > PlayArea.NUM_DRAW_BEATS)
            {
                mMainSelectNoteOffset = startBeat;
                mSelectAreaNoteOffset = 0f;

                //This is very similar to the checks in SetToNewPlayHeadPosition - this function and SetToNewPlayHeadPosition get called together and this was reseting the check from the previous,
                //I'm just fucking doing this again here to stop that cos I cant be arsed risking removing the other call
                if ((mMainSelectNoteOffset + BEATS_SHOWN) > PlayArea.MAX_BEATS)
                {
                    mMainSelectNoteOffset = PlayArea.MAX_BEATS - BEATS_SHOWN;

                    //see if need move select too
                    float newSelectRange = mMainSelectNoteOffset + PlayArea.NUM_DRAW_BEATS; //start beat of select window if moved to far right
                    if (startBeat >= newSelectRange)
                    {
                        mSelectAreaNoteOffset = BEATS_SHOWN - PlayArea.NUM_DRAW_BEATS; //move select to far right to cover this
                    }
                }
            }
            else
            {
                mMainSelectNoteOffset = 0f;
                mSelectAreaNoteOffset = startBeat;
            }

            int heightText = PlayArea.FULL_RANGE * PIX_PER_TRACK;

            int newX = SC_XPOS + (int)((float)mWidthTex * (float)mSelectAreaNoteOffset / (float)BEATS_SHOWN); // (float)PlayArea.MAX_BEATS);
            int newY = SC_YPOS + AIREGION_TRACK_HEIGHT + (int)((float)heightText * (float)startLine / (float)PlayArea.FULL_RANGE);

            mSelectRectangle.X = newX;
            mSelectRectangle.Y = newY;

            RefreshSideIndicators();
        }

        void UpdateRangeAdjustHeld(MelodyEditorInterface.MEIState state)
        {
            if (Math.Abs(state.input.mDeltaX) > 0)
            {
                if (state.input.mRectangle.Intersects(mLeftPointingRectangle))
                {
                    MoveRangeRect(ref mLeftPointingRectangle, state.input.mDeltaX, true);
                }
                else if (state.input.mRectangle.Intersects(mRightPointingRectangle))
                {
                    MoveRangeRect(ref mRightPointingRectangle, state.input.mDeltaX, false);
                }
            }

            if(mbRightPointingHeldLeft || mbLeftPointingHeldRight)
            {
                const int deltaX = POINTING_HELD_SCROLL_RATE; ;

                if (mbLeftPointingHeldRight && state.input.mRectangle.Intersects(mLeftPointingRectangle))
                {
                    MoveWholeAreaLeftOrRight(state, deltaX);
                }
                else if (mbRightPointingHeldLeft && state.input.mRectangle.Intersects(mRightPointingRectangle))
                {
                    MoveWholeAreaLeftOrRight(state, -deltaX);
                }
            }
        }

        void UpdateRangeAdjustSecondHeld(MelodyEditorInterface.MEIState state)
        {
            //state.mDebugPanel.SetFloat(state.input.mDeltaSecondTouchX, "LAST 2");

            if (Math.Abs(state.input.mDeltaSecondTouchX) > 0)
            {
                if (state.input.mSecondRectangle.Intersects(mLeftPointingRectangle))
                {
                    //state.mDebugPanel.SetFloat(state.input.mDeltaSecondTouchX, " < ");
                    MoveRangeRect(ref mLeftPointingRectangle, state.input.mDeltaSecondTouchX, true);
                }
                else if (state.input.mSecondRectangle.Intersects(mRightPointingRectangle))
                {
                    //state.mDebugPanel.SetFloat(state.input.mDeltaSecondTouchX, " > ");
                    MoveRangeRect(ref mRightPointingRectangle, state.input.mDeltaSecondTouchX, false);
                }
            }
        }
        void SetRangeRectToPlayArea(MelodyEditorInterface.MEIState state)
        {
            float StartBarBeat = state.mMeledInterf.GetCurrentPlayArea().StartBar * PlayArea.BEATS_PER_BAR;
            float EndBarBeat = state.mMeledInterf.GetCurrentPlayArea().EndBar * PlayArea.BEATS_PER_BAR; //why was it getting bar num then multiplying up by beat again?

            //float EndBarBeat = BEATS_SHOWN;

            float nominalRangeStart = mMainSelectNoteOffset;
            float noimnalRangeEnd = nominalRangeStart + BEATS_SHOWN;

            //Fix up start arrow for where ever the range is
            if(StartBarBeat < nominalRangeStart)
            {
                StartBarBeat = 0;
            }
            else if (StartBarBeat > noimnalRangeEnd)
            {
                StartBarBeat = BEATS_SHOWN;
            }
            else
            {
                StartBarBeat -= nominalRangeStart;
            }

            //Fix up end arrow for where ever the range is
            if (EndBarBeat < nominalRangeStart)
            {
                EndBarBeat = 0;
            }
            else if (EndBarBeat > noimnalRangeEnd)
            {
                EndBarBeat = BEATS_SHOWN;
            }
            else
            {
                EndBarBeat -= nominalRangeStart;
            }
            //else
            //{
            //    StartBarBeat -= nominalRangeStart;
            //}


            int rightPointePos = SC_XPOS + (int)(StartBarBeat * (float)PIX_PER_BEAT);
            int leftPointPos = SC_XPOS + (int)(EndBarBeat * (float)PIX_PER_BEAT) - WIDTH_RANGE_RECT;

            //Let go of held button if moving left or right extreme so reset these 
            System.Diagnostics.Debug.WriteLine(string.Format("RESET LEFT RIGHT STUFF ! "));

            if(!mbLeftPointingHeldRight)
            {
                mLeftPointingRectangle.X = leftPointPos;
            }
            if(!mbRightPointingHeldLeft)
            {
                mRightPointingRectangle.X = rightPointePos;
            }
        }

        void MoveRangeRect(ref Rectangle rect, int deltaX, bool bLeftPointing)
        {
            mbWaitForRangeTransitionToUp = true;
            int startPos = rect.X;

            mbRightPointingHeldLeft = false;
            mbLeftPointingHeldRight = false;

            Rectangle otherRect = mRightPointingRectangle; //On the left points right!

            int fudgeAdjust = 5; //Seems the rect wasn't always correcting down to 1 bar when released after being pulled furthest lefte. Seem sthis is because the rhs was just on the centre line of bar and kept getting set up, so I think this fudge will allow pulling it down in range
            int minLeft = mSquareAreaTrackRectangle.Left - fudgeAdjust;
            int maxRight = mSquareAreaTrackRectangle.Right + fudgeAdjust;

            mbLeftRangeRectHeld = bLeftPointing;

            if (!bLeftPointing)
            {
                //We must be looking at right pointing rect
                otherRect = mLeftPointingRectangle;

                if(otherRect.X < (maxRight - 2*otherRect.Width))
                {
                    maxRight = otherRect.X + 2*otherRect.Width;
                }
            }
            else
            {
                if (otherRect.X > (minLeft + otherRect.Width))
                {
                    minLeft = otherRect.X - otherRect.Width;
                }
            }

            if (rect.Left + deltaX < minLeft)
            {
                rect.X = minLeft;
                if (!bLeftPointing)
                {
                    mbRightPointingHeldLeft = true;
                    System.Diagnostics.Debug.WriteLine(string.Format("MIN LEFT <<<<<<<<<<<<<<<<< rect.X {0}", rect.X));
                }
            }
            else if (rect.Right + deltaX > maxRight)
            {
                rect.X = maxRight - rect.Width;

                if(bLeftPointing)
                {
                    mbLeftPointingHeldRight = true;
                    System.Diagnostics.Debug.WriteLine(string.Format("MAX RIGHT >>>>>>>>>>>>>>>> rect.X {0}", rect.X));
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(string.Format("DELTA rect.X {0} deltaX {1} ", rect.X, deltaX));
                rect.X += deltaX;
            }

            if (rect.X != startPos)
            {
                int beatPixDiff = rect.X - mSquareAreaTrackRectangle.X;
                float floatBeatDiff = BEAT_CHUNKS * beatPixDiff;
                //state.mMeledInterf.SetStartBeatPos(floatBeatDiff);
            }

            if(!mbLeftPointingHeldRight && !mbRightPointingHeldLeft)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("rect.X {0}", rect.X));
            }
        }

        bool SnapRangeRect(MelodyEditorInterface.MEIState state)
        {
            if(mbWaitForRangeTransitionToUp)
            {
                //Snap whichever needs snapping
                mbWaitForRangeTransitionToUp = false;

                int rectXPos = mRightPointingRectangle.X;

                if(mbLeftRangeRectHeld)
                {
                   rectXPos = mLeftPointingRectangle.X+ WIDTH_RANGE_RECT;
                }
                //Snap to bar in x direction
                int beatPixDiff = rectXPos - mSquareAreaTrackRectangle.X;
                float floatBeatDiff = BEAT_CHUNKS * beatPixDiff;
                float fOffset = floatBeatDiff % PlayArea.BEATS_PER_BAR;

                if (!mbLeftRangeRectHeld && fOffset < PlayArea.BEATS_PER_HALFBAR)
                {
                    floatBeatDiff -= fOffset;
                }
                else  if (mbLeftRangeRectHeld && fOffset <= PlayArea.BEATS_PER_HALFBAR)
                {
                    floatBeatDiff -= fOffset;
                }
                else
                {
                    float diffCorrectUpwards = PlayArea.BEATS_PER_BAR - fOffset;
                    floatBeatDiff += diffCorrectUpwards;
                }

                int rangeRectCorrectForSnap = (int)(floatBeatDiff * (float)PIX_PER_BEAT);

                //Use above to set the range rectangle but correct for actual position below to set the bars
                floatBeatDiff += mMainSelectNoteOffset;

                float fBars = floatBeatDiff / PlayArea.BEATS_PER_BAR;
                int iBars = Convert.ToInt32(fBars);

                //TODO ROUND ERRORS WHEN CONVERT TO BARS?
                if (mbLeftRangeRectHeld)
                {
                    mLeftPointingRectangle.X = mSquareAreaTrackRectangle.X + rangeRectCorrectForSnap - WIDTH_RANGE_RECT;
                    state.mMeledInterf.GetCurrentPlayArea().EndBar = iBars; //TODO END BARS THAT GO OFF SCREEN RHS
                }
                else
                {
                    mRightPointingRectangle.X = mSquareAreaTrackRectangle.X + rangeRectCorrectForSnap;
                    state.mMeledInterf.GetCurrentPlayArea().StartBar = iBars;//TODO END BARS THAT GO OFF SCREEN LHS
                }
                return true;
            }

            return false;
        }

        public bool UpdateTextureChanges(MelodyEditorInterface.MEIState state)
        {
            bool bChanged = false;
            if (state.mbPlayAreaChanged)
            {
                bChanged = true;
                SetRangeRectToPlayArea(state);
//#if ANDROID
//                //seems my lesser Sony H3113 phone suffers a lot and takes a lot of time when updating this texture!
//                if(!(state.mbRecording && state.Playing))
//                    UpdateCustomTexture(state);
//#else
                BlankTexture(); //TOOO Only when change play area
                UpdateCustomTexture(state);
//#endif
                state.mbPlayAreaChanged = false;
            }
            return bChanged;
        }

        public bool Update(MelodyEditorInterface.MEIState state)
        {
            bool bChanged = UpdateTextureChanges( state );

            bool bHeld = false;
            bool bUp = false;
            mbAllTrackAdjust = false;

            if (state.input.SecondHeld)
            {
                UpdateRangeAdjustSecondHeld(state);
                bHeld = true;
                mbAllTrackAdjust = true;
            }
            else if(state.input.Held)
            {
                UpdateRangeAdjustHeld(state);
                bHeld = true;
            }

            if (state.input.RightUp)
            {
                bUp = true;
#if ANDROID
                bHeld = false;
#endif
            }
            else if (state.input.LeftUp)
            {
                if(mbRightPointingHeldLeft || mbLeftPointingHeldRight)
                {
                    mbRightPointingHeldLeft = false;
                    mbLeftPointingHeldRight = false;
                    SnapMainArea(state);
                }
                bUp = true;
            }

            if (bHeld)
            {

                if(mbWaitForMainRectTransitionToUp || mbWaitForSelectRectTransitionToUp)
                {
                    System.Diagnostics.Debug.Assert(mbWaitForMainRectTransitionToUp!= mbWaitForSelectRectTransitionToUp, string.Format("Both these should not be true at same time! mbWaitForSelectRectTransitionToUp{0}  mbWaitForMainRectTransitionToUp{1}", mbWaitForSelectRectTransitionToUp, mbWaitForMainRectTransitionToUp));
                }

                if (!mbWaitForMainRectTransitionToUp && state.input.mRectangle.Intersects(mSelectRectangle))
                {
                    mbWaitForSelectRectTransitionToUp = true;
                    if (Math.Abs(state.input.mDeltaX) > 0 && !state.Playing) //Dont allow moving in x if playing!
                    {
                        int startPos = mSelectRectangle.X;
                        if(mSelectRectangle.Left+ state.input.mDeltaX < mSquareAreaTrackRectangle.Left)
                        {
                            mSelectRectangle.X = mSquareAreaTrackRectangle.X;
                        }
                        else if(mSelectRectangle.Right+ state.input.mDeltaX > mSquareAreaTrackRectangle.Right)
                        {
                            mSelectRectangle.X = mSquareAreaTrackRectangle.Right - mSelectRectangle.Width;
                        }
                        else
                        {
                            mSelectRectangle.X += state.input.mDeltaX;
                        }
                        if (mSelectRectangle.X !=startPos)
                        {
                            int beatPixDiff = mSelectRectangle.X - mSquareAreaTrackRectangle.X;
                            mSelectAreaNoteOffset = BEAT_CHUNKS * beatPixDiff;

                            //state.mDebugPanel.SetFloat(floatBeatDiff, "floatBeatDiff");
                            state.mMeledInterf.SetStartBeatPos(mMainSelectNoteOffset + mSelectAreaNoteOffset);
                        }
                    }
                    if (Math.Abs(state.input.mDeltaY) > 0)
                    {
                        if (mSelectRectangle.Top + state.input.mDeltaY < mSquareAreaTrackRectangle.Top)
                        {
                            mSelectRectangle.Y = mSquareAreaTrackRectangle.Y;
                        }
                        else if (mSelectRectangle.Bottom + state.input.mDeltaY > mSquareAreaTrackRectangle.Bottom)
                        {
                            mSelectRectangle.Y = mSquareAreaTrackRectangle.Bottom - mSelectRectangle.Height;
                        }
                        else
                        {
                            mSelectRectangle.Y += state.input.mDeltaY;
                        }
                    }

                    //snap to correct track
                    int diffY = mSelectRectangle.Y - mSquareAreaTrackRectangle.Y;
                    float floatTrackDiff = TRACK_CHUNKS * diffY;

                    int numTracksStart = diffY / PIX_PER_TRACK;

                    int overLap = diffY % PIX_PER_TRACK;

                    //state.mDebugPanel.SetFloat(floatTrackDiff, "floatTrackDiff");
                    //state.mDebugPanel.SetFloat(diffY, "diffY");
                    //state.mDebugPanel.SetFloat(overLap, "overLap");
                    //state.mDebugPanel.SetFloat(numTracksStart, "numTracksStart");
                    //state.mDebugPanel.SetFloat(-(floatTrackDiff % 1f), "v");

                    state.mMeledInterf.SetTrackOffsetPos(floatTrackDiff);
                    state.mMeledInterf.SetStartDrawLine((int)floatTrackDiff);
                    RefreshSideIndicators();

                    //if (overLap == 0)
                    //{
                    //    mSelectRectangle.Y -= overLap;
                    //    state.mMeledInterf.SetStartDrawLine(numTracksStart);
                    //}
                    //{
                    //    state.mMeledInterf.SetStartDrawLine(numTracksStart);
                    //}
                }
                //MOVE WHOLE AREA LEFT RIGHT WHEN OUTSIDE GREEN SLECET RECTANGLE
                else if(!mbWaitForSelectRectTransitionToUp && state.input.mRectangle.Intersects(mSquareAreaRectangle))
                {
                    mbWaitForMainRectTransitionToUp = true;
                    MoveWholeAreaLeftOrRight(state, state.input.mDeltaX);
                }
            }
            else if(bUp)
            {
                bool bSnapRange = SnapRangeRect(state);

                if (mbWaitForSelectRectTransitionToUp)
                {
                    mbWaitForSelectRectTransitionToUp = false;
                    //Snap to bar in x direction
                    int beatPixDiff = mSelectRectangle.X - mSquareAreaTrackRectangle.X;
                    mSelectAreaNoteOffset = BEAT_CHUNKS * beatPixDiff;
                    float fOffset = mSelectAreaNoteOffset % PlayArea.BEATS_PER_BAR;

                    if (fOffset < PlayArea.BEATS_PER_HALFBAR)
                    {
                        mSelectAreaNoteOffset -= fOffset;
                    }
                    else
                    {
                        float diffCorrectUpwards = PlayArea.BEATS_PER_BAR - fOffset;
                        mSelectAreaNoteOffset += diffCorrectUpwards;
                    }

                    int selectRectCorrectForSnap = (int)(mSelectAreaNoteOffset * (float)PIX_PER_BEAT);

                    mSelectRectangle.X = mSquareAreaTrackRectangle.X + selectRectCorrectForSnap;

                    state.mMeledInterf.SetStartBeatPos(mMainSelectNoteOffset + mSelectAreaNoteOffset);

                    //snap to correct track
                    int diffY = mSelectRectangle.Y - mSquareAreaTrackRectangle.Y;

                    //TODO ARRGH
                    float floatTrackDiff = TRACK_CHUNKS * diffY;
                    state.mMeledInterf.SetTrackOffsetPos(0f); // floatTrackDiff);
                    //state.mDebugPanel.SetFloat(-(floatTrackDiff % 1f), "vup");

                    int numTracksStart = diffY / PIX_PER_TRACK;

                    int overLap = diffY % PIX_PER_TRACK;
                    mSelectRectangle.Y -= overLap;

                    state.mMeledInterf.SetStartDrawLine(numTracksStart);
                }
                else if (mbWaitForMainRectTransitionToUp)
                {
                    SnapMainArea(state);
                }

                if (bSnapRange)
                {
                    state.mMeledInterf.SetStartBeatPos(mMainSelectNoteOffset + mSelectAreaNoteOffset, state.input.RightUp);
                    state.PlayAreaChanged(true);
                    RefreshSideIndicators();
                }
            }

            return bChanged;
        }

        void SnapMainArea(MelodyEditorInterface.MEIState state)
        {
            mbWaitForMainRectTransitionToUp = false;
            float fOffset = mMainSelectNoteOffset % PlayArea.BEATS_PER_BAR;

            if (fOffset < PlayArea.BEATS_PER_HALFBAR)
            {
                mMainSelectNoteOffset -= fOffset;
            }
            else
            {
                float diffCorrectUpwards = PlayArea.BEATS_PER_BAR - fOffset;
                mMainSelectNoteOffset += diffCorrectUpwards;
            }

            state.PlayAreaChanged(true);//TOSO make something happen
            state.mMeledInterf.SetStartBeatPos(mMainSelectNoteOffset + mSelectAreaNoteOffset);
            state.mMeledInterf.UpdateTrackOffsetPos(); //even though we haven't shifted track lines up/down we need this to make sure region offset gets represented n texture and main grid

            state.mDebugPanel.SetFloat(mMainSelectNoteOffset, "mMainSelectNoteOffset");
            RefreshSideIndicators();
        }

        bool MoveWholeAreaLeftOrRight(MelodyEditorInterface.MEIState state, int deltaX)
        {
            bool bMoved = false;
            if (Math.Abs(deltaX) > 0 && !state.Playing) //Dont allow moving in x if playing!
            {
                float startMainPos = mMainSelectNoteOffset;

                float maxRange = PlayArea.MAX_BEATS;
                float beatChange = (float)deltaX / (float)PIX_PER_BEAT;
                mMainSelectNoteOffset += beatChange;
                if ((mMainSelectNoteOffset + BEATS_SHOWN) > maxRange)
                {
                    mMainSelectNoteOffset = maxRange - BEATS_SHOWN;
                }
                if (mMainSelectNoteOffset < 0f)
                {
                    mMainSelectNoteOffset = 0f;
                }

                if (startMainPos != mMainSelectNoteOffset)
                {
                    bMoved = true;
                    state.PlayAreaChanged(true);//TODO make something happen
                }

                state.mMeledInterf.SetStartBeatPos(mMainSelectNoteOffset + mSelectAreaNoteOffset);
                state.mMeledInterf.UpdateTrackOffsetPos(); //even though we haven't shifted track lines up/down we need this to make sure region offset gets represented n texture and main grid
                //state.mDebugPanel.SetFloat(mMainSelectNoteOffset, "mMainSelectNoteOffset");
                RefreshSideIndicators();
            }

            return bMoved;
        }

        //Updates texture to represent notes
        public void UpdateCustomTexture(MelodyEditorInterface.MEIState state)
        {
            MelodyEditorInterface melEdInterf = state.mMeledInterf;

            int startLineIndex = 0;
            int lineNum = 0;
            int numBeats = (int)BEATS_SHOWN; // (int)PlayArea.MAX_BEATS;

            Color lineColor = Color.DarkBlue; //ai region track background colour
            Color colBarGrid = Color.DarkBlue;
            Color colRegion = Color.SaddleBrown;
            float StartBarBeat = state.mMeledInterf.GetCurrentPlayArea().StartBar * PlayArea.BEATS_PER_BAR;
            float EndBarBeat = state.mMeledInterf.GetCurrentPlayArea().EndBar * PlayArea.BEATS_PER_BAR;

            IsQuarterNoteHereDel paramRegion = null;
            IsQuarterNoteHereDel iqnhd = state.mMeledInterf.GetCurrentPlayArea().GetTopLineChordAI().QuarterNoteHere;

            bool bIsTopDrumRegion = state.mMeledInterf.GetCurrentPlayArea().IsDrumArea();

            if(bIsTopDrumRegion)
            {
                colBarGrid = Color.DarkOliveGreen;
                lineColor = Color.DarkOliveGreen;
                colRegion = Color.DarkSalmon;
                iqnhd = state.mMeledInterf.GetCurrentPlayArea().GetTopLineDrumAI().QuarterNoteHere;
            }
            else
            {
                paramRegion = state.mMeledInterf.GetCurrentPlayArea().GetTopLineParamRegion().QuarterNoteHere;
            }

            int beatOffset = (int)mMainSelectNoteOffset;
            int qnOffset = (int)((mMainSelectNoteOffset - (float)beatOffset) % 0.25f); //TODO

            const int MAX_LOOP_RANGE = AIREGION_TRACKS_TOP + PlayArea.FULL_RANGE;

            for (int ait = 0; ait < AIREGION_TRACKS_TOP; ait++)
            {
                Color paintColor = lineColor;

                for (int i = 0; i < BEATS_SHOWN; i++)
                {
                    int currentNotePos = i + beatOffset;
                    int offsetBar = currentNotePos % (int)PlayArea.BEATS_PER_BAR;
                    bool gridLineBeat = offsetBar == 0;
                    for (int qn = 0; qn < PIX_PER_BEAT; qn++)
                    {
                        if (gridLineBeat && qn == 0)
                        {
                            paintColor = colBarGrid;
                        }
                        else
                        {
                            float qnf = (float)currentNotePos + (float)qn * BEAT_CHUNKS;

                            if(paramRegion !=null && paramRegion(qnf))
                            {
                                paintColor = Color.LightSlateGray;
                            }
                            else if (iqnhd(qnf))
                            {
                                paintColor = colRegion;
                            }
                            else
                            {
                                paintColor = lineColor;
                            }
                        }

                        int xpos = (i * PIX_PER_BEAT) + qn + (mWidthTex * lineNum * PIX_PER_TRACK);
                        for (int tr = 0; tr < PIX_PER_TRACK; tr++)
                        {
                            int trOff = mWidthTex * tr;
                            int paintIndex = trOff + xpos;
                            mTextureColors[paintIndex] = paintColor;
                        }
                    }
                }

                lineNum++;
                startLineIndex = mWidthTex * lineNum * PIX_PER_TRACK;
            }

            foreach (var noteLine in melEdInterf.GetRenderedNoteLines())
            {
                lineColor = noteLine.mbSharp ? mDarkLine : Color.Beige;
                Color paintColor = lineColor;

                if(lineNum >= MAX_LOOP_RANGE)
                {
                    //ERROR!
                    var texEscape = new Texture2D(state.mSB.GraphicsDevice, mWidthTex, mHeightTex, false, SurfaceFormat.Color);
                    texEscape.SetData(mTextureColors);
                    mCustomTexture = texEscape;
                    return;
                }

                for (int i = 0; i < BEATS_SHOWN; i++)
                {
                    int currentNotePos = i + beatOffset;

                    int offsetBar = currentNotePos % (int)PlayArea.BEATS_PER_BAR;
                    bool gridLineBeat = offsetBar == 0;
 
                    for (int qn = 0; qn < PIX_PER_BEAT; qn++)
                    {
                        if(gridLineBeat && qn==0)
                        {
                            paintColor = colBarGrid;
                        }
                        else
                        {
                            float qnf = (float)currentNotePos + (float)qn * BEAT_CHUNKS;
                            if (noteLine.QuarterAIRegionNoteHere(qnf))
                            {
                                paintColor = new Color(0, 100, 100); ;
                            }
                            else if (noteLine.IsNoteHere(qnf)!=null)
                            {
                                paintColor = Color.Red;
                            }
                            else
                            {
                                if(qnf> StartBarBeat && qnf<EndBarBeat)
                                {
                                    paintColor = lineColor;
                                }
                                else
                                {
                                    paintColor = noteLine.mbSharp ? mDarkerLine : mDarkerBeige;
                                }
                            }
                        }

                        int xpos = (i * PIX_PER_BEAT) + qn + (mWidthTex * lineNum* PIX_PER_TRACK);

                        for (int tr = 0; tr < PIX_PER_TRACK; tr++)
                        {
                            int trOff = mWidthTex * tr;
                            int paintIndex = trOff + xpos;
                            mTextureColors[paintIndex] = paintColor;
                        }
                    }
                }

                lineNum++;
                startLineIndex = mWidthTex * lineNum * PIX_PER_TRACK;
            }

            //var tex = new Texture2D(state.mSB.GraphicsDevice, mWidthTex, mHeightTex, false, SurfaceFormat.Color);
            tex.SetData(mTextureColors);
            mCustomTexture = tex;
        }

        protected int GetArrayIndex(int x, int y, int width)
        {
            if (x >= width) x = width - 1;
            return (y * width) + x;
        }

        Vector2 mScreenStart;
        Vector2 mScreenEnd;

        public void SetPlayHead(float pos)
        {
            float offset = pos - mMainSelectNoteOffset;
            float ratioAcross = offset / BEATS_SHOWN;

            mScreenStart.X = mSquareAreaRectangle.X + ratioAcross*mSquareAreaRectangle.Width;
            mScreenEnd.X =  mScreenStart.X;

        }

        public void Draw(SpriteBatch sb)
        {
            sb.Begin();
            // public const int SC_TEXT_XPOS = 1305;
            //public const int SC_TEXT_MAIN_YPOS = 434;
            //public const int SC_TEXT_RANGE_YPOS = 464;
            Vector2 mainTextPos = new Vector2(SC_TEXT_XPOS, SC_TEXT_MAIN_YPOS);
            Vector2 rangeTextPos = new Vector2(SC_TEXT_XPOS, SC_TEXT_RANGE_YPOS);
            int mainPosInt = (int)(mMainSelectNoteOffset);
            int rangePosInt = (int)(mSelectAreaNoteOffset)+ mainPosInt;
            sb.DrawString(mFont, mainPosInt.ToString(), mainTextPos, Color.Black);
            sb.DrawString(mFont, rangePosInt.ToString(), rangeTextPos, Color.DarkGreen);
            sb.End();

            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp); //not heling on Sony android phone, some lines look darker

            sb.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            sb.Draw(mCustomTexture,
            /// set the filter to Point
            mSquareAreaRectangle,
            Color.White);

            if (mbDrawLeftStartBarIndicator)
            {
                sb.FillRectangle(mLeftStartBarIndicator, PlayArea.mSideBarColor);
            }
            if (mbDrawRightEndBarIndicator)
            {
                sb.FillRectangle(mRightEndBarIndicator, PlayArea.mSideBarColor);
            }

            sb.DrawRectangle(mSelectRectangle, Color.Green, 3f);

            Color adjustSquareColor = Color.LightSeaGreen;
            if (mbAllTrackAdjust)
            {
                adjustSquareColor = Color.LightPink;  
            }

            sb.DrawLine(mScreenStart, mScreenEnd, Color.Black, 1);

            sb.Draw(mRightTexture,
            mRightPointingRectangle,
            adjustSquareColor);

            sb.Draw(mLeftTexture,
            mLeftPointingRectangle,
            adjustSquareColor);

            sb.End();

        }

    }
}
