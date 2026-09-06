using System;
using System.Globalization;
using UnityEngine;

namespace Jimmy.Run
{
    public interface IBestTimeStore
    {
        double? Best { get; }
        bool Record(double seconds);
    }

    public sealed class LocalBestTimes : IBestTimeStore
    {
        private readonly string key;
        public LocalBestTimes(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A level-specific score key is required.", nameof(key));
            this.key = key;
        }
        public double? Best
        {
            get
            {
                string stored = PlayerPrefs.GetString(key, "");
                return double.TryParse(stored, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) &&
                    Valid(value) ? value : (double?)null;
            }
        }
        public bool Record(double seconds)
        {
            if (!Valid(seconds) || (Best is double best && seconds >= best)) return false;
            PlayerPrefs.SetString(key, seconds.ToString("R", CultureInfo.InvariantCulture));
            PlayerPrefs.Save(); return true;
        }
        private static bool Valid(double seconds) => seconds > 0 && !double.IsNaN(seconds) && !double.IsInfinity(seconds);
    }

    public sealed class MemoryBestTimes : IBestTimeStore
    {
        public double? Best { get; private set; }
        public bool Record(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds) || (Best is double best && seconds >= best)) return false;
            Best = seconds; return true;
        }
    }
}
