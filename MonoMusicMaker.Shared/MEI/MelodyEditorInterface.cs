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
using Jacobi.Vst.Core;
using Jacobi.Vst.Host.Interop;
using System.Threading;
using System.Windows.Forms;

namespace MonoMusicMaker //.MEI
{
    using ListRegionSaveData = List<AIChordRegion.AIRegionSaveData>; //Redeclared
    using ListDrumSaveData = List<AIDrumRegion.AIDrumSaveData>;
    using ListParamSaveData = List<ParameterNodeRegion.ParameterNodeRegionSaveData>;

    //Jacobi.Vst.Host.Interop.VstPluginContext;
    
    public class MelodyEditorInterface
    {
        public const float BEAT_PIXEL_WIDTH = 80f;
        public const float INV_BEAT_PIXEL_WIDTH = 1.0f/80f;
        public const int TRACK_START_Y = 220; // PLay area start
        public const int TRACK_START_X = 30;

        const float MIN_MEI_SCALE = 0.2f;
        const float MAX_MEI_SCALE = 8f;

        public PlayAreaPipelineManager mPAPipeLine = new PlayAreaPipelineManager();
        public AIChordManager mAIChordMgr = new AIChordManager();
        public PresetManager mPresetManager = new PresetManager();
        public DrumPresetManager mDrumPresetManager = new DrumPresetManager();
        public SaveSwitchingManager mSaveSwitchManager = new SaveSwitchingManager();
        public EditManager mEditManager = null; // will be set from MainMusicManager init
        public PluginAudioManager mPluginManager = new PluginAudioManager();

        public const float QUANT_NONE           = 0f;
        public const float QUANT_EIGHTH_NOTE    = 1.0f / 8;
        public const float QUANT_QUARTER_NOTE   = 1.0f / 4;
        public const float QUANT_HALF_NOTE      = 1.0f / 2;
        public const float QUANT_FULL_NOTE      = 1.0f;

        public float BPM { get; set; } = 120f;
        public float QuantiseSetting { get; set; } = QUANT_EIGHTH_NOTE;
        //const float BAR_LOOP_RANGE_BEATS = 16f*3;
        public float mMS_Per_Beat;
        public float mBeat_Per_MS;
        float mPlayHeadPos = 0;  //in beats so make it float for precision
        int mCurentTimeMs = 0;

        const float TR_W = 180f;
        const float TR_H = 80f;
        Rectangle mTestRect;
        float mfTestRectScale = 1f;

        public MEIState mState;
        List<PlayArea> mPlayAreas = new List<PlayArea>();
        PlayArea mCurrentPlayArea;
        SaveLoad mSaveLoad;

        AIChord mAIChordObject;

        struct playAreaSet //used to collect number of play areas to gether for editing loops
        {
            public int currentIndex;
            public int startIndex;
            public int endIndex;
        }

        playAreaSet mPlaySet;

        public enum UIButtonMask
        {
            UIBM_None =  0,
            UIBM_Play = 1 << 0,
            UIBM_Inst = 1 << 1,
            UIBM_Start  = 1 << 2,
            UIBM_Save   = 1 << 3,
            UIBM_Load   = 1 << 4,
            UIBM_Change = 1 << 5,
            UIBM_Undo   = 1 << 6,
            UIBM_Quant  = 1 << 7,
            UIBM_Mode   = 1 << 8,
            UIBM_BPM    = 1 << 9,
            UIBM_Popup = 1 << 10,
            UIBM_Tick = 1 << 11,
            UIBM_PanVol = 1 << 12,
            UIBM_PitchDet = 1 << 13,
            UIBM_EditMode = 1 << 14,
            UIBM_EditControl = 1 << 15,
            UIBM_DrumMachine = 1 << 16,
            UIBM_ToggleButtons = 1 << 17,
            UIBM_ToggleDrums = 1 << 18,
            UIBM_NukeAll = 1 << 19,
            UIBM_ToggNoteSlideVelocity = 1 << 20,
            UIBM_ToggParam = 1 << 21,
            UIBM_ClearPlayOffset = 1 << 22,
            UIBM_PlayAreaRecMode = 1 << 23,
            UIBM_Rewind = 1 << 24,
            UIBM_VST_Switch = 1 << 25,

            UIBM_ALL = 
                UIBM_Play | UIBM_Inst | UIBM_Start | UIBM_Save |
                UIBM_Load | UIBM_Change | UIBM_Undo | UIBM_Quant | 
                UIBM_Mode | UIBM_BPM | UIBM_Popup | UIBM_Tick | 
                UIBM_PanVol | UIBM_PitchDet |UIBM_EditMode | 
                UIBM_EditControl | UIBM_DrumMachine | UIBM_ToggleButtons |
                UIBM_ToggleDrums | UIBM_NukeAll | UIBM_ToggNoteSlideVelocity | UIBM_ToggParam | UIBM_ClearPlayOffset | UIBM_PlayAreaRecMode | UIBM_Rewind | UIBM_VST_Switch,
        }

        public bool mbNotEmpty = false;

