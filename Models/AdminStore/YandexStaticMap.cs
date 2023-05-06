using NetTopologySuite.Geometries;

namespace YandexStaticMap
{
	public static class YandexStaticMapTools
	{
		public static string GenerateSrcAttributeForMap(List<Point> coordinates, IConfiguration conf)
		{
			string query = conf["YandexStaticMapQuery"]! + "&pt=";

			foreach(Point coordinate in coordinates)
			{
				//Creating mark on map
				query += coordinate.X.ToString() + ',' + coordinate.Y.ToString() + ",pm2dom~";
			}

			return query.TrimEnd('~');
		}
		public static string GenerateSrcAttributeForMap(Point coordinate, IConfiguration conf)
		{
			return GenerateSrcAttributeForMap(new List<Point>() {coordinate}, conf);
		}
	}
}