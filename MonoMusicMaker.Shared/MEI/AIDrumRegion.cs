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
using Newtonsoft.Json;

namespace MonoMusicMaker
{
    public class AIDrumRegion
    {
        static AIDrumBeat msLastEditedBeat = null;

//35 Acoustic Bass Drum
//36 Bass Drum 1
//37 Side Stick/Rimshot
//38 Acoustic Snare
//39 Hand Clap
//40 Electric Snare
//41 Low Floor Tom
//42 Closed Hi-hat
//43 High Floor Tom
//44 Pedal Hi-hat
//45 Low Tom
//46 Open Hi-hat
//47 Low-Mid Tom
//48 Hi-Mid Tom
//49 Crash Cymbal 1
//50 High Tom
//51 Ride Cymbal 1
//52 Chinese Cymbal
//53 Ride Bell
//54 Tambourine
//55 Splash Cymbal
//56 Cowbell
//57 Crash Cymbal 2
//58 Vibra Slap
//59 Ride Cymbal 2
//60 High Bongo
//61 Low Bongo
//62 Mute High Conga
//63 Open High Conga
//64 Low Conga
//65 High Timbale
//66 Low Timbale
//67 High Agogô
//68 Low Agogô
//69 Cabasa
//70 Maracas
//71 Short Whistle
//72 Long Whistle
//73 Short Güiro
//74 Long Güiro
//75 Claves
//76 High Wood Block
//77 Low Wood Block
//78 Mute Cuíca
//79 Open Cuíca
//80 Mute Triangle
//81 Open Triang

        public static readonly int[] ROCK_KIT = { 35, 36, 37, 38, 39, 42, 46, 49, 47, 48, 50, 41, 43 , 40, 36, 38, 42, 47, 57, 28, 29, 50, 51, 52, 54, 27, 26, 77, 45 };
        public static readonly int[] ELECTRONIC_KIT = { 40, 36, 38, 42, 47, 57, 28, 29, 50, 51, 52, 54 };
        public static readonly int[] BOSSONOVA_KIT = { 81, 36, 38, 42, 47, 57, 28, 29, 50};
        public static readonly int[] MIXED_KIT = { 80, 72, 38, 42, 47, 57, 28, 29, 50, 51, 52, 54, 55 };
        public static readonly int[] LATIN_KIT = { 79, 68, 38, 42, 47, 57, 28, 29, 50, 51, 52, 54, 55 };
        public static readonly int[] WEIRD_KIT = { 78, 67, 38, 42, 47, 57, 28, 29, 50, 51, 52, 54, 55 };
        public static readonly int[] ETC_KIT = { 77, 76, 38, 42, 47, 57, 28, 29, 50, 51, 52, 54, 55 };

        public static List<int[]> DRUM_KIT_LIST = new List<int[]>
        {
            ROCK_KIT, 
            ELECTRONIC_KIT, 
            BOSSONOVA_KIT,
            MIXED_KIT,
            LATIN_KIT,
            WEIRD_KIT,
            ETC_KIT,
        };


        public const float BeatLength = 4f; //always 4 beats - or 8?
        public const float BeatWidthQuarter = BeatLength / 16; //always 4 beats - or 8?
        public const float BeatWidthThird = BeatLength / 24; //always 4 beats - or 8?
        //public const float BeatWidthDefault = BeatLength / 16; //always 4 beats - or 8?
        public float mWidth = BeatWidthQuarter;

        public int KitIndex { get; set; } = 0;

        public bool mSelected = false;
        public bool mPasteCandidate = false;
        public bool mCopied = false;

        public void ClearCutPasteFlags()
        {
            mSelected = false;
            mPasteCandidate = false;
            mCopied = false;
        }

    public bool mbHasBeenEdited = false;
        float mBeatPos;
        public float BeatPos
        {
            get { return mBeatPos; }
            set
            {
                mBeatPos = value;
                mDrumData.BeatPos = value;
            }
        }
        public float BeatEndPos { get; set; }

        List<Note> mNotes; //updated at every edit of popup to match latest requirements - when create popup  the old notelist is cleared and then the note list is filled out with region info again - keeps it straight

        public Color Col { set; get; } = Color.DarkOliveGreen;

        bool mInDrawRange = false;
        public Rectangle mRect;
        TopLineDrumAI mParent;

        public AIDrumSaveData mDrumData;