        public void ClearAll()
        {
            foreach (PlayArea pa in mPlayAreas)
            {
                pa.ResetBarLoopRange();
                pa.Clear();
                pa.ClearAIRegions();
                pa.ClearParamNodeManager();

                if (pa.GetTopLineDrumAI()!=null)
                {
                    pa.ClearDrumRegions();
                }

                SetInstrument(pa.mInstrument, pa.mChannel);
                SetVolume(pa.mVolume, pa.mChannel);
                SetPan(pa.mPan, pa.mChannel);

                string buttonText = string.Format("Track {0}", pa.mChannel);
                mState.mMidiTrackSelect.SetButtonText(pa.mChannel, buttonText);           
            }
            mbNotEmpty = false;
        }

        public void Undo()
        {

        }

        ///////////////////////////////////////////////////////////////////////////////////////// 
        ///////////////////////////////////////////////////////////////////////////////////////// 
        /// START MESTATE
        /// 
        public class MEIState
        {
            bool mbUsePlugin = true;
            public int mPitchBend = 0;
            public int mControllerBend = 0;

            public DebugPanel mDebugPanel;
            const int NOTE_HELD_TIME_FOR_DELETE_MS = 700;
            const int NOTE_DOUBLE_CLICK_TIME_MS = PsiKlopsInput.DOUBLE_CLICK_TIME_MS;
            public bool EditMode = false;
            public bool mbPreventTopLineInput = false;
            public bool mbAllowInputPlayArea = false;
            public bool mbPlayAreaChanged = true; //simplest way to get the square cursor - i.e. right hand side grid - set up when first start
            public bool mbEditNotSaved = false; //Set true when lay area changed and not saved out
            public bool mbRequestDrumMachine = false;
            public bool mSoloCurrent = false;
            public bool mbRecording = false;
            public bool mbPlacePlayHeadWithRightTouch = false;
            public bool mbKeyRangePlayAreaMode = false;
            public int mKeyRangeIndexSelected = 0; //0 = C, 1 = C # etc
            public int mKeyRangeScaleTypeSelected = 0; // Major
            public int mChordTypeSelected = 0; // I, ii, ii, IV, V vi, vii
            public bool mbNoteEditInVelocityMode = false; //When default false thismeasns in non ply enter mode we can slide the notes up/down left/right - otherwise velocity mode allow setting the note velocity up/down
            public bool PreviewPlayerState = false;


            public NoteChordGroup mNCG = null; //if some chord is being laid down on a speical line this will track the update and processing

            public void PlayAreaChanged(bool bValue, bool bEditNotSaved = true, bool bChangeEdit = true)
            {
                mbPlayAreaChanged = bValue;
                if(bChangeEdit)
                {
                    mMeledInterf.mbNotEmpty = true; //TODO Check for actual notes ? Could be empty if notes removed
                    mbEditNotSaved = bEditNotSaved;
                }
            }

            public void PrepareForLoadFile()
            {
                mSoloCurrent = false;
                GetMidiBase().StopAllNotes();
            }

            public void ResetBeatPos(float newBeatPos)
            {
                fMainCurrentBeat = newBeatPos;
                mCurentPlayingTimeMs = (int)(mMeledInterf.mMS_Per_Beat * fMainCurrentBeat);
            }

            //MIDI IN
            public int mMidiInNote = 0;
            public bool mMidiThrough = true;


            public bool mbPlayAreaUpdate = false;
            public bool mBPM_Updated = false;
            public bool mbMIDIFilePrefix = true;
            public bool mbSaveMIDIWithLoop = false; //True will save out notes in the loops that are set for a bar period defined - to be done - fixed for now probs 64 beats?
            public bool Playing = false;
            public SpriteBatch mSB;
            public int mCurrentTrackPlayArea = 0;

            public int mCurrentGameTimeMs;
            public MidiTrackSelect mMidiTrackSelect;
            public PsiKlopsInput input;
            public float scale;
            public int mCurentPlayingTimeMs;
            public float fMainCurrentBeat;
            //public MidiBase mStateMidiBase;
            public MidiManager mStateMidiMgr;
            public MelodyEditorInterface mMeledInterf;
            public UIButtonMask mCurrentButtonUpdate = UIButtonMask.UIBM_ALL;
            public UIButtonMask mMaskToSetButton = UIButtonMask.UIBM_None; //this can be set anywhere to a single button only - probably and when that button is update it gets removed - or probably removed at end of update anyway?

            public NoteUI mNoteUI = null;
            public ParamPopUp mAIRegionPopup = null;
            public DrumMachinePopUp mAIDrumRegionPopup = null;
            public bool mbRegionRefreshToEdit = false;
            public int mSetDownTime;

            public bool BlockBigGreyRectangle = false;

            public SquareCursor mSquareCursor;

            //From twit
            public Button mButCallback = null;
            public GameTime gameTime;
            public UIButtonMask mCurrentButtonMaskedOff = UIButtonMask.UIBM_None; //will set buttons with this mask type off


            public bool IsDrumMachineTrack()
            {
                return mCurrentTrackPlayArea == MidiTrackSelect.DRUM_MACHINE_PLAY_AREA_INDEX;
            }

            public void SetSquareCursorToNewPlayHeadPos(ref float fPlayHeadPos)
            {
                mSquareCursor.SetToNewPlayHeadPosition(ref fPlayHeadPos);
            }

            public void SetSquareCursor(SquareCursor squareCursor)
            {
                mSquareCursor = squareCursor;
            }

            public void SetSquareCursorFromPlayArea(PlayArea pa)
            {
                mSquareCursor.SetFromPlayArea(pa);
            }

