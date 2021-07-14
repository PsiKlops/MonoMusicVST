using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using NAudio.Midi;

namespace MonoMusicMaker
{
    public class EditManager
    {

        public bool mbKeyRangePlayAreaMode = false;
        public const float SCALE_SCREEN = 1f;
        public const int BOX_ART_WIDTH = 400;
        const int BUTTON_START_X = (int)(30f * SCALE_SCREEN);
        const int BUTTON_SPACE = (int)(250f * SCALE_SCREEN);
        const int BUTTON_HEIGHT_GAP = (int)(130f * SCALE_SCREEN);
        const int PLAY_BUTTON_XPOS = (int)(BUTTON_START_X);
        const int SECOND_BUTTON_XPOS = (int)(PLAY_BUTTON_XPOS + BUTTON_SPACE);
        const int THIRD_BUTTON_XPOS = (int)(SECOND_BUTTON_XPOS + BUTTON_SPACE);
        const int FOURTH_BUTTON_XPOS = (int)(THIRD_BUTTON_XPOS + BUTTON_SPACE);
        const int FIFTH_BUTTON_XPOS = (int)(FOURTH_BUTTON_XPOS + BUTTON_SPACE);
        const int SIXTH_BUTTON_XPOS = (int)(FIFTH_BUTTON_XPOS + BUTTON_SPACE);
        const int SEVENTH_BUTTON_XPOS = (int)(SIXTH_BUTTON_XPOS + BUTTON_SPACE);

        const int TOP_BUTTON_YPOS = (int)(20f * SCALE_SCREEN);
        const int SECOND_BUTTON_YPOS = (int)(TOP_BUTTON_YPOS + BUTTON_HEIGHT_GAP);
        const int THIRD_BUTTON_YPOS = (int)(SECOND_BUTTON_YPOS + BUTTON_HEIGHT_GAP);
        const int FOURTH_BUTTON_YPOS = (int)(THIRD_BUTTON_YPOS + BUTTON_HEIGHT_GAP);
        const int BOTTOM_BUTTON_YPOS = (int)(880f * SCALE_SCREEN);



        public class CopyNote
        {
            public static bool mbAllNotesSelectedTopToBottom = false;
            public static bool mbCopiedNotesSelectedTopToBottom = false;
            public static bool Active = false;
            public static float Quant = 0.25f;
            public static float QuantTopRegion = 1f;
            public static bool mbQuantize = true;
            public static List<CopyNote> msCopyNotes = new List<CopyNote>();

            public static Rectangle mRectPasteArea = new Rectangle(0, 0, WIDTH, HEIGHT); //rectangle shows width of past whole range notes
            static float mBeatLengthAllNotes = 0f;
            public static float mRectStartDiffBeat = 0f;

            public static int TopTrackPos = -1;

            public static int msNumLines = -1;
            public int Velocity = -1;
            public int NoteNum = -1;
            public int TrackOffset = -1; //Top track is 0 and so on down
            public float BeatPos = -1f;
            public float BeatLength = -1f;
            public Rectangle mRect = new Rectangle();

            public bool mbCanPlaceHere = true;
            public bool mbCanDraw = false; //may not be visible if no tracks to lay over

            public static List<CopyNote> Reset()
            {
                TopTrackPos = 0;
                msCopyNotes = new List<CopyNote>();

                return msCopyNotes;
            }

            public static List<CopyNote> GetCopyNotes()
            {
                return msCopyNotes;
            }

            void SetRectToTrackPos(NoteLine nl, float beatPosOffset)
            {
                mbCanDraw = true;
                mRect.Y = nl.mStartY;
                float actualBeatPos = BeatPos + beatPosOffset- nl.mParent.StartBeat;
                mRect.X = (int)(actualBeatPos * MelodyEditorInterface.BEAT_PIXEL_WIDTH)+ nl.mParent.mAreaRectangle.X;
            }

            public static void AddNewCopyNote(Note n)
            {
                System.Diagnostics.Debug.Assert(TopTrackPos != -1, string.Format("TopTrackPos HASN'T BEEN INITIALISED!!!"));

                CopyNote newCopyNote = new CopyNote();
                newCopyNote.mbCanDraw = true; //if we can add a note it must be drawable

                newCopyNote.mRect = new Rectangle(n.mRect.X, n.mRect.Y, n.mRect.Width, n.mRect.Height);

                newCopyNote.NoteNum = n.mParent.NoteNum;
                newCopyNote.Velocity = n.Velocity;
                newCopyNote.BeatPos = n.BeatPos;
                newCopyNote.BeatLength = n.BeatLength;

                msCopyNotes.Add(newCopyNote);

            }
            public static void RefreshWholeRangeCopyNotes(PlayArea pa, Rectangle rect)
            {
                if (mbCopiedNotesSelectedTopToBottom)
                {
                    if (mbCopiedNotesSelectedTopToBottom)
                    {
                        StraightCopyNotesToPa(pa, rect, false);
                    }
                }
            }

            public static void Paste(PlayArea pa, Rectangle rect)
            {
                ProcessCopyNotesToPa( pa,  rect,true);
            }

