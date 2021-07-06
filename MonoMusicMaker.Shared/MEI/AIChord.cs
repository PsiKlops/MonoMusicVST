using System;
using System.Collections.Generic;
using System.Text;

namespace MonoMusicMaker
{
    public class AIChord
    {
        //https://en.wikipedia.org/wiki/Pitch_class#Integer_notation


        public static readonly int[] MINOR_SECOND   = { 0, 1 };
        public static readonly int[] SECOND_INTERVAL= { 0, 2 };
        public static readonly int[] MINOR_THIRD    = { 0, 3 };
        public static readonly int[] THIRD_INTERVAL = { 0, 4 };
        public static readonly int[] FOURTH_INTERVAL= { 0, 5 };
        //public static readonly int[] SIXTH_INTERVAL = { 0, 6 };
        public static readonly int[] FIFTH_INTERVAL = { 0, 7 };
        public static readonly int[]    MINOR_SIXTH = { 0, 8 };
        public static readonly int[] SIXTH_INTERVAL = { 0, 9 };

        public static readonly int[] MINOR_SEVENTH = { 0, 10 };
        public static readonly int[] SEVENTH_INTERVAL = { 0, 11 };

        public static readonly int[] OCTAVE_INTERVAL = { 0, 12 };

        public const int OCTAVE_SPAN_SEMITONES = 12;
        public const int OCTAVE_SPAN_NOTES = 8;
        public static readonly int[] LYDIAN_SCALE = new int[] { 0, 2, 4, 6, 7, 9, 11, 12 };
        public static readonly int[] IONIAN_SCALE = new int[] { 0, 2, 4, 5, 7, 9, 11, 12 }; //MAJOR
        public static readonly int[] MIXOLYDIAN_SCALE = new int[] { 0, 2, 4, 5, 7, 9, 10, 12 };
        public static readonly int[] DORIAN_SCALE = new int[] { 0, 2, 3, 5, 7, 9, 10, 12 };
        public static readonly int[] AEOLIAN_SCALE = new int[] { 0, 2, 3, 5, 7, 8, 10, 12 }; //MINOR
        public static readonly int[] PHRYGIAN_SCALE = new int[] { 0, 1, 3, 5, 7, 8, 10, 12 };
        public static readonly int[] LOCRIAN_SCALE = new int[] { 0, 1, 3, 5, 6, 8, 10, 12 };

        public static readonly int[] NATURAL_MAJOR_SCALE = IONIAN_SCALE; //[0, 2, 4, 5, 7, 9, 11]
        public static readonly int[] NATURAL_MINOR_SCALE = AEOLIAN_SCALE;   //[0, 2, 3, 5, 7, 8, 10]

        public static readonly int[] HARMONIC_MINOR_SCALE = { 0, 2, 3, 5, 7, 8, 11, 12 };


        public static readonly int[] MAJOR_1ST_INVERSION = { -8, -5, 0 };
        public static readonly int[] MAJOR_2ND_INVERSION = { -5, 0, 4 };

        //https://en.wikipedia.org/wiki/List_of_chords
        public static readonly int[] AUGMENTED = { 0, 4, 8 };
        public static readonly int[] AUGMENTED_11TH = { 0, 4, 7, 10, 2, 6 };
        public static readonly int[] AUGMENTED_MAJ_7TH = { 0, 4, 8, 11 };
        public static readonly int[] AUGMENTED_7TH = { 0, 4, 8, 10 };
        public static readonly int[] AUGMENTED_6TH = { 0, 6, 8 };
        public static readonly int[] DIMINISHED = { 0, 3, 6 };
        public static readonly int[] DIMINISHED_MAJ_7TH = { 0, 3, 6, 11 };
        public static readonly int[] DIMINISHED_7TH = { 0, 3, 6, 9 };


        public static readonly int[] MAJOR = { 0, 4, 7 };  // notes through scale

        public static readonly int[] DOMINANT_11TH = { 0, 4, 7, 10, 2, 5 };
        public static readonly int[] DOMINANT_MINOR_9TH = { 0, 4, 7, 10, 1 };
        public static readonly int[] DOMINANT_9TH = { 0, 4, 7, 10, 2 };
        public static readonly int[] DOMINANT_7TH = { 0, 4, 7, 10, 2 };

