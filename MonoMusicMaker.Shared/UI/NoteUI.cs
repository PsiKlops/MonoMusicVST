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
using System.Threading;
using System.Threading.Tasks;

namespace MonoMusicMaker
{
    public class NoteUI
    {
        const float BEAT_TOUCH_RANGE_HELD = 0.2f; //if touch moves 
        const int VELOCITY_ADJUST_WAIT_TIME = 600; //time moved within note
        const int SLIDE_NOTE_WAIT_TIME = 200; //time moved within note
        const int HOLD_TO_PLACE_TIME = 100;
        const int HOLD_TO_DELETE_TIME = 100;
        Rectangle mCrossLeftRightRect;

        List<Rectangle> mChordCrossLinesRects;
        Rectangle mCrossUpDownRect;
        Color mmCurrentColor = Color.Red;
        float mAlpha = 1f; //fade up when held
        public bool Active { set; get; } = false;
        public Note mNoteOver = null; //note the touch is directly over
        public Note mNoteObstruct = null; //any note that is obstructing placing down - even if not directly under touch

        //Preview note
        public MusicPreviewer.NotesToPreview mNotestoPreview = new MusicPreviewer.NotesToPreview();
        public int mPreviewNote = -1;
        int mPreviewChannel = -1;
        MelodyEditorInterface.MEIState mState = null; //just for preview note really

        NoteLine mLastLineTouched = null;

        public MusicPreviewer mMusicPreviewer;

        NoteLine mLineNoteStartedOn = null; //When in key range or instant lay down mode this will help prevent moving to another line and started laying down notes
        public Note mCurrentLaidNote = null; //When in key range or instant lay down mode this records the note started on the line and will be th eonly note extend when moving held poistion on th eline
        public bool mbRemovingNoteMode = false; //When in key range or instant lay down mode, if a note is removed then set this bool to keep in removing mode until click up

        public bool mbSecondHeldSeen = false;
        bool mbActionCommitted = false;

        public float mfRevertBeatPos = -1f; //If an exiting note gets touch and extended then we record its pos/length so it can be reverted if a second touch happens during the edit cycle this NoteUI object exists for
        public float mfRevertBeatLength = -1f;

        float mLastBeatPos = 0f;
        public Note mDownNote = null;
        public Note mDoubleTapNote = null;
        public bool mDoubleTapNoteVelSet = false;
        public float mNoteBeatLengthStart = 1f;
        public bool mbOnDoubleTapNote = false;
        public bool mbDoubleTapNotePendingSnap = false;
        public bool mbHeldInSamePlace = false;
        int mTimeInPlace = 0; //This is constantly updated to latest time until it is held

        int mStartNoteAdjustTime = 0; //This is the time movement is seen over a note, can be up and down, left right so long as the same note is under - hence 'scratch' - new patent touch type ;)
        public bool mAdjustHeldNoteMode = false; //if scratch long enough then latch to this mode
        bool mbNoteEditInVelocityMode = false; //This will parallel th esame name value in state - so we can know if velocity adjusting or moving a note about in a local way - for drawing only maybe?
        Rectangle mRectAroundNoteSelectedForAdjust; //rectangle draw around held note so you can see under finger 
        Rectangle mRectNoteVelocityAdjust; //rectangle draw around held note so you can see under finger 
        public Note mNoteSelectedForAdjust = null; // note the scratch select was done on 

        const float TOUCH_MOVEMENT_DIFF = 0f; //5f; //pixel diff that is seen as a different position
        const float TOUCH_MOVEMENT_DIFF_SQ = TOUCH_MOVEMENT_DIFF* TOUCH_MOVEMENT_DIFF; //pixel diff that is seen as a different position
        const int MAX_TOUCH_POINT_LIST = 20;
        Vector2[] mListTouchPoints = new Vector2[MAX_TOUCH_POINT_LIST];
        int mNumTouchPoints = 0;
        bool mbListFullOfPoints = false;
        const int SELECT_VOL_RANGE = 7;
        const int HALF_VOL_RANGE = SELECT_VOL_RANGE / 2;
        const int MID_VOL_RANGE = HALF_VOL_RANGE + 1;
        int mVelocityValueSelected = MID_VOL_RANGE; // middle value at middle
        int mStartYPointNoteAdjust;
        int mStartXPointNoteAdjust;