            public void SetDownNote(Note n)
            {
                if(mNoteUI.mDoubleTapNote != null) //when set down happens need to refresh length
                {
                    mNoteUI.mNoteBeatLengthStart = mNoteUI.mDoubleTapNote.BeatLength;
                }
                // 25th April 2021 - just get rid of double tap by this comment out - will now be implementing move of notes on special bank 1 button to toggle between velocity or slide raise/lower note
                //if (mNoteUI.mDownNote == n)
                //{
                //    if (mCurrentGameTimeMs - mSetDownTime < NOTE_DOUBLE_CLICK_TIME_MS)
                //    {
                //        if (n != null)
                //        {
                //            if (mNoteUI.mDoubleTapNote != mNoteUI.mDownNote)
                //            {
                //                if (mNoteUI.mDoubleTapNote != null)
                //                {
                //                    mNoteUI.mDoubleTapNote.SetDefaultCol();
                //                }
                //                mNoteUI.mDoubleTapNote = mNoteUI.mDownNote;
                //                mNoteUI.mNoteBeatLengthStart = mNoteUI.mDoubleTapNote.BeatLength;
                //                mNoteUI.mDoubleTapNote.Col = Color.Green; ;
                //            }
                //        }
                //        else
                //        {
                //            if (mNoteUI.mDoubleTapNote != null)
                //            {
                //                mNoteUI.mDoubleTapNote.SetDefaultCol();
                //                mNoteUI.mDoubleTapNote = null;
                //            }
                //        }
                //    }
                //}
                mNoteUI.mDownNote = n;
                mSetDownTime = mCurrentGameTimeMs;
            }

            public bool UpdateHeldNote(Note heldNote)
            {
                if(heldNote != null && mNoteUI.mDownNote == heldNote)
                {
                    if(mCurrentGameTimeMs - mSetDownTime > NOTE_HELD_TIME_FOR_DELETE_MS)
                    {
                        return true;
                    }
                }

                return false;
            }


            //MIDI INTERFACE IS HERE NOW!!!!
            public MidiBase GetMidiBase() { return mStateMidiMgr.GetMidiBase(); }
            public void NoteOn(int channel, int NoteNum, int velocity)
            {
                if(mbUsePlugin)
                {
                    mMeledInterf.mPluginManager.NoteOn(channel, NoteNum, velocity);
                }
                else
                {
                    GetMidiBase().NoteOn(channel, NoteNum, velocity);
                }
            }
            public void NoteOff(int channel, int NoteNum)
            {
                if (mbUsePlugin)
                {
                    mMeledInterf.mPluginManager.NoteOff(channel, NoteNum);
                }
                else
                {
                    GetMidiBase().NoteOff(channel, NoteNum);
                }
            }
            public void StopAllNotes(int exceptChannel = -1)
            {
                if (mbUsePlugin)
                {
                    mMeledInterf.mPluginManager.StopAllNotes(exceptChannel);
                }
                else
                {
                    GetMidiBase().StopAllNotes(exceptChannel);
                }
            }
            public void ClearAllNotes(int channel)
            {
                if (mbUsePlugin)
                {
                    mMeledInterf.mPluginManager.ClearAllNotes(channel);
                }
                else
                {
                    GetMidiBase().ClearAllNotes(channel);
                }
            }
            public void HideKeyboard()
            {
                GetMidiBase().HideKeyboard();
            }
            public void ShowKeyboard()
            {
                GetMidiBase().ShowKeyboard();
            }
        }
        /// 
        /// END MESTATE
        /////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////

        public void EnableParamLine(bool bEnable)
        {
            foreach (PlayArea pa in mPlayAreas)
            {
                pa.EnableParam(bEnable);
            }
        }
        public string GetKeyName(int index)
        {
            return mAIChordMgr.GetKeyName(index);
        }
        public string[] GetKeyNameArray(int index)
        {
            return mAIChordMgr.GetKeyNameArray();
        }

        public SaveLoad GetSaveLoad()
        {
            return mSaveLoad;
        }

        public string GetSaveLoadFileName()
        {
            if(mState.mbEditNotSaved)
            {
                return mSaveLoad.LoadSaveFileName + "*";
            }
            return mSaveLoad.LoadSaveFileName;
        }

        public void SetSaveName(string saveName)
        {
            mState.mbEditNotSaved = false;
            mSaveLoad.LoadSaveFileName = saveName;
        }

        public void SetPlayAreaFontAndSpriteBatch(SpriteFont font, SpriteBatch sb)
        {
            mCurrentPlayArea.SetFont(font);

            foreach(PlayArea pa in mPlayAreas)
            {
                pa.SetFont(font);
            }
            mState.mSB = sb;
        }

        public List<PlayArea> GetPlayAreas()
        {
            return mPlayAreas;
        }

        public void SetBPM(int bpm)
        {
            BPM = bpm;
            mMS_Per_Beat = 1000f * 60f / BPM;
            mBeat_Per_MS = (float)(bpm/(1000f * 60f ));
            foreach (PlayArea pa in mPlayAreas)
            {
                pa.mMS_Per_Beat = mMS_Per_Beat;
            }
        }
        public void SetQuant(int quant)
        {
            if(quant==0)
            {
                QuantiseSetting = QUANT_NONE;
            }
            else if (quant == 1)
            {
                QuantiseSetting = QUANT_EIGHTH_NOTE;
            }
            else if (quant==2)
            {
                QuantiseSetting = QUANT_QUARTER_NOTE;
            }
            else if(quant == 3)
            {
                QuantiseSetting = QUANT_HALF_NOTE;
            }
            else if (quant == 4)
            {
                QuantiseSetting = QUANT_FULL_NOTE;
            }
        }

