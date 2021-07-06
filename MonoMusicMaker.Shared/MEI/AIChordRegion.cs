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

namespace MonoMusicMaker
{
    using BeatIntersectionNoteList = List<int>;

    public class AIChordRegion
    {

        public float BeatPos { get; set; }
        public float BeatLength { get; set; }
        public float BeatEndPos { get; set; }
        public float ExtendedEndPos { get; set; } //when chords stetched beyond region

        public Color Col { set; get; } = Color.SaddleBrown;
        bool mInDrawRange = false;
        public Rectangle mRect;
        TopLineChordAI mParent;
        public bool Active { get; set; } = false;
        public bool ChangePopupAfterRefreshToData { get; set; } = false; //used when region wants to change values that show in popup - e.g. set 16 beat when set to Akesson by popup

        List<int> mNotesOffsetFromRoot;
        List<int> mBassNotesOffsetFromRoot;

        List<BeatIntersectionNoteList> mMainPatternNotes; //note(s) to be played at each beat intersection as they are changed
        List<BeatIntersectionNoteList> mBassPatternNotes; //note(s) to be played at each beat intersection as they are changed

        public class AIRegionSaveData
        {
            public List<AIData> mDataList = new List<AIData>();
            public float BeatPos { get; set; }
            public float BeatLength { get; set; }
            public float BeatEndPos { get; set; }
            public float ExtendedEndPos { get; set; }
            public int mRootNoteNum;

            public bool PointInRegion(float touchPoint)
            {
                if( touchPoint>=BeatPos &&  touchPoint<BeatEndPos)
                {
                    return true;      
                }
                return false;
            }
            public List<int> mBeatVelocity = new List<int>(); //probably just 4 or 2 for each beat area - it may or may not coincide with current beat rate and note num
        };

        public class AIData
        {
            public int mValue = 0;
            public void Copy(AIData other)
            {
                mValue = other.mValue;
            }
        };

        Note mRootNote;

        AIData mScaleType = new AIData();
        AIData mChordType = new AIData();
        AIData mPlayPatternType = new AIData();
        AIData mBassBeatSubDivisionType = new AIData();
        AIData mBeatSubDivisionType = new AIData();
        AIData mBassPattern = new AIData();

        AIData mMainAccentBeat = new AIData();
        AIData mBassAccentBeat = new AIData();
        AIData mFeelGoodBools = new AIData();

        List<AIData> mDataList = new List<AIData>();
        List<Note> mNotes; //updated at every edit of popup to match latest requirements - TODO ghost note to be shown on the NoteLine - or reference notes on the line - you can access and edit velocity too

        List<Note> mTurnOffNotes = new List<Note>();

        float mBeatInterval = 1f;
        float mBassBeatInterval = 1f;
        float mBeatWidth = 1f;
        float mBassBeatWidth = 1f;
        int mNumNotes = 1;
        int mNumBassNotes = 1;

        public AIChordRegion(TopLineChordAI parent)
        {
            mNotes = new List<Note>();
            mScaleType.mValue = 0; //Major I think will always start
            mChordType.mValue = 0; //root
            mPlayPatternType.mValue = 0; //
            mBeatSubDivisionType.mValue = 0; //
            mBassPattern.mValue = 0; //
            mBassBeatSubDivisionType.mValue = 0; //

            mBassAccentBeat.mValue = 0;
            mMainAccentBeat.mValue = 0;
            mFeelGoodBools.mValue = 2 + 16; //2 = bit for root chord selected and 4 beat

            mDataList.Add(mScaleType);
            mDataList.Add(mChordType);
            mDataList.Add(mPlayPatternType);
            mDataList.Add(mBeatSubDivisionType);
            mDataList.Add(mBassPattern);
            mDataList.Add(mBassBeatSubDivisionType);

            mDataList.Add(mMainAccentBeat);
            mDataList.Add(mBassAccentBeat);
            mDataList.Add(mFeelGoodBools);

            mParent = parent; //Before INIT!
        }


        public List<Note> GetFirstIntersectionNotes()
        {
            int count = 0;
            List<Note> ln = new List<Note>();

            if (mMainPatternNotes!=null)
            {
                foreach (BeatIntersectionNoteList binl in mMainPatternNotes)
                {
                    if (binl.Count == 0)
                    {
                        if (count == 0)
                        {
                            //no notes on first beat so leave
                            return new List<Note>(); ;
                        }
                    }
                    else
                    {
                        if (count == 0)
                        {
                            ln = mNotes; //all these notes must be on the first beat alone
                        }
                        else
                        {
                            //found notes on later beat so can't stretch so leave
                            return new List<Note>(); ;
                        }

                    }
                }
            }

            return ln;
        }

        String GetFeelGoodBoolsText()
        {
            String FGChordType = "Maj";
            String FGText = "";

            if ((mFeelGoodBools.mValue & FG_2ND_NOTE) == FG_2ND_NOTE)
            {
                FGText = "+ 2nd, ";
            }

            if ((mFeelGoodBools.mValue & FG_1ST_INV_CHORD) == FG_1ST_INV_CHORD)
            {
                FGChordType = "1st Inv";
            }
            else if ((mFeelGoodBools.mValue & FG_2ND_INV_CHORD) == FG_2ND_INV_CHORD)
            {
                FGChordType = "2nd Inv";
            }

 
            return FGText + FGChordType;
        }

        public bool ShiftCurrentRegionLeft()
        {
            if ((mFeelGoodBools.mValue & FOUR_BEAT_REGION) == FOUR_BEAT_REGION && BeatPos > 0f)
            {
                float fMoveLeftAdjustAmount = 4f;

                foreach(Note n in mNotes)
                {
                    n.SetPosLen(n.BeatPos- fMoveLeftAdjustAmount, n.BeatLength);
                }

                if(mRootNote!=null)
                {
                    mRootNote.SetPosLen(mRootNote.BeatPos - fMoveLeftAdjustAmount, mRootNote.BeatLength);
                }

                SetPosLen(BeatPos - fMoveLeftAdjustAmount, BeatLength);

                return true;
            }

            return false;
        }

        public void SetNoteVelocityFromAccentBeat()
        {
            SetNoteVelocityFromAccent(mMainAccentBeat.mValue, false);
            SetNoteVelocityFromAccent(mBassAccentBeat.mValue, true);
        }