        public static readonly int[] MAJOR_11TH = { 0, 4, 7, 11, 2, 5 };
        public static readonly int[] MAJOR_6TH = { 0, 4, 7, 9 };
        public static readonly int[] MAJOR_7TH = { 0, 4, 7, 11 };
        public static readonly int[] MAJOR_9TH = { 0, 4, 7, 11, 2 };
        public static readonly int[] MAJOR_13TH = { 0, 4, 7, 11, 2, 6, 9 };
        public static readonly int[] MAJOR_7TH_SHRP11TH = { 0, 4, 8, 11, 6 };
        public static readonly int[] MAJOR_6TH_ADD_9TH = { 0, 4, 7, 9, 2 };

        public static readonly int[] MINOR = { 0, 3, 7 };
        public static readonly int[] MINOR_MAJOR_7TH = { 0, 3, 7, 11 };
        public static readonly int[] MINOR_7TH = { 0, 3, 7, 10 };
        public static readonly int[] MINOR_9TH = { 0, 3, 7, 10, 2 };


        public static readonly int[] MINOR_11TH = { 0, 3, 7, 10, 2, 5 };

        public static readonly int[] MINOR_6TH = { 0, 3, 7, 9 };
        public static readonly int[] MINOR_6TH_9TH = { 0, 3, 7, 9, 2 };
        public static readonly int[] MINOR_13TH = { 0, 3, 7, 10, 2, 5, 9 };

        //THESE CAN BE USED FOR MAJOR AND MINOR SCALES - THIS FORMAT CANT BE USED FOR AUGMENTED CHORDS AS THEY NEED TO SEMITONE ADJUST OUTSIDE EXISTING SCALES :(
        //Not interger notation but actual scale position - corrected -1 for array offset
        public static readonly int[] ROOT_TRIAD = { 0, 2, 4 }; //1,3,5
        public static readonly int[] SUS_2_TRIAD = { 0, 1, 4 }; //1,2,5
        public static readonly int[] SUS_4_TRIAD = { 0, 3, 4 }; //1,4,5
        public static readonly int[] SUS_9_TRIAD = { 0, 8, 4 }; //1,9,5

        public static readonly int[] INV_1ST_TRIAD = { 0, -4, -6 }; // negative numbers count back chords assuming 8 entries
        public static readonly int[] INV_2ND_TRIAD = { -4, 0, 2 }; //

        public static readonly int[] QUAD_6TH = { 0, 2, 4, 5 }; //
        public static readonly int[] QUAD_7TH = { 0, 2, 4, 6 }; //

        public static readonly int[] FIVE_9TH = { 0, 2, 4, 6, 8 }; //

        public static List<int[]> SCALE_LIST = new List<int[]>
        {
            NATURAL_MAJOR_SCALE, //IONIAN_SCALE
            NATURAL_MINOR_SCALE, //AEOLIAN_SCALE

            LYDIAN_SCALE,
            MIXOLYDIAN_SCALE,
            DORIAN_SCALE,
            PHRYGIAN_SCALE,
            LOCRIAN_SCALE,
            HARMONIC_MINOR_SCALE,
        };


        //Keep these names same as CHORD_TYPE_LIST below 
        public enum ChordType
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
        }

        public static List<int[]> CHORD_TYPE_LIST = new List<int[]>
        {
          ROOT_TRIAD,
          SUS_2_TRIAD,
          SUS_4_TRIAD,
          SUS_9_TRIAD,

          INV_1ST_TRIAD,
          INV_2ND_TRIAD,

          QUAD_6TH ,
          QUAD_7TH ,

          FIVE_9TH ,
        };

        public static List<int[]> AUG_CHORD_LIST = new List<int[]>
        {
            AUGMENTED,
            AUGMENTED_11TH,
            AUGMENTED_MAJ_7TH
        };

        public const int PATTERN_CODE_SKIP_NOTE = -1;
        public const int PATTERN_CODE_SKIP_REST = -2;
        public const int PATTERN_CODE_OCTAVE_DOWN = -3;
        public const int PATTERN_CODE_TWO_OCTAVE_DOWN = -4;
        public const int PATTERN_CODE_QUICK_ROLL_UP = -5;
        public const int PATTERN_CODE_QUICK_ROLL_DOWN = -6;
        public const int PATTERN_CODE_FULL_CHORD = -7;
        public const int PATTERN_CODE_HOLD_ACROSS_RANGE = -8;