            static void StraightCopyNotesToPa(PlayArea pa, Rectangle rect, bool bPaste)
            {
                List<NoteLine> nls = pa.GetNoteLines();

                float notePosTouch = nls[0].GetBeatFromTouch(rect.X, mbQuantize, Quant); //just using the given note line funcion as a helper here - the beat pos value is used for all subsequent lines not just ths one  

                float earliestStartBeat = 10000000f;
                float latestEndBeat = 0f;

                int renderedScaleIndex = 0;

                for (int i = 0; i < nls.Count; i++)
                {
                    NoteLine panl = nls[i];

                    foreach (CopyNote cn in msCopyNotes)
                    {
                        if (cn.NoteNum == panl.NoteNum)
                        {
                            cn.mbCanDraw = false;
                            float notePos = notePosTouch + cn.BeatPos;
                            float endNote = notePos + cn.BeatLength;

                            if(notePos< earliestStartBeat)
                            {
                                earliestStartBeat = notePos;
                            }

                            if (endNote > latestEndBeat)
                            {
                                latestEndBeat = endNote;
                            }

                            if (bPaste)
                            {
                                Note n = panl.AddNoteIfSpace(notePos, cn.BeatLength);
                                if (n != null)
                                {
                                    n.Velocity = cn.Velocity;
                                }
                            }
                            else
                            {
                                if(panl.mbInCurrentScale && pa.CanLineIndexBeDrawn(renderedScaleIndex))
                                {
                                    cn.SetRectToTrackPos(panl, notePosTouch);
                                }

                                Note possNote = panl.TouchInNote(cn.mRect);

                                if (possNote != null)
                                {
                                    cn.mbCanPlaceHere = false;
                                }
                                else
                                {
                                    cn.mbCanPlaceHere = true;
                                }
                            }
                        }
                    }
                    if (panl.mbInCurrentScale)
                    {
                        renderedScaleIndex++;
                    }
                }

                mBeatLengthAllNotes = latestEndBeat > 0f ?  latestEndBeat - earliestStartBeat : 0f;
                mRectPasteArea.Width = (int)(mBeatLengthAllNotes * MelodyEditorInterface.BEAT_PIXEL_WIDTH);
            }

