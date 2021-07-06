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
    public class ParamButtonBar
    {
        public bool mBarMode = false; //when true the bar is showing  all buttons showing for selecting

        const int SELECTED_GAP_ROUND_BUTTON = 16;
        const int SELECTED_WIDTH = SELECTED_GAP_ROUND_BUTTON * 2 + Button.mWidth;
        const int SELECTED_HEIGHT = SELECTED_GAP_ROUND_BUTTON * 2 + Button.mBarHeight;
        List<ButtonGrid.GridEntryData> mGEDs = new List<ButtonGrid.GridEntryData>();
        Vector2 mvScreenPos;
        Button mCurrentButton = null;
        Button mScrollButton = null;
        public List<Button> mButtons = new List<Button>();
        static public SpriteFont mFont;
        static public Texture2D mGridButtonTex;
        Rectangle mRectSelected;

        int mNumButtons = 0;
        int mNumAboveCentre = 0;
        int mCurrentButtonSelectIndex = 0;

        public enum DROP_TYPE
        {
            DOWN,
            MIDDLE,
            UP,
        }

        /////////////////////////////////////////////////////////////////////////////////////////////
        //STATIC HELPER FUNCTIONS
        public static int PARAM_BAR_YPOS = 0;
        public static int PARAM_BAR_XPOS_START = 0;
        public static int GAP_AFTER_IGNORE_X = 0;

        static public void SetOffsets(int pbypos, int pbxpos, int gap)
        {
            PARAM_BAR_YPOS = pbypos;
            PARAM_BAR_XPOS_START = pbxpos;
            GAP_AFTER_IGNORE_X = gap;
        }

        static public ParamButtonBar SetUpPopupParamBar(string[] typeNames, Action<Button, MelodyEditorInterface.MEIState> callBack, DROP_TYPE dropType = DROP_TYPE.MIDDLE, List<ParamButtonBar> paramBarsList = null)
        {
            List<ButtonGrid.GridEntryData> aiGED = new List<ButtonGrid.GridEntryData>();
            int count = 0;

            foreach (string str in typeNames)
            {
                ButtonGrid.GridEntryData aiGEDEntry;

                aiGEDEntry.mOnString = str;
                aiGEDEntry.mValue = count;
                aiGEDEntry.mClickOn = callBack;
                count++;
                aiGED.Add(aiGEDEntry);
            }

            ParamButtonBar pbb = new ParamButtonBar(aiGED, dropType);

            if(paramBarsList!=null)
            {
                AddParamBar(pbb, paramBarsList);
            }
            else
            {
                Vector2 vPos = new Vector2();
                vPos.Y = ParamButtonBar.PARAM_BAR_YPOS; // + (ParamButtonBar.GAP_AFTER_IGNORE_X + Button.mHeight);
                vPos.X = ParamButtonBar.PARAM_BAR_XPOS_START;
                pbb.Init(vPos);
            }

            return pbb;
        }

        static public void AddParamBar(ParamButtonBar pbb, List<ParamButtonBar> paramBarsList)
        {
            int numBars = paramBarsList.Count;
            Vector2 vPos = new Vector2();
            vPos.Y = ParamButtonBar.PARAM_BAR_YPOS + numBars * (ParamButtonBar.GAP_AFTER_IGNORE_X + Button.mHeight);
            vPos.X = ParamButtonBar.PARAM_BAR_XPOS_START;
            pbb.Init(vPos);
            paramBarsList.Add(pbb);
        }

        static public bool Update(MelodyEditorInterface.MEIState state, ParamButtonBar pbb)
        {

            ParamButtonBar beenSetInBarMode = null;
            if (pbb.Update(state))
            {
                beenSetInBarMode = pbb; //Exit since this has been set 
            }
            //Click off to close param bar
            if (beenSetInBarMode == null && pbb.mBarMode)
            {
                if (state.input.LeftUp)
                {
                    pbb.ShowSelected(state);
                }
            }
     
            return pbb.mBarMode;
        }

        static public bool Update(List<ParamButtonBar> paramBarsList)
        {
            //TODO
            return false;
        }

        //STATIC HELPER FUNCTIONS
        /////////////////////////////////////////////////////////////////////////////////////////////

        public ParamButtonBar(List<ButtonGrid.GridEntryData> geds, DROP_TYPE dropType = DROP_TYPE.MIDDLE)
        {
            mGEDs = geds;
            mNumButtons = mGEDs.Count;
            bool bEven = 0 == mNumButtons % 2;
            mNumAboveCentre = mNumButtons / 2;

            if(bEven)
            {
                mNumAboveCentre -= 1;
            }

            if(dropType == DROP_TYPE.DOWN)
            {
                mNumAboveCentre = 0;
            }
            else if (dropType == DROP_TYPE.UP)
            {
                mNumAboveCentre = mNumButtons-1;
            }

        }

        public void Exit()
        {
            //do stuff when parent exits
            mBarMode = false;
        }

        int mPostUpdateSelected = -1;

        public void SetPostUpdateSelected(int selectIndex)
        {
            mPostUpdateSelected = selectIndex;
        }

        public void SetSelected(int selectIndex)
        {
            mCurrentButtonSelectIndex = selectIndex;
            foreach (Button button in mButtons)
            {
                if(selectIndex == button.mGED.mValue)
                {
                    mCurrentButton = button;
                    mCurrentButton.SetScreenPosRefreshTags(mvScreenPos);
                    return;
                }
            }
        }
        public int GetSelected()
        {
            return mCurrentButtonSelectIndex;
        }

        public Button GetSelectedButton()
        {
            return mCurrentButton;
        }

        public void Init(Vector2 vScreenPos)
        {
            mvScreenPos = vScreenPos; //position is of the centre button on the bar will extend above and below

            Vector2 screenPos = new Vector2();

            mRectSelected = new Rectangle((int)mvScreenPos.X - SELECTED_GAP_ROUND_BUTTON, (int)mvScreenPos.Y - SELECTED_GAP_ROUND_BUTTON, SELECTED_WIDTH, SELECTED_HEIGHT);

            float barStart = mvScreenPos.Y - Button.mBarHeight * mNumAboveCentre;

            int buttCount = 0;
            foreach (ButtonGrid.GridEntryData ged in mGEDs)
            {
                screenPos.X = mvScreenPos.X;
                screenPos.Y = barStart + (Button.mBarHeight * buttCount);
                Button but = new Button(screenPos, "PlainWhite", ged);

                but.ColOn = new Color(0.3f, 0.4f, 0.1f);
                but.ColOff = but.ColOn;
                but.ButtonTextColour = Color.Yellow;
                but.ButtonText = ged.mOnString;
                but.mType = Button.Type.Bar;
                but.mbDrawBorder = true;

                //but.AddTag(Color.Red, "T"+ buttCount.ToString());

                mButtons.Add(but);
                buttCount++;
            }

            mCurrentButton = mButtons[mCurrentButtonSelectIndex];
            mCurrentButton.SetScreenPosRefreshTags(mvScreenPos);
            mBarMode = false;
        }

        void ResetBarPositions()
        {
            Vector2 screenPos = new Vector2();

            int buttCount = 0;
            float barStart = mvScreenPos.Y - Button.mBarHeight * mNumAboveCentre;
            foreach (Button button in mButtons)
            {
                screenPos.X = mvScreenPos.X;
                screenPos.Y = barStart + (Button.mBarHeight * buttCount);
                button.SetScreenPosRefreshTags(screenPos);
                buttCount++;
            }
        }

        public void LoadContent(ContentManager contentMan, SpriteFont font)
        {
            mFont = font;
            mGridButtonTex = contentMan.Load<Texture2D>("PlainWhite");
            foreach (Button button in mButtons)
            {
                button.mTexture = mGridButtonTex; // redundant? should have static global text in button
                button.mFont = mFont;
            }
        }

        void ShowBar()
        {
            mBarMode = true;
            ResetBarPositions();
        }

        public void ShowSelected(MelodyEditorInterface.MEIState state) //used by external stuff to clear bar if other param selected
        {
            if(mScrollValMax > 0)
            {
                if (mCurrentButton.mGED.mValue == (mScrollValMax - 1))
                {
                    mScrollMode = true;
                    InitScrollWheelDiff(state.input);
                }
                else
                {
                    mScrollButton.ButtonText = mScrollButtonText;
                    mScrollMode = false;
                }
            }

            System.Diagnostics.Debug.Assert(mCurrentButton != null);
            mBarMode = false;
            mCurrentButton.SetScreenPosRefreshTags(mvScreenPos);
            SetSelected(mCurrentButton.mGED.mValue);
        }


        //////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////
        ////// FOR SAVE SWITCHING
        public void SetButtonText(int index, string text)
        {
            System.Diagnostics.Debug.Assert(index < mButtons.Count);

            Button but = mButtons[index];
            but.ButtonText = text;
            but.ColOn = new Color(0.2f, 0.2f, 0.06f);
            but.ColOff = but.ColOn;
        }

        public void ResetButtonsToGED()
        {
            int buttCount = 0;
            foreach (ButtonGrid.GridEntryData ged in mGEDs)
            {
                Button but = mButtons[buttCount];

                but.ColOn = new Color(0.3f, 0.4f, 0.1f);
                but.ColOff = but.ColOn;
                but.ButtonTextColour = Color.Yellow;
                but.ButtonText = ged.mOnString;
                but.mbDrawBorder = true;
                but.ClearTags();

                buttCount++;
            }
        }

        public void ResetButtonToGED(int index)
        {
            if(index< mGEDs.Count)
            {
                ButtonGrid.GridEntryData ged = mGEDs[index];
                Button but = mButtons[index];
                but.ColOn = new Color(0.3f, 0.4f, 0.1f);
                but.ColOff = but.ColOn;
                but.ButtonTextColour = Color.Yellow;
                but.ButtonText = ged.mOnString;
                but.mbDrawBorder = true;
                but.ClearTags();
            }
        }

        ///////////////////////////////////
        ///////////////////////////////////
        ///////////////////////////////////
        /// SCROLL WHEEL
        ///////////////////////////////////
        ///////////////////////////////////
        ///////////////////////////////////
        public bool mScrollMode = false; //when a scollable option is selected
        int mLastScrollWheelValue = 0;
        int mScrollValMax = 0; //
        int mScrollValCurrent = 0; //
        string mScrollButtonText = "";

        public void InitScroll()
        {
            mScrollValMax = mButtons.Count; //Always the last button that is scroll select

            mScrollButton = mButtons[mScrollValMax - 1];
            mScrollButton.ColOn = new Color(0.4f, 0.4f, 0.4f);
            mScrollButton.ColOff = mScrollButton.ColOn;
            mScrollButtonText = mScrollButton.ButtonText;
         }

        public void SetButtonColours(bool addNumbers = false)
        {
            ColorGenerator cg = new ColorGenerator(); ;
            int data = 0;
            foreach (Button but in mButtons)
            {
                if(addNumbers)
                {
                    List<int> ld = new List<int>();
                    ld.Add(data);
                    but.AddTag(cg.Next(), data.ToString(), ld);
                    data++;
                }
                else
                {
                    but.AddTag(cg.Next());
                }
            }
        }

        void InitScrollWheelDiff(PsiKlopsInput input)
        {
            mScrollValCurrent = 0;
            mLastScrollWheelValue = input.ScrollWheelValue;
        }

        int GetScrollWheelDiff(PsiKlopsInput input, int currentChangeValue)
        {
            // https://stackoverflow.com/questions/7753123/why-is-wheeldelta-120
            int currentScrollWheelValue = input.ScrollWheelValue;

            if (currentScrollWheelValue != mLastScrollWheelValue)
            {
                int diffScrollWheel = mLastScrollWheelValue - currentScrollWheelValue;
                diffScrollWheel = diffScrollWheel / 120;
                currentChangeValue += diffScrollWheel;
                mLastScrollWheelValue = currentScrollWheelValue;
            }

            return currentChangeValue;
        }

        public bool Update(MelodyEditorInterface.MEIState state)
        {
            if(mScrollMode)
            {
                int newVal = GetScrollWheelDiff(state.input, mScrollValCurrent);

                if(mScrollValCurrent != newVal && newVal >= 0 && newVal < (mScrollValMax-1) ) //dont allow showing the scroll button itself!
                {
                    Button thisButton = mButtons[newVal];

                    mScrollButton.ButtonText = "Scr: " + thisButton.ButtonText;
                    mScrollValCurrent = newVal;

                    thisButton.mGED.mClickOn(thisButton, state);
                }
            }
            if(mBarMode)
            {
                foreach (Button button in mButtons)
                {
                    if (button.Update(state))
                    {
                        mCurrentButton = button;
                        ShowSelected(state);
                        return false;
                    }
                }
            }
            else if (mCurrentButton != null)
            {
                if(mCurrentButton.Update(state))
                {
                    ShowBar();
                    return true;
                }
            }

            //For when call back does something hacky and resets the selected button this should switch t oit after the button.Update above overrides it!
            if(mPostUpdateSelected>=0)
            {
                SetSelected(mPostUpdateSelected);
                ShowSelected(state);
                mPostUpdateSelected = -1;
            }

            return false;
        }

        public void Draw(SpriteBatch sb)
        {
            sb.Begin();
            sb.FillRectangle(mRectSelected, Color.Black);
            sb.End();
            if(mBarMode)
            {
                foreach (Button button in mButtons)
                {
                    if (button == mCurrentButton)
                    {
                        mCurrentButton.Draw(sb, Color.DarkBlue);
                    }
                    else
                    {
                        button.Draw(sb);
                    }
                }
            }
            else if(mCurrentButton!=null)
            {
                mCurrentButton.Draw(sb);
            }
        }

    }
}