        public const int PATTERN_CODE_JOIN_START = -30;
        public const int PATTERN_CODE_FIRST_JOIN = PATTERN_CODE_JOIN_START;
        public const int PATTERN_CODE_SECOND_JOIN = PATTERN_CODE_JOIN_START - 1;
        public const int PATTERN_CODE_THIRD_JOIN = PATTERN_CODE_JOIN_START - 2;
        public const int PATTERN_CODE_FOURTH_JOIN = PATTERN_CODE_JOIN_START - 3;
        public const int PATTERN_CODE_FIFTH_JOIN = PATTERN_CODE_JOIN_START - 4;
        public const int PATTERN_CODE_SIXTH_JOIN = PATTERN_CODE_JOIN_START - 5;
        public const int PATTERN_CODE_SEVENTH_JOIN = PATTERN_CODE_JOIN_START - 6;
        public const int PATTERN_CODE_EIGHTH_JOIN = PATTERN_CODE_JOIN_START - 7;


        //These next two get notes directly from their position  in the scale and are not using the relative chord position - bit yuck I know - sort of grew into this
        public const int PATTERN_CODE_SCALE_NOTE_OCT_BELOW_START = -40;
        public const int PATTERN_CODE_FIRST_SCALE_NOTE_OCT_BELOW = PATTERN_CODE_SCALE_NOTE_OCT_BELOW_START;
        public const int PATTERN_CODE_SECOND_SCALE_NOTE_OCT_BELOW = PATTERN_CODE_SCALE_NOTE_OCT_BELOW_START - 1;
        public const int PATTERN_CODE_THIRD_SCALE_NOTE_OCT_BELOW = PATTERN_CODE_SCALE_NOTE_OCT_BELOW_START - 2;
        public const int PATTERN_CODE_FOURTH_SCALE_NOTE_OCT_BELOW = PATTERN_CODE_SCALE_NOTE_OCT_BELOW_START - 3;
        public const int PATTERN_CODE_FIFTH_SCALE_NOTE_OCT_BELOW = PATTERN_CODE_SCALE_NOTE_OCT_BELOW_START - 4;
        public const int PATTERN_CODE_SIXTH_SCALE_NOTE_OCT_BELOW = PATTERN_CODE_SCALE_NOTE_OCT_BELOW_START - 5;
        public const int PATTERN_CODE_SEVENTH_SCALE_NOTE_OCT_BELOW = PATTERN_CODE_SCALE_NOTE_OCT_BELOW_START - 6;
        public const int PATTERN_CODE_EIGHTH_SCALE_NOTE_OCT_BELOW = PATTERN_CODE_SCALE_NOTE_OCT_BELOW_START - 7;

        public const int PATTERN_CODE_SCALE_NOTE_START = -50;
        public const int PATTERN_CODE_FIRST_SCALE_NOTE = PATTERN_CODE_SCALE_NOTE_START;
        public const int PATTERN_CODE_SECOND_SCALE_NOTE = PATTERN_CODE_SCALE_NOTE_START - 1;
        public const int PATTERN_CODE_THIRD_SCALE_NOTE = PATTERN_CODE_SCALE_NOTE_START - 2;
        public const int PATTERN_CODE_FOURTH_SCALE_NOTE = PATTERN_CODE_SCALE_NOTE_START - 3;
        public const int PATTERN_CODE_FIFTH_SCALE_NOTE = PATTERN_CODE_SCALE_NOTE_START - 4;
        public const int PATTERN_CODE_SIXTH_SCALE_NOTE = PATTERN_CODE_SCALE_NOTE_START - 5;
        public const int PATTERN_CODE_SEVENTH_SCALE_NOTE = PATTERN_CODE_SCALE_NOTE_START - 6;
        public const int PATTERN_CODE_EIGHTH_SCALE_NOTE = PATTERN_CODE_SCALE_NOTE_START - 7;


        //Thes order apply to whatever order notes appear in chord from low to top, so inverted ones will need consideration probably?
        public static readonly int[] SPACE = { PATTERN_CODE_SKIP_NOTE }; // Wont play at this point skip note there
        public static readonly int[] SPACE_UNTIL = { PATTERN_CODE_SKIP_REST }; // Pattern will continue ignoring
        public static readonly int[] OCTAVE_DOWN = { PATTERN_CODE_OCTAVE_DOWN }; // Note is octave lower than root
        public static readonly int[] TWO_OCTAVE_DOWN = { PATTERN_CODE_TWO_OCTAVE_DOWN }; // Note is 2 octaves lower than root
        public static readonly int[] LOW_OCTAVE_PAIR = { PATTERN_CODE_OCTAVE_DOWN, PATTERN_CODE_TWO_OCTAVE_DOWN }; // Two lower notes together octave apart