        public void SetAllPlayAreaLoops(List<PlayArea.LoopRange> lrl, bool bCurrentAreaOnly = false)
        {
            if(bCurrentAreaOnly)
            {
                PlayArea.LoopRange lr = lrl[0];
                mCurrentPlayArea.SetUpBarsPostLoadOrUndo(lr);
                return;
            }

            int index = 0;
            foreach(PlayArea.LoopRange lr in lrl)
            {
                mPlayAreas[index].SetUpBarsPostLoadOrUndo(lr);
                index++;
            }
        }

        public void SetAllPlayAreaRegionData(List<ListRegionSaveData> lrsd, bool bCurrentAreaOnly = false)
        {
            if (bCurrentAreaOnly)
            {
                ListRegionSaveData rsd = lrsd[0];
                mCurrentPlayArea.SetRegionSaveData(rsd);
                return;
            }

            int index = 0;
            foreach (ListRegionSaveData rsd in lrsd)
            {
                mPlayAreas[index].SetRegionSaveData(rsd);
                index++;
            }
        }

        public void SetAllPlayAreaParamRegionData(List<ListParamSaveData> lnsd, bool bCurrentAreaOnly = false)
        {
            if (bCurrentAreaOnly)
            {
                ListParamSaveData pnsd = lnsd[0];
                mCurrentPlayArea.SetPNRSaveData(pnsd);
                return;
            }

            int index = 0;
            foreach (ListParamSaveData pnsd in lnsd)
            {
                mPlayAreas[index].SetPNRSaveData(pnsd);
                index++;
            }
        }

        public void SetAllPlayAreaStartDrawNoteLineToTopActualNotes()
        {
            int index = 0;
            foreach (PlayArea pa in mPlayAreas)
            {
                pa.SetPlayAreaStartDrawNoteLineToToTopActualNotes();
                index++;
            }
        }

        public void SetDrumChannelDrumData(ListDrumSaveData ldsd)
        {
            mPlayAreas[MidiTrackSelect.DRUM_MACHINE_PLAY_AREA_INDEX].SetDrumSaveData(ldsd);
        }


        public void LoadFromMidiCollection(MidiEventCollection events, bool bFixStrangeTrackOffset = false)
        {
            foreach (PlayArea pa in mPlayAreas)
            {
                pa.HasNotes = false;
                pa.LoadFromMidiCollection(events, PlayArea.FULL_RANGE, bFixStrangeTrackOffset);
                if (pa.HasNotes)
                {
                    SetInstrument(pa.mInstrument, pa.mChannel);
                    SetVolume(pa.mVolume, pa.mChannel);
                    SetPan(pa.mPan, pa.mChannel);
                    string instName = InstrumentSelect.GetPatchString(pa.mInstrument);
                    mState.mMidiTrackSelect.SetButtonText(pa.mChannel, instName, true);
                }
                else
                {
                    string buttonText = string.Format("Track {0}", pa.mChannel);
                    mState.mMidiTrackSelect.SetButtonText(pa.mChannel, buttonText);
                }

                //will load new regions from JSON data
                pa.ClearAIRegions();
                pa.ClearParamNodeManager();
            }
            mState.mBPM_Updated = true;
            SetBPM(mPlayAreas[0].GetReadBPM());
        }

        public void LoadMidiFile(MidiFile newFile, bool bCurrentAreaOnly = false)
        {
            if(newFile.FileFormat == 0)
            {
                System.Diagnostics.Debug.Assert(false, string.Format("Dont expect this format any more newFile.Tracks {0} ", newFile.Tracks));

                //if (mCurrentPlayArea != null)
                //{
                //    mCurrentPlayArea.LoadMidiFile(newFile);
                //    int channel = 1;
                //    SetInstrument(mCurrentPlayArea.mInstrument, channel);
                //}
            }
            else if(newFile.FileFormat == 1)
            {
                //if(mCurrentPlayArea.HACKDRUMSSetFromMidiTrackEvents(newFile))
                //{
                //    SetBPM(mCurrentPlayArea.GetReadBPM());
                //    mState.mBPM_Updated = true;
                //    return;
                //}

                if(bCurrentAreaOnly)
                {
                    if(newFile.Tracks > 1)
                    {
                        System.Diagnostics.Debug.Assert(false, string.Format("Too many tracks for current area only load ! {0} ", newFile.Tracks));
                    }
                    LoadPlayAreaFromCollection(mCurrentPlayArea, newFile);
                }
                else
                {
                    foreach (PlayArea pa in mPlayAreas)
                    {
                        if (bCurrentAreaOnly)
                        {
                            if (pa != mCurrentPlayArea)
                            {
                                continue;
                            }
                        }
                        LoadPlayAreaFromCollection(pa, newFile);
                    }
                    mState.mBPM_Updated = true; //when loading all set BPM
                    SetBPM(mPlayAreas[0].GetReadBPM());
                }

                mState.SetSquareCursorFromPlayArea(mCurrentPlayArea);
            }
        }

