﻿using FishingFunBot.Bot.Interfaces;
using log4net;
using System;

namespace FishingFun
{
    public partial class PixelClassifier : IPixelClassifier
    {
        private static ILog logger = LogManager.GetLogger("Fishbot");

        public double ColourMultiplier { get; set; } = 0.5;
        public double ColourClosenessMultiplier { get; set; } = 2.0;

        public bool IsMatch(byte red, byte green, byte blue)
        {
            if (Mode == ClassifierMode.Red)
            {
                return isBigger(red, green) && isBigger(red, blue) && areClose(blue, green);
            }
            else
            {
                return isBigger(blue, green) && isBigger(blue, red) && areClose(red, green);
            }
        }

        public ClassifierMode Mode { get; set; } = ClassifierMode.Red;

        public void SetConfiguration(bool isWowClasic)
        {
            if (isWowClasic)
            {
                LogManager.GetLogger("Fishbot").Info("Wow Classic configuration");
                ColourMultiplier = 1;
                ColourClosenessMultiplier = 1;
            }
            else
            {
                LogManager.GetLogger("Fishbot").Info("Wow Standard configuration");
            }
        }

        private bool isBigger(byte red, byte other)
        {
            return (red * ColourMultiplier) > other;
        }

        private bool areClose(byte color1, byte color2)
        {
            byte max = Math.Max(color1, color2);
            byte min = Math.Min(color1, color2);

            return min * ColourClosenessMultiplier > max - 20;
        }
    }
}