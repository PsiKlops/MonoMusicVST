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

namespace MonoMusicMaker
{
    public class ButtonGrid
    {
        const int NUM_WIDTH_MAX = 5;
        const int NUM_HEIGHT_MAX = 5;
        const int MAX_BUTTONS = NUM_WIDTH_MAX * NUM_HEIGHT_MAX;
        public const int GRID_START_X = 30;
        const int GRID_START_Y = MelodyEditorInterface.TRACK_START_Y; //30;
        const int GRID_GAP_X = MelodyEditorInterface.TRACK_START_X; //30;
        public const int GRID_GAP_Y = 30;

        public enum GRID_TYPE {
            TWO_D_GRID,
            UP_DOWN_GRID
        };

        public static Color DefaultTextCol { get; set; } = Color.LightBlue;
        public static Color DefaultOnCol { get; set; } = new Color(0.3f, 0.3f, 0.6f);  //Color.OrangeRed;
        public static Color DefaultOffCol { get; set; } = new Color(0.1f, 0.1f, 0.2f);
        bool Active { get; set; } = false;

        static public SpriteFont    mFont;
        static public Texture2D     mGridButtonTex;

        public struct GridEntryData
        {
            public const int BACK_TYPE = -1;
            public const int EXIT_TYPE = -2;
            public const int TEXT_TYPE = -3;
            public int mValue;
            public string mOnString;

            public Action<Button, MelodyEditorInterface.MEIState> mClickOn;
        };

        protected Button mExitButton = null;
        protected int mGridHeight = 0;

        GRID_TYPE mGridType;

        public List<Button> mButtons = new List<Button>();

        //COuld be several types with colum and or heading types
        //but for now just blat out all selections in a rectangle in middle screen
        //bottom right slection with be the Back/Current slected option
        int mNumEntries = -1;

        public ButtonGrid(List<GridEntryData> entries, GRID_TYPE gt = GRID_TYPE.TWO_D_GRID)
        {
            mGridType = gt;

            Vector2 screenPos = new Vector2();

            mNumEntries = entries.Count; //some check if too much here?
            int count = 0;
            int xoffset = GRID_START_X;
            int yoffset = GRID_START_Y;

            int countWidth = NUM_WIDTH_MAX;

            if(gt == GRID_TYPE.UP_DOWN_GRID)
            {
                countWidth = 1;
            }

            foreach (GridEntryData ged in entries)
            {
                int posX = count % countWidth;
                int posY = count / countWidth;
                screenPos.X = posX * (Button.mWidth + GRID_GAP_X) + GRID_START_X;
                screenPos.Y = posY * (Button.mHeight + GRID_GAP_Y) + GRID_START_Y;
                Button but = new Button(screenPos, "PlainWhite", ged);

                if(gt==GRID_TYPE.UP_DOWN_GRID)
                {
                    but.mType = Button.Type.Wide;
                    //but.Wide = true;
                }

                if(ged.mValue== GridEntryData.BACK_TYPE)
                {
                    but.ColOn = new Color(0.1f, 0.1f, 0.25f);
                    but.ColOff = but.ColOn;
                    but.ButtonTextColour = Color.White;
                }
                else if (ged.mValue == GridEntryData.EXIT_TYPE)
                {
                    but.ColOn = new Color(0.1f,0.2f,0.1f);
                    but.ColOff = but.ColOn;
                    but.ButtonTextColour = Color.Yellow;
                    mExitButton = but;
                }
                else
                {
                    but.ColOn = DefaultOnCol;
                    but.ColOff = DefaultOffCol;
                    but.ButtonTextColour = DefaultTextCol;
                }

                mButtons.Add(but);
                count++;
            }

            mGridHeight = entries.Count * (Button.mHeight + GRID_GAP_Y);

            GridEntryData backGed;
            backGed.mOnString = "BACK";
            backGed.mValue = -1;
            backGed.mClickOn = null;
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

        public void InitDefaultTextFont()
        {
            if(Button.mStaticDefaultTexture==null || mFont==null)
            {
                return;
            }
            foreach (Button button in mButtons)
            {
                button.mTexture = Button.mStaticDefaultTexture; // redundant? should have static global text in button
                button.mFont = mFont;
            }
        }

        public virtual Button Update(MelodyEditorInterface.MEIState state)
        {
            foreach(Button button in mButtons)
            {
                if(button.Update(state))
                {
                    return button;
                }
            }
            return null;
        }

        public void ClearAllOnStateExcept(Button but)
        {
            foreach (Button button in mButtons)
            {
                if (but !=button)
                {
                    button.mbOn  =false;
                }
            }
        }
        public void SetOnAndClearAllOthers(Button but)
        {
            if(but!=null)
                but.mbOn = true;
            foreach (Button button in mButtons)
            {
                if (but != button)
                {
                    button.mbOn = false;
                }
            }
        }

        public bool AreAllOthersOff(Button but)
        {
            foreach (Button button in mButtons)
            {
                if (but != button)
                {
                    if(button.mbOn)
                    {
                        return false;
                    }
                }
                //else if(!but.mbOn) //your button is off so no!
                //{
                //    return false;
                //}
            }

            return true; //get here so must be
        }


        public virtual void Draw(SpriteBatch sb)
        {
            foreach (Button button in mButtons)
            {
                button.Draw(sb);
            }
        }
    }
}