        void LoadPlayAreaFromCollection(PlayArea pa, MidiFile newFile)
        {
            pa.HasNotes = false;
            pa.LoadFromMidiTrackEvents(newFile);
            if (pa.HasNotes)
            {
                SetInstrument(pa.mInstrument, pa.mChannel);
                SetVolume(pa.mVolume, pa.mChannel);
                SetPan(pa.mPan, pa.mChannel);
                string instName = InstrumentSelect.GetPatchString(pa.mInstrument);
                mState.mMidiTrackSelect.SetButtonText(pa.mChannel, instName, true);
            }
            else
            {
                string buttonText = string.Format("Track {0}", pa.mChannel);
                mState.mMidiTrackSelect.SetButtonText(pa.mChannel, buttonText);
            }

            //TODO will load new regions from JSON data
            pa.ClearAIRegions();
            pa.ClearParamNodeManager();
        }

        public void Rewind()
        {
            const float RESET_POS = 0f;

            mState.ResetBeatPos(RESET_POS);
            foreach (PlayArea pa in mPlayAreas)
            {
                pa.RewindToPos(RESET_POS);
            }
        }

        public void StopStart()
        {
            bool bRestarted = !mState.Playing;

            mState.Playing = !mState.Playing;

            if(bRestarted)
            {
                if (mState.mSoloCurrent)
                {
                    mCurrentPlayArea.RestartAllOnNotes(mState);
                }
                else
                {
                    //Get a list of notes for each play area that were left on and retstart immediatley
                    //to fix issue waiting for long notes to complete that were half way through
                    foreach (PlayArea pa in mPlayAreas)
                    {
                        pa.RestartAllOnNotes(mState);
                    }
                }
            }

            if (!mState.Playing)
            {
                mState.ClearAllNotes(1); //TODO Not sure why I cleared just track 1 here?
            }
        }

        public void Save(bool bOn)
        {
            //mSaveLoad.GatherMidiSave(this);
            if(bOn)
            {
                mState.ShowKeyboard();
            }
            else
            {
                mState.HideKeyboard();
            }
        }

        public void Load()
        {
            //mSaveLoad.GatherMidiSave(this);
        }

        public void SetMidiBase(MidiBase mi)
        {
            //mState.mMidiBase = mi;
        }
        public void SetMidiMgr(MidiManager midiMgr)
        {
            mState.mStateMidiMgr = midiMgr;
        }

        public void SetMelEdInterf()
        {
            mState.mMeledInterf = this;
        }

        public void SetCurrentPlayArea(int selectedTrack)
        {
            //if(selectedTrack == MidiTrackSelect.DRUM_MACHINE_TRACK_NUM) //now selected from top bar  like AI chords
            //{
            //    mState.mbRequestDrumMachine = true;
            //}

            if(selectedTrack !=mPlaySet.currentIndex)
            {
                SetCurrentPlayAreaAndSet(selectedTrack);
                mState.mNoteUI.ResetAll();
                mCurrentPlayArea.SetPlayAreaAsView(mPAPipeLine.Active);
                SetToNewPlayHeadPos(mCurrentPlayArea.mPlayHead.mBeatPos); // mCurrentPlayArea.StartBeat); //so the play head will appear if playing when switched
                mState.SetSquareCursorFromPlayArea(mCurrentPlayArea);
            }
        }

        public int GetInstrument(int channel)
        {
            foreach (PlayArea pa in mPlayAreas)
            {              
                if(pa.mChannel==channel)
                {
                    return pa.mInstrument;
                }
            }

            return 100; //ERROR weird instrument to let us know hopefully! ;)
        }

        public void SetInstrument(int instrument, int channel = -2)
        {
            if(channel==-2)
            {
                channel = mCurrentPlayArea.mChannel;
                mCurrentPlayArea.mInstrument = instrument;
                mCurrentPlayArea.UpdateMidiTrackSelectStatus(mState);
            }
            mState.mStateMidiMgr.MMSetInstrument(instrument, channel);
        }

        public void SetVolume(int volume, int channel = -2)
        {
            mState.mStateMidiMgr.MMSetVolume(volume, channel);
        }
        public void SetPan(int pan, int channel = -2)
        {
            mState.mStateMidiMgr.MMSetPan(pan, channel);
        }

        public void SetMidiCommand(int commandNum, int value, int channel)
        {
            mState.mStateMidiMgr.MMSetCommand( commandNum,  value,  channel);
        }

        public void LoadContent(ContentManager contentMan, SpriteFont font)
        {
            mSaveLoad.LoadContent(contentMan, font);
            mState.mAIRegionPopup.LoadContent(contentMan, font);
            mState.mDebugPanel.LoadContent(contentMan, font);
        }

        public void SetCurrentPlayAreaAndSet(int index)
        {
            const int AREA_RANGE = 4;

            mCurrentPlayArea = mPlayAreas[index];
        
            mPlaySet.currentIndex = index;
            mPlaySet.startIndex = (index/ AREA_RANGE)*AREA_RANGE;
            mPlaySet.endIndex = mPlaySet.startIndex + AREA_RANGE;
        }

