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
    public class Note
    {

        public float BeatPos { get; set; }
        public float BeatLength { get; set; }
        public float BeatEndPos { get; set;  }
        public int Chan { get; set; }
        public int mNoteNum;
        public int Velocity { get; set; }

        NoteEvent mNe;
        public Rectangle mRect;
        public Rectangle mMaskRect; //masks over above mRect by an amount to show the level of the velocity of the beat
        public NoteLine mParent;
        bool mNoteOn;
        public bool EditSelected = false;
        bool mInDrawRange = false;
        public AIChordRegion mRegion = null; //is note root for a region?

        Color mDefaultCol = Color.Red;

        public Color Col {set; get;}=Color.Red;
        public void SetDefaultCol()
        {
            Col = mDefaultCol;
        }


        ////////////////////////////////////////////////
        ///// CONSTRUCT INIT
        public Note() //THIS IS FOR TEST NOTES - CREATED ONCE AND SET UP LATER
        {
            Chan = 1;
            BeatPos = 0f;
            BeatLength = 0f;
            BeatEndPos = 0f;
        }
        public Note(float pos, float len)
        {
            Init(0);  //just set up channel

            mNoteOn = false;
            SetPosLen( pos,  len);
        }
        public Note(int noteNum, NoteLine parent)
        {
            mParent = parent; //Before INIT!
            Init(noteNum);  //set channel default 1
        }
        public Note(Note other)
        {
            Chan = other.Chan;
            mNoteNum = other.mNoteNum;
            Velocity = other.Velocity;
            mRect = other.mRect;
            mParent = other.mParent;
            BeatPos = other.BeatPos;
            BeatLength = other.BeatLength;
        }

        void Init(int noteNum)
        {
            Chan = mParent.mParent.mChannel;
            mNoteNum = noteNum;
            Velocity = MidiBase.MID_VELOCITY;
        }

        public bool Playing()
        {
            return mNoteOn;
        }

 
        public String GetName()
        {
            NoteEvent ne = new NoteEvent(0, mParent.GetChannel(), MidiCommandCode.NoteOff, mNoteNum, 0);

            return ne.NoteName;
        }

        public void RemoveFromParentRegionList() //AIChordRegion r)
        {
            mParent.RemoveAIRegionNote(this);
        }

        public void RemoveFromParentLine()
        {
            mParent.RemoveNote(this);
        }

        public void InformRegionNoteGone()
        {
            if(mRegion!=null)
            {
                mRegion.RefreshAfterChange(null, false, true);
            }
        }

        public bool Overlaps(Note testNote)
        {
            if(testNote.mRect.Intersects(mRect) || testNote.mRect.Contains(mRect) )
            {
                return true;
            }

            return false;
        }

        //This checks the positions not rectangles and better for notes on different lines
        public bool OverlapsBeats(Note testNote)
        {
            float testNoteEndpos = testNote.BeatPos + testNote.BeatLength;
            float thisEndPos = BeatLength + BeatPos;

            if(testNoteEndpos > BeatPos && testNoteEndpos < thisEndPos)
            {
                return true;
            }
            if (testNote.BeatPos > BeatPos && testNote.BeatPos < thisEndPos)
            {
                return true;
            }
            if (testNote.BeatPos < BeatPos && testNote.BeatEndPos > thisEndPos)
            {
                return true;
            }

            return false;
        }

        public bool Overlaps(Rectangle testRect)
        {
            if (testRect.Intersects(mRect) || testRect .Contains(mRect) )
            {
                return true;
            }

            return false;
        }

        public void SetPosLen(float pos, float len)
        {
            BeatPos     = pos;
            BeatLength  = len;
            BeatEndPos = pos + len;
            RefreshRectToData();
        }

        public enum NoteLoopedStatus
        {
            NLSNone,
            NLSPosBeforeStart,
            NLSPosBetweenStartAndEnd,
        }
        // For recording, the held note will get its length updated to current playhead position using this function 
        public NoteLoopedStatus AdjustBeatToPos(float pos )
        {
            if(BeatPos > pos)
            {
                return NoteLoopedStatus.NLSPosBeforeStart;
            }

            if(BeatEndPos > pos)
            {
                //Already ahead of the pos so probably looped around?
                return NoteLoopedStatus.NLSPosBetweenStartAndEnd;
            }
            float len = pos - BeatPos; //keep stretching out held note
            SetPosLen(BeatPos, len);

            return NoteLoopedStatus.NLSNone;
        }

        const float INV_127_FLOAT = 1.0f / 127.0f;

        public void RefreshRectToData()
        {
            float fStartPos = mParent.mParent.StartBeat;

            int startY = mParent.mStartY;
            int startX = mParent.mStartX + (int)((BeatPos - fStartPos) * MelodyEditorInterface.BEAT_PIXEL_WIDTH);
            int noteWidth = (int)(BeatLength * MelodyEditorInterface.BEAT_PIXEL_WIDTH);
            int velHeight = (int)((float)NoteLine.TRACK_HEIGHT * (float)Velocity * INV_127_FLOAT);
            //mMaskRect = new Rectangle(startX, startY, noteWidth, NoteLine.TRACK_HEIGHT - velHeight);  //NoteLine.TRACK_HEIGHT);
            //mRect = new Rectangle(startX, startY, noteWidth, NoteLine.TRACK_HEIGHT);  //NoteLine.TRACK_HEIGHT);
            mMaskRect.X = startX; 
            mMaskRect.Y = startY;
            mMaskRect.Width = noteWidth;
            mMaskRect.Height = NoteLine.TRACK_HEIGHT - velHeight;

            mRect.X = startX;
            mRect.Y = startY;
            mRect.Width = noteWidth;
            mRect.Height = NoteLine.TRACK_HEIGHT;

            mInDrawRange = mRect.Intersects(mParent.mParent.mAreaRectangle);
        }

        public bool UpdatePlaying(MelodyEditorInterface.MEIState state, bool bAllowTurnOffPlaying)
        {
            if(mRegion!=null)
            {
                return false; //dont play the root region note could overlap actual region area if stretched out
            }
            //if(> BeatPos && state.fCurrentBeat < BeatPos+ BeatLength)
            if(PlayPosInNote(state.fMainCurrentBeat))
            {
                if(!mNoteOn)
                {
                    //Set note on
                    mNoteOn = true;
                    state.NoteOn(Chan, mNoteNum, Velocity);
                    //mParent.StartNote(this, state);
                    return true;
                }
            }
            else
            {
                if (mNoteOn)
                {
                    //Set note off
                    mNoteOn = false;
                    if(bAllowTurnOffPlaying)
                    {
                        state.NoteOff(Chan, mNoteNum);
                        //mParent.EndNote(this, state);
                    }
                }
            }

            return false;
        }

        public void SetNoteOff(MelodyEditorInterface.MEIState state)
        {
            state.NoteOff(Chan, mNoteNum);
        }

        public bool IsNoteInRange(float start, float end) //TODO IsNoteInRange only excepts notes under loop bar range so wont accept ones starting/ending at the exact start/end
        {
            if(BeatPos>start && BeatEndPos<=end)
            {
                return true;
            }

            return false;
        }

        public bool IsNoteInRangeInclusive(float start, float end) //
        {
            if (BeatPos >= start && BeatEndPos <= end)
            {
                return true;
            }

            return false;
        }

        public bool PlayPosInNote(float playPos)
        {
            bool bOn = false;
            if (playPos >= BeatPos && playPos < (BeatPos + BeatLength) )
            {
                bOn = true;
            }
            return bOn;
        }

        public bool NoteEndsAfter(float playPos)
        {
            bool bOn = false;
            if (playPos > (BeatPos + BeatLength) )
            {
                bOn = true;
            }
            return bOn;
        }

        NoteOnEvent GetOn()
        {
            return new NoteOnEvent(0, Chan, mNoteNum, Velocity, 50);
        }
        //NoteOnEvent GetOff()
        //{
        //    //return new NoteOffEvent(0, Chan, mNoteNum, Velocity, 50);
        //}

        public void Draw(SpriteBatch sb, MusicPreviewer.PreviewLineType previewType = MusicPreviewer.PreviewLineType.None, bool bOverrideNote = false)
        {
            if(mInDrawRange)
            {
                if (previewType != MusicPreviewer.PreviewLineType.None)
                {
                    if (previewType == MusicPreviewer.PreviewLineType.Preview)
                    {
                        Color noteCol = Color.LightGreen;
                        Color masknoteCol = Color.Green;
                        Color rectCol = Color.Black;
                        float rectThickness = 1f;

                        if (mNoteOn)
                        {
                            noteCol = Color.LimeGreen;
                            masknoteCol = Color.DarkGreen;
                            rectCol = Color.Khaki;
                            rectThickness = 3f;
                        }

                        sb.FillRectangle(mRect, noteCol);
                        sb.FillRectangle(mMaskRect, masknoteCol * 0.40f);
                        sb.DrawRectangle(mRect, rectCol, rectThickness);
                    }
                    else if (previewType == MusicPreviewer.PreviewLineType.Record)
                    {
                        sb.FillRectangle(mRect, Color.Wheat);
                        sb.DrawRectangle(mRect, Color.Black);
                    }
                }
                else
                if (bOverrideNote)
                {
                    sb.FillRectangle(mRect, Color.Cyan * 0.20f);
                    sb.DrawRectangle(mRect, Color.Black);
                    sb.FillRectangle(mMaskRect, Color.DarkCyan * 0.40f);
                }
                else
                {
                    float alpha = mRegion != null ? 0.4f : 1f;

                    Color fillCol = mNoteOn ? Color.White : Col;
                    Color maskCol = mNoteOn ? Color.LightGray : Color.DarkRed;

                    sb.FillRectangle(mRect, fillCol * alpha);
                    if (mRegion == null)
                    {
                        sb.FillRectangle(mMaskRect, maskCol * alpha);
                    }


                    if (EditSelected)
                    {
                        sb.DrawRectangle(mRect, Color.Yellow, 6);
                    }
                    else
                    {
                        Color rectCol = Color.Black;
                        int thick = 1;

                        //if (mNoteOn)
                        //{
                        //    thick = 1;
                        //    rectCol = Color.White;
                        //}
                        sb.DrawRectangle(mRect, rectCol, thick);

                        ////So we can see 0 width notes that are not visible when filled
                        //if (mRect.Width == 0)
                        //{
                        //    sb.DrawRectangle(mRect, Color.DarkRed, 6);
                        //}
                    }
                }
            }
        }
    }
}