        public NoteUI()
        {
            int noteWidth = (int)(1 * MelodyEditorInterface.BEAT_PIXEL_WIDTH);
            int playAreaHeight = PlayArea.NUM_DRAW_LINES* NoteLine.TRACK_HEIGHT;
            int mStartPLayAreaY = MelodyEditorInterface.TRACK_START_Y;

            int crossYPos = 0;
            int crossXPos = 0;
            mCrossLeftRightRect = new Rectangle(MelodyEditorInterface.TRACK_START_X, crossYPos, NoteLine.mTrackWidth, NoteLine.TRACK_HEIGHT);

            mChordCrossLinesRects = new List<Rectangle>();

            mCrossUpDownRect = new Rectangle(crossXPos, mStartPLayAreaY, noteWidth, playAreaHeight);
        }

        public void ResetAll()
        {
            //Should stop all notes before this
            if (mDoubleTapNote != null)
            {
                mDoubleTapNote.SetDefaultCol();
            }
            mDoubleTapNote = null; //TODO check being left on
            mNoteOver = null; //note the touch is directly over
            mNoteObstruct = null; //any note that is obstructing placing down - even if not directly under touch
            Reset(null);
         }

        public bool IsPosOutSIdeCurrentNote(float pos)
        {
            if(mCurrentLaidNote!=null)
            {
                if(pos < mCurrentLaidNote.BeatPos || pos >= (mCurrentLaidNote.BeatPos + mCurrentLaidNote.BeatLength))
                {
                    return true;
                }
            }
            return false;
        }

        public void Reset(MelodyEditorInterface.MEIState state)
        {
            if(state!=null)
            {
                state.mNCG = null; //no more chord stuff now!
            }

            mfRevertBeatPos = -1;
            mfRevertBeatLength = -1;

            mbSecondHeldSeen = false;
            mbRemovingNoteMode = false;
            mCurrentLaidNote = null;
            mLastLineTouched = null;
            mLineNoteStartedOn = null;
            mLastBeatPos = 0f;
            mbHeldInSamePlace = false;
            mNoteOver = null; //note 
            mNoteObstruct = null; //a
            mTimeInPlace = int.MaxValue;
            mStartNoteAdjustTime = int.MaxValue;
            mAdjustHeldNoteMode = false;
            mListTouchPoints = new Vector2[MAX_TOUCH_POINT_LIST];
            mNumTouchPoints = 0;
            mbListFullOfPoints = false;
            mbActionCommitted = false;
            mNoteSelectedForAdjust = null;
            mVelocityValueSelected = MID_VOL_RANGE; // middle value at start
        }

        public void CopyPreviewToTracks()
        {
            if(mMusicPreviewer!=null)
            {
                mMusicPreviewer.CopyPreviewToTracks();
            }
        }
        public void SetRecordModeOn(bool bOn)
        {
            if (mMusicPreviewer != null)
            {
                mMusicPreviewer.SetRecordModeOn(bOn);
            }
        }
        public void previewNoteOff()
        {
            if (mPreviewNote != -1 && mState !=null)
            {
                mState.NoteOff(mPreviewChannel, mPreviewNote);
                mPreviewNote = -1;
                mPreviewChannel = -1;
            }
        }

        public void SetNoteOver(Note n, MelodyEditorInterface.MEIState state)
        {
            if(mNoteOver!=n)
            {
                mNoteOver = n;
            }
        }

        public void SetCurrentLaidNote(Note note, MelodyEditorInterface.MEIState state)
        {
            //System.Diagnostics.Debug.Assert(note !=null, string.Format("Passing null note!"));
            System.Diagnostics.Debug.Assert(mCurrentLaidNote == null, string.Format("mCurrentLaidNote should be null!"));
            System.Diagnostics.Debug.Assert(state.mNCG == null, string.Format("Note Chord Group should be null!"));

            if(note!=null)
            {
                state.mNCG = new NoteChordGroup(note);
            }
            mCurrentLaidNote = note;
        }