        void SetNoteVelocityFromAccent(int beatValues, bool bBass)
        {
            const int NUM_ACCENT_SLOTS = 8; //to be spread over 4 beat bar
            //mMainAccentBeat
            //Currently only 8 expected
            //looks through notes and if any at the slot sets them to accent velocity if set
            //clears them to mid velocity of not in case been changed from accented
            int value = 0;
            float accentBeatStart = BeatPos;
            float accentBeatWidth = 4f / NUM_ACCENT_SLOTS;
            Note lastNoteAdjusted = null; //if consecutive accent beats falls over the same note then only adjust with the first one - this keeps track of the last note set

            for (int i = 0; i < NUM_ACCENT_SLOTS; i++)
            {
                value = (1 << i);
                accentBeatStart = BeatPos + i * accentBeatWidth;
                float accentBeatEnd = accentBeatStart + accentBeatWidth;

                Note noteFound = null;

                if (bBass)
                {
                    noteFound = GetLowestNoteInRange(accentBeatStart, accentBeatEnd);
                }
                else
                {
                    noteFound = GetHighestNoteInRange(accentBeatStart, accentBeatEnd);
                }

                if (lastNoteAdjusted == noteFound)
                {
                    continue; //has been adjusted already so continue
                }

                lastNoteAdjusted = noteFound;

                if (noteFound != null)
                {
                    if ((beatValues & value) == value)
                    {
                        noteFound.Velocity = MidiBase.HIGH_VELOCITY;
                    }
                    else
                    {
                        noteFound.Velocity = MidiBase.MID_VELOCITY;
                    }
                    noteFound.RefreshRectToData();
                }
            }
        }

        Note GetHighestNoteInRange(float start, float end)
        {
            Note noteFound = null;

            int noteNumHighest = 0;

            foreach(Note n in mNotes)
            {
                if(n.IsNoteInRangeInclusive(start,end))
                {
                    if(n.mNoteNum > noteNumHighest)
                    {
                        noteFound = n;
                        noteNumHighest = n.mNoteNum;
                    }
                }
            }

            return noteFound;
        }

        Note GetLowestNoteInRange(float start, float end)
        {
            Note noteFound = null;

            int noteNumLowest = 999;

            foreach (Note n in mNotes)
            {
                if (n.IsNoteInRangeInclusive(start, end))
                {
                    if (n.mNoteNum < noteNumLowest)
                    {
                        noteFound = n;
                        noteNumLowest = n.mNoteNum;
                    }
                }
            }

            return noteFound;
        }

        public String GetChordTypeName()
        {
            String noteName = "";
            String scaleTypeName = "";
            String chordTypeName = "";
            String chordName = "";

            if (mRootNote!=null)
            {
                noteName += mRootNote.GetName();

               switch(mScaleType.mValue)
                {
                    case (int)AIChordManager.eScaleType.MAJOR:
                        scaleTypeName = "Maj";
                        break;
                    case (int)AIChordManager.eScaleType.MINOR:
                        scaleTypeName = "Min";
                        break;
                    case (int)AIChordManager.eScaleType.LYDIAN:
                        scaleTypeName = "Lyd";
                        break;
                    case (int)AIChordManager.eScaleType.MIXOLYDIAN:
                        scaleTypeName = "Mix";
                        break;
                    case (int)AIChordManager.eScaleType.DORIAN:
                        scaleTypeName = "Dor";
                        break;
                    case (int)AIChordManager.eScaleType.PHRYGIAN:
                        scaleTypeName = "Phr";
                        break;
                    case (int)AIChordManager.eScaleType.LOCRIAN:
                        scaleTypeName = "Loc";
                        break;
                    case (int)AIChordManager.eScaleType.HARM_MINOR:
                        scaleTypeName = "HarmMin";
                        break;
                }

                switch(mChordType.mValue)
                {
                    case (int)AIChordManager.eChordType.ROOT_TRIAD:
                        chordName = "";
                        break;
                    case (int)AIChordManager.eChordType.SUS_2_TRIAD:
                        chordName = "Sus(2)";
                        break;
                    case (int)AIChordManager.eChordType.SUS_4_TRIAD:
                        chordName = "Sus(4)"; ;
                        break;
                    case (int)AIChordManager.eChordType.SUS_9_TRIAD:
                        chordName = "Sus(9)";
                        break;
                    case (int)AIChordManager.eChordType.INV_1ST_TRIAD:
                        chordName = "1st Inv";
                        break;
                    case (int)AIChordManager.eChordType.INV_2ND_TRIAD:
                        chordName = "2nd Inv";
                        break;
                    case (int)AIChordManager.eChordType.QUAD_6TH:
                        chordName = "6th";
                        break;
                    case (int)AIChordManager.eChordType.QUAD_7TH:
                        chordName = "7th";
                        break;
                    case (int)AIChordManager.eChordType.FIVE_9TH:
                        chordName = "9th";
                        break;
                    case (int)AIChordManager.eChordType.FG_5_1:
                        chordName = "FG 5 ovr 1 "+ GetFeelGoodBoolsText();
                        break;
                    case (int)AIChordManager.eChordType.FG_1_4:
                        chordName = "FG 1 ovr 4 "+ GetFeelGoodBoolsText();
                        break;
                }
            }

            return chordTypeName = noteName + " " + scaleTypeName +" " + chordName;
        }

        // // // // // // // // // // // // //
        //LOAD SAVE
        public void LoadSaveData(AIRegionSaveData loadData)
        {
            if (loadData.mDataList.Count > mDataList.Count)
            {
                System.Diagnostics.Debug.Assert(false, string.Format("Save data loadData.mDataList.Count  {0} Too big for expected mDataList.Count {1}", loadData.mDataList.Count, mDataList.Count));
                return;
            }

            BeatPos = loadData.BeatPos;
            BeatEndPos = loadData.BeatEndPos;
            BeatLength = loadData.BeatLength;
            ExtendedEndPos = loadData.ExtendedEndPos;

            int i = 0;
            foreach (AIData dat in loadData.mDataList)
            {
                mDataList[i].Copy(dat);
                i++;
            }

            //Do this last
            PlayArea pa = mParent.mParent;
            NoteLine nl = pa.GetNoteLineAtNoteNumber(loadData.mRootNoteNum);
            if (nl==null)
            {
                //Can be empty regions so just refresh rectangles and leave
                //System.Diagnostics.Debug.Assert(false, string.Format("loadData.mRootNoteNum  {0} invalid note num", loadData.mRootNoteNum ));
                RefreshRectToData();
                return;
            }

            //add the note to the track and refresh
            mRootNote = nl.AddNote(BeatPos, BeatLength);

            RefreshAfterChange(mRootNote, true);
            RefreshRectToData();

            DeleteNonRegionNotesInArea();

            if (ExtendedEndPos!=0f)
            {
                foreach (Note n in mNotes)
                {
                    NoteLine ainl = n.mParent;
                    ainl.RemoveNotesInRegion(mRootNote.BeatPos, mRootNote.BeatEndPos);
                }
            }
        }

        void DeleteNonRegionNotesInArea()
        {
            if(mNotes.Count==0)
            {
                return;
            }
            PlayArea pa = mNotes[0].mParent.mParent;
            pa.RemoveAllNotesInRegion(BeatPos, BeatEndPos);
        }