        public static readonly int[] FULL_CHORD = { PATTERN_CODE_FULL_CHORD }; // Two lower notes together octave apart
        public static readonly int[] HOLD_CHORD = { PATTERN_CODE_HOLD_ACROSS_RANGE }; // Two lower notes together octave apart
        public static readonly int[] QUCK_ROLL_UP = { PATTERN_CODE_QUICK_ROLL_UP }; // Two lower notes together octave apart
        public static readonly int[] QUCK_ROLL_DOWN = { PATTERN_CODE_QUICK_ROLL_DOWN }; // Two lower notes together octave apart

        public static readonly int[] SECOND_DOWN = { PATTERN_CODE_SECOND_SCALE_NOTE_OCT_BELOW }; //
        public static readonly int[] THIRD_DOWN = { PATTERN_CODE_THIRD_SCALE_NOTE_OCT_BELOW }; //
        public static readonly int[] FOURTH_DOWN = { PATTERN_CODE_FOURTH_SCALE_NOTE_OCT_BELOW }; //
        public static readonly int[] FIFTH_DOWN = { PATTERN_CODE_FIFTH_SCALE_NOTE_OCT_BELOW }; //
        public static readonly int[] SIXTH_DOWN = { PATTERN_CODE_SIXTH_SCALE_NOTE_OCT_BELOW }; //
        public static readonly int[] SEVENTH_DOWN = { PATTERN_CODE_SEVENTH_SCALE_NOTE_OCT_BELOW }; //

        public static readonly int[] FIRST_SCALE_NOTE = { PATTERN_CODE_FIRST_SCALE_NOTE }; //
        public static readonly int[] SECOND_SCALE_NOTE = { PATTERN_CODE_SECOND_SCALE_NOTE }; //
        public static readonly int[] THIRD_SCALE_NOTE = { PATTERN_CODE_THIRD_SCALE_NOTE }; //
        public static readonly int[] FOURTH_SCALE_NOTE = { PATTERN_CODE_FOURTH_SCALE_NOTE }; //
        public static readonly int[] FIFTH_SCALE_NOTE = { PATTERN_CODE_FIFTH_SCALE_NOTE }; //
        public static readonly int[] SIXTH_SCALE_NOTE = { PATTERN_CODE_SIXTH_SCALE_NOTE }; //
        public static readonly int[] SEVENTH_SCALE_NOTE = { PATTERN_CODE_SEVENTH_SCALE_NOTE }; //

        public static readonly int[] FIRST = { 0 }; //
        public static readonly int[] SECOND = { 1 }; //
        public static readonly int[] THIRD = { 2 }; //
        public static readonly int[] FOURTH = { 3 }; //
        public static readonly int[] FIFTH = { 4 }; //
        public static readonly int[] SIXTH = { 5 }; //
        public static readonly int[] SEVENTH = { 6 }; //
        public static readonly int[] EIGHT = { 7 }; //Octave
        public static readonly int[] SECOND_THIRD = { 1, 2 }; //
        public static readonly int[] OUTSIDE = { 0, 4 }; //

        public static readonly int[] FIRST_J = { PATTERN_CODE_FIRST_JOIN }; // Notes without gaps - the J signifies join with next one if contiguous
        public static readonly int[] SECOND_J = { PATTERN_CODE_SECOND_JOIN }; //
        public static readonly int[] THIRD_J = { PATTERN_CODE_THIRD_JOIN }; //
        public static readonly int[] FOURTH_J = { PATTERN_CODE_FOURTH_JOIN }; //
        public static readonly int[] FIFTH_J = { PATTERN_CODE_FIFTH_JOIN }; //
        public static readonly int[] SIXTH_J = { PATTERN_CODE_SIXTH_JOIN }; //
        public static readonly int[] SEVENTH_J = { PATTERN_CODE_SEVENTH_JOIN }; //
        public static readonly int[] EIGHT_J = { PATTERN_CODE_EIGHTH_JOIN }; //Octave

        //Works with 3 note chord 0,1,2,1 - TODO go up to TENTH for potential number of fingers in a roll?
        public static List<int[]> ROLL_PATTERN = new List<int[]>
        {
            FIRST,
            SECOND,
            THIRD,
            FOURTH,
            FIFTH,
            SIXTH,
            SEVENTH,
            EIGHT,
        };

