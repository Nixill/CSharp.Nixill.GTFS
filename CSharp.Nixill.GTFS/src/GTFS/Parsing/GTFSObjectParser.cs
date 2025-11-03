using System;
using System.Drawing;
using System.Text.RegularExpressions;
using Nixill.GTFS.Collections;
using Nixill.GTFS.Entities;
using Nixill.GTFS.Parsing.Exceptions;
using Nixill.Utils;
using Nixill.Utils.Extensions;
using NodaTime;
using NodaTime.Text;

namespace Nixill.GTFS.Parsing
{
  public static class GTFSObjectParser
  {
    public static void AssertExists(string input, string key)
    {
      if (input == null || input == "") throw new PropertyNullException(key);
    }
    public static void AssertExists(this GTFSPropertyCollection properties, string key) => AssertExists(properties[key], key);

    public static void AssertDoesntExist(string input, string key)
    {
      if (!(input == null || input == "")) throw new PropertyException(key, "Should be null.");
    }
    public static void AssertDoesntExist(this GTFSPropertyCollection properties, string key) => AssertDoesntExist(properties[key], key);

    // Color: A color encoded as a six-digit hexadecimal number. Refer to
    //   https://htmlcolorcodes.com to generate a valid value (the leading
    //   "#" is not included).
    // Example: `FFFFFF` for white, `000000` for black or `0039A6` for the
    //   A, C, E lines in NYMTA.
    public static readonly Regex ColorRegex = new Regex(@"^[0-9a-f]{6}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static Color? GetNullableColor(string input)
    {
      if (input == null) return null;
      if (!ColorRegex.TryMatch(input, out Match match)) return null;
      int col = Convert.ToInt32(input, 16);
      return Color.FromArgb(255, Color.FromArgb(col));
    }
    public static Color? GetNullableColor(this GTFSPropertyCollection properties, string key) => GetNullableColor(properties[key]);

    public static Color GetColor(string input, Color? def = null)
    {
      Color? test = GetNullableColor(input);
      if (test.HasValue) return test.Value;
      if (def.HasValue) return def.Value;
      throw new ArgumentException($"{input ?? "(null)"} could not be parsed to a color.");
    }
    public static Color GetColor(this GTFSPropertyCollection properties, string key, Color? def = null) => GetColor(properties[key], def);

    public static bool IsColor(string input)
    {
      if (input == null) return false;
      return ColorRegex.IsMatch(input);
    }
    public static bool IsColor(this GTFSPropertyCollection properties, string key) => IsColor(properties[key]);

    public static void AssertColor(string input, string key)
    {
      AssertExists(input, key);
      if (!IsColor(input)) throw new PropertyTypeException(key, $"{key} is not a valid color.");
    }
    public static void AssertColor(this GTFSPropertyCollection properties, string key) => AssertColor(properties[key], key);

    // Date: Service day in the `YYYYMMDD` format. Since time within a
    //   service day can be above 24:00:00, a service day often contains
    //   information for the subsequent day(s).
    // Example: `20180913` for September 13th, 2018.
    public static readonly LocalDatePattern DatePattern = LocalDatePattern.CreateWithInvariantCulture("uuuuMMdd");
    public static readonly Regex DateRegex = new Regex(@"^\d{8}$", RegexOptions.Compiled);

    public static LocalDate? GetNullableDate(string input)
    {
      if (input == null) return null;
      ParseResult<LocalDate> res = DatePattern.Parse(input);
      if (res.Success) return res.Value;
      else return null;
    }
    public static LocalDate? GetNullableDate(this GTFSPropertyCollection properties, string key) => GetNullableDate(properties[key]);

    public static LocalDate GetDate(string input, LocalDate? def = null)
    {
      ParseResult<LocalDate> res = DatePattern.Parse(input);
      if (res.Success) return res.Value;
      if (def.HasValue) return def.Value;
      else throw res.Exception;
    }
    public static LocalDate GetDate(this GTFSPropertyCollection properties, string key, LocalDate? def = null) => GetDate(properties[key], def);

    public static bool IsDate(string input)
    {
      if (input == null) return false;
      return DateRegex.IsMatch(input);
    }
    public static bool IsDate(this GTFSPropertyCollection properties, string key) => IsDate(properties[key]);

    public static void AssertDate(string input, string key)
    {
      AssertExists(input, key);
      if (!IsDate(input)) throw new PropertyTypeException(key, $"{key} is not a valid date.");
    }
    public static void AssertDate(this GTFSPropertyCollection properties, string key) => AssertDate(properties[key], key);

    // Time in the HH:MM:SS format (H:MM:SS is also accepted). The time is
    //   measured from "noon minus 12h" of the service day (effectively
    //   midnight except for days on which daylight savings time changes
    //   occur. For more information, see the guidelines article). For
    //   times occurring after midnight, enter the time as a value greater
    //   than `24:00:00` in HH:MM:SS local time for the day on which the
    //   trip schedule begins.
    // Example: `14:30:00` for 2:30PM or `25:35:00` for 1:35AM on the next day.
    public static readonly DurationPattern TimePattern = DurationPattern.CreateWithInvariantCulture("H:mm:ss");
    public static readonly Regex TimeRegex = new Regex(@"^\d+:\d\d:\d\d$", RegexOptions.Compiled);

    public static Duration? GetNullableTime(string input)
    {
      if (input == null) return null;
      ParseResult<Duration> res = TimePattern.Parse(input.Trim());
      if (res.Success) return res.Value;
      else return null;
    }
    public static Duration? GetNullableTime(this GTFSPropertyCollection properties, string key) => GetNullableTime(properties[key]);

    public static Duration GetTime(string input, Duration? def = null)
    {
      ParseResult<Duration> res = TimePattern.Parse(input.Trim());
      if (res.Success) return res.Value;
      if (def.HasValue) return def.Value;
      else throw res.Exception;
    }
    public static Duration GetTime(this GTFSPropertyCollection properties, string key, Duration? def = null) => GetTime(properties[key], def);

    public static bool IsTime(string input)
    {
      if (input == null) return false;
      return TimeRegex.IsMatch(input.Trim());
    }
    public static bool IsTime(this GTFSPropertyCollection properties, string key) => IsTime(properties[key]);

    public static void AssertTime(string input, string key)
    {
      AssertExists(input, key);
      if (!IsTime(input)) throw new PropertyTypeException(key, $"{key} is not a valid time.");
    }
    public static void AssertTime(this GTFSPropertyCollection properties, string key) => AssertTime(properties[key], key);

    // Duration - When a duration of time is specified in a GTFS file,
    //   it's specified an an integer number of seconds. The methods below
    //   convert that into Duration objects.
    public static Duration? GetNullableDuration(string input)
    {
      int? val = GetNullableNonNegativeInt(input);
      if (val.HasValue) return Duration.FromSeconds(val.Value);
      else return null;
    }
    public static Duration? GetNullableDuration(this GTFSPropertyCollection properties, string key) => GetNullableDuration(properties[key]);

    public static Duration GetDuration(string input, Duration? def = null)
    {
      int? val = GetNullableNonNegativeInt(input);
      if (val.HasValue) return Duration.FromSeconds(val.Value);
      if (def.HasValue) return def.Value;
      throw new ArgumentException($"{input} could not be parsed into a duration in seconds.");
    }
    public static Duration GetDuration(this GTFSPropertyCollection properties, string key, Duration? def = null) => GetDuration(properties[key], def);

    public static bool IsDuration(string input) => IsNonNegativeInt(input);
    public static bool IsDuration(this GTFSPropertyCollection properties, string key) => IsNonNegativeInt(properties[key]);

    public static void AssertDuration(string input, string key)
    {
      AssertExists(input, key);
      if (!IsDuration(input))
      {
        if (IsInt(input)) throw new PropertyRangeException(key, $"Durations, such as {key}, must be non-negative.");
        else throw new PropertyTypeException(key, $"{key} is not a valid duration.");
      }
    }
    public static void AssertDuration(this GTFSPropertyCollection properties, string key) => AssertDuration(properties[key], key);

    // Timezone - TZ timezone from the https://www.iana.org/time-zones.
    //   Timezone names never contain the space character but may contain
    //   an underscore. Refer to
    //   http://en.wikipedia.org/wiki/List_of_tz_zones for a list of valid
    //   values. 
    private static readonly IDateTimeZoneProvider TimezoneProvider = DateTimeZoneProviders.Tzdb;

    public static DateTimeZone GetTimeZone(string input) => TimezoneProvider.GetZoneOrNull(input);
    public static DateTimeZone GetTimeZone(this GTFSPropertyCollection properties, string key) => GetTimeZone(properties[key]);

    public static void AssertTimeZone(string input, string key)
    {
      AssertExists(input, key);
      DateTimeZone dtz = GetTimeZone(input);
      if (dtz == null) throw new PropertyTypeException(key, $"{key} is not a proper timezone.");
    }
    public static void AssertTimeZone(this GTFSPropertyCollection properties, string key) => AssertTimeZone(properties[key], key);

    // Numeric parsers
    public static bool IsInt(string input) => int.TryParse(input, out int placeholder);
    public static bool IsInt(this GTFSPropertyCollection properties, string key) => IsInt(properties[key]);

    public static bool IsDecimal(string input) => decimal.TryParse(input, out decimal placeholder);
    public static bool IsDecimal(this GTFSPropertyCollection properties, string key) => IsDecimal(properties[key]);

    public static bool IsDouble(string input) => double.TryParse(input, out double placeholder);
    public static bool IsDouble(this GTFSPropertyCollection properties, string key) => IsDouble(properties[key]);

    public static void AssertInt(string input, string key)
    {
      AssertExists(input, key);
      if (!IsInt(input)) throw new PropertyTypeException(key, $"{key} is not a valid integer.");
    }
    public static void AssertInt(this GTFSPropertyCollection properties, string key) => AssertInt(properties[key], key);

    public static void AssertDouble(string input, string key)
    {
      AssertExists(input, key);
      if (!IsDouble(input)) throw new PropertyTypeException(key, $"{key} is not a valid double.");
    }
    public static void AssertDouble(this GTFSPropertyCollection properties, string key) => AssertDouble(properties[key], key);

    public static void AssertDecimal(string input, string key)
    {
      AssertExists(input, key);
      if (!IsDecimal(input)) throw new PropertyTypeException(key, $"{key} is not a valid decimal.");
    }
    public static void AssertDecimal(this GTFSPropertyCollection properties, string key) => AssertDecimal(properties[key], key);

    public static int GetInt(this GTFSPropertyCollection properties, string key, int? def = null)
    {
      string input = properties[key];
      int? ret = GetNullableInt(input);
      if (ret.HasValue) return ret.Value;
      if (def.HasValue) return def.Value;
      throw new ArgumentException($"{input} could not be cast to a number.");
    }

    public static decimal GetDecimal(this GTFSPropertyCollection properties, string key, decimal? def = null)
    {
      string input = properties[key];
      decimal? ret = GetNullableDecimal(input);
      if (ret.HasValue) return ret.Value;
      if (def.HasValue) return def.Value;
      throw new ArgumentException($"{input} could not be cast to a number.");
    }

    public static double GetDouble(this GTFSPropertyCollection properties, string key, double? def = null)
    {
      string input = properties[key];
      double? ret = GetNullableDouble(input);
      if (ret.HasValue) return ret.Value;
      if (def.HasValue) return def.Value;
      throw new ArgumentException($"{input} could not be cast to a number.");
    }

    public static int? GetNullableInt(string input)
    {
      if (int.TryParse(input, out int result)) return result;
      else return null;
    }
    public static int? GetNullableInt(this GTFSPropertyCollection properties, string key) => GetNullableInt(properties[key]);

    public static decimal? GetNullableDecimal(string input)
    {
      if (decimal.TryParse(input, out decimal result)) return result;
      else return null;
    }
    public static decimal? GetNullableDecimal(this GTFSPropertyCollection properties, string key) => GetNullableDecimal(properties[key]);

    public static double? GetNullableDouble(string input)
    {
      if (double.TryParse(input, out double result)) return result;
      else return null;
    }
    public static double? GetNullableDouble(this GTFSPropertyCollection properties, string key) => GetNullableDouble(properties[key]);

    public static int? GetNullableNonNegativeInt(string input)
    {
      if (int.TryParse(input, out int result) && result >= 0) return result;
      else return null;
    }
    public static int? GetNullableNonNegativeInt(this GTFSPropertyCollection properties, string key) => GetNullableNonNegativeInt(properties[key]);

    public static decimal? GetNullableNonNegativeDecimal(string input)
    {
      if (decimal.TryParse(input, out decimal result) && result >= 0) return result;
      else return null;
    }
    public static decimal? GetNullableNonNegativeDecimal(this GTFSPropertyCollection properties, string key) => GetNullableNonNegativeDecimal(properties[key]);

    public static double? GetNullableNonNegativeDouble(string input)
    {
      if (double.TryParse(input, out double result) && result >= 0) return result;
      else return null;
    }
    public static double? GetNullableNonNegativeDouble(this GTFSPropertyCollection properties, string key) => GetNullableNonNegativeDouble(properties[key]);

    public static bool IsNonNegativeInt(string input) => int.TryParse(input, out int placeholder) && placeholder >= 0;
    public static bool IsNonNegativeInt(this GTFSPropertyCollection properties, string key) => IsNonNegativeInt(properties[key]);

    public static bool IsNonNegativeDecimal(string input) => decimal.TryParse(input, out decimal placeholder) && placeholder >= 0;
    public static bool IsNonNegativeDecimal(this GTFSPropertyCollection properties, string key) => IsNonNegativeDecimal(properties[key]);

    public static bool IsNonNegativeDouble(string input) => double.TryParse(input, out double placeholder) && placeholder >= 0;
    public static bool IsNonNegativeDouble(this GTFSPropertyCollection properties, string key) => IsNonNegativeDouble(properties[key]);

    public static void AssertNonNegativeInt(string input, string key)
    {
      AssertExists(input, key);
      if (!IsInt(input)) throw new PropertyTypeException(key, $"{key} is not a valid integer.");
      if (!IsNonNegativeInt(input)) throw new PropertyRangeException(key, $"{key} is not non-negative.");
    }
    public static void AssertNonNegativeInt(this GTFSPropertyCollection properties, string key) => AssertNonNegativeInt(properties[key], key);

    public static void AssertNonNegativeDouble(string input, string key)
    {
      AssertExists(input, key);
      if (!IsDouble(input)) throw new PropertyTypeException(key, $"{key} is not a valid double.");
      if (!IsNonNegativeDouble(input)) throw new PropertyRangeException(key, $"{key} is not non-negative.");
    }
    public static void AssertNonNegativeDouble(this GTFSPropertyCollection properties, string key) => AssertNonNegativeDouble(properties[key], key);

    public static void AssertNonNegativeDecimal(string input, string key)
    {
      AssertExists(input, key);
      if (!IsDecimal(input)) throw new PropertyTypeException(key, $"{key} is not a valid decimal.");
      if (!IsNonNegativeDecimal(input)) throw new PropertyRangeException(key, $"{key} is not non-negative.");
    }
    public static void AssertNonNegativeDecimal(this GTFSPropertyCollection properties, string key) => AssertDecimal(properties[key], key);

    public static int? GetNullableNonZeroInt(string input)
    {
      if (int.TryParse(input, out int result) && result != 0) return result;
      else return null;
    }
    public static int? GetNullableNonZeroInt(this GTFSPropertyCollection properties, string key) => GetNullableNonZeroInt(properties[key]);

    public static decimal? GetNullableNonZeroDecimal(string input)
    {
      if (decimal.TryParse(input, out decimal result) && result != 0) return result;
      else return null;
    }
    public static decimal? GetNullableNonZeroDecimal(this GTFSPropertyCollection properties, string key) => GetNullableNonZeroDecimal(properties[key]);

    public static double? GetNullableNonZeroDouble(string input)
    {
      if (double.TryParse(input, out double result) && result != 0) return result;
      else return null;
    }
    public static double? GetNullableNonZeroDouble(this GTFSPropertyCollection properties, string key) => GetNullableNonZeroDouble(properties[key]);

    public static bool IsNonZeroInt(string input) => int.TryParse(input, out int placeholder) && placeholder != 0;
    public static bool IsNonZeroInt(this GTFSPropertyCollection properties, string key) => IsNonZeroInt(properties[key]);

    public static bool IsNonZeroDecimal(string input) => decimal.TryParse(input, out decimal placeholder) && placeholder != 0;
    public static bool IsNonZeroDecimal(this GTFSPropertyCollection properties, string key) => IsNonZeroDecimal(properties[key]);

    public static bool IsNonZeroDouble(string input) => double.TryParse(input, out double placeholder) && placeholder != 0;
    public static bool IsNonZeroDouble(this GTFSPropertyCollection properties, string key) => IsNonZeroDouble(properties[key]);

    public static void AssertNonZeroInt(string input, string key)
    {
      AssertExists(input, key);
      if (!IsInt(input)) throw new PropertyTypeException(key, $"{key} is not a valid integer.");
      if (!IsNonZeroInt(input)) throw new PropertyRangeException(key, $"{key} is zero.");
    }
    public static void AssertNonZeroInt(this GTFSPropertyCollection properties, string key) => AssertNonZeroInt(properties[key], key);

    public static void AssertNonZeroDouble(string input, string key)
    {
      AssertExists(input, key);
      if (!IsDouble(input)) throw new PropertyTypeException(key, $"{key} is not a valid double.");
      if (!IsNonZeroDouble(input)) throw new PropertyRangeException(key, $"{key} is zero.");
    }
    public static void AssertNonZeroDouble(this GTFSPropertyCollection properties, string key) => AssertNonZeroDouble(properties[key], key);

    public static void AssertNonZeroDecimal(string input, string key)
    {
      AssertExists(input, key);
      if (!IsDecimal(input)) throw new PropertyTypeException(key, $"{key} is not a valid decimal.");
      if (!IsNonZeroDecimal(input)) throw new PropertyRangeException(key, $"{key} is zero.");
    }
    public static void AssertNonZeroDecimal(this GTFSPropertyCollection properties, string key) => AssertDecimal(properties[key], key);

    public static bool GetBool(string input)
    {
      return input == "1";
    }
    public static bool GetBool(this GTFSPropertyCollection properties, string key) => GetBool(properties[key]);

    public static bool? GetNullableBool(string input)
    {
      if (input == "1") return true;
      if (input == "0") return false;
      return null;
    }
    public static bool? GetNullableBool(this GTFSPropertyCollection properties, string key) => GetNullableBool(properties[key]);

    public static bool IsBool(string input)
    {
      return (input == "0" || input == "1");
    }
    public static bool IsBool(this GTFSPropertyCollection properties, string key) => IsBool(properties[key]);

    public static void AssertBool(string input, string key)
    {
      AssertExists(input, key);
      if (input != "1" && input != "0") throw new PropertyTypeException(key, $"{key} isn't a valid bool.");
    }
    public static void AssertBool(this GTFSPropertyCollection properties, string key) => AssertBool(properties[key], key);

    // Foreign key checking
    public static void AssertForeignKeyExists<T>(string input, string key, IDEntityCollection<T> collection, string collectionName) where T : GTFSIdentifiedEntity
    {
      if (input == null) throw new PropertyNullException(key);
      if (!collection.Contains(input))
      {
        if (input == "") throw new PropertyNullException(key);
        else throw new PropertyForeignKeyException(key, $"The collection {collectionName} doesn't contain the key {input}.");
      }
    }
    public static void AssertForeignKeyExists<T>(this GTFSPropertyCollection properties, string key, IDEntityCollection<T> collection, string collectionName) where T : GTFSIdentifiedEntity
      => AssertForeignKeyExists(properties[key], key, collection, collectionName);

    public static void AssertForeignKeyExists<T>(string input, string key, GTFSOrderedEntityCollection<T> collection, string collectionName) where T : GTFSOrderedEntity
    {
      if (input == null) throw new PropertyNullException(key);
      if (!collection.Contains(input))
      {
        if (input == "") throw new PropertyNullException(key);
        else throw new PropertyForeignKeyException(key, $"The collection {collectionName} doesn't contain the key {input}.");
      }
    }
    public static void AssertForeignKeyExists<T>(this GTFSPropertyCollection properties, string key, GTFSOrderedEntityCollection<T> collection, string collectionName) where T : GTFSOrderedEntity
      => AssertForeignKeyExists(properties[key], key, collection, collectionName);

    public static void AssertForeignKeyExists(string input, string key, GTFSCalendarCollection collection, string collectionName)
    {
      if (input == null) throw new PropertyNullException(key);
      if (!collection.Contains(input))
      {
        if (input == "") throw new PropertyNullException(key);
        else throw new PropertyForeignKeyException(key, $"The collection {collectionName} doesn't contain the key {input}.");
      }
    }
    public static void AssertForeignKeyExists(this GTFSPropertyCollection properties, string key, GTFSCalendarCollection collection, string collectionName)
      => AssertForeignKeyExists(properties[key], key, collection, collectionName);

    public static void AssertOptionalForeignKeyExists<T>(string input, string key, IDEntityCollection<T> collection, string collectionName) where T : GTFSIdentifiedEntity
    {
      if (input == null) return;
      if (!collection.Contains(input))
      {
        if (input == "") return;
        else throw new PropertyForeignKeyException(key, $"The collection {collectionName} doesn't contain the key {input}.");
      }
    }
    public static void AssertOptionalForeignKeyExists<T>(this GTFSPropertyCollection properties, string key, IDEntityCollection<T> collection, string collectionName) where T : GTFSIdentifiedEntity
      => AssertOptionalForeignKeyExists(properties[key], key, collection, collectionName);

    public static void AssertOptionalForeignKeyExists<T>(string input, string key, GTFSOrderedEntityCollection<T> collection, string collectionName) where T : GTFSOrderedEntity
    {
      if (input == null) return;
      if (!collection.Contains(input))
      {
        if (input == "") return;
        else throw new PropertyForeignKeyException(key, $"The collection {collectionName} doesn't contain the key {input}.");
      }
    }
    public static void AssertOptionalForeignKeyExists<T>(this GTFSPropertyCollection properties, string key, GTFSOrderedEntityCollection<T> collection, string collectionName) where T : GTFSOrderedEntity
      => AssertOptionalForeignKeyExists(properties[key], key, collection, collectionName);

    public static void AssertOptionalForeignKeyExists(string input, string key, GTFSCalendarCollection collection, string collectionName)
    {
      if (input == null) return;
      if (!collection.Contains(input))
      {
        if (input == "") return;
        else throw new PropertyForeignKeyException(key, $"The collection {collectionName} doesn't contain the key {input}.");
      }
    }
    public static void AssertOptionalForeignKeyExists(this GTFSPropertyCollection properties, string key, GTFSCalendarCollection collection, string collectionName)
      => AssertOptionalForeignKeyExists(properties[key], key, collection, collectionName);
  }
}