        public AIRegionSaveData GetSaveData()
        {
            AIRegionSaveData saveData = new AIRegionSaveData();
            saveData.BeatPos = BeatPos;
            saveData.BeatEndPos = BeatEndPos;
            saveData.BeatLength = BeatLength;
            saveData.ExtendedEndPos = ExtendedEndPos;
            saveData.mRootNoteNum = mRootNote != null ? mRootNote.mNoteNum : -1;

            foreach (AIData dat in mDataList)
            {
                AIData newAidat = new AIData();
                newAidat.Copy(dat);
                saveData.mDataList.Add(newAidat);
            }

            return saveData;
        }
        //LOAD SAVE
        // // // // // // // // // // // // //

        public TopLineChordAI GetParentLine()
        {
            return mParent;
        }

        public void CopyValuesFromRegion(AIChordRegion other)
        {
            int index = 0;
            foreach(AIData d in mDataList)
            {
                d.mValue = other.mDataList[index++].mValue;
            }
        }

        public bool IsNoteInRegion(float pos, float width)
        {
            if (pos >= BeatPos && (pos + width) <= BeatEndPos)
            {
                return true;
            }
            return false;
        }

        bool IsNoteInRegion(Note n)
        {
            if (n.BeatPos >= BeatPos && n.BeatEndPos <= BeatEndPos)
            {
                return true;
            }
            return false;
        }

        public bool IncrementDecrementRootNote(bool bUp)
        {
            if(mRootNote!=null)
            {
                int noteNum = mRootNote.mNoteNum;
                PlayArea pa = mParent.mParent;

                if(bUp)
                {
                    noteNum++;
                }
                else
                {
                    noteNum--;
                }
                NoteLine nl = pa.GetNoteLineAtNoteNumber(noteNum);

                if(nl!=null)
                {
                    Note newNote = nl.AddNoteIfSpace(BeatPos, BeatLength);
                    RefreshAfterChange(newNote);
                    RefreshRectToData();
                    return true;
                }
            }

            return false;
        }

        public bool SetToNote(int noteNum)
        {
            PlayArea pa = mParent.mParent;

            NoteLine nl = pa.GetNoteLineAtNoteNumber(noteNum);
            if (nl == null)
            {
                return false;
            }

            if (mRootNote != null)
            {
                if(mRootNote.mNoteNum==noteNum)
                {
                    return false;
                }
            }

            Note newNote = nl.AddNoteIfSpace(BeatPos, BeatLength);
            RefreshAfterChange(newNote);
            RefreshRectToData();
            return true;
        }

        bool IsJoinType()
        {
            bool bJoinType = (mPlayPatternType.mValue == (int)AIChordManager.ePattern.Akesson) |
                             (mBassPattern.mValue == (int)AIChordManager.eBassPattern.Akesson);
            return bJoinType;
        }

        //When a note added in region reprocess, return false if no music will be played - i.e no notes
        public bool RefreshAfterChange(Note addedNote = null, bool bForceRefresh = false, bool bForceClear = false)
        {
            ChangePopupAfterRefreshToData = false;
            bool bJoinType = IsJoinType();

            if (bJoinType)
            {
                if (mNewBeatOffsets)
                {
                    if (mBeatSubDivisionType.mValue != 4)
                    {
                        ChangePopupAfterRefreshToData = true;
                        mBeatSubDivisionType.mValue = 4;
                        mBassBeatSubDivisionType.mValue = 3;
                        mBassPattern.mValue = (int)AIChordManager.eBassPattern.Akesson;
                    }
                }
                else
                {
                    if (mBeatSubDivisionType.mValue != 3)
                    {
                        ChangePopupAfterRefreshToData = true;
                        mBeatSubDivisionType.mValue = 3;
                        mBassBeatSubDivisionType.mValue = 3;
                        mBassPattern.mValue = (int)AIChordManager.eBassPattern.Akesson;
                    }
                }
            }

            if ((mFeelGoodBools.mValue & FOUR_BEAT_REGION) == FOUR_BEAT_REGION)
            {
                if(BeatLength!= TopLineChordAI.FOUR_BEAT_REGION_RANGE)
                {
                    bForceRefresh = true;
                    SetPosLen(BeatPos, TopLineChordAI.FOUR_BEAT_REGION_RANGE);
               }
            }
            else
            {
                if (BeatLength != TopLineChordAI.TWO_BEAT_REGION_RANGE)
                {
                    bForceRefresh = true;
                    SetPosLen(BeatPos, TopLineChordAI.TWO_BEAT_REGION_RANGE);
                }
            }

            if (bForceClear)
            {
                bForceRefresh = true;

                if (mRootNote != null)
                {
                    mRootNote.RemoveFromParentLine();
                }

                mRootNote = null;
            }
            PlayArea pa = mParent.mParent;
            //Note rootNoteFound = pa.GetHighesteNoteInRegion(this);
            Note rootNoteFound = mRootNote;

 
            bool bNewRoot = false;
            if(!bForceRefresh && addedNote != null && IsNoteInRegion(addedNote))
            {
                rootNoteFound = addedNote;
                bNewRoot = true;
            }

            if(mRootNote!= rootNoteFound || bForceRefresh)
            {
                if(bNewRoot && mRootNote != null)
                {
                    mRootNote.RemoveFromParentLine();
                }
                mTurnOffNotes = new List<Note>();
                //REMOVE ALL NOTES FIRST
                foreach (Note n in mNotes)
                {
                    mTurnOffNotes.Add(n);
                    n.RemoveFromParentRegionList();
                }
                //NOW CLEAR
                mNotes = new List<Note>();

                mRootNote = rootNoteFound;
                if (mRootNote != null)
                {
                    mRootNote.mRegion = this;

                    mRootNote.SetPosLen(BeatPos, BeatLength); //make it have same dimension as region
                    //set up new notes
                    SetUpAINotes();
                }
            }

            return mRootNote!=null;
        }


