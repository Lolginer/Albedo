using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;

namespace Albedo.Core
{
	public class SunriseEquation
	{
		static string region = RegionInfo.CurrentRegion.TwoLetterISORegionName;
		static double lat = 0; //North latitude (north positive, south negative)
		static double lon = 0; //West longitude (west positive, east negative)
		public static double sunrise;
		public static double sunset;
		public static double startup;

		public static void SetCoordinates() {
			int[] c = { 0, 0 }; //lat, long

			//Get latitude by country centrics
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			Stream stream = assembly.GetManifestResourceStream("Albedo.Resources.latitude.txt");
			StreamReader reader = new StreamReader(stream);
			string geoString = reader.ReadToEnd();
			dynamic geoData = JsonParser.Deserialize(geoString);

			int i = 0;
			foreach (string country in geoData.country) {
				if (region == country) {
					lat = geoData.latitude[i];
					break;
				}
				i++;
			}

			//Get longitude by time zone
			int offset = TimeZoneInfo.Local.BaseUtcOffset.Hours;
			lon = offset * -15; //UTC +12 == longitude -180

			//Set startup time
			if (CurrentJulianDate() - 0.83 > Platform.ReadSetting("startupTime") || CurrentJulianDate() < Platform.ReadSetting("startupTime")) {
				Platform.WriteSetting("startupTime", CurrentJulianDate());
			}
			startup = Platform.ReadSetting("startupTime");
		}

		private static double CurrentJulianDate()
		{
			JulianCalendar cal = new JulianCalendar();
			int isJanFeb = (14 - cal.GetMonth(DateTime.UtcNow)) / 12;
			int years = cal.GetYear(DateTime.UtcNow) + 4800 - isJanFeb;
			int monthsMod = cal.GetMonth(DateTime.UtcNow) + (12 * isJanFeb) - 3;

			double dayNumber = 
				cal.GetDayOfMonth(DateTime.UtcNow) 
				+ Math.Floor((double)(153 * monthsMod + 2) / 5) 
				+ 365 * years + Math.Floor((double)years / 4) - 32083;

			double julianDate = dayNumber 
				+ ((double)cal.GetHour(DateTime.UtcNow) - 12) / 24 
				+ (double)cal.GetMinute(DateTime.UtcNow) / 1440 
				+ (double)cal.GetSecond(DateTime.UtcNow) / 86400;

			return julianDate;
		}

		//Returns time in minutes from local midnight
		public static int JulianToTime(double julian)
		{
			double timeOnly = (julian + 0.5) % 1;
			int timeInMinutes = (int)(timeOnly * 1440) 
				+ (TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours * 60);

			return timeInMinutes;
		}

		const double year2000 = 2451545.0009;

		private static double JulianCycle()
		{
			double step1 = CurrentJulianDate() - year2000 - (lon / 360);
			double step2 = Math.Floor(step1 + 0.5);

			return step2;
		}
		
		private static double SolarNoon()
		{
			return year2000 + (lon / 360) + JulianCycle();
		}

		private static double MeanAnomaly()
		{
			return ((357.5291 + 0.98560028 * (SolarNoon() - year2000)) % 360) * (Math.PI / 180);
		}

		private static double EclipticLongitude()
		{
			double step1 = 
				1.9148 * Math.Sin(MeanAnomaly())
				+ 0.0200 * Math.Sin(2 * MeanAnomaly())
				+ 0.0003 * Math.Sin(3 * MeanAnomaly()); //Equation of center

			double step2 = ((MeanAnomaly() / (Math.PI / 180) + 102.9372 + step1 + 180) % 360) * (Math.PI / 180);

			return step2;
		}

		private static double SolarTransit()
		{
			return SolarNoon() + 0.0053 * Math.Sin(MeanAnomaly()) - 0.0069 * Math.Sin(2 * EclipticLongitude());
		}

		private static double HourAngle()
		{
			double step1 = Math.Sin(EclipticLongitude()) * Math.Sin(0.4093);
			double step2 = Math.Asin(step1); //Declination of the Sun

			double step3 = (Math.Sin(-0.0145) - Math.Sin(lat * (Math.PI / 180)) * step1)
				/ (Math.Cos(lat * (Math.PI / 180)) * Math.Cos(step2));

			return Math.Acos(step3) / (Math.PI / 180); //Output in degrees, not radians
		}

		public static void SunriseSunset()
		{
			sunset = year2000
				+ ((HourAngle() + lon) / 360)
				+ JulianCycle()
				+ 0.0053 * Math.Sin(MeanAnomaly())
				- 0.0069 * Math.Sin(2 * EclipticLongitude());

			sunrise = SolarTransit() - (sunset - SolarTransit());
			JulianToTime(sunrise);
		}
	}
}
