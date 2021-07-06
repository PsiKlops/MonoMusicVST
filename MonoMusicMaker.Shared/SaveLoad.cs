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
using System.Threading.Tasks;
using Newtonsoft.Json;

using System.IO;

#if WINDOWS 
using System.Windows.Forms;
#endif

#if ANDROID
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using OS = Android.OS;
#endif

namespace MonoMusicMaker
{
    using ListGridEntries = List<ButtonGrid.GridEntryData>;
    using ListRegionSaveData = List<AIChordRegion.AIRegionSaveData>;
    using ListDrumSaveData = List<AIDrumRegion.AIDrumSaveData>;

    using ListParamSaveData = List<ParameterNodeRegion.ParameterNodeRegionSaveData>;

    public class SaveLoad
    {
        public class SaveSongData
        {
            public SaveSongData(string fileName, bool bStrictChecking = false)
            {
                mMidiFile = new MidiFile(fileName, bStrictChecking);
            }

            //Empty song
            public SaveSongData()
            {
                mMidiEventCollection = GetNewMidiEventCollection();
                mJsonLoopRangeData = "";
                mJsonRegionSaveData = "";
                mJsonDrumSaveData = "";
                mJsonParamRegionSaveData = "";
            }

            public SaveSongData(MidiEventCollection midiEventCollection, string jsonLoopRangeData, string jsonRegionSaveData, string jsonRegionDrumData ="", string jsonParamRegionDrumData = "")
            {
                mMidiEventCollection = midiEventCollection;
                mJsonLoopRangeData = jsonLoopRangeData;
                mJsonRegionSaveData = jsonRegionSaveData;
                mJsonDrumSaveData = jsonRegionDrumData;
                mJsonParamRegionSaveData = jsonParamRegionDrumData;
            }

            public MidiFile mMidiFile;
            public MidiEventCollection mMidiEventCollection;

            public string mSongName = "";
            public string mJsonLoopRangeData = "";
            public string mJsonRegionSaveData = "";
            public string mJsonDrumSaveData = "";
            public string mJsonParamRegionSaveData = "";
        }
        public const string MidFileExt = ".mid";
        public const string JsonFileExt = ".json";
        public const string LoopedFileExt = ".Looped.mid";
        public string mLoadSaveFileNAme;

        // to map to chordTypeNames array below
        public enum eStartFileLetter
        {
            A,
            B,
            C,
            D,
            E,
            F,
            G,
            H,
            I,
            J,
            K,
            L,
            M,
            N,
            O,
            P,
            Q,
                R,
                S,
                T,
                U,
                V,
                W,
                X,
                Y,
                Z,

            num_letters,
        }
        public enum eStartFileLetterLow
        {
            None,
            A,
            B,
            C,
            D,
            E,
            F,
            G,
            H,
            I,
            J,
            K,
            L,
            M,
            num_letters,
        }
        public enum eStartFileLetterHi
        {
            None,
            N,
            O,
            P,
            Q,
            R,
            S,
            T,
            U,
            V,
            W,
            X,
            Y,
            Z,
            num_letters,
        }
        //string[] beatWidthNames = new string[(int)eBeatWidthType.num_beatWidths] { "Eighth", "Quarter", "Half", "Full" };
        List<ParamButtonBar> mParamBars = new List<ParamButtonBar>();

        Button mMIDITickBox;
        Button mUNDOITickBox;
        Button mCLEARTickBox;

        SaveSongData mLastSongData = null;

        List<Button> mAllButtons = new List<Button>();
        ParamButtonBar mCurrenSetInBarMode = null;

        //https://www.csie.ntu.edu.tw/~r92092/ref/midi/
        //MIDI File Formats
        //MIDI files come in 3 variations:

        //Format 0 
        //...which contain a single track
        //Format 1 
        //... which contain one or more simultaneous tracks
        //(ie all tracks are to be played simultaneously).
        //Format 2 
        //...which contain one or more independant tracks
        //(ie each track is to be played independantly of the others).
        string stuPath = "";
        const string SAVE_MIDI_PREFIX = "MIDI";
        const string SAVE_REGION_DATA_PREFIX = "REGION_";
        const string SAVE_PARAM_DATA_PREFIX = "PARAM_";
        const string SAVE_DRUM_DATA_PREFIX = "DRUM_";
        public const string PC_PATH_FILE = "D:\\MonoGame\\MonoMusicMaker\\PC_SAVES";
        public const string DRUM_PRESET_FOLDER = "DRUM_PRESETS";
        public const string PLAY_AREA_SAVES_FOLDER = "PLAY_AREA_SAVES";

        public const int IGNORE_LOAD_TOUCH_X_RHS = 1200;
        const int GAP_AFTER_IGNORE_X = 40;
        const int PARAM_BAR_XPOS_START = IGNORE_LOAD_TOUCH_X_RHS + GAP_AFTER_IGNORE_X;
        const int PARAM_BAR_YPOS = 500;
        const int MIDI_XPOS_START = PARAM_BAR_XPOS_START + Button.mWidth + 60;

        const int TICK_BOX_GAP = 30;
        const int MIDI_TICK_YPOS = 100 + Button.mTickBoxSize + TICK_BOX_GAP;
        const int UNDO_TICK_YPOS = MIDI_TICK_YPOS + Button.mTickBoxSize + TICK_BOX_GAP;
        const int CLEAR_TICK_YPOS = UNDO_TICK_YPOS + Button.mTickBoxSize + TICK_BOX_GAP;

        MidiEventCollection mCollection;

        public string LoadSaveFileName = "";

        ScrollWideButtons mLoadSongScreen = null;
        string[] mTestLoadFileNames;
        public bool Active { get; set; } = false;
        ButtonGrid.GridEntryData mBackGED;
        ButtonGrid.GridEntryData mExitGED;
        ButtonGrid mActiveGrid = null;
        ListGridEntries mFiles = new List<ButtonGrid.GridEntryData>();

        //TODO put somewhere else
        public static int TicksPerQuarterNote = 512;

        string mInitiaLetterSearch = "";

        const int TrackNumber = 0; //global for now

        bool mbSaveHasParamRegionData = false;
        bool mbSaveHasDrumData = false;
        bool mbSaveHasLoopRangeData = false;
        bool mbSaveIsLooped = false; //dont save region data if looped
        string mJsonLoopRangeData = "";
        string mJsonLoopRangeSavePath = "";

        string mJsonRegionSaveData = "";
        string mJsonRegionSavePath = "";

        string mJsonDrumSaveData = "";
        string mJsonDrumSavePath = "";

        string mJsonParamRegionSaveData = "";
        string mJsonParamRegionSavePath = "";

