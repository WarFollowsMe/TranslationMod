using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiLanguage
{
    public static class LocalizationBridge
    {
        private static Localization _localization;
        public static Localization Localization
        {
            get
            {
                if (_localization == null) _localization = new Localization();
                return _localization;
            }
            set
            {
                _localization = value;
            }
        }

        public static void ClientSizeChangedCallback()
        {
            Localization.OnWindowsSizeChanged();
        }

        public static void UpdateCallback()
        {
            Localization.OnUpdate();
        }

        public static void ChangeDropDownOptionCallback(int which, int selection, List<string> option)
        {
            Localization.OnChangeLanguage(which, selection, option);
        }

        public static void SetDropDownToProperValueCallback(object dropdown)
        {
            Localization.OnSetDropDownPropertyValue(dropdown as OptionsDropDown);
        }

        public static void LoadedGameCallback()
        {
            Localization.OnGameLoaded();
        }

        public static DetourEvent GetRandomNameCallback()
        {
            var result = Localization.OnGetRandomName();
            if(!string.IsNullOrEmpty(result))
            {
                return new DetourEvent { ReturnValue = result };
            }
            return new DetourEvent();
        }

        public static DetourEvent GetOtherFarmerNamesCallback()
        {
            var result = Localization.OnGetOtherFarmerNames();
            if (result != null && result.Count > 0)
            {
                return new DetourEvent { ReturnValue = result };
            }
            return new DetourEvent();
        }

        public static DetourEvent ParseTextCallback(string text, object whichFont, int width)
        {
            var result = Localization.OnParseText(text, whichFont as SpriteFont, width);
            if (!string.IsNullOrEmpty(result))
            {
                return new DetourEvent { ReturnValue = result };
            }
            else return new DetourEvent();
        }

        public static string DrawObjectDialogueCallback(string dialogue)
        {
            return Localization.OnDrawObjectDialogue(dialogue);
        }

        public static DialogueQuestion DrawObjectQuestionDialogueCallback(string dialogue, List<string> choices)
        {
            return Localization.OnDrawObjectQuestionDialogue(dialogue, choices);
        }

        public static DetourEvent SpriteTextDrawStringCallback(object b, string s, int x, int y, int characterPosition,
            int width, int height, float alpha, float layerDepth, bool junimoText,
            int drawBGScroll, string placeHolderScrollWidthText, int color)
        {
            var @event = new SpriteTextDrawStringEvent(b as SpriteBatch, s, x, y, characterPosition, width, height, alpha, layerDepth, junimoText, drawBGScroll, placeHolderScrollWidthText, color);
            Localization.OnDrawStringSpriteText(@event);
            return @event;
        }

        public static DetourEvent SpriteTextGetWidthOfStringCallback(string text)
        {
            var result = Localization.OnGetWidthSpriteText(text);
            if(result >= 1200)
            {

            }
            if(result != -1)
            {
                var @event = new DetourEvent { ReturnValue = result };
                return @event;
            }
            else return new DetourEvent();
        }

        public static DetourEvent StringBrokeIntoSectionsCallback(string s, int width, int height)
        {
            var result = Localization.OnStringBrokeIntoSections(s, width, height);
            if (result != null && result.Count > 0)
            {
                return new DetourEvent { ReturnValue = result };
            }
            return new DetourEvent();
        }

        public static string SparklingTextCallback(string text)
        {
            var result = Localization.OnSparklingTextCallback(text);
            if (string.IsNullOrEmpty(result)) return text;
            else return result;
        }

        public static void SpriteBatchDrawStringCallback(SpriteBatch batch, SpriteFont spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
        {
            var result = Localization.OnSpriteBatchDrawString(text);
            if (string.IsNullOrEmpty(result))
            {
                batch.DrawString(spriteFont, text, position, color, rotation, origin, scale, effects, layerDepth);
            }
            else
            {
                batch.DrawString(spriteFont, result, position, color, rotation, origin, scale, effects, layerDepth);
            }
        }

        public static void SpriteBatchDrawStringCallback(SpriteBatch batch, SpriteFont spriteFont, string text, Vector2 position, Color color)
        {
            var result = Localization.OnSpriteBatchDrawString(text);
            if (string.IsNullOrEmpty(result))
            {
                batch.DrawString(spriteFont, text, position, color);
            }
            else
            {
                batch.DrawString(spriteFont, result, position, color);
            }
        }

        public static Vector2 SpriteFontMeasureStringCallback(SpriteFont spriteFont, string text)
        {
            var result = Localization.OnSpriteFontMeasureString(text);
            if (string.IsNullOrEmpty(result))
            {
                return spriteFont.MeasureString(text);
            }
            else
            {
                return spriteFont.MeasureString(result);
            }
        }
    }
}
