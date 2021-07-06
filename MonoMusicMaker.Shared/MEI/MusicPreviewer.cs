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

using System.Diagnostics;

namespace MonoMusicMaker
{
    //This will be expected to run on its own thread and play whatever notes/chords the previewer is set up with
    public class MusicPreviewer
    {
        const int DELAY_ELAPSE_FUDGE_FACTOR = 8;

        public class NotesToPreview
        {

            public bool mbTestRiff = false;
            public int mSelectedRiff = 0;
            public int mXTouch = 0;

            public float mBeatPosTouch = 0f;
            public NotesToPreview()
            {
                mChordLower = -1;
                mChordUpper = -1;
            }

            public void SetNotes(MelodyEditorInterface.MEIState state, NoteLine nl )
            {
                mRootNote = nl.NoteNum;
                mChordLower =-1;
                mChordUpper =-1;

                if (nl.m_MyLLN != null)
                {
                    //n.mParent.m_MyLLN.Previous.Value
                    if (nl.m_MyLLN.Previous != null)
                    {
                        mChordLower = nl.m_MyLLN.Previous.Value.NoteNum;
                    }

                    if (nl.m_MyLLN.Next != null)
                    {
                        mChordUpper = nl.m_MyLLN.Next.Value.NoteNum;
                    }
                }
            }
            public int mRootNote;
            public int mChordLower;
            public int mChordUpper;
        }

        const float BEAT_STEP = 0.0625f;
        float mStartBeatOfRiff = 0f;
        float mEndBeatOfRiff = 0f;
        float mStartTouchOffset = 0f;
        bool mRecordMode = false;
        bool mBasicMode = false; //if true will only play basic one note for preview and not draw on screen. Any existing note buffer will be locked

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// HUD 
        /// HUD 
        // Hud display - some thing showing movement and time left for playing - cirlce with clock finger I think should do
        float mHudClockRadius = 40f;
        float mHudClockCentreX = 0f;
        float mHudClockCentreY = 0f;
        Rectangle mHudBackRect;


        //2 time bar Fill rectangles, one for progress through the riff, and one to show how lone before we run out of bar area.
        //probably use instand of clock face
        const int FILL_RECT_BACK_HEIGHT = 96;
        const int FILL_RECT_GAP = 4;
        const int FILL_RECT_WIDTH = 200;
        const int FILL_RECT_HEIGHT = (FILL_RECT_BACK_HEIGHT - FILL_RECT_GAP)/2;

        const int displayX = MainMusicGame.SECOND_TICK_BUTTON_XPOS;

        Rectangle mHudBackFillRectangle = new Rectangle(displayX, MainMusicGame.BOTTOM_BUTTON_YPOS, FILL_RECT_WIDTH, FILL_RECT_BACK_HEIGHT);

        Rectangle mHudMarkerRectangle1 = new Rectangle(displayX, MainMusicGame.BOTTOM_BUTTON_YPOS, FILL_RECT_WIDTH/4, FILL_RECT_BACK_HEIGHT);
        Rectangle mHudMarkerRectangle2 = new Rectangle(displayX, MainMusicGame.BOTTOM_BUTTON_YPOS, FILL_RECT_WIDTH/2, FILL_RECT_BACK_HEIGHT);
        Rectangle mHudMarkerRectangle3 = new Rectangle(displayX, MainMusicGame.BOTTOM_BUTTON_YPOS, FILL_RECT_WIDTH/4+ FILL_RECT_WIDTH / 2, FILL_RECT_BACK_HEIGHT);

        Rectangle mHudTimeLeftFillRectangle = new Rectangle(displayX, MainMusicGame.BOTTOM_BUTTON_YPOS, 0, FILL_RECT_HEIGHT);
        Rectangle mHudRiffProgressFillRectangle = new Rectangle(displayX, MainMusicGame.BOTTOM_BUTTON_YPOS + FILL_RECT_HEIGHT + FILL_RECT_GAP, 0, FILL_RECT_HEIGHT);


        [Flags]
        public enum PreviewLineType : short
        {
            None = 0,
            Preview = 1,
            Record = 2,
        };

        public struct NoteLinePair
        {
            public NoteLine mRecordLine;
            public NoteLine mSourceLine;

