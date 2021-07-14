using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Commons.Music.Midi;
using System.Windows.Forms;

#if ANDROID
using Microsoft.Xna.Framework.Input.Touch;
#endif

#if NAUDIO_ASIO
#else
using BlueWave.Interop.Asio;
#endif


namespace MonoMusicMaker
{
    // declare delegate
    public delegate void Play();
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class MainMusicGame : Game
    {
        public const bool EXTRA_WIDE_WINDOWS = false;
        const string KEY_RANGE_TEXT = "Key Range";
        string[] KEY_NAMES_DROP_DOWN_TEXT = new string[13] { "No Key", "C", "C#/Db", "D", "D#/Eb", "E", "F", "F#/Gb", "G", "G#/Ab", "A", "A#/Bb", "B" };
        string[] TRACK_SELECT_NAMES = new string[16] { "Track 1", "Track 2", "Track 3", "Track 4", "Track 5", "Track 6", "Track 7", "Track 8", "Track 9", "DRUMS", "Track 11", "Track 12", "Track 13", "Track 14", "Track 15", "Track 16" };

        const int START_X_AFTER_PLAY_AREA = MelodyEditorInterface.TRACK_START_X + PlayArea.WIDTH_DRAW_AREA + 50;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public static SpriteFont mMassiveFont;
        public GetClipBoardBase mGetClipBoardHelper;

        SpriteFont massiveFont;
        SpriteFont font;
        SpriteFont smallGridFont;
        SpriteFont instruButtFont;
        SpriteFont saveNameFont;
        MidiPlayer player;
        Play playDelegate = null;
        MidiBase mKeyboardAndMidiCommonControl = null;
        AudioRecordBase mAudioRecorder = null;
        MidiManager mMidiMgr = null;

        PsiKlopsInput m_InputState;

        Button mPlay;
        Button mInstSel;
        Button mLoad;
        Button mStartStop;
        Button mRewind;
        Button mSave;
        //Button mChangeViewTrack;


        Button mRClickSetPlayHead;

        Button mUndo;
        Button mClearAll;
        Button mNukeAll;
        Button mMode;
        Button mBPM;
        Button mSoloTrack;
        Button mLoopSaveTickBox;

        Button mCopyPreviewToTracksTick;
        Button mMidiInputTick;

        //Button mQuant;
        //Button mKeyNoteLines; //switches play area note to a mode where the just offer notes from a specifid key - and also allow quick entry and deletion withouit the long press deley - they will be quantised and later allow shifting
        ParamButtonBar mDrumPresetSelectPbb;
        ParamButtonBar mRiffSelectbb;
        ParamButtonBar mChordTypePbb;
        ParamButtonBar mModeTypePbb;
        ParamButtonBar mKeyNotesKeyPbb;
        ParamButtonBar mTrackSelectPbb;
        ParamButtonBar mAlternateSongsPbb;

        PluginHost.MainForm mMainForm;
        MonoAsio mMonoAsio;

        public void AlternateSaveCB(Button button, MelodyEditorInterface.MEIState state)
        {
            state.StopAllNotes(); //TODO Blatt out a clear to stop notes being stuck on 

            if (state.mMeledInterf.mSaveSwitchManager.GetSelected() != button.mGED.mValue)
            {
                if(mKeyNotesKeyPbb.GetSelected()!=0)
                {
                    state.mMeledInterf.mSaveSwitchManager.SetSelected(button, mKeyNotesKeyPbb.GetSelectedButton(), mModeTypePbb.GetSelectedButton());
                }
                else
                {
                    state.mMeledInterf.mSaveSwitchManager.SetSelected(button);
                }
            }

            System.Diagnostics.Debug.WriteLine(string.Format("New Save value {0}", button.mGED.mValue));
        }

        public void RiffSelectParamBarCB(Button button, MelodyEditorInterface.MEIState state)
        {
            state.StopAllNotes(); //TODO Blatt out a clear to stop notes being stuck on 

            if (state.mMeledInterf.mPresetManager.SetSelected(button.mGED.mValue))
            {
                mRiffSelectbb.SetPostUpdateSelected(0); //Reset if the selected says so - e.g. if save button
            }

            System.Diagnostics.Debug.WriteLine(string.Format("RIFF SELECT! value {0}", button.mGED.mValue));
        }

        public void DrumPresetSelectParamBarCB(Button button, MelodyEditorInterface.MEIState state)
        {
            state.StopAllNotes(); //TODO Blatt out a clear to stop notes being stuck on 

            if(state.mMeledInterf.mDrumPresetManager.SetSelected(button.mGED.mValue))
            {
                mDrumPresetSelectPbb.SetPostUpdateSelected(0); //Reset if the selected says so - e.g. if save button
            }

            System.Diagnostics.Debug.WriteLine(string.Format("RIFF SELECT! value {0}", button.mGED.mValue));
        }


        /////////////////////////////////////////////////
        /// TRACK SWAP CALLBACKS 
        /// 
        int mLASTTRACK_TOGGLE = 0;
        public void TrackSelectParamBarCB(Button button, MelodyEditorInterface.MEIState state)
        {
            if (state.mCurrentTrackPlayArea != button.mGED.mValue)
            {
                state.StopAllNotes(); //TODO Blatt out a clear to stop notes being stuck on 

                state.mSoloCurrent = false; //always turn off when we move away a track to another one
                state.mCurrentTrackPlayArea = button.mGED.mValue;

                mToggleDrumTrack.mbOn = state.mCurrentTrackPlayArea == MidiTrackSelect.DRUM_MACHINE_PLAY_AREA_INDEX;

                state.PlayAreaChanged(true, false, false);
                state.mMeledInterf.SetCurrentPlayArea(button.mGED.mValue);
            }

            System.Diagnostics.Debug.WriteLine(string.Format("TRACK SELECT! value {0}", button.mGED.mValue));
        }

        bool GotoDrumTrack(MelodyEditorInterface.MEIState state)
        {
            mLASTTRACK_TOGGLE = state.mCurrentTrackPlayArea;
            state.mSoloCurrent = false; //always turn off when we move away a track to another one
            state.mCurrentTrackPlayArea = MidiTrackSelect.DRUM_MACHINE_PLAY_AREA_INDEX;
            state.PlayAreaChanged(true, false, false);
            state.mMeledInterf.SetCurrentPlayArea(MidiTrackSelect.DRUM_MACHINE_PLAY_AREA_INDEX);
            //state.mMeledInterf.GetCurrentPlayArea().UpdateMidiTrackSelectStatus(state);

            mTrackSelectPbb.SetSelected(MidiTrackSelect.DRUM_MACHINE_PLAY_AREA_INDEX);
            return false;
        }

        bool BackToInstruTrack(MelodyEditorInterface.MEIState state)
        {
            state.mSoloCurrent = false; //always turn off when we move away a track to another one
            state.mCurrentTrackPlayArea = mLASTTRACK_TOGGLE;
            state.PlayAreaChanged(true, false, false);
            state.mMeledInterf.SetCurrentPlayArea(mLASTTRACK_TOGGLE);
            //state.mMeledInterf.GetCurrentPlayArea().UpdateMidiTrackSelectStatus(state);

            mTrackSelectPbb.SetSelected(mLASTTRACK_TOGGLE);
            return false;
        }

        public void ScaleTypeParamBarCB(Button button, MelodyEditorInterface.MEIState state)
        {
            mMelEdInterf.SetKeyScaleSelected(button.mGED.mValue);
            System.Diagnostics.Debug.WriteLine(string.Format("SCALE CHANGE! value {0}", button.mGED.mValue));

            mMelEdInterf.SetChordTypeSelected(state.mChordTypeSelected, true);
        }

        public void ChordTypeParamBarCB(Button button, MelodyEditorInterface.MEIState state)
        {
            mMelEdInterf.SetChordTypeSelected(button.mGED.mValue);
            System.Diagnostics.Debug.WriteLine(string.Format("CHORD TYPE CHANGE! value {0}", button.mGED.mValue));
        }

        public void KeyTypeParamBarCB(Button button, MelodyEditorInterface.MEIState state)
        {
            int newKeyRange = button.mGED.mValue - 1;

            if(newKeyRange<0)
            {
                mMelEdInterf.SetKeyNotesOnlyMode(false, mMelEdInterf.mState.mKeyRangeIndexSelected);
            }
            else if (newKeyRange != mMelEdInterf.mState.mKeyRangeIndexSelected || !state.mbKeyRangePlayAreaMode)
            {
                mMelEdInterf.SetKeyNotesOnlyMode(true, newKeyRange);
            }

            System.Diagnostics.Debug.WriteLine(string.Format("KEY CHANGE! value {0}", button.mGED.mValue));

            mMelEdInterf.SetChordTypeSelected(state.mChordTypeSelected, true);
        }

        Button mToggleButtonSet;
        Button mToggleDrumTrack;

        Button mEditModeToggle;
        Button mButtPitchDetect;
        Button mSetPanVolumeTickBox;

        Button mToggleNoteSlideOrVelocityAdj;
        Button mToggleParam;

        SquareCursor mSquareCursor;

        MelodyEditorInterface mMelEdInterf;

        List<Button> mAllUpdateButtons = new List<Button>();
        List<Button> mButtonSet2UpdateButtons = new List<Button>();
        List<Button> mAllButtons = new List<Button>();
        List<Button> mAllEditSpaceButtons = new List<Button>();

        List<Button> mMainButtonSet = new List<Button>();
        List<Button> mButtonSet1 = new List<Button>();
        List<Button> mButtonSet2 = new List<Button>();

        int mLastScrollWheelValue = 0;
        SliderInput m_BPMSlider;
        SliderInput m_QuantizeSlider;
        VolPanGridSelect m_VolPanSelector;
        MidiTrackSelect mMidiTrackSelect;
        InstrumentSelect mInstrumentSelect;
        PitchDetectResultManager m_PitchDetectResultManager;
        DrumMachinePopUp mDrumMachinePopup;

        ButtonGrid mTestGrid;

        //EditManager mOldEditMgr;
        EditManager mNewEditMgr;


        public const float SCALE_SCREEN = 1f;
        public const int BOX_ART_WIDTH = 400;
        const int BUTTON_START_X    = (int)(30f * SCALE_SCREEN);
        const int BUTTON_SPACE = (int)(250f * SCALE_SCREEN);
        const int BUTTON_HEIGHT_GAP = (int)(130f * SCALE_SCREEN);
        const int PLAY_BUTTON_XPOS = (int)(BUTTON_START_X);
        const int REWIND_BUTTON_XPOS = (int)(PLAY_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int SECOND_BUTTON_XPOS = (int)(PLAY_BUTTON_XPOS  + BUTTON_SPACE);
        public const int THIRD_BUTTON_XPOS = (int)(SECOND_BUTTON_XPOS + BUTTON_SPACE);
        public const int FOURTH_BUTTON_XPOS = (int)(THIRD_BUTTON_XPOS + BUTTON_SPACE);
        public const int FIFTH_BUTTON_XPOS = (int)(FOURTH_BUTTON_XPOS + BUTTON_SPACE);
        public const int SIXTH_BUTTON_XPOS = (int)(FIFTH_BUTTON_XPOS + BUTTON_SPACE);
        public const int SEVENTH_BUTTON_XPOS = (int)(SIXTH_BUTTON_XPOS + BUTTON_SPACE);


        const int TICK_BUTTON_SPACE = (int)(130f * SCALE_SCREEN);
        public const int SECOND_TICK_BUTTON_XPOS = (int)(PLAY_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int THIRD_TICK_BUTTON_XPOS = (int)(SECOND_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int FOURTH_TICK_BUTTON_XPOS = (int)(THIRD_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int FIFTH_TICK_BUTTON_XPOS = (int)(FOURTH_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int SIXTH_TICK_BUTTON_XPOS = (int)(FIFTH_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int SEVENTH_TICK_BUTTON_XPOS = (int)(SIXTH_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int EIGHTH_TICK_BUTTON_XPOS = (int)(SEVENTH_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int NINTH_TICK_BUTTON_XPOS = (int)(EIGHTH_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int TENTH_TICK_BUTTON_XPOS = (int)(NINTH_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int ELEVEN_TICK_BUTTON_XPOS = (int)(TENTH_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int TWELVE_TICK_BUTTON_XPOS = (int)(ELEVEN_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int THIRTEEN_TICK_BUTTON_XPOS = (int)(TWELVE_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int FOURTEEN_TICK_BUTTON_XPOS = (int)(THIRTEEN_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int FIFTEEN_TICK_BUTTON_XPOS = (int)(FOURTEEN_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);
        public const int SIXTEEN_TICK_BUTTON_XPOS = (int)(FIFTEEN_TICK_BUTTON_XPOS + TICK_BUTTON_SPACE);


        public const int BAR_BUTTON_PAIR_Y_GAP = (int)(10f * SCALE_SCREEN);
        public const int TOP_BUTTON_YPOS = (int)(20f * SCALE_SCREEN);
        public const int SECOND_BUTTON_YPOS = (int)(TOP_BUTTON_YPOS + BUTTON_HEIGHT_GAP);
        public const int SAVE_LOOP_BUTTON_YPOS = (int)(SECOND_BUTTON_YPOS + 30);
        public const int THIRD_BUTTON_YPOS = (int)(SECOND_BUTTON_YPOS + BUTTON_HEIGHT_GAP);
        public const int FOURTH_BUTTON_YPOS = (int)(THIRD_BUTTON_YPOS + BUTTON_HEIGHT_GAP);

        const int BOTTOM_BUTTON_GAP = 20;
        public const int BOTTOM_BUTTON_YPOS = (int)(MelodyEditorInterface.TRACK_START_Y + PlayArea.NUM_DRAW_LINES * NoteLine.TRACK_HEIGHT + BOTTOM_BUTTON_GAP);

        const int BPM_SLIDER_WIDTH = 500;
        const int BPM_MAX = 600;
        const int QUANT_MAX = 3;  //TODO shows integer but represents quarter note 0 - none - 1 1/4 2 - 1/2 3 - Full
        const int QUANT_SLIDER_WIDTH = 500;

        const int FILENAME_X = BUTTON_START_X;
        const int FILENAME_Y = TOP_BUTTON_YPOS + 110;
        const int MIDI_KEYBOARD_NAME_X = BUTTON_START_X + 500;

        const int CHORD_NAME_X = FIFTH_BUTTON_XPOS;
        const int CHORD_NAME_Y = TOP_BUTTON_YPOS;

        //MIDI INPUT
        MidiSynthIn mMidiSYnthIn;

        public MainMusicGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

#if ANDROID
            m_InputState = new PsiKlopsInput(TouchPanel.GetState());
#else
            m_InputState = new PsiKlopsInput(Mouse.GetState());
            mGetClipBoardHelper = new GetClipBoardTextWin();
#endif
            mMelEdInterf = new MelodyEditorInterface();
            mMelEdInterf.Init(m_InputState);

#if ANDROID
            int debugTextFudge = 30;
#else
            int debugTextFudge = -120;
#endif
            mMelEdInterf.mState.mDebugPanel.SetPos(START_X_AFTER_PLAY_AREA, SECOND_BUTTON_YPOS + debugTextFudge);

            mInstrumentSelect = new InstrumentSelect();
            mMidiTrackSelect = new MidiTrackSelect(mTrackSelectPbb);
            m_BPMSlider = new SliderInput(new Vector2(600, 500), BPM_SLIDER_WIDTH, true, BPM_MAX, (int)mMelEdInterf.BPM);
            m_QuantizeSlider = new SliderInput(new Vector2(600, 500), QUANT_SLIDER_WIDTH, true, QUANT_MAX, (int)mMelEdInterf.QuantiseSetting);

            m_VolPanSelector = new VolPanGridSelect(new Vector2(600, 500));

            //mTestGrid = new ButtonGrid(mInstrumentSelect.GetGridEntries());
        }

        Button mPlayAreaRecordMode;
        Button mClearAllPlayHeadOffset;
        Button mClearCurrentPlayHeadOffset;

        Button InitTickButton(int x, int y, string name, MelodyEditorInterface.UIButtonMask mask, Button.ButtonClick cb)
        {
            Button newTickButton = new Button(new Vector2(x, y), "PlainWhite");
            newTickButton.TextMid = name;
            newTickButton.mType = Button.Type.Tick;
            newTickButton.mMask = mask;
            newTickButton.ClickCB = cb;
            mAllUpdateButtons.Add(newTickButton);
            mMainButtonSet.Add(newTickButton);
            mAllButtons.Add(newTickButton);

            return newTickButton;
        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            //graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            //graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            graphics.PreferredBackBufferHeight = 1080 + 560;
            graphics.PreferredBackBufferWidth = 3440;
            graphics.ApplyChanges();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            this.IsMouseVisible = true; //Let's see the mouse pointer
            IsFixedTimeStep = true;

            //VST
            mMainForm = new PluginHost.MainForm(mMelEdInterf);
            //mMainForm.Init();
            mMainForm.Show();

            //mMonoAsio = new MonoAsio();
            //mMonoAsio.Init(mMainForm);
            //mMonoAsio.Start();

            //VSTiBox.StuAudioPugin sap = new VSTiBox.StuAudioPugin();



            //Application.Run(new MainForm());

#if ANDROID
#else
            graphics.PreferredBackBufferHeight = 1080 + 560;
            graphics.PreferredBackBufferWidth = 3440;

            if(EXTRA_WIDE_WINDOWS)
            {
                graphics.PreferredBackBufferWidth = 2900;
            }
            graphics.ApplyChanges();

            this.Window.Position = new Point(0, 10);

            this.Window.AllowUserResizing = false;
            this.Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);
#endif
            int gdW = GraphicsDevice.Viewport.Width;
            int gdH = GraphicsDevice.Viewport.Height;
            //when running windowed mode, as I find

            int gdW2 =GraphicsDevice.DisplayMode.Width;
            int gdH2 =GraphicsDevice.DisplayMode.Height;


            //mPlay = new Button(new Vector2(PLAY_BUTTON_XPOS, TOP_BUTTON_YPOS), "PlainWhite");
            //mPlay.ButtonText = "Play";
            //mPlay.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Play;

            mInstSel = new Button(new Vector2(SECOND_BUTTON_XPOS, TOP_BUTTON_YPOS), "PlainWhite");
            mInstSel.ButtonText = "Inst Sel";
            mInstSel.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Inst;
            mInstSel.mType = Button.Type.Bar;

            mBPM = new Button(new Vector2(SECOND_BUTTON_XPOS, TOP_BUTTON_YPOS + Button.mBarHeight + BAR_BUTTON_PAIR_Y_GAP), "PlainWhite"); //new Button(new Vector2(PLAY_BUTTON_XPOS, TOP_BUTTON_YPOS), "PlainWhite");  //new Button(new Vector2(PLAY_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite");
            mBPM.ButtonText = string.Format("BPM {0}", mMelEdInterf.BPM);
            mBPM.mMask = MelodyEditorInterface.UIButtonMask.UIBM_BPM;
            mBPM.mType = Button.Type.Bar;

            mStartStop = new Button(new Vector2(PLAY_BUTTON_XPOS, TOP_BUTTON_YPOS), "play", "stop"); //new Button(new Vector2(THIRD_BUTTON_XPOS, TOP_BUTTON_YPOS), "PlainWhite");
            //mStartStop.ButtonText = "Start";
            //mStartStop.ButtonTextOff = "Stop";
            mStartStop.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Start;
            mStartStop.mType = Button.Type.Tick;

            mRewind = new Button(new Vector2(REWIND_BUTTON_XPOS, TOP_BUTTON_YPOS), "rwnd"); //new Button(new Vector2(THIRD_BUTTON_XPOS, TOP_BUTTON_YPOS), "PlainWhite");
            mRewind.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Rewind;
            mRewind.mType = Button.Type.Tick;

            mSave = new Button(new Vector2(THIRD_BUTTON_XPOS, TOP_BUTTON_YPOS), "PlainWhite");
            mSave.ButtonText = "Save";
            mSave.ButtonTextOff = "Exit Save";
            mSave.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Save;
            mSave.mType = Button.Type.Bar;

            mLoad = new Button(new Vector2(THIRD_BUTTON_XPOS, TOP_BUTTON_YPOS + Button.mBarHeight + BAR_BUTTON_PAIR_Y_GAP), "PlainWhite");
            mLoad.ButtonText = "Load";
            mLoad.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Load;
            mLoad.mType = Button.Type.Bar;

            //mChangeViewTrack = new Button(new Vector2(SIXTH_BUTTON_XPOS, TOP_BUTTON_YPOS), "PlainWhite");
            //mChangeViewTrack.ButtonText = string.Format("Track {0}", 1);
            //mChangeViewTrack.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Change;

            ParamButtonBar.SetOffsets(TOP_BUTTON_YPOS, SIXTH_BUTTON_XPOS, 0);
            mTrackSelectPbb = ParamButtonBar.SetUpPopupParamBar(TRACK_SELECT_NAMES, TrackSelectParamBarCB, ParamButtonBar.DROP_TYPE.DOWN);

            ParamButtonBar.SetOffsets(TOP_BUTTON_YPOS, FIFTH_BUTTON_XPOS, 0);
            mRiffSelectbb = ParamButtonBar.SetUpPopupParamBar(mMelEdInterf.mPresetManager.riffNames, RiffSelectParamBarCB, ParamButtonBar.DROP_TYPE.DOWN);

            ParamButtonBar.SetOffsets(TOP_BUTTON_YPOS, FIFTH_BUTTON_XPOS, 0);
            mDrumPresetSelectPbb = ParamButtonBar.SetUpPopupParamBar(mMelEdInterf.mDrumPresetManager.mDrumPresetNames, DrumPresetSelectParamBarCB, ParamButtonBar.DROP_TYPE.DOWN);

            ParamButtonBar.SetOffsets(TOP_BUTTON_YPOS + Button.mBarHeight + 15, FIFTH_BUTTON_XPOS, 0);
            mAlternateSongsPbb = ParamButtonBar.SetUpPopupParamBar(mMelEdInterf.mSaveSwitchManager.mSaveSlotNames, AlternateSaveCB, ParamButtonBar.DROP_TYPE.DOWN);
            mMelEdInterf.mSaveSwitchManager.Init(mMelEdInterf, mAlternateSongsPbb);

            //QUANT
            //mQuant = new Button(new Vector2(SEVENTH_BUTTON_XPOS, TOP_BUTTON_YPOS), "PlainWhite"); //new Button(new Vector2(THIRD_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite");
            //mQuant.ButtonText = "Quant";
            //mQuant.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Quant;

            //mKeyNoteLines = new Button(new Vector2(SEVENTH_BUTTON_XPOS, TOP_BUTTON_YPOS), "PlainWhite"); //new Button(new Vector2(THIRD_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite");
            //mKeyNoteLines.ButtonText = KEY_RANGE_TEXT;
            //mKeyNoteLines.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Quant;
            //mKeyNoteLines.mType = Button.Type.Bar;

            ParamButtonBar.SetOffsets(TOP_BUTTON_YPOS, SEVENTH_BUTTON_XPOS, 0);
            mKeyNotesKeyPbb = ParamButtonBar.SetUpPopupParamBar(KEY_NAMES_DROP_DOWN_TEXT, KeyTypeParamBarCB, ParamButtonBar.DROP_TYPE.DOWN);

            ParamButtonBar.SetOffsets(TOP_BUTTON_YPOS + Button.mBarHeight + 15, SEVENTH_BUTTON_XPOS, 0);
            mModeTypePbb = ParamButtonBar.SetUpPopupParamBar(mMelEdInterf.mAIChordMgr.scaleNames, ScaleTypeParamBarCB, ParamButtonBar.DROP_TYPE.DOWN);
            mModeTypePbb.SetButtonColours();

            ParamButtonBar.SetOffsets(TOP_BUTTON_YPOS + Button.mBarHeight + 15, SIXTH_BUTTON_XPOS, 0);
            mChordTypePbb = ParamButtonBar.SetUpPopupParamBar(mMelEdInterf.mAIChordMgr.chordTypeNumbers, ChordTypeParamBarCB, ParamButtonBar.DROP_TYPE.DOWN);
            mChordTypePbb.InitScroll();

            //mMode = new Button(new Vector2(SEVENTH_BUTTON_XPOS, TOP_BUTTON_YPOS), "PlainWhite");
            //mMode.ButtonText = "Mode";
            //mMode.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Mode;

            mCopyPreviewToTracksTick = new Button(new Vector2(NINTH_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite"); ;
            mCopyPreviewToTracksTick.mType = Button.Type.Tick;
            mCopyPreviewToTracksTick.TextMid = "Copy\nPrev";
            mCopyPreviewToTracksTick.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Tick;

            mMidiInputTick = new Button(new Vector2(TENTH_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite"); ;
            mMidiInputTick.mType = Button.Type.Tick;
            //mMidiInputTick.ButtonText = "MIDI IN!";
            mMidiInputTick.TextMid = "MIDI";
            mMidiInputTick.ButtonTextOff = "REC";  //I know, for some reason the off text is on!?
            mMidiInputTick.ColOn = Color.Red;
            mMidiInputTick.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Tick;

            mLoopSaveTickBox = new Button(new Vector2(SIXTH_BUTTON_XPOS, SAVE_LOOP_BUTTON_YPOS), "PlainWhite"); ;
            mLoopSaveTickBox.mType = Button.Type.Tick;
            mLoopSaveTickBox.ButtonText = "Save looped";
            mLoopSaveTickBox.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Tick;

            ////////////////////////////////////////
            ////////////////////////////////////////
            /// TICK BUTTOMS ALONG BOTTOM
            ////////////////////////////////////////
            ////////////////////////////////////////
            mToggleButtonSet = new Button(new Vector2(PLAY_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite"); ;
            mToggleButtonSet.mType = Button.Type.Tick;
            //mToggleButtonSet.ButtonText = "Pan/Vol";
            mToggleButtonSet.TextMid = "SET 1";
            mToggleButtonSet.ButtonTextOff = "SET 2";  //I know, for some reason the off text is on!?
            mToggleButtonSet.ColOff = Color.Green;
            mToggleButtonSet.ColOn = Color.LightPink;
            mToggleButtonSet.mMask = MelodyEditorInterface.UIButtonMask.UIBM_ToggleButtons;

            mToggleDrumTrack = new Button(new Vector2(FOURTH_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite"); ;
            mToggleDrumTrack.mType = Button.Type.Tick;
            //mToggleButtonSet.ButtonText = "Pan/Vol";
            mToggleDrumTrack.TextMid = "Inst";
            mToggleDrumTrack.ButtonTextOff = "DRUMS";  //I know, for some reason the off text is on!?
            mToggleDrumTrack.ColOff = Color.LightBlue;
            mToggleDrumTrack.ColOn = Color.Green;
            mToggleDrumTrack.mMask = MelodyEditorInterface.UIButtonMask.UIBM_ToggleButtons;

            mToggleDrumTrack.SetCallbacks(GotoDrumTrack, BackToInstruTrack);


            mSetPanVolumeTickBox = new Button(new Vector2(FIFTH_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite"); ;
            mSetPanVolumeTickBox.mType = Button.Type.Tick;
            //mSetPanVolumeTickBox.ButtonText = "Pan/Vol";
            mSetPanVolumeTickBox.TextMid = "Pan";
            mSetPanVolumeTickBox.mMask = MelodyEditorInterface.UIButtonMask.UIBM_PanVol;

            mToggleNoteSlideOrVelocityAdj = new Button(new Vector2(FIFTH_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite"); ;
            mToggleNoteSlideOrVelocityAdj.mType = Button.Type.Tick;
            mToggleNoteSlideOrVelocityAdj.TextMid = "NOTE\nSLIDE";
            mToggleNoteSlideOrVelocityAdj.ButtonTextOff = "NOTE\nVEL";  //I know, for some reason the off text is on!?
            mToggleNoteSlideOrVelocityAdj.mMask = MelodyEditorInterface.UIButtonMask.UIBM_ToggNoteSlideVelocity;

            mToggleParam = new Button(new Vector2(SIXTH_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite"); ;
            mToggleParam.mType = Button.Type.Tick;
            mToggleParam.TextMid = "REGION";
            mToggleParam.ButtonTextOff = "PARAM";  //I know, for some reason the off text is on!?
            mToggleParam.ColOn = Color.Orange;
            mToggleParam.mMask = MelodyEditorInterface.UIButtonMask.UIBM_ToggParam;

            mButtPitchDetect = new Button(new Vector2(SIXTH_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite"); ;
            mButtPitchDetect.mType = Button.Type.Tick;
            //mButtPitchDetect.ButtonText = "Pitch Det";
            mButtPitchDetect.TextMid = "Pitch";
            mButtPitchDetect.mMask = MelodyEditorInterface.UIButtonMask.UIBM_PitchDet;

            mEditModeToggle = new Button(new Vector2(SEVENTH_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite"); ;
            mEditModeToggle.mType = Button.Type.Tick;
            //mEditModeToggle.ButtonText = "Edit Mode";
            mEditModeToggle.TextMid = "Edit";
            mEditModeToggle.mMask = MelodyEditorInterface.UIButtonMask.UIBM_EditMode;

            mSoloTrack = new Button(new Vector2(EIGHTH_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite"); ;
            mSoloTrack.mType = Button.Type.Tick;
            //mSoloTrack.ButtonText = "Solo Track";
            mSoloTrack.TextMid = "Solo";
            mSoloTrack.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Tick;


            mClearAll = new Button(new Vector2(NINTH_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite");
            mClearAll.TextMid = "CLR";
            mClearAll.mType = Button.Type.Tick;
            mClearAll.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Undo;
            mClearAll.ClickCB = ClearAll;
            mButtonSet2UpdateButtons.Add(mClearAll);

            mUndo = new Button(new Vector2(TENTH_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite");
            mUndo.TextMid = "Undo";
            mUndo.mType = Button.Type.Tick;
            mUndo.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Undo;
            mUndo.ClickCB = UndoCB;
            mButtonSet2UpdateButtons.Add(mUndo);

            mNukeAll = new Button(new Vector2(EIGHTH_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite");
            mNukeAll.TextMid = "NUKE";
            mNukeAll.mType = Button.Type.Tick;
            mNukeAll.mMask = MelodyEditorInterface.UIButtonMask.UIBM_NukeAll;
            mNukeAll.ClickCB = NukeAll;
            mButtonSet2UpdateButtons.Add(mNukeAll);
            // // // // // // // // // // // // // // // // // // // ///
        
            mClearAllPlayHeadOffset = InitTickButton(FIFTEEN_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS, "CLR\nOFFSET", MelodyEditorInterface.UIButtonMask.UIBM_ClearPlayOffset, ResetAllPlayHeadOffset);
            mPlayAreaRecordMode = InitTickButton(SIXTEEN_TICK_BUTTON_XPOS, BOTTOM_BUTTON_YPOS, "REC\nPLAY", MelodyEditorInterface.UIButtonMask.UIBM_PlayAreaRecMode, PlayAreaRecModeSwitch);

            /////////////////////////////////////////////
            /// BUTTONS IN SAME SPACE AS EDITOR BUTTONS
            ///             
            mRClickSetPlayHead = new Button(new Vector2(MainMusicGame.FOURTH_BUTTON_XPOS, MainMusicGame.TOP_BUTTON_YPOS), "PlainWhite"); ;
            mRClickSetPlayHead.ButtonText = "Set PlayH";
            mRClickSetPlayHead.ButtonTextColour = Color.Black;
            mRClickSetPlayHead.mMask = MelodyEditorInterface.UIButtonMask.UIBM_EditControl;
            //mRClickSetPlayHead.ClickCB = Copy;
            mRClickSetPlayHead.mType = Button.Type.Bar;

            mAllEditSpaceButtons.Add(mRClickSetPlayHead);

            //Main button set
            mAllButtons.Add(mToggleButtonSet);
            mAllButtons.Add(mToggleDrumTrack);
            mAllButtons.Add(mInstSel);
            mAllButtons.Add(mStartStop);
            mAllButtons.Add(mRewind);
            mAllButtons.Add(mSave);
            mAllButtons.Add(mLoad);
            mAllButtons.Add(mBPM);
            mAllButtons.Add(mLoopSaveTickBox);

            //Button set 1 in all buttons
            mAllButtons.Add(mEditModeToggle);
            mAllButtons.Add(mSoloTrack);
            mAllButtons.Add(mCopyPreviewToTracksTick);
            mAllButtons.Add(mMidiInputTick);
            mAllButtons.Add(mToggleNoteSlideOrVelocityAdj);
            mAllButtons.Add(mToggleParam);

            //Button set 2 in all buttons
            mAllButtons.Add(mSetPanVolumeTickBox);
            mAllButtons.Add(mButtPitchDetect);
            mAllButtons.Add(mClearAll);
            mAllButtons.Add(mNukeAll);
            mAllButtons.Add(mUndo);

            //SEPERATE LISTS FOR DRAWING
            //Main button set
            mMainButtonSet.Add(mToggleButtonSet);
            mMainButtonSet.Add(mToggleDrumTrack);
            mMainButtonSet.Add(mInstSel);
            mMainButtonSet.Add(mStartStop);
            mMainButtonSet.Add(mRewind);
            mMainButtonSet.Add(mSave);
            mMainButtonSet.Add(mLoad);
            mMainButtonSet.Add(mBPM);
            mMainButtonSet.Add(mLoopSaveTickBox);

            //Button set 1
            mButtonSet1.Add(mEditModeToggle);
            mButtonSet1.Add(mSoloTrack);
            mButtonSet1.Add(mCopyPreviewToTracksTick);
            mButtonSet1.Add(mMidiInputTick);
            mButtonSet1.Add(mToggleNoteSlideOrVelocityAdj);
            mButtonSet1.Add(mToggleParam);

            //Button set 2
            mButtonSet2.Add(mSetPanVolumeTickBox);
            mButtonSet2.Add(mButtPitchDetect);
            mButtonSet2.Add(mClearAll);
            mButtonSet2.Add(mNukeAll);
            mButtonSet2.Add(mUndo);

#if ANDROID
            mAudioRecorder = new AudioRecordAndroid();
#elif WINDOWS
            MidiWin midiUWP = new MidiWin();
            SetMidiInterface(midiUWP);
            mAudioRecorder = new AudioRecordWin();
#else
            MidiUWP midiUWP = new MidiUWP();
            SetMidiInterface(midiUWP);
#endif

            mKeyboardAndMidiCommonControl.SetState(mMelEdInterf.mState);

            mAudioRecorder.Init();
            mKeyboardAndMidiCommonControl.BaseInit();

            m_PitchDetectResultManager = new PitchDetectResultManager();
            m_PitchDetectResultManager.Init(mAudioRecorder);

            mDrumMachinePopup = new DrumMachinePopUp();
            mDrumMachinePopup.Init();
            mMelEdInterf.mState.mAIDrumRegionPopup = mDrumMachinePopup;

            //mOldEditMgr = new EditManager();
            //mOldEditMgr.Init(mMelEdInterf);

            mNewEditMgr = new EditManager();
            mNewEditMgr.InitKeyScaleMode(mMelEdInterf);

            mMelEdInterf.mState.mMidiTrackSelect = mMidiTrackSelect;
            mMidiTrackSelect.SetTrackSelectBar(mTrackSelectPbb);
            mMelEdInterf.mEditManager = mNewEditMgr;

            mLoopSaveTickBox.mbOn = mMelEdInterf.mState.mbSaveMIDIWithLoop; //make sure button is set to the default


            //MIDI SYNTH IN 
            mMidiSYnthIn = new MidiSynthIn(mKeyboardAndMidiCommonControl);

            ClearAll(mMelEdInterf.mState); // Make sure the volume is set to default on all tracks from the start!

            base.Initialize();
        }

        public bool UndoCB(MelodyEditorInterface.MEIState state)
        {
            return mMelEdInterf.GetSaveLoad().UndoCB(state);
        }

        public bool ClearAll(MelodyEditorInterface.MEIState state)
        {
            //Free up save slot
            if(mMelEdInterf.mSaveSwitchManager.FreeCurrentSaveSlot())
            {
                return true;
            }
            return mMelEdInterf.GetSaveLoad().ClearAll(state);
        }

        public bool NukeAll(MelodyEditorInterface.MEIState state)
        {
            mAlternateSongsPbb.ResetButtonsToGED();
            mMelEdInterf.mSaveSwitchManager.NukeAll();
            return mMelEdInterf.GetSaveLoad().ClearAll(state);
        }

        public bool ResetAllPlayHeadOffset(MelodyEditorInterface.MEIState state)
        {
            mClearAllPlayHeadOffset.mbOn = false;
            mMelEdInterf.ResetAllPlayHeadOffset();
            return true;
        }

        public bool PlayAreaRecModeSwitch(MelodyEditorInterface.MEIState state)
        {
            //mPlayAreaRecordMode.mbOn = !mPlayAreaRecordMode.mbOn;
            if(!mMelEdInterf.PlayAreaRecModeSwitch(mPlayAreaRecordMode.mbOn))
            {
                mPlayAreaRecordMode.mbOn = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            mMassiveFont = Content.Load<SpriteFont>("massiveFont");

            font = Content.Load<SpriteFont>("MusicFont");
            massiveFont = Content.Load<SpriteFont>("massiveFont");

            saveNameFont = Content.Load<SpriteFont>("SaveName");
#if ANDROID
            smallGridFont = Content.Load<SpriteFont>("AndroidButGridFont");
#else
            smallGridFont = Content.Load<SpriteFont>("smallGridFont");
#endif

            instruButtFont = Content.Load<SpriteFont>("instruButton");
            foreach (Button but in mAllButtons)
            {
                if(but.mType==Button.Type.Tick)
                {
                    but.LoadContent(Content, smallGridFont);
                    but.ButtonTextColour = Color.Black;
                }
                else
                {
                    but.LoadContent(Content, font);
                }
            }

            foreach (Button but in mAllEditSpaceButtons)
            {
                if (but.mType == Button.Type.Tick)
                {
                    but.LoadContent(Content, smallGridFont);
                    but.ButtonTextColour = Color.Black;
                }
                else
                {
                    but.LoadContent(Content, font);
                }
            }

            //mTestGrid.LoadContent(Content, smallGridFont);
            mInstrumentSelect.LoadContent(Content, smallGridFont);
 //           mMidiTrackSelect.LoadContent(Content, smallGridFont);
            mMelEdInterf.LoadContent(Content, smallGridFont);
            mMelEdInterf.SetPlayAreaFontAndSpriteBatch(smallGridFont, spriteBatch);
            mKeyboardAndMidiCommonControl.LoadContent(Content, font, saveNameFont);
            mSquareCursor = new SquareCursor(spriteBatch, Content, smallGridFont);
            m_BPMSlider.LoadContent(Content);
            m_QuantizeSlider.LoadContent(Content);
            m_VolPanSelector.LoadContent(Content);
            m_PitchDetectResultManager.LoadContent(Content, font, smallGridFont);

            //mOldEditMgr.LoadContent(Content, font, smallGridFont); //TODO
            mNewEditMgr.LoadContent(Content, font, smallGridFont);

            mDrumMachinePopup.LoadContent(Content, font, smallGridFont);

            mMelEdInterf.mState.SetSquareCursor(mSquareCursor);

            mModeTypePbb.LoadContent(Content, smallGridFont);
            mKeyNotesKeyPbb.LoadContent(Content, smallGridFont);
            mTrackSelectPbb.LoadContent(Content, instruButtFont);
            mChordTypePbb.LoadContent(Content, smallGridFont);
            mAlternateSongsPbb.LoadContent(Content, instruButtFont);

            mRiffSelectbb.LoadContent(Content, instruButtFont);
            mDrumPresetSelectPbb.LoadContent(Content, instruButtFont);

            //need the button to match defaults set in state 
            //mMIDITickBox.mbOn = mMelEdInterf.mState.mbMIDIFilePrefix;
            mLoopSaveTickBox.mbOn = mMelEdInterf.mState.mbSaveMIDIWithLoop;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // TODO: Add your update logic here

            if(IsActive)
            {
                UpdateInput(gameTime);
            }

            bool bButtonMaskChange = mMelEdInterf.Update(gameTime);

            if(bButtonMaskChange)
            {
                foreach (Button but in mAllButtons)
                {
                    but.UpdateMask(mMelEdInterf.mState);
                }

                if (!mEditModeToggle.mbOn)
                {
                    foreach (Button but in mAllEditSpaceButtons)
                    {
                        but.UpdateMask(mMelEdInterf.mState);
                    }
                }
            }

            base.Update(gameTime);
        }

        public void SetPlayDelegate(Play pd)
        {
            playDelegate = pd;
        }

        public void SetMidiInterface(MidiBase mi)
        {
            TextEntryBase textEntry = null;
#if ANDROID
            textEntry = new TextEntryAndroid();
#elif WINDOWS
           textEntry = new TextEntryWin();
#else

#endif
            textEntry.mParent = this;
            mi.SetTextEntry(textEntry);

            mKeyboardAndMidiCommonControl = mi;
            mMidiMgr = new MidiManager(mKeyboardAndMidiCommonControl);
            mMelEdInterf.SetMidiMgr(mMidiMgr); 
            mMelEdInterf.SetMelEdInterf();
        }

        void InitScrollWheelDiff(PsiKlopsInput input)
        {
            mLastScrollWheelValue = input.ScrollWheelValue;
        }

        int GetScrollWheelDiff(PsiKlopsInput input, int currentChangeValue)
        {
            // https://stackoverflow.com/questions/7753123/why-is-wheeldelta-120
            int currentScrollWheelValue = input.ScrollWheelValue;

            if (currentScrollWheelValue != mLastScrollWheelValue)
            {
                int diffScrollWheel = mLastScrollWheelValue - currentScrollWheelValue;
                diffScrollWheel = diffScrollWheel / 120;
                currentChangeValue += diffScrollWheel;
                mLastScrollWheelValue = currentScrollWheelValue;
            }

            return currentChangeValue;
        }

        public bool UpdateInput(GameTime gameTime)
        {

            //From twit
            mMelEdInterf.mState.gameTime = gameTime;

#if ANDROID
            TouchCollection tc = TouchPanel.GetState();
            m_InputState.UpdateFromTouchCollection(tc, gameTime);
#else
            MouseState ms = Mouse.GetState();
            m_InputState.UpdateFromMouseState(ms, gameTime);
#endif


            //HACKETY HACK! Gotta see keyboard name on android phone when USB is conneceted!
            if (mKeyboardAndMidiCommonControl.mMidiKeyboardName != "None")
            {
                mMelEdInterf.mState.mDebugPanel.SetFloat(0.0f, mKeyboardAndMidiCommonControl.mMidiKeyboardName);
            }

            mMidiSYnthIn.Update(mMelEdInterf.mState);

            mMelEdInterf.UpdateInput(mMelEdInterf.mState);

            //BEFORE EVERYTHING ELSE
            mSave.mbOn = mKeyboardAndMidiCommonControl.UpdateKeyboard(mMelEdInterf);

            // / // // // // // // // // // // // // // // // // // // // // // // 
            // / // // // // // // // // // // // // // // // // // // // // // // 
            //DRUM MACHINE
            // / // // // // // // // // // // // // // // // // // // // // // // 
            // / // // // // // // // // // // // // // // // // // // // // // // 
            //if (mMelEdInterf.mState.IsDrumMachineTrack())
            //{
            //    if (mSoloTrack.Update(mMelEdInterf.mState))
            //    {
            //        mSoloTrack.mbOn = false;
            //        if (mDrumMachinePopup.Active)
            //        {
            //            mDrumMachinePopup.Stop(mMelEdInterf.mState);
            //        }
            //        else
            //        {
            //            mDrumMachinePopup.Start(mMelEdInterf.mState);
            //        }
            //    }
            //}

            if (mMelEdInterf.mState.mSoloCurrent != mSoloTrack.mbOn)
            {
                //If the soloing has been taken off because we switched track, make sure button
                //is set off too
                mSoloTrack.mbOn = mMelEdInterf.mState.mSoloCurrent;
            }

            //BUTTON SET 1
            if(!mToggleButtonSet.mbOn)
            {
                if(mToggleNoteSlideOrVelocityAdj.Update(mMelEdInterf.mState))
                {
                    mMelEdInterf.mState.mbNoteEditInVelocityMode = mToggleNoteSlideOrVelocityAdj.mbOn;
                }

                if (mToggleParam.Update(mMelEdInterf.mState))
                {
                    //mKeyboardAndMidiCommonControl.TestSysex();
                    mMelEdInterf.EnableParamLine(mToggleParam.mbOn);
                }

                if (mMidiInputTick.Update(mMelEdInterf.mState))
                {
                    mMidiSYnthIn.Enabled = mMidiInputTick.mbOn;
                    mMelEdInterf.mState.mbRecording = mMidiInputTick.mbOn;
                    mMelEdInterf.mState.mNoteUI.SetRecordModeOn(mMidiInputTick.mbOn);
                }

                if (mCopyPreviewToTracksTick.Update(mMelEdInterf.mState))
                {
                    mCopyPreviewToTracksTick.mbOn = false;
                    mMelEdInterf.mState.mNoteUI.CopyPreviewToTracks();
                }

                if (mSoloTrack.Update(mMelEdInterf.mState))
                {
                    mMelEdInterf.mState.mSoloCurrent = mSoloTrack.mbOn;

                    if (mMelEdInterf.mState.mSoloCurrent)
                    {
                        mMelEdInterf.mState.StopAllNotes(mMelEdInterf.mState.mCurrentTrackPlayArea + 1); //plaus 1 because this is midi channel number to except from stopping
                    }
                }
            }

            if (mDrumMachinePopup.Active)
            {
                 mMelEdInterf.mState.mbAllowInputPlayArea = false;
                mDrumMachinePopup.Update(mMelEdInterf.mState);
                //return false;
            }
            if (mMelEdInterf.mState.mbRequestDrumMachine)
            {
                mDrumMachinePopup.Start(mMelEdInterf.mState);
                mMelEdInterf.mState.mbRequestDrumMachine = false;
            }
            // / // // // // // // // // // // // // // // // // // // // // // // 
            // / // // // // // // // // // // // // // // // // // // // // // // 
            //DRUM MACHINE
            // / // // // // // // // // // // // // // // // // // // // // // // 
            // / // // // // // // // // // // // // // // // // // // // // // // 

            if (mMelEdInterf.mState.mAIRegionPopup.Active)
            {
                //do no inputs this is updatedin meledinterf update
                mMelEdInterf.mState.mbAllowInputPlayArea = false;
                if(mMelEdInterf.mState.mAIRegionPopup.Update(mMelEdInterf.mState))
                {
                    mSquareCursor.Update(mMelEdInterf.mState);
                }

                //TODO HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK 
                //TODO HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK 
                //TODO HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK 
                if (mStartStop.Update(mMelEdInterf.mState))
                {
                    if (mMelEdInterf != null)
                    {
                        mMelEdInterf.StopStart();

                        if (!mStartStop.mbOn)
                        {
                            mMelEdInterf.mState.StopAllNotes(); //TODO - bit high level UI for this!
                        }
                    }
                }
                //TODO HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK 
                //TODO HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK 
                //TODO HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK HACK 


                return false;
            }

            if (mSave.mbOn)
            {
                mLoopSaveTickBox.Visible = true;
                if (mLoopSaveTickBox.Update(mMelEdInterf.mState))
                {
                    mMelEdInterf.mState.mbSaveMIDIWithLoop = mLoopSaveTickBox.mbOn;
                }

                if (mSave.Update(mMelEdInterf.mState))
                {
                    if (mMelEdInterf != null)
                    {
                        mMelEdInterf.Save(mSave.mbOn);
                    }
                }
                mMelEdInterf.mState.mbAllowInputPlayArea = false;
                return false;

             }
            else
            {
                mLoopSaveTickBox.Visible = false;
                if (mKeyboardAndMidiCommonControl.SaveNameEntered)
                {
                    mKeyboardAndMidiCommonControl.SaveNameEntered = false;
                    //mMelEdInterf.GetSaveLoad().GatherMidiFromTrackToSave(mMelEdInterf, mMi.GetSaveName());
                    mMelEdInterf.GetSaveLoad().SaveAllTracks(mMelEdInterf, mKeyboardAndMidiCommonControl.GetSaveName());
                }
            }


            mMelEdInterf.mState.mbAllowInputPlayArea = true;

            if (mDrumMachinePopup.Active)
            {
                mMelEdInterf.mState.mbAllowInputPlayArea = false;
            }

            //mTestGrid.Update(mMelEdInterf.mState);
            if (mInstrumentSelect.Update(mMelEdInterf.mState))
            {
                mMelEdInterf.mState.mbAllowInputPlayArea = false;
                //return false; //dont update any other button while this active
            }

            if(mBPM.mbOn)
            {
#if ANDROID
                if (m_BPMSlider.Visible)
                {
                    mMelEdInterf.mState.mbAllowInputPlayArea = false;
                    m_BPMSlider.Update(mMelEdInterf.mState);
                    int bpmFromSlider = m_BPMSlider.GetCurrentIntegerSelectedSliderPosition();

                    if (bpmFromSlider == 0)
                    {
                        bpmFromSlider = 1; //TODO until have proper limit on slider range
                    }
                    mMelEdInterf.SetBPM(bpmFromSlider);
                    mBPM.ButtonText = string.Format("BPM {0}", mMelEdInterf.BPM);
                }
#else
                int currentBPM = GetScrollWheelDiff(mMelEdInterf.mState.input, (int)mMelEdInterf.BPM);

                if (currentBPM <= 0)
                {
                    currentBPM = 1; //TODO until have proper limit on slider range
                }

                mMelEdInterf.SetBPM(currentBPM);
                mBPM.ButtonText = string.Format("BPM {0}", mMelEdInterf.BPM);
#endif
            }

            //QUANT
            //if (m_QuantizeSlider.Visible)
            //{
            //    mMelEdInterf.mState.mbAllowInputPlayArea = false;
            //    m_QuantizeSlider.Update(mMelEdInterf.mState);
            //    mMelEdInterf.SetQuant(m_QuantizeSlider.GetCurrentIntegerSelectedSliderPosition());
            //    mQuant.ButtonText = string.Format("Quant {0}", mMelEdInterf.Quant==0f?"None":mMelEdInterf.Quant.ToString());
            //}

            //Button set 2
            if (m_VolPanSelector.Visible)
            {
                mMelEdInterf.mState.mbAllowInputPlayArea = false;
                m_VolPanSelector.UpdateInput(mMelEdInterf.mState);
            }

            //Button set 2
            if (m_PitchDetectResultManager.Active)
            {
                mMelEdInterf.mState.mbAllowInputPlayArea = false;
                m_PitchDetectResultManager.UpdateInput(mMelEdInterf.mState);
            }
            else
            {
                //Button set 2
                if(mButtPitchDetect.mbOn)
                {
                    mButtPitchDetect.mbOn = false;
                }
            }

            if (mNewEditMgr.Active)
            {
                //mMelEdInterf.mState.mbAllowInputPlayArea = false;
                mNewEditMgr.Update(mMelEdInterf.mState);
            }
            else
            {
                if (mEditModeToggle.mbOn)
                {
                    mEditModeToggle.mbOn = false;
                }
            }

            if (mMelEdInterf.mState.mBPM_Updated)
            {
                mMelEdInterf.mState.mBPM_Updated = false;
                mBPM.ButtonText = string.Format("BPM {0}", mMelEdInterf.BPM);//TODO simplest way to make sure button show last loaded BPM for now
                m_BPMSlider.SetSliderToNewValue((int)mMelEdInterf.BPM);
            }

            ////HAVE TO UPDATE THE mChangeViewTrack BUTTON AGAIN IN HERE WHEN IT'S ON, BIT ANNOYING
            //if (mMidiTrackSelect.Update(mMelEdInterf.mState))
            //{
            //    //While track grid is showing comes through here
            //    //if (mChangeViewTrack.Update(mMelEdInterf.mState))
            //    //{
            //    //   if(!mChangeViewTrack.mbOn)
            //    //   {
            //    //       mMidiTrackSelect.SetInactive();
            //    //   }
            //    //}

            //    mMelEdInterf.mState.mbAllowInputPlayArea = false;
            //    return false; //dont update any other button while this active
            //}

            //HAVE TO UPDATE THE mLoad BUTTON AGAIN IN HERE WHEN IT'S ON, BIT ANNOYING
            if (mMelEdInterf.GetSaveLoad().Update(mMelEdInterf.mState))
            {
                //While track grid is showing comes through here
                if (mLoad.Update(mMelEdInterf.mState))
                {
                    if (!mLoad.mbOn)
                    {
                        mMelEdInterf.GetSaveLoad().SetInactive();
                    }
                }
                mSquareCursor.UpdateTextureChanges(mMelEdInterf.mState);

                mMelEdInterf.mState.mbAllowInputPlayArea = false;
                return false; //dont update any other button while this active
            }
            else if(mLoad.mbOn)
            {
                mLoad.mbOn = false;
            }


            if (!mMelEdInterf.mState.IsDrumMachineTrack() && mInstSel.Update(mMelEdInterf.mState))
            {
                if (mMidiMgr != null)
                {
                    if(mInstSel.mbOn)
                    {
                        mMelEdInterf.mState.mCurrentButtonUpdate = mInstSel.mMask; //set to only this button updates
                        mInstrumentSelect.SetActive();
                    }
                    else
                    {
                        mMelEdInterf.mState.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_ALL; //allow all
                        mInstrumentSelect.SetInActive();
                    }
                }
            }
            if(mInstSel.mbOn && !mInstrumentSelect.Active)
            {
                mInstSel.mbOn = false;
                mMelEdInterf.mState.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_ALL; //allow all
            }

            if (mStartStop.Update(mMelEdInterf.mState))
            {
                if (mMelEdInterf != null)
                {
                    mMelEdInterf.StopStart();

                    if(!mStartStop.mbOn)
                    {
                        mMelEdInterf.mState.StopAllNotes(); //TODO - bit high level UI for this!
                    }
                }
            }
            if (mRewind.Update(mMelEdInterf.mState))
            {
                if (mMelEdInterf != null)
                {
                    mMelEdInterf.Rewind();
                    mRewind.mbOn = false; //Keep it off since button is once only
                    // TODO might need mMelEdInterf.mState.StopAllNotes(); //TODO - bit high level UI for this!
                }
            }

            bool bLoadTurnedOn = false;
            if (mSave.Update(mMelEdInterf.mState))
            {
                if (mMelEdInterf != null)
                {
#if WINDOWS
                    if (mSave.mbOn)
                    {
                        mMelEdInterf.mState.StopAllNotes(); //Blatt out a clear to stop notes being stuck on 
                        mMelEdInterf.GetSaveLoad().SaveDialogFile(mMelEdInterf.mState);
                    }
#else
                    mMelEdInterf.Save(mSave.mbOn);
#endif
                }
            }
            if (mLoad.Update(mMelEdInterf.mState))
            {
                if (mMelEdInterf != null)
                {
                    if (mLoad.mbOn)
                    {
                        bLoadTurnedOn = true;
#if WINDOWS
                         mMelEdInterf.mState.StopAllNotes(); //Blatt out a clear to stop notes being stuck on 
                         mMelEdInterf.GetSaveLoad().LoadDialogFile(mMelEdInterf.mState);
#else
                        mMelEdInterf.GetSaveLoad().SetActive(mMelEdInterf.mState);
#endif
                    }
                }
            }

            //if (mChangeViewTrack.Update(mMelEdInterf.mState))
            //{
            //    if (mMelEdInterf != null)
            //    {
            //        if (mChangeViewTrack.mbOn)
            //        {
            //            mMidiTrackSelect.SetActive();
            //        }
            //     }
            //}
            if (mBPM.Update(mMelEdInterf.mState))
            {
                if (mBPM.mbOn)
                {
                    InitScrollWheelDiff(mMelEdInterf.mState.input);
#if ANDROID
                    mMelEdInterf.mState.mCurrentButtonUpdate = mBPM.mMask; //set to only this button updates
                    m_BPMSlider.SetVisible(true);
#endif
                }
                else
                {
#if ANDROID
                    mMelEdInterf.mState.mCurrentButtonUpdate = mBPM.mMask; //set to only this button updates
                    mMelEdInterf.mState.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_ALL; //allow all
                    m_BPMSlider.SetVisible(false);
#endif
                }
            }

            //OLD SCROLL WHEEL METHOD
            //else if (mKeyNoteLines.Update(mMelEdInterf.mState))
            //{
            //    mMelEdInterf.SetKeyNotesOnlyMode(mKeyNoteLines.mbOn, mMelEdInterf.mState.mKeyRangeIndexSelected);
            //    if (!mKeyNoteLines.mbOn)
            //    {
            //        mKeyNoteLines.ButtonText = KEY_RANGE_TEXT;
            //    }
            //    else
            //    {
            //        mKeyNoteLines.ButtonText = mMelEdInterf.GetKeyName(mMelEdInterf.mState.mKeyRangeIndexSelected);
            //    }
            //}

            //if (mKeyNoteLines.mbOn)
            //{
            //    int newKeyRange = GetScrollWheelDiff(mMelEdInterf.mState.input, mMelEdInterf.mState.mKeyRangeIndexSelected);

            //    if (newKeyRange != mMelEdInterf.mState.mKeyRangeIndexSelected)
            //    {
            //        mMelEdInterf.SetKeyNotesOnlyMode(mKeyNoteLines.mbOn, newKeyRange);
            //        mKeyNoteLines.ButtonText = mMelEdInterf.GetKeyName(mMelEdInterf.mState.mKeyRangeIndexSelected);
            //    }
            //}

            //QUANT - TODO
            //if (mQuant.Update(mMelEdInterf.mState))
            //{
            //    if (mQuant.mbOn)
            //    {
            //        mMelEdInterf.mState.mCurrentButtonUpdate = mQuant.mMask; //set to only this button updates
            //        m_QuantizeSlider.SetVisible(true);
            //    }
            //    else
            //    {
            //        mMelEdInterf.mState.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_ALL; //allow all
            //        m_QuantizeSlider.SetVisible(false);
            //    }
            //}

            //Button set 2
            foreach (Button but in mAllUpdateButtons)
            {
                if (but.Update(mMelEdInterf.mState))
                {
                    //but.mbOn = false;
                }

            }
            //BUTTON SET 2
            if (mToggleButtonSet.mbOn)
            {
                if (mSetPanVolumeTickBox.Update(mMelEdInterf.mState))
                {
                    if (mSetPanVolumeTickBox.mbOn)
                    {
                        mMelEdInterf.mState.mCurrentButtonUpdate = mSetPanVolumeTickBox.mMask; //set to only this button updates
                        m_VolPanSelector.Start(mMelEdInterf.mState);
                    }
                    else
                    {
                        mMelEdInterf.mState.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_ALL; //allow all
                        m_VolPanSelector.Visible = false;
                    }
                }

                //Button set 2
                foreach (Button but in mButtonSet2UpdateButtons)
                {
                    if (but.Update(mMelEdInterf.mState))
                    {
                        but.mbOn = false;
                    }
                }

                if (mButtPitchDetect.Update(mMelEdInterf.mState))
                {
                    //mMelEdInterf.Undo();

                    if (mButtPitchDetect.mbOn)
                    {
                        m_PitchDetectResultManager.Start(mMelEdInterf.mState); ;
                    }
                    else
                    {
                        m_PitchDetectResultManager.Stop(mMelEdInterf.mState); //allow?
                    }
                }
            }

            //BUTTON SET 1
            if (!mToggleButtonSet.mbOn && mEditModeToggle.Update(mMelEdInterf.mState) || bLoadTurnedOn)
            {
                if(bLoadTurnedOn)
                {
                    bLoadTurnedOn = false;
                    mEditModeToggle.mbOn = false;
                }
                //mMelEdInterf.ClearAll();
                if (mEditModeToggle.mbOn)
                {
                    mNewEditMgr.StartKeyScaleMode(mMelEdInterf.mState); 
                }
                else
                {
                    mNewEditMgr.Stop(mMelEdInterf.mState);
                }
            }

            if(!mEditModeToggle.mbOn)
            {
                bool bExternalToggle = false;
#if WINDOWS
                if(m_InputState.MiddleUp)
                {
                    bExternalToggle = true;
                    mRClickSetPlayHead.mbOn = !mRClickSetPlayHead.mbOn;
                }
#endif

                if (bExternalToggle || mRClickSetPlayHead.Update(mMelEdInterf.mState))
                {
                    mMelEdInterf.mState.mbPlacePlayHeadWithRightTouch = mRClickSetPlayHead.mbOn;

                    if (mMelEdInterf.mState.mNoteUI.mMusicPreviewer != null)
                    {
                        mMelEdInterf.mState.mNoteUI.mMusicPreviewer.SetBasicMode(mRClickSetPlayHead.mbOn);
                    }

                }
            }

 
            //if (mChangeViewTrack.mbOn != mMidiTrackSelect.Active)
            //{
            //    mChangeViewTrack.mbOn = mMidiTrackSelect.Active;
            //    mChangeViewTrack.ButtonText = string.Format("Track {0}", mMelEdInterf.mState.mCurrentTrackPlayArea + 1);
            //}


            if(!mButtPitchDetect.mbOn && !mSetPanVolumeTickBox.mbOn && !mDrumMachinePopup.Active) //&& !mEditModeToggle.mbOn) // && !mChangeViewTrack.mbOn) //TODO Use button mask or somthing to prevent inputs on param bars?
            {
                bool bDropDownOpen = false;
                /////////////////////////////////////////////////////////////////////////////
                //KEY RANGE CHANGE
                //KEY RANGE CHANGE
                //KEY RANGE CHANGE
                //KEY RANGE CHANGE
                bDropDownOpen = mKeyNotesKeyPbb.mBarMode | mModeTypePbb.mBarMode | mTrackSelectPbb.mBarMode | mChordTypePbb.mBarMode | mRiffSelectbb.mBarMode | mDrumPresetSelectPbb.mBarMode | mAlternateSongsPbb.mBarMode;

                if (ParamButtonBar.Update(mMelEdInterf.mState, mKeyNotesKeyPbb))
                {
                }
                else if (ParamButtonBar.Update(mMelEdInterf.mState, mModeTypePbb))
                {
                }

                if (ParamButtonBar.Update(mMelEdInterf.mState, mTrackSelectPbb))
                {
                }
                else if (ParamButtonBar.Update(mMelEdInterf.mState, mChordTypePbb))
                {
                }

                if (!mEditModeToggle.mbOn)
                {
                    bool bRiffSelOn = false;
                    bool bDrumSelOn = false;
                    if (mMelEdInterf.mState.IsDrumMachineTrack())
                    {
                        if (ParamButtonBar.Update(mMelEdInterf.mState, mDrumPresetSelectPbb))
                        {
                            bDrumSelOn = true;
                        }
                    }
                    else
                    {
                        if (ParamButtonBar.Update(mMelEdInterf.mState, mRiffSelectbb))
                        {
                            bRiffSelOn = true;
                        }
                    }

                    if(!(bRiffSelOn || bDrumSelOn) )
                    {
                        if (ParamButtonBar.Update(mMelEdInterf.mState, mAlternateSongsPbb))
                        {

                        }
                    }
                }

                if (bDropDownOpen)
                {
                    mMelEdInterf.mState.mbAllowInputPlayArea = false;
                    mMelEdInterf.mState.BlockBigGreyRectangle = true;
                }
                mMelEdInterf.mState.mbPreventTopLineInput = bDropDownOpen;
            }

            if(!mEditModeToggle.mbOn)
            {
                mToggleButtonSet.Update(mMelEdInterf.mState); //TODO just leave it toggling?
                mToggleDrumTrack.Update(mMelEdInterf.mState); //TODO just leave it toggling?
            }

            mSquareCursor.Update(mMelEdInterf.mState);
 
            return true;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Gray);

            mMelEdInterf.Draw(spriteBatch);

            foreach (Button but in mMainButtonSet)
            {
                but.Draw(spriteBatch);
            }

            if (mToggleButtonSet.mbOn)
            {
                foreach (Button but in mButtonSet2)
                {
                    but.Draw(spriteBatch);
                }
            }
            else
            {
                foreach (Button but in mButtonSet1)
                {
                    but.Draw(spriteBatch);
                }
            }

            if (!mEditModeToggle.mbOn)
            {
                foreach (Button but in mAllEditSpaceButtons)
                {
                    but.Draw(spriteBatch);
                }
            }

            mSquareCursor.Draw(spriteBatch);

            mModeTypePbb.Draw(spriteBatch); //these drop downs can overlap the square cursor
            mKeyNotesKeyPbb.Draw(spriteBatch);
            mChordTypePbb.Draw(spriteBatch);
            mTrackSelectPbb.Draw(spriteBatch);
            mAlternateSongsPbb.Draw(spriteBatch);
            if (mMelEdInterf.mState.IsDrumMachineTrack())
            {
                mDrumPresetSelectPbb.Draw(spriteBatch);
            }
            else
            {
                mRiffSelectbb.Draw(spriteBatch);
            }

            mMelEdInterf.DrawOverlay(spriteBatch);

            mInstrumentSelect.Draw(spriteBatch);
            mMidiTrackSelect.Draw(spriteBatch);

            mMelEdInterf.GetSaveLoad().Draw(spriteBatch);

            mKeyboardAndMidiCommonControl.Draw(spriteBatch);

            mLoopSaveTickBox.Draw(spriteBatch); //I know bit wasteful, already in the mAllButtons list above but want to keep in that list for update button input but need to draw here again over overlay

            m_BPMSlider.Draw(spriteBatch);
            m_QuantizeSlider.Draw(spriteBatch);
            m_VolPanSelector.Draw(spriteBatch);
            m_PitchDetectResultManager.Draw(spriteBatch);
            mNewEditMgr.Draw(spriteBatch);
            if (mMelEdInterf.mState.IsDrumMachineTrack())
            {
                mDrumMachinePopup.Draw(spriteBatch);
            }

            spriteBatch.Begin();

            string drawString = mMelEdInterf.GetSaveLoadFileName();
            if (mRClickSetPlayHead.mbOn)
            {
                drawString = mMelEdInterf.GetChordString();
            }

            if(mMelEdInterf.mState.mNoteUI.mMusicPreviewer!=null)
            {
                mMelEdInterf.mState.mNoteUI.mMusicPreviewer.MainHudDraw(spriteBatch);
            }

            spriteBatch.DrawString(font, drawString, new Vector2(FILENAME_X, FILENAME_Y), Color.Black);

            //spriteBatch.DrawString(font, mMelEdInterf.GetChordString(), new Vector2(CHORD_NAME_X, CHORD_NAME_Y), Color.Black);

            int kbNameX = FILENAME_X + PlayArea.WIDTH_DRAW_AREA - (int)font.MeasureString(mKeyboardAndMidiCommonControl.mMidiKeyboardName).X;
            spriteBatch.DrawString(font, mKeyboardAndMidiCommonControl.mMidiKeyboardName, new Vector2(kbNameX, FILENAME_Y), Color.Black);
            spriteBatch.End();


            base.Draw(gameTime);
        }
    }
}
