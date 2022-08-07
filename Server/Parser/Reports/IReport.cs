using NesterovskyBros.Parser;

namespace Parser.Reports;

public interface IReport
{
  int ReportNumber { get; }

  IEnumerable<object?> Parse(IEnumerable<Page> report, ITracer? tracer);
}