        bool mNewBeatOffsets = false; //Gah! Fogot adding new 2f beat at front would break saves! - needed for Elizabeth and Essex
        void SetBeatFromData()
        {

            if (mNewBeatOffsets)
            {
                //Bit naff but after adding bass notes separately, just cut and pasted code the previous code
                switch (mBeatSubDivisionType.mValue)
                {
                    case 0:
                        mBeatInterval = 1f;
                        break;
                    case 1:
                        mBeatInterval = 0.5f;
                        break;
                    case 2:
                        mBeatInterval = 0.25f;
                        break;
                    case 3:
                        mBeatInterval = 0.125f;
                        break;
                    case 4:
                        mBeatInterval = 2f;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                //Bit naff but after adding bass notes separately, just cut and pasted code the previous code
                switch (mBeatSubDivisionType.mValue)
                {
                    case 0:
                        mBeatInterval = 2f;
                        break;
                    case 1:
                        mBeatInterval = 1f;
                        break;
                    case 2:
                        mBeatInterval = 0.5f;
                        break;
                    case 3:
                        mBeatInterval = 0.25f;
                        break;
                    case 4:
                        mBeatInterval = 0.125f;
                        break;
                    default:
                        break;
                }
            }

            switch (mBassBeatSubDivisionType.mValue)
            {
                case 0:
                    mBassBeatInterval = 2f;
                    break;
                case 1:
                    mBassBeatInterval = 1f;
                    break;
                case 2:
                    mBassBeatInterval = 0.5f;
                    break;
                case 3:
                    mBassBeatInterval = 0.25f;
                    break;
                case 4:
                    mBassBeatInterval = 0.125f;
                    break;
                default:
                    break;
            }

            mBeatWidth = mBeatInterval * 0.5f;
            mBassBeatWidth = mBassBeatInterval * 0.5f;

            mNumNotes = (int)(BeatLength / mBeatInterval);
            mNumBassNotes = (int)(BeatLength / mBassBeatInterval);

            mMainPatternNotes = new List<BeatIntersectionNoteList>();
            mBassPatternNotes = new List<BeatIntersectionNoteList>(); ; //note(s) to be played at each beat intersection as they are changed

            for (int i=0; i<mNumNotes; i++)
            {
                List<int> noteList = new List<int>();
                mMainPatternNotes.Add(noteList);
            }
            for (int i = 0; i < mNumBassNotes; i++)
            {
                List<int> noteList = new List<int>();
                mBassPatternNotes.Add(noteList);
            }
        }

        List<int[]> FixUpPatternToChordSize(List<int[]> pattern)
        {
            mNotesOffsetFromRoot = SetUpChordFromRoot(); //TODO doing this here *and* in the later function this is passed to - waste

            int sizeChord = mNotesOffsetFromRoot.Count;
            List<int[]> convertRoll = new List<int[]>();

            for(int i =0; i< sizeChord; i++)
            {
                convertRoll.Add(pattern[i]);
            }

            return convertRoll;
        }

        //----------------------------------------------------------------------------------------------------------------
        void SetMainFromPattern(List<int[]> pattern, bool bBass = false)
        {
            int rootNoteNum = mRootNote.mNoteNum;

            //Requires mNotesOffsetFromRoot to be set up first
            mNotesOffsetFromRoot = SetUpChordFromRoot();

            int sizeChord = mNotesOffsetFromRoot.Count;

            List<BeatIntersectionNoteList> setPattern = mMainPatternNotes;
            int numNotes = mNumNotes;

            if(bBass)
            {
                setPattern = mBassPatternNotes;
                numNotes = mNumBassNotes;
            }

            int lenPattern = pattern.Count;
            for (int i = 0; i < numNotes;)
            {
                int idx = i % lenPattern; //mod offset in index - need this since we take care below?
                int[] ia = pattern[idx];
               // foreach (int[] ia in pattern)
                {
                    foreach (int p in ia)
                    {
                        if (p >= sizeChord)
                        {
                            System.Diagnostics.Debug.Assert(false, string.Format("p  {0} Too big for sizeChord {1}", p, sizeChord));
                            continue;
                        }

                        if(p < 0)
                        {
                            int finalNoteNumOffset = GetNoteFromCode(p);

                            if(finalNoteNumOffset<0)
                            {
                                if(finalNoteNumOffset== AIChord.PATTERN_CODE_SKIP_REST)
                                {
                                    return;
                                }

                                if (finalNoteNumOffset == AIChord.PATTERN_CODE_QUICK_ROLL_DOWN||
                                    finalNoteNumOffset == AIChord.PATTERN_CODE_QUICK_ROLL_UP ||
                                    finalNoteNumOffset == AIChord.PATTERN_CODE_FULL_CHORD )
                                {
                                    //add all notes of the chord since we going to roll the chord on till it is playing complete
                                    foreach(int k in mNotesOffsetFromRoot)
                                    {
                                        setPattern[i].Add(k + rootNoteNum);
                                    }
                                }
                                //any other negative is just skipped
                            }
                            else
                            {
                                setPattern[i].Add(finalNoteNumOffset);
                            }
                        }
                        else
                        {
                            int finalNoteNumOffset = mNotesOffsetFromRoot[p];
                            setPattern[i].Add(finalNoteNumOffset + rootNoteNum);
                        }
                    }
                    i++;

                    if(i>= numNotes)
                    {
                        break; //more notes than intervals so break!
                    }
                }
            }
        }

        bool IsScaleNoteCode(int code)
        {
            switch (code)
            {

                case AIChord.PATTERN_CODE_FIRST_SCALE_NOTE:
                case AIChord.PATTERN_CODE_SECOND_SCALE_NOTE:
                case AIChord.PATTERN_CODE_THIRD_SCALE_NOTE:
                case AIChord.PATTERN_CODE_FOURTH_SCALE_NOTE:
                case AIChord.PATTERN_CODE_FIFTH_SCALE_NOTE:
                case AIChord.PATTERN_CODE_SIXTH_SCALE_NOTE:
                case AIChord.PATTERN_CODE_SEVENTH_SCALE_NOTE:

                case AIChord.PATTERN_CODE_FIRST_SCALE_NOTE_OCT_BELOW:
                case AIChord.PATTERN_CODE_SECOND_SCALE_NOTE_OCT_BELOW:
                case AIChord.PATTERN_CODE_THIRD_SCALE_NOTE_OCT_BELOW:
                case AIChord.PATTERN_CODE_FOURTH_SCALE_NOTE_OCT_BELOW:
                case AIChord.PATTERN_CODE_FIFTH_SCALE_NOTE_OCT_BELOW:
                case AIChord.PATTERN_CODE_SIXTH_SCALE_NOTE_OCT_BELOW:
                case AIChord.PATTERN_CODE_SEVENTH_SCALE_NOTE_OCT_BELOW:
                    return true ;
            }

            return false;
        }

        int GetNoteFromCode(int code)
        {
            int[] scaleArraySelected = GetScale();
            int iReturn = -1;
            int index = 0;

            switch (code)
            {
                case AIChord.PATTERN_CODE_SKIP_NOTE:
                case AIChord.PATTERN_CODE_SKIP_REST:
                case AIChord.PATTERN_CODE_QUICK_ROLL_UP:
                case AIChord.PATTERN_CODE_QUICK_ROLL_DOWN:
                case AIChord.PATTERN_CODE_FULL_CHORD:
                case AIChord.PATTERN_CODE_HOLD_ACROSS_RANGE:
                    iReturn = code;
                    break;

                case AIChord.PATTERN_CODE_FIRST_JOIN:
                case AIChord.PATTERN_CODE_SECOND_JOIN:
                case AIChord.PATTERN_CODE_THIRD_JOIN:
                case AIChord.PATTERN_CODE_FOURTH_JOIN:
                case AIChord.PATTERN_CODE_FIFTH_JOIN:
                case AIChord.PATTERN_CODE_SIXTH_JOIN:
                case AIChord.PATTERN_CODE_SEVENTH_JOIN:
                case AIChord.PATTERN_CODE_EIGHTH_JOIN:
                    iReturn = AIChord.PATTERN_CODE_JOIN_START - code + mRootNote.mNoteNum;
                    break;

                case AIChord.PATTERN_CODE_FIRST_SCALE_NOTE_OCT_BELOW:
                case AIChord.PATTERN_CODE_SECOND_SCALE_NOTE_OCT_BELOW:
                case AIChord.PATTERN_CODE_THIRD_SCALE_NOTE_OCT_BELOW:
                case AIChord.PATTERN_CODE_FOURTH_SCALE_NOTE_OCT_BELOW:
                case AIChord.PATTERN_CODE_FIFTH_SCALE_NOTE_OCT_BELOW:
                case AIChord.PATTERN_CODE_SIXTH_SCALE_NOTE_OCT_BELOW:
                case AIChord.PATTERN_CODE_SEVENTH_SCALE_NOTE_OCT_BELOW:
                //case AIChord.PATTERN_CODE_EIGHTH_SCALE_NOTE_OCT_BELOW:
                    index = AIChord.PATTERN_CODE_SCALE_NOTE_OCT_BELOW_START - code ;
                    iReturn = scaleArraySelected[index] + mRootNote.mNoteNum - AIChord.OCTAVE_SPAN_SEMITONES;
                    break;

                case AIChord.PATTERN_CODE_FIRST_SCALE_NOTE:
                case AIChord.PATTERN_CODE_SECOND_SCALE_NOTE:
                case AIChord.PATTERN_CODE_THIRD_SCALE_NOTE:
                case AIChord.PATTERN_CODE_FOURTH_SCALE_NOTE:
                case AIChord.PATTERN_CODE_FIFTH_SCALE_NOTE:
                case AIChord.PATTERN_CODE_SIXTH_SCALE_NOTE:
                case AIChord.PATTERN_CODE_SEVENTH_SCALE_NOTE:
                    //case AIChord.PATTERN_CODE_EIGHTH_SCALE_NOTE_OCT_BELOW:
                    index = AIChord.PATTERN_CODE_SCALE_NOTE_START - code;
                    iReturn = scaleArraySelected[index] + mRootNote.mNoteNum;
                    break;

                case AIChord.PATTERN_CODE_OCTAVE_DOWN:
                    iReturn = mRootNote.mNoteNum - AIChord.OCTAVE_SPAN_SEMITONES;
                    break;
                case AIChord.PATTERN_CODE_TWO_OCTAVE_DOWN:
                    iReturn = mRootNote.mNoteNum - 2*AIChord.OCTAVE_SPAN_SEMITONES;
                    break;
            }

            return iReturn;
        }

        int[] GetScale()
        {
            int scaleIndex = mScaleType.mValue;
            int sizeCurrentList = AIChord.SCALE_LIST.Count;

            if (scaleIndex >= sizeCurrentList)
            {
                System.Diagnostics.Debug.Assert(false, string.Format("Scaleindex  {0} Too big for list size {1}", scaleIndex, sizeCurrentList));
                return null;
            }

            int[] scaleArraySelected = AIChord.SCALE_LIST[scaleIndex];

            return scaleArraySelected;
        }

        // // // // // // // // // // // // // // // // // // // // // // // // //
        // // // // // // // // // // // // // // // // // // // // // // // // //
        /// Feel good chords
        /// Feel good chords
        /// Feel good chords
        /// Feel good chords
        /// 

        const int FG_2ND_NOTE         = 1 << 0;
        const int FG_ROOT_CHORD       = 1 << 1;
        const int FG_1ST_INV_CHORD    = 1 << 2;
        const int FG_2ND_INV_CHORD  = 1 << 3;
        const int FOUR_BEAT_REGION   = 1 << 4; //yes I know not a feel good chord control ! This will switch to full f beat coverage of the region

        List<int> GetFeelGoodChordNotesOffsetFromRoot()
        {
            //Put em in here -> mNotesOffsetFromRoot
            if (mChordType.mValue == (int)AIChordManager.eChordType.FG_1_4)
            {

            }
            else if (mChordType.mValue == (int)AIChordManager.eChordType.FG_5_1)
            {

            }
            else
            {
                System.Diagnostics.Debug.Assert(false, string.Format("mChordType.mValue  {0} unexpected }", mChordType.mValue ));
                return null;
            }

            int rootNoteNum = mRootNote.mNoteNum;

            int[] scale = GetScale();

            //int fiveOverRootNoteNum = rootNoteNum + scale[4];
            //int fourUnderRootNoteNum = rootNoteNum + scale[3] - 12; // get the 4 above then subtract the octave

            int fiveOverRootNoteOffset = scale[4];
            int fourUnderRootNoteOffset = scale[3] - 12; // get the 4 above then subtract the octave

            //List<int> newChordList = new List<int>();

            int[] chordTypeForBoth = AIChord.CHORD_TYPE_LIST[(int)AIChord.ChordType.ROOT_TRIAD];

            if((mFeelGoodBools.mValue & FG_1ST_INV_CHORD) == FG_1ST_INV_CHORD)
            {
                chordTypeForBoth = AIChord.CHORD_TYPE_LIST[(int)AIChord.ChordType.INV_1ST_TRIAD];
            }
            else if ((mFeelGoodBools.mValue & FG_2ND_INV_CHORD) == FG_2ND_INV_CHORD)
            {
                chordTypeForBoth = AIChord.CHORD_TYPE_LIST[(int)AIChord.ChordType.INV_2ND_TRIAD];
            }

            List<int> relativeNotes = SetUpChordFromRoot(chordTypeForBoth); //TODO Always same for both?
            if ((mFeelGoodBools.mValue & FG_2ND_NOTE) == FG_2ND_NOTE)
            {
                int[] scaleArraySelected = GetScale();
                relativeNotes.Add(scaleArraySelected[1]); //add in the second note of the scale - should never overlap what's there?
            }

            List<int> secondRelativeNotes = new List<int>();

            int secOffsetToUse = 0;
            if (mChordType.mValue == (int)AIChordManager.eChordType.FG_1_4)
            {
                secOffsetToUse = fiveOverRootNoteOffset;
            }
            else if (mChordType.mValue == (int)AIChordManager.eChordType.FG_5_1)
            {
                secOffsetToUse = fourUnderRootNoteOffset;
            }

            foreach (int noteOffset in relativeNotes)
            {
                int secOffset = noteOffset + secOffsetToUse;
                secondRelativeNotes.Add(secOffset);
            }

            foreach (int noteOffset in secondRelativeNotes)
            {
                if(!relativeNotes.Contains(noteOffset))
                {
                    relativeNotes.Add(noteOffset);
                }
            }

            relativeNotes.Sort();

            return relativeNotes;
        }

        // // // // // // // // // // // // // // // // // // // // // // // // //
        // // // // // // // // // // // // // // // // // // // // // // // // //

        int[] GetChordType()
        {
            int chordIndex = mChordType.mValue;
            int sizeCurrentList = AIChord.CHORD_TYPE_LIST.Count;

            //if (chordIndex > AIChordManager.INDEX_LAST_CHORD_ARRAY)
            //{
            //    return GetFeelGoodChord();
            //}

            if (chordIndex >= sizeCurrentList)
            {
                System.Diagnostics.Debug.Assert(false, string.Format("chordIndex  {0} Too big for list size {1}", chordIndex, sizeCurrentList));
                return null;
            }

 
            int[] chordArraySelected = AIChord.CHORD_TYPE_LIST[chordIndex];

            return chordArraySelected;
        }

        List<int> SetUpChordFromRoot() //TODO getting called too many times - should be just once at AI start
        {
            //Put em in here -> mNotesOffsetFromRoot
            if (mChordType.mValue == (int)AIChordManager.eChordType.FG_1_4 || mChordType.mValue == (int)AIChordManager.eChordType.FG_5_1)
            {         
                return GetFeelGoodChordNotesOffsetFromRoot(); 
            }

            int[] chordRelativeIndexes = GetChordType();

            return SetUpChordFromRoot(chordRelativeIndexes);
        }

        List<int> SetUpChordFromRoot(int[] chordRelativeIndexes) 
        {
            int[] scaleArraySelected = GetScale();

            List<int> notesOffsetFromRoot = new List<int>();

            //NOTE: THe scale arrays have 
            foreach (int i in chordRelativeIndexes)
            {
                if (i >= 0)
                {
                    if (i >= scaleArraySelected.Length)
                    {
                        if (i == 8) //allow 8 as the root start of next octave
                        {
                            notesOffsetFromRoot.Add(scaleArraySelected[0] + AIChord.OCTAVE_SPAN_SEMITONES);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false, string.Format("chordTriad index {0} Too big for scaleArraySelected {1}", i, scaleArraySelected.Length));
                            continue;
                        }
                    }
                    else
                    {
                        notesOffsetFromRoot.Add(scaleArraySelected[i]);
                    }
                }
                else
                {
                    //Negative get offset backwards from scale
                    if (Math.Abs(i) >= (scaleArraySelected.Length))
                    {
                        System.Diagnostics.Debug.Assert(false, string.Format("chordTriad index {0} Too small for scaleArraySelected {1} - indexing backwards", i, scaleArraySelected.Length));
                        continue;
                    }

                    int offsetMinus = scaleArraySelected.Length + i; //i is minus!
                    int scaleIndex = scaleArraySelected[offsetMinus] - 12;

                    notesOffsetFromRoot.Add(scaleIndex);
                }
            }

            return notesOffsetFromRoot;
        }

