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
using Newtonsoft.Json;

namespace MonoMusicMaker
{
    using DrumLineButtonList = List<Button>;

    public class DrumMachinePopUp
    {
        //DEFINES
        const int NUM_BANKS = 2;
#if WINDOWS
        const int MAX_LINES_IN_DRUM_BANK = 14;
#else
        const int MAX_LINES_IN_DRUM_BANK = 8;
#endif
        const int XPOS = 10;
        const int YPOS = 60;
        const int WIDTH = 1600;
        const int HEIGHT = 800;
        const int BUTTON_GAP = 20;
        const int EXIT_BUTT_X = XPOS + (WIDTH / 2) - (Button.mWidth / 2);
        const int EXIT_BUTT_Y = YPOS + BUTTON_GAP;
        const int DELETE_BUTT_Y = YPOS + HEIGHT - (Button.mBarHeight + BUTTON_GAP);

        public const int TOP_DRUM_LINE_Y = EXIT_BUTT_Y + (Button.mTickBoxSize);
        public const int DRUM_LINESTART_X = XPOS + BUTTON_GAP;
        const int DRUM_LINE_GAP = (Button.mTickBoxSize + BUTTON_GAP);
        const int DRUM_X_GAP = (Button.mTickBoxSize + BUTTON_GAP);
        const int DRUM_X_GAP_THIRD = (Button.mTripletTickBoxSize + (int)(BUTTON_GAP*0.6666f));

        const int NUM_BEATS_LINE = 16;

        public const int DRUM_BEAT_AREA_W = NUM_BEATS_LINE * DRUM_X_GAP;
        public const int DRUM_BEAT_AREA_H = MAX_LINES_IN_DRUM_BANK * DRUM_LINE_GAP;

        const int BASS_BEAT_ACCENT_Y = EXIT_BUTT_Y + (2 * Button.mTickBoxSize + 2 * BUTTON_GAP);
        const int FEEL_GOOD_BOOLS_Y = EXIT_BUTT_Y + (6 * Button.mTickBoxSize + 2 * BUTTON_GAP);

        const int UP_DOWN_NOTE_X = XPOS + WIDTH - BUTTON_GAP - Button.mTickBoxSize;

        //const int START_STOP_NOTE_X = XPOS + WIDTH - 2 * (BUTTON_GAP + Button.mTickBoxSize);

        const int START_STOP_NOTE_X = XPOS + WIDTH - (BUTTON_GAP + Button.mTickBoxSize) + 80; //Top right
        const int START_STOP_NOTE_Y = YPOS + (BUTTON_GAP) - 30; //Top right

        const int BUTT_TEXT_WIDTH = 110;
        const int COPY_X = XPOS + BUTTON_GAP + BUTT_TEXT_WIDTH; //
        const int PASTE_X = COPY_X + (BUTTON_GAP + Button.mTickBoxSize + BUTT_TEXT_WIDTH); //
        const int TRIPLET_X = PASTE_X + (BUTTON_GAP + Button.mTickBoxSize + BUTT_TEXT_WIDTH); //
   
        const int DRUM_BANK_X = START_STOP_NOTE_X - (BUTTON_GAP + Button.mTickBoxSize + 90); //
        const int DRUM_BANK_Y = START_STOP_NOTE_Y; //


        const int PARAM_BAR_YPOS = YPOS + 10; // YPOS + HEIGHT / 2 - (Button.mBarHeight / 2);
        const int PARAM_BAR_XPOS_START = 40;
        const int PARAM_BAR_XPOS_GAP = 40;

        const int TEXT_INFO_X = XPOS + BUTTON_GAP;
        const int TEXT_INFO_Y = EXIT_BUTT_Y;
        const int TEXT_CHORD_INFO_Y = TEXT_INFO_Y + 40;

        const int TEXT_RECT_BACK_X = TEXT_INFO_X;
        const int TEXT_RECT_BACK_Y = TEXT_INFO_Y;
        const int TEXT_RECT_BACK_H = 70;
        const int TEXT_RECT_BACK_W = 600;

        public const int NUM_DRUM_LINES = 6;
        public const int NUM_DRUM_BEATS_FOUR = 16; //to be shared over 4 beats
        public const int NUM_DRUM_BEATS_THIRDS = 24; //to be shared over 4 beats
        public int DRUM_LINE_TEXT_Y_UP_OFFSET = 20;

        public const int GRAY_RECTANGLE_START_X = XPOS;
        public const int GRAY_RECTANGLE_START_Y = YPOS - 40;
        public const int GRAY_RECTANGLE_WIDTH = 1700;

        const int HEIGHT_GRAY_AREA_DRUM = (TOP_DRUM_LINE_Y - GRAY_RECTANGLE_START_Y) + DRUM_LINE_GAP * MAX_LINES_IN_DRUM_BANK;
        const int HEIGHT_BUTTONS_DIVISION = 940 / 8;
        public const int GRAY_RECTANGLE_HEIGHT = HEIGHT_GRAY_AREA_DRUM; // HEIGHT_BUTTONS_DIVISION * MAX_LINES_IN_DRUM_BANK;


        // to map to chordTypeNames array below
        public enum eDrumKitType
        {
            Rock,
            Electronic,
            Bossonova,
            Mixed,
            Latin,
            Weird,
            Etc,
            num_kits,
        }

        /////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////
        /// COPY AND PASTE CLASS vvvvvv
        /////////////////////////////////////////////////////////////////////////////////

        class CPasteData
        {
            public int mBank = 0;
            const int WIDTH = 10;
            const int HEIGHT = 10;
            Rectangle mRectSelect = new Rectangle(0, 0, WIDTH, HEIGHT);
            Rectangle mAreaRectangle = new Rectangle(DrumMachinePopUp.DRUM_LINESTART_X, DrumMachinePopUp.TOP_DRUM_LINE_Y, DRUM_BEAT_AREA_W, DRUM_BEAT_AREA_H);

            public AIDrumRegion.AIDrumSaveData mPasteData;
            public string mJsonDrumPasteData = "";
            bool mbRectChangeWaitForLeftUp = false; //when the area rectangle changes set this true and when the left button up and this true select drum beats in area and clear this

            int mSelectStartBeat = -1;
            int mSelectEndBeat = -1;

            int mSelectStartLine = -1;
            int mSelectEndLine = -1;

            int mDataStartBeat = -1;
            int mDataEndBeat = -1;
            int mDataStartLine = -1;
            int mDataEndLine = -1;
            int mDataBank = 0; //bank in which the copy was made