        public void Init(PsiKlopsInput input)
        {
            mAIChordObject = new AIChord();

            mSaveLoad = new SaveLoad();
            mSaveLoad.Init();

            mPlayHeadPos = 0;

            mMS_Per_Beat = 1000f * 60f / BPM;
            mBeat_Per_MS = (float)(BPM / (1000f * 60f));

            //mCurrentPlayArea = new PlayArea();
            //mCurrentPlayArea.Init(mMS_Per_Beat);

            for (int i=0; i<MidiTrackSelect.NUM_MIDI_TRACKS;i++)
            {
                PlayArea pa = new PlayArea();
                pa.mChannel = i + 1; //Can't have 0 for NAudio. Also set channel before init so drum sounds get their text set in note line channel 10
                pa.Init(mMS_Per_Beat);
                mPlayAreas.Add(pa);
            }

            SetCurrentPlayAreaAndSet(0);

            //TEST RECT
            //mTestRect = new Rectangle(1400, 400, 90, 40);
            mState = new MEIState();
            mState.scale = 1f;
            mState.input = input;
            //mState.mNo
            mState.mNoteUI = new NoteUI();
            mState.mAIRegionPopup = new ParamPopUp();
            mState.mAIRegionPopup.Init();
            mState.Playing = false;
            mState.mDebugPanel = new DebugPanel();
            mState.mDebugPanel.Visible = true;// DEBUG ONLY !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            mState.mDebugPanel.Init();

            mAIChordMgr.InitPopUpAI(mState.mAIRegionPopup);


            mDrumPresetManager.Init(this);
            mPresetManager.Init(this);
        }


        ///// ////// TEST PARAMETER POP UP BAR
        //public void TestParamBar(Button button, MelodyEditorInterface.MEIState state)
        //{
        //    System.Diagnostics.Debug.WriteLine(string.Format("TestParamBar value {0}", button.mGED.mValue));
        //}

        //void SetUpRegionPopupParamChordBar() //TODO need an ai manager
        //{
        //    List<ButtonGrid.GridEntryData> mMidiTrackGED = new List<ButtonGrid.GridEntryData>();
        //    string[] mTrackNames = new string[16] {    "Major", "Minor", "Maj 7th", "Min 7th", "Maj Sus9", "Min Sus9", "Maj 11th", "Min 11th",
        //                                            "Dim 9", "Maj 1st Inv", "Maj 2nd Inv", "Track 12", "Maj 6th", "Min 6 9", "Dom 7th", "Dom 9th"  };
        //    int count = 0;
        //    int patchCount = 0;

        //    foreach (string str in mTrackNames)
        //    {
        //        ButtonGrid.GridEntryData midiTrackEntry;

        //        midiTrackEntry.mOnString = str;
        //        midiTrackEntry.mValue = count;
        //        midiTrackEntry.mClickOn = TestParamBar;
        //        count++;
        //        mMidiTrackGED.Add(midiTrackEntry);
        //    }

        //    ParamButtonBar pbb = new ParamButtonBar(mMidiTrackGED);

        //    mState.mAIRegionPopup.AddParamBar(pbb);
        //}

        public void Draw(SpriteBatch spriteBatch)
        {
            // TODO: Add your drawing code here
            spriteBatch.Begin();
            mCurrentPlayArea.Draw(spriteBatch);
            //mState.mNoteUI.Draw(spriteBatch);


            spriteBatch.End();
            mState.mDebugPanel.Draw(spriteBatch);

            //TEST RECT
            //spriteBatch.FillRectangle(mTestRect, Color.Yellow);
        }
        public void DrawOverlay(SpriteBatch spriteBatch)
        {
            mState.mAIRegionPopup.Draw(spriteBatch);
            mCurrentPlayArea.DrawOverlay(spriteBatch);
        }
        public List<NoteLine> GetNoteLines()
        {
            return mCurrentPlayArea.GetNoteLines();
        }
        public void SetChordTypeSelected(int chordType, bool bForce = false)
        {
            if (mState.mChordTypeSelected == chordType && !bForce)
            {
                return;
            }

#if WINDOWS
            if(AIChordManager.eChordTypeNum.scroll == (AIChordManager.eChordTypeNum)chordType )
            {
            
            }
#endif
            mState.mChordTypeSelected = chordType;

            mCurrentPlayArea.SetChordTypeSelected(chordType);

        }

        public void SetKeyScaleSelected(int scale)
        {
            if(mState.mKeyRangeScaleTypeSelected == scale)
            {
                return;
            }
            mState.mKeyRangeScaleTypeSelected = scale;

            foreach (PlayArea pa in mPlayAreas)
            {
                if (pa.mChannel == PlayArea.DRUM_CHANNEL_NUM)
                {
                    continue; //leave drum track alone
                }
                pa.SetKeyModeScale(scale);
            }

            if (!mState.mbKeyRangePlayAreaMode)
            {
                return; //dont need to do anything if not in mode
            }

            mCurrentPlayArea.RefreshLinesToOffset();
            mState.mSquareCursor.BlankTexture(); //Blank out areas that my no be covered by fewer lines showing
            mState.PlayAreaChanged(true);
            mState.StopAllNotes();
        }

