using System;
using System.Collections.Generic;
using System.Text;

namespace MonoMusicMaker
{
    public class AIChordManager
    {

        string[] KEY_NAMES = new string[12] { "C", "C#/Db", "D", "D#/Eb", "E", "F", "F#/Gb", "G", "G#/Ab","A", "A#/Bb", "B" };

        public string GetKeyName(int index)
        {
            return KEY_NAMES[index];
        }
        public string[] GetKeyNameArray()
        {
            return KEY_NAMES;
        }

        ParamPopUp mAIRegionParamPopup;

        // to map to chordTypeNames array below
        public enum eScaleType
        {
            MAJOR,
            MINOR,
            LYDIAN,
            MIXOLYDIAN,
            DORIAN,
            PHRYGIAN,
            LOCRIAN,
            HARM_MINOR,
            num_scales,
        }
        public  string[] scaleNames     = new string[(int)eScaleType.num_scales] {    "MAJOR", "MINOR", "LYDIAN", "MIXOLYDIAN", "DORIAN", "PHRYGIAN", "LOCRIAN", "HARM MINOR"  };
        
        // to map to chordTypeNames array below
        public enum eChordTypeNum
        {
            None,
            I,
            ii,
            iii,
            IV,
            V,
            vi,
            vii,
#if WINDOWS
            scroll,
#endif
            num_chordNums,
        }

        public string[] chordTypeNumbers = new string[(int)eChordTypeNum.num_chordNums] { "--", "I", "ii", "iii", "IV", "V", "vi", "vii"
#if WINDOWS
            ,"Scroll"
#endif
        };



        // to map to chordTypeNames array below
        public enum eChordType
        {
            ROOT_TRIAD,
            SUS_2_TRIAD,
            SUS_4_TRIAD,
            SUS_9_TRIAD,
            INV_1ST_TRIAD,
            INV_2ND_TRIAD,
            QUAD_6TH,
            QUAD_7TH,
            FIVE_9TH,
            FG_5_1,
            FG_1_4,
            num_chords
        }
        string[] chordTypeNames = new string[(int)eChordType.num_chords] {       "ROOT_TRIAD", "SUS_2_TRIAD", "SUS_4_TRIAD", "SUS_9_TRIAD", "INV_1ST_TRIAD", "INV_2ND_TRIAD",
                                                        "QUAD_6TH", "QUAD_7TH",
                                                        "FIVE_9TH","FG_5_1","FG_1_4"};

        public const int INDEX_LAST_CHORD_ARRAY = 8; //keep up to date based on where FG_.. chords start in chordTypeNames above


        // to map to patternNames array below
        public enum ePattern
        {
            None,
            Up,
            Down,
            Adele,
            Ballad,
            SlowRoll,
            QRollUp,
            QRollDown,
            FullChordPair,
            RhythmChord,
            WholeRange,
            Akesson,
            BalladUpDown,
            num_patterns,
        }
        string[] patternNames           = new string[(int)ePattern.num_patterns] { "Chord: None", "Up", "Down", "Adele", "Ballad", "Slow Roll", "Quick Roll up", "Q. Roll down", "Full pair", "Rhythm Chord ", "Whole range", "Akesson", "Ballad U/D"};

        string[] beatSubDivTypeNames = new string[4] { "2 Beat", "4 Beat", "8 Beat", "16 Beat" };
        string[] bassBeatSubDivTypeNames = new string[4] { "Bass 2 Beat", "Bass 4 Beat", "Bass 8 Beat", "Bass 16 Beat" };
        // to map to patternNames array below
        public enum eBassPattern
        {
            None,
            Single,
            Octave,
            Arpeg,
            Akesson,
            WalkUp,
            WalkDown,
            num_patterns,
        }
        string[] bassPattern = new string[(int)eBassPattern.num_patterns]            { "Bass: None" , "Bass Single", "Bass Octave", "Bass Arpeg.","Akesson", "Walk Up", "Walk Down" };

        //INitialise the passed objetc t have all input parameters set up for chord type, arpeggio etc
        public void InitPopUpAI(ParamPopUp regionppu)
        {
            mAIRegionParamPopup = regionppu;
            //SetUpRegionPopupParamChordBar();
            //SetUpPlayPatternPopupParamChordBar();
            //SetUpRegionPopupParamBeatSubdivisionBar();
            SetUpRegionPopupParamBars();
        }

        /// ////// TEST PARAMETER POP UP BAR
        public void ChordTypeCB(Button button, MelodyEditorInterface.MEIState state)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("ChordTypeCB value {0}", button.mGED.mValue));
        }

        public void PlayPatternTypeCB(Button button, MelodyEditorInterface.MEIState state)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("PlayPatternTypeCB value {0}", button.mGED.mValue));
        }

        public void BeatSubdivisionCB(Button button, MelodyEditorInterface.MEIState state)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("BeatSubdivisionCB value {0}", button.mGED.mValue));
        }

        void SetUpRegionPopupParamBars()
        {
            //Set in order appears left to right in pop up
            SetUpRegionPopupParamBar(scaleNames);
            SetUpRegionPopupParamBar(chordTypeNames);
            SetUpRegionPopupParamBar(patternNames);
            SetUpRegionPopupParamBar(beatSubDivTypeNames);
            SetUpRegionPopupParamBar(bassPattern);
            SetUpRegionPopupParamBar(bassBeatSubDivTypeNames);
        }

        void SetUpRegionPopupParamBar(string [] typeNames)
        {
            List<ButtonGrid.GridEntryData> aiGED = new List<ButtonGrid.GridEntryData>();
            int count = 0;

            foreach (string str in typeNames)
            {
                ButtonGrid.GridEntryData aiGEDEntry;

                aiGEDEntry.mOnString = str;
                aiGEDEntry.mValue = count;
                aiGEDEntry.mClickOn = ChordTypeCB;
                count++;
                aiGED.Add(aiGEDEntry);
            }

            ParamButtonBar pbb = new ParamButtonBar(aiGED);

            mAIRegionParamPopup.AddParamBar(pbb);
        }
    }
}