        void SetUpBassNotesNew()
        {
            switch(mBassPattern.mValue)
            {
                case (int)(int)AIChordManager.eBassPattern.None:
                    return;
                case (int)AIChordManager.eBassPattern.Single:
                    SetMainFromPattern(AIChord.BASS_SINGLE_PATTERN, true);
                    ConvertPatternToNotes(true);
                    return;
                case (int)AIChordManager.eBassPattern.Octave:
                    SetMainFromPattern(AIChord.BASS_OCTAVE_PATTERN, true);
                    ConvertPatternToNotes(true);
                    return;
                case (int)AIChordManager.eBassPattern.Akesson:
                    SetMainFromPattern(AIChord.AKESSON_BASS_PATTERN, true);
                    ConvertPatternToNotes(true);
                    return;
                case (int)AIChordManager.eBassPattern.WalkUp:
                    SetMainFromPattern(AIChord.WALKUP_BASS_PATTERN, true);
                    ConvertPatternToNotes(true);
                    return;
                case (int)AIChordManager.eBassPattern.WalkDown:
                    SetMainFromPattern(AIChord.WALKDOWN_BASS_PATTERN, true);
                    ConvertPatternToNotes(true);
                    return;
            }
        }

        void SetUpBassNotes()
        {
            //you should have the chord set up from root by here
            int noteNum = mRootNote.mNoteNum;
            PlayArea pa = mParent.mParent;

            switch (mBassPattern.mValue)
            {
                case 0:
                    return;
                case 1:
                    mBassNotesOffsetFromRoot.Add( - AIChord.OCTAVE_SPAN_SEMITONES); //todo
                    break;
                case 2:
                    mBassNotesOffsetFromRoot.Add( - AIChord.OCTAVE_SPAN_SEMITONES); //todo
                    mBassNotesOffsetFromRoot.Add( - 2*AIChord.OCTAVE_SPAN_SEMITONES); //todo
                    break;
            }

            int numNotesInChord = mBassNotesOffsetFromRoot.Count;

            if(numNotesInChord==0)
            {
                return;
            }

            float currentBeatPos = BeatPos;
            float baseBeatWidth = 1f;
            int baseNumNotes= 4;

            for (int index = 0; index < mNumNotes; index++)
            {
                int modIndex = index % numNotesInChord;
                int offSet = mBassNotesOffsetFromRoot[modIndex];
                int currentNoteNum = noteNum + offSet;

                NoteLine nl = pa.GetNoteLineAtNoteNumber(currentNoteNum);
                if (nl != null)
                {
                    Note newNote = new Note(currentNoteNum, nl);
                    newNote.SetPosLen(currentBeatPos, mBeatWidth);
                    mNotes.Add(newNote); //Sort?
                    nl.AddAIRegionNote(newNote);
                }
                mNotes.Sort(NoteLine.SortByNoteStart);

                currentBeatPos += mBeatInterval;
            }
        }