            public CPasteData()
            {
                Reset();
            }

            public void Reset()
            {
                mSelectStartBeat = -1;
                mSelectEndBeat = -1;
                mSelectStartLine = -1;
                mSelectEndLine = -1;
                mRectSelect = new Rectangle(0, 0, WIDTH, HEIGHT);

                // mJsonDrumPasteData = "";
            }

            public bool HasAreaSelected() { return mSelectStartBeat != -1 && mSelectEndBeat != -1 && mSelectStartLine != -1 && mSelectEndLine != -1; }
            public bool HasDataSubArea() { return mDataStartBeat != -1 && mDataEndBeat != -1 && mDataStartLine != -1 && mDataEndLine != -1; }

            public bool IsLineBeatIndexInRange(int line, int beat, ref Color outCol)
            {
                int lineShowHighlight = line - mBank * MAX_LINES_IN_DRUM_BANK; //when switch between banks the current yellow highlight will adjust around shown beats

                int adjustDataStartLine = mDataStartLine + mDataBank * MAX_LINES_IN_DRUM_BANK;
                int adjustDataEndLine = mDataEndLine + mDataBank * MAX_LINES_IN_DRUM_BANK;
                //line -= mBank * MAX_LINES_IN_DRUM_BANK;
                if (line >= adjustDataStartLine && line <= adjustDataEndLine)
                {
                    if (beat >= mDataStartBeat && beat <= mDataEndBeat)
                    {
                        outCol = Color.Purple;
                        return true;
                    }
                }
                if (lineShowHighlight >= mSelectStartLine && lineShowHighlight <= mSelectEndLine)
                {
                    if (beat >= mSelectStartBeat && beat <= mSelectEndBeat)
                    {
                        outCol = Color.Yellow;
                        return true;
                    }
                }
                outCol = Color.Black;
                return false;
            }

            public void PreparePasteData(AIDrumRegion.AIDrumSaveData mergeData = null)
            {
                mPasteData = JsonConvert.DeserializeObject<AIDrumRegion.AIDrumSaveData>(mJsonDrumPasteData);

                if (mergeData != null && HasDataSubArea())
                {
                    int adjustDataStartLine = mDataStartLine + mDataBank * MAX_LINES_IN_DRUM_BANK;
                    int adjustDataEndLine = mDataEndLine + mDataBank * MAX_LINES_IN_DRUM_BANK;
                    int line = 0;
                    int beat = 0;

                    //Go through the existing data and copy just the data that is outside the paste data region
                    foreach (AIDrumRegion.AIBeatLine bl in mergeData.mBeatLineList)
                    {
                        beat = 0;

                        bool bInsideLinePasteRegion = line >= adjustDataStartLine && line <= adjustDataEndLine;

                        foreach (AIDrumRegion.AIDrumBeat db in bl.mBeatList)
                        {
                            bool bInsideBeatPasteRegion = beat >= mDataStartBeat && beat <= mDataEndBeat;
                            bool bInsidePasteRegion = bInsideBeatPasteRegion && bInsideLinePasteRegion;

                            if (!bInsidePasteRegion)
                            {
                                //We're outside the region now, so adjust the paste data to be same as the passed data
                                AIDrumRegion.AIDrumBeat pasteDb = mPasteData.mBeatLineList[line].mBeatList[beat];
                                pasteDb.CopyBeat(db); //copy what is in the existing data to the paste buffer
                            }

                            beat++;
                        }
                        
                        line++;
                    }
                }
            }

            public bool  CopyData(AIDrumRegion.AIDrumSaveData drumData)
            {
                //if(!HasPasteData())
                //{
                //    return false;
                //}
                //Copy all drum data if no region selected

                mJsonDrumPasteData = JsonConvert.SerializeObject(drumData);

                mDataStartBeat = mSelectStartBeat;
                mDataEndBeat = mSelectEndBeat;
                mDataStartLine = mSelectStartLine;
                mDataEndLine = mSelectEndLine;
                mDataBank = mBank;

                return true;
            }

            public bool Update(MelodyEditorInterface.MEIState state)
            {
                bool bUpdating = false; //prevent any notes being laid downn if true
                PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();

                //mbUndoAvailable = pa.UndoChangeData != null;

                bool bSecondTouch = state.input.SecondHeld;

                int x1 = state.input.X;
                int y1 = state.input.Y;

                int x2 = state.input.X2;
                int y2 = state.input.Y2;

                int startBeat = -1;
                int startLine = -1;

                if (bSecondTouch && mAreaRectangle.Contains(x1, y1) && mAreaRectangle.Contains(x2, y2))
                {
                    int rectx = x2 < x1 ? x2 : x1;
                    int recty = y2 < y1 ? y2 : y1;

                    int rectWidth = Math.Abs(x2 - x1);
                    int rectHeight = Math.Abs(y2 - y1);
                    mRectSelect = new Rectangle(rectx, recty, rectWidth, rectHeight);
                    mbRectChangeWaitForLeftUp = true;

                    mSelectStartBeat = (rectx - mAreaRectangle.X) / DRUM_X_GAP;
                    mSelectStartLine = (recty - mAreaRectangle.Y) / DRUM_LINE_GAP;
                    mSelectEndBeat = mSelectStartBeat + rectWidth / DRUM_X_GAP;
                    mSelectEndLine = mSelectStartLine + rectHeight / DRUM_LINE_GAP;

                    bUpdating = true;
                }
                else if (state.input.LeftUp)
                {
                    //TODO
                }

                if (mbRectChangeWaitForLeftUp && state.input.LeftUp)
                {
                    //TODO
                }

                return bUpdating;
            }

            public bool Active { get; set; } = true;

            public void Draw(SpriteBatch sb)
            {
                if (!Active)
                {
                    return;
                }

                sb.Begin();
                sb.DrawRectangle(mRectSelect, Color.Yellow, 8);

                sb.End();
            }
        }
        /////////////////////////////////////////////////////////////////////////////////
        /// COPY AND PASTE CLASS  ^^^^^
        /////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////

        CPasteData mPasteData = new CPasteData();

        //string[] drumKitNames = new string[(int)eDrumKitType.num_kits] { "Rock", "Electronic" };
        string[] drumKitNames = new string[(int)eDrumKitType.num_kits] { "Rock", "Electronic", "Bossonova", "Mixed", "Latin", "Weird", "Etc" };
        List<ParamButtonBar> mParamBars = new List<ParamButtonBar>();
        ParamButtonBar mDrumKitParamBar;