        public void SetKeyNotesOnlyMode(bool bOn, int key)
        {
            if(key<0)
            {
                key = 0;
            }
            else if (key >= AIChord.OCTAVE_SPAN_SEMITONES)
            {
                key = AIChord.OCTAVE_SPAN_SEMITONES-1;
            }

            mState.mKeyRangeIndexSelected = key;// % AIChord.OCTAVE_SPAN_SEMITONES;

            mState.mbKeyRangePlayAreaMode = bOn;
            foreach (PlayArea pa in mPlayAreas)
            {
                if (pa.mChannel == PlayArea.DRUM_CHANNEL_NUM)
                {
                    continue; //leave drum track alone
                }
                pa.mKeyModeNoteLines = bOn;
                pa.SetRenderedNoteLinesToKeyRequested(key);
            }
            mCurrentPlayArea.RefreshLinesToOffset();

            mState.mSquareCursor.BlankTexture(); //Blank out areas that my no be covered by fewer lines showing
            mState.PlayAreaChanged(true, false, false);
            mState.StopAllNotes();
        }

        public List<NoteLine> GetRenderedNoteLines()
        {
            return mCurrentPlayArea.GetRenderedNoteLines();
        }

        public PlayArea GetCurrentPlayArea()
        {
            return mCurrentPlayArea;
        }

        public void SetStartDrawLine(int startLine)
        {
            mCurrentPlayArea.StartDrawLine = startLine;
            mCurrentPlayArea.RefreshLinesToOffset();
        }

        public void SetToNewPlayHeadPos(float fPlayHeadPos)
        {
            //snap to nearest bar that shows new head pos
            //int barNum = (int)(fPlayHeadPos / PlayArea.BEATS_PER_BAR);
            //float newPos = barNum * PlayArea.BEATS_PER_BAR;
            int barNum = (int)(fPlayHeadPos / PlayArea.NUM_DRAW_BEATS); //Align to whole window draw not just bar
            float newPos = barNum * PlayArea.NUM_DRAW_BEATS;

            mState.SetSquareCursorToNewPlayHeadPos(ref newPos);
            //mState.SetSquareCursorFromPlayArea(mCurrentPlayArea);
            SetStartBeatPos(newPos);
            UpdateTrackOffsetPos(); //even though we haven't shifted track lines up/down we need this to make sure region offset gets represented n texture and main grid
        }

        public void ResetAllPlayHeadOffset()
        {
            foreach (PlayArea pa in mPlayAreas)
            {
                pa.mPlayAreaPlayingTimeOffsetTimeMs = 0;
            }
        }

        public bool PlayAreaRecModeSwitch(bool bOn)
        {
            mPAPipeLine.Active = bOn;

            if(bOn)
            {
                PlayArea.SetPlayAreaAsRecord(mPlayAreas[15]);
                if (!mPAPipeLine.SetSourceDestination(mCurrentPlayArea, mPlayAreas[15]))
                {
                    mPAPipeLine.Active = false;
                    return false;
                }
            }
            else
            {
                PlayArea.SetPlayAreaAsRecord(mCurrentPlayArea);
            }

            return true;
        }