        // NEW NEW NEW NEW NEW NEW
        // NEW NEW NEW NEW NEW NEW
        // NEW NEW NEW NEW NEW NEW
        void ConvertPatternToNotes(bool bBass = false)
        {
            int noteNum = mRootNote.mNoteNum;
            float currentBeatPos = BeatPos;
            float joinCurrentBeatPos = BeatPos;
            PlayArea pa = mParent.mParent;

            mNotesOffsetFromRoot = SetUpChordFromRoot();
            //SetUpBassNotes();
            List<BeatIntersectionNoteList> setPattern = mMainPatternNotes;
            float beatWidth     = mBeatWidth;
            float beatInterval  = mBeatInterval;

            if (bBass)
            {
                setPattern      = mBassPatternNotes;
                beatWidth       = mBassBeatWidth;
                beatInterval    = mBassBeatInterval;
            }

            int numNotesInChord = mNotesOffsetFromRoot.Count;

            if (numNotesInChord == 0)
            {
                return;
            }

            bool bJoinType = IsJoinType();


            if (bJoinType)
            {
                beatWidth = 0f;
            }

            BeatIntersectionNoteList lastLi = null;
            const int MAX_JOINED = 2;
            int currentBeatWidthCount = 0;

            for (int i= 0; i < setPattern.Count; i++)
            {
                BeatIntersectionNoteList li = setPattern[i];

                if (mPlayPatternType.mValue == (int)AIChordManager.ePattern.QRollDown ||
                    mPlayPatternType.mValue == (int)AIChordManager.ePattern.QRollUp)
                {
                    SetUpQuickRoll(li, currentBeatPos);
                }
                else if (mPlayPatternType.mValue == (int)AIChordManager.ePattern.WholeRange)
                {
                    foreach (int finalNoteOffset in li)
                    {
                        int currentNoteNum = finalNoteOffset;

                        NoteLine nl = pa.GetNoteLineAtNoteNumber(currentNoteNum);
                        if (nl != null)
                        {
                            Note newNote = new Note(currentNoteNum, nl);
                            newNote.SetPosLen(BeatPos, BeatLength);
                            mNotes.Add(newNote); //Sort?
                            nl.AddAIRegionNote(newNote);
                        }
                    }
                    mNotes.Sort(NoteLine.SortByNoteStart);
                }
                else
                {
                    if(bJoinType) //JOIN TYPE
                    {
                        bool bLastBeat = i == (setPattern.Count - 1);
                        bool bDoubleBeatHack = false;
                        bool onBeatBreak = currentBeatWidthCount >0 && (currentBeatWidthCount % MAX_JOINED)==0;

                        if ((li.Count != 0 && !bLastBeat) && onBeatBreak)
                        {
                            //HACK!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                            bDoubleBeatHack = true;
                        }
                        if (li.Count == 0 || bLastBeat || bDoubleBeatHack)
                        {
                            if(bLastBeat && li.Count>0)
                            {
                                beatWidth += mBeatInterval;
                                lastLi = li;
                            }

                            //if(bDoubleBeatHack && li.Count > 0)
                            //{
                            //    beatWidth += mBeatInterval;
                            //}

                            if (beatWidth>0f && lastLi!=null) //TODO assert on lastLi
                            {
                                currentBeatWidthCount = 0;

                                foreach (int finalNoteOffset in lastLi)
                                {
                                    int currentNoteNum = finalNoteOffset;

                                    NoteLine nl = pa.GetNoteLineAtNoteNumber(currentNoteNum);
                                    if (nl != null)
                                    {
                                        Note newNote = new Note(currentNoteNum, nl);
                                        newNote.SetPosLen(joinCurrentBeatPos, beatWidth );
                                        joinCurrentBeatPos += beatWidth;
                                        mNotes.Add(newNote); //Sort?
                                        nl.AddAIRegionNote(newNote);
                                    }
                                }
                                mNotes.Sort(NoteLine.SortByNoteStart);
                                beatWidth = 0f; //for next join
                            }
                            if(!bDoubleBeatHack)
                            {
                                joinCurrentBeatPos += mBeatInterval;
                            }
                            else
                            {
                                beatWidth += mBeatInterval;
                            }
                        }
                        else
                        {
                            currentBeatWidthCount++;
                            beatWidth += mBeatInterval;
                            lastLi = li;
                        }
                    }
                    else
                    {
                        foreach (int finalNoteOffset in li)
                        {
                            int currentNoteNum = finalNoteOffset;

                            NoteLine nl = pa.GetNoteLineAtNoteNumber(currentNoteNum);
                            if (nl != null)
                            {
                                Note newNote = new Note(currentNoteNum, nl);
                                newNote.SetPosLen(currentBeatPos, beatWidth + ExtendedEndPos);
                                mNotes.Add(newNote); //Sort?
                                nl.AddAIRegionNote(newNote);
                            }
                        }
                        mNotes.Sort(NoteLine.SortByNoteStart);
                    }
                }

                currentBeatPos += beatInterval;
            }

            //Now that notes are set we can set up accents 
            SetNoteVelocityFromAccentBeat();
        }