        public DrumMachinePopUp()
        {
            //TODO temp
            //mCurrentRegion = new AIDrumRegion();
        }

        AIDrumRegion mCurrentRegion = null;
        bool mbCurrentRegionHasNotes = false;

        public bool Active { get; set; } = false;

        int mBank = 0; //probably just two ever!
        int mNumberBanks = 2; //derived from the number of lines in kit

        Button mExitButton;
        Button mDeleteButton;
        Button mStartStopPlayback;
        Button mDrumBank;

        Button mCopyButt;
        Button mPasteButt;

        Button mTriplet;

        List<Button> mControlButtons = new List<Button>();

        //Button mUpNote;
        //Button mDownNote;
        //Button mStartStop;
        //Button mShiftLeft;

        Rectangle mPopUpRect = new Rectangle(XPOS, YPOS, WIDTH, HEIGHT);
        SpriteFont mFont;
        SpriteFont mDrumLineFont;

        Rectangle mFirst4SurroundRect;
        Rectangle mSecond4SurroundRect;
        Rectangle mThird4SurroundRect;
        Rectangle mFourth4SurroundRect;
        const int mSurroundRectThickness = 12;

        public Rectangle mGrayOverLayRectangle;
        public Rectangle mPlayHeadRectangle;

        List<DrumLine> mDrumLines = new List<DrumLine>();

        int CacheLoopStart = 0;
        int CacheLoopEnd = 0;

        class DrumBeat
        {
            public bool Active = false;
            public Button mButton = null;
            //public Note mNote = null;
            public int mLineNum = -1;
            public int mBeatNum = -1;
            public int mVelocityLevel = 0;
            DrumMachinePopUp mDMParent;
            public Rectangle mVelLevelMid;
            public Rectangle mVelLevelHi;

            public DrumBeat(Vector2 pos, SpriteFont font, DrumMachinePopUp parent, int linenum, int beatnum, bool bThirdBeat = false)
            {
                mDMParent = parent;
                mBeatNum = beatnum;
                mLineNum = linenum;

                mButton = new Button(pos, "PlainWhite");
                mButton.mFont = font;
                mButton.mTexture = Button.mStaticDefaultTexture;

                int rectMidWidth = Button.mTickBoxSize / 2;
                int rectHiWidth = Button.mTickBoxSize / 4;
                int rectMidOffset = rectMidWidth / 2;
                int rectHiOffset = rectMidOffset + rectHiWidth / 2;

                int rectMidWidthX = bThirdBeat ? (int)(rectMidWidth * 0.666f) : rectMidWidth;
                int rectHiWidthX = bThirdBeat ? (int)(rectHiWidth * 0.666f) : rectHiWidth;
                int rectMidOffsetX = rectMidWidthX / 2;
                int rectHiOffsetX = rectMidOffsetX + rectHiWidthX / 2;

                mVelLevelMid = new Rectangle(rectMidOffsetX + (int)pos.X, rectMidOffset + (int)pos.Y, rectMidWidthX, rectMidWidth);
                mVelLevelHi = new Rectangle(rectHiOffsetX + (int)pos.X, rectHiOffset + (int)pos.Y, rectHiWidthX, rectHiWidth);

                mButton.mType = Button.Type.Beat;
                mButton.mbTripletMode = bThirdBeat;
                mButton.ColOff = Color.DarkGray * 0.4f;
                mButton.ColOn = Color.DarkGreen * 0.4f;
                //mButton.SetCallbacks(parent.BeatOnCB, parent.BeatOffCB);
                mButton.ClickCB = parent.BeatCB;
                mButton.DoubleClickCB = DoubleClickCB;
                mButton.SlideLeftRightCB = SlideLeftRightCB;
            }

            public bool DoubleClickCB(MelodyEditorInterface.MEIState state)
            {
                mButton.mbOn = true; //Always set on, double cliks will leave the beat on always and do this before the call to DoubleClickFromBeatCB below which will refresh to this being on.

                if (mDMParent.DoubleClickFromBeatCB(state, mLineNum, mBeatNum))
                {
                }
                return false;
            }

            public bool SlideLeftRightCB(MelodyEditorInterface.MEIState state, int stepValue)
            {
                if(mButton.mbOn)
                {
                    if (stepValue == 1)
                    {
                        mButton.ColHeld = Color.Red;
                    }
                    else if (stepValue == 2)
                    {
                        mButton.ColHeld = Color.Cyan;
                    }
                    else if (stepValue == -1)
                    {
                        mButton.ColHeld = Color.Pink;
                    }
                    else if (stepValue == -2)
                    {
                        mButton.ColHeld = Color.Magenta;
                    }
                    else
                    {
                        mButton.ColHeld = Color.DarkGreen;
                    }

                    if (mDMParent.OffsetLevelChangeFromBeatCB(state, mLineNum, mBeatNum, stepValue))
                    {
                    }
                }

                return false;
            }

            public void Draw(SpriteBatch sb)
            {
                if(Active)
                {
                    mButton.Draw(sb);
                }
            }
            public void Update(MelodyEditorInterface.MEIState state)
            {
                if (Active)
                {
                    mButton.Update(state);
                }
            }
        }

        class DrumLine
        {
            public bool Active = false;
            public int mBankNumber = 0;
            public Vector2 mvStart;
            //TODO saveable data
            public string mName = "Test";
            public List<DrumBeat> mDrumLine = new List<DrumBeat>();
        }