        //////////////////////////////////////////////////////////////////////////////
        //PLUGIN VIEWER
        public PluginHost.MainForm mMainForm= null;
        PluginHost.PluginForm mPluginForm = null;
        PluginHost.EditorFrame mEddlg = null;
        public bool VSTSwitch(bool bIsOn)
        {
            if (mMainForm.PluginThreadRunning())
            {
                if (!bIsOn)
                {
                    if(mEddlg!=null)
                    {
                        try
                        {
                            mEddlg.BeginInvoke(new Action(() => mEddlg.Close())); // use this BeginInvoke to prevent thread exception not calling form accesses on the same thread
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                    return true; //TODO ?
                }
                else
                {
                    return true; //On already - somehow
                }
            }

            if(bIsOn)
            {
                int currentChannel = mCurrentPlayArea != null ? mCurrentPlayArea.mChannel - 1 : -1;

                if (currentChannel >= 0 && mPluginManager.GetNumPluginContext() > currentChannel)
                {

                    VSTPlugin vp = mPluginManager.GetPluginContext(currentChannel);
                    VstPluginContext PluginContext = vp.PluginContext;

                    //PluginHost.PluginForm dlg = new PluginHost.PluginForm();

                    PluginHost.MainForm dlg = mMainForm;

                    dlg = mMainForm;
                    dlg.PluginContext = PluginContext;

                    //dlg.ShowDialog();
                    mEddlg = new PluginHost.EditorFrame
                    {
                        PluginCommandStub = PluginContext.PluginCommandStub
                    };

                    dlg.ThreadShowPlugin(mEddlg);

                    return true;
                }
            }

            return false;
        }

        public void ResetCurrentPlayHeadOffset()
        {
            mCurrentPlayArea.mPlayAreaPlayingTimeOffsetTimeMs = 0;
        }


        public void SetStartBeatPos(float  startPos, bool bSetAllInPlayAreaSet = false)
        {
            mCurrentPlayArea.StartBeat = startPos;
            mCurrentPlayArea.UpdateGridLines();
            mCurrentPlayArea.UpdateNoteLinesPosWhileSelectXShift();

            if(bSetAllInPlayAreaSet)
            {
                for(int i=mPlaySet.startIndex; i < mPlaySet.endIndex; i++)
                {
                    if(i != mPlaySet.currentIndex)
                    {
                        mPlayAreas[i].CopyPlayAreaOffsets(mCurrentPlayArea);
                    }
                }
            }
            //mPlayArea.RefreshLinesToOffset(); //dont nned this if set this then SetStartDrawLine
        }

        public void SetCurrentAreaVolume(float ratio)
        {
            mCurrentPlayArea.mVolume = (int)(ratio*127f);
            SetVolume(mCurrentPlayArea.mVolume, mCurrentPlayArea.mChannel);
        }
        public void SetCurrentAreaPan(float ratio)
        {
            mCurrentPlayArea.mPan = (int)(ratio * 127f);
            SetPan(mCurrentPlayArea.mPan, mCurrentPlayArea.mChannel);
        }

        public void SetTrackOffsetPos(float startPos)
        {
            mCurrentPlayArea.SetTrackFloatPos(startPos);
            UpdateTrackOffsetPos();
        }

        public void UpdateTrackOffsetPos()
        {
            mCurrentPlayArea.RefreshLinesToOffset();
        }

        public string GetChordString()
        {
            if(mState.mbPlacePlayHeadWithRightTouch && mCurrentPlayArea.mNotesOn.Count>0)
            {
                ////Get all the notes under play head
                List<int> chordListOut = new List<int>();

                mAIChordObject.GetNormalisedChordIntervalsFromBottomUp(mCurrentPlayArea.mNotesOn, ref chordListOut);
                string chordName = mAIChordObject.GetChordFromAILookUp(chordListOut.ToArray());
                int lowestNoteIndex = mCurrentPlayArea.mNotesOn.Count - 1;
                int rootNoteNum = mCurrentPlayArea.mNotesOn[lowestNoteIndex];

                if (mAIChordObject.mCurrentInversionLevel > 0)
                {
                    rootNoteNum = mCurrentPlayArea.mNotesOn[lowestNoteIndex - mAIChordObject.mCurrentInversionLevel];
                }
                NoteEvent ne = new NoteEvent(0, 15, MidiCommandCode.NoteOff, rootNoteNum, 0);

                chordName += " [ ";

                foreach (int i in chordListOut)
                {
                    chordName += i.ToString();
                    chordName += " ";
                }
                chordName += "] ";

                string rootNote = ne.NoteName;

                return rootNote + " " + chordName;
            }
            return "";
        }
        ///////////////////////////////////////////////////////////////////////
        /// MOSTLY FOR HANDLING GRAB PLAY HEAD
        public bool UpdateInput(MelodyEditorInterface.MEIState state)
        {
            if(state.mbPlacePlayHeadWithRightTouch && mCurrentPlayArea.mPlayHead != null)
            {
                bool bSecondTouch = state.input.SecondHeld;
                if(bSecondTouch)
                {
                    int x1 = state.input.X;
                    int y1 = state.input.Y;

                    int x2 = state.input.X2;
                    int y2 = state.input.Y2;

                    if (mCurrentPlayArea.mAreaRectangle.Contains(x2, y2))
                    {
                        float rectStartPos = NoteLine.GetBeatFromTouchStatic(x2, false, 0f, mCurrentPlayArea.StartBeat);

                        mCurrentPlayArea.mPlayHead.SetGrabbedPlayHead(state, rectStartPos);
                    }
                }
            }

            return false;
        }

        public bool Update(GameTime gameTime)
        {
            TimeSpan timeSpan = gameTime.ElapsedGameTime;

            MelodyEditorInterface.UIButtonMask startMask = mState.mCurrentButtonUpdate;

            mPluginManager.ProcessAllPluginMidi();
            //Thread.Sleep(2);

            mState.mCurrentGameTimeMs = (int)gameTime.TotalGameTime.TotalMilliseconds;
            if (mState.Playing)
            {
                mState.mCurentPlayingTimeMs += timeSpan.Milliseconds;
            }

            mState.mNoteUI.Active = false; //Set off before checking if touching in area
            foreach (PlayArea pa in mPlayAreas)
            {
                mState.mbPlayAreaUpdate = mCurrentPlayArea == pa;
                pa.Update(mState);

                if (mState.mbPlayAreaUpdate)
                {
                    mState.mSquareCursor.SetPlayHead(pa.mPlayHead.mBeatPos);
                }
            }

            return startMask != mState.mCurrentButtonUpdate;

            //mCurrentPlayArea.Update(mState);

            //TEST RECT
            //float fDrawScale = GetCurrentPinchScale(mState);
            //mTestRect.Size = new Point ((int)(TR_W * fDrawScale), (int)(TR_H * fDrawScale));
        }

        float GetCurrentPinchScale( MEIState state)
        {
            float pinchScale = state.input.GetPinchScale();
            float fDrawScale = state.scale;

            if (state.input.SecondTouchHeld() && pinchScale != 1.0f)
            {
                float newSCale = state.scale * pinchScale;

                if (newSCale > MAX_MEI_SCALE)
                {
                    newSCale = MAX_MEI_SCALE;
                }
                else if (newSCale < MIN_MEI_SCALE)
                {
                    newSCale = MIN_MEI_SCALE;
                }
                fDrawScale = newSCale;
            }
            else if (state.input.SecondTouchUp())
            {
                float newSCale = state.scale * state.input.GetLastPinchScale();

                if (newSCale > MAX_MEI_SCALE)
                {
                    newSCale = MAX_MEI_SCALE;
                }
                else if (newSCale < MIN_MEI_SCALE)
                {
                    newSCale = MIN_MEI_SCALE;
                }
                state.scale = newSCale;
                fDrawScale = newSCale;
            }
            return fDrawScale;
        }
    }
}
