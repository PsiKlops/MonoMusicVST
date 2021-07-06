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
//using StuThread = System.Threading.Thread;

namespace MonoMusicMaker //.MEI
{
    public class PlayArea
    {
        const int DEFAULT_START_DRAW_LINE = 0;

        public class LoopRange
        {
            public int StartBar = 0;
            public int EndBar = PlayArea.MAX_BAR_RANGE;

            public int GetNumBeats() { return (EndBar - StartBar) * (int)BEATS_PER_BAR; }
            public float GetStartBeat() { return (float)(StartBar) * BEATS_PER_BAR; }
            public float GetEndBeat() { return (float)(EndBar) * BEATS_PER_BAR; }

            public void Reset()
            {
                StartBar = 0;
                EndBar= PlayArea.MAX_BAR_RANGE;
            }
        }

        NoteUI mNoteUI = null;

        public List<int> mNotesOn = new List<int>();

        //NEW KEY NODE WHERE JUST THE NOTES FOR THE SLECTED KEY ARE SHOWN
        public bool mbEnableParamLines = false;
        public bool mKeyModeNoteLines = false;
        public int mKeyModeKey = 0;   // C
        public int mKeyModeScale = 0; //Major
        public int mChordType = 0; //I - TODO have this variable in every play area?

        public bool HasCurrentView = false;

        public MidiEventCollection UndoChangeData = null; //each play area will keep a record of their last edit change in case it needs undoing

        public int mInstrument = 0;
#if WINDOWS
        public const int NUM_DRAW_BEATS = 32; //<- BEATS_SHOWN!
        public const int NUM_DRAW_LINES = 32;
        public const int FULL_RANGE =(int) (128);
        public const int NOTE_START = 127;
#else
        public const int NUM_DRAW_BEATS = 16; //<- BEATS_SHOWN!
        public const int NUM_DRAW_LINES = 16;
        public const int FULL_RANGE = (int)(16 * 5);
        public const int NOTE_START = 100;
#endif
        public const int NOTE_END = NOTE_START - (FULL_RANGE - 1);

        public const int DRAW_AREA_NOTE_START = NOTE_START + NUM_DRAW_LINES;
        public const int GRAY_RECTANGLE_START_X = 10;
        public const int GRAY_RECTANGLE_Y_LIFT = 60;

        public const int DRUM_CHANNEL_NUM = 10;

        public static  Color mSideBarColor = Color.YellowGreen;

        public const int WIDTH_DRAW_AREA = NUM_DRAW_BEATS * (int)MelodyEditorInterface.BEAT_PIXEL_WIDTH;

        const int ABUT_BAR_WIDTH = 12;

        Rectangle mDebugTouch;

        Rectangle mBottonAreaMask;
        Rectangle mLeftAreaMask;
        public Rectangle mRightAreaMask;

        Rectangle mLeftStartBarIndicator; //only drawn to show area is at start
        Rectangle mRightEndBarIndicator; //only drawn to show area is abutting end
        bool mbDrawLeftStartBarIndicator = true;
        bool mbDrawRightEndBarIndicator = true;

        Rectangle mLeftLoopAreaOverlay; //Show alpha'd grey covering the area before where loop starts
        Rectangle mRightLoopAreaOverlay;//Show alpha'd grey covering the areas after where loop ends

        public Rectangle mAreaRectangle;
        public Rectangle mGrayOverLayRectangle;
        float mScale;
        public const float MAX_BEATS = 128f;
        public const float MIN_BEAT_LENGTH = 0.1f;
        public const float BEATS_PER_BAR = 4f;
        public const float BEATS_PER_HALFBAR = BEATS_PER_BAR / 2f;
        public const int MAX_BAR_RANGE = (int)(MAX_BEATS / BEATS_PER_BAR);

        public const int NUM_BARS_FOR_LOOPS_SAVED = (int)MAX_BEATS*2; // Todo make a whole multiple of MAX_BEATS (int)MAX_BEATS; //TODO LONGER SAVES

        TopLineChordAI mTopLineAIChord;
        TopLineDrumAI mTopLineAIDrum = null;
        ParameterNodeManager mPNM;

        List<NoteLine> m_NoteLines;
        List<NoteLine> m_RenderedNoteLines;
        List<GridLine> m_Lines;

        LinkedList<NoteLine> mNoteLineLL = new LinkedList<NoteLine>();

        bool mbExtraLineWhileScrolling = false;

        public PlayHead mPlayHead;
        public float mMS_Per_Beat;
        public int mPlayAreaPlayingTimeOffsetTimeMs = 0;


        public int StartBar { set; get; } = 0;
        public int EndBar { set; get; } = (MAX_BAR_RANGE);

        public List<Note> GetNotesInRectangle(Rectangle rect, bool bAllTracks = false)
        {
            List<NoteLine> updateNL = GetRenderedNoteLines();
            //RENDERED NOTES LINES
            List<Note> ln = new List<Note>();
            foreach (NoteLine nl in updateNL)
            {
                List<Note> lnl = nl.GetNotesInRectangle(rect, bAllTracks);
                ln.AddRange(lnl);
            }
            return ln;
        }

        ///////////////////////////////////////////////////////////////////
        //HELPERS TO GET NOTE LINES RELATIVE IN THE CURRENT RENDERED LINES -I.E. NOTE RELATIVE TO NOTE NUM
        public int GetLineIndexForNoteNum(int notenum)
        {
            List<NoteLine> updateNL = GetRenderedNoteLines();

            int lineIndex = 0;
            foreach (NoteLine nl in updateNL)
            {
                if (nl.NoteNum == notenum)
                {
                    return lineIndex; 
                }
                lineIndex++;
            }

            return -1;
        }

        public NoteLine GetNoteLineFromLineIndex(int lineIndex)
        {
            if(lineIndex < 0)
            {
                return null;
            }

            List<NoteLine> updateNL = GetRenderedNoteLines();

            if(lineIndex >= updateNL.Count)
            {
                return null;
            }

            return updateNL[lineIndex];
        }

        public NoteLine GetTopNoteLineInRect(Rectangle rect)
        {
            List<NoteLine> updateNL = GetRenderedNoteLines();

            foreach (NoteLine nl in updateNL)
            {
                if(nl.IsNoteLineCoveredByRect(rect))
                {
                    return nl; //should be the top one
                }
            }

            return null;
        }

        public Note GetHighesteNoteInRegion(AIChordRegion r)
        {
            foreach (NoteLine nl in m_NoteLines)
            {
                Note n = nl.GetFirstNoteInRegion(r);

                if(n!=null)
                {
                    return n;
                }
            }

            return null;
        }

        public Note GetHighesteNoteInRegionAndDeleteRest(AIChordRegion r)
        {
            foreach (NoteLine nl in m_NoteLines)
            {
                Note n = nl.GetFirstNoteInRegion(r);

                if (n != null)
                {
                    return n;
                }
            }

            return null;
        }

        void DrawMasks(SpriteBatch sb)
        {

        }

        public ParameterNodeManager GetTopLineParamRegion()
        {
            return mPNM;
        }

        public void EnableParam(bool bEnable)
        {
            mbEnableParamLines = bEnable;

            //if (mPNM!=null)
            //{
            //    mPNM.ToggleRegions(bEnable);
            //}
        }

        public TopLineChordAI GetTopLineChordAI()
        {
            return mTopLineAIChord;
        }
        public ParameterNodeManager GetParamNodeManager()
        {
            return mPNM;
        }
        public TopLineDrumAI GetTopLineDrumAI()
        {
            return mTopLineAIDrum;
        }

        public void ClearParamNodeManager()
        {
            if(mPNM!=null)
            {
                mPNM.Init();
            }
        }

        public void ClearDrumRegions()
        {
            mTopLineAIDrum.Init();
        }

        public void ClearAIRegions()
        {
            mTopLineAIChord.Init();
        }

        public void RemoveAllNotesInRegion(float start, float end)
        {
            foreach (NoteLine t in m_NoteLines)
            {
                t.RemoveNotesInRegion(start, end);
            }
        }

        public void ResetAllNotesAndRegion()
        {
            RemoveAllNotesInRegion(0, MAX_BEATS);
        }

        public List<AIChordRegion.AIRegionSaveData> GetRegionSaveData()
        {
            return mTopLineAIChord.GetRegionSaveData();
        }
        public void  SetRegionSaveData(List<AIChordRegion.AIRegionSaveData> rsd)
        {
           mTopLineAIChord.SetRegionSaveData(rsd);
        }
        public void SetDrumSaveData(List<AIDrumRegion.AIDrumSaveData> ldsd )
        {
            if (mTopLineAIDrum != null)
            {
                mTopLineAIDrum.SetRegionSaveData(ldsd);
            }
        }

        public List<ParameterNodeRegion.ParameterNodeRegionSaveData> GetPNRSaveData()
        {
            return mPNM.GetRegionSaveData();
        }
        public void SetPNRSaveData(List<ParameterNodeRegion.ParameterNodeRegionSaveData> pnsd)
        {
            if(mPNM!=null)
            {
                mPNM.SetRegionSaveData(pnsd);
            }
        }

        public void SetPNMSaveData(List<ParameterNodeRegion.ParameterNodeRegionSaveData> ldsd)
        {
            if (mPNM != null)
            {
                mPNM.SetRegionSaveData(ldsd);
            }
        }