        public void Init()
        {
            mNumberBanks = AIDrumRegion.ROCK_KIT.Length / MAX_LINES_IN_DRUM_BANK;

            int remainder = AIDrumRegion.ROCK_KIT.Length % MAX_LINES_IN_DRUM_BANK;
            if (remainder>0)
            {
                mNumberBanks += 1;
            }
            //InitDrumLines();
            mGrayOverLayRectangle = new Rectangle(GRAY_RECTANGLE_START_X, GRAY_RECTANGLE_START_Y, GRAY_RECTANGLE_WIDTH, GRAY_RECTANGLE_HEIGHT);

            mPlayHeadRectangle = new Rectangle(GRAY_RECTANGLE_START_X, GRAY_RECTANGLE_START_Y, 1, 1);

            mFirst4SurroundRect = new Rectangle(GRAY_RECTANGLE_START_X, GRAY_RECTANGLE_START_Y, 1, 1);
            mSecond4SurroundRect = new Rectangle(GRAY_RECTANGLE_START_X, GRAY_RECTANGLE_START_Y, 1, 1);
            mThird4SurroundRect = new Rectangle(GRAY_RECTANGLE_START_X, GRAY_RECTANGLE_START_Y, 1, 1);
            mFourth4SurroundRect = new Rectangle(GRAY_RECTANGLE_START_X, GRAY_RECTANGLE_START_Y, 1, 1);

            mExitButton = new Button(new Vector2(EXIT_BUTT_X, EXIT_BUTT_Y), "PlainWhite");
            mExitButton.ButtonText = "Exit";
            mExitButton.mMask = MelodyEditorInterface.UIButtonMask.UIBM_DrumMachine;
            mExitButton.mType = Button.Type.Bar;
            mExitButton.ColOn = Color.BlanchedAlmond;
            mExitButton.ColOff = Color.BlanchedAlmond;
            mExitButton.ButtonTextColour = Color.Black;
            mExitButton.ClickCB = ExitButtonCB;

            mCopyButt = new Button(new Vector2(COPY_X, DRUM_BANK_Y), "PlainWhite");
            mCopyButt.mType = Button.Type.Tick;
            mCopyButt.ColOff = Color.DarkBlue * 0.6f;
            mCopyButt.ColOn = Color.LightBlue * 0.6f;
            mCopyButt.ColHeld = Color.Black;
            mCopyButt.ButtonTextLeft = "Copy";
            mCopyButt.ButtonTextOff = "Copy";
            mCopyButt.ButtonTextColour = Color.Black;
            mCopyButt.mMask = MelodyEditorInterface.UIButtonMask.UIBM_DrumMachine;

            mPasteButt = new Button(new Vector2(PASTE_X, DRUM_BANK_Y), "PlainWhite");
            mPasteButt.mType = Button.Type.Tick;
            mPasteButt.ColOff = Color.DarkGreen * 0.6f;
            mPasteButt.ColOn = Color.LightGreen * 0.6f;
            mPasteButt.ColHeld = Color.Black;
            mPasteButt.ButtonTextLeft = "Paste";
            mPasteButt.ButtonTextOff = "Paste";
            mPasteButt.ButtonTextColour = Color.Black;
            mPasteButt.mMask = MelodyEditorInterface.UIButtonMask.UIBM_DrumMachine;

            mTriplet = new Button(new Vector2(TRIPLET_X, DRUM_BANK_Y), "PlainWhite");
            mTriplet.mType = Button.Type.Tick;
            mTriplet.ColOff = Color.DarkGreen * 0.6f;
            mTriplet.ColOn = Color.LightGreen * 0.6f;
            mTriplet.ColHeld = Color.Black;
            mTriplet.ButtonTextLeft = "16";
            mTriplet.ButtonTextOff = "24";
            mTriplet.ButtonTextColour = Color.Black;
            mTriplet.mMask = MelodyEditorInterface.UIButtonMask.UIBM_DrumMachine;

            mDrumBank = new Button(new Vector2(DRUM_BANK_X, DRUM_BANK_Y), "PlainWhite");
            mDrumBank.mType = Button.Type.Tick;
            mDrumBank.ColOff = Color.DarkOrange * 0.6f;
            mDrumBank.ColOn = Color.Orange * 0.6f;
            mDrumBank.ButtonTextLeft = "Bank 0";
            mDrumBank.ButtonTextOff = "Bank 0";
            mDrumBank.ButtonTextColour = Color.Black;
            mDrumBank.mMask = MelodyEditorInterface.UIButtonMask.UIBM_DrumMachine;

            mStartStopPlayback = new Button(new Vector2(START_STOP_NOTE_X, START_STOP_NOTE_Y), "PlainWhite");
            mStartStopPlayback.mType = Button.Type.Tick;
            mStartStopPlayback.ColOff = Color.Red * 0.6f;
            mStartStopPlayback.ColOn = Color.Green * 0.6f;
            mStartStopPlayback.ButtonTextLeft = "Start";
            mStartStopPlayback.ButtonTextOff = "Stop";
            mStartStopPlayback.ButtonTextColour = Color.Black;
            mStartStopPlayback.mMask = MelodyEditorInterface.UIButtonMask.UIBM_DrumMachine;

            mControlButtons.Add(mCopyButt);
            mControlButtons.Add(mPasteButt);
            mControlButtons.Add(mTriplet);

            mControlButtons.Add(mExitButton);
            mControlButtons.Add(mStartStopPlayback);
            mControlButtons.Add(mDrumBank);

            mPlayHeadRectangle = new Rectangle(DRUM_LINESTART_X, TOP_DRUM_LINE_Y, Button.mTickBoxSize, MAX_LINES_IN_DRUM_BANK * DRUM_LINE_GAP);

            mFirst4SurroundRect = new Rectangle(DRUM_LINESTART_X, TOP_DRUM_LINE_Y, DRUM_LINE_GAP * 4, MAX_LINES_IN_DRUM_BANK * DRUM_LINE_GAP);
            mSecond4SurroundRect = new Rectangle(DRUM_LINESTART_X + DRUM_LINE_GAP * 8, TOP_DRUM_LINE_Y, DRUM_LINE_GAP * 4, MAX_LINES_IN_DRUM_BANK * DRUM_LINE_GAP);
            mThird4SurroundRect = new Rectangle(DRUM_LINESTART_X + DRUM_LINE_GAP * 8, TOP_DRUM_LINE_Y, DRUM_LINE_GAP * 4, MAX_LINES_IN_DRUM_BANK * DRUM_LINE_GAP);
            mFourth4SurroundRect = new Rectangle(DRUM_LINESTART_X, TOP_DRUM_LINE_Y, DRUM_LINE_GAP * 4, MAX_LINES_IN_DRUM_BANK * DRUM_LINE_GAP);
            //mDrumKitParamBar = SetUpPopupParamBar(drumKitNames);
        }

        //void InitDrumLines(MelodyEditorInterface.MEIState state) //TODO may not need state but for now need for playarea
        //{
        //    //GET RID PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();

        //    int drumLineY = TOP_DRUM_LINE_Y;
        //    int drumLineX = DRUM_LINESTART_X;
        //    mDrumLines = new List<DrumLine>();

        //    for (int linenum=0; linenum < mCurrentRegion.mDrumKit.Length; linenum++)
        //    {
        //        drumLineY = TOP_DRUM_LINE_Y + DRUM_LINE_GAP * (linenum  % MAX_LINES_IN_DRUM_BANK);
        //        drumLineX = DRUM_LINESTART_X;

