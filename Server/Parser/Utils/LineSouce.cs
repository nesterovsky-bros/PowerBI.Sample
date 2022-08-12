using System.IO.Compression;
using System.Text;
using System.Xml;

namespace NesterovskyBros.Utils;

/// <summary>
/// A disposable stream source.
/// </summary>
public class LineSource
{
  /// <summary>
  /// Indicates whether to skip top empty lines.
  /// </summary>
  public bool SkipTopEmptyLines { get; set; }

  /// <summary>
  /// A file pth.
  /// </summary>
  public string? Path { get; set; }

  /// <summary>
  /// A content encoding.
  /// </summary>
  public Encoding? Encoding { get; set; }

  /// <summary>
  /// Line width in case of fixed width content.
  /// </summary>
  public int? Width { get; set; }

  /// <summary>
  /// Gets a content lines enumerator.
  /// </summary>
  /// <returns>a content lines enumerator.</returns>
  public IEnumerable<string> GetLines()
  {
    return string.IsNullOrEmpty(Path) ? Array.Empty<string>() :
      Path.EndsWith(".xml.gz", true, null) ||
      Path.EndsWith(".xml-gz", true, null) ?
      GetXmlGzLines() :
      Path.EndsWith(".gz", true, null) ?
      GetGzLines() :
      Path.EndsWith(".xml", true, null) ?
      GetXmlLines() :
      GetTextLines();
  }

  /// <summary>
  /// Gets stream of data.
  /// </summary>
  /// <returns></returns>
  public virtual Stream GetStream() => File.OpenRead(Path!);

  /// <summary>
  /// Gets a content lines enumerator for the text file.
  /// </summary>
  /// <returns>a content lines enumerator.</returns>
  public IEnumerable<string> GetTextLines()
  {
    using var stream = GetStream();
    using var reader = new StreamReader(stream, Encoding!);

    foreach(var line in
      (Width != null) && (Width > 0) ?
        GetLines(reader, Width.Value) :
        GetLines(reader))
    {
      yield return line;
    }
  }

  /// <summary>
  /// Gets a content lines enumerator for the .gz extension.
  /// </summary>
  /// <returns>a content lines enumerator.</returns>
  public IEnumerable<string> GetGzLines()
  {
    using var stream = GetStream();
    using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
    using var reader = new StreamReader(gzipStream, Encoding!);

    foreach(var line in
      (Width != null) && (Width > 0) ?
        GetLines(reader, Width.Value) :
        GetLines(reader))
    {
      yield return line;
    }
  }

  /// <summary>
  /// Gets a content lines enumerator for the .zip extension.
  /// </summary>
  /// <returns>a content lines enumerator.</returns>
  public IEnumerable<string> GetXmlLines()
  {
    var hasData = false;
    var topEmptyLine = true;
    using var stream = GetStream();
    using var reader = XmlReader.Create(stream);

    while(reader.Read())
    {
      switch(reader.NodeType)
      {
        case XmlNodeType.Element:
        {
          if (reader.LocalName == "raw")
          {
            var lines = reader.ReadString();

            if (lines != null)
            {
              var stringReader = new StringReader(lines);

              while(true)
              {
                var value = stringReader.ReadLine();

                if (value == null)
                {
                  break;
                }

                hasData = true;

                if (topEmptyLine && SkipTopEmptyLines &&
                  string.IsNullOrWhiteSpace(value))
                {
                  continue;
                }

                topEmptyLine = false;

                yield return value;
              }
            }
          }

          break;
        }
      }
    }

    if (!hasData)
    {
      throw new IOException("No data is found in.");
    }
  }

  /// <summary>
  /// Gets a content lines enumerator for the .xml-gz/.xml.gz extension.
  /// </summary>
  /// <returns>a content lines enumerator.</returns>
  public IEnumerable<string> GetXmlGzLines()
  {
    var hasData = false;
    var topEmptyLine = true;

    using var stream = GetStream();
    using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
    using var reader = XmlReader.Create(gzipStream);
    
    while(reader.Read())
    {
      switch(reader.NodeType)
      {
        case XmlNodeType.Element:
        {
          if (reader.LocalName == "raw")
          {
            var lines = reader.ReadString();

            if (lines != null)
            {
              var stringReader = new StringReader(lines);

              while(true)
              {
                var value = stringReader.ReadLine();

                if (value == null)
                {
                  break;
                }

                hasData = true;

                if (topEmptyLine && SkipTopEmptyLines &&
                  string.IsNullOrWhiteSpace(value))
                {
                  continue;
                }

                topEmptyLine = false;

                yield return value;
              }
            }
          }

          break;
        }
      }
    }

    if (!hasData)
    {
      throw new IOException("No data is found in.");
    }
  }

  /// <summary>
  /// Gets a fixed lines for the reader.
  /// </summary>
  /// <param name="reader">A text reader.</param>
  /// <param name="width">A line width.</param>
  /// <returns>A data enumerator.</returns>
  private IEnumerable<string> GetLines(TextReader reader, int width)
  {
    var buffer = new char[width];
    var topEmptyLine = true;

    while(true)
    {
      int count = reader.ReadBlock(buffer, 0, width);

      if (count == 0)
      {
        break;
      }

      var line = new string(buffer, 0, count);

      if (topEmptyLine && SkipTopEmptyLines &&
        string.IsNullOrWhiteSpace(line))
      {
        continue;
      }

      topEmptyLine = false;

      yield return line;
    }
  }

  /// <summary>
  /// Gets a variable length lines for the reader.
  /// </summary>
  /// <param name="reader">A text reader.</param>
  /// <returns>A data enumerator.</returns>
  private IEnumerable<string> GetLines(TextReader reader)
  {
    var topEmptyLine = true;

    while(true)
    {
      var value = reader.ReadLine();

      if (value == null)
      {
        break;
      }

      if (topEmptyLine && SkipTopEmptyLines &&
        string.IsNullOrWhiteSpace(value))
      {
        continue;
      }

      topEmptyLine = false;

      yield return value;
    }
  }
}
