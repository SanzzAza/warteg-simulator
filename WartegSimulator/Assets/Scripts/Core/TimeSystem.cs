using System;
using UnityEngine;

namespace Warteg
{
    /// <summary>
    /// Jam & kalender dalam game. Satu hari dimulai pukul 06:00 dan tutup pukul 23:00.
    /// Kecepatan waktu bisa diatur pemain lewat menu Settings.
    /// </summary>
    public class TimeSystem
    {
        public const float MinutesPerRealSecond = 2.2f;   // 1 detik nyata = 2.2 menit game

        public int Day { get; private set; }
        public float Hour { get; private set; }
        public Weather Weather { get; private set; }
        public bool IsOpen { get { return Hour >= Catalog.OpenHour && Hour < Catalog.CloseHour; } }

        public event Action OnHourChanged;
        public Action OnClockHook;      // dipanggil GameManager setiap jam berubah
        public event Action OnPhaseChanged;
        public event Action OnWeatherChanged;
        public event Action<int> OnDayStarted;
        public event Action<int> OnDayEnded;

        DayPhase lastPhase;
        int lastHour = -1;
        System.Random rng;

        public TimeSystem(int day, float hour, Weather weather)
        {
            Day = day;
            Hour = Mathf.Clamp(hour, Catalog.OpenHour, Catalog.CloseHour);
            Weather = weather;
            rng = new System.Random(day * 977 + 13);
            lastPhase = Catalog.Phase(Hour);
            lastHour = Mathf.FloorToInt(Hour);
        }

        public DayPhase Phase { get { return Catalog.Phase(Hour); } }

        public string ClockText()
        {
            int h = Mathf.FloorToInt(Hour);
            int m = Mathf.FloorToInt((Hour - h) * 60f);
            return h.ToString("00") + ":" + (m / 5 * 5).ToString("00");
        }

        public string DayOfWeekName()
        {
            string[] days = { "Senin", "Selasa", "Rabu", "Kamis", "Jumat", "Sabtu", "Minggu" };
            return days[(Day - 1) % 7];
        }

        /// <summary>Dipanggil GameManager setiap frame (hanya saat tidak pause).</summary>
        public void Advance(float realDelta, float timeScale)
        {
            if (!IsOpen) return;
            float hoursPerSecond = (MinutesPerRealSecond * timeScale) / 60f;
            Hour += realDelta * hoursPerSecond;
            if (Hour > Catalog.CloseHour) Hour = Catalog.CloseHour;

            int hourNow = Mathf.FloorToInt(Hour);
            if (hourNow != lastHour)
            {
                lastHour = hourNow;
                if (OnHourChanged != null) OnHourChanged();
                if (OnClockHook != null) OnClockHook();
                // cuaca bisa berubah siang hari
                if (hourNow == 12 && rng.NextDouble() < 0.35) RollWeather(true);
            }

            DayPhase p = Catalog.Phase(Hour);
            if (p != lastPhase)
            {
                lastPhase = p;
                if (OnPhaseChanged != null) OnPhaseChanged();
            }

            if (Hour >= Catalog.CloseHour)
            {
                if (OnDayEnded != null) OnDayEnded(Day);
            }
        }

        public void RollWeather(bool midDay)
        {
            double r = rng.NextDouble();
            Weather w;
            if (midDay) w = r < 0.55 ? Weather.Cerah : (r < 0.82 ? Weather.Mendung : Weather.Hujan);
            else w = r < 0.5 ? Weather.Cerah : (r < 0.78 ? Weather.Mendung : Weather.Hujan);
            if (w != Weather)
            {
                Weather = w;
                if (OnWeatherChanged != null) OnWeatherChanged();
            }
        }

        /// <summary>Mulai hari baru (dipanggil setelah laporan harian ditutup).</summary>
        public void StartNextDay()
        {
            Day++;
            Hour = Catalog.OpenHour;
            lastHour = Mathf.FloorToInt(Hour);
            lastPhase = Catalog.Phase(Hour);
            rng = new System.Random(Day * 977 + 13);
            RollWeather(false);
            if (OnDayStarted != null) OnDayStarted(Day);
            if (OnHourChanged != null) OnHourChanged();
        }

        public float HourFloat { get { return Hour; } }

        public void SetHour(float h)
        {
            Hour = Mathf.Clamp(h, Catalog.OpenHour, Catalog.CloseHour);
            lastHour = Mathf.FloorToInt(Hour);
        }
    }
}