        /////////////////////////////////////////////////////////////////////
        //// SLIDE ADJUST NOTE
        void UpdateNoteSlideAdjust(MelodyEditorInterface.MEIState state)
        {
            System.Diagnostics.Debug.Assert(mNoteSelectedForAdjust != null, string.Format("Anchor note invalid!"));

            if (state.input.mRectangle.Y < mNoteSelectedForAdjust.mRect.Y)
            {
                mState.mMeledInterf.mEditManager.MoveDirUp(mState);
                Note movedNote = mState.mMeledInterf.mEditManager.GetAnchorNote();
                SetSelectedNoteAndDisplayRect(movedNote);
                System.Diagnostics.Debug.Assert(movedNote != null, string.Format("Failed to move up anchor note!"));
            }
            else if (state.input.mRectangle.Y > (mNoteSelectedForAdjust.mRect.Y + mNoteSelectedForAdjust.mRect.Height))
            {
                mState.mMeledInterf.mEditManager.MoveDirDown(mState);
                Note movedNote = mState.mMeledInterf.mEditManager.GetAnchorNote();
                SetSelectedNoteAndDisplayRect(movedNote);
                System.Diagnostics.Debug.Assert(movedNote != null, string.Format("Failed to move down anchor note!"));
            }

            int XpointDiff = state.input.mRectangle.X - mStartXPointNoteAdjust;

            if(mState.mMeledInterf.mEditManager.MoveLeftOrRightOnXdiff(state, XpointDiff))
            {
                mStartXPointNoteAdjust = state.input.mRectangle.X;
                SetSelectedNoteAndDisplayRect(mNoteSelectedForAdjust);
            }
        }

        /////////////////////////////////////////////////////////////////////
        //// VELOCITY ADJUST NOTE
        void UpdateVelocityAdjust(int YpointDiff)
        {
            int stepVolPixels = NoteLine.TRACK_HEIGHT / HALF_VOL_RANGE;

            mVelocityValueSelected = 1 + YpointDiff / stepVolPixels;

            if (mVelocityValueSelected > SELECT_VOL_RANGE)
            {
                mVelocityValueSelected = SELECT_VOL_RANGE;
            }
            if (mVelocityValueSelected < 0)
            {
                mVelocityValueSelected = 0;
            }
            mRectNoteVelocityAdjust.Y = mRectAroundNoteSelectedForAdjust.Y + mVelocityValueSelected * stepVolPixels;
        }