        public void SetUpBarsPostLoadOrUndo(LoopRange lr)
        {
            StartBar =lr.StartBar;
            EndBar = lr.EndBar;
            UpdateLoopOverlay();
        }

        public bool GetSaveBarRange(LoopRange lr)
        {
            if(StartBar!=0 || EndBar!=MAX_BAR_RANGE)
            {
                lr.StartBar = StartBar;
                lr.EndBar = EndBar;
                return true;
            }
            return false;
        }

        int mBPM = 0; //just when reading back from midi files
        public int GetReadBPM() { return mBPM; }

        int mNumTracks = 0;
        public int StartDrawLine = 0;

        private float _StartBeat = 0f;

        public float StartBeat
        {
            set { _StartBeat = value; RefreshEndIndicators(); }
            get { return _StartBeat; }
        } 

        public static void SetPlayAreaAsRecord(PlayArea pa)
        {
            mMidiRecorder.SetNewParent(pa);
            mEventMaker.Init(pa); //TODO sort the overlap in functionality and data going on with mPAPipeLine, mEventMaker and mMidiRecorder
        }

        public void SetPlayAreaAsView(bool pipelineMode)
        {
            if(!pipelineMode)
            {
                SetPlayAreaAsRecord(this); //pipeline from one playare to another is taking the record feed so dont set when change view
            }

            _StartBeat = StartBar * BEATS_PER_BAR;
        }

        private void RefreshEndIndicators()
        {
            mbDrawLeftStartBarIndicator = false;
            mbDrawRightEndBarIndicator = false;
            if (StartBeat == 0)
            {
                mbDrawLeftStartBarIndicator = true;
            }
            if (NUM_DRAW_BEATS + StartBeat >= MAX_BEATS)
            {
                mbDrawRightEndBarIndicator = true;
            }
        }

        public float TrackFloatPos = 0f;

        public void SetTrackFloatPos(float setVal)
        {
            TrackFloatPos = setVal;
            mbExtraLineWhileScrolling = TrackFloatPos == 0f? false: true;
        }

        public int mPan = 127 / 2;
        public int mVolume = 127 / 2;
        public int mChannel = 1;
        public SpriteFont mFont;

        public bool HasNotes { set; get; } = false;

        bool mbAllowTouchChangeInArea = true;
        bool mbShowGrayRectangle = false;

        void ResetPanVol()
        {
            mPan = 127 / 2;
            mVolume = 127 / 2;
        }

        public float GetVolAsRatio()
        {
            return (float)mVolume / 127.0f;
        }
        public float GetPanAsRatio()
        {
            return (float)mPan / 127.0f;
        }

        public void SetFont(SpriteFont font )
        {
            mFont = font;

            if(mPNM!=null)
            {
                mPNM.SetFont(font);
            }

            foreach (NoteLine nl in m_NoteLines)
            {
                nl.mFont = mFont;
            }
            foreach (GridLine gl in m_Lines)
            {
                gl.mFont = mFont;
            }
        }

        public void SetPlayAreaStartDrawNoteLineToToTopActualNotes()
        {
            StartDrawLine = 0;

            foreach (NoteLine nl in m_NoteLines)
            {
                if(nl.GetNotes().Count>0)
                {
                    if (StartDrawLine > (m_NoteLines.Count - NUM_DRAW_LINES) )
                    {
                        StartDrawLine = m_NoteLines.Count - NUM_DRAW_LINES;
                    }
                    return;
                }
                StartDrawLine++;
            }

            //None found put this at the default
            StartDrawLine = DEFAULT_START_DRAW_LINE;
        }

        public void RestartAllOnNotes(MelodyEditorInterface.MEIState state)
        {
            foreach (NoteLine nl in m_NoteLines)
            {
                nl.ReStartNote(state);
            }
        }

        public void RewindToPos(float pos)
        {
            mPlayAreaPlayingTimeOffsetTimeMs = 0; //Alwasy clear the offset if rewinding to a pos
            mPlayHead.SetBeatPos(pos);
        }

        
        /////////////////////////////////////////////////////////////////////////////////////////////////
        //EVENT MAKER RUNS THROUGH THE NOTELINE STATES AND GENERATES EVENTS FOR ANY CHANGES FROM ON TO OFF OR OFF TO ON 
        //SIMILAR TO MIDI KEYBOARD IN
        /////////////////////////////////////////////////////////////////////////////////////////////////
        static PA_EventMaker mEventMaker;
        public class PA_EventMaker
        {
            PlayArea mParent;

            public struct NoteEvent
            {
                public int mNoteNum;
                public int mVelocity;
                public bool mOn;
            }

            public void Init(PlayArea pa)
            {
                mParent = pa;

                for ( int i =0; i< 128; i++)
                {
                    mRec[i] = false;
                }
            }

            bool[] mRec = new bool[128];

            public void StartUpdate()
            {
                mOnEvent = new List<NoteEvent>();
            }

            public void Update(int noteNum, int velocity)
            {
                bool bOn = velocity != -1;

                if(mRec[noteNum] != bOn)
                {
                    mRec[noteNum] = bOn;
                    NoteEvent ne = new NoteEvent();
                    ne.mOn = bOn;
                    ne.mVelocity = velocity;
                    ne.mNoteNum = noteNum;
                    mOnEvent.Add(ne);
                }
            }

            public void FeedEventsThrough()
            {
                foreach(NoteEvent ne in mOnEvent)
                {
                    if(ne.mOn)
                    {
                        mParent.RecordNoteOn(ne.mNoteNum, ne.mVelocity);
                    }
                    else
                    {
                        mParent.RecordNoteOff(ne.mNoteNum);
                    }
                }
            }

            public List<NoteEvent> mOnEvent = new List<NoteEvent>();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
        //RECORD FROM MIDI IN
        /////////////////////////////////////////////////////////////////////////////////////////////////
        static PA_MidiRecorder mMidiRecorder;
        public class PA_MidiRecorder
        {
            const float START_NOTE_WIDTH = 0.0f;
            PlayArea mParent;
            List<Note> mRecordOnList = new List<Note>();

            bool mbAndroidOffNoteTransition = false;

            private Object recThreadLock = new Object();

            public PA_MidiRecorder(PlayArea pa)
            {
                mParent = pa;
            }
            Note IsRecNoteCurrentOn(int noteNum)
            {
                foreach (Note n in mRecordOnList)
                {
                    if (n.mNoteNum == noteNum)
                    {
                        return n;
                    }
                }

                return null;
            }

            void StartRecordNoteAdd(int noteNem, int velocity, NoteLine nl)
            {
                float fPos = mParent.mPlayHead.mBeatPos;
                Note n = nl.AddNote(fPos, START_NOTE_WIDTH);
                n.Velocity = velocity;
                mRecordOnList.Add(n);
            }

            public void SetNewParent(PlayArea pa)
            {
                lock (recThreadLock)
                {
                    if (mParent!=pa)
                    {
                        StopAllRecordNotes();
                        mParent = pa;
                    }
                }
            }

            public bool UpdateRecNoteCurrentOn(MelodyEditorInterface.MEIState state)
            {
                lock (recThreadLock)
                {
                    if (!state.mbRecording && !state.mMeledInterf.mPAPipeLine.Active) //TODO!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    {
                        StopAllRecordNotes();
                    }

                    if (!state.Playing)
                    {
                        return false;
                    }

                    bool bChanged = false;

                    float fPos = mParent.mPlayHead.mBeatPos;

                    foreach (Note n in mRecordOnList)
                    {
                        NoteLine nl = n.mParent;

                        Note.NoteLoopedStatus noteLoopStatus = n.AdjustBeatToPos(fPos) ;

                        if (noteLoopStatus != Note.NoteLoopedStatus.NLSNone)
                        {
                            if (noteLoopStatus == Note.NoteLoopedStatus.NLSPosBetweenStartAndEnd)
                            {
                                //This must be a looped note that we are still holding down a key on after looping round so leave it be
                                continue;
                            }

                            //Note must have looped back, this position is before its start, probably because of being held over loop end playhead gone round
                            float fLoopNoteEnd = mParent.EndBar * PlayArea.BEATS_PER_BAR;

                            //set the note to the loop end bar pos
                            noteLoopStatus = n.AdjustBeatToPos(fLoopNoteEnd);
                            if (noteLoopStatus != Note.NoteLoopedStatus.NLSNone)
                            {
                                System.Diagnostics.Debug.Assert(false, string.Format("Note looped back at {0} but failed to have end set to loop point {1}! noteLoopStatus {2} ", fPos, fLoopNoteEnd, noteLoopStatus));
                                continue;
                            }

                            //See if we can add new note on loop back start or if this note was the same note being held from the start
                            //if same as the start keep it here and dont shorten
                            Note noteHereAlready = nl.IsNoteHere(fPos, START_NOTE_WIDTH);

                            if(noteHereAlready==n)
                            {
                                //This must be a looped note that we are still holding down a key on after looping round so leave it be but not sure how that could happen so assert
                                System.Diagnostics.Debug.Assert(false, string.Format("Note looped back at {0} but failed to have end set to loop point {1}! noteLoopStatus {2} ", fPos, fLoopNoteEnd, noteLoopStatus));
                                continue;
                            }

                            int oldVelocity = n.Velocity;
                            int oldNoteNum = n.mNoteNum;

                            //Remove the old note that was here at the start
                            mRecordOnList.Remove(n);
                            bChanged = true; //try just send change when notes are removed?

                            //add new note here
                            StartRecordNoteAdd(oldNoteNum, oldVelocity, nl);

                            //since we have altered the loop list leave
                            break;
                        }

                        Note noteOverlapped = nl.TouchInNote(n.mRect, n);
                        if (noteOverlapped != null)
                        {
                            nl.RemoveNote(noteOverlapped);
                        }
                    }

                    return bChanged;
                }

