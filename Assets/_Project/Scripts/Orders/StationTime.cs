using System;

namespace VentsAndVending.Orders
{
    /// <summary>24 real minutes per station day. All scheduling uses the board's saved gameplay clock.</summary>
    public static class StationTime
    {
        public const float DaySeconds=1440f;
        public const int StartHour=8;
        public static double TotalMinutes(float elapsed)=>StartHour*60d+Math.Max(0,elapsed);
        public static bool IsNight(float elapsed){int hour=(int)(TotalMinutes(elapsed)%1440)/60;return hour<6 || hour>=20;}
        public static string Label(float elapsed)
        {
            double minutes=TotalMinutes(elapsed);int day=(int)(minutes/1440)+1;int time=(int)(minutes%1440);
            return $"Day {day} / {time/60:00}:{time%60:00}";
        }
        public static float NextHalfHour(float elapsed)=>Math.Max(elapsed+30,(float)((Math.Floor(TotalMinutes(elapsed)/30)+2)*30-StartHour*60));
    }
}