        //Works with 3 note chord 0,1,2,1 - TODO go up to TENTH for potential number of fingers in a roll?
        public static List<int[]> AKESSON_PATTERN = new List<int[]>
        {
            FIRST_J,
            FIRST_J,
            SPACE,
            FIRST_J,
            FIRST_J,
            SPACE,
            FIRST_J,
            FIRST_J,
            SPACE,
            FIRST_J,
            FIRST_J,
            SPACE,
            FIRST_J,
            FIRST_J,
            FIRST_J,
            FIRST_J,
         };
        //Works with 3 note chord 0,1,2,1 - TODO go up to TENTH for potential number of fingers in a roll?
        public static List<int[]> AKESSON_BASS_PATTERN = new List<int[]>
        {
            SPACE,
            SPACE,
            OCTAVE_DOWN,
            SPACE,
            SPACE,
            OCTAVE_DOWN,
            SPACE,
            SPACE,
            OCTAVE_DOWN,
            SPACE,
            SPACE,
            OCTAVE_DOWN,
            SPACE,
            SPACE,
            SPACE,
            SPACE,
         };

        // Simple Walking Bass Line For Jazz Beginners (Autumn Leaves) https://www.talkingbass.net/simple-walking-bass-line-for-jazz-beginners/ https://www.youtube.com/watch?v=76azkeDhdCA 
        public static List<int[]> WALKUP_BASS_PATTERN = new List<int[]>
        {
            FIRST_SCALE_NOTE,
            SECOND_SCALE_NOTE,
            THIRD_SCALE_NOTE,
            FIFTH_SCALE_NOTE,
        };

        public static List<int[]> WALKDOWN_BASS_PATTERN = new List<int[]>
        {
            FIRST_SCALE_NOTE,
            SEVENTH_DOWN,
            SIXTH_DOWN,
            FIFTH_DOWN,
        };

        //Works with 3 note chord 0,1,2,1 - TODO go up to TENTH for potential number of fingers in a roll?
        public static List<int[]> RHYTHM_CHORD = new List<int[]>
        {
            FULL_CHORD,
            SPACE,
            SPACE,
            FULL_CHORD,
            FULL_CHORD,
            FULL_CHORD,
            SPACE,
            FULL_CHORD,
        };

        //Works with 3 note chord 0,1,2,1
        public static List<int[]> ADELE_PATTERN = new List<int[]>
        {
            FIRST,
            SECOND,
            THIRD,
            SECOND,
        };
        //Works with 3 note chord 0, then 2 & 3 together
        public static List<int[]> BALLAD_PATTERN = new List<int[]>
        {
            FIRST,
            SECOND_THIRD,
        };
        //Works with 3 note chord 0, then 2 & 3 together
        public static List<int[]> BALLAD_PATTERN_UP_DOWN = new List<int[]>
        {
           SECOND_THIRD,
           FIRST,
        };
        //Works with 3 note chord 0, then 2 & 3 together
        public static List<int[]> BALLAD_PATTERN_OUTSIDE_IN = new List<int[]>
        {
            FIRST,
            SECOND_THIRD,
        };

        //Works with 1 note chord 
        public static List<int[]> BASS_SINGLE_PATTERN = new List<int[]>
        {
            OCTAVE_DOWN,
            SPACE_UNTIL,
        };
        //Works with 1 note chord 
        public static List<int[]> BASS_OCTAVE_PATTERN = new List<int[]>
        {
            LOW_OCTAVE_PAIR,
            SPACE_UNTIL,
        };
        //Works with 1 note chord 
        public static List<int[]> QUICK_ROLL_UP_PATTERN = new List<int[]>
        {
            QUCK_ROLL_UP,
            QUCK_ROLL_UP,
            SPACE_UNTIL,
        };
        //Works with 1 note chord 
        public static List<int[]> QUICK_ROLL_DOWN_PATTERN = new List<int[]>
        {
            QUCK_ROLL_DOWN,
            QUCK_ROLL_DOWN,
            SPACE_UNTIL,
        };
        //Works with 1 note chord 
        public static List<int[]> FULL_CHORD_PAIR_PATTERN = new List<int[]>
        {
            FULL_CHORD,
            FULL_CHORD,
        };
        //Works with 1 note chord 
        public static List<int[]> CHORD_ACROSS_WHOLE_RANGE_PATTERN = new List<int[]>
        {
            FULL_CHORD,
            HOLD_CHORD,
        };


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// FUNCTIONS COPIED FROM C++ CODE IN MusicMakerVST
        /// 
        public static readonly int[] AUGMENTED_6TH_IT = { 0, 6, 8 };
        public static readonly int[] AUGMENTED_6TH_FR = { 0, 4, 6, 10 };
        public static readonly int[] AUGMENTED_6TH_GER = { 0, 4, 7, 10 };
        //Suspended
        public static readonly int[] SUS_4 = { 0, 5, 7 };
        public static readonly int[] SUS_2 = { 0, 2, 7 };
        public static readonly int[] SUS_2_ADD_4 = { 0, 2, 5, 7 };