        //        Vector2 vdllpos = new Vector2(drumLineX, drumLineY- DRUM_LINE_TEXT_Y_UP_OFFSET);
        //        DrumLine dl = new DrumLine();
        //        List<DrumBeat> drumLineList = dl.mDrumLine;
        //        NoteLine nl = mCurrentRegion.GetPlayArea().GetNoteLineAtNoteNumber(mCurrentRegion.mDrumKit[linenum]);
        //        dl.mName = nl.Text;
        //        dl.mvStart = vdllpos;

        //        dl.mBankNumber = linenum < MAX_LINES_IN_DRUM_BANK ? 0 : 1;

        //        //get rid dl.mNoteLine = nl;

        //        for (int beatnum = 0; beatnum < NUM_DRUM_BEATS; beatnum++)
        //        {
        //            DrumBeat beatTick;

        //            beatTick = new DrumBeat(new Vector2(drumLineX, drumLineY), mFont, this, linenum, beatnum);

        //            //TODO cant wait for LoadCOntent shenanigans when we open screen and create new set of buttons so check these are always ready here - should be
        //            //beatTick.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Tick;

        //            drumLineX += DRUM_LINE_X_GAP;

        //            drumLineList.Add(beatTick);
        //        }

        //        mDrumLines.Add(dl);
        //    }
        //}

        void InitDrumLines() //Create the drum beat lines with all buttons for max number of banks and hide them and wait for actual drumlines to be set by selected kits
        {
            int drumLineY = TOP_DRUM_LINE_Y;
            int drumLineX = DRUM_LINESTART_X;
            mDrumLines = new List<DrumLine>();
            int drumLineGap = DRUM_X_GAP;

            int numDrumBeats = NUM_DRUM_BEATS_FOUR;
            if (mCurrentRegion.mDrumData.mbThirdBeat)
            {
                drumLineGap = DRUM_X_GAP_THIRD;
                numDrumBeats = NUM_DRUM_BEATS_THIRDS;
            }

            int numLines = MAX_LINES_IN_DRUM_BANK * mNumberBanks;

            for (int linenum = 0; linenum < numLines; linenum++)
            {
                drumLineY = TOP_DRUM_LINE_Y + DRUM_LINE_GAP * (linenum % MAX_LINES_IN_DRUM_BANK);
                drumLineX = DRUM_LINESTART_X;

                Vector2 vdllpos = new Vector2(drumLineX, drumLineY - DRUM_LINE_TEXT_Y_UP_OFFSET);
                DrumLine dl = new DrumLine();
                List<DrumBeat> drumLineList = dl.mDrumLine;
                dl.mvStart = vdllpos;

                dl.mBankNumber = linenum / MAX_LINES_IN_DRUM_BANK;

                for (int beatnum = 0; beatnum < numDrumBeats; beatnum++)
                {
                    DrumBeat beatTick;
                    beatTick = new DrumBeat(new Vector2(drumLineX, drumLineY), mFont, this, linenum, beatnum, mCurrentRegion.mDrumData.mbThirdBeat);
                    beatTick.mButton.mbOn = false;
                    drumLineX += drumLineGap;
                    drumLineList.Add(beatTick);
                }
                mDrumLines.Add(dl);
            }
        }

        void UpdateDrumLinesFromRegion()
        {
            System.Diagnostics.Debug.Assert(mCurrentRegion != null, string.Format("mCurrentRegion is NULL!!!"));

            if (mCurrentRegion == null)
            {
                return;
            }
            int numLines = MAX_LINES_IN_DRUM_BANK * mNumberBanks;

            for (int linenum = 0; linenum < numLines; linenum++)
            {
                DrumLine dl = mDrumLines[linenum];
                NoteLine nl = mCurrentRegion.GetNoteLine(linenum);
                int numDrumBeats = NUM_DRUM_BEATS_FOUR;
                if (mCurrentRegion.mDrumData.mbThirdBeat)
                {
                    numDrumBeats = NUM_DRUM_BEATS_THIRDS;
                }

                if (nl!=null)
                {
                    dl.Active = true;
                    dl.mName = nl.Text;
                    for (int beatnum = 0; beatnum < numDrumBeats; beatnum++)
                    {
                        DrumBeat beatTick = dl.mDrumLine[beatnum];
                        beatTick.Active = true;
                        beatTick.mButton.mbOn = mCurrentRegion.mDrumData.mBeatLineList[linenum].mBeatList[beatnum].mbBeatOn;

                        beatTick.mButton.SetSlideValue(  mCurrentRegion.mDrumData.mBeatLineList[linenum].mBeatList[beatnum].mOffsetLevel); //TODO check if has call back for slide or do anyway all the time? The button can handle that
                    }
                }
                else
                {
                    dl.Active = false;
                    dl.mName = "--";
                    for (int beatnum = 0; beatnum < numDrumBeats; beatnum++)
                    {
                        DrumBeat beatTick = dl.mDrumLine[beatnum];
                        beatTick.Active = false;
                    }
                }

            }

            int firstRectStart = mDrumLines[0].mDrumLine[0].mButton.Rectangle.X;
            int secondRectStart = mCurrentRegion.mDrumData.mbThirdBeat ? mDrumLines[0].mDrumLine[6].mButton.Rectangle.X : mDrumLines[0].mDrumLine[4].mButton.Rectangle.X;
            int thirdRectStart = mCurrentRegion.mDrumData.mbThirdBeat? mDrumLines[0].mDrumLine[12].mButton.Rectangle.X : mDrumLines[0].mDrumLine[8].mButton.Rectangle.X;
            int fourtheRectStart = mCurrentRegion.mDrumData.mbThirdBeat ? mDrumLines[0].mDrumLine[18].mButton.Rectangle.X : mDrumLines[0].mDrumLine[12].mButton.Rectangle.X;

            int firstEnd = mDrumLines[0].mDrumLine[3].mButton.Rectangle.X + mDrumLines[0].mDrumLine[3].mButton.Rectangle.Width;
            //int secondEnd = mDrumLines[0].mDrumLine[7].mButton.Rectangle.X + mDrumLines[0].mDrumLine[7].mButton.Rectangle.Width;
            //int thirdEnd = mDrumLines[0].mDrumLine[11].mButton.Rectangle.X + mDrumLines[0].mDrumLine[11].mButton.Rectangle.Width;
            //int fourthEnd = mDrumLines[0].mDrumLine[15].mButton.Rectangle.X + mDrumLines[0].mDrumLine[15].mButton.Rectangle.Width;

            if (mCurrentRegion.mDrumData.mbThirdBeat)
            {
                firstEnd = mDrumLines[0].mDrumLine[5].mButton.Rectangle.X + mDrumLines[0].mDrumLine[5].mButton.Rectangle.Width;
                //secondEnd = mDrumLines[0].mDrumLine[11].mButton.Rectangle.X + mDrumLines[0].mDrumLine[11].mButton.Rectangle.Width;
                //thirdEnd = mDrumLines[0].mDrumLine[17].mButton.Rectangle.X + mDrumLines[0].mDrumLine[17].mButton.Rectangle.Width;
                //fourthEnd = mDrumLines[0].mDrumLine[23].mButton.Rectangle.X + mDrumLines[0].mDrumLine[23].mButton.Rectangle.Width;
            }
            const int maxBeatLine = MAX_LINES_IN_DRUM_BANK - 1;

            int widthRect = firstEnd - firstRectStart + mSurroundRectThickness;
            int heightRect = (mDrumLines[maxBeatLine].mDrumLine[0].mButton.Rectangle.Y + mDrumLines[maxBeatLine].mDrumLine[0].mButton.Rectangle.Height) - mDrumLines[0].mDrumLine[0].mButton.Rectangle.Y + mSurroundRectThickness;

            mFirst4SurroundRect = new Rectangle(firstRectStart, TOP_DRUM_LINE_Y- mSurroundRectThickness, widthRect, heightRect);
            mSecond4SurroundRect = new Rectangle(secondRectStart, TOP_DRUM_LINE_Y - mSurroundRectThickness, widthRect, heightRect);
            mThird4SurroundRect = new Rectangle(thirdRectStart, TOP_DRUM_LINE_Y- mSurroundRectThickness, widthRect, heightRect);
            mFourth4SurroundRect = new Rectangle(fourtheRectStart, TOP_DRUM_LINE_Y - mSurroundRectThickness, widthRect, heightRect);

        }


