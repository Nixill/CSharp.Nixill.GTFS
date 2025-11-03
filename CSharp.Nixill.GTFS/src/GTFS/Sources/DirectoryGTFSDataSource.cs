using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nixill.GTFS.Collections;
using Nixill.GTFS.Entities;
using Nixill.Serialization;
using Nixill.Utils;

namespace Nixill.GTFS.Sources
{
  public class DirectoryGTFSDataSource : IGTFSDataSource
  {
    private string SourceDirectory;

    public DirectoryGTFSDataSource(string directory)
    {
      SourceDirectory = directory;
      if (!Directory.Exists(SourceDirectory)) throw new DirectoryNotFoundException("The specified directory does not exist.");
    }

    public DirectoryGTFSDataSource()
    {
      SourceDirectory = Directory.GetCurrentDirectory();
    }

    private FileStream GetFileOrNull(string path)
    {
      try
      {
        return File.OpenRead(path);
      }
      catch (Exception)
      {
        return null;
      }
    }

    public IEnumerable<T> GetObjects<T>(string table, GTFSEntityFactory<T> factory, List<GTFSUnparsedEntity> unparsed = null) where T : GTFSEntity
    {
      // Get the file:
      FileStream file = GetFileOrNull($"{SourceDirectory}{Path.DirectorySeparatorChar}{table}");

      // If that's not found, try appending .txt:
      if (file == null) file = File.OpenRead($"{SourceDirectory}{Path.DirectorySeparatorChar}{table}.txt");

      // If still nout found, return an empty collection.
      if (file == null) yield break;

      var rows = CSVParser.EnumerableToRows(FileUtils.StreamCharEnumerator(new StreamReader(file)));

      List<string> header = new List<string>();
      bool first = true;

      foreach (List<string> row in rows)
      {
        if (first)
        {
          header = row;
          first = false;
          continue;
        }

        var props = header.Zip(row);

        T obj;

        try
        {
          obj = factory(props);
        }
        catch (Exception ex)
        {
          if (unparsed == null) throw;
          GTFSUnparsedEntity ent = new GTFSUnparsedEntity(new GTFSPropertyCollection(props), ex);
          unparsed.Add(ent);
          continue;
        }

        yield return obj;
      }
    }
  }
}