            static void ProcessCopyNotesToPa(PlayArea pa, Rectangle rect, bool bPaste)
            {
                if (!bPaste)
                {
                    foreach (CopyNote cn in msCopyNotes)
                    {
                        cn.mbCanDraw = false; //if not being pasted then being set up for rectangle position so set off in case we dont get a track to set the position on 
                    }
                }

                NoteLine nl = pa.GetTopNoteLineInRect(rect);
                if (mbCopiedNotesSelectedTopToBottom)
                {
                    StraightCopyNotesToPa(pa, rect, bPaste);
                    return;
                }

                if (nl != null)
                {
                    float notePosTouch = nl.GetBeatFromTouch(rect.X, mbQuantize, Quant); //just using the given note line funcion as a helper here - the beat pos value is used for all subsequent lines not just ths one  

                    List<NoteLine> lines = pa.GetRenderedNoteLines();

                    int offsetToTheSelectedTrack = -1;

                    for (int i = 0; i < lines.Count; i++)
                    {
                        NoteLine panl = lines[i];

                        if (offsetToTheSelectedTrack < 0)
                        {
                            if (panl == nl)
                            {
                                offsetToTheSelectedTrack = i;
                            }
                        }

                        if (offsetToTheSelectedTrack >= 0)
                        {
                            //We've found the first track so start adding paste notes at the appropriate lines
                            foreach (CopyNote cn in msCopyNotes)
                            {
                                if (cn.TrackOffset == (i - offsetToTheSelectedTrack))
                                {
                                    float notePos = notePosTouch + cn.BeatPos;
                                    if(bPaste)
                                    {
                                        Note n = panl.AddNoteIfSpace(notePos, cn.BeatLength);
                                        if(n!=null)
                                        {
                                            n.Velocity = cn.Velocity;
                                        }
                                    }
                                    else
                                    {
                                        cn.SetRectToTrackPos(panl, notePosTouch);

                                        Note possNote = panl.TouchInNote(cn.mRect);

                                        if(possNote!=null)
                                        {
                                            cn.mbCanPlaceHere = false;
                                        }
                                        else
                                        {
                                            cn.mbCanPlaceHere = true;
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }

            public static void PostCopyUpdate(PlayArea pa, Rectangle rect)
            {         
                float leftMostNotePos = 100000000f; //start big num
                int topMostTrack = -1;

                List<NoteLine> lines = pa.GetRenderedNoteLines();

                int firstLine = -1;
                int lastLine = -1;

                for (int i = 0; i < lines.Count; i++)
                {
                    foreach (CopyNote cn in msCopyNotes)
                    {
                        if(cn.NoteNum == lines[i].NoteNum)
                        {
                            if (firstLine == -1)
                            {
                                firstLine = i;
                            }
                            if (i > lastLine)
                            {
                                lastLine = i;
                            }
                            cn.TrackOffset = i; //now set to count down actual lines on screen
                        }
                    }
                }

                msNumLines = lastLine - firstLine;

                foreach (CopyNote cn in msCopyNotes)
                {
                   if (cn.BeatPos < leftMostNotePos)
                    {
                        leftMostNotePos = cn.BeatPos;
                    }
                }

                mRectStartDiffBeat = 0f;
                if (CopyNote.mbAllNotesSelectedTopToBottom)
                {
                    float rectStartPos = NoteLine.GetBeatFromTouchStatic(rect.X, true, QuantTopRegion, pa.StartBeat);

                    mRectStartDiffBeat = leftMostNotePos - rectStartPos;
                }

                //Now fix up relative offsets
                foreach (CopyNote cn in msCopyNotes)
                {
                    cn.TrackOffset -= firstLine;
                    cn.BeatPos -= leftMostNotePos;
                    cn.BeatPos += mRectStartDiffBeat;
                 }

                System.Diagnostics.Debug.WriteLine(string.Format("Num notes {0} leftMostNotePos {1} topMostTrack {2}, msNumLines {3}", msCopyNotes.Count, leftMostNotePos, topMostTrack, msNumLines));

            }

            public static void RecalcCopyNotes(PlayArea pa, Rectangle rect)
            {
                mbCopiedNotesSelectedTopToBottom = mbAllNotesSelectedTopToBottom;
                ProcessCopyNotesToPa(pa, rect, false);
            }

            public static void DrawPlayArea(SpriteBatch sb)
            {
                if (!Active)
                {
                    return;
                }

            }

            public static void Draw(SpriteBatch sb)
            {
                if(!Active)
                {
                    return;
                }
 
                if (msCopyNotes.Count > 0)
                {
                    bool bAnyBlocked = false;
                    foreach (CopyNote cn in msCopyNotes)
                    {
                        if (cn.mbCanDraw)
                        {
                            Color colDraw = Color.Aquamarine;
                            if (!cn.mbCanPlaceHere)
                            {
                                bAnyBlocked = true; //I know, a bit ai-ish for a draw function
                                colDraw = Color.Bisque;
                            }
                            //if (!mbCopiedNotesSelectedTopToBottom)
                            {
                                sb.DrawRectangle(cn.mRect, colDraw, 6); //
                            }
                        }
                    }

                    Color colDrawRect = bAnyBlocked? Color.Bisque:Color.Aquamarine;          

                    if (mbCopiedNotesSelectedTopToBottom)
                    {
                        if (mRectPasteArea != null)
                        {
                            sb.FillRectangle(mRectPasteArea, colDrawRect * 0.3f);
                            sb.DrawRectangle(mRectPasteArea, colDrawRect, 8);
                        }
                    }

                 }
            }

        }

        const float BEAT_MOVE_ALONG_TRACK = 0.25f; //temp - have quantise?

        //const int DIR_BUTTON_REGION_XPOS = MelodyEditorInterface.TRACK_START_X+NoteLine.mTrackWidth;
        const int DIR_BUTTON_REGION_YPOS = 0;
        const int BUTTON_GAP = 20;
        //const int UP_DOWN_NOTE_X = XPOS + WIDTH - BUTTON_GAP - Button.mTickBoxSize;
        //const int START_STOP_NOTE_X = XPOS + WIDTH - 2 * (BUTTON_GAP + Button.mTickBoxSize);
        const int UP_NOTE_Y = BUTTON_GAP;
        const int LEFT_RIGHT_NOTE_Y = UP_NOTE_Y + BUTTON_GAP + Button.mTickBoxSize;
        const int DOWN_NOTE_Y = UP_NOTE_Y+(BUTTON_GAP + Button.mTickBoxSize) * 2;

        public bool Active { get; set; } = false;
        const int WIDTH = 10;
        const int HEIGHT = 10;
        Rectangle mRectSelect = new Rectangle(0, 0, WIDTH, HEIGHT);
        SpriteFont mFont;
        Rectangle mRightAreaMask;

        Button mUpNote;
        Button mDownNote;
        Button mShiftRight;
        Button mShiftLeft;

        Button mJoin;
        Button mDelete;
        Button mUndo;
        Button mCopy;
        Button mPaste;
        bool mbUndoAvailable = false;

        bool mbRectChangeWaitForLeftUp = false; //when the area rectangle changes set this true and when the left button up and this true select notes in area and clear this

        List<Button> mDirButtons = new List<Button>();
        List<Button> mControlButtons = new List<Button>();

        List<Note> mSelectedNotes = new List<Note>();
        Note mAnchorNote = null; //Used for when all mSelectedNotes are moved based on the selectd mAnchorNote being shifted. If it's moved up a line you need to switch the note for the note on the next line, when slid left right it is the same note
        //List<CopyNote> mCopiedNotes = new List<CopyNote>();


        //DRUM REGION COPY
        List<AIDrumRegion> mDrumRegionsSelected;
        List<AIDrumRegion> mDrumRegionsCopied = new List<AIDrumRegion>();

        public void InitKeyScaleMode(MelodyEditorInterface melEdInterf)
        {
            PlayArea pa = melEdInterf.GetCurrentPlayArea();
            mbKeyRangePlayAreaMode = true;

            /////////// other buttons
            ///
            mCopy = new Button(new Vector2(MainMusicGame.FOURTH_BUTTON_XPOS, MainMusicGame.TOP_BUTTON_YPOS), "PlainWhite"); ;
            mCopy.ButtonText = "Copy";
            mCopy.ButtonTextColour = Color.Black;
            mCopy.mMask = MelodyEditorInterface.UIButtonMask.UIBM_EditControl;
            mCopy.ClickCB = Copy;
            mCopy.mType = Button.Type.Bar;

            mPaste = new Button(new Vector2(MainMusicGame.FIFTH_BUTTON_XPOS, MainMusicGame.TOP_BUTTON_YPOS), "PlainWhite"); ;
            mPaste.ButtonText = "Paste";
            mPaste.ButtonTextColour = Color.Black;
            mPaste.mMask = MelodyEditorInterface.UIButtonMask.UIBM_EditControl;
            mPaste.ClickCB = Paste;
            mPaste.mType = Button.Type.Bar;

            mDelete = new Button(new Vector2(MainMusicGame.FOURTH_BUTTON_XPOS, MainMusicGame.TOP_BUTTON_YPOS + Button.mBarHeight + MainMusicGame.BAR_BUTTON_PAIR_Y_GAP), "PlainWhite"); ;
            mDelete.ButtonText = "Delete";
            mDelete.ButtonTextColour = Color.Black;
            mDelete.mMask = MelodyEditorInterface.UIButtonMask.UIBM_EditControl;
            mDelete.ClickCB = Delete;
            mDelete.mType = Button.Type.Bar;

            mUndo = new Button(new Vector2(MainMusicGame.FIFTH_BUTTON_XPOS, MainMusicGame.TOP_BUTTON_YPOS + Button.mBarHeight + MainMusicGame.BAR_BUTTON_PAIR_Y_GAP), "PlainWhite"); ;
            mUndo.ButtonText = "Undo";
            mUndo.ButtonTextColour = Color.Black;
            mUndo.mMask = MelodyEditorInterface.UIButtonMask.UIBM_EditControl;
            mUndo.ClickCB = Undo;
            mUndo.mType = Button.Type.Bar;

            mControlButtons.Add(mCopy);
            mControlButtons.Add(mPaste);
            mControlButtons.Add(mUndo);
            mControlButtons.Add(mDelete);
        }

        public void Init(MelodyEditorInterface melEdInterf)
        {
            PlayArea pa = melEdInterf.GetCurrentPlayArea();

            mRightAreaMask = pa.mRightAreaMask;
            mRightAreaMask.Height = SquareCursor.SC_YPOS - SquareCursor.HEIGHT_RANGE_RECT;
            mRightAreaMask.Y = 0;

            int buttonLeftX = mRightAreaMask.Left +  BUTTON_GAP;
            int buttonUpDownX = buttonLeftX + Button.mTickBoxSize;
            int buttonRightX = buttonUpDownX + Button.mTickBoxSize;

            mUpNote = new Button(new Vector2(buttonUpDownX, UP_NOTE_Y), "UpArrow");
            mUpNote.mType = Button.Type.Tick;
            mUpNote.ColOff = Color.White * 0.6f;
            mUpNote.ColOn = Color.White * 0.6f;
            mUpNote.ClickCB = MoveDirUp;

            mDownNote = new Button(new Vector2(buttonUpDownX, DOWN_NOTE_Y), "DownArrow");
            mDownNote.mType = Button.Type.Tick;
            mDownNote.ColOff = Color.White * 0.6f;
            mDownNote.ColOn = Color.White * 0.6f;
            mDownNote.ClickCB = MoveDirDown;

            mShiftRight = new Button(new Vector2(buttonRightX, LEFT_RIGHT_NOTE_Y), "RightArrow");
            mShiftRight.mType = Button.Type.Tick;
            mShiftRight.ColOff = Color.White * 0.6f;
            mShiftRight.ColOn = Color.White * 0.6f;
            mShiftRight.ClickCB = MoveDirRight;

            mShiftLeft = new Button(new Vector2(buttonLeftX, LEFT_RIGHT_NOTE_Y), "LeftArrow");
            mShiftLeft.mType = Button.Type.Tick;
            mShiftLeft.ColOff = Color.White * 0.6f;
            mShiftLeft.ColOn = Color.White * 0.6f;
            mShiftLeft.ClickCB = MoveDirLeft;

            mDirButtons.Add(mUpNote);
            mDirButtons.Add(mDownNote);
            mDirButtons.Add(mShiftLeft);
            mDirButtons.Add(mShiftRight);

            /////////// other buttons
            ///
            mJoin = new Button(new Vector2(SECOND_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite"); ;
            mJoin.mType = Button.Type.Tick;
            mJoin.ButtonText = "JOIN";
            mJoin.ButtonTextColour = Color.Black;
            mJoin.mMask = MelodyEditorInterface.UIButtonMask.UIBM_EditControl;
            mJoin.ClickCB = Join;

            mDelete = new Button(new Vector2(FIFTH_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite"); ;
            mDelete.mType = Button.Type.Tick;
            mDelete.ButtonText = "DELETE";
            mDelete.ButtonTextColour = Color.Black;
            mDelete.mMask = MelodyEditorInterface.UIButtonMask.UIBM_EditControl;
            mDelete.ClickCB = Delete;

            mUndo = new Button(new Vector2(PLAY_BUTTON_XPOS, BOTTOM_BUTTON_YPOS), "PlainWhite"); ;
            mUndo.mType = Button.Type.Tick;
            mUndo.ButtonText = "UNDO";
            mUndo.ButtonTextColour = Color.Black;
            mUndo.mMask = MelodyEditorInterface.UIButtonMask.UIBM_EditControl;
            mUndo.ClickCB = Undo;

            mControlButtons.Add(mJoin);
            mControlButtons.Add(mUndo);
            mControlButtons.Add(mDelete);
        }

        bool Copy(MelodyEditorInterface.MEIState state)
        {
            CopyNote.Reset();

            if (mDrumRegionsCopied != null)
            {
                foreach (AIDrumRegion r in mDrumRegionsCopied)
                {
                    r.mCopied = false;
                }
            }

            mDrumRegionsCopied = new List<AIDrumRegion>();

            if (mDrumRegionsSelected!=null)
            {
                foreach(AIDrumRegion r in mDrumRegionsSelected )
                {
                    if(r.mbHasBeenEdited)
                    {
                        r.mCopied = true;
                        mDrumRegionsCopied.Add(r);
                    }
                }
                return false;
            }

            if (mSelectedNotes.Count == 0)
            {
                return false;
            }

            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();
            pa.SetUndoState(state.mMeledInterf); //?

            foreach (Note n in mSelectedNotes)
            {
                CopyNote.AddNewCopyNote(n);
            }

            CopyNote.PostCopyUpdate(pa,mRectSelect);
            //state.PlayAreaChanged(true);
            return false;
        }

        bool Paste(MelodyEditorInterface.MEIState state)
        {
            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();
            pa.SetUndoState(state.mMeledInterf);

            if (mDrumRegionsCopied != null && mDrumRegionsCopied.Count>0)
            {
                if(mDrumRegionsSelected != null && mDrumRegionsSelected.Count>0)
                {
                    mDrumRegionsSelected[0].CopyOtherReegion(mDrumRegionsCopied[0]);
                }
            }
            else
            {
                CopyNote.Paste(pa, mRectSelect);
            }

            state.PlayAreaChanged(true);
            return false;
        }

        bool Undo(MelodyEditorInterface.MEIState state)
        {
            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();

            if (pa.UndoChangeData != null)
            {
                MidiEventCollection tempCollection = SaveLoad.GetNewMidiEventCollection();
                pa.GatherIntoCollectionFromPlayArea(tempCollection, state.mMeledInterf);

                state.PlayAreaChanged(true);

                pa.UndoLastChange();

                pa.UndoChangeData = tempCollection;
            }

            GetSelectedNotesFromRectangle(state);

            return false;
        }

        bool Delete(MelodyEditorInterface.MEIState state)
        {
            if(mSelectedNotes.Count==0)
            {
                return false;
            }

            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();
            pa.SetUndoState(state.mMeledInterf);

            foreach (Note n in mSelectedNotes)
            {
                if (n.Playing())
                {
                    state.NoteOff(pa.mChannel, n.mNoteNum);
                }
                n.RemoveFromParentLine();
            }
            mSelectedNotes = new List<Note>();
            state.PlayAreaChanged(true);

            return false;
        }

        bool Join(MelodyEditorInterface.MEIState state)
        {
            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();
            MidiEventCollection cacheUndoState = pa.GetUndoState(state.mMeledInterf);

            Dictionary<int, List<Note>> noteDict = new Dictionary<int, List<Note>>(); //reset

            //gather line lists
            foreach (Note n in mSelectedNotes)
            {
                if (!noteDict.ContainsKey(n.mNoteNum))
                {
                    List<Note> ln = new List<Note>();
                    ln.Add(n);
                    noteDict.Add(n.mNoteNum, ln);
                }
                else
                {
                    List<Note> ln = noteDict[n.mNoteNum];
                    ln.Add(n);
                }
            }
            bool bNotesJoined = false;

            foreach (KeyValuePair<int, List<Note>> entry in noteDict)
            {
                List<Note> ln = entry.Value;
                bNotesJoined|= JoinNotesInList(ln, state);
            }

            if(bNotesJoined)
            {
                state.PlayAreaChanged(true);
            }

            if(bNotesJoined)
            {
                pa.UndoChangeData = cacheUndoState;
            }

            return bNotesJoined;
        }

        bool JoinNotesInList(List<Note> ln, MelodyEditorInterface.MEIState state)
        {
            if (ln.Count < 2)
            {
                //Doesn't have enough notes to join
                return false;
            }
            bool bNotesJoined = false;

            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();
            ln.Sort(NoteLine.SortByNoteStart);
            Note startNote = ln[0];
            float startPosNewNote = ln[0].BeatPos;
            float endPosNewNote = ln[ln.Count-1].BeatEndPos;
            float newNoteLength = endPosNewNote - startPosNewNote;

            NoteLine nl = pa.GetNoteLineAtNoteNumber(startNote.mNoteNum); //

            Note newNote = new Note(startNote);
            newNote.SetPosLen(startPosNewNote, newNoteLength);

            if (nl.CanNoteFitInLine(newNote, true))
            {
                foreach (Note n in ln)
                {
                    if (n.Playing())
                    {
                        state.NoteOff(pa.mChannel, n.mNoteNum);
                    }
                    n.RemoveFromParentLine();
                    mSelectedNotes.Remove(n);
                }

                Note noteAdded = nl.AddNote(startPosNewNote, newNoteLength);

                noteAdded.EditSelected = true;
                mSelectedNotes.Add(noteAdded);

                bNotesJoined = true;
            }

            GetSelectedNotesFromRectangle(state);

            return bNotesJoined;
        }

        public bool MoveDirUp(MelodyEditorInterface.MEIState state)
        {
            return MoveUpOrDown(state, true);
        }


        public bool MoveDirDown(MelodyEditorInterface.MEIState state)
        {
            return MoveUpOrDown(state, false);
        }

        bool MoveUpOrDown(MelodyEditorInterface.MEIState state, bool bUp)
        {
            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();

            foreach (Note n in mSelectedNotes)
            {
                NoteLine nl = pa.GetNoteLineAtNoteNumber(n.mNoteNum); //TODO easier way? !
                NoteLine nextLine = nl.GetAboveOrBelow(bUp);
                if (nextLine == null)
                {
                    return false;
                }

                if (!nextLine.CanNoteFitInLine(n, true))
                {
                    return false;
                }
            }

            List<Note> newNotesInSelected = new List<Note>();
            foreach (Note n in mSelectedNotes)
            {
                NoteLine nl = pa.GetNoteLineAtNoteNumber(n.mNoteNum); //TODO easier way? !
                NoteLine nextLine = nl.GetAboveOrBelow(bUp);

                if (n.Playing())
                {
                    state.NoteOff(pa.mChannel, n.mNoteNum);
                }

                n.RemoveFromParentLine();

                Note newNote = nextLine.AddNote(n.BeatPos, n.BeatLength);
                newNote.Velocity = n.Velocity;

                mAnchorNote = mAnchorNote == n ? newNote : mAnchorNote; //Keep the mAnchorNote correct to the held note 

                newNotesInSelected.Add(newNote);
            }

            state.PlayAreaChanged(true);

            mSelectedNotes = newNotesInSelected;
            SetSelectedNote(true);
            return mSelectedNotes.Count > 0;
        }

        public bool MoveLeftOrRightOnXdiff(MelodyEditorInterface.MEIState state, int XpointDiff)
        {
            int absXMove = Math.Abs(XpointDiff);

            if(absXMove > (int)(MelodyEditorInterface.BEAT_PIXEL_WIDTH * state.mMeledInterf.QuantiseSetting))
            {
                if(XpointDiff> 0)
                {
                    MoveDirRight(state);
                }
                else
                {
                    MoveDirLeft(state);
                }
                return true;
            }

            return false;
        }

        bool MoveDirRight(MelodyEditorInterface.MEIState state)
        {
            return MoveLeftRight(state, false);
        }

        bool MoveDirLeft(MelodyEditorInterface.MEIState state)
        {
            return MoveLeftRight(state, true);
        }

        bool MoveLeftRight(MelodyEditorInterface.MEIState state, bool bLeft)
        {
            float moveAlongValue = state.mMeledInterf.QuantiseSetting;

            float moveDist = bLeft ? -moveAlongValue : moveAlongValue;

            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();

            foreach (Note n in mSelectedNotes)
            {
                NoteLine nl = pa.GetNoteLineAtNoteNumber(n.mNoteNum); //TODO easier way? !

                float newNoteStart = n.BeatPos;
                float newNoteEnd = n.BeatEndPos;

                if(bLeft)
                {
                    newNoteStart -= moveAlongValue;
                }
                else
                {
                    newNoteEnd += moveAlongValue;
                }

                Note testNote = new Note(n);
                testNote.SetPosLen(newNoteStart, newNoteEnd - newNoteStart);

                if (!nl.CanNoteFitInLine(testNote, true))
                {
                    return false;
                }
            }

            foreach (Note n in mSelectedNotes)
            {
                n.SetPosLen(n.BeatPos + moveDist, n.BeatLength);
            }
            state.PlayAreaChanged(true);

            return false;
        }

        public void LoadContent(ContentManager contentMan, SpriteFont font, SpriteFont smallGridFont)
        {
            mFont = font;
            foreach (Button ab in mDirButtons)
            {
                ab.LoadContent(contentMan, font);
            }
            foreach (Button ab in mControlButtons)
            {
                ab.LoadContent(contentMan, font);
            }
        }

        // // // // // // // // // // // // // // // // // // // // 
        //ENTER MODE!
        // // // // // // // // // // // // // // // // // // // // 
        public void Start(MelodyEditorInterface.MEIState state)
        {
            state.BlockBigGreyRectangle = true;
            state.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_EditMode | MelodyEditorInterface.UIButtonMask.UIBM_Start | MelodyEditorInterface.UIButtonMask.UIBM_EditControl; //set to only this button updates
            Active = true;
            GetSelectedNotesFromRectangle(state);
            CopyNote.Active = true;
        }

        public void StartKeyScaleMode(MelodyEditorInterface.MEIState state)
        {
            state.EditMode = true;
            state.BlockBigGreyRectangle = true;
            Active = true;
            GetSelectedNotesFromRectangle(state);
            CopyNote.Active = true;
        }

        // // // // // // // // // // // // // // // // // // // // 
        //EXIT MODE!
        // // // // // // // // // // // // // // // // // // // // 
        public void Stop(MelodyEditorInterface.MEIState state)
        {
            state.EditMode = false;
            state.BlockBigGreyRectangle = false;
            state.mCurrentButtonUpdate = MelodyEditorInterface.UIButtonMask.UIBM_ALL; //allow all
            Active = false;
            CopyNote.Active = false;
            ClearSelectedNotes();
        }

        void SetSelectedNote(bool bSet)
        {
            foreach (Note n in mSelectedNotes)
            {
                n.EditSelected = bSet;
            }
        }

        void ClearSelectedNotes()
        {
            if(mDrumRegionsCopied!=null)
            {
                foreach (AIDrumRegion r in mDrumRegionsCopied)
                {
                    r.ClearCutPasteFlags();
                }
            }
            if (mDrumRegionsSelected != null)
            {
                foreach (AIDrumRegion r in mDrumRegionsSelected)
                {
                    r.ClearCutPasteFlags();
                }
            }

            SetSelectedNote(false);
            mSelectedNotes = new List<Note>();
        }


        public bool AddAnchordNote(Note noteAdd, bool bEditMode)
        {
            if(AddSelectedNote( noteAdd, bEditMode))
            {
                mAnchorNote = noteAdd;
                return true;
            }
            return false;
        }

        public bool  AddSelectedNote(Note noteAdd, bool bEditMode)
        {
            foreach (Note n in mSelectedNotes)
            {
                if (noteAdd == n)
                {
                    if(bEditMode)
                    {
                        //Its in here but we are in edit mode so return true and allow group shifting
                        return true;
                    }
                    return false;
                }
            }
            mSelectedNotes.Add(noteAdd);
            noteAdd.EditSelected = true;
            return true;
        }

        public bool RemoveSelectedNote(Note noteRemove)
        {
            foreach (Note n in mSelectedNotes)
            {
                if(noteRemove == n)
                {
                    noteRemove.EditSelected = false;
                    mSelectedNotes.Remove(noteRemove);
                    return true;
                }
            }

            return false;
        }

        public Note GetAnchorNote()
        {
            return mAnchorNote;
        }

        void GetSelectedNotesFromRectangle(MelodyEditorInterface.MEIState state)
        {
            //Add ability to copy all in the rectangle below to bottom - by starting rectangle in top region area
            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();
            SetSelectedNote(false);
            mbRectChangeWaitForLeftUp = false;
            //Gather notes overlapped by rectangle
            mSelectedNotes = pa.GetNotesInRectangle(mRectSelect, CopyNote.mbAllNotesSelectedTopToBottom);

            //If copied notes exist then recalc the positions
            CopyNote.RecalcCopyNotes(pa, mRectSelect);

            SetSelectedNote(true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        /// UPDATE
        //////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        public bool Update(MelodyEditorInterface.MEIState state)
        {
            if (state.mNoteUI.mAdjustHeldNoteMode)
            {
                //if moving notes around that have been selected using area select then dot do stuff like let left up delete note - cross fingers the timing works
                return false;
            }

            PlayArea pa = state.mMeledInterf.GetCurrentPlayArea();
            mbKeyRangePlayAreaMode = state.mbKeyRangePlayAreaMode;

            mbUndoAvailable = pa.UndoChangeData != null;

            bool bSecondTouch = state.input.SecondHeld;

            int x1 = state.input.X;
            int y1 = state.input.Y;

            int x2 = state.input.X2;
            int y2 = state.input.Y2;

            TopLineChordAI tlcr = pa.GetTopLineChordAI();
            TopLineDrumAI tldr = pa.GetTopLineDrumAI();
            bool b1stTouchInTopRegion = false;
            bool b2ndTouchInTopRegion = false;

            //TODO TO FIX ANDROID 2ND TOUCH SWAP THIS SO CAN HOLD FIXED A 1ST TOUCH OUT OF THE PLAY AREA AND CONTROL TOUCH IN TOP REGION MORE EASILY

            if(tldr!=null && tldr.mRect.Contains(x1, y1))
            {
                x1 = pa.GetNoteLines()[0].GetBeatXPosQuantFromTouch(x1, CopyNote.QuantTopRegion);
                b1stTouchInTopRegion = true;
                //Just make a list of all drum regions overlapped and 
                if (mDrumRegionsSelected != null)
                {
                    foreach(AIDrumRegion r in mDrumRegionsSelected)
                    {
                        r.mSelected = false;
                        r.mPasteCandidate = false;
                    }
                }
                mDrumRegionsSelected = tldr.GetOverLappedRegions(mRectSelect);

                if (mDrumRegionsSelected != null)
                {
                    foreach (AIDrumRegion r in mDrumRegionsSelected)
                    {
                        r.mSelected = true;
                        r.mPasteCandidate = (mDrumRegionsCopied.Count > 0 && !r.mCopied);
                    }
                }

            }
            else if (tlcr.mRect.Contains(x1, y1))
            {
                x1 = pa.GetNoteLines()[0].GetBeatXPosQuantFromTouch(x1, CopyNote.QuantTopRegion);
                b1stTouchInTopRegion = true;
            }

            if (bSecondTouch && (pa.mAreaRectangle.Contains(x1,y1) || b1stTouchInTopRegion) && (pa.mAreaRectangle.Contains(x2, y2) || b1stTouchInTopRegion))
            {
                int rectx = x2 < x1 ? x2 : x1;
                int recty = y2 < y1 ? y2 : y1;

                int rectWidth = Math.Abs(x2 - x1);
                int rectHeight = Math.Abs(y2 - y1);

                if(b1stTouchInTopRegion)
                {
                    CopyNote.mbAllNotesSelectedTopToBottom = true; //if set will grabe *ALL* notes from the top of the left/right area down to the bottom to copy - TODO will need to weed out scale notes for selected scale I think
                    recty = tlcr.mRect.Y;
                    rectHeight = pa.mAreaRectangle.Height + NoteLine.TRACK_HEIGHT;
                }
                else
                {
                    CopyNote.mbAllNotesSelectedTopToBottom = false; //if set will grabe *ALL* notes from the top of the left/right area down to the bottom to copy - TODO will need to weed out scale notes for selected scale I think
                }

                mbRectChangeWaitForLeftUp = true;

                if (mRectSelect==null)
                {
                    mRectSelect = new Rectangle(rectx, recty, rectWidth, rectHeight);
                }
                else
                {
                    mRectSelect.X = rectx;
                    mRectSelect.Y = recty;
                    mRectSelect.Width = rectWidth;
                    mRectSelect.Height = rectHeight;
                }

                CopyNote.mRectPasteArea.Height = rectHeight;
                CopyNote.mRectPasteArea.X = rectx;
                CopyNote.mRectPasteArea.Y = tlcr.mRect.Y;
            }
            else if(state.input.LeftUp)
            {
                List<Note> leftTouchNote = pa.GetNotesInRectangle(state.input.mRectangle);

                if(leftTouchNote.Count>0)
                {
                    leftTouchNote[0].EditSelected = !leftTouchNote[0].EditSelected;
                    if(leftTouchNote[0].EditSelected)
                    {
                        mSelectedNotes.Add(leftTouchNote[0]);
                    }
                    else
                    {
                        mSelectedNotes.Remove(leftTouchNote[0]);
                    }
                }
            }

            if(mbRectChangeWaitForLeftUp && (state.input.LeftUp || state.input.RightUp))
            {
                if(CopyNote.mbAllNotesSelectedTopToBottom)
                {
                    int rhsRect = mRectSelect.X + mRectSelect.Width;
                    bool relaseBelow1stTouch = false;
                    if (x2 == mRectSelect.X)
                    {
                        //means that the rect rhs is the first touch pos that has been quantised so we 
                        //need to anchor to that
                        relaseBelow1stTouch = true;
                    }

                    x2 = pa.GetNoteLines()[0].GetBeatXPosQuantFromTouch(x2, CopyNote.QuantTopRegion);

                    int rectx = relaseBelow1stTouch ? x2 : mRectSelect.X;
                    int rectWidth = relaseBelow1stTouch ? Math.Abs(rhsRect - x2) : Math.Abs(x2 - mRectSelect.X);

                    mRectSelect.X = rectx;
                    mRectSelect.Width = rectWidth;
                }
                GetSelectedNotesFromRectangle(state);
            }

            foreach (Button ab in mDirButtons)
            {
                ab.Update(state);
                ab.mbOn = false;
            }
            foreach (Button ab in mControlButtons)
            {
                ab.Update(state);
                ab.mbOn = false;
            }

            if (pa.GetEditedY())
            {
                CopyNote.RefreshWholeRangeCopyNotes(pa, mRectSelect);
            }

            return false;
        }

        public void Draw(SpriteBatch sb)
        {
            if (!Active)
            {
                return;
            }

            foreach (Button ab in mControlButtons)
            {
                ab.Draw(sb);
            }

            sb.Begin();

            //CopyNote.Draw(sb);
 
            sb.DrawRectangle(mRectSelect, Color.Yellow, 8);

            if (!mbKeyRangePlayAreaMode)
            {
                sb.FillRectangle(mRightAreaMask, Color.Aquamarine * 0.6f);
                sb.DrawRectangle(mRightAreaMask, Color.Aquamarine, 8); //control button highlight
            }
            if (mbUndoAvailable)
            {
                sb.DrawRectangle(mUndo.Rectangle, Color.Yellow, 8); //control button highlight
            }
            sb.End();

            if(!mbKeyRangePlayAreaMode)
            {
                foreach (Button ab in mDirButtons)
                {
                    ab.Draw(sb);
                }
            }

        }
    }
}