        void InitDrumLinesFromSave(MelodyEditorInterface.MEIState state) //TODO may not need state but for now need for playarea
        {
            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();

            int drumLineY = TOP_DRUM_LINE_Y;
            int drumLineX = DRUM_LINESTART_X;
            mDrumLines = new List<DrumLine>();

            for (int linenum = 0; linenum < mCurrentRegion.mDrumData.mBeatLineList.Count; linenum++)
            {
                drumLineY = TOP_DRUM_LINE_Y + DRUM_LINE_GAP * (linenum % MAX_LINES_IN_DRUM_BANK);
                drumLineX = DRUM_LINESTART_X;

                Vector2 vdllpos = new Vector2(drumLineX, drumLineY- DRUM_LINE_TEXT_Y_UP_OFFSET);
                DrumLine dl = new DrumLine();
                List<DrumBeat> drumLineList = dl.mDrumLine;
                NoteLine nl = mCurrentRegion.GetNoteLine(linenum);
                dl.mName = nl.Text;
                dl.mvStart = vdllpos;

                dl.mBankNumber = linenum / MAX_LINES_IN_DRUM_BANK;

                //get rid dl.mNoteLine = nl;
                int numDrumBeats = NUM_DRUM_BEATS_FOUR;
                if (mCurrentRegion.mDrumData.mbThirdBeat)
                {
                    numDrumBeats = NUM_DRUM_BEATS_THIRDS;
                }

                for (int beatnum = 0; beatnum < numDrumBeats; beatnum++)
                {
                    DrumBeat beatTick;

                    beatTick = new DrumBeat(new Vector2(drumLineX, drumLineY), mFont, this, linenum, beatnum, mCurrentRegion.mDrumData.mbThirdBeat);

                    beatTick.mButton.mbOn = mCurrentRegion.mDrumData.mBeatLineList[linenum].mBeatList[beatnum].mbBeatOn;

                    //TODO cant wait for LoadCOntent shenanigans when we open screen and create new set of buttons so check these are always ready here - should be
                    //beatTick.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Tick;

                    drumLineX += DRUM_X_GAP;

                    drumLineList.Add(beatTick);
                }

                mDrumLines.Add(dl);
            }
        }

        void InitSaveFromDrumLines() //TODO may not need state but for now need for playarea
        {
            int drumLineY = TOP_DRUM_LINE_Y;
            int drumLineX = DRUM_LINESTART_X;
            int numDrumBeats = NUM_DRUM_BEATS_FOUR;
            if (mCurrentRegion.mDrumData.mbThirdBeat)
            {
                numDrumBeats = NUM_DRUM_BEATS_THIRDS;
            }

            if (mCurrentRegion.mDrumData.mBeatLineList.Count==0)
            {
                mCurrentRegion.mDrumData.CreateLinesIfNone(AIDrumRegion.ROCK_KIT, numDrumBeats);
            }
            for (int linenum = 0; linenum < mDrumLines.Count; linenum++)
            {
                //mCurrentRegion.mSaveData.mBeatLineList.Count

                DrumLine dl = mDrumLines[linenum];
                if(dl.Active)
                {
                    for (int beatnum = 0; beatnum < numDrumBeats; beatnum++)
                    {
                        DrumBeat beatTick = dl.mDrumLine[beatnum];

                        mCurrentRegion.mDrumData.mBeatLineList[linenum].mBeatList[beatnum].mbBeatOn = beatTick.mButton.mbOn;

                        //TODO cant wait for LoadCOntent shenanigans when we open screen and create new set of buttons so check these are always ready here - should be
                        //beatTick.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Tick;
                    }
                }
            }
        }

        void SwitchBank()
        {
            mBank++;
            mBank = mBank % mNumberBanks;
            mPasteData.mBank = mBank;
        }

        void CopyData()
        {
            InitSaveFromDrumLines();
            mPasteData.CopyData(mCurrentRegion.mDrumData);
        }

        void PasteData()
        {
            if (mPasteData.mJsonDrumPasteData =="")
            {
                //no data!
                return;
            }

            InitSaveFromDrumLines();
            mPasteData.PreparePasteData(mCurrentRegion.mDrumData);

            mCurrentRegion.ClearAllNotes(); //we're pasting over this so might as well clear current notes in actual play area

            //Going to paste here so make sure the data (from wherever it was) has beat start and length set this current region
            float beatPos = mCurrentRegion.BeatPos;
            float beatEndPos = mCurrentRegion.BeatEndPos;
            mPasteData.mPasteData.BeatPos = beatPos;
            //BeatEndPos = mDrumData.BeatPos + BeatLength;

            mCurrentRegion.LoadSaveData(mPasteData.mPasteData);
            //InitSaveFromDrumLines();

            InitDrumLines();
            UpdateDrumLinesFromRegion();
            RefreshNotesToCurrent();
        }