        bool UpdateHeldAdjustNote(MelodyEditorInterface.MEIState state)
        {
            if(mbActionCommitted)
            {
                //already committed to delete or add note so dont bother with this
                return false;
            }
            mbNoteEditInVelocityMode = state.mbNoteEditInVelocityMode;

            if (mAdjustHeldNoteMode)
            {
                int YpointDiff = state.input.mRectangle.Y - mStartYPointNoteAdjust;

                if (mbNoteEditInVelocityMode)
                {
                    UpdateVelocityAdjust(YpointDiff);
                }
                else
                {
                    UpdateNoteSlideAdjust(state);
                }
                return false;
            }

            //Add the point in to current position
            mListTouchPoints[mNumTouchPoints] = new Vector2(state.input.mRectangle.X, state.input.mRectangle.Y);

            mNumTouchPoints++;

            if(mNumTouchPoints>=MAX_TOUCH_POINT_LIST)
            {
                mbListFullOfPoints = true;
            }
            mNumTouchPoints %= MAX_TOUCH_POINT_LIST; //loop round if needed

            int numPointsToCalc = mbListFullOfPoints ? MAX_TOUCH_POINT_LIST : mNumTouchPoints;

            float totalX = 0f, totalY = 0f;
            float maxX = 0f, maxY = 0f;
            float minX = 10000f, minY = 10000f;

            for (int i=0;i< numPointsToCalc;i++)
            {
                Vector2 p = mListTouchPoints[i];
                totalX += p.X;
                totalY += p.Y;

                if(p.X > maxX)
                {
                    maxX = p.X;
                }
                if(p.X < minX)
                {
                    minX = p.X;
                }

                if (p.Y > maxY)
                {
                    maxY = p.Y;
                }
                if (p.Y < minY)
                {
                    minY = p.Y;
                }
            }

            int centerX = (int)(totalX / numPointsToCalc);
            int centerY = (int)(totalY / numPointsToCalc);

            if (mNoteSelectedForAdjust==null && mNoteOver != null)
            {
                if(mNoteOver.mRect.Contains(new Point(centerX,centerY)))
                {
                    SetSelectedNoteAndDisplayRect(mNoteOver);
                    mStartNoteAdjustTime = state.mCurrentGameTimeMs; //reset to wait
                }
            }

            if (mNoteSelectedForAdjust != null)
            {
                if (!mRectAroundNoteSelectedForAdjust.Contains(new Point(centerX, centerY)))
                {
                    mStartNoteAdjustTime = state.mCurrentGameTimeMs; //reset to wait
                    mNoteSelectedForAdjust = null;
                }
            }

            if (mNoteSelectedForAdjust!=null)
            {
                Vector2 vMax = new Vector2(maxX, maxY);
                Vector2 vMin = new Vector2(minX, minY);

                Vector2 vDiff = vMax - vMin;
                float fDiff = vDiff.LengthSquared();

                if (fDiff >= TOUCH_MOVEMENT_DIFF_SQ)
                {
                    mTimeInPlace = state.mCurrentGameTimeMs; //moving about still so stop timer going down
                }

                int waitTime = SLIDE_NOTE_WAIT_TIME;

                if(mbNoteEditInVelocityMode)
                {
                    waitTime = VELOCITY_ADJUST_WAIT_TIME;
                }

                if ((state.mCurrentGameTimeMs - mStartNoteAdjustTime) > waitTime)
                {
                    mStartYPointNoteAdjust = state.input.mRectangle.Y;
                    mStartXPointNoteAdjust = state.input.mRectangle.X;

                    mAlpha = 0.5f; //We will be only updating the velocity mode so set the cross visible instead of waiting for fade up

                    if(!mbNoteEditInVelocityMode)
                    {
                        bool addedOK = state.mMeledInterf.mEditManager.AddAnchordNote(mNoteSelectedForAdjust, state.EditMode);
                        System.Diagnostics.Debug.Assert(addedOK, string.Format("Failed to add anchor note! {0}", mNoteSelectedForAdjust));
                    }

                    mAdjustHeldNoteMode = true;
                    return true;
                }
            }

            return false;
        }

        public void EndHeldNoteAdjust()
        {
            mAdjustHeldNoteMode = false;

            if(mNoteSelectedForAdjust==null)
            {
                System.Diagnostics.Debug.Assert(false, string.Format("mNoteScratchSelectedForAdjust is not null!"));
                return;
            }

            if(mbNoteEditInVelocityMode)
            {
                int velocity = MidiBase.LOW_VELOCITY;
                switch (mVelocityValueSelected)
                {
                    case 7:
                        velocity = MidiBase.VELOCITY_3;
                        break;
                    case 6:
                        velocity = MidiBase.VELOCITY_4;
                        break;
                    case 5:
                        velocity = MidiBase.VELOCITY_5;
                        break;
                    case 4:
                        velocity = MidiBase.VELOCITY_6;
                        break;
                    case 3:
                        velocity = MidiBase.VELOCITY_7;
                        break;
                    case 2:
                        velocity = MidiBase.VELOCITY_8;
                        break;
                    case 1:
                        velocity = MidiBase.VELOCITY_9;
                        break;
                    case 0:
                        velocity = MidiBase.VELOCITY_10;
                        break;
                }

                mNoteSelectedForAdjust.Velocity = velocity;
            }
            else
            {
                if(!mState.EditMode) //if in edit mode we dont remove the hilighter selected not and leave it to be selected for what ever other edit stuff goes on
                {
                    mState.mMeledInterf.mEditManager.RemoveSelectedNote(mNoteSelectedForAdjust);
                }
            }

            mNoteSelectedForAdjust.RefreshRectToData();
        }