        Dictionary<string, int[]> mChordStringLookUp = new Dictionary<string, int[]>();
        Dictionary<string, int[]> mExcludeFromInversion = new Dictionary<string, int[]>();

        string[] InversionNames = new string[5]  { "First", "Second", "Third", "Fourth", "Fifth" };

        public int mCurrentInversionLevel = 0;


        public AIChord()
        {
            InitChordLookUp();
        }

        public void InitChordLookUp()
        {
            //2 Note
            mChordStringLookUp.Add("min_2nd", MINOR_SECOND);
            mChordStringLookUp.Add("2nd", SECOND_INTERVAL);

            mChordStringLookUp.Add("min_3rd", MINOR_THIRD);
            mChordStringLookUp.Add("3rd", THIRD_INTERVAL);

            mChordStringLookUp.Add("4th", FOURTH_INTERVAL);

            mChordStringLookUp.Add("5th", FIFTH_INTERVAL);

            mChordStringLookUp.Add("min_6th", MINOR_SIXTH);
            mChordStringLookUp.Add("6th", SIXTH_INTERVAL);
            mChordStringLookUp.Add("min_7th", MINOR_SEVENTH);
            mChordStringLookUp.Add("7th", SEVENTH_INTERVAL);

            mChordStringLookUp.Add("oct", OCTAVE_INTERVAL);

            //public static readonly int[] MINOR_SECOND = { 0, 1 };
            //public static readonly int[] SECOND_INTERVAL = { 0, 2 };
            //public static readonly int[] MINOR_THIRD = { 0, 3 };
            //public static readonly int[] THIRD_INTERVAL = { 0, 4 };
            //public static readonly int[] FOURTH_INTERVAL = { 0, 5 };
            ////public static readonly int[] SIXTH_INTERVAL = { 0, 6 };
            //public static readonly int[] FIFTH_INTERVAL = { 0, 7 };
            //public static readonly int[] MINOR_SIXTH = { 0, 8 };
            //public static readonly int[] SIXTH_INTERVAL = { 0, 9 };

            //public static readonly int[] MINOR_SEVENTH = { 0, 10 };
            //public static readonly int[] SEVENTH_INTERVAL = { 0, 11 };

            //public static readonly int[] OCTAVE_INTERVAL = { 0, 12 };


            //3 Note
            mChordStringLookUp.Add("MAJOR", MAJOR);
            mChordStringLookUp.Add("MINOR", MINOR);

            mChordStringLookUp.Add("AUGMENTED", AUGMENTED);
            mChordStringLookUp.Add("AUGMENTED_6TH_IT", AUGMENTED_6TH_IT);

            mChordStringLookUp.Add("DIMINISHED", DIMINISHED);

            //Exclude
            mExcludeFromInversion.Add("SUS_2", SUS_2);
            mExcludeFromInversion.Add("SUS_4", SUS_4);
            mExcludeFromInversion.Add("SUS_2_ADD_4", SUS_2_ADD_4);

            //4 Note
            mChordStringLookUp.Add("MAJOR_6TH", MAJOR_6TH);
            mChordStringLookUp.Add("MAJOR_7TH", MAJOR_7TH);

            mChordStringLookUp.Add("MINOR_MAJOR_7TH", MINOR_MAJOR_7TH);
            mChordStringLookUp.Add("MINOR_6TH", MINOR_6TH);
            mChordStringLookUp.Add("MINOR_7TH", MINOR_7TH);

            mChordStringLookUp.Add("AUGMENTED_7TH", AUGMENTED_7TH);
            mChordStringLookUp.Add("AUGMENTED_MAJ_7TH", AUGMENTED_MAJ_7TH);
            mChordStringLookUp.Add("AUGMENTED_6TH_FR", AUGMENTED_6TH_FR);
            mChordStringLookUp.Add("DOMINANT_7TH", DOMINANT_7TH);

            mChordStringLookUp.Add("DIMINISHED_7TH", DIMINISHED_7TH);
            mChordStringLookUp.Add("DIMINISHED_MAJ_7TH", DIMINISHED_MAJ_7TH);


            //5 or higher note - cycles to over 12th semitone
            mChordStringLookUp.Add("AUGMENTED_11TH", AUGMENTED_11TH);
            mChordStringLookUp.Add("MAJOR_9TH", MAJOR_9TH);
            mChordStringLookUp.Add("MAJOR_13TH", MAJOR_13TH);

            mChordStringLookUp.Add("MAJOR_11TH", MAJOR_11TH);
            mChordStringLookUp.Add("MAJOR_7TH_SHRP11TH", MAJOR_7TH_SHRP11TH);
            mChordStringLookUp.Add("MAJOR_6TH_ADD_9TH", MAJOR_6TH_ADD_9TH);

            mChordStringLookUp.Add("MINOR_6TH_9TH", MINOR_6TH_9TH);
            mChordStringLookUp.Add("MINOR_11TH", MINOR_11TH);

            mChordStringLookUp.Add("MINOR_13TH", MINOR_13TH);

        }