            public void Draw(SpriteBatch sb, MusicPreviewer.PreviewLineType previewType = MusicPreviewer.PreviewLineType.None)
            {
                mRecordLine.Draw(sb, previewType);
            }
            public void CopyRecordToSource()
            {
                foreach(Note n in mRecordLine.GetNotes())
                {
                    float pos = n.BeatPos;
                    float len = n.BeatLength;

                    mSourceLine.AddNoteIfSpace(pos, len);
                }
            }
         }

        //public List<NoteLine> mPrevLines = new List<NoteLine>();
        public List<NoteLine> mPreviewLines = null; // new List<NoteLine>();
        public List<NoteLinePair> mRecordLines = new List<NoteLinePair>();
        List<NoteLinePair> mBackupRecordLines = new List<NoteLinePair>();

        MelodyEditorInterface.MEIState mPreviewState = new MelodyEditorInterface.MEIState();
        int mRootNote;
        bool mRunning = false; //Running when updating and counting time
        bool mPlaying = false; //Playing when allowed to lay a riff if mouse held
        float mUpdateBeatOfRiff = 0f;
        int mXTouch = 0;
        float mBeatStartPosTouch = 0f;
        float mBeatsToEndOfMax = 0f;
        float mRatioThroughRiff = 0f;
        float mRatioToEndOfBeats = 0.7f;
        float mBeatLengthOfRiff = 0f;

        int mRootNoteLineIndex = 0;
        PlayArea mPa = null;

        public MusicPreviewer(MelodyEditorInterface.MEIState state)
        {
            //Copy the minimum needed to get a state working for playing the preview notes in a seperate thread
            mPreviewState.mMeledInterf = state.mMeledInterf;
            mPreviewState.mStateMidiMgr = state.mStateMidiMgr;
            mPreviewState.gameTime = state.gameTime;
            mPreviewState.mbRecording = state.mbRecording; //records if recording when clicked - doesnt need to be updated in real time because of the transitory nature if this preview
            mPreviewState.PreviewPlayerState = true;

            System.Diagnostics.Debug.WriteLine(string.Format(" MUSIC PREVIEWER!!! {0} ", mPreviewState.gameTime));
        }


        public void Reset()
        {

        }

        public void CopyPreviewToTracks()
        {
            if(mBasicMode)
            {
                return;
            }

            foreach(NoteLinePair nlp in mRecordLines)
            {
                nlp.CopyRecordToSource();
            }

            mRecordLines = new List<NoteLinePair>();
        }

        //TODO CLEAR MANAGE WHEN LOAD/SAVE, CHANGE TRACK ETC
        public void BackuUpRecordBuffer()
        {
            mBackupRecordLines = mRecordLines;
            mRecordLines = new List<NoteLinePair>();
        }
        public void RestoreRecordBuffer()
        {
            mRecordLines = mBackupRecordLines;
        }

        public void SetRecordModeOn(bool bOn)
        {
            mRecordMode = bOn;
        }
        public void SetBasicMode(bool bOn)
        {
            if(bOn)
            {
                BackuUpRecordBuffer();
            }
            else
            {
                RestoreRecordBuffer();
            }

            mBasicMode = bOn;
        }

