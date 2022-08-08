using NesterovskyBros.Collections;

namespace Parser.Reports;

public interface IReport
{
  int ReportNumber { get; }

  IEnumerable<object> Parse(IEnumerable<Page> pages, ITracer? tracer);
}
