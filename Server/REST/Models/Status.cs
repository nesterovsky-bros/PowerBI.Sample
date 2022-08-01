using System;
using System.Collections.Generic;

namespace REST.Models;

public partial class Status
{
  public int ID { get; set; }
  public bool Check { get; set; }
  public string? Comment { get; set; }
}