        public void Init()
        {
            mExitGED.mClickOn = ExitToPlayArea;
            mExitGED.mValue = ButtonGrid.GridEntryData.EXIT_TYPE;
            mExitGED.mOnString = "EXIT";

            mTestLoadFileNames = new string[16] {    "File name one", "File name 2", "File name 3",
                                                    "File name 4", "File name 5", "File name 6",
                                                    "File name 7", "File name 8",
                                                    "File name 9", "File name 10", "File name 11",
                                                    "File name 12", "File name 13",
                                                    "File name 14", "File name 15", "File name 16"  };

            CreateLoadScreen(mTestLoadFileNames);

            List<string> listNames = new List<string>();

            for (int i = 0; i < (int)eStartFileLetterLow.num_letters; i++)
            {
                listNames.Add(Enum.GetName(typeof(eStartFileLetterLow), i));
            }

            ParamButtonBar.SetOffsets(PARAM_BAR_YPOS, PARAM_BAR_XPOS_START, GAP_AFTER_IGNORE_X);
            ParamButtonBar.SetUpPopupParamBar(listNames.ToArray(), LetterParamCB, ParamButtonBar.DROP_TYPE.MIDDLE, mParamBars);
           // SetUpPopupParamBar(listNames.ToArray());

            listNames = new List<string>();

            for (int i = 0; i < (int)eStartFileLetterHi.num_letters; i++)
            {
                listNames.Add(Enum.GetName(typeof(eStartFileLetterHi), i));
            }

            //SetUpPopupParamBar(listNames.ToArray());
            ParamButtonBar.SetUpPopupParamBar(listNames.ToArray(), LetterParamCB, ParamButtonBar.DROP_TYPE.MIDDLE, mParamBars);

            mMIDITickBox = new Button(new Vector2(MIDI_XPOS_START, MIDI_TICK_YPOS), "PlainWhite"); ;
            mMIDITickBox.mType = Button.Type.Tick;
            mMIDITickBox.ButtonText = "MIDI File";
            mMIDITickBox.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Tick;
            mMIDITickBox.ButtonTextColour = Color.Black;

            mUNDOITickBox = new Button(new Vector2(MIDI_XPOS_START, UNDO_TICK_YPOS), "PlainWhite"); ;
            mUNDOITickBox.mType = Button.Type.Tick;
            mUNDOITickBox.ButtonText = "UNDO";
            mUNDOITickBox.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Tick;
            mUNDOITickBox.ButtonTextColour = Color.Black;
            mUNDOITickBox.ClickCB = UndoCB;

            mCLEARTickBox = new Button(new Vector2(MIDI_XPOS_START, CLEAR_TICK_YPOS), "PlainWhite"); ;
            mCLEARTickBox.mType = Button.Type.Tick;
            mCLEARTickBox.ButtonText = "CLEAR";
            mCLEARTickBox.mMask = MelodyEditorInterface.UIButtonMask.UIBM_Tick;
            mCLEARTickBox.ButtonTextColour = Color.Black;
            mCLEARTickBox.ClickCB = ClearAll;

            mMIDITickBox.mbOn = true;

            mMIDITickBox.SetCallbacks(MidiOnCB, MidiOffCB);

            mAllButtons.Add(mCLEARTickBox);
            mAllButtons.Add(mUNDOITickBox);
            mAllButtons.Add(mMIDITickBox);

            return;

         }

        bool MidiOnCB(MelodyEditorInterface.MEIState state)
        {
            RefreshFiles( state);
            return false;
        }

        bool MidiOffCB(MelodyEditorInterface.MEIState state)
        {
            RefreshFiles( state);
            return false;
        }

        public void LetterParamCB(Button button, MelodyEditorInterface.MEIState state)
        {
            if(button.ButtonText!= mInitiaLetterSearch)
            {
                mInitiaLetterSearch = button.ButtonText;

                if(mInitiaLetterSearch=="None")
                {
                    foreach (ParamButtonBar pbb in mParamBars)
                    {
                        pbb.SetSelected(0);
                    }
                }
            }

            RefreshFiles(state);
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
                aiGEDEntry.mClickOn = LetterParamCB;
                count++;
                aiGED.Add(aiGEDEntry);
            }

            ParamButtonBar pbb = new ParamButtonBar(aiGED);

            AddParamBar(pbb);

            return pbb;
        }

        public void AddParamBar(ParamButtonBar pbb)
        {
            int numBars = mParamBars.Count;
            Vector2 vPos = new Vector2();

            vPos.Y = PARAM_BAR_YPOS + numBars * (GAP_AFTER_IGNORE_X + Button.mHeight);
            vPos.X = PARAM_BAR_XPOS_START;
            pbb.Init(vPos);

            mParamBars.Add(pbb);
        }

        void CreateLoadScreen(string[] fileList)
        {
            ListGridEntries mFiles = new List<ButtonGrid.GridEntryData>();
            int count = 0;
            mFiles.Add(mExitGED);

            foreach (string str in fileList)
            {
                ButtonGrid.GridEntryData fileEntry;

                fileEntry.mOnString = str;
                fileEntry.mValue = count;
                fileEntry.mClickOn = LoadFile;
                count++;
                mFiles.Add(fileEntry);
            }

            mLoadSongScreen = new ScrollWideButtons(mFiles);
            mLoadSongScreen.InitDefaultTextFont();
            mLoadSongScreen.mIgnoreTouchAfterX = IGNORE_LOAD_TOUCH_X_RHS;
        }