        public AIDrumRegion(TopLineDrumAI parent, bool bThirdBeat)
        {
            mParent = parent;
            ResetToBeatArrangement(bThirdBeat);
        }

        public void ResetToBeatArrangement(bool bThirdBeat)
        {
            int numDrumBeats = DrumMachinePopUp.NUM_DRUM_BEATS_FOUR;
            mWidth = BeatWidthQuarter;
            if (bThirdBeat)
            {
                numDrumBeats = DrumMachinePopUp.NUM_DRUM_BEATS_THIRDS;
                mWidth = BeatWidthThird;
            }

            //TODO temp create one to init the pop up
            mDrumKit = ROCK_KIT;

            mDrumData = new AIDrumSaveData(mParent.mParent, bThirdBeat); //12==numbeats
            mDrumData.mWidth = mWidth;
            mDrumData.BeatPos = BeatPos;

            if (mDrumData.mBeatLineList.Count == 0)
            {
                mDrumData.CreateLinesIfNone(mDrumKit, numDrumBeats);
            }
        }

        //Dont see the point of "drum kits" and all the hassle of changing around notes erasing and putting back on new lines etc when selecting a different kit- better to have more banks and cycle round hopefully not too much pain cycling round
        //public void SetNewDrumIndex(int index)
        //{
        //    mDrumKit = DRUM_KIT_LIST[KitIndex];
        //    KitIndex = index;
        //    mDrumData.CreateNewEmptyLinesFromKit(mDrumKit, DrumMachinePopUp.NUM_DRUM_BEATS);
        //}

        public PlayArea GetPlayArea()
        {
            return mParent.mParent;
        }

        public NoteLine GetNoteLine(int linenum)
        {
            NoteLine nl = null;
            if(linenum < mDrumData.mBeatLineList.Count)
            {
                nl = mParent.mParent.GetNoteLineAtNoteNumber(mDrumData.mBeatLineList[linenum].mRootNoteNum);
            }
            return nl;
        }

        public int GetBeatVelocityLevelIndex(int linenum, int beatnum)
        {
            AIDrumBeat aidb = mDrumData.GetBeat(linenum, beatnum);
            return aidb.mVelocityLevel;
        }
        public bool SetBeat(int linenum, int beatnum)
        {
            bool bHasBeenEdited =  mDrumData.SetBeat(linenum, beatnum);
            mbHasBeenEdited  |= bHasBeenEdited;
            return bHasBeenEdited;
        }
        public bool ClearBeat(int linenum, int beatnum)
        {
            bool bHasBeenEdited = mDrumData.ClearBeat(linenum, beatnum); //set this when clear?
            mbHasBeenEdited |= bHasBeenEdited;
            return bHasBeenEdited;
        }
        public bool CycleVelocityLevel(int linenum, int beatnum)
        {
            bool bHasBeenEdited = mDrumData.CycleVelocityLevel(linenum, beatnum); //set this when clear?
            mbHasBeenEdited |= bHasBeenEdited;
            return bHasBeenEdited;
        }
        public bool SetOffsetLevel(int linenum, int beatnum, int newOffsetLevel)
        {
            bool bHasBeenEdited = mDrumData.SetOffsetLevel(linenum, beatnum, newOffsetLevel); //set this when clear?
            mbHasBeenEdited |= bHasBeenEdited;
            return bHasBeenEdited;
        }