        public void Draw(SpriteBatch sb, MusicPreviewer.PreviewLineType previewType = MusicPreviewer.PreviewLineType.None)
        {
            if(mBasicMode)
            {
                return;
            }
            if (mRecordLines != null)
            {
                foreach (MusicPreviewer.NoteLinePair mpnl in mRecordLines)
                {
                    mpnl.Draw(sb, MusicPreviewer.PreviewLineType.Record);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////
        /// MAIN HUD DRAW
        /////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////
        public void MainHudDraw(SpriteBatch sb)
        {
            //Old clock meter
            //sb.FillRectangle(mHudBackRect, Color.SkyBlue);
            //sb.DrawCircle(mHudClockCentreX, mHudClockCentreY, mHudClockRadius, 20, Color.DarkBlue, 6f);
            //float angle = mRatioThroughRiff * 2f * (float)Math.PI - (float)Math.PI * .5f;
            //sb.DrawLine( new Vector2(mHudClockCentreX, mHudClockCentreY), mHudClockRadius, angle, Color.DarkBlue, 6f);

            mHudRiffProgressFillRectangle.Width = (int)(mRatioThroughRiff * (float)mHudBackFillRectangle.Width);
            sb.FillRectangle(mHudBackFillRectangle, Color.PaleTurquoise);
            sb.FillRectangle(mHudRiffProgressFillRectangle, Color.Aquamarine); 
            sb.DrawRectangle(mHudRiffProgressFillRectangle, Color.Black);

            mHudTimeLeftFillRectangle.Width = (int)(mRatioToEndOfBeats * (float)mHudBackFillRectangle.Width);
            sb.FillRectangle(mHudTimeLeftFillRectangle, Color.Aqua); 
            sb.DrawRectangle(mHudTimeLeftFillRectangle, Color.Black);

            sb.DrawRectangle(mHudMarkerRectangle1, Color.Black, 2f);
            sb.DrawRectangle(mHudMarkerRectangle2, Color.Black, 2f);
            sb.DrawRectangle(mHudMarkerRectangle3, Color.Black, 2f);
            sb.DrawRectangle(mHudBackFillRectangle, Color.Black, 3f);
        }

        public void RefreshLinesToOffset(int offset)
        {
            if (mRecordLines != null)
            {
                foreach (MusicPreviewer.NoteLinePair mpnl in mRecordLines)
                {
                    mpnl.mRecordLine.CopyMinDrawPosInfo(mpnl.mSourceLine);
                    mpnl.mRecordLine.RefreshDrawRectForOffset(offset);
                }
            }
        }

        public bool Playing()
        {
            return mPreviewLines != null;
        }


        void SetJazzRiff()
        {
            AddNote(mRootNoteLineIndex, 0.0f, 0.3f);
            AddNote(mRootNoteLineIndex, 1.02f, 0.3f);
            AddNote(mRootNoteLineIndex, 1.4f, 0.1f);
            AddNote(mRootNoteLineIndex + 1, 1.4f, 0.1f);
            AddNote(mRootNoteLineIndex, 1.50f, 0.3f);
            AddNote(mRootNoteLineIndex + 2, 1.9f, 0.2f);
            AddNote(mRootNoteLineIndex - 1, 2.3f, 1f);

        }

        void SetFunkRiff()
        {
            AddNote(mRootNoteLineIndex, 0.0f, 0.3f);
            AddNote(mRootNoteLineIndex, 0.5f, 0.3f);
            AddNote(mRootNoteLineIndex + 1, 1.4f, 0.1f);
            AddNote(mRootNoteLineIndex + 2, 1.5f, 0.1f);
            AddNote(mRootNoteLineIndex + 3, 1.7f, 0.1f);
            AddNote(mRootNoteLineIndex + 4, 1.9f, 0.2f);
            AddNote(mRootNoteLineIndex + 5, 2.3f, 1f);
        }

        void SetRiff3()
        {
            AddNote(mRootNoteLineIndex, 0.0f, 0.3f);
            AddNote(mRootNoteLineIndex, 1f, 0.5f);
            AddNote(mRootNoteLineIndex + 1, 2f, 0.5f);
            AddNote(mRootNoteLineIndex + 2, 3f, 0.5f);
            AddNote(mRootNoteLineIndex + 3, 4f, 0.5f);
            AddNote(mRootNoteLineIndex + 4, 5f, 0.5f);
            AddNote(mRootNoteLineIndex + 5, 6f, 0.5f);
        }

        void SetFromCopyNotesPasteBuffer()
        {
            List<EditManager.CopyNote> lcn = EditManager.CopyNote.GetCopyNotes();

            foreach (EditManager.CopyNote cn in lcn)
            {
                int lineNum = cn.TrackOffset + mRootNoteLineIndex;
                AddNote(lineNum, cn.BeatPos - EditManager.CopyNote.mRectStartDiffBeat, cn.BeatLength, cn.Velocity);
            }
        }

        public void InitNotes(NotesToPreview ntp)
        {
            mRunning = true;
            mPlaying = true;
            mPa = mPreviewState.mMeledInterf.GetCurrentPlayArea();

            mHudClockCentreX = mPa.mAreaRectangle.X + mPa.mAreaRectangle.Width + +mHudClockRadius * 2;
            mHudClockCentreY = mPa.mAreaRectangle.Y + mPa.mAreaRectangle.Height + mHudClockRadius * 2;
            mHudBackRect = new Rectangle((int)mHudClockCentreX - (int)mHudClockRadius, (int)mHudClockCentreY - (int)mHudClockRadius, 2 * (int)mHudClockRadius, 2 * (int)mHudClockRadius);

            //mHudBackFillRectangle = new Rectangle(MainMusicGame.FIFTH_TICK_BUTTON_XPOS, MainMusicGame.BOTTOM_BUTTON_YPOS, FILL_RECT_WIDTH, FILL_RECT_BACK_HEIGHT);
            //mHudTimeLeftFillRectangle = new Rectangle(MainMusicGame.FIFTH_TICK_BUTTON_XPOS, MainMusicGame.BOTTOM_BUTTON_YPOS, 0, FILL_RECT_HEIGHT);
            mHudRiffProgressFillRectangle.Width = 0; // = new Rectangle(MainMusicGame.FIFTH_TICK_BUTTON_XPOS, MainMusicGame.BOTTOM_BUTTON_YPOS + FILL_RECT_HEIGHT + FILL_RECT_GAP, 0, FILL_RECT_HEIGHT);

            if (Playing())
            {
                System.Diagnostics.Debug.Assert(mRecordLines!=null, string.Format("EXPECT mRecordLines to be valid!"));

            }
            else
            {
                //Wasn't playing so initialised record list
                mRecordLines = new List<NoteLinePair>();
                mStartTouchOffset = ntp.mBeatPosTouch;
                mPreviewState.fMainCurrentBeat = ntp.mBeatPosTouch; //Back to start
                mPreviewState.fMainCurrentBeat += mPa.StartBeat;


                mBeatStartPosTouch = mPreviewState.fMainCurrentBeat;
                mBeatsToEndOfMax = PlayArea.MAX_BEATS - mBeatStartPosTouch;
                mRatioToEndOfBeats = 0f;
            }

            ClearRiffNotesOff(); //If any existing notes on
            mPreviewLines = new List<NoteLine>();
            mXTouch = ntp.mXTouch;

            mBeatLengthOfRiff = 0f;
            mRatioThroughRiff = 0f;
            mEndBeatOfRiff = 0f;

            mRootNoteLineIndex = mPa.GetLineIndexForNoteNum(ntp.mRootNote);

            if (mBasicMode)
            {
                AddNote(mRootNoteLineIndex, 0f, 1f);
            }
            else
            {
                switch (ntp.mSelectedRiff)
                {
                    case (int)PresetManager.eRiffTypeNum.Jazz:
                        SetJazzRiff();
                        break;
                    case (int)PresetManager.eRiffTypeNum.Funk:
                        SetFunkRiff();
                        break;
                    case (int)PresetManager.eRiffTypeNum.Riff3:
                        SetRiff3();
                        break;
                    case (int)PresetManager.eRiffTypeNum.CopyNotes:
                        SetFromCopyNotesPasteBuffer();
                        break;
                    default:
                        AddNote(mRootNoteLineIndex, 0f, 1f);
                        AddNote(mRootNoteLineIndex, 10f, 1f);
                        break;
                }
            }

            if (ntp.mChordLower>0)
            {
                int lowerIndex = mPa.GetLineIndexForNoteNum(ntp.mChordLower);
                AddNote(lowerIndex, 0f, 1f);
            }

            if (ntp.mChordUpper > 0)
            {
                int upperIndex = mPa.GetLineIndexForNoteNum(ntp.mChordUpper);
                AddNote(upperIndex, 0f, 1f);
            }

            mStartBeatOfRiff = mPreviewState.fMainCurrentBeat;
            mBeatLengthOfRiff = mEndBeatOfRiff - mPreviewState.fMainCurrentBeat;
        }

        void RecordNote(Note n)
        {

        }

        void AddNote(int lineIndex, float pos, float length, int velocity = 70)
        {
            NoteLine panl = mPa.GetNoteLineFromLineIndex(lineIndex);

            if(panl == null)
            {
                return;
            }

            int noteNum = panl.NoteNum;

            NoteLine nl = HaveLine(panl);

            if(nl ==null)
            {
                nl = new NoteLine(mPreviewState.mMeledInterf.GetCurrentPlayArea());
                int trackNum = 0;
                int drawTrackOffset = 0;
                nl.Init(trackNum, noteNum, drawTrackOffset);
                nl.CopyMinDrawPosInfo(panl);

                NoteLine recline = new NoteLine(mPreviewState.mMeledInterf.GetCurrentPlayArea());
                recline.Init(trackNum, noteNum, drawTrackOffset);
                recline.CopyMinDrawPosInfo(panl);

                //NoteLinePair nlp;
                //nlp.mPreviewLine = nl;
                //nlp.mSourceLine = panl;
                //nlp.mRecordLine = recline;
                //nl.mStartX = mXTouch;
                mPreviewLines.Add(nl);
            }

            float lengthThisNote = mPreviewState.fMainCurrentBeat + pos + length;

            if(lengthThisNote > mEndBeatOfRiff)
            {
                mEndBeatOfRiff = lengthThisNote;
            }

            Note n = nl.AddNote(mPreviewState.fMainCurrentBeat + pos, length);
            n.Velocity = velocity;
        }

        NoteLine HaveLine(NoteLine other)
        {
            foreach (NoteLine nlp in mPreviewLines)
            {
                if (nlp.NoteNum == other.NoteNum)
                {
                    return nlp;
                }
            }
            return null;
        }

        NoteLine GetNoteLine(int noteNum)
        {
            foreach (NoteLine nlp in mPreviewLines)
            {
                if (nlp.NoteNum ==noteNum)
                {
                    return nlp;
                }
            }

            return null;
        }

        NoteLinePair RecordNoteToLinePair(Note note)
        {
            NoteLinePair newNlp;
            newNlp.mSourceLine = null;
            newNlp.mRecordLine = null;
            bool bExistingNoteLine = false;

            foreach (NoteLinePair nlp in mRecordLines)
            {
                if (nlp.mRecordLine.NoteNum == note.mNoteNum)
                {
                    bExistingNoteLine = true;
                    newNlp = nlp;
                    break;
                }
            }

            if(!bExistingNoteLine)
            {
                //Create the NoteLinePair for this note
                NoteLine nl = mPreviewState.mMeledInterf.GetCurrentPlayArea().GetNoteLineAtNoteNumber(note.mNoteNum);

                if (nl != null)
                {
                    newNlp.mSourceLine = nl;

                    NoteLine recline = new NoteLine(mPreviewState.mMeledInterf.GetCurrentPlayArea());
                    int trackNum = 0;
                    int drawTrackOffset = 0;
                    recline.Init(trackNum, note.mNoteNum, drawTrackOffset);
                    recline.CopyMinDrawPosInfo(nl);

                    newNlp.mRecordLine = recline;
                    mRecordLines.Add(newNlp);
                    bExistingNoteLine = true;
                }
            }

            if (!bExistingNoteLine)
            {
                System.Diagnostics.Debug.Assert(false, string.Format("No line for noteNum {0}!", note.mNoteNum));
                return newNlp;
            }

            Note n = newNlp.mRecordLine.AddNoteIfSpace(note.BeatPos, note.BeatLength);

            System.Diagnostics.Debug.WriteLine(string.Format("WRITE 0  mPreviewState.fCurrentBeat {0}, n {1}", mPreviewState.fMainCurrentBeat, n));

            if (n==null)
            {
                const float TINY_NOTE_WIDTH = 0.07f; //Dont make this too small since the check requires comparing rectangles that could have 0 pixel width if too small!
                n = newNlp.mRecordLine.IsNoteHere(note.BeatPos, TINY_NOTE_WIDTH);

                System.Diagnostics.Debug.WriteLine(string.Format("WRITE 1  note.BeatPo {0}, n {1}", note.BeatPos, n));
            }

            if (n!=null)
            {
                float newLength = mPreviewState.fMainCurrentBeat - n.BeatPos;
                if(newLength>0f)
                {
                    n.BeatLength = newLength;
                    System.Diagnostics.Debug.WriteLine(string.Format("WRITE 2  newLength {0}", newLength));
                    n.RefreshRectToData();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("WRITE 2  newLength ZERO SO WAIT!"));
                }
            }

            return newNlp;
        }

        public void QuitNow()
        {
            if(!mRecordMode)
            {
                mRunning = false;
            }

            mPlaying = false;
            AllNotesOff();
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        /// UPDATE
        /// UPDATE
        /// UPDATE
        /// UPDATE
        /// UPDATE
        /// UPDATE
        //////////////////////////////////////////////////////////////////////////////////////////
        public async Task Update()
        {
            try
            {
                var swAll = Stopwatch.StartNew();
                while (mRunning) 
                {
                    var sw = Stopwatch.StartNew();

                    if(mPlaying)
                    {
                        foreach (NoteLine nlp in mPreviewLines)
                        {
                            nlp.UpdatePlaying(mPreviewState);

                            if (nlp.mbNoteInPlay && !mBasicMode)
                            {
                                RecordNoteToLinePair(nlp.mCurrentNoteInPlay);
                                //float pos = nlp.mPreviewLine.mCurrentNoteInPlay.BeatPos;
                                //float length = nlp.mPreviewLine.mCurrentNoteInPlay.BeatLength;
                                //nlp.mRecordLine.AddNoteIfSpace(pos, length);
                            }
                        }
                    }

                    await Task.Delay(1); //set to lowest since we have to try and get the most out of this, this will be limited by resolution to 15 ms and could be 30ms sometimes, so use a stopwatch to work out how much beat time elapses each cycle

                    float beatIncrement = sw.ElapsedMilliseconds * mPreviewState.mMeledInterf.mBeat_Per_MS;

                    mPreviewState.fMainCurrentBeat += beatIncrement;
                    if (mPreviewState.fMainCurrentBeat >= mEndBeatOfRiff)
                    {
                        if (!mRecordMode)
                        {
                            mRunning = false;
                        }
                    }

                    mRatioThroughRiff = (mPreviewState.fMainCurrentBeat - mStartBeatOfRiff) / mBeatLengthOfRiff;

                    mRatioToEndOfBeats = (mPreviewState.fMainCurrentBeat - mBeatStartPosTouch)/ mBeatsToEndOfMax;

                    if(mRatioToEndOfBeats>=1f)
                    {
                        mRatioToEndOfBeats = 1f;
                        mRunning = false; //stop it going off the end of maximum recordable space!
                    }

                    if (mRatioThroughRiff>=1f)
                    {
                        mRatioThroughRiff = 1f;
                    }

                    //System.Diagnostics.Debug.WriteLine(string.Format(" MUSIC PREVIEWER UPDATE mUpdateCount {0} sw {1} adjustMS {2} -- fCurrentBeat {3}", mUpdateCount, elapsedMS, adjustMS, mPrevState.fCurrentBeat));
                }
                int totalMSTimeForRiff = (int)swAll.ElapsedMilliseconds;

                float expectedMSForRIff = mEndBeatOfRiff * mPreviewState.mMeledInterf.mMS_Per_Beat;

                System.Diagnostics.Debug.WriteLine(string.Format("- - - - - -MUSIC PREVIEWER UPDATE ALL {0} expectedMSForRIff {1} mBeatLengthOfRiff {2}  - mPrevState.fCurrentBeat {3}", totalMSTimeForRiff, expectedMSForRIff, mEndBeatOfRiff, mPreviewState.fMainCurrentBeat));

            }
            catch (Exception e)
            {
                //mActiveGrid = null; //leave altogether
                System.Diagnostics.Debug.WriteLine(string.Format("MUSIC PREVIEWER Exception {0}", e.ToString()));
            }

            ClearRiffNotesOff();
        }

        void ClearRiffNotesOff()
        {
            AllNotesOff();
            mPreviewLines = null;
        }

        void AllNotesOff()
        {
            if (mPreviewLines == null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format(" MUSIC PREVIEWER AllNotesOff mPrevLines NULL"));
                return;
            }
            foreach (NoteLine nlp in mPreviewLines)
            {
                System.Diagnostics.Debug.WriteLine(string.Format(" MUSIC PREVIEWER AllNotesOff mPrevLines nl.NoteNum {0}", nlp.NoteNum));
                mPreviewState.GetMidiBase().NoteOff(nlp.GetChannel(), nlp.NoteNum);
            }
        }

    }
}