        void SetUpQuickRoll(BeatIntersectionNoteList li, float currentBeatPos)
        {
            PlayArea pa = mParent.mParent;

            float beatWidth = mBeatWidth;
            float rollInterval = beatWidth;

            rollInterval *= 0.5f;
            float quickInterval = rollInterval / (float)li.Count;

            float quickRollBeatPos = currentBeatPos;
            float beatWidthAdjusted = beatWidth;

            if (mPlayPatternType.mValue == (int)AIChordManager.ePattern.QRollDown)
            {
                quickRollBeatPos = currentBeatPos + rollInterval- quickInterval; //last one first then go down
                beatWidthAdjusted = beatWidth - rollInterval;
                quickInterval = -quickInterval;
            }

            foreach (int finalNoteOffset in li)
            {
                int currentNoteNum = finalNoteOffset;
                NoteLine nl = pa.GetNoteLineAtNoteNumber(currentNoteNum);
                if (nl != null)
                {
                    Note newNote = new Note(currentNoteNum, nl);
                    newNote.SetPosLen(quickRollBeatPos, beatWidthAdjusted + ExtendedEndPos);
                    mNotes.Add(newNote); //Sort?
                    nl.AddAIRegionNote(newNote);
                    quickRollBeatPos += quickInterval;
                    beatWidthAdjusted -= quickInterval;
                }
            }

            mNotes.Sort(NoteLine.SortByNoteStart);
        }

        public void UpdateExtendEndPos(float stretchEndPos)
        {
            ExtendedEndPos = stretchEndPos - mBeatWidth;
        }
        // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // //
        // MAIN UPDATE 
        // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // // //

        void SetUpAINotes()
        {

            if (mPlayPatternType.mValue == 0 && mBassPattern.mValue==0) //No notes are going to be set so don't do anything else
            {
                return;
            }

            mBassNotesOffsetFromRoot = new List<int>(); //reset bass

            SetBeatFromData();
            SetUpBassNotesNew(); //works here?


            //TODO refactor to switch
            if (mPlayPatternType.mValue == (int)AIChordManager.ePattern.Ballad)
            {
                SetMainFromPattern(AIChord.BALLAD_PATTERN);
                ConvertPatternToNotes();
                //SetUpBallad();
                return;
            }
            else if (mPlayPatternType.mValue == (int)AIChordManager.ePattern.BalladUpDown)
            {
                //Roll chord should only be as big as the chord is so adjust here
                //this fix up will be unique to roll I think 
                SetMainFromPattern(AIChord.BALLAD_PATTERN_UP_DOWN);
                ConvertPatternToNotes();
                //SetUpAdele();
                return;
            }
            else if (mPlayPatternType.mValue == (int)AIChordManager.ePattern.Adele)
            {            
                //Roll chord should only be as big as the chord is so adjust here
                //this fix up will be unique to roll I think 
                SetMainFromPattern(AIChord.ADELE_PATTERN);
                ConvertPatternToNotes();
                //SetUpAdele();
                return;
            }
            else if (mPlayPatternType.mValue == (int)AIChordManager.ePattern.SlowRoll)
            {
                SetMainFromPattern(FixUpPatternToChordSize(AIChord.ROLL_PATTERN));
                ConvertPatternToNotes();
                //SetUpChordRoll();
                return;
            }
            else if (mPlayPatternType.mValue == (int)AIChordManager.ePattern.QRollDown)
            {
                SetMainFromPattern(AIChord.QUICK_ROLL_DOWN_PATTERN);
                ConvertPatternToNotes();
                //SetUpChordRoll();
                return;
            }
            else if (mPlayPatternType.mValue == (int)AIChordManager.ePattern.QRollUp)
            {
                SetMainFromPattern(AIChord.QUICK_ROLL_UP_PATTERN);
                ConvertPatternToNotes();
                //SetUpChordRoll();
                return;
            }
            else if (mPlayPatternType.mValue == (int)AIChordManager.ePattern.FullChordPair)
            {
                SetMainFromPattern(AIChord.FULL_CHORD_PAIR_PATTERN);
                ConvertPatternToNotes();
                //SetUpChordRoll();
                return;
            }
            else if (mPlayPatternType.mValue == (int)AIChordManager.ePattern.RhythmChord)
            {
                SetMainFromPattern(AIChord.RHYTHM_CHORD);
                ConvertPatternToNotes();
                return;
            }
            else if (mPlayPatternType.mValue == (int)AIChordManager.ePattern.WholeRange)
            {
                SetMainFromPattern(AIChord.CHORD_ACROSS_WHOLE_RANGE_PATTERN);
                ConvertPatternToNotes();
                return;
            }
            else if (mPlayPatternType.mValue == (int)AIChordManager.ePattern.Akesson)
            {
                SetMainFromPattern(AIChord.AKESSON_PATTERN);
                ConvertPatternToNotes();
                return;
            }

            int noteNum = mRootNote.mNoteNum;
            PlayArea pa = mParent.mParent;
            float currentBeatPos = BeatPos;
            int scaleIndex = mScaleType.mValue;
            int sizeCurrentList = AIChord.SCALE_LIST.Count;

            if(scaleIndex>=sizeCurrentList)
            {
                System.Diagnostics.Debug.Assert(false, string.Format("{0} Too big for list size {1}", scaleIndex, sizeCurrentList));
                return;
            }

            int[] scaleArraySelected = AIChord.SCALE_LIST[scaleIndex];

            int sizeArraySelected = scaleArraySelected.Length;
            if (mPlayPatternType.mValue == 2)
            {
                for (int index = mNumNotes-1; index >= 0; index--)
                {
                    int useIndex = index % sizeArraySelected;
                    int offSet = scaleArraySelected[useIndex];

                    int currentNoteNum = noteNum + offSet;

                    NoteLine nl = pa.GetNoteLineAtNoteNumber(currentNoteNum);

                    if (nl != null)
                    {
                        Note newNote = new Note(currentNoteNum, nl);
                        newNote.SetPosLen(currentBeatPos, mBeatWidth);
                        mNotes.Add(newNote); //Sort?
                        nl.AddAIRegionNote(newNote);
                    }
                    currentBeatPos += mBeatInterval;
                }
                mNotes.Sort(NoteLine.SortByNoteStart);
            }
            else if (mPlayPatternType.mValue == 1)
            {
                for (int index = 0; index < mNumNotes; index++)
                {
                    int useIndex = index % sizeArraySelected;
                    int offSet = scaleArraySelected[useIndex];

                    int currentNoteNum = noteNum + offSet;

                    NoteLine nl = pa.GetNoteLineAtNoteNumber(currentNoteNum);

                    if (nl != null)
                    {
                        Note newNote = new Note(currentNoteNum, nl);
                        newNote.SetPosLen(currentBeatPos, mBeatWidth);
                        mNotes.Add(newNote); //Sort?
                        nl.AddAIRegionNote(newNote);
                    }
                    currentBeatPos += mBeatInterval;
                }

            }

            SetNoteVelocityFromAccentBeat();
        }

        public void UpdatePlaying(MelodyEditorInterface.MEIState state)
        {
            bool bNoteSetOnThisUpdate = false;
            foreach (Note n in mNotes)
            {
                bNoteSetOnThisUpdate |= n.UpdatePlaying(state, !bNoteSetOnThisUpdate);
            }
        }

        public int GetDataValue(int index)
        {
            if(index< mDataList.Count)
            {
                return mDataList[index].mValue;
            }

            return -1;
        }
        public bool SetDataValue(int index, int value)
        {
            if (index < mDataList.Count)
            {
                if (mDataList[index].mValue != value)
                {
                    mDataList[index].mValue = value;
                    return true;
                }
            }
            return false;
        }

        public void Update(MelodyEditorInterface.MEIState state)
        {
            if (mTurnOffNotes.Count > 0)
            {
                foreach (Note n in mTurnOffNotes)
                {
                    n.SetNoteOff(state);
                }
                mTurnOffNotes = new List<Note>();
            }

            if (state.fMainCurrentBeat >= BeatPos && state.fMainCurrentBeat < BeatPos + BeatLength)
            {
                if (!Active)
                {
                    //Set active
                    Active = true;
                    //state.GetMidiBase().NoteOn(Chan, mNoteNum, 70);
                }
             }
            else
            {
                if (Active)
                {
                    //Set inactive
                    Active = false;
                    //state.GetMidiBase().NoteOff(Chan, mNoteNum);
                }
            }

            //UpdatePlaying(state); //TODO watch out for notes being left on when out of region! if only update while 'Active' is true!

            Input(state);
        }

        bool Input(MelodyEditorInterface.MEIState state)
        {
            if (state.mbAllowInputPlayArea && state.input.LeftUp)
            {
                if (state.input.mRectangle.Intersects(mRect) && state.mbPlayAreaUpdate)
                {
                    state.mAIRegionPopup.Show(this, state);
                    return true;
                }
            }

            return false;
        }

        public void SetPosLen(float pos, float len)
        {
            BeatPos = pos;
            BeatLength = len;
            BeatEndPos = pos + len;
            RefreshRectToData();
        }
        public void SetPlayAreaLoopRange()
        {
            mParent.mParent.SetBarLoopRangeToCoverBeatRange(BeatPos, BeatEndPos);
        }

        public bool Overlaps(Rectangle testRect)
        {
            if (testRect.Intersects(mRect) || testRect.Contains(mRect))
            {
                return true;
            }

            return false;
        }

        public void RefreshRectToData()
        {
            float fStartPos = mParent.mParent.StartBeat;

            int startY = mParent.mStartY;
            int startX = mParent.mStartX + (int)((BeatPos - fStartPos) * MelodyEditorInterface.BEAT_PIXEL_WIDTH);
            int noteWidth = (int)(BeatLength * MelodyEditorInterface.BEAT_PIXEL_WIDTH);

            mRect = new Rectangle(startX, startY, noteWidth, NoteLine.TRACK_HEIGHT);  //NoteLine.TRACK_HEIGHT);

            mInDrawRange = mRect.Intersects(mParent.mRect);
        }

        public void Draw(SpriteBatch sb)
        {
            if (mInDrawRange)
            {
                sb.FillRectangle(mRect, Col);
                sb.DrawRectangle(mRect, Color.Black);
            }
        }
    }
}
