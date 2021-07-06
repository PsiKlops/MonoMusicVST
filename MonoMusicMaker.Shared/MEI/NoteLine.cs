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

namespace MonoMusicMaker //.MEI
{
    public class NoteLine
    {
        public bool mbDELETE_NOTE_ON_RELEASE = true;
        public const int TRACK_HEIGHT = 40; //Pixel height of the track in the play area
        public const int TRACK_HALF_HEIGHT = TRACK_HEIGHT/2; //Pixel height of the track in the play area

        const int ON_RECT_WIDTH = 800; //It's goinmg to extend back from start of track
        const int ON_RECT_STARTX = 0; //It's goinmg to extend back from start of track

        const float QUANTISED_NOTE_ALIGN = 0.125f;

        public bool mbNoteInPlay = false; // Think this will be mostly needed for chord display when we detect if this note line is on

        public bool mbHighlight = false; //highlight drawing when in a chord progression for the current key

        public bool mbInCurrentScale = true; // when in scale selected mode the track can be hidden and not playable and not drawn - mostly useful for edit manager 
        float mScale = 1f;
        const int mBeatLength = PlayArea.NUM_DRAW_BEATS; //num beats in track
        public int NoteNum { get; set; }
        Rectangle mRect;

        public bool mbOnRectOn = false;
        Rectangle mOnRect; //For when midi keyboard plays to show which not is on
        List<Note> mNotes;
        List<Note> mAIRegionNotes; //notes on this line controlled by ai region(s)
        int mTrackNum;
        public int mStartX;
        public int mStartY;
        public static int mTrackWidth;
        public bool mbSharp = false;
        public string Text { get; set; } = "";
        public SpriteFont mFont;
        public PlayArea mParent;

        Note mCurrentNoteEnter = null;
        public Note mCurrentNoteInPlay = null; //used for music previewer initially but may be useful for pipeline
        public bool Touched { get; set; } = false;
        public Note AddedNote { get; set; } = null;
        public Note RemovedNote { get; set; } = null;

        public LinkedListNode<NoteLine> m_MyLLN = null;

        public Note CurrentLaidNote { get; set; } = null; //when in quick add mode you can extend this note if you slide along

        public NoteLine(PlayArea parent)
        {
            mAIRegionNotes = new List<Note>();
            mParent = parent;
        }

        public NoteLine(NoteLine other)
        {

        }

        public List<Note> GetNotes()
        {
            return mNotes;
        }

        public List<Note> GetRegionNotes()
        {
            return mAIRegionNotes;
        }

        public int GetChannel()
        {
            return mParent.mChannel;
        }

