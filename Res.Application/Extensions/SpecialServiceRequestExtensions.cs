using System.Text;
using Res.Domain.Entities.Pnr;

namespace Res.Application.Extensions
{
    public static class SpecialServiceRequestExtensions
    {
        public static string OutputSsrList(this List<Ssr> ssrs)
        {
            if (!ssrs.Any())
                return "NO SSRS FOUND";

            var sb = new StringBuilder();
            sb.AppendLine("SPECIAL SERVICE REQUESTS");
            sb.AppendLine(new string('-', 75));

            foreach (var ssr in ssrs)
            {
                var paxInfo = $"P{ssr.PassengerId}";
                var segInfo = $"S{ssr.SegmentNumber}";

                sb.AppendLine($"{ssr.Id,-6} {ssr.Code,-5} {paxInfo,-5} {segInfo,-5} {ssr.Status,-10} " +
                              $"{ssr.ActionCode,-2} {ssr.CompanyId,-2} {ssr.Text}");
            }

            return sb.ToString();
        }

    }
}