        public bool UndoCB(MelodyEditorInterface.MEIState state)
        {
            if(mLastSongData != null)
            {
                state.mMeledInterf.LoadFromMidiCollection(mLastSongData.mMidiEventCollection, true);
                LoadSaveFileName = mLastSongData.mSongName;

                //Make sure to do after playareas loaded (I think maybe doesn't matter)
                if (mLastSongData.mJsonLoopRangeData!="")
                {
                    mbSaveHasLoopRangeData = true;
                    List<PlayArea.LoopRange> lrl = JsonConvert.DeserializeObject<List<PlayArea.LoopRange>>(mLastSongData.mJsonLoopRangeData);
                    state.mMeledInterf.SetAllPlayAreaLoops(lrl);
                }

                //Make sure to do after playareas loaded (I think maybe doesn't matter)
                if (mLastSongData.mJsonRegionSaveData != "")
                {
                    mbSaveHasLoopRangeData = true;
                    List<ListRegionSaveData> lrsd = JsonConvert.DeserializeObject<List<ListRegionSaveData>>(mLastSongData.mJsonRegionSaveData);
                    state.mMeledInterf.SetAllPlayAreaRegionData(lrsd);
                }

                //Make sure to do after playareas loaded (I think maybe doesn't matter)
                if (mLastSongData.mJsonParamRegionSaveData != "")
                {
                    mbSaveHasParamRegionData = true;
                    List<ListParamSaveData> lpsd = JsonConvert.DeserializeObject<List<ListParamSaveData>>(mLastSongData.mJsonParamRegionSaveData);
                    state.mMeledInterf.SetAllPlayAreaParamRegionData(lpsd);
                }

                //TODO DRUM
                if (mLastSongData.mJsonDrumSaveData != "")
                {
                    mbSaveHasDrumData = true;
                    ListDrumSaveData ldsd = JsonConvert.DeserializeObject<ListDrumSaveData>(mLastSongData.mJsonDrumSaveData);
                    state.mMeledInterf.SetDrumChannelDrumData(ldsd);
                }
                state.PlayAreaChanged(true, false, false);
                return true;
            }

            return false;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// SWITCH SAVES
        public bool SetToSaveData(SaveSongData songData, MelodyEditorInterface mei)
        {
            System.Diagnostics.Debug.Assert(songData != null, string.Format("Expect valid last saved data songData!"));

            if (songData != null)
            {
                mei.LoadFromMidiCollection(songData.mMidiEventCollection, true);
                LoadSaveFileName = songData.mSongName;

                //Save switching issues found this - with param regions sticking around - needs revisiting
                mbSaveHasParamRegionData = false;
                mbSaveHasLoopRangeData = false;
                mbSaveHasDrumData = false;

                //Make sure to do after playareas loaded (I think maybe doesn't matter)
                if (songData.mJsonLoopRangeData != "")
                {
                    mbSaveHasLoopRangeData = true;
                    List<PlayArea.LoopRange> lrl = JsonConvert.DeserializeObject<List<PlayArea.LoopRange>>(songData.mJsonLoopRangeData);
                    mei.SetAllPlayAreaLoops(lrl);
                }

                //Make sure to do after playareas loaded (I think maybe doesn't matter)
                if (songData.mJsonRegionSaveData != "")
                {
                    mbSaveHasLoopRangeData = true;
                    List<ListRegionSaveData> lrsd = JsonConvert.DeserializeObject<List<ListRegionSaveData>>(songData.mJsonRegionSaveData);
                    mei.SetAllPlayAreaRegionData(lrsd);
                }

                //Make sure to do after playareas loaded (I think maybe doesn't matter)
                if (songData.mJsonParamRegionSaveData != "")
                {
                    mbSaveHasParamRegionData = true;
                    List<ListParamSaveData> lpnsd = JsonConvert.DeserializeObject<List<ListParamSaveData>>(songData.mJsonParamRegionSaveData);
                    mei.SetAllPlayAreaParamRegionData(lpnsd);
                }

                //TODO DRUM
                if (songData.mJsonDrumSaveData != "")
                {
                    mbSaveHasDrumData = true;
                    ListDrumSaveData ldsd = JsonConvert.DeserializeObject<ListDrumSaveData>(songData.mJsonDrumSaveData);
                    mei.SetDrumChannelDrumData(ldsd);
                }

                mei.mState.PlayAreaChanged(true, false, false);
                return true;
            }

            return false;
        }


        // TODO: USE THIS?
        public bool SetSaveData(MelodyEditorInterface.MEIState state, SaveSongData songData)
        {
            if (songData != null)
            {
                state.mMeledInterf.LoadFromMidiCollection(songData.mMidiEventCollection, true);
                LoadSaveFileName = songData.mSongName;

                //Make sure to do after playareas loaded (I think maybe doesn't matter)
                if (songData.mJsonLoopRangeData != "")
                {
                    mbSaveHasLoopRangeData = true;
                    List<PlayArea.LoopRange> lrl = JsonConvert.DeserializeObject<List<PlayArea.LoopRange>>(songData.mJsonLoopRangeData);
                    state.mMeledInterf.SetAllPlayAreaLoops(lrl);
                }

                //Make sure to do after playareas loaded (I think maybe doesn't matter)
                if (songData.mJsonRegionSaveData != "")
                {
                    mbSaveHasLoopRangeData = true;
                    List<ListRegionSaveData> lrsd = JsonConvert.DeserializeObject<List<ListRegionSaveData>>(songData.mJsonRegionSaveData);
                    state.mMeledInterf.SetAllPlayAreaRegionData(lrsd);
                }

                //Make sure to do after playareas loaded (I think maybe doesn't matter)
                if (songData.mJsonParamRegionSaveData != "")
                {
                    mbSaveHasParamRegionData = true;
                    List<ListParamSaveData> lpsd = JsonConvert.DeserializeObject<List<ListParamSaveData>>(songData.mJsonParamRegionSaveData);
                    state.mMeledInterf.SetAllPlayAreaParamRegionData(lpsd);
                }

                //TODO DRUM
                if (songData.mJsonDrumSaveData != "")
                {
                    mbSaveHasDrumData = true;
                    ListDrumSaveData ldsd = JsonConvert.DeserializeObject<ListDrumSaveData>(songData.mJsonDrumSaveData);
                    state.mMeledInterf.SetDrumChannelDrumData(ldsd);
                }
                state.PlayAreaChanged(true, false, false);
                return true;
            }

            return false;
        }

        public bool ClearAll(MelodyEditorInterface.MEIState state)
        {
            GetCurrentSongData(state);
            state.mMeledInterf.ClearAll();
            LoadSaveFileName = "";
            state.GetMidiBase().StopAllNotes(); //Blatt out a clear to stop notes being stuck on 
            state.PlayAreaChanged(true, false, false);
            return true;
        }

        void GetCurrentSongData(MelodyEditorInterface.MEIState state)
        {
            //mLastSongData = new SaveSongData(LoadSaveFileName);
            ////Delete current track and load this one?
            //state.mMeledInterf.LoadMidiFile(mLastSongData.mMidiFile);

            GatherMidiFromAllTracks(state.mMeledInterf);
            mLastSongData = new SaveSongData(mCollection,mJsonLoopRangeData,mJsonRegionSaveData, mJsonDrumSaveData, mJsonParamRegionSaveData);
            mLastSongData.mSongName = LoadSaveFileName;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// SWITCH SAVES
        public SaveSongData GetCurrentSongData(MelodyEditorInterface melEdInterf, bool bGetCurrentKeyLines = false)
        {
            GatherMidiFromAllTracks(melEdInterf, bGetCurrentKeyLines);
            SaveSongData songData = new SaveSongData(mCollection, mJsonLoopRangeData, mJsonRegionSaveData, mJsonDrumSaveData, mJsonParamRegionSaveData);
            songData.mSongName = LoadSaveFileName;

            return songData;
        }

        void LoadFromFileName(string ButtonText, MelodyEditorInterface.MEIState state, bool bFullPathPassed = false, bool bCurrentAreaOnly = false)
        {
#if ANDROID
            var fileDelim = "/";
            var pathFile = OS.Environment.GetExternalStoragePublicDirectory(OS.Environment.DirectoryDownloads);
            string pathFileString = pathFile.ToString();
#else
            var fileDelim = "\\";
            var pathFileString = PC_PATH_FILE;

            if (bCurrentAreaOnly)
            {
                pathFileString += fileDelim + PLAY_AREA_SAVES_FOLDER;
            }
#endif
            //Bit of a hack but, fuck it
            if (bFullPathPassed)
            {
                ButtonText = StaticHelpers.SubtractString(ButtonText, pathFileString + fileDelim);
            }

            string fileName = pathFileString + fileDelim + ButtonText;

            mbSaveHasLoopRangeData = false;
            mbSaveHasDrumData = false;
            mbSaveHasParamRegionData = false;
            try
            {
                bool bStrictChecking = false;
                MidiFile midi = new MidiFile(fileName, bStrictChecking);
                //mActiveGrid = null; //leave altogether
                System.Diagnostics.Debug.WriteLine(string.Format("Number notes {0}", midi.Events.Tracks));

                string loopBarFileName = fileName.Substring(0, fileName.Length - 3) + "json";


                //SORT OUT REGION AND DRUM SAVE
                string regionSaveFileName = fileName.Substring(0, fileName.Length - 3) + "json";
                string drumSaveFileName = fileName.Substring(0, fileName.Length - 3) + "json";
                string subtractedFileName = StaticHelpers.SubtractString(regionSaveFileName, pathFileString + fileDelim);
                string paramSaveFileName = fileName.Substring(0, fileName.Length - 3) + "json";

                string isMididStr = subtractedFileName.Substring(0, SAVE_MIDI_PREFIX.Length);

                if(isMididStr=="MIDI")
                {
                    subtractedFileName = subtractedFileName.Substring(SAVE_MIDI_PREFIX.Length);
                }

                regionSaveFileName = pathFileString + fileDelim + SAVE_REGION_DATA_PREFIX + subtractedFileName;

                paramSaveFileName = pathFileString + fileDelim + SAVE_PARAM_DATA_PREFIX + subtractedFileName;

                drumSaveFileName = pathFileString + fileDelim + SAVE_DRUM_DATA_PREFIX + subtractedFileName;

                //Delete current track and load this one?
                state.mMeledInterf.LoadMidiFile(midi, bCurrentAreaOnly);

                state.PlayAreaChanged(true, false);
                LoadSaveFileName = ButtonText;

                Active = false; //close screen
                mActiveGrid = null; //seems we have to do this to exit

                state.PrepareForLoadFile(); //Blatt out a clear to stop notes being stuck on, turn off soloing

                //Make sure to do after playareas loaded (I think maybe doesn't matter)
                if (File.Exists(loopBarFileName))
                {
                    mbSaveHasLoopRangeData = true;
                    string jsonFileString = File.ReadAllText(loopBarFileName);

                    List<PlayArea.LoopRange> lrl = JsonConvert.DeserializeObject<List<PlayArea.LoopRange>>(jsonFileString);
                    state.mMeledInterf.SetAllPlayAreaLoops(lrl, bCurrentAreaOnly);
                }

                //Make sure to do after playareas loaded (I think maybe doesn't matter)
                if (File.Exists(regionSaveFileName))
                {
                    mbSaveHasLoopRangeData = true;
                    string jsonFileString = File.ReadAllText(regionSaveFileName);

                    List<ListRegionSaveData> lrsd = JsonConvert.DeserializeObject<List<ListRegionSaveData>>(jsonFileString);
                    state.mMeledInterf.SetAllPlayAreaRegionData(lrsd, bCurrentAreaOnly);
                }

                //Make sure to do after playareas loaded (I think maybe doesn't matter)
                if (File.Exists(paramSaveFileName))
                {
                    mbSaveHasParamRegionData = true;
                    string jsonFileString = File.ReadAllText(paramSaveFileName);

                    List<ListParamSaveData> lpsd = JsonConvert.DeserializeObject<List<ListParamSaveData>>(jsonFileString);
                    state.mMeledInterf.SetAllPlayAreaParamRegionData(lpsd, bCurrentAreaOnly);
                }

                //Make sure to do after playareas loaded (I think maybe doesn't matter)
                if (File.Exists(drumSaveFileName))
                {
                    System.Diagnostics.Debug.Assert(!bCurrentAreaOnly, string.Format("Dont expect bCurrentAreaOnly for drum area!"));

                    mbSaveHasDrumData = true;
                    string jsonFileString = File.ReadAllText(drumSaveFileName);

                    ListDrumSaveData lrsd = JsonConvert.DeserializeObject<ListDrumSaveData>(jsonFileString);
                    state.mMeledInterf.SetDrumChannelDrumData(lrsd);
                }

                if(!bCurrentAreaOnly)
                {
                    //Set loaded song in top slot of Save songs drop down
                    state.mMeledInterf.mSaveSwitchManager.LoadInSong(ButtonText);
                }
            }
            catch (Exception e)
            {
                //mActiveGrid = null; //leave altogether
                System.Diagnostics.Debug.WriteLine(string.Format("Exception {0}", e.ToString()));
            }


            mLastSongData = null; //Make sure we dont overwrite with old cleared data when load in new song
        }

        public void LoadFile(Button button, MelodyEditorInterface.MEIState state)
        {
            mActiveGrid.SetOnAndClearAllOthers(button);
            System.Diagnostics.Debug.WriteLine(string.Format("FILE LIST ExitToPlayArea{0}", button.mGED.mValue));

            LoadFromFileName(button.ButtonText, state);
        }

        public void SetInactive()
        {
            mActiveGrid = null; //leave altogether

        }

        public void ExitToPlayArea(Button button, MelodyEditorInterface.MEIState state)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("FILE LIST ExitToPlayArea{0}", button.mGED.mValue));
            SetInactive();
        }