                return false;
            }

            public void RecordNoteOn(int noteNum, int velocity)
            {
                lock (recThreadLock)
                {
                    NoteLine nl = mParent.GetRecordNoteLineAtNoteNumber(noteNum);
                    if (nl != null)
                    {
                        if (IsRecNoteCurrentOn(noteNum) != null)
                        {
                            System.Diagnostics.Debug.Assert(false, string.Format("Note already on!? {0} ", noteNum));
                            return;
                        }

                        StartRecordNoteAdd(noteNum, velocity, nl);
                        return;
                    }

                    System.Diagnostics.Debug.Assert(false, string.Format("No note line at  {0} ", noteNum));
                }
            }

            public void RecordNoteOff(int noteNum)
            {
                lock (recThreadLock)
                {
                    Note n = IsRecNoteCurrentOn(noteNum);
                    if (n != null)
                    {
                        //There was some issues with record length not sounding too good
                        //probably threads no playing well - play back is fine the recorded
                        //not so good
                        float fPos = mParent.mPlayHead.mBeatPos; //putting this final end pos note revision here seems to have fixed above
                        n.AdjustBeatToPos(fPos);

                        mRecordOnList.Remove(n);
                        bool bTransitionedOff = mRecordOnList.Count == 0;
                        mbAndroidOffNoteTransition |= bTransitionedOff;
                        return;
                    }
                    //Probably dont need to assert as record off can be seen when not playing and I think better to allow off signals through all the time? -> System.Diagnostics.Debug.Assert(false, string.Format("No note line at  {0} ", noteNum));
                }
            }

            void StopAllRecordNotes()
            {
                bool bHadNotes = mRecordOnList.Count != 0;
                mbAndroidOffNoteTransition |= bHadNotes;
                mRecordOnList.Clear();
            }

            //Have an issue with a phone being very slow to update the custom
            //mini note texture "SquareCursor" window when holding down notes
            //and letting it get called constantly, so a bit hacky but for android will 
            //only update when we see notes go off, so completed notes get rendered
            //all in one go at the end of all keys released - yes, yes, will make for weird issues
            //of not seeing notes laid down if you hold for a while
            public bool GetAndroidUpdateSquareCursor()
            {
                if(mbAndroidOffNoteTransition)
                {
                    mbAndroidOffNoteTransition = false;
                    return true;
                }
                return false;
            }

        }

        public void RecordNoteOn(int noteNum, int velocity)
        {
            mMidiRecorder.RecordNoteOn(noteNum, velocity);
        }

        public void RecordNoteOff(int noteNum)
        {
            mMidiRecorder.RecordNoteOff(noteNum);
        }


        void InitNoteLines()
        {
            m_NoteLines = new List<NoteLine>();
            NoteLine t = null;
            int noteNum = NOTE_START;
            bool bSharp = false;
            for (int i = 0; i < FULL_RANGE; i++)
            {
                t = new NoteLine(this);
                int noteNumber = noteNum - i;
                t.Init(i, noteNumber, StartDrawLine);
                if(mChannel==10) //whay!? I thought I removed this?  || mChannel==16)
                {
                    t.mbSharp = bSharp;
                    bSharp = !bSharp;
                }
                m_NoteLines.Add(t);
            }

            if (mChannel != PlayArea.DRUM_CHANNEL_NUM) //Dont need key for drums!
            {
                SetRenderedNoteLinesToKeyRequested(mKeyModeKey); //When loading songs this mKeyModeKey should stay the same so I think this should be OK
            }
            //Init Area Masks - for hiding not and region scrolling off the play area window
        }


        //Create a way to convert C notes to contiguous indexed array
        int[] mC_Notes = new int[128];
        void CreateC_Notes()
        {
            int oldkms = mKeyModeScale;
            int oldkmk = mKeyModeKey;

            int count = 0;
            for (int noteNum = 0; noteNum < 128; noteNum++)
            {
                if(IsNoteInSelectedScale( noteNum))
                {
                    mC_Notes[noteNum] = count;
                    count++;
                }
                else
                {
                    mC_Notes[noteNum] = -1;
                }
            }

            mKeyModeScale = oldkms ;
            mKeyModeKey = oldkmk ;
        }