        bool ExitButtonCB(MelodyEditorInterface.MEIState state)
        {
            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();
            //put the play area loop back to how it was before we enetered this UI
            pa.StartBar = CacheLoopStart;
            pa.EndBar = CacheLoopEnd;
            state.PlayAreaChanged(true); //so square cursor is updated to show loop region correctly back to how it was

            mPasteData.Reset();

            Stop(state);
            return true;
        }

        static bool DOUBLE_CLICK_DEBUG = false;
        static bool SLIDE_VALUE_CHANGE_DEBUG = false;
        //public bool BeatOnCB(MelodyEditorInterface.MEIState state)
        //{
        //    bool bChanged = RefreshNotesToCurrent();
        //    state.PlayAreaChanged(bChanged); //so square cursor is updated
        //    return true;
        //}
        //public bool BeatOffCB(MelodyEditorInterface.MEIState state)
        //{
        //    bool bChanged = RefreshNotesToCurrent();
        //    state.PlayAreaChanged(bChanged); //so square cursor is updated
        //    return true;
        //}
        public bool BeatCB(MelodyEditorInterface.MEIState state)
        {
            if(DOUBLE_CLICK_DEBUG)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("BEAT CALL BACK "));
            }
            bool bChanged = RefreshNotesToCurrent();
            state.PlayAreaChanged(bChanged); //so square cursor is updated

