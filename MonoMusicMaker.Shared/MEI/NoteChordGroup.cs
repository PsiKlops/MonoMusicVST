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

//This is for when we lay down notes in a group to make a chord
//usually by editing one noteline :  adding, extending or deleting a note on the noteline and other notelines  
//in the chord associated current noteline linked list for the play area will have their notes modified in sympathy with them
//probably an object of this type exists just while laying down note on a line that is in chord type selected
namespace MonoMusicMaker
{
    public class NoteChordGroup
    {
        //For now when we edit/add a note on the master line we will 
        //have parallel notes on other parallel lines
        //added and tracked in these two same size list
        public List<NoteLine> mChordLines = new List<NoteLine>();
        public List<Note> mChordNotes = new List<Note>();

        public NoteLine mMasterLine = null; //which is editing this set
        public Note mMainEditNote = null;


        public NoteChordGroup(Note n)
        {
            if(n!=null)
            {
                if (n.mParent.m_MyLLN!=null)
                {
                    mMainEditNote = n;
                    mMasterLine = n.mParent;

                    //n.mParent.m_MyLLN.Previous.Value
                    if(n.mParent.m_MyLLN.Previous != null)
                    {
                        mChordLines.Add(n.mParent.m_MyLLN.Previous.Value);
                    }

                    if (n.mParent.m_MyLLN.Next != null)
                    {
                        mChordLines.Add(n.mParent.m_MyLLN.Next.Value);
                    }
                }
            }
        }

        public void EndNoteUpdate(NoteLine nl)
        {
            if(nl==mMasterLine)
            {
                System.Diagnostics.Debug.WriteLine(string.Format(" END  NOTE GROUP NOTE!: {0}   ", mMainEditNote.mNoteNum));

                mMasterLine = null;
            }
        }

        public void RemoveNote()
        {
            System.Diagnostics.Debug.Assert(mChordLines.Count == mChordNotes.Count, string.Format("Expect same num notes as lines"));

            int noteIndex = 0;
            foreach (NoteLine nl in mChordLines)
            {
                nl.RemoveNote(mChordNotes[noteIndex]);
                noteIndex++;
            }
        }

        public void SetPosLen(float pos, float len)
        {
            foreach (Note n in mChordNotes)
            {
                if(n!=null)
                {
                    n.SetPosLen(pos, len);
                }
            }
        }
        public void AddNoteIfSpace(float pos, float len)
        {
            foreach (NoteLine nl in mChordLines)
            {
                Note n = nl.AddNoteIfSpace(pos, len);

                mChordNotes.Add(n);
            }
        }

        public bool Update(MelodyEditorInterface.MEIState state)
        {
            if(mMasterLine!=null)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("   NOTE GROUP NOTE!: {0}   ", mMainEditNote.mNoteNum));
            }

            return false;
        }
    }
}