        public void SetPlayAreaLoopRange()
        {
            mParent.mParent.SetBarLoopRangeToCoverBeatRange(BeatPos, BeatEndPos);
        }
        public void ClearAllNotes() //useful for when switching from kits - need to clear the notes for the last kit when the same button position used to set an alternate selection of NoteLines on
        {
            mDrumData.ClearAll();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        public class AIDrumSaveData
        {
            PlayArea mPa = null; //private so doesnt save

            public AIDrumSaveData(PlayArea pa = null, bool bThirdBeat=true)
            {
                mbThirdBeat = bThirdBeat;
                mPa = pa;

                if(bThirdBeat)
                {
                    mWidth = BeatWidthThird;
                }
            }

            public void SetPlayArea(PlayArea pa) //useful for setting play area after loading data in
            {
                mPa = pa;
            }

            // // // // // // // // // // //
            //PUBLIC SAVED DATA
            public List<AIBeatLine> mBeatLineList = new List<AIBeatLine>();
            public float BeatPos { get; set; }
            public bool mbThirdBeat;
            public float mWidth; //maximum and default is 4 beat/16 i.e. 1/4 beat

            public bool CycleVelocityLevel(int linenum, int beatnum)
            {
                AIDrumBeat beatToEdit = mBeatLineList[linenum].mBeatList[beatnum];

                if(beatToEdit.GetNote()==null && beatToEdit == AIDrumRegion.msLastEditedBeat)
                {
                    SetBeat( linenum,  beatnum); //the double click means it removes the note so we have to check the last note edited and if off we know we need to put it back
                }
                return mBeatLineList[linenum].mBeatList[beatnum].CycleVelocity();
            }

            public bool SetOffsetLevel(int linenum, int beatnum, int offsetLevel)
            {
                AIDrumBeat beatToEdit = mBeatLineList[linenum].mBeatList[beatnum];

                if (beatToEdit.GetNote() != null)
                {
                    float beatPos = BeatPos + beatnum * mWidth;

                    //if (mbThirdBeat)
                    //{
                    //    float fraction = (beatnum % 3) * mWidth;
                    //    beatPos = BeatPos + beatnum / 3 + fraction;
                    //}
                    Note note = beatToEdit.GetNote();

                    float widthNote = mWidth;
                    float offsetUnit = AIDrumBeat.OFFSET_BEAT_DIVISION * widthNote;
                    float noteOffset = offsetUnit * (float)beatToEdit.mOffsetLevel;

                    float newPos = beatPos + noteOffset;
                    note.SetPosLen(newPos, mWidth);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false, string.Format("SetOffsetLevel - linenum {0} beatnum{1} offsetLevel {2}!", linenum, beatnum, offsetLevel));
                }
                return mBeatLineList[linenum].mBeatList[beatnum].SetOffsetLevel(offsetLevel);
            }

            public bool SetBeat(int linenum, int beatnum)
            {
                bool bEdited = false;
                AIDrumBeat db = mBeatLineList[linenum].mBeatList[beatnum];
                if (mPa!=null && !db.mbBeatOn) //todo velocity
                {
                    if (linenum == 0 && beatnum == 0)
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("NOT ON SetBeat EXPECTED?  {0} {1}", linenum, beatnum));
                    }
                    bEdited = true;
                    float beatPos = BeatPos + beatnum * mWidth;

                    NoteLine nl = mPa.GetNoteLineAtNoteNumber(mBeatLineList[linenum].mRootNoteNum);

                    Note note = nl.AddNote(beatPos, mWidth);
                    mBeatLineList[linenum].mBeatList[beatnum].SetNote(note);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("ON ALREADY SetBeat EXPECTED?  {0} {1}", linenum, beatnum));
                }