            DOUBLE_CLICK_DEBUG = false;
            return true;
        }

        public bool DoubleClickFromBeatCB(MelodyEditorInterface.MEIState state, int linenum, int beatnum)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("DOUBLE CLICK CALLBACK {0} {1}", linenum,  beatnum));

            DOUBLE_CLICK_DEBUG = true;

            mCurrentRegion.CycleVelocityLevel(linenum, beatnum);
            bool bChanged = RefreshNotesToCurrent();
            state.PlayAreaChanged(bChanged); //so square cursor is updated
            return true;
        }

        public bool OffsetLevelChangeFromBeatCB(MelodyEditorInterface.MEIState state, int linenum, int beatnum, int newOffsetLevel)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("OFFSET_LEVEL_CHANGE_DEBUG CLICK CALLBACK Line {0} Beat {1}. newOffsetLevel {2}", linenum, beatnum, newOffsetLevel));

            SLIDE_VALUE_CHANGE_DEBUG = true;

            mCurrentRegion.SetOffsetLevel(linenum, beatnum, newOffsetLevel);
            bool bChanged = RefreshNotesToCurrent();
            state.PlayAreaChanged(bChanged); //so square cursor is updated
            return true;
        }

        bool RefreshNotesToCurrent()
        {
            System.Diagnostics.Debug.Assert(mCurrentRegion!=null, string.Format("mCurrentRegion is NULL!!!"));

            mbCurrentRegionHasNotes = false;
            if (mCurrentRegion==null)
            {
                return false;
            }

            bool bEdited = false;

            foreach (DrumLine dll in mDrumLines)
            {
                foreach (DrumBeat but in dll.mDrumLine)
                {
                    if(but.Active)
                    {
                        if (but.mButton.mbOn)
                        {
                            bEdited |= mCurrentRegion.SetBeat(but.mLineNum, but.mBeatNum);
                            mbCurrentRegionHasNotes = true;
                        }
                        else
                        {
                            bEdited |= mCurrentRegion.ClearBeat(but.mLineNum, but.mBeatNum);
                        }

                        but.mVelocityLevel = mCurrentRegion.GetBeatVelocityLevelIndex(but.mLineNum, but.mBeatNum);
                    }
                }
            }

            return bEdited;
        }

        public void Show(AIDrumRegion region, MelodyEditorInterface.MEIState state)
        {
            mCurrentRegion = region;
            state.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_DrumMachine; //blank out all other buttons
            Start(state);
            //RefreshPopupToRegion();
        }

        public void Start(MelodyEditorInterface.MEIState state)
        {
            if(mCurrentRegion==null)
            {
                //TODO can hit this when hitting drum button in main screen and no region edited yet - is that button ever needed? does it just go to last edited?
                return;
            }

            if (mCurrentRegion.mDrumData.mBeatLineList.Count > 0)
            {
                InitDrumLines();
                UpdateDrumLinesFromRegion();

                //InitDrumLinesFromSave(state);
                RefreshNotesToCurrent();
            }
            //else
            //{
            //    InitDrumLines(state);
            //}
            if(mCurrentRegion.mDrumData.mbThirdBeat)
            {
                mTriplet.mbOn = true;
            }
            else
            {
                mTriplet.mbOn = false;
            }

            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();
            CacheLoopStart = pa.StartBar;
            CacheLoopEnd = pa.EndBar;

            mCurrentRegion.SetPlayAreaLoopRange();
            state.PlayAreaChanged(true); //so square cursor is updated

            state.BlockBigGreyRectangle = true;
            state.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_DrumMachine; //set to only this button updates
            Active = true;
        }

        public void Stop(MelodyEditorInterface.MEIState state)
        {
            InitSaveFromDrumLines();
            state.BlockBigGreyRectangle = false;
            state.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_ALL; //allow all
            Active = false;
        }

        public void LoadContent(ContentManager contentMan, SpriteFont font, SpriteFont smallfont)
        {
            mFont = font;
            mDrumLineFont = smallfont;
            //foreach (DrumLine dll in mDrumLines)
            //{
            //    foreach (Button but in dll.mDrumLine)
            //    {
            //        but.LoadContent(contentMan, font);
            //    }
            //}
            foreach (Button ab in mControlButtons)
            {
                ab.LoadContent(contentMan, font);
            }
            foreach (ParamButtonBar pbb in mParamBars)
            {
                pbb.LoadContent(contentMan, font);
            }
        }

        public bool Update(MelodyEditorInterface.MEIState state)
        {
            if (!Active)
            {
                return false;
            }

            if(mPasteData.Update( state))
            {
                return false;
            }

            bool bBarShowing = false;
            //PARAM BAR
            ParamButtonBar beenSetInBarMode = null;
            foreach (ParamButtonBar pbb in mParamBars)
            {
                bBarShowing |= pbb.mBarMode;

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
            if(bBarShowing)
            {
                return false; //dont allow touching anything else while a bar showing e.g. beats that are below and underneath the bar drop down
            }

            if (state.Playing)
            {
                float currentBeat = state.fMainCurrentBeat; //assume this regions loop range is always set

                int drumBeatDivision = (int)(currentBeat / mCurrentRegion.mDrumData.mWidth);
                int drumLineGap = DRUM_X_GAP;
                int numDrumBeats = NUM_DRUM_BEATS_FOUR;
                mPlayHeadRectangle.Width = Button.mTickBoxSize;
                if (mCurrentRegion.mDrumData.mbThirdBeat)
                {
                    mPlayHeadRectangle.Width = Button.mTripletTickBoxSize;
                    numDrumBeats = NUM_DRUM_BEATS_THIRDS;
                    drumLineGap = DRUM_X_GAP_THIRD;
                }
                drumBeatDivision = drumBeatDivision % numDrumBeats;

                mPlayHeadRectangle.X = DRUM_LINESTART_X + drumBeatDivision * drumLineGap;
            }

            foreach (DrumLine dll in mDrumLines)
            {
                if(dll.mBankNumber == mBank)
                {
                    foreach (DrumBeat but in dll.mDrumLine)
                    {
                        but.Update(state);
                    }
                }
            }

            //foreach (Button ab in mControlButtons)
            //{
            //    if(mStartStopPlayback!=ab)
            //    {
            //        ab.Update(state);
            //        ab.mbOn = false;
            //    }
            //}

            if (mExitButton.Update(state))
            {
                mExitButton.mbOn = true;
            }

            if (mStartStopPlayback.Update(state))
            {
                state.mMaskToSetButton = MelodyEditorInterface.UIButtonMask.UIBM_Start;
            }

            if (mDrumBank.Update(state))
            {
                SwitchBank();
                mDrumBank.mbOn = true;
                mDrumBank.ButtonTextLeft = string.Format("Bank {0}", mBank);
                mDrumBank.ButtonTextOff = string.Format("Bank {0}", mBank);
            }

            if (mCopyButt.Update(state))
            {
                CopyData();
                mCopyButt.mbOn = true;
            }
            if (mPasteButt.Update(state))
            {
                PasteData();
                mPasteButt.mbOn = true;
            }

            if (!mbCurrentRegionHasNotes && mTriplet.Update(state))
            {
                mCurrentRegion.ResetToBeatArrangement(mTriplet.mbOn);
                InitDrumLines();
                UpdateDrumLinesFromRegion();
                RefreshNotesToCurrent();
            }

            return false;
        }

        //PARAM BAR

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

            //if(button.mGED.mValue!=mCurrentRegion.KitIndex)
            //{
            //    System.Diagnostics.Debug.WriteLine(string.Format("Set new kit value {0} previous {1}", button.mGED.mValue, mCurrentRegion.KitIndex));
            //    mCurrentRegion.ClearAllNotes(); //call this before the SetNewDrumIndex call so notes are removed from NoteLines
            //    mCurrentRegion.SetNewDrumIndex(button.mGED.mValue);
            //    UpdateDrumLinesFromRegion();
            //    bool bChanged = RefreshNotesToCurrent();
            //    state.PlayAreaChanged(bChanged); //so square cursor is updated
            //}
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

            ParamButtonBar pbb = new ParamButtonBar(aiGED, ParamButtonBar.DROP_TYPE.DOWN);

            AddParamBar(pbb);

            return pbb;
        }


        public void Draw(SpriteBatch sb)
        {
            if(!Active)
            {
                return;
            }
            sb.Begin();
            sb.FillRectangle(mGrayOverLayRectangle, Color.Gray * .7f);
            sb.DrawRectangle(mGrayOverLayRectangle, Color.Black, 10);

            const float surroundRectAlpha = 0.33f;
            Color surroundCol = Color.DarkGoldenrod;
            sb.DrawRectangle(mFirst4SurroundRect, surroundCol * surroundRectAlpha, mSurroundRectThickness);
            sb.DrawRectangle(mSecond4SurroundRect, surroundCol * surroundRectAlpha, mSurroundRectThickness);
            sb.DrawRectangle(mThird4SurroundRect, surroundCol * surroundRectAlpha, mSurroundRectThickness);
            sb.DrawRectangle(mFourth4SurroundRect, surroundCol * surroundRectAlpha, mSurroundRectThickness);

            sb.FillRectangle(mPlayHeadRectangle, Color.Purple * .35f);
            sb.DrawRectangle(mStartStopPlayback.Rectangle, Color.Black);
            sb.End();

 
            foreach (Button ab in mControlButtons)
            {
                ab.Draw(sb);
            }

            sb.Begin();
            sb.DrawRectangle(mStartStopPlayback.Rectangle, Color.Black);
            sb.End();

            foreach (DrumLine dll in mDrumLines)
            {
                if (dll.Active && dll.mBankNumber == mBank)
                {
                    foreach (DrumBeat but in dll.mDrumLine)
                    {
                        but.Draw(sb);
                    }
                }
            }

            sb.Begin();
            int line = 0;
            int beat = 0;
            foreach (DrumLine dll in mDrumLines)
            {
                beat = 0;
                if (dll.Active && dll.mBankNumber == mBank)
                {
                    if (mDrumLineFont != null)
                    {
                        sb.DrawString(mDrumLineFont, dll.mName, dll.mvStart, Color.Black);
                    }
                    foreach (DrumBeat but in dll.mDrumLine)
                    {
                        Color buttCol = Color.Black;
                        float thick = 1.0f;

                        if(mPasteData.IsLineBeatIndexInRange(line,beat, ref buttCol))
                        {
                            thick = 3.0f;
                        }
                        sb.DrawRectangle(but.mButton.Rectangle, buttCol, thick);

                        if(but.mButton.mbOn)
                        {
                            if (but.mVelocityLevel > 0)
                            {
                                sb.DrawRectangle(but.mVelLevelMid, Color.Black, 3);
                                if (but.mVelocityLevel > 1)
                                {
                                    sb.DrawRectangle(but.mVelLevelHi, Color.Black, 3);
                                }
                            }
                        }

                        beat++;
                    }
                }
                line++;
                //line = line % MAX_LINES_IN_DRUM_BANK;
            }
            sb.End();

            foreach (ParamButtonBar pbb in mParamBars)
            {
                pbb.Draw(sb);
            }

            mPasteData.Draw(sb);
        }

    }
}