        public string GetChordFromAILookUp(int[] chordNoteList)
        {
            int lenChordIn = chordNoteList.Length;

            if (lenChordIn == 0)
            {
                return "-";
            }

            mCurrentInversionLevel = 0;

            string inverseLevel = "";

            int rootNoteNum = chordNoteList[0]; //TODO ths should be lowest! and we have to work out inversion sometime assume root is always lowest for now.

            foreach (KeyValuePair<string, int[]> it in mChordStringLookUp)
            {
                // do something with entry.Value or entry.Key
                if (it.Value.Length == lenChordIn)
                {
                    bool bFound = AreChordPatternsSame(chordNoteList, it.Value);
                    if (bFound)
                    {
                        return it.Key;
                    }
                    else
                    {
                        if (lenChordIn == 3 || lenChordIn == 4) //just find inversions for triads for now
                        {
                            List<int> chordNoteListOut = new List<int>();
                            int[] chordNoteListIn = it.Value;

                            int checkInversionCount = lenChordIn - 1; //only check for this amount otherwise cycle back to root chord!

                            for (int j = 0; j < checkInversionCount; j++)
                            {
                                inverseLevel = " " + InversionNames[j] + " Inversion";
                                chordNoteListOut.Clear();

                                InvertChord(chordNoteListIn, ref chordNoteListOut);

                                bFound = AreChordPatternsSame(chordNoteList, chordNoteListOut.ToArray());
                                if (bFound)
                                {
                                    mCurrentInversionLevel = j + 1;
                                    return it.Key + inverseLevel;
                                }
                                chordNoteListIn = chordNoteListOut.ToArray(); //go round again
                            }
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, int[]> it in mExcludeFromInversion)
            {
                if (it.Value.Length == lenChordIn)
                {
                    bool bFound = AreChordPatternsSame(chordNoteList, it.Value);
                    if (bFound)
                    {
                        return it.Key;
                    }
                }
            }

            return "";
        }

        bool AreChordPatternsSame(int[] chordNoteList1, int[] chordNoteList2)
        {
            int chordSize = chordNoteList1.Length;
            System.Diagnostics.Debug.Assert(chordNoteList1.Length == chordNoteList2.Length);

            for (int i = 0; i < chordSize; i++)
            {
                //Compare each element if one is not the same then they dont map and this chord type is not right
                if (chordNoteList1[i] != chordNoteList2[i])
                {
                    return false; //break out for loop checking this chord
                }
            }
            return true;
        }

        //Takes a given chord of ascending relative notes and turns into 1st inversion
        //can be be repeatedly called on a given chordlist to get further inversions, 2nd, 3rd etc
        //TODO assumes not extended chord i.e. offsets don't go above octave - 12
        bool InvertChord(int[] chordNoteListIn, ref List<int> chordNoteListOut)
        {
            int chordSize = chordNoteListIn.Length;
            int chordSizeLess1 = chordSize - 1;

            System.Diagnostics.Debug.Assert(chordSize > 0);
            System.Diagnostics.Debug.Assert(chordNoteListOut.Count == 0);

            int lastNoteOffset = chordNoteListIn[chordSizeLess1];

            System.Diagnostics.Debug.Assert(lastNoteOffset < OCTAVE_SPAN_SEMITONES);

            int diffEndToOctave = OCTAVE_SPAN_SEMITONES - lastNoteOffset;

            chordNoteListOut.Add(0); //Always starts with offset 0

            //for (std::vector<int>::const_iterator itr = chordNoteListIn.begin(), end = std::prev(chordNoteListIn.end());
            // itr != end; ++itr, i++)
            for (int i=0;i< chordSizeLess1;i++)
            {

                int adjustNoteOffset = chordNoteListIn[i] + diffEndToOctave;
                chordNoteListOut.Add(adjustNoteOffset);
            }

            return true;
        }

        public void GetNormalisedChordIntervalsFromBottomUp(List<int> rawChordListIn, ref List<int>  chordListOut)
        {
            int sizeChordIn = rawChordListIn.Count;

            int startNoteNum = rawChordListIn[sizeChordIn-1]; //Different to the VST C++ Version, the notes start from high to low
            int startOctaveNum = startNoteNum / AIChord.OCTAVE_SPAN_SEMITONES;

            int lastNoteNum = 0; //always set to be root
            int lastOctaveNum = 0; //always set to be root

	        for (int i= sizeChordIn-1; i>=0 ; i--) //Different to the VST C++ Version, the notes start from high to low so go backwards
            {
		        int noteOffsetNum = rawChordListIn[i]- startNoteNum;
                int normalisedNoteOffsetNum = noteOffsetNum % AIChord.OCTAVE_SPAN_SEMITONES;
                int octaveOffsetNum = noteOffsetNum / AIChord.OCTAVE_SPAN_SEMITONES;

                bool bNoteAddedToAccumulatedChord = true;

		        if (octaveOffsetNum == lastOctaveNum)
		        {
			        //same octave as last so just add, must be higher within octave, cant get two same notes within the same octave, and we expect them in rising order
			        chordListOut.Add(noteOffsetNum);
		        }
		        else
		        {
                    System.Diagnostics.Debug.Assert(octaveOffsetNum > lastOctaveNum);
                    int octDiff = octaveOffsetNum - lastOctaveNum;
			        if (octDiff > 1)
			        {
				        //Jumped up more than one octave since last entered note so correct down to just one octave above
				        noteOffsetNum -= (AIChord.OCTAVE_SPAN_SEMITONES* (octDiff - 1));
				        octaveOffsetNum = noteOffsetNum / AIChord.OCTAVE_SPAN_SEMITONES;
			        }

                    int noteDiff = noteOffsetNum - lastNoteNum;

			        if (noteDiff == AIChord.OCTAVE_SPAN_SEMITONES)
			        {
				        //Skip add as note is the same but in the next octave
				        bNoteAddedToAccumulatedChord = false;
			        }
			        else if (noteDiff<AIChord.OCTAVE_SPAN_SEMITONES)
			        {
				        //We can add this note
				        bNoteAddedToAccumulatedChord = AddIntIfNotInList(ref chordListOut, noteOffsetNum);
			        }
			        else
			        {
				        noteOffsetNum -= AIChord.OCTAVE_SPAN_SEMITONES ;
				        octaveOffsetNum = noteOffsetNum / AIChord.OCTAVE_SPAN_SEMITONES;
				        bNoteAddedToAccumulatedChord = AddIntIfNotInList(ref chordListOut, noteOffsetNum);
			        }
		        }

		        if (bNoteAddedToAccumulatedChord)
		        {
			        lastOctaveNum = octaveOffsetNum;
			        lastNoteNum = noteOffsetNum; //always set to be root
		        }
	        }
        }

        bool AddIntIfNotInList(ref List<int> intListInOut, int intIn)
        {
            //CHeck if this note is the same as an existing one in the chord even if in different (higher) octave
            int normalisedNoteOffsetNum = intIn % AIChord.OCTAVE_SPAN_SEMITONES;

            foreach(int i in intListInOut)
            {
                int itNormalisedNote = i % AIChord.OCTAVE_SPAN_SEMITONES;
                if (itNormalisedNote == normalisedNoteOffsetNum)
                {
                    return false;
                }
            }

            intListInOut.Add(intIn);

            return true;
        }
    }

}
