using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace MonoMusicMaker
{
    public class Button
    {
        public delegate bool ButtonLeftRightSlide(MelodyEditorInterface.MEIState state, int stepVal);
        public delegate bool ButtonDoubleClick(MelodyEditorInterface.MEIState state);
        public delegate bool ButtonClick(MelodyEditorInterface.MEIState state);
        public delegate bool ButtonClickOn(MelodyEditorInterface.MEIState state);
        public delegate bool ButtonClickOff(MelodyEditorInterface.MEIState state);
        public Action<int> mClickOn;

        public MelodyEditorInterface.UIButtonMask mMask = MelodyEditorInterface.UIButtonMask.UIBM_ALL;

        public interface ITextureRectangle
        {
            Rectangle GetRectangle(int value);
            Texture2D GetTextureSheet();
            int GetNumSubDivisions();
        }


        ////////////////////////////////////////////////////////////////////////////
        /// BUTTON TAGS - Used to add information over a button and can added/removed depending on some state - e.g. key information
        List<ButtonTag> mTags = new List<ButtonTag>();
        public class ButtonTag
        {
            public ButtonTag(Button parent, Color col, string name)
            {
                mParent = parent;
                mName = name;
                mColor = col;
                if(mColor == Color.Blue)
                {
                    textCol = Color.Yellow; //I know - just that blue is so darn dark for some reason!
                }
            }
            public void Draw(SpriteBatch sb)
            {
                sb.FillRectangle(mRect, mColor);
                if (!string.IsNullOrEmpty(mName))
                {
                    int stringHeight = (int)mParent.mFont.MeasureString(mName).Y;
                    int stringWidth = (int)mParent.mFont.MeasureString(mName).X;

                    var x = ((mRect.X + (mRect.Width / 2)) - (mParent.mFont.MeasureString(mName).X / 2));
                    var y = (mRect.Y + (mRect.Height / 2)) - (stringHeight / 2);
                    sb.DrawString(mParent.mFont, mName, new Vector2(x, y), textCol );
                }
            }
            public List<int> mData = new List<int>();
            public Button mParent;
            public Rectangle mRect;
            public Color mColor;
            Color textCol = Color.Black;
            public string mName;
        }
        public void AddTag(Color col, string name = "", List<int> data = null)
        {
            //6 tags possible along the bottom of the buttom start at RHS
            int widthTag = Rectangle.Width / 6;
            int heightTag = Rectangle.Height / 2;
            int tagNum = mTags.Count;
            
             ButtonTag bt = new ButtonTag(this, col, name);

            if (data != null)
            {
                bt.mData = data;
            }
            int xpos = Rectangle.X +  Rectangle.Width - (widthTag+ widthTag * tagNum);
            int ypos = Rectangle.Y + heightTag;

            bt.mRect = new Rectangle(xpos, ypos, widthTag, heightTag);
            mTags.Add(bt);
        }

        public void CopyButtonTags(Button other)
        {
            mTags = new List<ButtonTag>();

            foreach(ButtonTag bt in other.mTags)
            {       
                AddTag(bt.mColor, String.Copy(bt.mName), new List<int>(bt.mData));
            }
        }

        public bool DataExists(List<int> data )
        {
            foreach(ButtonTag bt in mTags)
            {
                if(bt.mData.Count == data.Count)
                {
                    bool bFail = false;
                    for (int i=0;i<data.Count;i++)
                    {
                        if(data[i] != bt.mData[i])
                        {
                            bFail = true;
                            break;
                        }
                    }
                    if(!bFail)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Color GetTopTagCol()
        {
            Color retCol = Color.Green;

            if(mTags.Count>0)
            {
                retCol = mTags[mTags.Count-1].mColor;
            }

            return retCol;
        }

        public void SetScreenPosRefreshTags(Vector2 screenPos)
        {
            mScreenPos = screenPos;
            RefreshTags();
        }

        public void RefreshTags()
        {
            foreach (ButtonTag bt in mTags)
            {
                bt.mRect.Y = Rectangle.Y + Rectangle.Height / 2;
            }
        }

        public void DrawTags(SpriteBatch sb)
        {
            foreach(ButtonTag bt in mTags)
            {
                bt.Draw(sb);
            }
        }
        public void ClearTags()
        {
            mTags.Clear();
        }

        List<Button> mToggleOffButtons = new List<Button>(); //If any exist then these other buttons get turned off when this is turned on

        public enum Type
        {
            Default,
            Tick,
            Beat, //new type that can vary depending if it is triplet beat
            Wide,
            Narrow,
            Label,
            Bar,
        }
        public Type mType = Type.Default;

        void SetDimensionForType(ref int iWidth, ref int iHeight)
        {
            switch(mType)
            {
                case Type.Tick:
                    iWidth = mTickBoxSize;
                    iHeight = mTickBoxSize;
                    break;
                case Type.Beat:
                    if (mbTripletMode)
                    {
                        iWidth = mTripletTickBoxSize;
                        iHeight = mTickBoxSize;
                    }
                    else
                    {
                        iWidth = mTickBoxSize;
                        iHeight = mTickBoxSize;
                    }
                    break;
                case Type.Wide:
                    iWidth = mWideWidth;
                    break;
                case Type.Narrow:
                    iWidth = mNarrowWidth;
                    break;
                case Type.Label:
                    iWidth = mLabelWidth;
                    break;
                case Type.Bar:
                    iHeight = mBarHeight;
                    break;
            }
        }

        public const int mTickBoxSize = (int)(MainMusicGame.SCALE_SCREEN * 80);
        public const int mHalfTickBoxSize = (int)(mTickBoxSize / 2);

        public const int mTripletTickBoxSize = (int)(mTickBoxSize*0.6666666f);
        public const int mHalfTripletTickBoxSize = (int)(mTripletTickBoxSize / 2);

        public const int mNarrowWidth = (int)(MainMusicGame.SCALE_SCREEN * 120);
        public const int mWidth = (int)(MainMusicGame.SCALE_SCREEN * 220);
        public const int mWideWidth = (int)(MainMusicGame.SCALE_SCREEN * 900);
        public const int mLabelWidth = MainMusicGame.BOX_ART_WIDTH;
        public const int mHeight = (int)(MainMusicGame.SCALE_SCREEN * 100);
        public const int mBarHeight = (int)(MainMusicGame.SCALE_SCREEN * 50);



        /////////////////////////////////////////////////////////////////////////
        /// SLIDE UPDATING vvvvv
        ////////////////////////////////////////////////////////////////////////
        ///

        public int GetSlideValue() { return mSlideRender != null ? mSlideRender.mNumBars : 0; }
        public void SetSlideValue(int value)
        {
            if(SlideLeftRightCB==null)
            {
                //ASSERT?
                return;
            }
            if(mSlideRender==null)
            {
                 mSlideRender = new SlideRender(this);
                 mSlideRender.SetSliderMode(mbSlideMode);
            }
            mSlideRender.mNumBars = value;
        }

        class SlideRender
        {
            public const int NUM_LEVEL_BARS = 2;
            public const float SCALE = 3f;

            public int mNumBars = 0;
            public bool mbSlideMode = false; //only render large rect and bars if ths set, otherwise the small bars
            Button mParent;
            Rectangle mRect; // going to be a larger rectangle drawn on screen centred and surrounding the button held so you can see it - especially useful for touch screen use when the button is held under the finger and probably obscured from view

            Rectangle [] mLeftBars = new Rectangle[NUM_LEVEL_BARS];
            Rectangle [] mRightBars = new Rectangle[NUM_LEVEL_BARS];

            public SlideRender(Button parent)
            {
                mParent = parent;

                SetSideBars( SCALE);
            }

            void SetSideBars(float scale)
            {
                int w = (int)(mParent.Rectangle.Width * scale);
                int h = (int)(mParent.Rectangle.Height * scale);

                int xOffset = (int)((float)w / 2f - (float)mParent.Rectangle.Width / 2f);
                int yOffset = (int)((float)h / 2f - (float)mParent.Rectangle.Height / 2f);
                int x = mParent.Rectangle.X - xOffset;
                int y = mParent.Rectangle.Y - yOffset;

                mRect = new Rectangle(x, y, w, h);

                int levelBarWidth = w / 6;
                int levelBarHeight = h;
                int levelBarLeftX = x - levelBarWidth;
                int levelBarRightX = x + w;
                int levelBarY = y;

                for (int i = 0; i < NUM_LEVEL_BARS; ++i)
                {
                    mLeftBars[i] = new Rectangle(levelBarLeftX, levelBarY, levelBarWidth, levelBarHeight);
                    mRightBars[i] = new Rectangle(levelBarRightX, levelBarY, levelBarWidth, levelBarHeight);

                    levelBarLeftX -= levelBarWidth;
                    levelBarRightX += levelBarWidth;
                }
            }

            public void SetSliderMode(bool mode)
            {
                float scale = mode ? SCALE : 1.0f;
                SetSideBars(scale);
                mbSlideMode = mode;
            }

            public void Draw(SpriteBatch sb)
            {
                if(!mParent.mbOn)
                {
                    return;
                }
                Color[] arrayCol = { Color.Cyan, Color.Magenta };

                int barIndex = 0;

                if(mbSlideMode)
                {
                    sb.FillRectangle(mRect, mParent.ColOn);
                }
                else if(mNumBars==0)
                {
                    return; //no need to draw any bars
                }

                if (mNumBars > 0)
                {
                    foreach (Rectangle r in mRightBars)
                    {
                        if(barIndex< mNumBars)
                        {
                            sb.FillRectangle(r, arrayCol[barIndex % 2]);
                        }
                        barIndex++;
                    }
                }
                else if (mNumBars < 0)
                {
                    int absNumBars = Math.Abs(mNumBars);

                    foreach (Rectangle r in mLeftBars)
                    {
                        if (barIndex < absNumBars)
                        {
                            sb.FillRectangle(r, arrayCol[barIndex % 2]);
                        }
                        barIndex++;
                    }
                }
            }
        }

        SlideRender mSlideRender = null;
        public const int SLIDE_TIME = 500; // time before we say button held
        public bool mbSlideMode = false; //Both these for drum beat 
        public bool mbTripletMode = false; //Both these for drum beat 
        public ButtonLeftRightSlide SlideLeftRightCB = null; //if valid then holding the button puts it in a mode where you can slide on it left or right to adjust a value
        public int mSlideTime = -1;
        /////////////////////////////////////////////////////////////////////////
        /// SLIDE UPDATING ^^^^^
        ////////////////////////////////////////////////////////////////////////

        public bool mbDrawBorder = false;

        public ButtonDoubleClick DoubleClickCB = null;
        public ButtonClick ClickCB;
        ButtonClickOn mOnCallBack;
        ButtonClickOff mOffCallBack;
        public ButtonGrid.GridEntryData mGED;

        //Vector2 mScreenPos;
        public static Texture2D mStaticDefaultTexture;

        public Texture2D mTexture;
        Texture2D mTextureToggled; //IF you you use constructor with two names they work as texture on and off see play/stop button in music maker
        Texture2D gridSelectTex;
        string mtextName;
        string mtextNameToggled;
        public bool Hovering { get; set; }

        public bool Visible { get; set; } = true;
        public bool Held { get; set; } = false;
        public bool mbOn = false;
        public bool mbGridSelect = false;
        bool mbGamePadSetSelect = false;
        ITextureRectangle mItextureSheet;
        int mTextSheetID = 0;
        public int mRightTextPos; //used for cursor

        public string BackgroundIdentity; //instaed of a side text identity have it written large alpha'd out in the background
        public string ButtonTextLeft { get; set; }
        public string ButtonText { get; set; }
        public string TextMid { get; set; }
        public string ButtonTextOff { get; set; }
        public Color ButtonTextColour { get; set; }
        public SpriteFont mFont;

        public Color ColOn { set; get; } = Color.LightGoldenrodYellow;
        public Color ColOff { set; get; } = Color.LightGreen;
        public Color ColHeld { set; get; } = Color.DarkGoldenrod;

        bool mbAccessMaskedOut = false; //to let draw show it faded out or something

        void Init()
        {
            mGED.mValue = -1; //Denote it's not valid for default
            ButtonTextColour = Color.DarkCyan;
            mOffsetPos = Vector2.Zero;
        }

        public Button(Vector2 screenPos, string texName, ButtonGrid.GridEntryData ged)
        {
            mScreenPos = screenPos;
            mtextName = texName;
            ButtonText = WordHelper.GetWords(ged.mOnString, 54);  //ged.mOnString;
            Init();
            mGED = ged;
        }

        public Button(bool bOn = false)
        {
            mbOn = bOn;
            mScreenPos = new Vector2();
            mtextName = null;
            mtextNameToggled = null;
            Init();
        }

        public Button(Vector2 screenPos, string texName = null, string toggleTexName = null)
        {
            mScreenPos = screenPos;
            mtextName = texName;
            mtextNameToggled = toggleTexName;
            Init();
        }

        public Button(Vector2 screenPos, ITextureRectangle texSheet, int texSheetID)
        {
            mScreenPos = screenPos;
            mTextSheetID = texSheetID;
            mItextureSheet = texSheet;
            Init();
        }

        public void SetCallbacks(ButtonClickOn onCallBack, ButtonClickOff offCallBack)
        {
            mOnCallBack = onCallBack;
            mOffCallBack = offCallBack;
        }

        public Vector2 mOffsetPos { get; set; }
        public Vector2 mScreenPos { get; set; }
        public Rectangle Rectangle
        {
            get
            {
                int iWidth = mWidth;
                int iHeight = Button.mHeight;
                SetDimensionForType(ref iWidth, ref iHeight);
                Vector2 vActual = mOffsetPos + mScreenPos;

                return new Rectangle((int)vActual.X, (int)vActual.Y, iWidth, iHeight);
            }
        }
 
        public bool ClickCallBack(MelodyEditorInterface.MEIState state, bool bOn )
        {
           if(bOn)
            {
                if(mOnCallBack!=null)
                {
                    mOnCallBack(state);
                }
            }
            else
            {
                if (mOffCallBack != null)
                {
                    mOffCallBack(state);
                }
            }

            return false;
        }

        public void SetFontDefaultTexture(SpriteFont font)
        {
            mFont = font;
            mTexture = mStaticDefaultTexture;
        }
        public void LoadContent(ContentManager contentMan, SpriteFont font)
        {
            mFont = font;
            if (mItextureSheet == null)
            {
                mTexture = contentMan.Load<Texture2D>(@mtextName);
            }

            if(mtextName=="PlainWhite")
            {
                mStaticDefaultTexture = mTexture;
            }

            gridSelectTex = contentMan.Load<Texture2D>(@"WhiteCircleSelect"); //TODO load is repeated from board, refactor this

            if (mtextNameToggled != null)
            {
                mTextureToggled = contentMan.Load<Texture2D>(mtextNameToggled);
            }
        }

        public void AddMutualToggle(Button but)
        {
            mToggleOffButtons.Add(but);
        }

        public bool InRange(PsiKlopsInput psiKlopsInput)
        {
            if (mtextName == null && mItextureSheet == null)
            {
                return true; //secial case when we dont care where button is just want to get mouse click
            }

            System.Diagnostics.Debug.Assert(mScreenPos != null);

            //Hovering = false;

            if (psiKlopsInput.mRectangle.Intersects(Rectangle))
            {
                // Hovering = true;
                return true;
            }

            //int startx  = (int)mScreenPos.X;
            //int endx    = (int)mScreenPos.X + mWidth;
            //int starty  = (int)mScreenPos.Y;
            //int endy    = (int)mScreenPos.Y + mHeight;
            //if (pos.X > startx &&
            //     pos.X < endx &&
            //     pos.Y > starty &&
            //     pos.Y < endy)
            //{
            //    //mbToggle = !mbToggle;
            //    return true;
            //}

            return false;

        }

        public void GamePadSet()
        {
            mbGamePadSetSelect = true;
        }

        bool GetGamePadSelectSet()
        {
            bool bSet = mbGamePadSetSelect;

            mbGamePadSetSelect = false; //clear this 

            return bSet;
        }

        public bool UpdateMask(MelodyEditorInterface.MEIState state)
        {
            //mCurrentButtonMaskedOff
            if ((mMask & state.mCurrentButtonMaskedOff) == mMask)
            {
                mbAccessMaskedOut = true;
                return true;
            }

            if ((mMask & state.mCurrentButtonUpdate) == MelodyEditorInterface.UIButtonMask.UIBM_None)
            {
                mbAccessMaskedOut = true;
                return true;
            }
            return false;
        }

        public bool UpdateMaskToSetButton(MelodyEditorInterface.MEIState state)
        {
            if ((mMask & state.mMaskToSetButton) == mMask)
            {
                state.mMaskToSetButton = MelodyEditorInterface.UIButtonMask.UIBM_None; //clear this since it was destined for us
                return true;
            }
            return false;
        }

        public bool Update(MelodyEditorInterface.MEIState state)
        {
            if(!Visible)
            {
                return false;
            }
            PsiKlopsInput psiKlopsInput = state.input;

            if (UpdateMaskToSetButton(state))
            {
                mbOn = !mbOn;
                return true;
            }

            if (UpdateMask(state))
            {
                return false;
            }

            mbAccessMaskedOut = false;


            /////////////////////////////////////////////
            /// SPECIAL INPUT STUFF FOR THE TEXT ENTRY 
            /// BASICALLY JUST DETECTS IF CLICKED ON
            /////////////////////////////////////////////
            if (mGED.mValue == ButtonGrid.GridEntryData.TEXT_TYPE)
            {
                if (psiKlopsInput.LeftButton == ButtonState.Pressed)
                {
                    if (InRange(psiKlopsInput))
                    {
                        Held = true; 
                    }
                    else
                    {
                        Held = false;
                    }
                }
                if (Held && psiKlopsInput.LeftButton == ButtonState.Released)
                {
                    if (InRange(psiKlopsInput))
                    {
                        Held = false;
                        return true;
                    }
                }
                //No update just display
                return false;
            }
            /////////////////////////////////////////////
            /// SPECIAL INPUT STUFF FOR THE TEXT ENTRY 
            /// BASICALLY JUST DETECTS IF CLICKED ON
            /////////////////////////////////////////////

            if (GetGamePadSelectSet())
            {
                mbOn = !mbOn;
                return true;
            }

            bool on = mbOn;

            if (DoubleClickCB != null)
            {
                if (state.input.DoubleTapInRect(Rectangle, true)) //if call back called then consume the info
                {
                    Held = false;
                    DoubleClickCB(state);
                    return true;
                }
            }

            if (psiKlopsInput.LeftButton == ButtonState.Pressed)
            {
                if (InRange(psiKlopsInput))
                {
                    Held = true; //TODO why have 'Held' in button as well as input?
                }
                else
                {
                    Held = false;
                }
            }

           if(SlideLeftRightCB != null )
            {
                if (Held)
                {
                    if (mSlideTime == -1)
                    {
                        //first time we see the held consition
                        mSlideTime = state.mCurrentGameTimeMs;
                    }
                    else
                    {
                        if(state.mCurrentGameTimeMs> mSlideTime+ SLIDE_TIME)
                        {
                            if(mSlideRender==null)
                            {
                                mSlideRender = new SlideRender(this);
                            }
                            mbSlideMode = true;
                            mSlideRender.SetSliderMode( mbSlideMode);
                        }
                    }

                    if(mbSlideMode)
                    {
                        int diffCentre = state.input.X - Rectangle.Center.X;
                        int thirdHalfRectWidth = Rectangle.Width / 6;
                        int stepVal = diffCentre / thirdHalfRectWidth;

                        mSlideRender.mNumBars = stepVal;

                        SlideLeftRightCB(state, stepVal);
                    }             
                }
                else
                {
                    mSlideTime = -1;
                }
             }

            if (psiKlopsInput.LeftButton == ButtonState.Released)
            {
                if(mbSlideMode)
                {
                    //If was in slide mode
                    //then when released we don't
                    //toggle a click
                    mbSlideMode = false;
                    mSlideTime = -1;
                    //mSlideRender = null;

                    if(mSlideRender!=null)
                        mSlideRender.SetSliderMode(mbSlideMode);

                }
                else if (Held && InRange(psiKlopsInput))
                {
                    mbOn = !mbOn;

                    if(mToggleOffButtons.Count!=0)
                    {
                        //we're in a group with other buttons
                        if(mbOn)
                        {
                            //transitioned to on so turn others off
                            foreach(Button but in mToggleOffButtons)
                            {
                                but.mbOn = false;
                            }
                        }
                        else
                        {
                            mbOn = true; //dont allow turning off when pressed
                        }
                    }
                }

                Held = false;
            }

            bool bStateChange = mbOn != on;
            
            if(bStateChange)
            {
                if(mGED.mClickOn != null)
                {
                    mGED.mClickOn(this, state);
                }
                ClickCallBack( state, mbOn);
                if(ClickCB!=null)
                {
                    state.mButCallback = this;

                    ClickCB(state);
                }
            }

            return bStateChange;  //tell the outside world it has switched
        }

        public void Draw(SpriteBatch sb)
        {
            Color c = mbOn? ColOn : ColOff  ;

            if(Held)
            {
                c = ColHeld;
            }

            Draw(sb, c);
        }

        public void DrawExtBeginEnd(SpriteBatch sb)
        {
            Color c = mbOn ? ColOn : ColOff;

            if (Held)
            {
                c = ColHeld;
            }

            DrawInternalBeginEnd(sb, c);
        }

        void DrawInternalBeginEnd(SpriteBatch spriteBatch, Color? color = null)
        {
            Texture2D texture = mTexture;
            if (mbOn && mTextureToggled != null)
            {
                texture = mTextureToggled;
            }

            Vector2 vActual = mOffsetPos + mScreenPos;
            if (mItextureSheet != null)
            {
                Texture2D texSheet = mItextureSheet.GetTextureSheet();
                Rectangle PieceRectangle = mItextureSheet.GetRectangle(mTextSheetID);

                int iWidth = mWidth;
                int iHeight = Button.mHeight;
                SetDimensionForType(ref iWidth, ref iHeight);
                spriteBatch.Draw(
                    texSheet, new Rectangle(
                        (int)vActual.X,
                        (int)vActual.Y,
                        iWidth,
                        iHeight),
                        PieceRectangle,
                    color.Value);

            }
            else
            {
                if (mType == Type.Label)
                {
                    color *= 0.4f;
                }
                int iWidth = mWidth;
                int iHeight = Button.mHeight;
                SetDimensionForType(ref iWidth, ref iHeight);
                spriteBatch.Draw(texture,
                    new Rectangle((int)vActual.X, (int)vActual.Y,
                        iWidth,
                        iHeight),
                    color.Value);
            }

            if (mbGridSelect)
            {
                int iWidth = mWidth;
                int iHeight = Button.mHeight;
                SetDimensionForType(ref iWidth, ref iHeight);
                Color alphaColor = new Color(1f, 0f, 0f, 1.0f); //green half transparent
                spriteBatch.Draw(gridSelectTex,
                    new Rectangle((int)vActual.X, (int)vActual.Y,
                        iWidth,
                        iHeight),
                    alphaColor);
            }

            if (mbDrawBorder)
            {
                spriteBatch.DrawRectangle(Rectangle, Color.Black);
            }

            string textToUse = "";
            if (BackgroundIdentity != null && BackgroundIdentity != "")
            {
                textToUse = BackgroundIdentity;
                int stringHeight = (int)MainMusicGame.mMassiveFont.MeasureString(textToUse).Y;
                int stringWidth = (int)MainMusicGame.mMassiveFont.MeasureString(textToUse).X;
                var x = GetXposEndStringCentred(textToUse, MainMusicGame.mMassiveFont);
                var y = (Rectangle.Y + (Rectangle.Height / 2)) - (stringHeight / 2);

                spriteBatch.DrawString(MainMusicGame.mMassiveFont, BackgroundIdentity, new Vector2(x, y), Color.Gray * 0.15f);
            }

            textToUse = ButtonText;

            bool bLeftText = false;
            bool bTextMid = false;

            if (!string.IsNullOrEmpty(ButtonTextLeft))
            {
                bLeftText = true;
                textToUse = ButtonTextLeft;
            }
            else if (!string.IsNullOrEmpty(TextMid))
            {
                bTextMid = true;
                textToUse = TextMid;
            }

            if (mbOn && !string.IsNullOrEmpty(ButtonTextOff))
            {
                textToUse = ButtonTextOff;
            }

            if (!string.IsNullOrEmpty(textToUse))
            {
                float fTextAlhpa = 1.0f;
                if (mbAccessMaskedOut)
                {
                    fTextAlhpa = 0f;
                }
                int stringHeight = (int)mFont.MeasureString(textToUse).Y;
                int stringWidth = (int)mFont.MeasureString(textToUse).X;
                var x = GetXposEndStringCentred(textToUse);
                var y = (Rectangle.Y + (Rectangle.Height / 2)) - (stringHeight / 2);
                if (mType == Type.Tick)
                {
                    if (bTextMid)
                    {
                        x = Rectangle.X + mHalfTickBoxSize - stringWidth / 2;
                    }
                    else
                    if (bLeftText)
                    {
                        x = Rectangle.X - mFont.MeasureString(textToUse).X - 10;
                    }
                    else
                    {
                        x = Rectangle.X + mTickBoxSize + 10; // text to right
                    }
                }
                Color textCol = ButtonTextColour;

                mRightTextPos = (int)x + stringWidth;

                spriteBatch.DrawString(mFont, textToUse, new Vector2(x, y), textCol * fTextAlhpa);
            }
            else
            {
                mRightTextPos = (Rectangle.X + (Rectangle.Width / 2));
            }

            if (mSlideRender != null)
            {
                mSlideRender.Draw(spriteBatch);
            }

            //TAGS
            DrawTags(spriteBatch);
        }


        public void Draw(SpriteBatch spriteBatch, Color? color = null)
        {
            if (!Visible)
            {
                return;
            }
            if (color == null)
            {
                color = Color.White;
            }

            spriteBatch.Begin();

            DrawInternalBeginEnd(spriteBatch, color);

            spriteBatch.End();

        }
        public float GetXposEndStringCentred(string testString, SpriteFont font =null)
        {
            if(font==null)
            {
                font = mFont;
            }
            return ((Rectangle.X + (Rectangle.Width / 2)) - (font.MeasureString(testString).X / 2));
        }
    }


    class ButtonPanel
    {
        Vector2 mScreenPos;
        bool mbVisible = false;
        //TODO for now panel is just right to left line fomr the pos for number units define here from text sheet init, could be grid etc
        int mNumButtons;
        int mButtonOnNum;

        bool mbButtonDown = false;
        public bool mbOn = false;
        public bool mbGridSelect = false;
        bool mbGamePadSetSelect = false;
        Button.ITextureRectangle mItextureSheet;
        int mTextSheetID = 0;

        List<Button> m_Buttons;

        public ButtonPanel(Vector2 screenPos, Button.ITextureRectangle texSheet, int buttonNumStart = 0)
        {
            m_Buttons = new List<Button>();

            mNumButtons = texSheet.GetNumSubDivisions();
            mScreenPos = screenPos;
            mButtonOnNum = buttonNumStart;
            mItextureSheet = texSheet;

            Vector2 pos = mScreenPos;

            for(int i=0; i<mNumButtons;i++)
            {
                Button newButton = new Button(pos, mItextureSheet,  i);

                m_Buttons.Add(newButton);
                pos.X += Button.mWidth;
            }
        }
        //Constructor for single texture 
        public ButtonPanel(Vector2 screenPos, string texName, List<String> namesOn, string toggleTexName = null, int buttonNumStart = 0)
        {
            m_Buttons = new List<Button>();
            mScreenPos = screenPos;
            mButtonOnNum = buttonNumStart;
            mNumButtons = namesOn.Count;

            Vector2 pos = mScreenPos;

            for (int i = 0; i < mNumButtons; i++)
            {
                Button newButton = new Button(pos, texName, toggleTexName);
                newButton.ButtonText = namesOn[i];

                m_Buttons.Add(newButton);
                pos.X += Button.mWidth;
            }
        }

        public void SetVisible(bool value)
        {
            mbVisible = value;
        }

        public bool Update(MelodyEditorInterface.MEIState state)
        {
            if (!mbVisible)
            {
                return false;
            }
            for (int i= 0; i < m_Buttons.Count; i ++  )
            {
                Button b = m_Buttons[i];
                if(b.Update(state))
                {
                    mButtonOnNum = i;
                    return true;
                }
            }
            return false;
        }

        public int GetCurrentIntegerSelected()
        {
            return mButtonOnNum;
        }

        public void Draw(SpriteBatch spriteBatch, Color? color = null)
        {
            if(!mbVisible)
            {
                return;
            }
            foreach(Button b in m_Buttons)
            {
                b.Draw(spriteBatch);
            }
        }
    }
}