        void SetSelectedNoteAndDisplayRect(Note n)
        {
            mNoteSelectedForAdjust = n;
            float scale = 7f;
            float scaleVol = 5f;
            int w = (int)(n.mRect.Width * scale);
            int h = (int)(n.mRect.Height * scale);

            int hvol = (int)(n.mRect.Height * scaleVol);
            int yOffsetVol = (int)((float)hvol / 2f - (float)n.mRect.Height / 2f);
            int yvol = n.mRect.Y - yOffsetVol;


            int xOffset = (int)((float)w / 2f - (float)n.mRect.Width / 2f);
            int yOffset = (int)((float)h / 2f - (float)n.mRect.Height / 2f);
            int x = n.mRect.X - xOffset;
            int y = n.mRect.Y - yOffset;

            mRectAroundNoteSelectedForAdjust = new Rectangle(x, y, w, h);
            mRectNoteVelocityAdjust = new Rectangle(x, yvol, w, hvol);
        }
        static float HACK_DEBUG_FLOAT = 0.0f;


        void UpdateCrossLinesForPlaying(MelodyEditorInterface.MEIState state)
        {
            int numLines = mMusicPreviewer.mPreviewLines.Count;

            mChordCrossLinesRects = new List<Rectangle>();

            PlayArea cpa = state.mMeledInterf.GetCurrentPlayArea();

            for (int i=0;i<numLines;i++)
            {
                if(mMusicPreviewer.mPreviewLines[i].mbNoteInPlay)
                {
                    NoteLine nl = cpa.GetNoteLineAtNoteNumber(mMusicPreviewer.mPreviewLines[i].NoteNum);
                    if(nl!=null)
                    {
                        Rectangle rect = new Rectangle(MelodyEditorInterface.TRACK_START_X, nl.mStartY, NoteLine.mTrackWidth, NoteLine.TRACK_HEIGHT);
                        mChordCrossLinesRects.Add(rect);
                    }
                }
            }
        }

