﻿using System.Drawing;
using HelpersLib.GraphicsHelper;
using ImageEffects.IPlugin;

namespace ImageAdjustment
{
    public class Brightness : IPluginItem
    {
        public override string Name { get { return "Brightness"; } }

        public override string Description { get { return "Brightness"; } }

        private float brightnessValue;

        public float BrightnessValue
        {
            get
            {
                return brightnessValue;
            }
            set
            {
                brightnessValue = value;
                OnPreviewTextChanged(brightnessValue + "%");
            }
        }

        public override Image ApplyEffect(Image img)
        {
            return ColorMatrixMgr.ApplyColorMatrix(img, ColorMatrixMgr.Brightness(BrightnessValue));
        }
    }
}