                return bEdited;
            }
            public AIDrumBeat GetBeat(int linenum, int beatnum)
            {
                AIDrumBeat db = mBeatLineList[linenum].mBeatList[beatnum];
                return db;
            }
            public bool ClearBeat(int linenum, int beatnum)
            {
                AIDrumBeat db = mBeatLineList[linenum].mBeatList[beatnum];

                if (linenum==0 && beatnum==0 && db.mbBeatOn)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("ClearBeat EXPECTED? {0} {1}", linenum, beatnum));
                }
                return db.ClearNote();
            }
            public void ClearAll() //Useful for when switching kits and knowing the buttons are there to rebuild the notelines and beats from the buttons that are on immediately after switching 
            {
                foreach(AIBeatLine bl in mBeatLineList )
                {
                    bl.ClearLine();
                }
            }

            public void CreateNewEmptyLinesFromKit(int[] drumKit, int numBeats)
            {
                mBeatLineList = new List<AIBeatLine>();
                CreateLinesIfNone(drumKit,  numBeats);
            }

            public void CreateLinesIfNone(int[] drumKit, int numBeats)
            {
                if(mBeatLineList.Count==0)
                {
                    for (int i = 0; i < drumKit.Length; i++)
                    {
                        AIBeatLine bl = new AIBeatLine();
                        bl.mRootNoteNum = drumKit[i];
                        bl.CreateBeatsIfNone(numBeats);
                        mBeatLineList.Add(bl);
                    }
                }
                System.Diagnostics.Debug.Assert(mBeatLineList.Count == drumKit.Length, string.Format("mBeatLineList.Count {0} not equal numline {1}!", mBeatLineList.Count, drumKit.Length));
            }

            public bool PointInRegion(float touchPoint)
            {
                if (touchPoint >= BeatPos && touchPoint < (BeatPos + BeatLength))
                {
                    return true;
                }
                return false;
            }
        };
        //class AIDrumSaveData
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////

        public class AIBeatLine
        {
            // // // // // // // // // // //
            //PUBLIC SAVED DATA
            public List<AIDrumBeat> mBeatList = new List<AIDrumBeat>();
            public int mRootNoteNum;

            public void CreateBeatsIfNone( int numBeats)
            {
                if (mBeatList.Count == 0)
                {
                    for (int i = 0; i < numBeats; i++)
                    {
                        AIDrumBeat bl = new AIDrumBeat(i);
                        mBeatList.Add(bl);
                    }
                }
                System.Diagnostics.Debug.Assert(mBeatList.Count == numBeats, string.Format("mBeatList.Count {0} not equal numBeats {1}!", mBeatList.Count, numBeats));
            }

            public void ClearLine()
            {
                foreach(AIDrumBeat db  in mBeatList)
                {
                    db.ClearNote();
                }
            }
        }; //class AIBeatLine

        ////////////////////////////////////////////////////////////////////////////////////////////
        /// DRUM BEAT SAVE DATA
        public class AIDrumBeat
        {
            const int VELOCITY_LEVELS = 3; //each double click will cycle the 3 levels
            public const float OFFSET_BEAT_DIVISION = 0.03f; //we currrently allow up to twice this offset on the beat to give it a more 'live' feel

            // // // // // // // // // // //
            //PUBLIC SAVED DATA
            public int mVelocityLevel = 1;
            public int mVelocity = MidiBase.MID_VELOCITY;
            public bool mbBeatOn;
            public int mOffsetLevel = 0; //How much the beat is pushed forward or back in time to give a more natural feel
            //public int mNoteIndex = -1; //TODO I DONT THINK WE NEED THIS!!!! 

            Note mNote;
            public void SetNote( Note n)
            {
                //System.Diagnostics.Debug.Assert(mNote == null, string.Format("mNote is not null!"));
                mbBeatOn = true;
                mNote = n;

                if(mNote!=null)
                {
                    mNote.Velocity = mVelocity;
                    AIDrumRegion.msLastEditedBeat = this;

                    if(mOffsetLevel!=0)
                    {
                        float widthNote = n.BeatLength;
                        float offsetUnit = OFFSET_BEAT_DIVISION * widthNote;
                        float noteOffset = offsetUnit * (float)mOffsetLevel;

                        float newPos = mNote.BeatPos + noteOffset;
                        mNote.SetPosLen(newPos, mNote.BeatLength);
                    }
                }
            }

            public bool SetOffsetLevel(int newOffsetLevel)
            {
                mOffsetLevel = newOffsetLevel;

                if (mNote != null)
                {
                }

                return true;
            }

            public bool CycleVelocity()
            {
                if (mNote != null)
                {
                    mVelocityLevel++;
                    mVelocityLevel = mVelocityLevel % VELOCITY_LEVELS;

                    switch(mVelocityLevel)
                    {
                        case 0:
                            mVelocity = MidiBase.LOW_VELOCITY;
                            break;
                        case 1:
                            mVelocity = MidiBase.MID_VELOCITY;
                            break;
                        case 2:
                            mVelocity = MidiBase.HIGH_VELOCITY;
                            break;
                    }
                    mNote.Velocity = mVelocity;
                    mNote.RefreshRectToData();
                    return true;
                }

                return false;
            }

            public bool ClearNote()
            {
                bool bRemoved = false;
                //System.Diagnostics.Debug.Assert(mNote != null, string.Format("Expect mNote is not null!"));
                mbBeatOn = false;

                if (mNote!=null)
                {
                    AIDrumRegion.msLastEditedBeat = this;
                    bRemoved = true;
                    mNote.RemoveFromParentLine();
                }
                mNote = null;

                return bRemoved;
            }

            public void CopyBeat(AIDrumBeat other)
            {
                //mNoteIndex = other.mNoteIndex;
                mVelocityLevel = other.mVelocityLevel;
                mVelocity = other.mVelocity;
                mbBeatOn = other.mbBeatOn;
                mOffsetLevel = other.mOffsetLevel;
            }

            public Note GetNote()
            {
                return mNote;
            }

            public AIDrumBeat(int noteIndex)
            {
                //mNoteIndex = noteIndex;
                Reset();
            }

            public void Reset()
            {
                //Remove note from note line - 
                mVelocity = MidiBase.MID_VELOCITY;
                mVelocityLevel = 1;
                mbBeatOn = false;
             }

            public void CreatNoteFromCurrent(int noteNum)
            {
                if(mbBeatOn)
                {

                }
            }
        } //class AIDrumBeat
        /// DRUM BEAT SAVE DATA
        ////////////////////////////////////////////////////////////////////////////////////////////

        public void SetPosLen(float pos, float len)
        {
            BeatPos = pos;
            BeatEndPos = pos + len;
            RefreshRectToData();
        }

        public int[] mDrumKit;


        public void CopyOtherReegion(AIDrumRegion other)
        {
            AIDrumSaveData dsd = other.GetSaveData();
            //I know, TODO
            string serialisedDSD  = JsonConvert.SerializeObject(dsd);
            AIDrumSaveData ldsd = JsonConvert.DeserializeObject<AIDrumSaveData>(serialisedDSD);

            ldsd.BeatPos = BeatPos;
            LoadSaveData(ldsd);
        }

        // // // // // // // // // // // // //
        //LOAD SAVE


        //NEEDS MORE WORK!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        public void LoadSaveData(AIDrumSaveData loadData)
        {

            //Fix up old data being loaded that could removed the extra beat lines.
            //If I ever add more drum beat types they will be at the end and hopefully will still show
            //after loading olddrum saves

            List<AIBeatLine> fixUpExtraBeatLines = new List<AIBeatLine>();
            if (mDrumData!=null)
            {
                if(mDrumData.mBeatLineList.Count > loadData.mBeatLineList.Count)
                {
                    int diff = mDrumData.mBeatLineList.Count - loadData.mBeatLineList.Count;

                    int endNewLine = mDrumData.mBeatLineList.Count;
                    int firstNewLine = endNewLine - diff;

                    for (int i= firstNewLine; i< endNewLine; i++)
                    {
                        AIBeatLine addBL = mDrumData.mBeatLineList[i];
                        fixUpExtraBeatLines.Add(addBL);
                    }
                }
            }

            foreach(AIBeatLine addBL in fixUpExtraBeatLines)
            {
                loadData.mBeatLineList.Add(addBL);
            }

            mDrumData = loadData;
            mDrumData.SetPlayArea(mParent.mParent);
            BeatPos = mDrumData.BeatPos;
            BeatEndPos = mDrumData.BeatPos + BeatLength;
            mWidth = mDrumData.mWidth;

            //TODO set up region to loaded data - check it has parent - or do we need to?
            foreach (AIBeatLine bl in mDrumData.mBeatLineList)
            {
                NoteLine nl = mParent.mParent.GetNoteLineAtNoteNumber(bl.mRootNoteNum);

                int beatnum = 0;
                foreach (AIDrumBeat db in bl.mBeatList)
                {
                    if(db.mbBeatOn)
                    {
                        mbHasBeenEdited = true; //If there are any beats found set then we can set this :) 

                        float beatPos = BeatPos + beatnum * mWidth;

                        Note note = nl.AddNote(beatPos, mWidth); // expect all lines cleared by here 
                        db.SetNote(note);
                   
                    }

                    beatnum++;
                }
            }
        }

        public AIDrumSaveData GetSaveData()
        {
            return mDrumData;
        }
        
        //LOAD SAVE
        // // // // // // // // // // // // //
        public void Update(MelodyEditorInterface.MEIState state)
        {        
            Input(state);
        }

        bool Input(MelodyEditorInterface.MEIState state)
        {
            if (state.mbAllowInputPlayArea && state.input.LeftUp)
            {
                if (state.input.mRectangle.Intersects(mRect) && state.mbPlayAreaUpdate)
                {
                    state.mAIDrumRegionPopup.Show(this, state);
                    return true;
                }
            }

            return false;
        }

        public bool IsNoteInRegion(float pos, float width)
        {
            if (pos >= BeatPos && (pos + width) <= BeatEndPos)
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
                Color fillCol = Col;
                Color col = Color.DarkSalmon;
                if(mCopied)
                {
                    fillCol = Color.Purple;
                }
                else
                if(mPasteCandidate)
                {
                    fillCol = Color.PaleVioletRed;
                }
                if (mSelected)
                {
                    col = Color.Yellow;
                }
                sb.FillRectangle(mRect, fillCol);
                sb.DrawRectangle(mRect, mbHasBeenEdited? col : Color.Black, mbHasBeenEdited ?5f:1f );
            }
        }
    }
}
