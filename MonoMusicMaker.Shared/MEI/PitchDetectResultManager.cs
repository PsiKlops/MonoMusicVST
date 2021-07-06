using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace MonoMusicMaker
{
    public class PitchDetectResultManager
    {
        // to map to chordTypeNames array below
        public enum eBeatWidthType
        {
            Eighth,
            Quarter,
            Half,
            Full,
            num_beatWidths,
        }
        string[] beatWidthNames = new string[(int)eBeatWidthType.num_beatWidths] { "Eighth", "Quarter", "Half", "Full"};

        // to map to chordTypeNames array below
        public enum eBarDestination
        {
            one,
            two,
            three,
            four,
            num_barDestWidths,
        }
        string[] barDestNames = new string[(int)eBarDestination.num_barDestWidths] { "One", "Two", "Three", "Four" };

        // to map to chordTypeNames array below
        public enum eBarOffset
        {
            zero,
            one,
            two,
            three,
            num_barOffset,
        }
        string[] barOffsetNames = new string[(int)eBarOffset.num_barOffset] { "Zero", "One", "Two", "Three" };


        const int XPOS = 100;
        const int YPOS = 100;
        const int WIDTH = 1600;
        const int HEIGHT = 800;
        const int BUTTON_GAP = 20;
        const int EXIT_BUTT_X = XPOS + (WIDTH / 2) - (Button.mWidth / 2);
        const int EXIT_BUTT_Y = YPOS + BUTTON_GAP;
        const int BOTTOM_ROW_BUTT_Y = YPOS + HEIGHT - (Button.mBarHeight + BUTTON_GAP);
        const int BUTTON_START_X = XPOS + BUTTON_GAP;
        const int BUTTON_START_X2 = BUTTON_START_X + Button.mWidth + BUTTON_GAP;
        const int BUTTON_START_X3 = BUTTON_START_X + 2 * (Button.mWidth + BUTTON_GAP);
        const int BUTTON_START_X4 = BUTTON_START_X + 3 * (Button.mWidth + BUTTON_GAP);

        const int START_STOP_NOTE_X = XPOS + WIDTH - (BUTTON_GAP + Button.mTickBoxSize); //Top right
        const int START_STOP_NOTE_Y = YPOS + (BUTTON_GAP); //Top right
        const int BAR_COMPRESS_NOTE_Y = YPOS + (2 * BUTTON_GAP + Button.mTickBoxSize); //Top right
        const int START_AT_FIRST_SAMPLE_NOTE_Y = YPOS + 3*( BUTTON_GAP + Button.mTickBoxSize); //Top right


        const int TEXT_INFO_X = XPOS + BUTTON_GAP;
        const int TEXT_BACK_WIDTH = 32;
        const int TEXT_BACK_YPOS = YPOS + 60;
        const int TEXT_BACK_HEIGHT = HEIGHT - 120;

        const float PIXELS_PER_SEC = 100f;
        const int NOTE_RECORD_WIDTH = 3;
        const int NOTE_HEIGHT = 8;
        const int BOTTOM_LINE_NOTES_Y = YPOS + HEIGHT - (Button.mBarHeight + BUTTON_GAP) - 40;

        public bool Active { get; set; } = false;
        Vector2 mScreenPos;   //top left
        Rectangle mPopUpRect = new Rectangle(XPOS, YPOS, WIDTH, HEIGHT);
        SpriteFont mFont;
        SpriteFont mSmallFont;

        Rectangle mTextBackRect;

        List<Rectangle> mNotePoints = new List<Rectangle>();

        Button mExitButton;

        Button mStartStopSampling;
        Button mProcess;
        Button mStartStopPlayback;
        Button mDoCompressToBarsSelected;
        Button mStartAtFirstSample;

        List<Button> mButtons = new List<Button>();

        AudioRecordBase mAudioRecorder = null;
        List<Pitch.PitchTracker.PitchRecord> mPitchRecords = null;

        String mTextTopNote =""; //bar range covered mostly I think
        String mTextBotNote =""; //bar range covered mostly I think

        int mTextTopY;
        int mTextBotY;
        int mLowestNote;
        int mHighestNote;

        List<ParamButtonBar> mParamBars = new List<ParamButtonBar>();

        ParamButtonBar mBinWidthBar;
        ParamButtonBar mBarsToResizeBar;
        ParamButtonBar mBarsToStartAt;

        const int PARAM_BAR_YPOS = BOTTOM_ROW_BUTT_Y; // YPOS + HEIGHT / 2 - (Button.mBarHeight / 2);
        const int PARAM_BAR_XPOS_START = BUTTON_START_X3;
        const int PARAM_BAR_XPOS_GAP = 40;


        float mCurrentWindowWidth = 0;
        int mNumBarsToCompressTo = PlayArea.MAX_BAR_RANGE;
        int mNumBarsOffsetStart = 0;

        //Lower notes need less?
        class noteWindow
        {
            public const int MIN_COUNT = 4; //num freq samples seen within this note window period before denote as valid - if 0.1 sec window and sampling is 0.02 allow for 4
            public const float THRESHOLD = 0.75f; //?
            public const int SAMPLE_GAP_AFTER_VALID = 2; //for a current valid candidate we can allow this gap between further samples to be addded to length of note 
            public const int SAMPLE_GAP_AFTER_VALID_ACCUM = 3; //for a current valid candidate we can allow this gap between further samples to be addded to length of note 
            public const int SAMPLE_GAP_FAIL_BEFORE_VALID = 1; //Before current is valid if we see more than this gap between consecutive samples before we abort and restart this candidate - min is 1 for consecutive
            int mNumIndexesForMinNote = 5;

            public int mMididNote=0;
            public float mWidthBeat;
            public static float mEarliestStart = 999f;

            public noteWindow(float width)
            {
                mWidthBeat = width;
            }

            noteCandidate mCurrentCandidate = null;

            public class noteCandidate
            {
                public int mCount=0;
                public float mStart;
                public float mEnd;
                public bool mOverThresh = false;
                public bool mValid = false;
                public int mLastIndex = 0;
                public int mStartIndex = 0;
                public int mAccumulateGapInitial = 0;
                public int mAccumulateGapPost = 0;

                //public void 
                public void Clear()
                {
                    mCount = 0;
                    mStart =0f;
                    mEnd=0f;
                    mOverThresh = false;
                    mValid = false;
                    mLastIndex = 0;
                    mStartIndex = 0;
                    mAccumulateGapInitial = 0;
                    mAccumulateGapPost = 0;
                }

                public void AddCount()
                {
                    mCount++;

                    if(mCount>= MIN_COUNT && mOverThresh)
                    {
                        mValid = true;
                    }
                }
                public void AddCountNew()
                {
                    mCount++;

                    if (mCount >= MIN_COUNT)
                    {
                        mValid = true;
                    }
                }
            }

            void SetStart(Pitch.PitchTracker.PitchRecord pr)
            {
                float fTime = pr.mTimeFromPos;
                int thisIndex = pr.RecordIndex;

                mCurrentCandidate.mStart = fTime;
                if(fTime<mEarliestStart)
                {
                    mEarliestStart = fTime;
                }
                mCurrentCandidate.mEnd = fTime + Pitch.PitchTracker.TIME_PER_PITCH_RECORD; //update end in case this is the last sample
                mCurrentCandidate.AddCountNew();
                mCurrentCandidate.mStartIndex = thisIndex;
                mCurrentCandidate.mLastIndex = thisIndex;
            }

            void Add(Pitch.PitchTracker.PitchRecord pr)
            {
                float fTime = pr.mTimeFromPos;
                int thisIndex = pr.RecordIndex;

                mCurrentCandidate.AddCountNew();
                mCurrentCandidate.mLastIndex = thisIndex;
                mCurrentCandidate.mEnd = fTime + Pitch.PitchTracker.TIME_PER_PITCH_RECORD; //update end in case this is the last sample
            }

            void NewCandidate(Pitch.PitchTracker.PitchRecord pr)
            {
                mCurrentCandidate = new noteCandidate();

                SetStart(pr);

                mCandidates.Add(mCurrentCandidate);
            }

            public void NewCheckRecord(Pitch.PitchTracker.PitchRecord pr )
            {
                float fTime = pr.mTimeFromPos;
                int thisIndex = pr.RecordIndex;

                if (mCurrentCandidate == null)
                {
                    NewCandidate( pr );
                }
                else if (thisIndex < (mCurrentCandidate.mStartIndex + mNumIndexesForMinNote))
                {
                    int gap = thisIndex - mCurrentCandidate.mLastIndex;

                    mCurrentCandidate.mAccumulateGapInitial += (gap-1);

                    if (mCurrentCandidate.mAccumulateGapInitial <= SAMPLE_GAP_FAIL_BEFORE_VALID)
                    {
                        Add(pr);
                    }
                    else
                    {
                        //too big gap between samples, rejig this candidate along to this index as start
                        mCurrentCandidate.Clear();

                        SetStart(pr);
                    }
                }
                else //Over the min for one unit keep updating and adding if within permitted gap or end the note and start new one
                {
                    int gap = thisIndex - mCurrentCandidate.mLastIndex;
                    mCurrentCandidate.mAccumulateGapPost += (gap-1);

                    if (gap <= SAMPLE_GAP_AFTER_VALID && mCurrentCandidate.mAccumulateGapPost <= SAMPLE_GAP_AFTER_VALID_ACCUM)
                    {
                        //keep adding and stretching this candidate
                        Add(pr);
                    }
                    else
                    {
                        //we have seen a big enough gap after our current candidate so create new one
                        NewCandidate(pr);
                    }
                }
            }

            /// <summary>
            /// /////////////////////////////
            /// </summary>
            /// <param name="ftime"></param>
            public void CheckRecord(float ftime)
            {
                if(mCurrentCandidate==null)
                {
                    mCurrentCandidate = new noteCandidate();
                    mCurrentCandidate.mStart = ftime;
                    mCurrentCandidate.AddCount();
                    mCandidates.Add(mCurrentCandidate);
                }

                else if(ftime < (mCurrentCandidate.mStart+ mWidthBeat))
                {
                    if ((ftime + Pitch.PitchTracker.TIME_PER_PITCH_RECORD) > (mCurrentCandidate.mStart + 0.75f*mWidthBeat))
                    {
                        mCurrentCandidate.mOverThresh = true;
                    }
                    mCurrentCandidate.AddCount();
                }
                else //ove here if we have th emin for one unit (prob settle on 0.1 sec 5 samples) and we see gap is bigger than 2 proba 2
                {
                    mCurrentCandidate = new noteCandidate();
                    mCurrentCandidate.mStart = ftime;
                    mCurrentCandidate.AddCount();
                    mCandidates.Add(mCurrentCandidate);
                }
            }

            public List<noteCandidate> mCandidates = new List<noteCandidate>();
        };

        Dictionary<int, noteWindow> mNoteWindows =   new Dictionary<int, noteWindow>();

        public void SetPitchRecords(List<Pitch.PitchTracker.PitchRecord> pitchRecords)
        {
            mPitchRecords = pitchRecords;
        }

        void UpdateWIndowWidthFromInput()
        {
            switch((eBeatWidthType)mBinWidthBar.GetSelected())
            {
                case eBeatWidthType.Eighth:
                    mCurrentWindowWidth = 0.1f; //make these multiples of 1/PITCH_RECORDS_PER_SEC
                    break;
                case eBeatWidthType.Quarter:
                    mCurrentWindowWidth = 0.2f;
                    break;
                case eBeatWidthType.Half:
                    mCurrentWindowWidth = 0.5f;
                    break;
                case eBeatWidthType.Full:
                    mCurrentWindowWidth = 1f;
                    break;

                default:
                    mCurrentWindowWidth = 1f;
                    break;

            }
        }
        void UpdateBarDestinationFromInput()
        {
            switch ((eBarDestination)mBarsToResizeBar.GetSelected())
            {
                case eBarDestination.one:
                    mNumBarsToCompressTo = 1; //make these multiples of 1/PITCH_RECORDS_PER_SEC
                    break;
                case eBarDestination.two:
                    mNumBarsToCompressTo = 2;
                    break;
                case eBarDestination.three:
                    mNumBarsToCompressTo = 3;
                    break;
                case eBarDestination.four:
                    mNumBarsToCompressTo =PlayArea.MAX_BAR_RANGE;
                    break;
            }
        }

        //TODO will need to limit how far off right this pushes it!
        void UpdateNumBarsOffsetStart()
        {
            switch ((eBarOffset)mBarsToStartAt.GetSelected())
            {
                case eBarOffset.one:
                    mNumBarsOffsetStart = 1; 
                    break;
                case eBarOffset.two:
                    mNumBarsOffsetStart = 2;
                    break;
                case eBarOffset.three:
                    mNumBarsOffsetStart = 3;
                    break;
                case eBarOffset.zero:
                    mNumBarsOffsetStart = 0; //TODO too far!
                    break;
            }
        }

        void ProcessRecords(MelodyEditorInterface.MEIState state)
        {
            noteWindow.mEarliestStart = 999f;

            UpdateWIndowWidthFromInput();
            UpdateBarDestinationFromInput();
            UpdateNumBarsOffsetStart();

            mNoteWindows = new Dictionary<int, noteWindow>(); //reset

            mPitchRecords = mAudioRecorder.GetPitchRecords();

            mNotePoints = new List<Rectangle>();
            mLowestNote = 999;
            mHighestNote = 0;

            foreach (Pitch.PitchTracker.PitchRecord pr in mPitchRecords)
            {

                if(pr.MidiNote>mHighestNote)
                {
                    mHighestNote = pr.MidiNote;
                }
                if (pr.MidiNote !=0 && pr.MidiNote < mLowestNote)
                {
                    mLowestNote = pr.MidiNote;
                }
            }

            mTextBotY = BOTTOM_LINE_NOTES_Y;
            mTextTopY = BOTTOM_LINE_NOTES_Y + (mLowestNote - mHighestNote )* NOTE_HEIGHT;

            mTextTopNote = string.Format("{0}", mHighestNote);
            mTextBotNote = string.Format("{0}", mLowestNote);

            int countAcross = 0;

            bool bUseNewConvert = true;

            foreach (Pitch.PitchTracker.PitchRecord pr in mPitchRecords)
            {
               // int xpos = BUTTON_START_X + countAcross * NOTE_RECORD_WIDTH;

                if(pr.MidiNote!=0)
                {
                    //float fTime = (float)pr.mTimeWhen.TotalSeconds;
                    float fTime = (float)pr.mTimeFromPos;
                    if (!mNoteWindows.ContainsKey(pr.MidiNote))
                    {
                        noteWindow nw = new noteWindow(mCurrentWindowWidth);
                        nw.mMididNote = pr.MidiNote;
                        if(bUseNewConvert)
                        {
                            nw.NewCheckRecord(pr);
                        }
                        else
                        {
                            nw.CheckRecord(fTime);
                        }
                        mNoteWindows.Add(pr.MidiNote, nw);
                    }
                    else
                    {
                        noteWindow nw = mNoteWindows[pr.MidiNote];
                        if (bUseNewConvert)
                        {
                            nw.NewCheckRecord(pr);
                        }
                        else
                        {
                            nw.CheckRecord(fTime);
                        }
                    }

                    int xpos = BUTTON_START_X + (int)(fTime * PIXELS_PER_SEC);
                    int ypos = BOTTOM_LINE_NOTES_Y + (mLowestNote - pr.MidiNote) * NOTE_HEIGHT;

                    Rectangle rect = new Rectangle(xpos, ypos, NOTE_RECORD_WIDTH, NOTE_HEIGHT);

                    mNotePoints.Add(rect);
                }

                countAcross++;
            }

            if (bUseNewConvert)
            {
                NewNoteWindowsConvertToFinalNotes(state);
            }
            else
            {
                OldNoteWindowsConvertToFinalNotes(state);
            }
        }

        void OldNoteWindowsConvertToFinalNotes(MelodyEditorInterface.MEIState state)
        {
            //change current play area - TODO will be limited to defined bar region just blatt out notes on the note lines to get a feel for near enough

            PlayArea currentPlayArea = state.mMeledInterf.GetCurrentPlayArea();

            currentPlayArea.ResetAllNotesAndRegion();
            state.PlayAreaChanged(true);

            float bpm = state.mMeledInterf.BPM;
            float bpmConvert = bpm / 60f;

            const float joinBeatGap = 0.1f; //if gap between a note end and the next note start on a line are less than this then join them

            foreach (KeyValuePair<int, noteWindow> entry in mNoteWindows)
            {
                // do something with entry.Value or entry.Key

                noteWindow nw = entry.Value;
                float pendingBeatStart = -1f;
                float pendingBeatEnd = -1f;
                bool bAddPendingNote = false;

                for (int i = 0; i < nw.mCandidates.Count; i++)
                {
                    NoteLine nl = currentPlayArea.GetNoteLineAtNoteNumber(nw.mMididNote);
                    noteWindow.noteCandidate nc = nw.mCandidates[i];

                    if (i >= (nw.mCandidates.Count - 1))
                    {
                        bAddPendingNote = true;
                    }

                    if (nc.mValid)
                    {
                        float beatStart = nc.mStart * bpmConvert;
                        float beatWidth = nw.mWidthBeat * bpmConvert;

                        if (pendingBeatStart == -1f)
                        {
                            pendingBeatStart = beatStart;
                            pendingBeatEnd = beatStart + beatWidth;
                        }
                        else
                        {
                            float thisBeatEnd = beatStart + beatWidth;
                            if ((beatStart - pendingBeatEnd) > joinBeatGap)
                            {
                                nl.AddNote(pendingBeatStart, pendingBeatEnd - pendingBeatStart);
                                pendingBeatStart = beatStart;
                                pendingBeatEnd = beatStart + beatWidth;
                            }
                            else
                            {
                                pendingBeatEnd = beatStart + beatWidth;
                            }
                        }

                    }

                    if (bAddPendingNote && pendingBeatStart > -1f)
                    {
                        nl.AddNote(pendingBeatStart, pendingBeatEnd - pendingBeatStart);
                        nl.AddNote(pendingBeatStart, pendingBeatEnd - pendingBeatStart);
                    }
                }
            }
        }

        void NewNoteWindowsConvertToFinalNotes(MelodyEditorInterface.MEIState state)
        {
            PlayArea currentPlayArea = state.mMeledInterf.GetCurrentPlayArea();
            currentPlayArea.ResetAllNotesAndRegion();
            state.PlayAreaChanged(true);

            float bpm = state.mMeledInterf.BPM;
            float bpmConvert = bpm / 60f;

            float offsetTimeToGetStartAt0 = noteWindow.mEarliestStart;

            foreach (KeyValuePair<int, noteWindow> entry in mNoteWindows)
            {
                noteWindow nw = entry.Value;

                for (int i = 0; i < nw.mCandidates.Count; i++)
                {
                    NoteLine nl = currentPlayArea.GetNoteLineAtNoteNumber(nw.mMididNote);
                    noteWindow.noteCandidate nc = nw.mCandidates[i];

                    if (nc.mValid)
                    {
                        float timeStart = nc.mStart ;
                        float timeEnd = nc.mEnd ;
                        if (mStartAtFirstSample.mbOn)
                        {
                            timeStart -= offsetTimeToGetStartAt0;
                            timeEnd -= offsetTimeToGetStartAt0;
                        }
                        float beatStart = timeStart * bpmConvert;
                        float beatEnd = timeEnd * bpmConvert;
                        //if (mStartAtFirstSample.mbOn)
                        //{
                        //    float beatOffset = mNumBarsOffsetStart * PlayArea.BEATS_PER_BAR;
                        //    beatStart += beatOffset;
                        //    beatEnd += beatOffset;
                        //}
                        nl.AddNote(beatStart, beatEnd - beatStart);
                    }
                 }
            }

            //Set loop region to encompass new notes
            int noteRangeBars = currentPlayArea.SetBarLoopRangeToCoverNotesRange();

            if(mDoCompressToBarsSelected.mbOn)
            {
                float factor = (float)mNumBarsToCompressTo / (float)noteRangeBars;

                currentPlayArea.ResetNotesByFactor(factor);
                currentPlayArea.SetBarLoopRangeToCoverNotesRange();
            }
            if (mStartAtFirstSample.mbOn)
            {
                currentPlayArea.StartNotesAtBar(mNumBarsOffsetStart);
            }
            if (mStartAtFirstSample.mbOn || mDoCompressToBarsSelected.mbOn)
            {
                currentPlayArea.SetBarLoopRangeToCoverNotesRange();
            }
        }

        public void Start(MelodyEditorInterface.MEIState state)
        {
            state.BlockBigGreyRectangle = true;
            state.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_PitchDet; //set to only this button updates
            Active = true;
        }

        public void Stop(MelodyEditorInterface.MEIState state)
        {
            state.BlockBigGreyRectangle = false;
            state.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_ALL; //allow all
            Active = false;
        }

        public void Init(AudioRecordBase audioRecorder)
        {
            mAudioRecorder = audioRecorder;

            mStartStopSampling = new Button(new Vector2(BUTTON_START_X, BOTTOM_ROW_BUTT_Y), "PlainWhite");
            mStartStopSampling.ButtonText = "Start";
            mStartStopSampling.mType = Button.Type.Bar;
            mStartStopSampling.ColOn = Color.Green;
            mStartStopSampling.ColOff = Color.Red;
            mStartStopSampling.ButtonTextColour = Color.Black;

            mStartStopPlayback = new Button(new Vector2(START_STOP_NOTE_X, START_STOP_NOTE_Y), "PlainWhite");
            mStartStopPlayback.mType = Button.Type.Tick;
            mStartStopPlayback.ColOff   = Color.Red * 0.6f;
            mStartStopPlayback.ColOn = Color.Green * 0.6f;
            mStartStopPlayback.ButtonTextLeft = "Start";
            mStartStopPlayback.ButtonTextOff = "Stop";
            mStartStopPlayback.ButtonTextColour = Color.Black;

            mDoCompressToBarsSelected = new Button(new Vector2(START_STOP_NOTE_X, BAR_COMPRESS_NOTE_Y), "PlainWhite");
            mDoCompressToBarsSelected.mType = Button.Type.Tick;
            mDoCompressToBarsSelected.ColOff = Color.Gray * 0.6f;  
            mDoCompressToBarsSelected.ColOn = Color.Purple * 0.6f;
            mDoCompressToBarsSelected.ButtonTextLeft = "Comp On";
            mDoCompressToBarsSelected.ButtonTextOff = "Comp Off";
            mDoCompressToBarsSelected.ButtonTextColour = Color.Black;

            mStartAtFirstSample = new Button(new Vector2(START_STOP_NOTE_X, START_AT_FIRST_SAMPLE_NOTE_Y), "PlainWhite");
            mStartAtFirstSample.mType = Button.Type.Tick;
            mStartAtFirstSample.ColOff = Color.Gray * 0.6f;
            mStartAtFirstSample.ColOn = Color.Purple * 0.6f;
            mStartAtFirstSample.ButtonTextLeft = "Immed On";
            mStartAtFirstSample.ButtonTextOff = "Immed Off";
            mStartAtFirstSample.ButtonTextColour = Color.Black;

            mProcess = new Button(new Vector2(BUTTON_START_X2, BOTTOM_ROW_BUTT_Y), "PlainWhite");
            mProcess.ButtonText = "Process";
            mProcess.mType = Button.Type.Bar;
            mProcess.ColOn = Color.BlanchedAlmond;
            mProcess.ColOff = Color.BlanchedAlmond;
            mProcess.ButtonTextColour = Color.Black;

            mExitButton = new Button(new Vector2(EXIT_BUTT_X, EXIT_BUTT_Y), "PlainWhite");
            mExitButton.ButtonText = "Exit";
            mExitButton.mType = Button.Type.Bar;
            mExitButton.ColOn = Color.BlanchedAlmond;
            mExitButton.ColOff = Color.BlanchedAlmond;
            mExitButton.ButtonTextColour = Color.Black;


            mButtons.Add(mStartAtFirstSample);
            mButtons.Add(mDoCompressToBarsSelected);
            mButtons.Add(mStartStopPlayback);
            mButtons.Add(mStartStopSampling);
            mButtons.Add(mProcess);
            mButtons.Add(mExitButton);

            foreach (Button ab in mButtons)
            {
                ab.mMask = MelodyEditorInterface.UIButtonMask.UIBM_PitchDet;
            }

            mTextBackRect = new Rectangle(TEXT_INFO_X, TEXT_BACK_YPOS, TEXT_BACK_WIDTH, TEXT_BACK_HEIGHT);

            mBinWidthBar = SetUpPopupParamBar(beatWidthNames);
            mBarsToResizeBar = SetUpPopupParamBar(barDestNames);
            mBarsToStartAt = SetUpPopupParamBar(barOffsetNames);
        }

        public void LoadContent(ContentManager contentMan, SpriteFont font, SpriteFont smallGridFont)
        {
            mFont = font;
            mSmallFont = smallGridFont;

            foreach (Button ab in mButtons)
            {
                ab.LoadContent(contentMan, font);
            }
            foreach (ParamButtonBar pbb in mParamBars)
            {
                pbb.LoadContent(contentMan, font);
            }
        }

        public void UpdateInput(MelodyEditorInterface.MEIState state)
        {
            if (state.input.Held)
            {
                //if (state.input.mRectangle.Intersects(mSelectRectangle))
                //{
                //}
            }

            //BUTTON UPDATES
            if (mExitButton.Update(state))
            {
                Stop(state);
            }

            if (mStartStopSampling.Update(state))
            {
                if (mStartStopSampling.mbOn)
                {
                    mAudioRecorder.Start();
                }
                else
                {
                    mAudioRecorder.Stop();
                }
            }

            if (mStartStopPlayback.Update(state))
            {
                state.mMaskToSetButton = MelodyEditorInterface.UIButtonMask.UIBM_Start;
            }

            if (mDoCompressToBarsSelected.Update(state))
            {
                //state.mMaskToSetButton = MelodyEditorInterface.UIButtonMask.UIBM_Start;
            }
            if (mStartAtFirstSample.Update(state))
            {
                //state.mMaskToSetButton = MelodyEditorInterface.UIButtonMask.UIBM_Start;
            }

            if (mProcess.Update(state))
            {
                if (mProcess.mbOn)
                {
                    mAudioRecorder.Process();
                }
                mProcess.mbOn = false;
            }

            if(mAudioRecorder.ResultsReady)
            {
                ProcessRecords(state);
                mAudioRecorder.ResultsReady = false;
            }

            bool bValueChanged = false;
            int count = 0;

            ParamButtonBar beenSetInBarMode = null;
            foreach (ParamButtonBar pbb in mParamBars)
            {
                if (pbb.Update(state))
                {
                    beenSetInBarMode = pbb; //Exit since this has been set 
                }
            }

            //Click off to close param bar
            if (beenSetInBarMode == null)
            {
                if (state.input.LeftUp)
                {
                    foreach (ParamButtonBar pbb in mParamBars)
                    {
                        pbb.ShowSelected(state);
                    }
                }
            }
        }

        public void AddParamBar(ParamButtonBar pbb)
        {
            int numBars = mParamBars.Count;
            Vector2 vPos = new Vector2();

            vPos.Y = PARAM_BAR_YPOS;
            vPos.X = XPOS + PARAM_BAR_XPOS_START + numBars * (PARAM_BAR_XPOS_GAP + Button.mWidth);
            pbb.Init(vPos);

            mParamBars.Add(pbb);
        }

        public void ParamBarCB(Button button, MelodyEditorInterface.MEIState state)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("ParamBarCB value {0}", button.mGED.mValue));
        }

        ParamButtonBar SetUpPopupParamBar(string[] typeNames)
        {
            List<ButtonGrid.GridEntryData> aiGED = new List<ButtonGrid.GridEntryData>();
            int count = 0;

            foreach (string str in typeNames)
            {
                ButtonGrid.GridEntryData aiGEDEntry;

                aiGEDEntry.mOnString = str;
                aiGEDEntry.mValue = count;
                aiGEDEntry.mClickOn = ParamBarCB;
                count++;
                aiGED.Add(aiGEDEntry);
            }

            ParamButtonBar pbb = new ParamButtonBar(aiGED, ParamButtonBar.DROP_TYPE.UP);

            AddParamBar(pbb);

            return pbb;
        }


        public void Draw(SpriteBatch sb)
        {
            if (!Active)
            {
                return;
            }

            sb.Begin();
            sb.FillRectangle(mTextBackRect, Color.White * 0.5f );
            sb.FillRectangle(mPopUpRect, Color.SteelBlue * 0.5f );
            sb.DrawRectangle(mPopUpRect, Color.Black, 4);
            sb.FillRectangle(mTextBackRect, Color.White * 0.5f );
            foreach (Rectangle rect in mNotePoints)
            {
                sb.FillRectangle(rect, Color.Yellow);
            }

            int halfStringHeight = (int)mSmallFont.MeasureString(mTextTopNote).Y/2;

            sb.DrawString(mSmallFont, mTextTopNote, new Vector2(TEXT_INFO_X, mTextTopY- halfStringHeight), Color.Black);
            sb.DrawString(mSmallFont, mTextBotNote, new Vector2(TEXT_INFO_X, mTextBotY- halfStringHeight), Color.Black);

            sb.End();

            foreach (Button ab in mButtons)
            {
                ab.Draw(sb);
            }

            foreach (ParamButtonBar pbb in mParamBars)
            {
                pbb.Draw(sb);
            }

            sb.Begin();
            sb.DrawRectangle(mStartStopPlayback.Rectangle, Color.Black);
            sb.DrawRectangle(mDoCompressToBarsSelected.Rectangle, Color.Black);
            sb.DrawRectangle(mStartAtFirstSample.Rectangle, Color.Black);
            sb.End();
        }
    }
}