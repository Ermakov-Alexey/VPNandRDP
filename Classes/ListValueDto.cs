using System;

namespace VaR;

/// <summary>
/// Универсальный DTO для получения одного столбца из SQL-запроса.
/// Использование: SELECT column AS AsString / AsInt / AsDouble / AsDateTime / AsGuid ...
/// Затем list.Select(x => x.AsString) — и получается типизированный список.
/// или для Nullable var result = ids.Where(x => x.AsInt.HasValue).Select(x => x.AsInt.Value).ToList();
/// </summary>
public class ListValueDto
{
    public string AsString { get; set; }
    public int? AsInt { get; set; }
    public double? AsDouble { get; set; }
    public DateTime? AsDateTime { get; set; }
    public Guid? AsGuid { get; set; }
    public bool? AsBool { get; set; }
}