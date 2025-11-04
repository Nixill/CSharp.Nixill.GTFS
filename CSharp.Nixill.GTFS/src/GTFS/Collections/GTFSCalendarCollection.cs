using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using Nixill.GTFS.Entities;
using Nixill.GTFS.Sources;
using NodaTime;

namespace Nixill.GTFS.Collections
{
  public class GTFSCalendarCollection : IReadOnlyCollection<FullyDescribedCalendar>
  {
    public readonly IDEntityCollection<Calendar> Calendars;
    public readonly TwoKeyEntityCollection<CalendarDate, string, LocalDate> CalendarDates;
    public readonly IReadOnlyList<string> ServiceIDs;
    public int Count => ServiceIDs.Count;
    public FullyDescribedCalendar this[string id]
      => (ServiceIDs.Contains(id)) ? new(id, Calendars[id], CalendarDates.WithFirstKey(id)) : null;

    public GTFSCalendarCollection(IGTFSDataSource source, IDEntityCollection<Calendar> calendars, TwoKeyEntityCollection<CalendarDate, string, LocalDate> calendarDates)
    {
      Calendars = calendars;
      CalendarDates = calendarDates;

      ServiceIDs = Calendars.Select(x => x.ID).Union(CalendarDates.FirstKeys).ToList().AsReadOnly();
    }

    public IEnumerator<FullyDescribedCalendar> GetEnumerator() => ServiceIDs.Select(x => this[x]).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)(GetEnumerator());
    public bool Contains(string serviceID) => ServiceIDs.Contains(serviceID);
  }

  public record class FullyDescribedCalendar(string Name, Calendar Calendar, IEnumerable<CalendarDate> Exceptions)
  {
    public bool HasServiceOn(LocalDate date)
    {
      if (Exceptions.Any(cd => cd.IsAdded && cd.Date == date)) return true;
      if (Calendar != null && Calendar.ServiceOn(date) && !Exceptions.Any(cd => cd.IsRemoved && cd.Date == date)) return true;
      return false;
    }
  }
}