        //DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW
        //DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW
        //DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW
        //DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW
        //DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW
        //DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW
        //DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW//DRAW
        public void Draw(SpriteBatch sb)
        {
            if (mActiveGrid != null)
            {
                mActiveGrid.Draw(sb);

                if(mCurrenSetInBarMode!=null)
                {
                    mCurrenSetInBarMode.Draw(sb);
                }
                else
                {
                    foreach (ParamButtonBar pbb in mParamBars)
                    {
                        pbb.Draw(sb);
                    }
                }

                foreach (Button but in mAllButtons)
                {
                    but.Draw(sb);
                }
            }
        }

        public bool Update(MelodyEditorInterface.MEIState state)
        {
            if (mActiveGrid != null)
            {
                mActiveGrid.Update(state);

                ParamButtonBar beenSetInBarMode = null;

                if(mCurrenSetInBarMode==null)
                {
                    foreach (ParamButtonBar pbb in mParamBars)
                    {
                        if (pbb.Update(state))
                        {
                            beenSetInBarMode = pbb; //Exit since this has been set 
                            mCurrenSetInBarMode = pbb;
                        }
                    }
                }
                else
                {
                    mCurrenSetInBarMode.Update(state);
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
                        mCurrenSetInBarMode = null;
                    }
                }

                foreach (Button but in mAllButtons)
                {
                    if (but != mMIDITickBox)
                    {
                        if (but.Update(state))
                        {
                            but.mbOn = false;
                        }
                    }
                    else
                    {
                        but.Update(state);
                    }
                }

                return true;
            }

