using System.Xml.Linq;

using NesterovskyBros.Parser;

namespace Parser.Reports;

public interface IReport
{
  int ReportNumber { get; }

  IEnumerable<XElement> Parse(IEnumerable<Page> report, ITracer? tracer);
}