        ///////////////////////////////////////////////////////////////////////////////////////////
        //KEY SCALE STUFF
        bool IsNoteInSelectedScale(int noteNum)
        {
            int[] scaleArraySelected = AIChord.SCALE_LIST[mKeyModeScale];

            int scaleLen = scaleArraySelected.Length;

            int normalIsedNote = noteNum % AIChord.OCTAVE_SPAN_SEMITONES;

            for(int i=0; i<scaleLen; i++)
            {
                int scaledNote = scaleArraySelected[i] + mKeyModeKey;
                int modScaled = scaledNote % AIChord.OCTAVE_SPAN_SEMITONES;

                if (normalIsedNote == modScaled)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetChordTypeSelected(int chordType)
        {
            mChordType = chordType;
            SetRenderedNoteLinesToChordTypeRequested();
        }

        public void SetKeyModeScale(int scale)
        {
            mKeyModeScale = scale;
            SetRenderedNoteLinesToKeyRequested(mKeyModeKey);
        }

        public void SetRenderedNoteLinesToChordTypeRequested()
        {
            List<NoteLine> nls = GetRenderedNoteLines();

            /////////////
            /////////////
            /// CREATE LINKED LIST  OF ALL THE NOTE LINES ADDED HERE SO WE CAN ADD NOTES IN CHORDS LATER
            mNoteLineLL = new LinkedList<NoteLine>();

            int startLine = -1;
            if(mChordType > 0)
            {
                startLine = mChordType - 1;
            }

            int currentHighLightLine = 0;

            int secondNote = -1;
            int thirdNote = -1;

            int startNoteSeen = -1; //We need to scroll back through not lines as they ascend nots that way and the drawn
                                    //lines dont necessarily start on the key note e..g. C starts in A line, so we have to find the first 
                                    //proper line upwards that matches the scale then start the process of assigning highlights

            int keyToFind = mKeyModeKey;

            bool bFoundStart = false;

            for( int i= nls.Count-1; i>=0 ; i--)
            {
                NoteLine nl = nls[i];
                nl.m_MyLLN = null;

                if (startLine>-1)
                {
                    bool bHighlight = false;

                    bool bFoundKey = keyToFind == nl.NoteNum % AIChord.OCTAVE_SPAN_SEMITONES;

                    if (startNoteSeen == -1 && bFoundKey)
                    {
                        bFoundStart = true;
                        startNoteSeen = 0;
                    }

                    if(!bFoundStart)
                    {
                        nl.mbHighlight = false;
                        continue;
                    }

                    bool startHighlight = startLine == startNoteSeen % 7; //Have to be 7 here or we 3 note gaps and out of whack chords! AIChord.OCTAVE_SPAN_NOTES; IN hindsight not sure why I ever had OCTAVE_SPAN_NOTES = 8 notes for modding like this

                    if (startHighlight)
                    {
                        bHighlight = true;
                        secondNote = startNoteSeen + 2;
                        thirdNote = secondNote + 2;
                    }

                    if(startNoteSeen == secondNote || startNoteSeen == thirdNote)
                    {
                        bHighlight = true;
                    }

                    nl.mbHighlight = bHighlight;

                    if(bHighlight)
                    {
                        LinkedListNode<NoteLine> llnl = new LinkedListNode<NoteLine>(nl);
                        nl.m_MyLLN = llnl;
                        //Link list
                        mNoteLineLL.AddLast(llnl);
                    }

                    if (startNoteSeen != -1 )
                    {
                        startNoteSeen++;
                    }
                }
                else
                {
                    nl.mbHighlight = false;
                }
            }
        }

        public void SetRenderedNoteLinesToKeyRequested(int key)
        {
            mKeyModeKey = key;
            NoteLine t = null;

            m_RenderedNoteLines = new List<NoteLine>();
            int renderedTrackNum = 0;
 
            for (int i = 0; i < m_NoteLines.Count; i++)
            {
                t = m_NoteLines[i];
                if (mKeyModeNoteLines)
                {
                    if (IsNoteInSelectedScale(t.NoteNum))
                    {
                        m_RenderedNoteLines.Add(t);
                        t.Init(renderedTrackNum, t.NoteNum, StartDrawLine, false); // set the rendered note line to the track num and position
                        renderedTrackNum++;
                    }
                    else
                    {
                        t.mbInCurrentScale = false;
                    }
                }
                else
                {
                    m_RenderedNoteLines.Add(t);
                    t.Init(renderedTrackNum, t.NoteNum, StartDrawLine, false); // This will reset the positions of the m_NoteLines back to match the init state
                    renderedTrackNum++;
                }
            }
        }

        public NoteLine GetNoteLineAtNoteNumber(int noteNumber)
        {
            foreach (NoteLine nl in m_NoteLines)
            {
                if(nl.NoteNum == noteNumber)
                {
                    return nl;
                }
            }
            return null;
        }

        public int GetConvertedNoteNumForCurrentKeyScale(int noteIn, bool bOn, bool bKeyScaleMode)
        {
            int newNoteNum = noteIn;

            if(bKeyScaleMode)
            {
                newNoteNum =  mC_Notes[noteIn];

                if (newNoteNum != -1)
                {
                    int renderedIndex = m_RenderedNoteLines.Count - 1;
                    NoteLine knl = m_RenderedNoteLines[renderedIndex - newNoteNum];
                    knl.mbOnRectOn = bOn;
                    newNoteNum = knl.NoteNum;
                }
            }
            else
            {
                NoteLine nl = GetRecordNoteLineAtNoteNumber(newNoteNum);
                nl.mbOnRectOn = bOn;
            }

            return newNoteNum;
        }

        public NoteLine GetRecordNoteLineAtNoteNumber(int noteNumber)
        {
            foreach (NoteLine nl in m_NoteLines)
            {
                if (nl.NoteNum == noteNumber)
                {
                    return nl;
                }
            }
            return null;
        }

        public NoteLine GetNoteLineAboveOrBelowNoteNumInCurrentScale(int noteNumber, bool bUp)
        {
            int indexCount = 0;
            foreach (NoteLine nl in m_RenderedNoteLines)
            {
                if (nl.NoteNum == noteNumber)
                {
                    if (bUp)
                    {
                        if ((indexCount - 1) >=0)
                        {
                            return m_RenderedNoteLines[indexCount - 1];
                        }
                    }
                    else
                    {
                        if ((indexCount + 1) < m_RenderedNoteLines.Count)
                        {
                            return m_RenderedNoteLines[indexCount + 1];
                        }
                    }
                    break;
                }
                indexCount++;
            }

            return null;
        }


        public bool GetEditedY()
        {
            bool retVal = mbEditedYWindowToken;
            mbEditedYWindowToken = false;
            return retVal;
        }
        bool mbEditedYWindowToken = false;
        public void RefreshLinesToOffset()
        {
            foreach (NoteLine nl in m_NoteLines)
            {
                nl.RefreshDrawRectForOffset(StartDrawLine);
            }

            if (mPNM != null)
            {
                mPNM.RefreshDrawRectForOffset(StartDrawLine);
            }

            if (mTopLineAIDrum!=null)
            {
                mTopLineAIDrum.RefreshDrawRectForOffset(StartDrawLine);
            }
            else
            {
                mTopLineAIChord.RefreshDrawRectForOffset(StartDrawLine);
            }

            if (mNoteUI.mMusicPreviewer != null)
            {
                mNoteUI.mMusicPreviewer.RefreshLinesToOffset(StartDrawLine);
            }
            mbEditedYWindowToken = true; //if this changes then the edit window can read and reset it and do any changes it needs
        }

        public bool IsDrumArea()
        {
            return mChannel == (MidiTrackSelect.DRUM_MACHINE_PLAY_AREA_INDEX + 1);
        }

        public void Init(float fMS_Per_Beat)
        {
            if(mMidiRecorder==null)
            {
                mMidiRecorder = new PA_MidiRecorder(this);
            }
            if (mEventMaker == null)
            {
                mEventMaker = new PA_EventMaker();
            }

            mMS_Per_Beat = fMS_Per_Beat;
            m_Lines = new List<GridLine>();

            mTopLineAIChord = new TopLineChordAI(this);
            mTopLineAIChord.Init();


            if (IsDrumArea())
            {
                mTopLineAIDrum = new TopLineDrumAI(this);
            }
 
            InitNoteLines();
            NoteLine t = m_NoteLines[0];

            mAreaRectangle = new Rectangle(t.mStartX, m_NoteLines[0].mStartY, NoteLine.mTrackWidth, NUM_DRAW_LINES * NoteLine.TRACK_HEIGHT);
            mGrayOverLayRectangle = new Rectangle(GRAY_RECTANGLE_START_X, mAreaRectangle.Y- GRAY_RECTANGLE_Y_LIFT, mAreaRectangle.Width+480, mAreaRectangle.Height+ GRAY_RECTANGLE_Y_LIFT);

            //else //IS DRUMS!!!! ALLOW PARAMS NOW!!!! put after area rect set
            {
                mPNM = new ParameterNodeManager(this); //TODO For now no param changes on drum area
                mPNM.Init();
            }

            mDebugTouch = new Rectangle(0, 0, 4, 4);

            //After all the set up above
            mPlayHead = new PlayHead(this);
            mPlayHead.Init(this, Color.White);

            //int numgridLines = (int)(NUM_DRAW_BEATS / BEATS_PER_BAR);

            //for(int i=0;i<numgridLines;i++)
            //{
            //    GridLine gl = new GridLine();
            //    int startX = MelodyEditorInterface.TRACK_START_X + (int)(BEATS_PER_BAR * (i+1) * MelodyEditorInterface.BEAT_PIXEL_WIDTH);
            //    gl.Init(this, Color.LightBlue, startX);
            //    m_Lines.Add(gl);
            //}
            int numgridLines = (int)(NUM_DRAW_BEATS);

            for (int i = 0; i < numgridLines; i++)
            {
                GridLine gl = new GridLine();
                int startX = MelodyEditorInterface.TRACK_START_X + (int)( (i + 1) * MelodyEditorInterface.BEAT_PIXEL_WIDTH);
                gl.Init(this, Color.Black, startX);
                m_Lines.Add(gl);
            }

            UpdateGridLines();

            int bottomMaskY = m_NoteLines[0].mStartY + NUM_DRAW_LINES * NoteLine.TRACK_HEIGHT;
            int leftAreaWidth = t.mStartX;
            int sideMaskHeight = NUM_DRAW_LINES * NoteLine.TRACK_HEIGHT + 10 * NoteLine.TRACK_HEIGHT;
            int sideMaskY = m_NoteLines[0].mStartY - NoteLine.TRACK_HEIGHT;
            int fudge = 1;
            mBottonAreaMask = new Rectangle(t.mStartX, bottomMaskY, NoteLine.mTrackWidth+ fudge, NoteLine.TRACK_HEIGHT*10);
            mLeftAreaMask = new Rectangle(0, sideMaskY, leftAreaWidth, sideMaskHeight);
            mRightAreaMask = new Rectangle(t.mStartX + NoteLine.mTrackWidth, sideMaskY, NoteLine.mTrackWidth, sideMaskHeight); //leave width at NoteLine.mTrackWidth for now since notes could be wide


            int leftStartIndicatorX = t.mStartX - ABUT_BAR_WIDTH;
            int sideIndicatorHeight = NUM_DRAW_LINES * NoteLine.TRACK_HEIGHT + NoteLine.TRACK_HEIGHT; //+1 for region track
            mLeftStartBarIndicator = new Rectangle(leftStartIndicatorX, sideMaskY, ABUT_BAR_WIDTH, sideIndicatorHeight);
            mRightEndBarIndicator = new Rectangle(t.mStartX + NoteLine.mTrackWidth, sideMaskY, ABUT_BAR_WIDTH, sideIndicatorHeight); //leave width at NoteLine.mTrackWidth for now since notes could be wide

            int leftLoopOverlayStartX = t.mStartX;
            int leftLoopOverlayWidth = 0;
            int rightLoopOverlayWidth = 0;
            int rightLoopOverlayStartX = t.mStartX + NoteLine.mTrackWidth - rightLoopOverlayWidth;

            int loopOverlayY = sideMaskY;
            int loopOverlayHeight = sideMaskHeight;

            mLeftLoopAreaOverlay = new Rectangle(leftLoopOverlayStartX, loopOverlayY, leftLoopOverlayWidth, loopOverlayHeight);
            mRightLoopAreaOverlay = new Rectangle(rightLoopOverlayStartX, loopOverlayY, rightLoopOverlayWidth, loopOverlayHeight);


            CreateC_Notes(); //TODO MAKE STATIC AND CALL JUST ONCE!
        }

        public bool GetNoteRange(ref float refstart, ref float refend)
        {
            refstart = 999f;
            refend = 0f;

            bool bFoundNotes = false;
            foreach (NoteLine nl in m_NoteLines)
            {
                float noteStart = 0;
                float noteEnd= PlayArea.BEATS_PER_BAR * PlayArea.MAX_BAR_RANGE;
                if (nl.GetNoteRange(ref noteStart,ref noteEnd))
                {
                    if(noteStart < refstart)
                    {
                        refstart = noteStart;
                    }
                    if (refend < noteEnd)
                    {
                        refend = noteEnd;
                    }
                    bFoundNotes = true;
                }
            }

            return bFoundNotes;
        }

        public void ResetBarLoopRange()
        {
            StartBar = 0;
            EndBar = PlayArea.MAX_BAR_RANGE;
        }

        public int SetBarLoopRangeToCoverNotesRange()
        {
            ResetBarLoopRange();

            float fStartBeat = 0;
            float fEndBeat = 0;
            if(GetNoteRange(ref fStartBeat, ref fEndBeat))
            {
                StartBar = (int)(fStartBeat / PlayArea.BEATS_PER_BAR);
                EndBar = (int)(fEndBeat / PlayArea.BEATS_PER_BAR) + 1; //Forgot why I added this +1 !? Maybe specially needed for the detected notes ranges after audio sample
            }

            return EndBar - StartBar;
        }

        public void SetBarLoopRangeToCoverBeatRange(float fStartBeat, float fEndBeat)
        {
            StartBar = (int)(fStartBeat / PlayArea.BEATS_PER_BAR);
            EndBar = (int)(fEndBeat / PlayArea.BEATS_PER_BAR);
        }

        public void ResetNotesByFactor(float factor)
        {
            foreach (NoteLine nl in m_NoteLines)
            {
                nl.ResetNotesByFactor(factor);
            }
        }

        public void StartNotesAtBar(int bar)
        {
            float fStartBeat = 0;
            float fEndBeat = 0;
            if (GetNoteRange(ref fStartBeat, ref fEndBeat))
            {
                StartBar = (int)(fStartBeat / PlayArea.BEATS_PER_BAR);
                EndBar = (int)(fEndBeat / PlayArea.BEATS_PER_BAR) + 1;

                int diff = bar - StartBar;
                foreach (NoteLine nl in m_NoteLines)
                {
                    nl.MoveForwardByBeats(diff* PlayArea.BEATS_PER_BAR);
                }
            }
        }

        public void UpdateNoteLinesPosWhileSelectXShift()
        {
            float fOffset = StartBeat % BEATS_PER_BAR;
            foreach (NoteLine nl in m_NoteLines)
            {
                nl.OffsetNotesWhileSelectXShift(fOffset);
            }
        }
 
        public void UpdateGridLines()
        {
            int beatIntervalGridLine = 1; //(int)BEATS_PER_BAR;

            float fOffset = StartBeat % beatIntervalGridLine;

            int startGridNum = 1;
            int startOffset = (int)(StartBeat / beatIntervalGridLine);
            startGridNum += startOffset;

            int numgridLines = m_Lines.Count;
            for (int i = 0; i < numgridLines; i++)
            {
                int startX = MelodyEditorInterface.TRACK_START_X + (int)((beatIntervalGridLine * (i + 1) - fOffset)* MelodyEditorInterface.BEAT_PIXEL_WIDTH);
                m_Lines[i].UpdateSCreenX(startX);
                bool barLine = startGridNum % BEATS_PER_BAR == 0;
                if(barLine)
                {
                    m_Lines[i].TopText = startGridNum.ToString();
                    m_Lines[i].mBarLine = true;
                }
                else
                {
                    m_Lines[i].TopText = "";
                    m_Lines[i].mBarLine = false;
                }
                startGridNum++;
            }

            UpdateLoopOverlay();
        }

        void UpdateLoopOverlay()
        {
            float fOffset = StartBeat % BEATS_PER_BAR;
            float fEndBeatShown = NUM_DRAW_BEATS + StartBeat;

            float fLoopNoteStart = StartBar * PlayArea.BEATS_PER_BAR;
            float fLoopNoteEnd = EndBar * PlayArea.BEATS_PER_BAR;

           

            NoteLine t = m_NoteLines[0];
            int sideMaskHeight = NUM_DRAW_LINES * NoteLine.TRACK_HEIGHT + 2 * NoteLine.TRACK_HEIGHT;
            int sideMaskY = m_NoteLines[0].mStartY - NoteLine.TRACK_HEIGHT;

            int leftLoopOverlayStartX = t.mStartX;
            int leftLoopOverlayWidth = 0;
            if(fLoopNoteStart > StartBeat)
            {
                leftLoopOverlayWidth =(int)((fLoopNoteStart - StartBeat) * MelodyEditorInterface.BEAT_PIXEL_WIDTH);

                if(leftLoopOverlayWidth > NoteLine.mTrackWidth)
                {
                    leftLoopOverlayWidth = NoteLine.mTrackWidth;
                }
            }

            int rightLoopOverlayWidth = 0;
            if (fLoopNoteEnd < fEndBeatShown)
            {
                rightLoopOverlayWidth = (int)((fEndBeatShown - fLoopNoteEnd) * MelodyEditorInterface.BEAT_PIXEL_WIDTH);

                if (rightLoopOverlayWidth > NoteLine.mTrackWidth)
                {
                    rightLoopOverlayWidth = NoteLine.mTrackWidth;
                }
            }

            mRightLoopAreaOverlay.Width = rightLoopOverlayWidth;
            mRightLoopAreaOverlay.X = t.mStartX + NoteLine.mTrackWidth - rightLoopOverlayWidth;
            mLeftLoopAreaOverlay.Width = leftLoopOverlayWidth;
        }

        void PostLoadFixUp()
        {
            bool bSharp = false;
            int i = 0;
            bool bClearNotes = false;

            List<NoteLine> updateNL = m_NoteLines;
            if (mKeyModeNoteLines)
            {
                updateNL = m_RenderedNoteLines;
            }

            foreach (NoteLine nl in updateNL)
            {
                nl.Init(i, nl.NoteNum, StartDrawLine, bClearNotes);

                //Seem NAudio returns drum info for channel 16 for some strange reason
                // https://github.com/naudio/NAudio/blob/e359ca0566e9f9b14fee1ba6e0ec17e4482c7844/NAudio/Midi/NoteEvent.cs
                // if ((Channel == 16) || (Channel == 10))
                if (mChannel == PlayArea.DRUM_CHANNEL_NUM) //Why did I have this? -> || mChannel == 16)
                {
                    nl.mbSharp = bSharp;
                    bSharp = !bSharp;
                }
                i++;
            }

            SetFont(mFont);

            UpdateLoopOverlay();
        }

        public List<NoteLine> GetNoteLines()
        {
            return m_NoteLines;
        }

        public List<NoteLine> GetRenderedNoteLines()
        {
            if (mKeyModeNoteLines)
            {
                return m_RenderedNoteLines;
            }
            return m_NoteLines;
        }

        NoteLine GetNlForNum(int num, bool bOffEvent = false)
        {
            foreach(NoteLine nl in m_NoteLines)
            {
                if (nl.NoteNum == num)
                {
                    return nl;
                }
            }
            if (bOffEvent)
            {
                //ERROR! Should have found an existing line!
                return null;
            }
            NoteLine nnl = null;
            nnl = new NoteLine(this);
            nnl.NoteNum = num;
            m_NoteLines.Add(nnl); //new note line
            return nnl;
        }

        public void Update(MelodyEditorInterface.MEIState state)
        {
            HasCurrentView = false;

            mNoteUI = state.mNoteUI;

            if (state.mSoloCurrent && mChannel != (state.mCurrentTrackPlayArea + 1))
            {
                return;
            }

            HasCurrentView = state.mMeledInterf.GetCurrentPlayArea() == this;

            bool bRecordOrPlayChange = HasCurrentView && mMidiRecorder.UpdateRecNoteCurrentOn(state); //only update on playarea that has view

#if ANDROID
            bRecordOrPlayChange = mMidiRecorder.GetAndroidUpdateSquareCursor(); //will only allow updates if an end transition is pending being consumed
#endif
            if (state.Playing && mPlayHead.mbLeftView)
            {
                bRecordOrPlayChange |= true;
                mPlayHead.mbLeftView = false;
                state.mMeledInterf.SetToNewPlayHeadPos(mPlayHead.mBeatPos);
            }

            mPlayHead.UpdateMillisecond(state.mCurentPlayingTimeMs + mPlayAreaPlayingTimeOffsetTimeMs);
            state.fMainCurrentBeat = mPlayHead.mBeatPos; //SO THE MASTER BEAT COUNT GETS SET FROM THE CURRENT PLAY AREA HERE BEFORE NOTES REGION PARAMS ETC GET UPDATED FROM IT HERE!

            mbAllowTouchChangeInArea = state.mbAllowInputPlayArea && state.mbPlayAreaUpdate;
            mbShowGrayRectangle = !mbAllowTouchChangeInArea && !state.mAIRegionPopup.Active && !state.BlockBigGreyRectangle;

            if (state.mbPlayAreaUpdate)
            {
                mDebugTouch.X = state.input.mRectangle.X;
                mDebugTouch.Y = state.input.mRectangle.Y;
            }

            bool bAllowTouchChangeInArea = mbAllowTouchChangeInArea;
            if (bAllowTouchChangeInArea && state.input.Held)
            {
                if (!state.input.mRectangle.Intersects(mAreaRectangle))
                {
                    //Ignore touches outside
                    bAllowTouchChangeInArea = false;
                }
                state.mNoteUI.Active = bAllowTouchChangeInArea; //Show this while touching in area
            }

             HasNotes = false;

            bool bChanged = false;
            bool AnyAIRegionActive = false;
            bool bChordAIChanged = false;
            bool AnyPNodeRegionActive = false; //True if working in an active area
            if (!state.mbPreventTopLineInput)
            {
                bChordAIChanged = mTopLineAIChord.Update(state, true, ref AnyAIRegionActive);

            }
            if (mPNM != null)
            {
                if(HasCurrentView && mbEnableParamLines)
                {
                    AnyPNodeRegionActive = mPNM.UpdateInput(state, true);
                    if (AnyPNodeRegionActive)
                    {
                        state.mNoteUI.Active = false;
                    }
                }

                if (state.Playing )
                {
                    mPNM.UpdatePlaying(state);
                }
            }

            //Put drum line after Param input update as we need to know if drum update is being overridden
            if (!mbEnableParamLines && mTopLineAIDrum != null && HasCurrentView) //since the below is for input only should only update if it is in view 
            {
                bool AnyAIDrumRegionActive = false;
                mTopLineAIDrum.UpdateInput(state, true, ref AnyAIDrumRegionActive);
            }


            bool bPlayNoteLines = state.Playing && !AnyAIRegionActive;
            Note lastAddedNote = null;
            Note lastRemovedNote = null;
            bool anyLineTouched = false;

            List<NoteLine> updateNL = m_NoteLines;

            if(mKeyModeNoteLines)
            {
                updateNL = m_RenderedNoteLines;
            }

            int updateRange = updateNL.Count;

            if(HasCurrentView)
            {
                 mNotesOn = new List<int>();          
            }

            bool pipelineSource = bPlayNoteLines && state.mMeledInterf.mPAPipeLine.Active && state.mMeledInterf.mPAPipeLine.GetSource() == this;
            bool pipelineDestination = bPlayNoteLines && state.mMeledInterf.mPAPipeLine.Active && state.mMeledInterf.mPAPipeLine.GetDestination() == this;

            if (pipelineSource)
            {
                mEventMaker.StartUpdate();
            } 

            foreach (NoteLine t in updateNL)
            {
                if(bPlayNoteLines)
                {
                    if(!pipelineDestination) //dont play where the notes are going
                    {
                        t.UpdatePlaying(state);
                    }

                    if (pipelineSource)
                    {
                        mEventMaker.Update(t.NoteNum, t.mCurrentNoteInPlay != null? t.mCurrentNoteInPlay.Velocity:-1);
                    }
                }
                else
                {
                    t.UpdateHasNoteInPlayHead(state);
                }

                if (HasCurrentView)
                {
                    if (t.mbNoteInPlay)
                    {
                        mNotesOn.Add(t.NoteNum);
                    }
                }

                anyLineTouched |= t.Touched; //is no lines were touched we may need to ensure the preview note is not left on

                if (state.mbPlayAreaUpdate && !bChanged && !AnyPNodeRegionActive) //state.mbPlayAreaUpdate is true when this play area is the one on screen so only update input when this is true!
                {
                    bChanged |= t.InputUpdate(state, bAllowTouchChangeInArea);
                    lastAddedNote = t.AddedNote;
                    lastRemovedNote = t.RemovedNote;
                }
                if(!HasNotes)
                {
                    HasNotes |= t.GetNotes().Count > 0;
                }
            }

            if (pipelineSource)
            {
                mEventMaker.FeedEventsThrough();
            }

            //When note added or removed do some AI region recalculation here
            if (bChanged)
            {
                //TODO !!!!!!!!!!! Need more targetted refresh instead of just brute force all
                mTopLineAIChord.RefreshAllRegionsToLatest(lastAddedNote);

                if(lastRemovedNote!=null)
                {
                    lastRemovedNote.InformRegionNoteGone();
                }

                state.PlayAreaChanged(true);
                //update custom mini texture
                UpdateMidiTrackSelectStatus(state); //mostly text name
            }

            if(bChordAIChanged || bRecordOrPlayChange)
            {
                state.PlayAreaChanged(true, false, false);
            }

            if(StartDrawLine!=0)
            {
                //state.mDebugPanel.SetFloat(StartDrawLine, "StartDrawLine");
            }

            //Doesnt do owt but debug!
            //if(state.mNCG!=null)
            //{
            //    state.mNCG.Update(state);
            //}
        }

        public void UpdateMidiTrackSelectStatus(MelodyEditorInterface.MEIState state)
        {
            if (HasNotes || mInstrument!=0)
            {
                state.mMidiTrackSelect.SetButtonText(mChannel, InstrumentSelect.GetPatchString(mInstrument), HasNotes);
            }
            else
            {
                string buttonText = string.Format("Track {0}", mChannel);
                state.mMidiTrackSelect.SetButtonText(mChannel, buttonText);
            }
        }

        int mDrawRange = 0;

        public bool CanLineIndexBeDrawn(int index)
        {
            return index < mDrawRange && index >= StartDrawLine;
        }

        public void Draw(SpriteBatch sb)
        {
            sb.FillRectangle(mAreaRectangle, Color.Yellow*.4f);

            mDrawRange = StartDrawLine + NUM_DRAW_LINES;

            List<NoteLine> drawNL = m_NoteLines;

            if (mKeyModeNoteLines)
            {
                drawNL = m_RenderedNoteLines;
            }

            if (mDrawRange >= drawNL.Count)
            {
                mDrawRange = drawNL.Count;
            }

            if(mbExtraLineWhileScrolling)
            {
                if(mDrawRange< drawNL.Count)
                {
                    mDrawRange += 1;
                }
            }

            if (mNoteUI.mMusicPreviewer != null)
            {
                //if (mNoteUI.mMusicPreviewer.mPreviewLines != null)
                //{
                //    foreach (MusicPreviewer.NoteLinePair mpnl in mNoteUI.mMusicPreviewer.mPreviewLines)
                //    {
                //        mpnl.mRecordLine.Draw(sb, MusicPreviewer.PreviewLineType.Record);
                //        mpnl.mPreviewLine.Draw(sb, MusicPreviewer.PreviewLineType.Preview);
                //    }

                //}
                if (mNoteUI.mMusicPreviewer.mRecordLines != null)
                {
                    foreach (MusicPreviewer.NoteLinePair mpnl in mNoteUI.mMusicPreviewer.mRecordLines)
                    {
                        mpnl.Draw(sb, MusicPreviewer.PreviewLineType.Record);
                        //mpnl.mRecordLine. Draw(sb, MusicPreviewer.PreviewLineType.Record);
                    }
                }

                if (mNoteUI.mMusicPreviewer.mPreviewLines != null)
                {
                    foreach (NoteLine mpnl in mNoteUI.mMusicPreviewer.mPreviewLines)
                    {
                        mpnl.Draw(sb, MusicPreviewer.PreviewLineType.Preview);
                    }

                }
            }

            for (int i = StartDrawLine; i < mDrawRange; i++)
            {
                NoteLine t = drawNL[i];
                t.Draw(sb);
            }

            mTopLineAIChord.Draw(sb);

            if (mTopLineAIDrum != null)
            {
                mTopLineAIDrum.Draw(sb);
            }

 
            foreach (GridLine gl in m_Lines)
            {
                gl.Draw(sb);
            }

            EditManager.CopyNote.Draw(sb);
            mNoteUI.Draw(sb);

            //Loop area Overlay
            float fLoopOverlayAlpha = 0.5f;
            sb.FillRectangle(mLeftLoopAreaOverlay, Color.Gray * fLoopOverlayAlpha);
            sb.FillRectangle(mRightLoopAreaOverlay, Color.Gray * fLoopOverlayAlpha);

            if (mbEnableParamLines && mPNM != null)
            {
                mPNM.Draw(sb);
            }

            //Draw masks over scroll off area bits
            sb.FillRectangle(mBottonAreaMask, Color.Gray);
            sb.FillRectangle(mLeftAreaMask, Color.Gray);
            sb.FillRectangle(mRightAreaMask, Color.Gray);

            mPlayHead.Draw(sb);

            if (mbDrawLeftStartBarIndicator)
            {
                sb.FillRectangle(mLeftStartBarIndicator, mSideBarColor);
            }
            if(mbDrawRightEndBarIndicator)
            {
                sb.FillRectangle(mRightEndBarIndicator, mSideBarColor);
            }

            //TODO Where is the mouse pointer touching!?
            sb.FillRectangle(mDebugTouch, Color.Black);         
        }

        public void DrawOverlay(SpriteBatch sb)
        {
            if (mbShowGrayRectangle)
            {
                sb.Begin();
                sb.FillRectangle(mGrayOverLayRectangle, Color.Gray * .7f);
                sb.DrawRectangle(mGrayOverLayRectangle, Color.Black, 10);
                sb.End();
            }
        }

        //Helper for trhe play area set stuff to set a set to have the same loop range and start
        public void CopyPlayAreaOffsets(PlayArea other)
        {
            StartDrawLine = other.StartDrawLine;
            StartBeat = other.StartBeat;
            StartBar = other.StartBar;
            EndBar = other.EndBar;
        }


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// 
        ///
        /// 
        /// SAVE LOAD MIDI interfaces - The SaveLoad file has too much processing of play area stuff going on - try and make the save gather/load functionality more logical
        /// 
        ///
        /// 
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void SetUndoState(MelodyEditorInterface melEdInterf)
        {
            UndoChangeData = SaveLoad.GetNewMidiEventCollection();
            GatherIntoCollectionFromPlayArea(UndoChangeData,  melEdInterf);
        }
        public MidiEventCollection GetUndoState(MelodyEditorInterface melEdInterf)
        {
            MidiEventCollection tempUndoChangeData = SaveLoad.GetNewMidiEventCollection();
            GatherIntoCollectionFromPlayArea(tempUndoChangeData, melEdInterf);
            return tempUndoChangeData;
        }

        public void GatherIntoCollectionFromPlayArea(MidiEventCollection midiCollection, MelodyEditorInterface melEdInterf, bool bGetCurrentKeyLines = false)
        {
            int NoteVelocity = MidiBase.MID_VELOCITY;
            int NoteDuration = 0; //;!
            int BeatsPerMinute = (int)melEdInterf.BPM;

            // Looking in https://github.com/naudio/NAudio/blob/master/NAudio/Midi/MidiEventCollection.cs
            //source code seems if you set this to 0 then it automatically will
            //create tracks for each channel as you send in channel data, it seems track 0 is reserved for
            //non channel events so your channels will start from track[1] for channel 1 etc
            //int localTrackNum = 0; // pa.mChannel - 1;  

            //Forget above I prefer having all data separated out like this I think be best
            //although when use this the tracks read back as in track 0 -15 array!? 
            int localTrackNum = mChannel;

            string trackName = string.Format("Play Area {0}", mChannel);
            long absoluteTime = 0;
            midiCollection.AddEvent(new TextEvent(trackName, MetaEventType.TextEvent, absoluteTime), localTrackNum);
            ++absoluteTime;
            midiCollection.AddEvent(new TempoEvent(SaveLoad.CalculateMicrosecondsPerQuaterNote(BeatsPerMinute), absoluteTime), localTrackNum);

            int instrument = mInstrument;
            int ChannelNumber = mChannel;

            midiCollection.AddEvent(new PatchChangeEvent(0, ChannelNumber, instrument), localTrackNum);

            ControlChangeEvent volumeEvent = new ControlChangeEvent(0, ChannelNumber, MidiController.MainVolume, mVolume);
            ControlChangeEvent panEvent = new ControlChangeEvent(0, ChannelNumber, MidiController.Pan, mPan);

            midiCollection.AddEvent(volumeEvent, localTrackNum);
            midiCollection.AddEvent(panEvent, localTrackNum);

            bool bLoopedSave = melEdInterf.mState.mbSaveMIDIWithLoop;
            List<NoteLine> paNoteLines = GetNoteLines();

            if (bGetCurrentKeyLines)
            {
                paNoteLines = GetRenderedNoteLines();
            }

            foreach (var noteLine in paNoteLines)
            {
                List<Note> ln = null;

                if (bLoopedSave)
                {
                    ln = noteLine.GetNotesLoopedInRegion(PlayArea.NUM_BARS_FOR_LOOPS_SAVED);
                }
                else
                {
                    ln = noteLine.GetNotesNoLooping();
                }

                if (ln == null)
                {
                    continue;
                }

                foreach (var note in ln)
                {
                    NoteVelocity = note.Velocity;
                    absoluteTime = (long)((float)SaveLoad.TicksPerQuarterNote * note.BeatPos);
                    NoteDuration = (int)((float)SaveLoad.TicksPerQuarterNote * note.BeatLength);

                    midiCollection.AddEvent(new NoteOnEvent(absoluteTime, ChannelNumber, note.mNoteNum, NoteVelocity, NoteDuration), localTrackNum);
                    midiCollection.AddEvent(new NoteEvent(absoluteTime + NoteDuration, ChannelNumber, MidiCommandCode.NoteOff, note.mNoteNum, 0), localTrackNum);
                }
            }

            ////HACK! dummy last note to keep playing till 16 beats
            absoluteTime = (long)((float)SaveLoad.TicksPerQuarterNote * PlayArea.MAX_BEATS);
            int dummyLastNoteToDenoteEndBar = 60;
            midiCollection.AddEvent(new NoteOnEvent(absoluteTime, ChannelNumber, dummyLastNoteToDenoteEndBar, 0, 1), localTrackNum);
            NoteDuration = 1;
            midiCollection.AddEvent(new NoteEvent(absoluteTime + NoteDuration, ChannelNumber, MidiCommandCode.NoteOff, dummyLastNoteToDenoteEndBar, 0), localTrackNum);
        }


        List<ParameterNodeRegion> GatherEffectsLoopRegions()
        {
            PlayArea.LoopRange lr = new PlayArea.LoopRange();
            bool hasLoopRange = GetSaveBarRange(lr);
            List<ParameterNodeRegion> finalLoopedParamList = new List<ParameterNodeRegion>();

            if (lr.EndBar == lr.StartBar)
            {
                //no need to add any events if no playing range
                return finalLoopedParamList;
            }

            if (!hasLoopRange)
            {
               return mPNM.GetRegions();
            }

            int numberBeatsRequired = PlayArea.NUM_BARS_FOR_LOOPS_SAVED;

            int numBeatsInLoop = lr.GetNumBeats();
            int numberLoops = (int)numberBeatsRequired / numBeatsInLoop;
            float remainBeats = numberBeatsRequired - numberLoops * numBeatsInLoop;

            if (remainBeats > 0)
            {
                numberLoops++; //unlike notes just run another lot of effects processing for a few bars, can't be arsed chopping this up if notes have finished before then shouldn't be a problem?
            }

            float loopStartBeat = lr.GetStartBeat();
            float loopEndBeat = lr.GetEndBeat();

            List<ParameterNodeRegion> normalisedRegions = mPNM.GetNormalisedRegionsForRange(loopStartBeat, loopEndBeat);

            //Now loop the above
            float incrementBeats = 0;
            for (int i = 0; i < numberLoops; i++)
            {
                foreach (ParameterNodeRegion r in normalisedRegions)
                {
                    ParameterNodeRegion newRegion = r.GetShiftedCopy(incrementBeats);
                    finalLoopedParamList.Add(newRegion);
                }

                incrementBeats += numBeatsInLoop;
            }

            return finalLoopedParamList;
        }


        public void GatherEffectsMidiInfo(MidiEventCollection midiCollection, MelodyEditorInterface melEdInterf)
        {
            if (mPNM == null)
            {
                /// this happens in clear all at init so leave it for now -> System.Diagnostics.Debug.Assert(false, string.Format("Should this ever happen? "));
                return; 
            }

            bool bLoopedSave = melEdInterf.mState.mbSaveMIDIWithLoop;
            const float BEAT_CONTROL_RESOLUTION = 0.05f;

            List<ParameterNodeRegion> processRegions = mPNM.GetRegions();

            if(bLoopedSave)
            {
                processRegions = GatherEffectsLoopRegions();
            }

            if(processRegions.Count>0)
            {
                float lastBeat = processRegions[processRegions.Count - 1].BeatEndPos;

                float updateBeat = 0f;

                while(updateBeat<lastBeat)
                {
                    foreach (ParameterNodeRegion r in processRegions)
                    {
                        if (r.UpdateGatherMidiEventsPlaying(updateBeat, midiCollection))
                        {
                            break;
                        }
                    }
                   
                    updateBeat += BEAT_CONTROL_RESOLUTION;
                }
            }
        }

        //public bool HACKDRUMSSetFromMidiTrackEvents(MidiFile newFile)
        //{
        //    bool bDOALLTRACKSONTHIS = false;
        //    bool bHACKDRUMLOAD = bDOALLTRACKSONTHIS;
        //    bool bDOOTHERTYPE = true;

        //    if (!bHACKDRUMLOAD)
        //    {
        //        return false;
        //    }
        //    int trackNum = mChannel - 1;
        //    InitNoteLines();
        //    mNumTracks = newFile.Tracks;
        //    MidiEventCollection events = newFile.Events;

        //    SaveLoad.TicksPerQuarterNote = events.DeltaTicksPerQuarterNote;

        //    ResetPanVol(); //may not be set

        //    if (!bDOALLTRACKSONTHIS)
        //    {
        //        if (mNumTracks > 0)
        //        {
        //            //NoteLine nl = new NoteLine(this); //TODO assume file has right beat count

        //            foreach (MidiEvent me in events[1])
        //            {
        //                if (MidiEvent.IsNoteOn(me) || MidiEvent.IsNoteOff(me))
        //                {
        //                    ProcessNoteEvent(me);
        //                }
        //                else if (me.CommandCode == MidiCommandCode.PatchChange)
        //                {
        //                    PatchChangeEvent pc = (PatchChangeEvent)me;
        //                    mInstrument = pc.Patch;
        //                }
        //                else if (me.CommandCode == MidiCommandCode.ControlChange)
        //                {
        //                    ControlChangeEvent cce = (ControlChangeEvent)me;
        //                    if (cce.Controller == MidiController.MainVolume)
        //                    {
        //                        mVolume = cce.ControllerValue;
        //                    }
        //                    else if (cce.Controller == MidiController.Pan)
        //                    {
        //                        mPan = cce.ControllerValue;
        //                    }
        //                }
        //                else if (me.CommandCode == MidiCommandCode.MetaEvent)
        //                {
        //                    MetaEvent metaEv = (MetaEvent)me;
        //                    if (metaEv.MetaEventType == MetaEventType.SetTempo)
        //                    {
        //                        TempoEvent te = (TempoEvent)me;
        //                        int microsecPerquarternote = te.MicrosecondsPerQuarterNote;
        //                        mBPM = 60000000 / microsecPerquarternote;
        //                    }
        //                }
        //            }

        //            if (bDOOTHERTYPE)
        //            {
        //                foreach (MidiEvent me in events[2])
        //                {
        //                    if (MidiEvent.IsNoteOn(me) || MidiEvent.IsNoteOff(me))
        //                    {
        //                        ProcessNoteEvent(me);
        //                    }
        //                    else if (me.CommandCode == MidiCommandCode.PatchChange)
        //                    {
        //                        PatchChangeEvent pc = (PatchChangeEvent)me;
        //                        mInstrument = pc.Patch;
        //                    }
        //                    else if (me.CommandCode == MidiCommandCode.ControlChange)
        //                    {
        //                        ControlChangeEvent cce = (ControlChangeEvent)me;
        //                        if (cce.Controller == MidiController.MainVolume)
        //                        {
        //                            mVolume = cce.ControllerValue;
        //                        }
        //                        else if (cce.Controller == MidiController.Pan)
        //                        {
        //                            mPan = cce.ControllerValue;
        //                        }
        //                    }
        //                    else if (me.CommandCode == MidiCommandCode.MetaEvent)
        //                    {
        //                        MetaEvent metaEv = (MetaEvent)me;
        //                        if (metaEv.MetaEventType == MetaEventType.SetTempo)
        //                        {
        //                            TempoEvent te = (TempoEvent)me;
        //                            int microsecPerquarternote = te.MicrosecondsPerQuarterNote;
        //                            mBPM = 60000000 / microsecPerquarternote;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (mNumTracks > 0)
        //        {
        //            //NoteLine nl = new NoteLine(this); //TODO assume file has right beat count

        //            for (int track = 0; track < mNumTracks; track++)
        //            {
        //                foreach (MidiEvent me in events[track])
        //                {
        //                    if (MidiEvent.IsNoteOn(me) || MidiEvent.IsNoteOff(me))
        //                    {
        //                        ProcessNoteEvent(me);
        //                    }
        //                    else if (me.CommandCode == MidiCommandCode.PatchChange)
        //                    {
        //                        PatchChangeEvent pc = (PatchChangeEvent)me;
        //                        mInstrument = pc.Patch;
        //                    }
        //                    else if (me.CommandCode == MidiCommandCode.ControlChange)
        //                    {
        //                        ControlChangeEvent cce = (ControlChangeEvent)me;
        //                        if (cce.Controller == MidiController.MainVolume)
        //                        {
        //                            mVolume = cce.ControllerValue;
        //                        }
        //                        else if (cce.Controller == MidiController.Pan)
        //                        {
        //                            mPan = cce.ControllerValue;
        //                        }
        //                    }
        //                    else if (me.CommandCode == MidiCommandCode.MetaEvent)
        //                    {
        //                        MetaEvent metaEv = (MetaEvent)me;
        //                        if (metaEv.MetaEventType == MetaEventType.SetTempo)
        //                        {
        //                            TempoEvent te = (TempoEvent)me;
        //                            int microsecPerquarternote = te.MicrosecondsPerQuarterNote;
        //                            mBPM = 60000000 / microsecPerquarternote;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    PostLoadFixUp();

        //    return true;
        //}

        public void LoadFromMidiTrackEvents(MidiFile newFile)
        {
            int numTracks = newFile.Tracks;
            MidiEventCollection events = newFile.Events;

            LoadFromMidiCollection(events, numTracks);
        }


        public void UndoLastChange()
        {
            if(UndoChangeData!=null)
            {
                LoadFromMidiCollection(UndoChangeData, PlayArea.FULL_RANGE, true);
            }
        }

        public void Clear()
        {
            int trackNum = mChannel - 1;
            InitNoteLines();
            mNumTracks = FULL_RANGE;
            bool bFixStrangeTrackOffset = false;
            if (bFixStrangeTrackOffset)
            {
                trackNum += 1;
            }
            ResetPanVol(); //may not be set
            mInstrument = 0;
            PostLoadFixUp();
        }

        public void LoadFromMidiCollection(MidiEventCollection events, int numTracks = FULL_RANGE, bool bFixStrangeTrackOffset = false)
        {
            int trackNum = mChannel - 1;
            InitNoteLines();
            mNumTracks = numTracks;

            trackNum = mNumTracks == 1 ? 0 : trackNum; //If loading to current play area set to the expect single track

            if (bFixStrangeTrackOffset)
            {
                trackNum += 1;
            }

            SaveLoad.TicksPerQuarterNote = events.DeltaTicksPerQuarterNote;

            ResetPanVol(); //may not be set

            if (mNumTracks > 0 && events.Tracks > 0)
            {
                NoteLine nl = new NoteLine(this); //TODO assume file has right beat count

                foreach (MidiEvent me in events[trackNum])
                {
                    if (MidiEvent.IsNoteOn(me) || MidiEvent.IsNoteOff(me))
                    {
                        ProcessNoteEvent(me);
                    }
                    else if (me.CommandCode == MidiCommandCode.PatchChange)
                    {
                        PatchChangeEvent pc = (PatchChangeEvent)me;
                        mInstrument = pc.Patch;
                    }
                    else if (me.CommandCode == MidiCommandCode.ControlChange)
                    {
                        ControlChangeEvent cce = (ControlChangeEvent)me;
                        if (cce.Controller == MidiController.MainVolume)
                        {
                            mVolume = cce.ControllerValue;
                        }
                        else if (cce.Controller == MidiController.Pan)
                        {
                            mPan = cce.ControllerValue;
                        }
                    }
                    else if (me.CommandCode == MidiCommandCode.MetaEvent)
                    {
                        MetaEvent metaEv = (MetaEvent)me;
                        if (metaEv.MetaEventType == MetaEventType.SetTempo)
                        {
                            TempoEvent te = (TempoEvent)me;
                            int microsecPerquarternote = te.MicrosecondsPerQuarterNote;
                            mBPM = 60000000 / microsecPerquarternote;
                        }
                    }
                }
            }
            SetPlayAreaStartDrawNoteLineToToTopActualNotes();
            PostLoadFixUp();
        }

        //public void LoadMidiFile(MidiFile newFile) ///THis is the old format I think not used now
        //{
        //    int outputFileType = newFile.FileFormat;
        //    MidiEventCollection events = new MidiEventCollection(outputFileType, newFile.DeltaTicksPerQuarterNote); //TODO !?
        //    mNumTracks = newFile.Tracks;

        //    InitNoteLines();

        //    if (mNumTracks > 0)
        //    {
        //        events = newFile.Events;
        //        NoteLine nl = new NoteLine(this); //TODO assume file has right beat count

        //        foreach (MidiEvent me in events[0])
        //        {
        //            if (MidiEvent.IsNoteOn(me) || MidiEvent.IsNoteOff(me))
        //            {
        //                ProcessNoteEvent(me);
        //            }
        //            else if (me.CommandCode == MidiCommandCode.PatchChange)
        //            {
        //                PatchChangeEvent pc = (PatchChangeEvent)me;
        //                mInstrument = pc.Patch;
        //            }
        //            else if (me.CommandCode == MidiCommandCode.MetaEvent)
        //            {
        //                MetaEvent metaEv = (MetaEvent)me;
        //                if (metaEv.MetaEventType == MetaEventType.SetTempo)
        //                {
        //                    TempoEvent te = (TempoEvent)me;
        //                    int microsecPerquarternote = te.MicrosecondsPerQuarterNote;
        //                    mBPM = 60000000 / microsecPerquarternote;
        //                }
        //            }
        //        }
        //    }

        //    PostLoadFixUp();
        //}

        NoteLine ProcessNoteEvent(MidiEvent me)
        {
            if (MidiEvent.IsNoteOn(me))
            {
                NoteOnEvent noe = (NoteOnEvent)me;

                int nn = noe.NoteNumber;
                NoteLine nl = GetNlForNum(nn);

                if (nl != null)
                {
                    //absoluteTime = (long)((float)TicksPerQuarterNote * PlayArea.MAX_BEATS);
                    long abst = noe.AbsoluteTime;
                    int velocity = noe.Velocity;
                    float pos = (float)abst / (float)SaveLoad.TicksPerQuarterNote;

                    if (velocity > 0)
                    {
                        HasNotes = true;
                    }

                    if (pos <= MAX_BEATS)
                    {
                        nl.AddStartNote(pos, velocity);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format(" Out of range loaded! NoteNum: {0} pos {1}", nl.NoteNum, pos));
                    }
                }
                else
                {
                    //ERROR - probably
                }
            }
            else if (MidiEvent.IsNoteOff(me))
            {
                NoteEvent noe = (NoteEvent)me;
                int nn = noe.NoteNumber;
                NoteLine nl = GetNlForNum(nn, true);

                if (nl != null)
                {
                    //absoluteTime = (long)((float)TicksPerQuarterNote * PlayArea.MAX_BEATS);
                    long abst = noe.AbsoluteTime;
                    float pos = (float)abst / (float)SaveLoad.TicksPerQuarterNote;

                    if (pos <= MAX_BEATS)
                    {
                        nl.AddEndNote(pos);
                    }
                }
                else
                {
                    //ERROR!
                }
            }

            return null;
        }
    }


}