            return false;
        }
        public void SetActive(MelodyEditorInterface.MEIState state)
        {
            RefreshFiles(state);
            Active = true;
        }

        void RefreshFiles(MelodyEditorInterface.MEIState state)
        {
#if ANDROID
            var fileDelim = "/";
            var pathFile = OS.Environment.GetExternalStoragePublicDirectory(OS.Environment.DirectoryDownloads);
            string pathFileString = pathFile.ToString();
#else
            var fileDelim = "\\";
            //string pathFileString = ".";
            //var pathWithEnv = @"%USERPROFILE%\AppData\";
            //var pathFileString = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var pathFileString = PC_PATH_FILE;

#endif
            //stuPath = pathFileString + "/MIDI" + "TestFileName" + ".mid";
            //Get file list here
            var files = System.IO.Directory.GetFiles(pathFileString, "*.mid");
            if (files.Length == 0)
                return;

            List<string> fileNames = new List<string>();


            foreach (var file in files)
            {
                //do something
                string fileName = file.ToString();

                string searchStart = SAVE_MIDI_PREFIX;
                if (mInitiaLetterSearch!="None")
                {
                    searchStart += mInitiaLetterSearch;
                }

                string subtractedFileName = StaticHelpers.SubtractString(fileName, pathFileString + fileDelim);
                if (mMIDITickBox.mbOn)
                    //if (state.mbMIDIFilePrefix)
                {
                    if (subtractedFileName.StartsWith(searchStart, StringComparison.InvariantCultureIgnoreCase))
                    {
                        //fileNames.Add(fileName);
                        fileNames.Add(subtractedFileName);
                    }
                }
                else
                {
                    if (!subtractedFileName.StartsWith(SAVE_MIDI_PREFIX, StringComparison.InvariantCultureIgnoreCase))
                    {
                        //fileNames.Add(fileName);
                        fileNames.Add(subtractedFileName);
                    }
                }
            }
            CreateLoadScreen(fileNames.ToArray());

            mActiveGrid = mLoadSongScreen;
            mLoadSongScreen.ResetScroll();
        }