        public static int SortByNoteStart(Note x, Note y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal. 
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    if (x.BeatPos < y.BeatPos)
                    {
                        return -1;
                    }
                    else if (x.BeatPos > y.BeatPos)
                    {
                        return 1;
                    }

                    return 0;
                }
            }
        }

        public List<Note> GetNotesNoLooping()
        {
            List<Note> listNotes = new List<Note>();
            foreach (Note n in mNotes)
            {
                if (n.mRegion == null)
                {
                    listNotes.Add(n);
                }
            }
            foreach (Note n in mAIRegionNotes)
            {
                listNotes.Add(n);
            }

            return listNotes; // no loops just give out the current - TODO we may need to allow cutting down below the passed numberBeatsRequired
        }

        public bool GetNoteRange(ref float refstart, ref float refend)
        {
            refstart = 0f;
            refend = PlayArea.BEATS_PER_BAR * PlayArea.MAX_BAR_RANGE;

            if(mNotes.Count>0)
            {
                refstart = mNotes[0].BeatPos;
                refend = mNotes[mNotes.Count-1].BeatEndPos;
                return true;
            }

            return false;
        }

        public void ResetNotesByFactor(float factor)
        {
            foreach (Note n in mNotes)
            {
                float newStart = n.BeatPos * factor;
                float newEnd = n.BeatEndPos * factor;
                n.SetPosLen(newStart, newEnd - newStart);
            }
        }
        public void MoveForwardByBeats(float beats)
        {
            foreach (Note n in mNotes)
            {
                float newStart = n.BeatPos  + beats;
                float newEnd = n.BeatEndPos + beats;
                n.SetPosLen(newStart, newEnd - newStart);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///// FOR MUSIC PREVIEWER
        ///
        /// when we create NoteLines for holding notes in MusicPreviewer
        /// and we will want to draw those notes then the notes will need
        /// this minumum info to get screen pos when refreshed to screen
        /// 
        public void CopyMinDrawPosInfo(NoteLine other)
        {
            mStartX = other.mStartX;
            mStartY = other.mStartY;
            mParent = other.mParent;
            mTrackNum = other.mTrackNum;
        }

        /// ///////////////////////////////////////////////////////////////////////////////////////////////////
        /// FOR EDITOR 
        /// FOR EDITOR 
        /// FOR EDITOR 
        /// ///////////////////////////////////////////////////////////////////////////////////////////////////
 
        public NoteLine GetLineAbove()
        {
            //return mParent.GetNoteLineAtNoteNumber(NoteNum + 1);
            return mParent.GetNoteLineAboveOrBelowNoteNumInCurrentScale(NoteNum, true);
        }
        public NoteLine GetLineBelow()
        {
            //return mParent.GetNoteLineAtNoteNumber(NoteNum -1);
            return mParent.GetNoteLineAboveOrBelowNoteNumInCurrentScale(NoteNum, false);
        }

        public NoteLine GetAboveOrBelow(bool bAbove)
        {
            if(bAbove)
            {
                return GetLineAbove();
            }

            return GetLineBelow();
        }

        public bool CanNoteFitInLine(Note noteIn, bool bIgnoreEditNotes = false )
        {
            //TODO HANDLE REGION NOTES!
            foreach (Note n in mNotes)
            {
                bool bCheckNote = bIgnoreEditNotes ? !n.EditSelected : true;

                if (bCheckNote && noteIn.OverlapsBeats(n))
                {
                    return false;
                }
            }

            return true;
        }


        public List<Note> GetNotesInRectangle(Rectangle rect, bool bAllTracks = false)
        {
            List<Note> ln = new List<Note>();

            if(bAllTracks)
            {
                rect = new Rectangle(rect.X, mRect.Y, rect.Width, mRect.Width); //if we want all tracks we make sure this track height is covered and just check the start and width
            }
            foreach (Note n in mNotes)
            {
                if (n.Overlaps(rect))
                {
                    ln.Add(n);
                }
            }
            return ln;
        }

        public bool IsNoteLineCoveredByRect(Rectangle rect)
        {
            int lowestY = mRect.Y + mRect.Height / 2;

            if(rect.Y < lowestY)
            {
                return true;
            }

            return false;
        }

        /// ///////////////////////////////////////////////////////////////////////////////////////////////////
        /// FOR EDITOR 
        /// FOR EDITOR 
        /// FOR EDITOR 
        /// ///////////////////////////////////////////////////////////////////////////////////////////////////

        public List<Note> GetNotesLoopedInRegion(float numberBeatsRequired)
        {
            List<Note> baselistNotes = new List<Note>(); //note in first cycle listed will be used to base future cycles with offset
            List<Note> listNotes = new List<Note>();

            PlayArea.LoopRange lr = new PlayArea.LoopRange();
            bool hasLoopRange = mParent.GetSaveBarRange(lr);
            if (lr.EndBar == lr.StartBar)
            {
                //return empty list since no range
                return listNotes;
            }

            if (!hasLoopRange)
            {
                 return GetNotesNoLooping(); // no loops just give out the current - TODO we may need to allow cutting down below the passed numberBeatsRequired
            }

            int numBeatsInLoop = lr.GetNumBeats();
            int numberLoops = (int)numberBeatsRequired / numBeatsInLoop;

            float remainBeats = numberBeatsRequired - numberLoops * numBeatsInLoop;

            List<Note> normalListNotes = GetNotesForRangeFromList(lr.StartBar, lr.EndBar, mNotes);
            baselistNotes.AddRange(normalListNotes);
            List<Note> aiListNotes = GetNotesForRangeFromList(lr.StartBar, lr.EndBar, mAIRegionNotes);
            baselistNotes.AddRange(aiListNotes);

            //Now loop the above
            float incrementBeats = 0;
            for(int i=0; i< numberLoops;i++)
            {
                foreach (Note n in baselistNotes)
                {
                    Note copyNote = new Note(n);
                    float startNote = n.BeatPos + incrementBeats;
                    float endNote   = n.BeatEndPos + incrementBeats;
                    copyNote.SetPosLen(startNote, n.BeatEndPos - n.BeatPos);
                    listNotes.Add(copyNote);
                }
                incrementBeats += numBeatsInLoop;
            }

            if(remainBeats>0)
            {
                foreach (Note n in baselistNotes)
                {
                    Note copyNote = new Note(n);
                    float startNote = n.BeatPos + incrementBeats;
                    float endNote = n.BeatEndPos + incrementBeats;

                    if (startNote > (incrementBeats + remainBeats))
                    {
                        continue; //start of note is above the bar range we have left so skip
                    }
                    if (endNote > (incrementBeats + remainBeats))
                    {
                        endNote = incrementBeats + remainBeats; //end of note is above the bar range we have left so cut it down
                    }
                    copyNote.SetPosLen(startNote, endNote- startNote);
                    listNotes.Add(copyNote);
                }
            }
            return listNotes;
        }

        List<Note> GetThisTrackNotesInRangeInclusive(float start, float end)
        {
            List<Note> outPutNotes = new List<Note>(); //note in first cycle listed will be used to base future cycles with offset

            foreach (Note n in mNotes)
            {
                if (n.IsNoteInRangeInclusive(start, end)) //
                {
                    outPutNotes.Add(n);
                }
            }

            return outPutNotes;
        }

        List<Note> GetNotesForRangeFromList(float start, float end, List<Note> inputList)
        {
            List<Note> baselistNotes = new List<Note>(); //note in first cycle listed will be used to base future cycles with offset

            //Get the notes in the loop range and shift them to start from 0

            float beatStart = start * PlayArea.BEATS_PER_BAR;
            float beatEnd = end * PlayArea.BEATS_PER_BAR;

            float beatWidth = beatEnd - beatStart;
            foreach (Note n in inputList)
            {
                if (n.mRegion == null) //TODO bit of a waste on region notes 
                {
                    if(n.IsNoteInRange(beatStart, beatEnd)) //TODO IsNoteInRange only excepts notes under loop bar range so wont accept ones starting/ending at the exact start/end
                    {
                        Note newNote = new Note(n);
                        float offsetStart   = n.BeatPos - beatStart;
                        float offsetEnd     = n.BeatEndPos - beatStart;
                        newNote.SetPosLen(offsetStart, (offsetEnd - offsetStart));

                        baselistNotes.Add(newNote);
                    }
                    else
                    {
                        float noteStart = -1f;
                        float noteEnd = -1f;
                        if (n.PlayPosInNote(beatStart))
                        {
                            noteStart = 0;
                            if (n.BeatEndPos< beatEnd)
                            {
                                noteEnd = n.BeatEndPos - beatStart;
                            }
                            else
                            {
                                //must be above so set to end
                                noteEnd = beatWidth;
                            }
                        }
                        else if (n.PlayPosInNote(beatEnd))
                        {
                            noteStart = n.BeatPos- beatStart;
                            noteEnd = beatWidth;
                        }

                        if(noteStart!=-1f && noteEnd !=-1f)
                        {
                            Note newNote = new Note(n);
                            newNote.SetPosLen(noteStart, (noteEnd - noteStart));
                            baselistNotes.Add(newNote);
                        }
                    }
                }
            }

            return baselistNotes;
        }

        /// REGION PROCESSING //////////////////
        /// 
        /// 
        public void ClearRegionNotes()
        {
            mAIRegionNotes.Clear();
        }

        public Note GetFirstNoteInRegion(AIChordRegion r)
        {
            //Refresh existing notes
            foreach (Note n in mNotes)
            {
                if(n.BeatPos >= r.BeatPos && n.BeatEndPos<=r.BeatEndPos)
                {
                    return n;
                }
            }

            return null;
        }

        public List<Note> GetAllNoteInRegion(AIChordRegion r)
        {
            List<Note> allNotes = new List<Note>();
            //Refresh existing notes
            foreach (Note n in mNotes)
            {
                if (n.BeatPos >= r.BeatPos && n.BeatEndPos <= r.BeatEndPos)
                {
                    allNotes.Add( n );
                }
            }

            return allNotes;
        }

        public void AddAIRegionNote(Note n)
        {
            //n.mp
            mAIRegionNotes.Add(n);
        }

        public bool RemoveAIRegionNote(Note n)
        {
            return mAIRegionNotes.Remove(n);
        }
        /// 
        /// 
        /// REGION PROCESSING //////////////////

        //Helper mostly for minimap display I think
        public bool NoteInbeatArea(int beatArea)
        {
            return false;
        }

        public void Init(int trackNum, int noteNum, int drawTrackOffset, bool bClearNotes = true)
        {
            mNotesPlaying = new List<Note>();

            mbInCurrentScale = true; //to be safe always set on will be hidden when changing scale mode

            mTrackNum = trackNum;
            NoteNum = noteNum;
            int channel = mParent.mChannel;

            int channelForNoteNames = channel;

            //Seems NAudio returns drum info names for channel 16 for some strange reason!? Bug?
            // https://github.com/naudio/NAudio/blob/e359ca0566e9f9b14fee1ba6e0ec17e4482c7844/NAudio/Midi/NoteEvent.cs
            // if ((Channel == 16) || (Channel == 10))
            // So anyway, since the note event here is purely used to get the note names  
            // then when we see we are channel 16 just set it to 15
            if(channel == 16)
            {
                channelForNoteNames = 15;
            }

            NoteEvent ne = new NoteEvent(0, channelForNoteNames, MidiCommandCode.NoteOff, NoteNum, 0);

            String debugText = String.Format("- {0}", NoteNum );

            Text =  ne.NoteName + debugText;


            int indexSharp = Text.IndexOf('#');
            mbSharp = indexSharp != -1;

            int drawOffset = mTrackNum - drawTrackOffset;

            mStartY = MelodyEditorInterface.TRACK_START_Y + drawOffset * TRACK_HEIGHT;
            mStartX = MelodyEditorInterface.TRACK_START_X;
            mTrackWidth = (int)mBeatLength * (int)MelodyEditorInterface.BEAT_PIXEL_WIDTH;

            mRect = new Rectangle(mStartX, mStartY, mTrackWidth, TRACK_HEIGHT);
            mOnRect = new Rectangle(mStartX, mStartY, mTrackWidth, TRACK_HEIGHT);

            if (bClearNotes)
            {
                mNotes = new List<Note>();
            }
            else
            {
                //Refresh existing notes
                foreach(Note n in mNotes)
                {
                    n.RefreshRectToData();
                }
            }
        }

        public void OffsetNotesWhileSelectXShift(float fOffset)
        {
            foreach (Note n in mNotes)
            {
                n.RefreshRectToData();
            }
        }

        public void RefreshDrawRectForOffset(int offset)
        {
            int drawOffset = mTrackNum - offset;

            float offsetYForSquareCursorAdjust = (float)mTrackNum - mParent.TrackFloatPos;
            int intFloatOffset = (int)(offsetYForSquareCursorAdjust * (float)TRACK_HEIGHT);

            int trackOffsetY = drawOffset * TRACK_HEIGHT;
            if (mParent.TrackFloatPos != 0f)
            {
                trackOffsetY = intFloatOffset;
            }

            mStartY = MelodyEditorInterface.TRACK_START_Y + trackOffsetY;
            mStartX = MelodyEditorInterface.TRACK_START_X;
            mTrackWidth = (int)mBeatLength * (int)MelodyEditorInterface.BEAT_PIXEL_WIDTH;

            mRect = new Rectangle(mStartX, mStartY, mTrackWidth, TRACK_HEIGHT);
            mOnRect = new Rectangle(mStartX, mStartY, mTrackWidth, TRACK_HEIGHT);

            //Refresh existing notes
            foreach (Note n in mNotes)
            {
                n.RefreshRectToData();
            }
            //Refresh ai notes
            foreach (Note n in mAIRegionNotes)
            {
                n.RefreshRectToData();
            }
        }

        public void AddStartNote(float pos, int velocity = MidiBase.MID_VELOCITY)
        {
            if(mNotes==null)
            {
                mNotesPlaying = new List<Note>();
                mNotes = new List<Note>();
            }
            if (mCurrentNoteEnter==null)
            {
                mCurrentNoteEnter = new Note(NoteNum, this);
                mCurrentNoteEnter.BeatPos = pos;
                mCurrentNoteEnter.Velocity = velocity;
            }
            else
            {
                //ERROR!
            }

        }

        List<Note> mNotesPlaying = new List<Note>();

        public void ReStartNote( MelodyEditorInterface.MEIState state)
        {
            foreach (Note n in mNotes)
            {
                if(n.Playing())
                {
                    state.GetMidiBase().NoteOn(mParent.mChannel, NoteNum, n.Velocity);
                    return;
                }
            }
        }

        public void StartNote(Note noteOn, MelodyEditorInterface.MEIState state)
        {
            mNotesPlaying.Add(noteOn);
            state.GetMidiBase().NoteOn(mParent.mChannel, NoteNum, noteOn.Velocity);
        }

        public void EndNote(Note noteOff, MelodyEditorInterface.MEIState state)
        {
            mNotesPlaying.Remove(noteOff);

            if (mNotesPlaying.Count == 0)
            {
                state.GetMidiBase().NoteOff(mParent.mChannel, NoteNum);
            }
        }

        public void AddEndNote(float pos)
        {
            if (mCurrentNoteEnter != null)
            {
                if(pos >= mCurrentNoteEnter.BeatPos)
                {
                    if(mCurrentNoteEnter.Velocity!=0)
                    {   //Dont add the dummy note to keep it playing
                        mCurrentNoteEnter.BeatLength = pos - mCurrentNoteEnter.BeatPos;
                        mCurrentNoteEnter.SetPosLen(mCurrentNoteEnter.BeatPos, mCurrentNoteEnter.BeatLength);
                        mNotes.Add(mCurrentNoteEnter);
                        mNotes.Sort(SortByNoteStart);
                    }
                    mCurrentNoteEnter = null;
                }
                else
                {
                    //ERROR!
                }
            }
            else
            {
                //ERROR!
            }
        }

        public Note AddNote(float pos, float length)
        {
            Note n = new Note(NoteNum, this);
            n.SetPosLen(pos, length);
            mNotes.Add(n);
            mNotes.Sort(SortByNoteStart);

            return n;
        }

        //This is for when not playing music but need to update whether playhead
        //is currentnly inside a note region so we can display the current chord
        public void UpdateHasNoteInPlayHead(MelodyEditorInterface.MEIState state)
        {
            mbNoteInPlay = false;

            foreach (Note n in mNotes)
            {
                if (n.PlayPosInNote(state.fMainCurrentBeat))
                {
                    mbNoteInPlay = true;
                    return;
                }
            }
        }

        public void UpdatePlaying(MelodyEditorInterface.MEIState state)
        {
            bool bNoteSetOnThisUpdate = false;

            mbNoteInPlay = false;
            mCurrentNoteInPlay = null;
            foreach (Note n in mNotes)
            {
                bNoteSetOnThisUpdate |= n.UpdatePlaying(state, !bNoteSetOnThisUpdate);
                mbNoteInPlay |= n.Playing();
                if(mbNoteInPlay)
                {
                    mCurrentNoteInPlay = n;
                    if(state.PreviewPlayerState)
                    {
                        break; //Dont use this in normal play which uses loops since it if there was a last note that went to the end of the loop and the whole space was taken up with notes the last note would never get turned off leaving a gap at the end
                    }
                }
            }

            bNoteSetOnThisUpdate = false;
            foreach (Note n in mAIRegionNotes)
            {
                bNoteSetOnThisUpdate |= n.UpdatePlaying(state, !bNoteSetOnThisUpdate);
            }
        }

        public bool InputUpdate(MelodyEditorInterface.MEIState state, bool bAllowInputPlayArea)
        {
            if (!(state.input.mRectangle.Intersects(mRect) || state.input.mSecondRectangle.Intersects(mRect)) )
            {
                return false;
            }

            if (bAllowInputPlayArea)
            {
                return InputUpdate(state);
            }

            return false;
        }

        public int  GetBeatXPosQuantFromTouch(int touchX, float Quant)
        {
            float actualbeatTouch = GetBeatFromTouch(touchX, true, Quant);

            float correctedTouch = actualbeatTouch - mParent.StartBeat;

            int touchPosX = (int)(correctedTouch * MelodyEditorInterface.BEAT_PIXEL_WIDTH);

            return touchPosX + MelodyEditorInterface.TRACK_START_X;
        }


        public float GetBeatFromTouch(int touchX, bool bQuantizedPlacement, float Quant)
        {
            return GetBeatFromTouchStatic( touchX,  bQuantizedPlacement,  Quant,  mParent.StartBeat);
        }

        public static float GetBeatFromTouchStatic(int touchX, bool bQuantizedPlacement, float Quant, float startBeat)
        {
            int pixelOffsetX = touchX - MelodyEditorInterface.TRACK_START_X;
            float drawPosBeatTouch = (float)pixelOffsetX / MelodyEditorInterface.BEAT_PIXEL_WIDTH;
            float actualbeatTouch = drawPosBeatTouch + startBeat; //correct for draw offset

            if (bQuantizedPlacement)
            {
                float halfQuant = Quant * 0.5f;
                float beatPos = actualbeatTouch;
                float fOffset = beatPos % Quant;

                if (fOffset < halfQuant)
                {
                    beatPos -= fOffset;
                }
                else
                {
                    float diffCorrectUpwards = Quant - fOffset;
                    beatPos += diffCorrectUpwards;
                }

                actualbeatTouch = beatPos;
            }

            return actualbeatTouch;
        }

        bool InputUpdate(MelodyEditorInterface.MEIState state)
        {
            bool bQuantizedPlacement = true; //MAke it all quantised to 1/4 note now was just doing it for the key selection for a while

            //if (state.mbKeyRangePlayAreaMode)
            //{
            //    bQuantizedPlacement = true;
            //}

            AddedNote = null;
            RemovedNote = null;
            Touched = false;
            //if (state.input.LeftDown())
            //{
            //    if (state.input.mRectangle.Intersects(mRect))
            //    {
            //        System.Diagnostics.Debug.WriteLine(string.Format(" DOWN ! Track InputUpdate NoteNum: {0}", NoteNum));

//        int pixelOffsetX = state.input.mRectangle.X - mStartX;
//        float beatTouch = (float)pixelOffsetX / MelodyEditorInterface.BEAT_PIXEL_WIDTH;

//        beatTouch += mParent.StartBeat; //correct for draw offset
//        Note n = AddTouchNote(beatTouch);

//        state.SetDownNote (n); //track whatever was set when down so if a note was held it can get deleted
//        System.Diagnostics.Debug.WriteLine(string.Format(" DOWN ! Track InputUpdate NoteNum: {0} {1}", NoteNum, n==null?"null":"not null"));

//        //Note UI
//        state.mNoteUI.mNoteOver = n;
//        state.mNoteUI.mNoteObstruct = n != null ? Overlaps(n) : null;
//        state.mNoteUI.Update(this, beatTouch, state);

//        //if (state.UpdateHeldNote(n))
//        //{
//        //    RemoveNote(n);
//        //}
//        if (n==null)
//        {
//            return true; //means new note was laid down
//        }
//    }
//}
//else 

#if ANDROID
            //SHOULD ONLY HAPPEN ON ANDROID 
            if(state.input.SecondHeld && ! state.mNoteUI.mbSecondHeldSeen )
            {
                state.mNoteUI.mbSecondHeldSeen = state.input.SecondHeld; //latch on second touch held signal to allow for removing any notes we may have laid down when first touch happened

                //Put back any removed note if needed
                if(state.mNoteUI.mfRevertBeatPos !=-1f)
                {
                    System.Diagnostics.Debug.Assert(state.mNoteUI.mCurrentLaidNote == null, string.Format("mCurrentLaidNote should be NULL!"));
                    System.Diagnostics.Debug.Assert(state.mNoteUI.mfRevertBeatPos != -1f, string.Format("mCurrentLaidNote is NULL!!!"));

                    if (state.mNoteUI.mCurrentLaidNote == null)
                    {
                        Note noteAdded = AddNoteIfSpace(state.mNoteUI.mfRevertBeatPos, state.mNoteUI.mfRevertBeatLength);

                        System.Diagnostics.Debug.Assert(noteAdded != null, string.Format("noteAdded should be added back OK!"));

                        RemovedNote = null; //dont tell the outside world ;)
                    }
                }
                else
                {
                    //Remove any added note if needed
                    if (state.mNoteUI.mCurrentLaidNote != null)
                    {
                        RemoveNote(state.mNoteUI.mCurrentLaidNote);
                        AddedNote = null; //dont tell the outside world ;)
                    }
                }
            }
#endif
            if (state.input.Held)
            {
                bool bThisTrackHasDoubleTapNote = state.mNoteUI.mDoubleTapNote != null && state.mNoteUI.mDoubleTapNote.mNoteNum == NoteNum;
                if (state.input.mRectangle.Intersects(mRect) || bThisTrackHasDoubleTapNote)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format(" HELD - - Track InputUpdate NoteNum: {0}", NoteNum));

                    Note n = TouchInNote(state.input.mRectangle);

                    bool bTapDoubleTapNote = false;
                    if (state.input.LeftDown() ) 
                    {
                        if(!bThisTrackHasDoubleTapNote)
                        {
                            //Dont tap again while we have the edit note bThisTrackHasDoubleTapNote
                            state.SetDownNote(n); //track taps
                            state.mNoteUI.Reset(state);
                        }
                        else
                        {
                            if (state.mNoteUI.mDoubleTapNote == n)
                            {
                                state.mNoteUI.mDoubleTapNoteVelSet = !state.mNoteUI.mDoubleTapNoteVelSet;
                                if (state.mNoteUI.mDoubleTapNoteVelSet)
                                {
                                    state.mNoteUI.mDoubleTapNote.Col = Color.Blue;
                                }
                                else
                                {
                                    state.mNoteUI.mDoubleTapNote.Col = Color.Green;
                                }
                            }
                        }
                    }

                    float noteWidthLaidDown = bQuantizedPlacement ? state.mMeledInterf.QuantiseSetting : 1f;

                    //Note UI
                    int pixelOffsetX = state.input.mRectangle.X - mStartX;
                    float drawPosBeatTouch = (float)pixelOffsetX / MelodyEditorInterface.BEAT_PIXEL_WIDTH;
                    float actualbeatTouch = drawPosBeatTouch + mParent.StartBeat; //correct for draw offset
                    Note possibleNote = new Note(NoteNum, this);

                    if(bQuantizedPlacement)
                    {
                        float Quant = noteWidthLaidDown;
                        float halfQuant = noteWidthLaidDown * 0.5f;
                        float beatPos = actualbeatTouch;
                        float fOffset = beatPos % Quant;

                        if (fOffset < halfQuant)
                        {
                            beatPos -= fOffset;
                        }
                        else
                        {
                            float diffCorrectUpwards = Quant - fOffset;
                            beatPos += diffCorrectUpwards;
                        }

                        actualbeatTouch = beatPos;
                    }

                    //actualbeatTouch = GetBeatFromTouch(state.input.mRectangle, bQuantizedPlacement, noteWidthLaidDown);

                    possibleNote.SetPosLen(actualbeatTouch, noteWidthLaidDown);
                    state.mNoteUI.SetNoteOver(n,state);
                    state.mNoteUI.mbOnDoubleTapNote = (n != null && n == state.mNoteUI.mDoubleTapNote);
                    state.mNoteUI.mNoteObstruct = possibleNote != null ? Overlaps(possibleNote) : null;
                    Touched = true; //mostly useful for turning off preview notes I think
                    bool bAllowAction = state.mNoteUI.Update(this, drawPosBeatTouch, state);

                    if (state.mNoteUI.mDoubleTapNote != null)
                    {
                        //if (state.mNoteUI.mbOnDoubleTapNote)
                        {
                            //Do update stretch or move note here
                            if (UpdateDoubleTapNote(state))
                            {
                                return true;
                            }
                        }
                    }
                    else if (bAllowAction)
                    {
                        bool bExtendCurrentLaidNote = false;

                        if (state.mNoteUI.mCurrentLaidNote != null && state.mNoteUI.mCurrentLaidNote == n)
                        {
                            System.Diagnostics.Debug.WriteLine(string.Format(" CURENT LAID NOTE TOUCHED!: n {0} CurrentLaidNote {1} ", n.mNoteNum, state.mNoteUI.mCurrentLaidNote.mNoteNum));

                            if(bQuantizedPlacement)
                            {
                                bExtendCurrentLaidNote = true;
                            }
                        }

                        if (n != null &&!bExtendCurrentLaidNote)
                        {   
                            ////////////////////////////////////////////////////////////////////
                            //DELETE NOTE
                            if(n.mRegion!=null)
                            {
                                //Don't allow deleting region notes will only get removed when region is removed or new note added
                                return false;
                            }

                            if(!mbDELETE_NOTE_ON_RELEASE)
                            {
                                RemoveNoteEdit(state, n);
                                return true;
                            }
                            return false;
                        }
                        else if(!state.mNoteUI.mbSecondHeldSeen && !(state.mNoteUI.mbRemovingNoteMode && bQuantizedPlacement))
                        {
                            bool bAltered = false;
                            if(bQuantizedPlacement && state.mNoteUI.mCurrentLaidNote != null)
                            {
                                //Extend existing note on this note line
                                //Modify this existing note to see if we can extend it either way in quantised manner
                                //if(state.mNoteUI.IsPosOutSIdeCurrentNote(actualbeatTouch)) //only use this is we want to prevent the note being reduced when holding and going back over any note length we already laid down
                                {
                                    //Adjust note to this position if possible
                                    float newPos = state.mNoteUI.mCurrentLaidNote.BeatPos;
                                    float newLength = state.mNoteUI.mCurrentLaidNote.BeatLength;

                                    if (actualbeatTouch < newPos)
                                    {
                                        float diff = newPos - actualbeatTouch;
                                        newPos = actualbeatTouch;
                                        newLength += diff;
                                    }
                                    else
                                    {
                                        float diff = (actualbeatTouch+ noteWidthLaidDown) - (newPos + newLength);
                                        //must be after so just stretch length
                                        newLength += diff;
                                    }

                                    //Note overlapsNote =Overlaps(state.mNoteUI.mCurrentLaidNote, state.mNoteUI.mCurrentLaidNote);

                                    bAltered = state.mNoteUI.mCurrentLaidNote.BeatPos != newPos || state.mNoteUI.mCurrentLaidNote.BeatLength != newLength;
                                    if (bAltered)
                                    {
                                        state.mNoteUI.mCurrentLaidNote.SetPosLen(newPos, newLength);
                                        if (state.mNCG != null)
                                        {
                                            state.mNCG.SetPosLen(newPos, newLength);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                AddedNote = AddNoteIfSpace(actualbeatTouch, noteWidthLaidDown);
                                state.mNoteUI.SetCurrentLaidNote(AddedNote, state);

                                bAltered = AddedNote != null;

                                if (state.mNCG != null)
                                {
                                    state.mNCG.AddNoteIfSpace(actualbeatTouch, noteWidthLaidDown);
                                }
                            }

                            //TODO SEE RETURNING TRUE HERE IS VERY EXPENSIVE AND SLOW BECAUSE IT CAUSES SQUARE CURSOR UPDATES EVERY FRAME ONLY RETURN TRUE IF NEW NOTE CHANGED/ADDED!
                            return bAltered;
                        }
                    }
                    //if (state.UpdateHeldNote(n))
                    //{
                    //    RemoveNote(n);
                    //    return true;
                    //}
                }
            }
            else if (state.input.LeftUp && state.mNoteUI.mDoubleTapNote != null && state.mNoteUI.mbDoubleTapNotePendingSnap)
            {
                state.mNoteUI.mbDoubleTapNotePendingSnap = false;
                if (state.mMeledInterf.QuantiseSetting!=0f)
                {
                    Note testNote = new Note(NoteNum, this);
                    Note dtn = state.mNoteUI.mDoubleTapNote;
                    float Quant         = state.mMeledInterf.QuantiseSetting;
                    float halfQuant     = state.mMeledInterf.QuantiseSetting*0.5f;
                    float beatPos       = state.mNoteUI.mDoubleTapNote.BeatPos;
                    float fOffset       = beatPos % Quant;

                    if (fOffset < halfQuant)
                    {
                        beatPos -= fOffset;
                    }
                    else
                    {
                        float diffCorrectUpwards = Quant - fOffset;
                        beatPos += diffCorrectUpwards;
                    }

                    if (dtn.BeatPos!=beatPos)
                    {
                        testNote.SetPosLen(beatPos, dtn.BeatLength);
                        List<Note> listIgnore = new List<Note>();
                        listIgnore.Add(state.mNoteUI.mDoubleTapNote);
                        Note overlapsNote = OverlapsBeats(testNote, listIgnore);
                        if(overlapsNote==null)
                        {
                            dtn.SetPosLen(beatPos, dtn.BeatLength);
                            return true;
                        }
                    }
                }
            }
            else if (state.input.LeftUp)
            {
                if(state.mNCG!=null)
                {
                    state.mNCG.EndNoteUpdate(this);
                }
                state.mNoteUI.EndPreview();

                if (state.mNoteUI.mAdjustHeldNoteMode)
                {
                    state.mNoteUI.EndHeldNoteAdjust();
                    state.PlayAreaChanged(true); //think this should show new velocity set above on the current note
                }

                if(mbDELETE_NOTE_ON_RELEASE && state.mbPlacePlayHeadWithRightTouch )
                {
                    if (state.input.mRectangle.Intersects(mRect))
                    {
                        Note n = TouchInNote(state.input.mRectangle);
 
                        if(n !=null && state.mNoteUI.mCurrentLaidNote !=n)
                        {
                            RemoveNoteEdit(state, n);
                            return true;
                        }
                    }
                }

            }

            return false;
        }

        void RemoveNoteEdit(MelodyEditorInterface.MEIState state, Note n)
        {
            state.mNoteUI.mfRevertBeatLength = n.BeatLength;   // if second touch happens put note back to how it was using this info!
            state.mNoteUI.mfRevertBeatPos = n.BeatPos;         // if second touch happens put note back to how it was using this info!

            state.mNoteUI.mbRemovingNoteMode = true;
            RemoveNote(n);
            RemovedNote = n;
            if (state.mNCG != null)
            {
                state.mNCG.RemoveNote();
            }
        }

        bool UpdateDoubleTapNote(MelodyEditorInterface.MEIState state)
        {
            Note dtn = state.mNoteUI.mNoteOver;
            if (state.mNoteUI.mDoubleTapNote != null && state.input.SecondHeld && state.mNoteUI.mDoubleTapNote.mNoteNum == NoteNum)
            {
                float pinchScale = state.input.GetPinchScale();
                float oldBeatLength = state.mNoteUI.mDoubleTapNote.BeatLength;
                float newBeatLength = pinchScale * state.mNoteUI.mNoteBeatLengthStart;

                System.Diagnostics.Debug.WriteLine(string.Format(" UpdateDoubleTapNote: pinchScale {0} oldBeatLength {1} oldBeatLength {2}", pinchScale, oldBeatLength, oldBeatLength));

                Note testNote = new Note(NoteNum, this);

                if (newBeatLength < PlayArea.MIN_BEAT_LENGTH)
                {
                    newBeatLength = PlayArea.MIN_BEAT_LENGTH;
                }

                if ((state.mNoteUI.mDoubleTapNote.BeatPos + newBeatLength) > PlayArea.MAX_BEATS)
                {
                    newBeatLength = PlayArea.MAX_BEATS - state.mNoteUI.mDoubleTapNote.BeatPos;
                }
                testNote.SetPosLen(state.mNoteUI.mDoubleTapNote.BeatPos, newBeatLength);
                List<Note> listIgnore = new List<Note>();
                listIgnore.Add(state.mNoteUI.mDoubleTapNote);
                Note overlapsNote = OverlapsBeats(testNote, listIgnore);

                if (overlapsNote != null)
                {
                    newBeatLength = overlapsNote.BeatPos - state.mNoteUI.mDoubleTapNote.BeatPos;
                }

                if (newBeatLength != oldBeatLength)
                {
                    if(state.mNoteUI.mDoubleTapNote.mRegion !=null)
                    {
                        List<Note> ln = state.mNoteUI.mDoubleTapNote.mRegion.GetFirstIntersectionNotes();

                        if(ln.Count!=0)
                        {
                            List<float> newLen = new List<float>();
                            List<float> diffAdjustBackList = new List<float>();

                            float diffLen = newBeatLength - oldBeatLength;
                            foreach(Note ain in ln)
                            {
                                NoteLine aiNl = ain.mParent;
                                Note aitestNote = new Note(NoteNum, aiNl);
                                float ainewBeatLength = ain.BeatLength + diffLen;
                                float aiBeatPos = ain.BeatPos;
                                if (ainewBeatLength < PlayArea.MIN_BEAT_LENGTH)
                                {
                                    ainewBeatLength = PlayArea.MIN_BEAT_LENGTH;
                                }
                                if ((aiBeatPos + ainewBeatLength) > PlayArea.MAX_BEATS)
                                {
                                    ainewBeatLength = PlayArea.MAX_BEATS - aiBeatPos;
                                }
                                aitestNote.SetPosLen(aiBeatPos, ainewBeatLength);

                                listIgnore = new List<Note>();
                                listIgnore.Add(ain);
                                listIgnore.Add(state.mNoteUI.mDoubleTapNote);

                                Note aioverlapsNote = aiNl.OverlapsBeats(aitestNote, listIgnore, true);
                                float diffAdjustBack = 0f;
                                if (aioverlapsNote != null)
                                {
                                    //ainewBeatLength = aioverlapsNote.BeatPos - aiBeatPos;
                                    diffAdjustBack = aitestNote.BeatEndPos - aioverlapsNote.BeatPos;
                                }

                                diffAdjustBackList.Add(diffAdjustBack);
                                newLen.Add(ainewBeatLength);
                                //ain.SetPosLen(aiBeatPos, ainewBeatLength);
                            }

                            float largestBackCorrection = 0f;
                            int count = 0;
                            foreach (Note ain in ln)
                            {
                                if(diffAdjustBackList[count]> largestBackCorrection)
                                {
                                    largestBackCorrection = diffAdjustBackList[count];
                                }
                                count++;
                            }

                            count = 0;
                            foreach (Note ain in ln)
                            {
                                float newLength = newLen[count] - largestBackCorrection;
                                ain.SetPosLen(ain.BeatPos, newLength);
                                count++;
                            }

                            state.mNoteUI.mDoubleTapNote.mRegion.UpdateExtendEndPos(newLen[0]); //pass in new len to get extended
                            state.mNoteUI.mDoubleTapNote.SetPosLen(state.mNoteUI.mDoubleTapNote.BeatPos, newBeatLength - largestBackCorrection);
                        }
                    }
                    else
                    {
                        state.mNoteUI.mDoubleTapNote.SetPosLen(state.mNoteUI.mDoubleTapNote.BeatPos, newBeatLength);
                    }
                    return true;
                }
            }
            else
            if (state.mNoteUI.mbOnDoubleTapNote && dtn != null)
            {
                if (Math.Abs(state.input.mDeltaX) > 0 && dtn.mRegion == null) //don't slide notes associated with a region - they can be slid though
                {
                    float deltaBeatPos = (float)state.input.mDeltaX / MelodyEditorInterface.BEAT_PIXEL_WIDTH;
                    float oldStartPos = dtn.BeatPos;
                    float newBeatPos = oldStartPos + deltaBeatPos;

                    //Dont allow sliding off left below zero! Gives "not sorted" fail when saving midi too
                    if(newBeatPos<0f)
                    {
                        newBeatPos = 0f;
                    }
                    //Note testNote = new Note(dtn);
                    Note testNote = new Note(NoteNum, this);
                    testNote.SetPosLen(newBeatPos, dtn.BeatLength);
                    Note overlapsNote = Overlaps(testNote, dtn);

                    if (overlapsNote != null)
                    {
                        if (state.input.mDeltaX > 0)
                        {
                            float newStartBeatPos = overlapsNote.BeatPos - dtn.BeatLength;
                            dtn.SetPosLen(newStartBeatPos, dtn.BeatLength);
                        }
                        else
                        {
                            float newStartBeatPos = overlapsNote.BeatPos + overlapsNote.BeatLength;
                            dtn.SetPosLen(newStartBeatPos, dtn.BeatLength);
                        }
                    }
                    else
                    {
                        dtn.SetPosLen(newBeatPos, dtn.BeatLength);
                    }

                    if (oldStartPos != newBeatPos)
                    {
                        state.mNoteUI.mbDoubleTapNotePendingSnap = true;
                        return true;
                    }

                    //int startPos = dtn.mRect.X;
                    //if (dtn.mRect.Left + state.input.mDeltaX < mAreaRectangle.Left)
                    //{
                    //    dtn.mRect.X = mAreaRectangle.X;
                    //}
                    //else if (dtn.mRect.Right + state.input.mDeltaX > dtn.mRect.Right)
                    //{
                    //    dtn.mRect.X = dtn.mRect.Right - dtn.mRect.Width;
                    //}
                    //else
                    //{
                    //    dtn.mRect.X += state.input.mDeltaX;
                    //}
                    //if (dtn.mRect.X != startPos)
                    //{
                    //    int beatPixDiff = dtn.mRect.X - mAreaRectangle.X;
                    //    float floatBeatDiff = BEAT_CHUNKS * beatPixDiff;
                    //    state.mMeledInterf.SetStartBeatPos(floatBeatDiff);
                    //}
                }
            }

            return false;
        }

        public void RemoveNote(Note rn)
        {
            mNotes.Remove(rn);
        }

        //remove any notes on this line within the start-end range
        //useful for when loaded midi file that has loaded back in notes here
        //that are going to be re-set up by region ai
        public void RemoveNotesInRegion(float start, float end)
        {
            List<Note> noteToRemove = new List<Note>();
            foreach (Note n in mNotes)
            {
                if(n.BeatPos>=start && n.BeatPos <= end)
                {
                    if(n.mRegion==null)
                    {
                        noteToRemove.Add(n);
                    }
                }
            }

            foreach (Note n in noteToRemove)
            {
                mNotes.Remove(n);
            }
        }

        Note testpossibleNote = new Note();
        public Note IsNoteHere(float pos, float width=0.25f)
        {
            testpossibleNote.mParent = this;
            //Note testpossibleNote = new Note(NoteNum, this);
            testpossibleNote.SetPosLen(pos, width);

            return  Overlaps(testpossibleNote);
        }

        public bool QuarterAIRegionNoteHere(float pos)
        {
            testpossibleNote.mParent = this;
            //Note possibleNote = new Note(NoteNum, this);
            testpossibleNote.SetPosLen(pos, 0.25f);

            if (OverlapsAIRegionNote(testpossibleNote) != null)
            {
                return true;
            }
            return false;
        }

        public Note TouchInNote(Rectangle touchRect, Note ignoreNote=null)
        {
            foreach (Note n in mNotes)
            {
                if (ignoreNote!= n && n.Overlaps(touchRect))
                {
                    return n;
                }
            }
            return null;
        }

        Note OverlapsBeats(Note possibleNote, List<Note> ignoreNote = null, bool bCheckRegion = false)
        {
            foreach (Note n in mNotes)
            {
                bool bIgnoreContinue = false;
                if(ignoreNote!=null)
                {
                    foreach (Note iN in ignoreNote)
                    {
                        if (iN == n)
                        {
                            bIgnoreContinue = true;
                            continue;
                        }
                    }
                }
                if(bIgnoreContinue)
                {
                    continue;
                }
                if (n.OverlapsBeats(possibleNote))
                {
                     return n;
                }
            }

            if(bCheckRegion)
            {
                foreach (Note n in mAIRegionNotes)
                {
                    bool bIgnoreContinue = false;
                    if (ignoreNote != null)
                    {
                        foreach (Note iN in ignoreNote)
                        {
                            if (iN == n)
                            {
                                bIgnoreContinue = true;
                                continue;
                            }
                        }
                    }
                    if (bIgnoreContinue)
                    {
                        continue;
                    }
                    if (n.OverlapsBeats(possibleNote))
                    {
                        return n;
                    }
                }
            }

            return null;
        }

        Note OverlapsAIRegionNote(Note possibleNote, Note ignoreNote = null)
        {
            foreach (Note n in mAIRegionNotes)
            {
                if (n.Overlaps(possibleNote))
                {
                    if (ignoreNote != null && ignoreNote == n)
                    {
                        continue;
                    }
                    return n;
                }
            }

            return null;
        }

        public Note AddNoteIfSpace(float pos, float length = 1f)
        {
            Note possibleNote = new Note(NoteNum, this);
            possibleNote.SetPosLen(pos, length);

            Note overlapNote = Overlaps(possibleNote);

            if (overlapNote == null)
            {
                return InsertNote(possibleNote);
            }

            return null;
        }

        Note Overlaps(Note possibleNote, Note ignoreNote = null, bool bCheckRegion = false)
        {
            Note returnNote = null;

            foreach (Note n in mNotes)
            {
                if (n.Overlaps(possibleNote))
                {
                    if(ignoreNote!=null && ignoreNote==n)
                    {
                        continue;
                    }
                    return n;
                }
            }
            if(bCheckRegion)
            {
                returnNote = OverlapsAIRegionNote(possibleNote, ignoreNote);
            }
            return returnNote;
        }

        public Note InsertNote(Note note) //TODO exposed so we can add 
        {
            mNotes.Add(note);
            mNotes.Sort(SortByNoteStart);
            return note;
        }

        public void Draw(SpriteBatch sb, MusicPreviewer.PreviewLineType previewType = MusicPreviewer.PreviewLineType.None )
        {
            if(previewType== MusicPreviewer.PreviewLineType.None)
            {
                if(mbOnRectOn)
                {
                    sb.FillRectangle(mOnRect,  Color.Green);
                }
                sb.FillRectangle(mRect, (mbSharp ? Color.Gray : Color.Beige) * 0.4f);
                if (mbHighlight)
                {
                    sb.FillRectangle(mRect, Color.Green * 0.3f);
                }
                sb.DrawRectangle(mRect, Color.Black);
            }

            foreach (Note n in mNotes)
            {
                n.Draw(sb, previewType, false);
            }

            if (previewType == MusicPreviewer.PreviewLineType.None)
            {
                foreach (Note n in mAIRegionNotes)
                {
                    n.Draw(sb, MusicPreviewer.PreviewLineType.None, true);
                }
                if (mFont != null)
                {
                    sb.DrawString(mFont, Text, new Vector2(mStartX, mStartY), Color.Blue*0.7f);
                }
            }
        }
    }
}
