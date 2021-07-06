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
    public class ParameterNodeRegion
    {
        public float BeatPos { get; set; }
        public float BeatLength { get; set; }
        public float BeatEndPos { get; set; }

        public Color Col { set; get; } = Color.Red;

        public const float DEFAULT_PARAM_RATIO = 0.5f; //TODO MAKE MORE GENERAL FOR EACH PARAM IN A DATA INITIALISER 


        static readonly Color[] COLOURSET1 = { Color.Red, Color.Blue };
        static readonly Color[] COLOURSET2 = { Color.Green, Color.Cyan };
        const int NUM_BANK_COMMANDS = 2;

        bool mInDrawRange = false;
        public Rectangle mRect;
        ParameterNodeManager mParent;
        public bool Active { get; set; } = false;
        public bool ChangePopupAfterRefreshToData { get; set; } = false; //used when region wants to change values that show in popup - e.g. set 16 beat when set to Akesson by popup

        public bool mSelected = false;
        public bool mPasteCandidate = false;
        public bool mCopied = false;
        public bool mbHasBeenEdited = false;

        PNR_Node mLastHeld = null;
        List<PNR_Node> mMidNodeTriplet = new List<PNR_Node>(); // when moving nodes in the middle of the end points they can move left right and are limite by their nodes either side
        PNR_CommandData mPNRC_InputAllowed = null; //this will be set to the last area clicked in to allow input.

        SpriteFont mFont;

        List<PNR_CommandData> mCommands = new List<PNR_CommandData>();
        List<PNR_CommandData> mCurrentBankCommands = new List<PNR_CommandData>();

        public void SetCurrentBankCommands(int bankNum)
        {
            int indexBankStart = bankNum * NUM_BANK_COMMANDS;

            mCurrentBankCommands = new List<PNR_CommandData>();

            if(indexBankStart>= mCommands.Count)
            {
                return; //leave it, not enough for this bank
            }
            int indexBankEnd = (indexBankStart + NUM_BANK_COMMANDS);

            indexBankEnd = mCommands.Count< indexBankEnd ? mCommands.Count: indexBankEnd;

            for (int i=indexBankStart; i< indexBankEnd; i++)
            {
                mCurrentBankCommands.Add(mCommands[i]);
            }

            //TODO Will just clearing these be the safest/simplest thing here?
            if(mPNRC_InputAllowed!=null)
            {
                mPNRC_InputAllowed.SetInputAllowed(false);
                mPNRC_InputAllowed = null;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// 
        /// MAIN CONSTRUCT
        /// 
        /////////////////////////////////////////////////////////////////////
        public ParameterNodeRegion(ParameterNodeManager parent, SpriteFont font)
        {
            mParent = parent;
            mFont = font;
        }
        public ParameterNodeRegion(ParameterNodeRegion other, float beatShift)
        {
            mParent = other.mParent;
            mFont = other.mFont;

            BeatPos = other.BeatPos + beatShift;
            BeatEndPos = other.BeatEndPos + beatShift;
            BeatLength = other.BeatLength;
        }

        ///////////////////////////////////////////////////////////////////
        /// MIDI SAVING
        /// For saving out midi in loops we need to create a shifted copy
        public ParameterNodeRegion GetShiftedCopy(float beatShift)
        {
            ParameterNodeRegion copyRegion = new ParameterNodeRegion(this, beatShift);

            foreach (PNR_CommandData pcd in mCommands)
            {
                PNR_CommandData newCommand = pcd.GetShiftedCopy(beatShift);
                copyRegion.mCommands.Add(newCommand);
            }

            return copyRegion;
        }

        const int NODE_DIVISION = 5; //Hard coded means not friendly to old saves - if we're happy with 6 nodes per 4 beat bar then fine
        float BEAT_DIV = (ParameterNodeManager.FOUR_BEAT_REGION_RANGE) / NODE_DIVISION;
        void AddCommand(ParameterNodeManager.ChannelParamType cpt)
        {       
            PNR_CommandData param = new PNR_CommandData(cpt);

            param.SetColour(Color.Black);
            param.SetFont(mFont);

            PNR_Node startNode = new PNR_Node(BeatPos, param.GetDefaultRatio());
            PNR_Node endNode = new PNR_Node(BeatEndPos, param.GetDefaultRatio());

            //const int NODE_DIVISION = 5;
            //float BEAT_DIV = (BeatEndPos - BeatPos) / NODE_DIVISION;
            float nextNodeBeat = BeatPos + BEAT_DIV;

            param.AddNode(startNode);
            for (int i = 0; i < (NODE_DIVISION - 1); i++)
            {
                PNR_Node node = new PNR_Node(nextNodeBeat, param.GetDefaultRatio());
                param.AddNode(node);
                nextNodeBeat += BEAT_DIV;
            }
            param.AddNode(endNode);

            mCommands.Add(param);
            //Recalc layout to draw
        }

        //void AddCommand(string name, Color col, int midiCommand, float defaultRatio)
        //{
        //    PNR_CommandData param = new PNR_CommandData(defaultRatio);
        //    param.mMidiCommand = midiCommand;
        //    param.mName = name;

        //    param.SetColour(col);
        //    param.SetFont(mFont);

        //    PNR_Node startNode = new PNR_Node(BeatPos, defaultRatio);
        //    PNR_Node endNode = new PNR_Node(BeatEndPos, defaultRatio);

        //    const int NODE_DIVISION = 5;
        //    float BEAT_DIV = (BeatEndPos - BeatPos) / NODE_DIVISION;
        //    float nextNodeBeat = BeatPos + BEAT_DIV;

        //    param.AddNode(startNode);
        //    for(int i=0;i<(NODE_DIVISION-1);i++)
        //    {
        //        PNR_Node node = new PNR_Node(nextNodeBeat, defaultRatio);
        //        param.AddNode(node);
        //        nextNodeBeat += BEAT_DIV;
        //    }
        //    param.AddNode(endNode);

        //    mCommands.Add(param);
        //    //Recalc layout to draw
        //}

        void RecalcDrawLayout()
        {

            //ChangePopupAfterRefreshToData to fix num commands - 3 - with space at bottom for bank select
            int heightDivision = NUM_BANK_COMMANDS; // mCommands.Count;
            int regionWidth = (int)(BeatLength * MelodyEditorInterface.BEAT_PIXEL_WIDTH);
            int eachParamAreaHeight = (mParent.mParent.mAreaRectangle.Height - ParameterNodeManager.SPACE_FOR_BANK_SELECT_BUTTONS) / heightDivision;

            int startY = mRect.Y + mRect.Height; //start after the top bar, then add the wdith
            int y = startY;
            int x = GetXPosOnBeat(BeatPos);

            int countIndex = 0;
            foreach (PNR_CommandData pcd in mCommands)
            {
                pcd.SetWidth(regionWidth);
                pcd.SetX(x);
                pcd.SetRecYAndHeightDimension(y, eachParamAreaHeight);
                pcd.SetFont(mFont);
                pcd.RefreshNameAndGrid();

                foreach(PNR_Node node in pcd.mNodes)
                {
                    int xpos = GetXPosOnBeat(node.mBeatPos);
                    node.SetX(xpos);
                    int ypos = y + (int)(eachParamAreaHeight * (1f-node.mRatio));
                    node.SetY(ypos);
                }
                countIndex++;
                if (countIndex % NUM_BANK_COMMANDS == 0)
                {
                    y = startY; //start after the top bar, then add the wdith
                }
                else
                {
                    y += eachParamAreaHeight;
                }
           }
        }

        void ResetColours()
        {
            bool bColset1 = BeatPos % 8 == 0; //Alternate colour sets along regions 

            Color[] useCols = bColset1 ? COLOURSET1 : COLOURSET2;
            int colIndex = 0;

            foreach (PNR_CommandData pcd in mCommands)
            {
                Color useCol = useCols[colIndex];
                pcd.SetColour(useCol);

                colIndex++;
                colIndex = colIndex % useCols.Length;
            }
        }

        public class ParameterNodeRegionSaveData
        {
            public List<PNR_CommandData> mCommands = new List<PNR_CommandData>();
            public float BeatPos { get; set; }
            public float BeatLength { get; set; }
            public float BeatEndPos { get; set; }

            public bool PointInRegion(float touchPoint)
            {
                if (touchPoint >= BeatPos && touchPoint < BeatEndPos)
                {
                    return true;
                }
                return false;
            }
        };

        public bool UpdatePlaying(MelodyEditorInterface.MEIState state)
        {
            float playPos = state.fMainCurrentBeat;
            int channel = mParent.mParent.mChannel;

            if (playPos >= BeatPos && playPos < (BeatPos + BeatLength)) //>= start but only < the end - was getting crashes in the pcd update when end point was 19.999999
            {
                foreach (PNR_CommandData pcd in mCommands)
                {
                    int value = pcd.UpdatePlaying(playPos);

                    if(value != pcd.GetLastDataSent())
                    {
                        pcd.SetLastDataSent(value);
                        state.mMeledInterf.SetMidiCommand(pcd.mMidiCommand, value, channel);
                    }
                }

                return true;
            }

            return false;
        }

        public bool UpdateGatherMidiEventsPlaying(float beatPos, MidiEventCollection midiCollection)
        {
            float playPos = beatPos;
            int channel = mParent.mParent.mChannel;

            long absoluteTime = 0;

            if (playPos >= BeatPos && playPos <= (BeatPos + BeatLength))
            {
                foreach (PNR_CommandData pcd in mCommands)
                {
                    int value = pcd.UpdatePlaying(playPos);
                    if (value != pcd.GetLastDataSent())
                    {
                        pcd.SetLastDataSent(value);
                        absoluteTime = (long)((float)SaveLoad.TicksPerQuarterNote * beatPos);
                        ControlChangeEvent commandEvent = new ControlChangeEvent(absoluteTime, channel, (MidiController)pcd.mMidiCommand, value);
                        midiCollection.AddEvent(commandEvent, channel);
                    }
                }

                return true;
            }

            return false;
        }

        public void ResetMidiToDefault(MelodyEditorInterface.MEIState state)
        {
            int channel = mParent.mParent.mChannel;
            foreach (PNR_CommandData pcd in mCommands)
            {
                int defaultData = pcd.GetDefaultData();

                //Make sure to revert to these globals
                if(pcd.mMidiCommand == (int)MidiController.MainVolume)
                {
                    defaultData = mParent.mParent.mVolume;
                }
                else if (pcd.mMidiCommand == (int)MidiController.Pan)
                {
                    defaultData = mParent.mParent.mPan;
                }
                state.mMeledInterf.SetMidiCommand(pcd.mMidiCommand, defaultData, channel);
            }
        }

        const float NODE_PROXIMITY_X_EPSILON = 0.05f;

        public bool UpdateInput(MelodyEditorInterface.MEIState state)
        {
            if(!Active)
            {
                return false;
            }

            if (state.input.SecondHeld)
            {
                int maxNodes = mCurrentBankCommands[0].mNodes.Count; // NODE_DIVISION + 1;

                foreach (PNR_CommandData pcd in mCurrentBankCommands)
                {
                    if(state.input.mSecondRectangle.Intersects(pcd.GetRect()))
                    {
                        float secondTouchBeat = NoteLine.GetBeatFromTouchStatic(state.input.X2, false, 0f, mParent.mParent.StartBeat);
                        float touchRangeStart = BeatPos - BEAT_DIV/2;  //Starts before the bar
                        float touchRangeEnd = touchRangeStart + BEAT_DIV;

                        float beatStart = BeatPos;  //Pos to set the node to


                        //Process the NODE_DIVISION + 1 number of nodes that sit at each possible node division when at start default and check each one for collising with this second touch
                        for (int i = 0; i < maxNodes; i++)
                        {
                            if(secondTouchBeat > touchRangeStart && secondTouchBeat< touchRangeEnd)
                            {
                                if(i != NODE_DIVISION && i != 0)
                                {
                                    //Not an end point so set the new beat pos
                                    pcd.mNodes[i].mBeatPos = beatStart; 
                                }

                                pcd.SetNodeToX(pcd.mNodes[i], beatStart, GetXPosOnBeat);
                                pcd.SetNodeToNewValueOnTouchY(pcd.mNodes[i], state.input.Y2);
                            
                                break; // found the node region so reset it and leave
                            }
                            beatStart += BEAT_DIV;
                            touchRangeStart += BEAT_DIV;
                            touchRangeEnd += BEAT_DIV;
                        }
                        break; //found a touched command so leave
                    }
                }

                mPNRC_InputAllowed = null; //Reset this on second touch
            }
            else if (state.input.Held)
            {
                if(mPNRC_InputAllowed!=null)
                {
                    bool bTouchedParamRect = false;

                    List<PNR_Node> nodeTriplet = new List<PNR_Node>();

                    PNR_Node nodeHeld = mPNRC_InputAllowed.UpdateInput(state, mLastHeld, ref bTouchedParamRect, ref nodeTriplet);

                    if (nodeHeld != null)
                    {
                        if (mLastHeld != null && nodeHeld != mLastHeld)
                        {
                            mLastHeld.SetHeld(false);
                        }
                        mLastHeld = nodeHeld;
                        mLastHeld.SetHeld(true);
                        mPNRC_InputAllowed.SetNodeToNewValueOnTouchY(mLastHeld, state.input.Y);

                        if(nodeTriplet.Count==3 && Math.Abs(state.input.mDeltaX) > 0)
                        {
                            float beatShift  = GetBeatForXShift(state.input.mDeltaX);
                            mPNRC_InputAllowed.SetNodeToNewValueOnTouchX(nodeTriplet, beatShift, GetXPosOnBeat);
                        }
                    }
                    else if (mLastHeld != null)
                    {
                        mLastHeld.SetHeld(false);
                        mLastHeld = null;
                    }

                    if(bTouchedParamRect || mLastHeld!=null)
                    {
                        return true;
                    }
                    mPNRC_InputAllowed.SetInputAllowed(false);
                    mPNRC_InputAllowed = null;
                }

                foreach (PNR_CommandData pcd in mCurrentBankCommands)
                {
                    bool bTouchedParamRect = false;

                    List<PNR_Node> nodeTriplet = new List<PNR_Node>();
                    PNR_Node nodeHeld = pcd.UpdateInput(state, mLastHeld, ref bTouchedParamRect, ref nodeTriplet);

                    if (nodeHeld != null)
                    {
                        if (mLastHeld != null && nodeHeld != mLastHeld)
                        {
                            mLastHeld.SetHeld(false);
                        }
                        mLastHeld = nodeHeld;
                        mLastHeld.SetHeld(true);
                        pcd.SetNodeToNewValueOnTouchY(mLastHeld, state.input.Y);

                        if (nodeTriplet.Count == 3 && Math.Abs(state.input.mDeltaX) > 0)
                        {
                            float beatShift = GetBeatForXShift(state.input.mDeltaX);
                            pcd.SetNodeToNewValueOnTouchX(nodeTriplet, beatShift, GetXPosOnBeat);
                        }
                    }
                    else if (mLastHeld != null)
                    {
                        mLastHeld.SetHeld(false);
                        mLastHeld = null;
                    }

                    if (bTouchedParamRect || mLastHeld != null)
                    {
                        if(mPNRC_InputAllowed!=null)
                        {
                            System.Diagnostics.Debug.Assert(false, string.Format("Is this possible?"));
                        }
                        mPNRC_InputAllowed = pcd;
                        mPNRC_InputAllowed.SetInputAllowed(true);
                        break;
                    }
                }
            }
            else
            {
                if (mLastHeld != null)
                {
                    mLastHeld.SetHeld(false);
                    mLastHeld = null;
                }

                if(mPNRC_InputAllowed!=null)
                {
                    //???
                }
            }

            return mPNRC_InputAllowed != null;
        }

        public class PNR_Node
        {
            //Public save able in json
            public float mBeatPos;
            public float mRatio;

            //Private draw/manage
            const int GRAB_RECT_SIZE = 50;
            Rectangle mGrabRect = new Rectangle(0,0, GRAB_RECT_SIZE, GRAB_RECT_SIZE);
            Rectangle mMagnifiedGrabRectangle;
            Color mColGrab = Color.Yellow;
            int mDrawAreaHeight = -1;
            int mDrawAreaStartY = -1;
            int mParamStartX = -1; //just use rect internal?
            bool mbHeld = false;

            public void SetHeld(bool val)
            {
                mbHeld = val;
                if (mbHeld)
                {
                    const int HELD_MAGNIFY = 4;

                    int shiftXY = (HELD_MAGNIFY - 1) * mGrabRect.Width / 2;
                    int heldX = mGrabRect.X - shiftXY;
                    int heldY = mGrabRect.Y - shiftXY;

                    mMagnifiedGrabRectangle = new Rectangle(heldX, heldY, mGrabRect.Width * HELD_MAGNIFY, mGrabRect.Height * HELD_MAGNIFY);
                }
             }

            public Rectangle GetRect() { return mGrabRect; }

            public PNR_Node(float beatPos, float ratio)
            {
                mBeatPos = beatPos;
                mRatio = ratio;
            }

            public void Copy(PNR_Node other)
            {
                mBeatPos = other.mBeatPos;
                mRatio = other.mRatio;
            }

            public void SetRatio(float val)
            {
                mRatio = val;
            }

            ///////////////////////////////////////////////////////
            /// DRAW
            public void Draw(SpriteBatch sb, bool bInputAllowed)
            {
                if(mbHeld)
                {
                    sb.FillRectangle(mMagnifiedGrabRectangle, 0.5f*mColGrab);
                }
                float alpha = bInputAllowed ? 1.0f : 0.5f;

                sb.FillRectangle(mGrabRect, alpha*mColGrab);
                sb.DrawRectangle(mGrabRect, Color.Black);
            }

            public void SetColour(Color col)
            {
                mColGrab = col;
            }
            public void SetX(int x)
            {
                mGrabRect.X = x - mGrabRect.Width/2;
            }
            public void SetY(int y)
            {
                mGrabRect.Y = y - mGrabRect.Height /2;
            }

            public Vector2 GetStartPoint()
            {
                return new Vector2(mGrabRect.Center.X,mGrabRect.Center.Y);
            }

            public bool Intersects(Rectangle rect)
            {
                if(mbHeld)
                {
                    return rect.Intersects(mMagnifiedGrabRectangle);
                }
                return rect.Intersects(mGrabRect);
            }
        };
 
        //Command code and node data
        public class PNR_CommandData
        {
            public string mName = "";
            public int mMidiCommand=-1;
            public List<PNR_Node> mNodes = new List<PNR_Node>();
            PNR_Node mLastNode = null;

            ParameterNodeManager.ChannelParamType mCPT; //useful for reference info for whole channel

            public delegate int DelegateGetXPosOnBeat(float beatPos);

            const int BACK_RECT_SIZE = 16; //Needs setting depending on the beats used width and param height 
            Rectangle mBackRect = new Rectangle(0, 0, BACK_RECT_SIZE, BACK_RECT_SIZE);
            Vector2 mMidlineStart = new Vector2(0, 0);
            Vector2 mMidlineEnd = new Vector2(0, 0);
            Color mColGrab = Color.Beige;
            SpriteFont mFont;
            public Rectangle GetRect() { return mBackRect; }
            bool mbInputAllowed = false;
            public void SetInputAllowed(bool val) { mbInputAllowed = val; }

            //float mDefaultRatio = ParameterNodeRegion.DEFAULT_PARAM_RATIO;

            //When saving/playing try not to send the same command too often
            //int mLastDataSent= -1;
            public int GetLastDataSent() { return mCPT.lastDataSent; }
            public void SetLastDataSent(int val) { mCPT.lastDataSent = val; }

            public PNR_CommandData(ParameterNodeManager.ChannelParamType cpt)
            {
                mCPT = cpt;
                mName = cpt.name;
                mMidiCommand = cpt.command;
               // mDefaultRatio = cpt.defaultRatio;
            }

            //For Json deserialise have this parameterless constructor that wont overwrite the mDefaultRatio - TODO fix up loaded deserialised data to default values 
            public PNR_CommandData()
            {
            }
            //Weird, the json deserilaiser is happy to use this constructor passing a zero defaultRatio with no exception or warning!? <-- ignore now a this copy?
            public PNR_CommandData(PNR_CommandData other)
            {
                //mDefaultRatio = other.mDefaultRatio;
                mMidiCommand = other.mMidiCommand;
                mCPT = other.mCPT;
            }

            public void SetCPT(ParameterNodeManager.ChannelParamType CPT)
            {
                mCPT = CPT;
                System.Diagnostics.Debug.Assert(mMidiCommand == CPT.command, string.Format("Commands dont match! {0} {1}", mMidiCommand ,CPT.command));
            }
            //Weird, the json deserilaiser is happy to use this constructor passing a zero defaultRatio with no exception or warning!? <-- ignore now a this copy?
            //public PNR_CommandData(float defaultRatio)
            //{
            //    mDefaultRatio = defaultRatio;
            //}

            /////////////////////////////////////////////////////////////////////
            /// MIDI SAVING
            /// This is for saves only so no need for names, rect etc,
            public PNR_CommandData GetShiftedCopy(float beatShift)
            {
                PNR_CommandData newCommand = new PNR_CommandData(this);

                foreach (PNR_Node node in mNodes)
                {
                    PNR_Node copyNode = new PNR_Node(node.mBeatPos+beatShift, node.mRatio);
                    newCommand.AddNode(copyNode);
                }

                return newCommand;
            }

            public float GetDefaultRatio()
            {
                mCPT.lastDataSent = -1; //reset this if default ever sent so next command is always sent
                return mCPT.defaultRatio;
            }

            public int GetDefaultData()
            {
                mCPT.lastDataSent = -1; //reset this if default ever sent so next command is always sent
                return (int)(127f*mCPT.defaultRatio);
            }

            int stringOffsetFromX = -1;
            int stringY = -1;
            int stringHeight = -1;
            int stringWidth = -1;

            public void SetName(string name)
            {
                mName = name;
                RefreshNameAndGrid();
            }

            public void RefreshNameAndGrid()
            {
                stringHeight = (int)mFont.MeasureString(mName).Y;
                stringWidth = (int)mFont.MeasureString(mName).X;
                stringY = mBackRect.Y + 2;
                stringOffsetFromX = mBackRect.Width / 2 - stringWidth / 2;

                int midRect = mBackRect.Y + mBackRect.Height / 2;

                mMidlineStart = new Vector2(mBackRect.X, midRect);
                mMidlineEnd = new Vector2(mBackRect.X + mBackRect.Width, midRect);
            }

            public void SetNodeToNewValueOnTouchY(PNR_Node node, int newY)
            {
                float newRatio = 0f;

                if (newY < mBackRect.Y)
                {
                    newY = mBackRect.Y;
                    newRatio = 1f;
                }
                else if (newY > (mBackRect.Y + mBackRect.Height))
                {
                    newY = mBackRect.Y + mBackRect.Height;
                    newRatio = 0f;
                }
                else
                {
                    int diffY = newY - mBackRect.Y;
                    newRatio = 1f - ((float)diffY/(float)mBackRect.Height);
                }

                node.SetY(newY);
                node.mRatio = newRatio;
            }

            public void SetNodeToNewValueOnTouchX(List<PNR_Node> nodeTriplet, float beatShift, DelegateGetXPosOnBeat shiftFunc)
            {
                //This is moving the middle node of the passed triplet 
                //wont be allowed to move before or ahead of the x position of the nodes either side
                System.Diagnostics.Debug.Assert( nodeTriplet.Count==3, string.Format("Should only be 3 nodes"));

                float newNodePos = nodeTriplet[1].mBeatPos + beatShift;

                float mostRightLimit = nodeTriplet[2].mBeatPos - NODE_PROXIMITY_X_EPSILON;
                float mostLeftLimit = nodeTriplet[0].mBeatPos + NODE_PROXIMITY_X_EPSILON;

                if(newNodePos> mostRightLimit)
                {
                    newNodePos = mostRightLimit;
                }

                if (newNodePos < mostLeftLimit)
                {
                    newNodePos = mostLeftLimit;
                }


                SetNodeToX(nodeTriplet[1], newNodePos, shiftFunc);

                return;
            }

            public void SetNodeToX(PNR_Node node, float newNodePos, DelegateGetXPosOnBeat shiftFunc)
            {
                node.SetX(shiftFunc(newNodePos));
                node.mBeatPos = newNodePos;
            }

            public void SetRecYAndHeightDimension(int y, int height)
            {
                mBackRect.Y = y;
                mBackRect.Height = height;
            }

            public void SetX(int x)
            {
                mBackRect.X = x;
            }
            public void SetWidth(int w)
            {
                mBackRect.Width = w;
            }

            public void SetColour(Color col)
            {
                mColGrab = col;

                foreach (PNR_Node node in mNodes)
                {
                    node.SetColour(col);
                }
            }

            public Color GetColour()
            {
                return mColGrab;
            }

            public void AddNode(PNR_Node node)
            {
                node.SetColour(GetColour());
                //Refresh draw info
                mNodes.Add(node);
                mLastNode = node;
            }

            public void SetFont(SpriteFont font)
            {
                mFont = font;
            }

            /////////////////////////////////////////////////////////
            /// INPUT
            ///  
            /// 
            /// 
            public PNR_Node UpdateInput(MelodyEditorInterface.MEIState state, PNR_Node lastHeld, ref bool touchInside, ref List<PNR_Node> nodeTriplet)
            {
                ///nodeTriplet = new List<PNR_Node>();
                ///
                touchInside = state.input.mRectangle.Intersects(mBackRect);

                if(!touchInside)
                {
                    return null;
                }

                bool mustFindLastHeld = false;
                bool mustFindLastHeldIsMiddle = true;
                if (lastHeld!=null)
                {
                    if (lastHeld.Intersects(state.input.mRectangle))
                    {
                        if (lastHeld == mNodes[0] || lastHeld == mNodes[mNodes.Count - 1])
                        {
                            mustFindLastHeldIsMiddle = false;
                        }
                        mustFindLastHeld = true;
                    }
                }

                int index = 0;
                foreach (PNR_Node node in mNodes)
                {
                    if (mustFindLastHeld)
                    {
                        if (node == lastHeld)
                        {
                            if(mustFindLastHeldIsMiddle)
                            {
                                nodeTriplet.Add(mNodes[index - 1]);
                                nodeTriplet.Add(mNodes[index]);
                                nodeTriplet.Add(mNodes[index + 1]);
                            }
                            return node;
                        }
                    }
                    else
                    if (node.Intersects(state.input.mRectangle))
                    {
                        if (node==mNodes[0] || node == mNodes[mNodes.Count-1])
                        {
                            //End node just return
                            return node;
                        }
                        else
                        {
                            nodeTriplet.Add(mNodes[index - 1]);
                            nodeTriplet.Add(mNodes[index]);
                            nodeTriplet.Add(mNodes[index + 1]);
                            return node;
                        }
                    }
                    index++;
                }

                return null;
            }

            public int UpdatePlaying(float beatPos)
            {
                int index = 0;
                int indexFound = -1;
                foreach (PNR_Node node in mNodes)
                {
                    if(node == mLastNode)
                    {
                        return (int)(127f * mLastNode.mRatio); //The float accuracy that allows this could do with looking at e.g. the last beat is 3.999999 so can get past if beat is 4
                    }

                    if(beatPos>= node.mBeatPos && beatPos <= mNodes[index+1].mBeatPos)
                    {
                        indexFound = index;
                        break; 
                    }
                    index++;
                }

                if(indexFound!=-1)
                {
                    float startRange    = mNodes[indexFound].mBeatPos;
                    float endRange      = mNodes[indexFound +1].mBeatPos;
                    float distIn = beatPos - startRange;
                    float diffRange = endRange - startRange;
                    float ratioThruRange = distIn / diffRange;

                    float startRatio = mNodes[indexFound].mRatio;
                    float endRatio = mNodes[indexFound + 1].mRatio;
                    float diffRatio = endRatio - startRatio;

                    float finalValue = ratioThruRange * diffRatio + startRatio;

                    if(finalValue>1f)
                    {
                        finalValue = 1f;
                    }
                    return (int)(127f*finalValue);
                }

                return -1;
            }

            ///////////////////////////////////////////////////////
            /// DRAW
            public void Draw(SpriteBatch sb, PNR_Node lastHeld)
            {
                float backAlpha = mbInputAllowed ? 0.2f : 0.1f;
                float alpha = mbInputAllowed ? 1f : 0.5f;
                float stringalpha = mbInputAllowed ? 1f : 0.8f;
                Color stringcol = mbInputAllowed ? Color.White : Color.Black;

                if(mbInputAllowed)
                {
                    //drawstring rect
                    Rectangle stringRect = new Rectangle(mBackRect.X + stringOffsetFromX, stringY, stringWidth, stringHeight);
                    sb.FillRectangle(stringRect, Color.Black); //TODO same colour as grab tabs but more transparent
                }

                sb.FillRectangle(mBackRect, mColGrab * backAlpha); //TODO same colour as grab tabs but more transparent
                sb.DrawRectangle(mBackRect, alpha* Color.Black, 4);
                sb.DrawString(mFont, mName, new Vector2(mBackRect.X+stringOffsetFromX, stringY), stringalpha * stringcol);

                sb.DrawLine(mMidlineStart, mMidlineEnd, alpha * Color.Yellow, 4);

                Vector2 lastPoint = new Vector2(0,0);

                bool bHaveTwoPoints = false;

                foreach (PNR_Node node in mNodes)
                {
                    if(node!=lastHeld)
                    {
                        node.Draw(sb, mbInputAllowed);
                    }
                    Vector2 thisPoint = node.GetStartPoint();
                    if (bHaveTwoPoints)
                    {
                        sb.DrawLine(lastPoint, thisPoint, alpha*mColGrab, 4);
                    }
                    bHaveTwoPoints = true;
                    lastPoint = thisPoint;
                }

                if (lastHeld != null)
                {
                    lastHeld.Draw(sb, mbInputAllowed);
                }
            
            }
        };

        public class ParameterNodeRegionData
        {
            //public int mValue = 0;
            public List<PNR_CommandData> mCommandData = new List<PNR_CommandData>();
            //public void Copy(ParameterNodeRegionData other)
            //{
            //    mValue = other.mValue;
            //}
        };

        public ParameterNodeRegionSaveData GetSaveData()
        {
            ParameterNodeRegionSaveData saveData = new ParameterNodeRegionSaveData();
            saveData.BeatPos = BeatPos;
            saveData.BeatEndPos = BeatEndPos;
            saveData.BeatLength = BeatLength;
            saveData.mCommands = mCommands;

            return saveData;
        }

        public bool Overlaps(Rectangle testRect)
        {
            if (testRect.Intersects(mRect) || testRect.Contains(mRect))
            {
                return true;
            }

            return false;
        }

        public void LoadSaveData(ParameterNodeRegionSaveData loadData, List<ParameterNodeManager.ChannelParamType> cptl )
        {
            BeatPos = loadData.BeatPos;
            BeatEndPos = loadData.BeatEndPos;
            BeatLength = loadData.BeatLength;

            mCommands = loadData.mCommands;

            System.Diagnostics.Debug.Assert(cptl.Count == mCommands.Count, string.Format("Param list size dont match"));

            int index = 0;
            foreach (PNR_CommandData pcd in mCommands)
            {
                pcd.SetCPT(cptl[index]);
                index++;
            }
            ResetColours();

            //TODO Check commands in mCommands in our current InitParamRegion list of commands and match names and set default ratios as necessary to whatever possible new ones we have now that may be different to the ones saved.
            RefreshRectToData();
            SetCurrentBankCommands(mParent.mParamBank);
        }

        int GetXPosOnBeat(float beatPos)
        {
            float fStartPos = mParent.mParent.StartBeat;
            int startX = mParent.mStartX + (int)((beatPos - fStartPos) * MelodyEditorInterface.BEAT_PIXEL_WIDTH);

            return startX;
        }

        float GetBeatForXShift(int xShift )
        {
            return (float)xShift * MelodyEditorInterface.INV_BEAT_PIXEL_WIDTH;
        }

        public void RefreshRectToData()
        {
            int startY = mParent.mStartY;
            int startX = GetXPosOnBeat(BeatPos);
            int regionWidth = (int)(BeatLength * MelodyEditorInterface.BEAT_PIXEL_WIDTH);
            mRect = new Rectangle(startX, startY, regionWidth, NoteLine.TRACK_HEIGHT);  //NoteLine.TRACK_HEIGHT);
            mInDrawRange = mRect.Intersects(mParent.mRect);

            RecalcDrawLayout();
        }

        public void InitParamRegion(float pos, float len, List<ParameterNodeManager.ChannelParamType> cptl)
        {
            BeatPos = pos;
            BeatLength = len;
            BeatEndPos = pos + len;

            foreach(ParameterNodeManager.ChannelParamType cpt in cptl)
            {
                AddCommand(cpt);
            }

            //AddCommand("Volume", Color.Red, (int)MidiController.MainVolume, DEFAULT_PARAM_RATIO);
            //AddCommand("Pan", Color.Blue, (int)MidiController.Pan, DEFAULT_PARAM_RATIO);

            //AddCommand("Reverb Send", Color.Green, (int)91, DEFAULT_PARAM_RATIO);
            //AddCommand("Chorus Send", Color.Cyan, (int)93, DEFAULT_PARAM_RATIO);

            //AddCommand("Filter Cutoff", Color.Purple, (int)74, DEFAULT_PARAM_RATIO);
            //AddCommand("Resonance", Color.Indigo, (int)71, DEFAULT_PARAM_RATIO);

            //AddCommand("Phaser", Color.Purple, (int)95, DEFAULT_PARAM_RATIO);
            //AddCommand("Detune", Color.Indigo, (int)94, DEFAULT_PARAM_RATIO);

            //AddCommand("Variation Send", Color.Cyan, (int)70, DEFAULT_PARAM_RATIO);

            //AddCommand("95", Color.Green, (int)95);
            //AddCommand("7", Color.Yellow, (int)7);

            RefreshRectToData();

            ResetColours(); //Blow off those lovely colours above!

            SetCurrentBankCommands(mParent.mParamBank);
        }


        public bool IsNoteInRegion(float pos, float width)
        {
            if (pos >= BeatPos && (pos + width) <= BeatEndPos)
            {
                return true;
            }
            return false;
        }


        ///////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////
        ///
        ///
        /// MAIN DRAW
        /// 
        ///
        ///////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////
        public void Draw(SpriteBatch sb)
        {
            if (mInDrawRange)
            {
                Color fillCol = Col;
                Color col = Color.LightSlateGray;

                //Top line
                sb.FillRectangle(mRect, fillCol * .2f);

                sb.DrawRectangle(mRect, mbHasBeenEdited ? col : Color.Black, mbHasBeenEdited ? 5f : 1f);
                if (Active)
                {
                    foreach (PNR_CommandData pcd in mCurrentBankCommands)
                    {
                        if (mPNRC_InputAllowed != pcd)
                        {
                            pcd.Draw(sb, mLastHeld);
                        }
                    }
                    if (mPNRC_InputAllowed != null)
                    {
                        mPNRC_InputAllowed.Draw(sb, mLastHeld);
                    }
                }
            }
        }
    }
}