#if WINDOWS
        public bool LoadDialogFile(MelodyEditorInterface.MEIState state, bool bCurrentAreaOnly = false)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            string saveLoadPath = SaveLoad.PC_PATH_FILE;

            if(bCurrentAreaOnly)
            {
                var fileDelim = "\\";
                saveLoadPath += fileDelim + PLAY_AREA_SAVES_FOLDER;
                if (!System.IO.Directory.Exists(saveLoadPath))
                {
                    System.IO.Directory.CreateDirectory(saveLoadPath);
                }
            }

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = saveLoadPath;
                openFileDialog.Filter = "Music FIle (*" + SaveLoad.MidFileExt + ")|*" + SaveLoad.MidFileExt + "|Looped Music File (*" + SaveLoad.LoopedFileExt + ")|*" + SaveLoad.LoopedFileExt + "|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    mLoadSaveFileNAme = openFileDialog.FileName;

                    Console.WriteLine(mLoadSaveFileNAme);

                    //Is users or tweets file?
                    string endUsers = mLoadSaveFileNAme.Substring(mLoadSaveFileNAme.Length - SaveLoad.LoopedFileExt.Length);
                    string endTweets = mLoadSaveFileNAme.Substring(mLoadSaveFileNAme.Length - SaveLoad.MidFileExt.Length);
                    if (endUsers == SaveLoad.LoopedFileExt)
                    {
                        LoadFromFileName(mLoadSaveFileNAme,  state, true, bCurrentAreaOnly);
                    }
                    else if (endTweets == SaveLoad.MidFileExt)
                    {
                        LoadFromFileName(mLoadSaveFileNAme,  state, true, bCurrentAreaOnly);
                    }

                    return true;
                }

                return false;
            }

            //MessageBox.Show(fileContent, "File Content at path: " + filePath, MessageBoxButtons.OK); return false;
        }

        public bool SaveDialogFile(MelodyEditorInterface.MEIState state, bool bCurrentAreaOnly = false)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            string saveLoadPath = SaveLoad.PC_PATH_FILE;
            if(bCurrentAreaOnly)
            {
                var fileDelim = "\\";
                saveLoadPath += fileDelim + PLAY_AREA_SAVES_FOLDER;
                if (!System.IO.Directory.Exists(saveLoadPath))
                {
                    System.IO.Directory.CreateDirectory(saveLoadPath);
                }
            }
    
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = saveLoadPath;
                saveFileDialog.Filter = "Music FIle (*" + SaveLoad.MidFileExt + ")|*" + SaveLoad.MidFileExt + "|Looped Music File (*" + SaveLoad.LoopedFileExt + ")|*" + SaveLoad.LoopedFileExt + "|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    mLoadSaveFileNAme = saveFileDialog.FileName;

                    Console.WriteLine(mLoadSaveFileNAme);
                    
                    state.mbSaveMIDIWithLoop =    saveFileDialog.FilterIndex ==2; //use the SaveLoad.LoopFileExt  to denote saying with a loop on windows build

                    SaveAllTracks(state.mMeledInterf, mLoadSaveFileNAme, true, bCurrentAreaOnly);

                    state.mbSaveMIDIWithLoop = false; // reset once SaveAllTracks has been called

                    return true;
                }

                return false;
            }

            //MessageBox.Show(fileContent, "File Content at path: " + filePath, MessageBoxButtons.OK); return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////
        /// DRUM LOOPS LOAD SAVE
        ///////////////////////////////////////////////////////////////////////////////
        public bool SaveDrumLoop(TopLineDrumAI tlda)
        {
#if ANDROID
            var fileDelim = "/";
            var pathFile = OS.Environment.GetExternalStoragePublicDirectory(OS.Environment.DirectoryDownloads);
            string pathFileString = pathFile.ToString();
#else
            var fileDelim = "\\";
            //string pathFileString = ".";
            //var pathWithEnv = @"%USERPROFILE%\AppData\";
            //var pathFileString = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var pathFileString = PC_PATH_FILE;

#endif
            string pathToDrumPresets = pathFileString + fileDelim + DRUM_PRESET_FOLDER;
            if (!System.IO.Directory.Exists(pathToDrumPresets))
            {
                System.IO.Directory.CreateDirectory(pathToDrumPresets);
            }

#if WINDOWS
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (SaveFileDialog saveFileDIalog = new SaveFileDialog())
            {
                saveFileDIalog.InitialDirectory = pathToDrumPresets;
                saveFileDIalog.Filter = "Drum data (*" + SaveLoad.JsonFileExt + ")|*" + SaveLoad.JsonFileExt + "|All files (*.*)|*.*";
                saveFileDIalog.FilterIndex = 1;
                saveFileDIalog.RestoreDirectory = true;

                if (saveFileDIalog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    mLoadSaveFileNAme = saveFileDIalog.FileName;

                    Console.WriteLine(mLoadSaveFileNAme);

                    string isJsonType = mLoadSaveFileNAme.Substring(mLoadSaveFileNAme.Length - SaveLoad.JsonFileExt.Length);
                    if (isJsonType == SaveLoad.JsonFileExt)
                    {
                         SaveDrum( tlda , mLoadSaveFileNAme);
                         return true;
                    }
                }
            }
#endif
            return false;
        }
        public bool LoadDrumLoop(MelodyEditorInterface melEdInterf)
        {
#if ANDROID
            var fileDelim = "/";
            var pathFile = OS.Environment.GetExternalStoragePublicDirectory(OS.Environment.DirectoryDownloads);
            string pathFileString = pathFile.ToString();
#else
            var fileDelim = "\\";
            //string pathFileString = ".";
            //var pathWithEnv = @"%USERPROFILE%\AppData\";
            //var pathFileString = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var pathFileString = PC_PATH_FILE;

#endif
            string pathToDrumPresets = pathFileString + fileDelim + DRUM_PRESET_FOLDER;
            if (!System.IO.Directory.Exists(pathToDrumPresets))
            {
                System.IO.Directory.CreateDirectory(pathToDrumPresets);
            }

#if WINDOWS
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (OpenFileDialog openFileDIalog = new OpenFileDialog())
            {
                openFileDIalog.InitialDirectory = pathToDrumPresets;
                openFileDIalog.Filter = "Drum data (*" + SaveLoad.JsonFileExt + ")|*" + SaveLoad.JsonFileExt + "|All files (*.*)|*.*";
                openFileDIalog.FilterIndex = 1;
                openFileDIalog.RestoreDirectory = true;

                if (openFileDIalog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    mLoadSaveFileNAme = openFileDIalog.FileName;

                    Console.WriteLine(mLoadSaveFileNAme);

                    string isJsonType = mLoadSaveFileNAme.Substring(mLoadSaveFileNAme.Length - SaveLoad.JsonFileExt.Length);
                    if (isJsonType == SaveLoad.JsonFileExt)
                    {
                        LoadDrum( melEdInterf,  mLoadSaveFileNAme);
                    }
                }

            }
#endif
            return false;
        }

        //////////////////////////////////////////////////////////////////////////////
        /// DRUM SAVE LOAD
        void SaveDrum(TopLineDrumAI tlda , string drumSaveFileName)
        {
            ListDrumSaveData  dsd = tlda.GetRegionSaveData(); //just the one drm save from track 9, channel 10 
            if(dsd.Count>0)
            {
                mJsonDrumSaveData = JsonConvert.SerializeObject(dsd);
                mJsonDrumSavePath = drumSaveFileName;
                mbSaveHasDrumData = true;
                mbSaveHasLoopRangeData = false;
                DrumAsyncSave();
            }
        }

        void LoadDrum(MelodyEditorInterface melEdInterf, string drumSaveFileName)
        {
            if (File.Exists(drumSaveFileName))
            {
                mbSaveHasDrumData = true;
                string jsonFileString = File.ReadAllText(drumSaveFileName);

                ListDrumSaveData lrsd = JsonConvert.DeserializeObject<ListDrumSaveData>(jsonFileString);

                if(lrsd!=null)
                {
                    melEdInterf.SetDrumChannelDrumData(lrsd);
                    melEdInterf.mState.PlayAreaChanged(true, false, false);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("No drum data found for file  {0}", drumSaveFileName));
                }
            }
        }


        ///////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////
        /// PLAY AREA LOAD SAVE
        ///////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////
        public bool SavePlayArea()
        {

            return false;
        }

        public bool LoadPlayArea(MelodyEditorInterface melEdInterf)
        {

            return false;
        }


        public void LoadContent(ContentManager contentMan, SpriteFont font)
        {
            mLoadSongScreen.LoadContent(contentMan, font);

            foreach (ParamButtonBar pbb in mParamBars)
            {
                pbb.LoadContent(contentMan, font);
            }

            foreach (Button but in mAllButtons)
            {
                but.LoadContent(contentMan, font);
            }
        }

        public void LoadTrackFromMidiSave(MelodyEditorInterface melEdInterf, MidiFile midi)
        {

        }

        public static MidiEventCollection GetNewMidiEventCollection()
        {
            const int MidiFileType = 1; //have a track for each channel makes it easier to load and save I think
            return new MidiEventCollection(MidiFileType, TicksPerQuarterNote);
        }

        //
        public void GatherMidiFromAllTracks(MelodyEditorInterface melEdInterf, bool bGetCurrentKeyLines = false, bool bCurrentAreaOnly = false)
        {
            mCollection = GetNewMidiEventCollection();

            List<PlayArea> pas = melEdInterf.GetPlayAreas();

            if( bCurrentAreaOnly )
            {
                pas = new List<PlayArea>();
                pas.Add(melEdInterf.GetCurrentPlayArea()); //just get the one area - dont thinkn it matter about the channel number
            }

            mbSaveHasLoopRangeData = false;
            mbSaveHasDrumData = false;
            mbSaveIsLooped = melEdInterf.mState.mbSaveMIDIWithLoop;

            //Surely these should all be cleared here? Had issue with param regions reappearing when switching slots
            mJsonParamRegionSaveData = "";
            mJsonRegionSaveData = "";
            mJsonLoopRangeData = "";
            mJsonDrumSaveData = "";

            mbSaveHasParamRegionData = false;

            List<PlayArea.LoopRange> lrl = new List<PlayArea.LoopRange>();

            List<ListRegionSaveData> lrsd = new List<ListRegionSaveData>();

            List<ListParamSaveData> prsd = new List<ListParamSaveData>();

            ListDrumSaveData dsd = new ListDrumSaveData();  //TODO DRUM SAVE - -

            foreach (PlayArea pa in pas)
            {
                PlayArea.LoopRange lr = new PlayArea.LoopRange();
                mbSaveHasLoopRangeData |= pa.GetSaveBarRange(lr);
                lrl.Add(lr);
                lrsd.Add(pa.GetRegionSaveData()); //TODO SAVING IF NO REGIONS!!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! 
                pa.GatherIntoCollectionFromPlayArea(mCollection, melEdInterf, bGetCurrentKeyLines);
                pa.GatherEffectsMidiInfo(mCollection, melEdInterf);

                TopLineDrumAI tlda = pa.GetTopLineDrumAI();

                if(tlda!=null) //TODO SAVING IF NO REGIONS!!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! !!!!!! 
                {
                    dsd = tlda.GetRegionSaveData(); //just the one drm save from track 9, channel 10 
                    mbSaveHasDrumData = dsd.Count != 0;
                }

                ParameterNodeManager tlpnm = pa.GetParamNodeManager();

                if (tlpnm != null)
                {
                    ListParamSaveData  lpsd= tlpnm.GetRegionSaveData();
                    if(lpsd.Count!=0)
                    {
                        prsd.Add(lpsd); 
                        mbSaveHasParamRegionData = true;
                    }
                }
            }

            if (mbSaveIsLooped)
            {
                mbSaveHasLoopRangeData = false; //the track loops are going to be saved as unrolled and played through anyway so dont save this
            }

            if(mbSaveHasDrumData)
            {
                mJsonDrumSaveData = JsonConvert.SerializeObject(dsd);
            }

            if (mbSaveHasLoopRangeData)
            {
                mJsonLoopRangeData = JsonConvert.SerializeObject(lrl);
            }

            if (!mbSaveIsLooped)
            {
                mJsonRegionSaveData = JsonConvert.SerializeObject(lrsd);
            }

            if(mbSaveHasParamRegionData)
            {
                mJsonParamRegionSaveData = JsonConvert.SerializeObject(prsd);
            }
        }

        public void SaveAllTracks(MelodyEditorInterface melEdInterf, string fileName, bool bFullPathPassed = false, bool bCurrentAreaOnly = false)
        {
            //TODO BETTER CHECK!
            if (fileName == "")
            {
                return;
            }

            mbSaveHasLoopRangeData = false;     //put this before GatherMidiFromAllTracks which will set it true if needed
            mbSaveHasDrumData = false;          //put this before GatherMidiFromAllTracks which will set it true if needed

            GatherMidiFromAllTracks(melEdInterf, false, bCurrentAreaOnly);

            mbSaveIsLooped = melEdInterf.mState.mbSaveMIDIWithLoop;

#if ANDROID
            var fileDelim = "/";
            var pathFile = OS.Environment.GetExternalStoragePublicDirectory(OS.Environment.DirectoryDownloads);
            string pathFileString = pathFile.ToString();
#else
            var pathFileString = PC_PATH_FILE;
            var fileDelim = "\\";

            if(bCurrentAreaOnly)
            {
                pathFileString += fileDelim + PLAY_AREA_SAVES_FOLDER;
            }
#endif
            if (mbSaveIsLooped)
            {
                mbSaveHasLoopRangeData = false; //the track loops are going to be saved as unrolled and played through anyway so dont save this
            }

            if (bFullPathPassed)
            {
                fileName = StaticHelpers.SubtractString(fileName, pathFileString + fileDelim);
                fileName = fileName.Substring(0, fileName.Length - 4); //TODO
            }

            if(mbSaveHasLoopRangeData)
            {
                if (bFullPathPassed)
                {
                    mJsonLoopRangeSavePath = pathFileString + "/" + fileName + ".json"; //for file dialog - i.e. non Android the loop range data is just teh same as teh midi file but with json extension
                }
                else
                {
                    mJsonLoopRangeSavePath = pathFileString + "/" + SAVE_MIDI_PREFIX + fileName + ".json";
                }
            }

            if (mbSaveHasDrumData)
            {
                mJsonDrumSavePath = pathFileString + "/" + SAVE_DRUM_DATA_PREFIX + fileName + ".json";
            }

            if (mbSaveHasParamRegionData)
            {
                mJsonParamRegionSavePath = pathFileString + "/"+ SAVE_PARAM_DATA_PREFIX + fileName + ".json";
            }

            if (!mbSaveIsLooped)
            {
                mJsonRegionSavePath = pathFileString + "/" + SAVE_REGION_DATA_PREFIX + fileName + ".json";
            }

            if (bFullPathPassed)
            {
                //for the main midi file we save as we see now so the file dialog warning makes sense if we resuse a name - make sure that the load knows this!
                stuPath = pathFileString + "/" + fileName + ".mid";
            }
            else
            {
                stuPath = pathFileString + "/" + SAVE_MIDI_PREFIX + fileName + ".mid";
            }

            melEdInterf.SetSaveName(fileName);

            mCollection.PrepareForExport();
            DoWorkAsync();
        }


        ////IS THIS USED?  JUST SEEMS TO BE A COPY OF THE SAME NAME FUNCTION IN PLAY AREA
        //void GatherIntoCollectionFromPlayArea(PlayArea pa, MelodyEditorInterface melEdInterf)
        //{
        //    int NoteVelocity = MidiBase.MID_VELOCITY;
        //    int NoteDuration = 0; //;!
        //    int BeatsPerMinute = (int)melEdInterf.BPM;

        //    // Looking in https://github.com/naudio/NAudio/blob/master/NAudio/Midi/MidiEventCollection.cs
        //    //source code seems if you set this to 0 then it automatically will
        //    //create tracks for each channel as you send in channel data, it seems track 0 is reserved for
        //    //non channel events so your channels will start from track[1] for channel 1 etc
        //    //int localTrackNum = 0; // pa.mChannel - 1;  

        //    //Forget above I prefer having all data separated out like this I think be best
        //    //although when use this the tracks read back as in track 0 -15 array!? 
        //    int localTrackNum = pa.mChannel;  

        //    string trackName = string.Format("Play Area {0}", pa.mChannel);
        //    long absoluteTime = 0;
        //    mCollection.AddEvent(new TextEvent(trackName, MetaEventType.TextEvent, absoluteTime), localTrackNum);
        //    ++absoluteTime;
        //    mCollection.AddEvent(new TempoEvent(CalculateMicrosecondsPerQuaterNote(BeatsPerMinute), absoluteTime), localTrackNum);

        //    int instrument = pa.mInstrument;
        //    int ChannelNumber = pa.mChannel;

        //    mCollection.AddEvent(new PatchChangeEvent(0, ChannelNumber, instrument), localTrackNum);

        //    ControlChangeEvent volumeEvent = new ControlChangeEvent(0, ChannelNumber, MidiController.MainVolume, pa.mVolume);
        //    ControlChangeEvent panEvent = new ControlChangeEvent(0, ChannelNumber, MidiController.Pan, pa.mPan);

        //    mCollection.AddEvent(volumeEvent, localTrackNum);
        //    mCollection.AddEvent(panEvent, localTrackNum);

        //    bool bLoopedSave = melEdInterf.mState.mbSaveMIDIWithLoop;
        //    List<NoteLine> paNoteLines = pa.GetNoteLines();
        //    foreach (var noteLine in paNoteLines)
        //    {
        //        List<Note> ln = null;
           
        //        if(bLoopedSave)
        //        {
        //            ln = noteLine.GetNotesLoopedInRegion(PlayArea.NUM_BARS_FOR_LOOPS_SAVED);
        //        }
        //        else
        //        {
        //            ln = noteLine.GetNotesNoLooping();
        //        }

        //        if(ln==null)
        //        {
        //            continue;
        //        }

        //        foreach (var note in ln)
        //        {
        //            NoteVelocity = note.Velocity;
        //            absoluteTime = (long)((float)TicksPerQuarterNote * note.BeatPos);
        //            NoteDuration = (int)((float)TicksPerQuarterNote * note.BeatLength);

        //            mCollection.AddEvent(new NoteOnEvent(absoluteTime, ChannelNumber, note.mNoteNum, NoteVelocity, NoteDuration), localTrackNum);
        //            mCollection.AddEvent(new NoteEvent(absoluteTime + NoteDuration, ChannelNumber, MidiCommandCode.NoteOff, note.mNoteNum, 0), localTrackNum);
        //        }
        //    }

        //    //List<NoteLine> paNoteLines = pa.GetNoteLines();
        //    //foreach (var noteLine in paNoteLines)
        //    //{
        //    //    List<Note> ln = noteLine.GetNotesNoLooping();
        //    //    foreach (var note in noteLine.GetNotes())
        //    //    {
        //    //        if(note.mRegion != null)
        //    //        {
        //    //            //Don't save out the root note for an ai region that will be saved below.
        //    //            continue;
        //    //        }
        //    //        NoteVelocity = note.Velocity;
        //    //        absoluteTime = (long)((float)TicksPerQuarterNote * note.BeatPos);
        //    //        NoteDuration = (int)((float)TicksPerQuarterNote * note.BeatLength);

        //    //        mCollection.AddEvent(new NoteOnEvent(absoluteTime, ChannelNumber, note.mNoteNum, NoteVelocity, NoteDuration), localTrackNum);
        //    //        mCollection.AddEvent(new NoteEvent(absoluteTime + NoteDuration, ChannelNumber, MidiCommandCode.NoteOff, note.mNoteNum, 0), localTrackNum);
        //    //    }
        //    //    //AI REGION SAVING
        //    //    //TODO we need to screen out the root notes above that are in the regions below
        //    //    //Roots notes seem annoying, maybe dont have them and just create the region notes
        //    //    //when press on screen?
        //    //    //This gets the ai created notes that currently are lost when shut down app 
        //    //    //but can be saved out here and should be able to load in as regular notes
        //    //    foreach (var note in noteLine.GetRegionNotes())
        //    //    {
        //    //        NoteVelocity = note.Velocity;
        //    //        absoluteTime = (long)((float)TicksPerQuarterNote * note.BeatPos);
        //    //        NoteDuration = (int)((float)TicksPerQuarterNote * note.BeatLength);

        //    //        mCollection.AddEvent(new NoteOnEvent(absoluteTime, ChannelNumber, note.mNoteNum, NoteVelocity, NoteDuration), localTrackNum);
        //    //        mCollection.AddEvent(new NoteEvent(absoluteTime + NoteDuration, ChannelNumber, MidiCommandCode.NoteOff, note.mNoteNum, 0), localTrackNum);
        //    //    }
        //    //}
        //    ////HACK! dummy last note to keep playing till 16 beats
        //    absoluteTime = (long)((float)TicksPerQuarterNote * PlayArea.MAX_BEATS);
        //    int dummyLastNoteToDenoteEndBar = 60;
        //    mCollection.AddEvent(new NoteOnEvent(absoluteTime, ChannelNumber, dummyLastNoteToDenoteEndBar, 0, 1), localTrackNum);
        //    NoteDuration = 1;
        //    mCollection.AddEvent(new NoteEvent(absoluteTime + NoteDuration, ChannelNumber, MidiCommandCode.NoteOff, dummyLastNoteToDenoteEndBar, 0), localTrackNum);

        //}

        public void GatherMidiFromTrackToSave(MelodyEditorInterface melEdInterf, string fileName)
        {
            //TODO BETTER CHECK!
            if(fileName=="")
            {
                return;
            }
            int BeatsPerMinute = (int)melEdInterf.BPM;
            const int MidiFileType = 0;
            int ChannelNumber = 1;

            mCollection = new MidiEventCollection(MidiFileType, TicksPerQuarterNote);
            long absoluteTime = 0;
            int NoteVelocity = 100;
            int NoteDuration = 0; //;!

            int instrument = melEdInterf.GetInstrument(ChannelNumber);


            mCollection.AddEvent(new TextEvent("Note Stream", MetaEventType.TextEvent, absoluteTime), TrackNumber);
            ++absoluteTime;
            mCollection.AddEvent(new TempoEvent(CalculateMicrosecondsPerQuaterNote(BeatsPerMinute), absoluteTime), TrackNumber);


            mCollection.AddEvent(new PatchChangeEvent(0, ChannelNumber, instrument), TrackNumber);

            foreach (var noteLine in melEdInterf.GetNoteLines())
            {
                foreach (var note in noteLine.GetNotes())
                {
                    NoteVelocity = note.Velocity;
                    absoluteTime = (long)((float)TicksPerQuarterNote * note.BeatPos);
                    NoteDuration = (int)((float)TicksPerQuarterNote * note.BeatLength);

                    mCollection.AddEvent(new NoteOnEvent(absoluteTime, ChannelNumber, note.mNoteNum, NoteVelocity, NoteDuration), TrackNumber);
                    mCollection.AddEvent(new NoteEvent(absoluteTime + NoteDuration, ChannelNumber, MidiCommandCode.NoteOff, note.mNoteNum, 0), TrackNumber);
                }
            }

            //HACK TEST DIFFERENT INSTRUMENT PLAYING
            //instrument = 88;
            //ChannelNumber = 2;
            //collection.AddEvent(new PatchChangeEvent(0, ChannelNumber, instrument), TrackNumber);

            //foreach (var track in melEdInterf.GetTracks())
            //{
            //    foreach (var note in track.GetNotes())
            //    {
            //        NoteVelocity = note.Velocity;
            //        absoluteTime = (long)((float)TicksPerQuarterNote * (note.BeatPos-1f));
            //        NoteDuration = (int)((float)TicksPerQuarterNote * note.BeatLength/2);

            //        int hackNote = 70;
            //        collection.AddEvent(new NoteOnEvent(absoluteTime, ChannelNumber, hackNote, NoteVelocity, NoteDuration), TrackNumber);
            //        collection.AddEvent(new NoteEvent(absoluteTime + NoteDuration, ChannelNumber, MidiCommandCode.NoteOff, hackNote, 0), TrackNumber);
            //    }
            //}

            //HACK! dummy last note to keep playing till 16 beats
            absoluteTime = (long)((float)TicksPerQuarterNote * PlayArea.MAX_BEATS);
            int dummyLastNoteToDenoteEndBar = 60;
            mCollection.AddEvent(new NoteOnEvent(absoluteTime, ChannelNumber, dummyLastNoteToDenoteEndBar, 0, 1), TrackNumber);
            NoteDuration = 1;
            mCollection.AddEvent(new NoteEvent(absoluteTime + NoteDuration, ChannelNumber, MidiCommandCode.NoteOff, dummyLastNoteToDenoteEndBar, 0), TrackNumber);

#if ANDROID
            var pathFile = OS.Environment.GetExternalStoragePublicDirectory(OS.Environment.DirectoryDownloads);
            string pathFileString = pathFile.ToString();
#else
            //string pathFileString = ".";
            //var pathWithEnv = @"%USERPROFILE%\AppData\";
            //var pathFileString = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var pathFileString = PC_PATH_FILE;

#endif
            stuPath = pathFileString + "/" + SAVE_MIDI_PREFIX + fileName + ".mid";

            melEdInterf.SetSaveName(fileName);

            mCollection.PrepareForExport();
            DoWorkAsync();

        }


        private Task DoWorkAsync() // No async because the method does not need await
        {
            return Task.Run(() =>
            {
                Task.Delay(1000); //maybe needed for PrepareForExport
                MidiFile.Export(stuPath, mCollection); //TODO PC Synchronous save

                if (mbSaveHasLoopRangeData)
                {
                    File.WriteAllText(mJsonLoopRangeSavePath, mJsonLoopRangeData);
                }

                if (!mbSaveIsLooped)
                {
                    File.WriteAllText(mJsonRegionSavePath, mJsonRegionSaveData);
                }

                if(mbSaveHasDrumData)
                {
                    File.WriteAllText(mJsonDrumSavePath, mJsonDrumSaveData);
                }

                if (mbSaveHasParamRegionData)
                {
                    File.WriteAllText(mJsonParamRegionSavePath, mJsonParamRegionSaveData);
                }

            });
        }

        private Task DrumAsyncSave() // No async because the method does not need await
        {
            return Task.Run(() =>
            {
 
                if (mbSaveHasDrumData)
                {
                    File.WriteAllText(mJsonDrumSavePath, mJsonDrumSaveData);
                }

            });
        }

        public static int CalculateMicrosecondsPerQuaterNote(int bpm)
        {
            return 60 * 1000 * 1000 / bpm;
        }

    }
}