        public bool Update(NoteLine lineTouched, float beatPos, MelodyEditorInterface.MEIState state)
        {
            if(mAdjustHeldNoteMode)
            {
                UpdateHeldAdjustNote(state);
                return false;
            }

            mCrossLeftRightRect.Y = lineTouched.mStartY;

            if(mMusicPreviewer != null && mMusicPreviewer.Playing())
            {
                UpdateCrossLinesForPlaying(state);
            }

            int holdToPlaceTime = HOLD_TO_DELETE_TIME;
            bool bQuantizedPlacement = false;
            bool bAllowPlacement = false;
            if (state.mbPlacePlayHeadWithRightTouch)
            {
                bQuantizedPlacement = true;
                bAllowPlacement = true;
                if (mNoteSelectedForAdjust == null)
                {
                    holdToPlaceTime = 0;
                }
            }
            else
            {
                //Only allow note velocity adjust when not in place remove and play head adjust mode
                if (UpdateHeldAdjustNote(state))
                {
                    return false;
                }
            }

            bool setCurrentTime = false;
            //Preview note when touched
            if (mLastLineTouched!=lineTouched)
            {
                previewNoteOff();
                mState = state;
                mPreviewNote = lineTouched.NoteNum;
                mPreviewChannel = lineTouched.GetChannel();

                mNotestoPreview.SetNotes(state, lineTouched);

                mNotestoPreview.mSelectedRiff = state.mMeledInterf.mPresetManager.GetSelected();
                mNotestoPreview.mXTouch = state.input.mRectangle.X;
                mNotestoPreview.mBeatPosTouch = beatPos;

                if (mDoubleTapNote==null)
                {
                    PlayNoteAsync();
                }

                setCurrentTime = true;
                mLastLineTouched = lineTouched;
            }

            if (mNoteOver!=null)
            {
                mCrossUpDownRect.X = mNoteOver.mRect.X;
            }
            else
            {
                mCrossUpDownRect.X = MelodyEditorInterface.TRACK_START_X + (int)(MelodyEditorInterface.BEAT_PIXEL_WIDTH * beatPos);
            }

            if(!bQuantizedPlacement && Math.Abs(beatPos- mLastBeatPos) > BEAT_TOUCH_RANGE_HELD)
            {
                //state.mDebugPanel.SetFloat(HACK_DEBUG_FLOAT, "RESET TOUCH");
                HACK_DEBUG_FLOAT += 0.01f;

                mLastBeatPos = beatPos;
                setCurrentTime = true;
            }

            if (setCurrentTime)
            {
                mTimeInPlace = state.mCurrentGameTimeMs;
            }
            else
            {
                if(!mbHeldInSamePlace)
                {
                    if ((state.mCurrentGameTimeMs - mTimeInPlace) > holdToPlaceTime)
                    {
                        if(mLineNoteStartedOn ==null)
                        {
                            mLineNoteStartedOn = lineTouched;
                        }

                        if (bQuantizedPlacement)
                        {
                            if (mLineNoteStartedOn != lineTouched)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            mbHeldInSamePlace = true; //only do this one shot when not in quantized/key range lay down continuous notes mode
                        }

                        mbActionCommitted = true;
                        return bAllowPlacement;
                    }
                    mAlpha = (float)(state.mCurrentGameTimeMs - mTimeInPlace) / holdToPlaceTime;
                }
            }

            return false;
        }

        public void Draw(SpriteBatch sb)
        {
            if(Active)
            {
                Color drawCol = Color.LightSeaGreen;

                if (mNoteOver !=null)
                {
                    if(mbOnDoubleTapNote)
                    {
                        drawCol = Color.Green;
                    }
                    else
                    {
                        drawCol = Color.Pink;
                    }
                }
                else if (mNoteObstruct != null)
                {
                    drawCol = Color.Yellow;
                }

                float fAlpha = 0.15f + (0.4f * mAlpha);
                //sb.Draw(Button.mStaticDefaultTexture,
                //mCrossLeftRightRect,
                //drawCol * fAlpha);

                foreach(Rectangle chordRect in mChordCrossLinesRects)
                {
                    sb.Draw(Button.mStaticDefaultTexture,
                    chordRect,
                    drawCol * fAlpha);
                }

                sb.Draw(Button.mStaticDefaultTexture,
                mCrossUpDownRect,
                drawCol * fAlpha);

                if(mAdjustHeldNoteMode)
                {
                    Color largeRectCol = mbNoteEditInVelocityMode ? Color.DarkBlue : Color.DarkGoldenrod;
                    Color smallRectCol = mbNoteEditInVelocityMode ? Color.Aquamarine : Color.Yellow;

                    sb.Draw(Button.mStaticDefaultTexture,
                    mRectAroundNoteSelectedForAdjust,
                    largeRectCol * 0.6f);

                    sb.DrawRectangle(mRectNoteVelocityAdjust, smallRectCol, 8); //control button highlight
                }
            }
        }

        public void EndPreview()
        {
            if (mMusicPreviewer != null && mMusicPreviewer.Playing())
            {
                mMusicPreviewer.QuitNow();
            }
        }

        public  async Task PlayNoteAsync()
        {
            if(mState!=null)
            {
                bool bPlaying = false;
                if(mMusicPreviewer==null)
                {
                    mMusicPreviewer = new MusicPreviewer(mState);
                    mMusicPreviewer.InitNotes(mNotestoPreview);
                }
                else
                {
                    bPlaying = mMusicPreviewer.Playing();
                    mMusicPreviewer.InitNotes(mNotestoPreview);
                }
                if(!bPlaying)
                {
                    await mMusicPreviewer.Update();
                }
            }
        }

    }
}
