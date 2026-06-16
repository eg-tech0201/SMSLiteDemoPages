using System.Globalization;

namespace sms_lite.Services;

// "SurveyInstance" is an in-memory demo source representing a unique
// (ReferenceDate, SurveyId, SampleId) combination. Randomly generated data for now.
public sealed class SurveyInstanceService
{
    private readonly List<SurveyInstance> _instances;

    public SurveyInstanceService()
    {
        _instances = BuildSeedInstances();
    }

    public IReadOnlyList<SurveyInstance> Instances => _instances;

    public SurveyInstance? FindInstance(DateTime referenceDate, int surveyId, string sampleId)
    {
        return _instances.FirstOrDefault(i =>
            i.ReferenceDate.Date == referenceDate.Date &&
            i.SurveyId == surveyId &&
            i.SampleId.Equals(sampleId, StringComparison.OrdinalIgnoreCase));
    }

    public FilterOptionsResponse GetFilterOptions(DateTime? referenceDate, int? surveyId, string? sampleId)
    {
        IEnumerable<SurveyInstance> q = _instances;

        if (referenceDate.HasValue)
        {
            var date = referenceDate.Value.Date;
            q = q.Where(i => i.ReferenceDate.Date == date);
        }

        if (surveyId.HasValue)
        {
            q = q.Where(i => i.SurveyId == surveyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(sampleId))
        {
            q = q.Where(i => i.SampleId.Equals(sampleId, StringComparison.OrdinalIgnoreCase));
        }

        return new FilterOptionsResponse(
            q.Select(i => i.ReferenceDate.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                .ToList(),
            q.Select(i => i.SurveyId)
                .Distinct()
                .OrderBy(id => id)
                .ToList(),
            q.Select(i => i.SampleId)
                .Distinct()
                .OrderBy(id => id)
                .ToList()
        );
    }

    public SurveyInstanceDetailResponse? GetDetail(DateTime referenceDate, int surveyId, string sampleId)
    {
        var match = _instances.FirstOrDefault(i =>
            i.ReferenceDate.Date == referenceDate.Date &&
            i.SurveyId == surveyId &&
            i.SampleId.Equals(sampleId, StringComparison.OrdinalIgnoreCase));

        var resolved = match;
        if (resolved is null)
        {
            var baseInstance = _instances
                .Where(i => i.SurveyId == surveyId && i.SampleId.Equals(sampleId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.ReferenceDate)
                .FirstOrDefault();

            if (baseInstance is null)
                return null;

            resolved = CreateSyntheticInstanceForDate(baseInstance, referenceDate.Date);
        }

        var fullRecord = BuildFullRecord(resolved);
        var surveyDesignerAssociations = BuildSurveyDesignerAssociations(resolved);
        return new SurveyInstanceDetailResponse(
            resolved.SampleId,
            resolved.SampleName,
            resolved.SurveyId,
            resolved.Title,
            resolved.SubTitle,
            resolved.SurveyFrequency,
            resolved.Version,
            resolved.SurveyDate,
            resolved.ReferenceDate,
            resolved.SurveyStartDate,
            resolved.SurveyStopDate,
            resolved.HqSurveyAdmin,
            resolved.ProjectCode,
            "N/A",
            "N/A",
            resolved.Modes.Select(m => new ModeWindow(m.Mode, m.StartDate, m.StopDate)).ToList(),
            resolved.OpDomCounts.Select(c => new CountItem(c.Code, c.Definition, c.Count)).ToList(),
            resolved.DcmsCounts.Select(c => new CountItem(c.Code, c.Definition, c.Count)).ToList(),
            resolved.OpDomCounts.Select(c => new CountItem(c.Code, c.Definition, c.Count)).ToList(),
            resolved.DcmsCounts.Select(c => new CountItem(c.Code, c.Definition, c.Count)).ToList(),
            resolved.TotalReceived + resolved.TotalDeleted,
            resolved.TotalReceived,
            resolved.TotalDeleted,
            resolved.BudgetAllocation,
            resolved.RespondentInstancesLast1Year,
            resolved.RespondentInstancesLast3Years,
            resolved.RespondentInstancesLast5Years,
            resolved.ResponseHistoryRate,
            resolved.ResponseHistoryBreakdown.Select(b => new ResponseHistoryItem(b.Label, b.Count)).ToList(),
            surveyDesignerAssociations,
            BuildCollectionMaterials(resolved),
            GetSurveyRecordIndex(resolved.SurveyId)
                .Where(r => r.SampleId.Equals(resolved.SampleId, StringComparison.OrdinalIgnoreCase) && r.ReferenceDate.Date == resolved.ReferenceDate.Date)
                .Select(r => new SurveyDetailRecordRow(r.StateId, r.StateAlpha, r.SKey, "PUBLISHED", r.Poid, r.TargetPoid, r.SampleId, r.ReferenceDate))
                .ToList(),
            fullRecord
        );
    }

    public List<SurveyInstance> FilterInstances(DateTime? referenceDate, int? surveyId, string? sampleId)
    {
        IEnumerable<SurveyInstance> q = _instances;

        if (referenceDate.HasValue)
        {
            var date = referenceDate.Value.Date;
            q = q.Where(i => i.ReferenceDate.Date == date);
        }

        if (surveyId.HasValue)
        {
            q = q.Where(i => i.SurveyId == surveyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(sampleId))
        {
            q = q.Where(i => i.SampleId.Equals(sampleId, StringComparison.OrdinalIgnoreCase));
        }

        return q.ToList();
    }

    public IReadOnlyList<SurveyInstance> GetSurveyInstances(int surveyId)
        => _instances
            .Where(i => i.SurveyId == surveyId)
            .OrderBy(i => i.StateId)
            .ThenBy(i => i.SampleId)
            .ToList();

    public IReadOnlyList<SurveyRecordIndexItem> GetSurveyRecordIndex(int surveyId)
    {
        var records = new List<SurveyRecordIndexItem>();
        foreach (var instance in _instances.Where(i => i.SurveyId == surveyId))
        {
            records.AddRange(BuildInstanceGridRecords(instance, 12));
        }

        return records
            .OrderBy(i => i.ReferenceDate)
            .ThenBy(i => i.StateId)
            .ThenBy(i => i.SKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<SurveyRespondentRecord> GetRespondentsForInstance(
        DateTime referenceDate,
        int surveyId,
        string sampleId,
        string? skey,
        int count = 24)
    {
        var instance = _instances.FirstOrDefault(i =>
            i.ReferenceDate.Date == referenceDate.Date &&
            i.SurveyId == surveyId &&
            i.SampleId.Equals(sampleId, StringComparison.OrdinalIgnoreCase));

        var resolved = instance;
        if (resolved is null)
        {
            var baseInstance = _instances
                .Where(i => i.SurveyId == surveyId && i.SampleId.Equals(sampleId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.ReferenceDate)
                .FirstOrDefault();
            if (baseInstance is null)
                return [];

            resolved = CreateSyntheticInstanceForDate(baseInstance, referenceDate.Date);
        }

        var respondentCount = Math.Max(12, count);
        var records = new List<SurveyRespondentRecord>(respondentCount);
        for (var i = 0; i < respondentCount; i++)
        {
            records.Add(new SurveyRespondentRecord(
                i + 1,
                surveyId,
                sampleId,
                referenceDate,
                resolved.StateId,
                resolved.StateAlpha,
                skey ?? $"S-{surveyId}-{i + 1:D3}",
                BuildFullRecord(resolved, i + 1)));
        }

        return records;
    }

    public IReadOnlyList<DateTime> GetReferenceDatesForSurveySample(int surveyId, string sampleId)
    {
        var exact = _instances
            .Where(i => i.SurveyId == surveyId && i.SampleId.Equals(sampleId, StringComparison.OrdinalIgnoreCase))
            .Select(i => i.ReferenceDate.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (exact.Count > 0)
        {
            if (exact.Count >= 3)
                return exact;

            var expanded = ExpandReferenceDates(exact.First(), 6)
                .Union(exact)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();
            return expanded;
        }

        var surveyDates = _instances
            .Where(i => i.SurveyId == surveyId)
            .Select(i => i.ReferenceDate.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (surveyDates.Count > 0)
            return ExpandReferenceDates(surveyDates.First(), 6);

        return [];
    }

    private static List<DateTime> ExpandReferenceDates(DateTime seedDate, int totalCount)
    {
        var dates = new HashSet<DateTime> { seedDate.Date };
        var offset = 1;
        while (dates.Count < totalCount)
        {
            dates.Add(seedDate.AddMonths(-offset).Date);
            if (dates.Count >= totalCount)
                break;
            dates.Add(seedDate.AddMonths(offset).Date);
            offset++;
        }

        return dates.OrderByDescending(d => d).ToList();
    }

    private static SurveyInstance CreateSyntheticInstanceForDate(SurveyInstance source, DateTime referenceDate)
    {
        var delta = referenceDate.Date - source.ReferenceDate.Date;
        return source with
        {
            ReferenceDate = referenceDate.Date,
            SurveyDate = referenceDate.Date,
            SurveyStartDate = source.SurveyStartDate.Add(delta),
            SurveyStopDate = source.SurveyStopDate.Add(delta),
            Modes = source.Modes
                .Select(m => new ModeWindow(
                    m.Mode,
                    m.StartDate.HasValue ? m.StartDate.Value.Add(delta) : null,
                    m.StopDate.HasValue ? m.StopDate.Value.Add(delta) : null))
                .ToList()
        };
    }

    public IReadOnlyList<SurveyRespondentRecord> GetSurveyRespondentRecords(int surveyId)
    {
        var instances = _instances
            .Where(i => i.SurveyId == surveyId)
            .OrderBy(i => i.ReferenceDate)
            .ThenBy(i => i.SampleId)
            .ToList();

        var rows = new List<SurveyRespondentRecord>();
        foreach (var instance in instances)
        {
            rows.AddRange(GetRespondentsForInstance(instance.ReferenceDate, instance.SurveyId, instance.SampleId, $"S-{instance.SurveyId}-001", 12));
        }

        return rows;
    }

    public SurveyRecordLookupResult? FindSurveyRecordByPoid(int surveyId, string poid)
    {
        if (string.IsNullOrWhiteSpace(poid))
            return null;

        var normalizedInput = NormalizePoid(poid);
        foreach (var instance in _instances.Where(i => i.SurveyId == surveyId))
        {
            var fullRecord = BuildFullRecord(instance);
            var recordPoid = fullRecord.FirstOrDefault(f => f.Field == "poid")?.Value;
            var targetPoid = fullRecord.FirstOrDefault(f => f.Field == "target_poid")?.Value;

            if (string.IsNullOrWhiteSpace(recordPoid) && string.IsNullOrWhiteSpace(targetPoid))
                continue;

            if (string.Equals(NormalizePoid(recordPoid), normalizedInput, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(NormalizePoid(targetPoid), normalizedInput, StringComparison.OrdinalIgnoreCase))
            {
                return new SurveyRecordLookupResult(
                    instance.ReferenceDate,
                    instance.SurveyId,
                    instance.SampleId,
                    recordPoid ?? string.Empty,
                    targetPoid ?? string.Empty);
            }
        }

        return null;
    }

    private static string NormalizePoid(string? poid)
    {
        if (string.IsNullOrWhiteSpace(poid))
            return string.Empty;

        return new string(poid
            .Where(ch => !char.IsWhiteSpace(ch) && ch != '-')
            .ToArray())
            .ToUpperInvariant();
    }

    private static List<SurveyInstance> BuildSeedInstances()
    {
        var instances = new List<SurveyInstance>
        {
            new SurveyInstance(
                SurveyId: 1001,
                SampleId: "SMP-1001-A",
                SampleName: "CY-2026 Core Sample",
                ReferenceDate: new DateTime(2026, 1, 5),
                Title: "Crop Yield Survey (CY-2026)",
                SubTitle: "Winter grain follow-up",
                SurveyFrequency: "Annual",
                Version: "v2.1",
                SurveyDate: new DateTime(2026, 1, 5),
                SurveyStartDate: new DateTime(2026, 1, 5),
                SurveyStopDate: new DateTime(2026, 3, 31),
                HqSurveyAdmin: "A. Chen",
                ProjectCode: "CY26-CORE",
                Status: "In progress",
                OmbNumber: "0535-0123",
                OmbExpires: new DateTime(2028, 6, 30),
                State: "IA",
                Region: "UMR",
                StateId: "19",
                StateAlpha: "IA",
                DcmsCodeId: "A1",
                OpDomStatusId: "Active",
                EnumeratorId: "E-101",
                EnumeratorName: "A. Rivera",
                ManagerId: "M-201",
                ManagerName: "J. Howard",
                CoachId: "C-301",
                CoachName: "L. Park",
                EnumeratorNotes: "Initial outreach complete.",
                ResponseCode: "Complete",
                Mode: "CAPI",
                Modes: new List<ModeWindow>
                {
                    new("CASI", new DateTime(2026, 1, 5), new DateTime(2026, 2, 28)),
                    new("CAPI", new DateTime(2026, 1, 10), new DateTime(2026, 3, 10)),
                    new("CATI", new DateTime(2026, 1, 20), new DateTime(2026, 3, 31)),
                    new("MAIL", new DateTime(2026, 1, 15), new DateTime(2026, 2, 20))
                },
                OpDomCounts: new List<CountItem>
                {
                    new("Active", "Open cases currently in field", 482),
                    new("Complete", "Final disposition received", 1298),
                    new("Refused", "Refusal recorded", 66)
                },
                DcmsCounts: new List<CountItem>
                {
                    new("A1", "Accepted and complete", 1044),
                    new("B2", "Partial response", 212),
                    new("C3", "Non-contact", 145)
                },
                TotalReceived: 1256,
                TotalDeleted: 24,
                BudgetAllocation: 320_000m,
                RespondentInstancesLast1Year: 2,
                RespondentInstancesLast3Years: 4,
                RespondentInstancesLast5Years: 6,
                ResponseHistoryRate: 83.3m,
                ResponseHistoryBreakdown: new List<ResponseHistoryItem>
                {
                    new("Completed", 5),
                    new("Partial", 1)
                }
            ),
            new SurveyInstance(
                SurveyId: 1001,
                SampleId: "SMP-1001-B",
                SampleName: "CY-2026 Supplemental",
                ReferenceDate: new DateTime(2026, 2, 15),
                Title: "Crop Yield Survey (CY-2026)",
                SubTitle: "Late-season addendum",
                SurveyFrequency: "Annual",
                Version: "v2.2",
                SurveyDate: new DateTime(2026, 2, 15),
                SurveyStartDate: new DateTime(2026, 2, 15),
                SurveyStopDate: new DateTime(2026, 4, 15),
                HqSurveyAdmin: "M. Lewis",
                ProjectCode: "CY26-SUP",
                Status: "Not started",
                OmbNumber: "0535-0123",
                OmbExpires: new DateTime(2028, 6, 30),
                State: "IA",
                Region: "UMR",
                StateId: "19",
                StateAlpha: "IA",
                DcmsCodeId: "B2",
                OpDomStatusId: "Active",
                EnumeratorId: "E-102",
                EnumeratorName: "K. Nguyen",
                ManagerId: "M-201",
                ManagerName: "J. Howard",
                CoachId: "C-302",
                CoachName: "S. Patel",
                EnumeratorNotes: "Follow-up scheduled.",
                ResponseCode: "Pending",
                Mode: "CATI",
                Modes: new List<ModeWindow>
                {
                    new("CASI", new DateTime(2026, 2, 15), new DateTime(2026, 3, 15)),
                    new("CATI", new DateTime(2026, 2, 20), new DateTime(2026, 4, 1)),
                    new("MAIL", new DateTime(2026, 2, 25), new DateTime(2026, 3, 10))
                },
                OpDomCounts: new List<CountItem>
                {
                    new("Active", "Open cases currently in field", 312),
                    new("Complete", "Final disposition received", 418),
                    new("Refused", "Refusal recorded", 19)
                },
                DcmsCounts: new List<CountItem>
                {
                    new("A1", "Accepted and complete", 395),
                    new("B2", "Partial response", 72),
                    new("C3", "Non-contact", 57)
                },
                TotalReceived: 467,
                TotalDeleted: 6,
                BudgetAllocation: 110_000m,
                RespondentInstancesLast1Year: 1,
                RespondentInstancesLast3Years: 3,
                RespondentInstancesLast5Years: 4,
                ResponseHistoryRate: 75.0m,
                ResponseHistoryBreakdown: new List<ResponseHistoryItem>
                {
                    new("Completed", 3),
                    new("Partial", 1)
                }
            ),
            new SurveyInstance(
                SurveyId: 1002,
                SampleId: "SMP-1002-A",
                SampleName: "ACP Pilot Frame",
                ReferenceDate: new DateTime(2026, 1, 8),
                Title: "Agricultural Census Pilot (ACP)",
                SubTitle: "Pilot compliance test",
                SurveyFrequency: "One-time",
                Version: "v1.0",
                SurveyDate: new DateTime(2026, 1, 8),
                SurveyStartDate: new DateTime(2026, 1, 10),
                SurveyStopDate: new DateTime(2026, 2, 28),
                HqSurveyAdmin: "D. Khan",
                ProjectCode: "ACP-PILOT",
                Status: "IN HQ REVIEW",
                OmbNumber: "0535-0456",
                OmbExpires: new DateTime(2027, 9, 30),
                State: "AZ",
                Region: "MTR",
                StateId: "04",
                StateAlpha: "AZ",
                DcmsCodeId: "C3",
                OpDomStatusId: "Active",
                EnumeratorId: "E-103",
                EnumeratorName: "T. Jordan",
                ManagerId: "M-202",
                ManagerName: "S. Hale",
                CoachId: "C-303",
                CoachName: "R. Kim",
                EnumeratorNotes: "Refusal noted.",
                ResponseCode: "Refused",
                Mode: "CATI",
                Modes: new List<ModeWindow>
                {
                    new("CAPI", new DateTime(2026, 1, 10), new DateTime(2026, 2, 5)),
                    new("CATI", new DateTime(2026, 1, 15), new DateTime(2026, 2, 28)),
                    new("MAIL", new DateTime(2026, 1, 12), new DateTime(2026, 1, 30))
                },
                OpDomCounts: new List<CountItem>
                {
                    new("Active", "Open cases currently in field", 112),
                    new("Complete", "Final disposition received", 225),
                    new("Refused", "Refusal recorded", 31)
                },
                DcmsCounts: new List<CountItem>
                {
                    new("A1", "Accepted and complete", 196),
                    new("B2", "Partial response", 64),
                    new("C3", "Non-contact", 52)
                },
                TotalReceived: 280,
                TotalDeleted: 9,
                BudgetAllocation: 95_000m,
                RespondentInstancesLast1Year: 1,
                RespondentInstancesLast3Years: 2,
                RespondentInstancesLast5Years: 3,
                ResponseHistoryRate: 33.3m,
                ResponseHistoryBreakdown: new List<ResponseHistoryItem>
                {
                    new("Completed", 1),
                    new("Refused", 2)
                }
            ),
            new SurveyInstance(
                SurveyId: 1003,
                SampleId: "SMP-1003-A",
                SampleName: "FLS 2026 Sample",
                ReferenceDate: new DateTime(2026, 1, 11),
                Title: "Farm Labor Study (FLS)",
                SubTitle: "Quarterly labor check-in",
                SurveyFrequency: "Quarterly",
                Version: "v3.0",
                SurveyDate: new DateTime(2026, 1, 11),
                SurveyStartDate: new DateTime(2026, 1, 15),
                SurveyStopDate: new DateTime(2026, 4, 15),
                HqSurveyAdmin: "J. Patel",
                ProjectCode: "FLS-2026",
                Status: "In progress",
                OmbNumber: "0535-0789",
                OmbExpires: new DateTime(2029, 1, 31),
                State: "OR",
                Region: "PCR",
                StateId: "41",
                StateAlpha: "OR",
                DcmsCodeId: "A1",
                OpDomStatusId: "Active",
                EnumeratorId: "E-104",
                EnumeratorName: "P. Singh",
                ManagerId: "M-203",
                ManagerName: "E. Walsh",
                CoachId: "C-304",
                CoachName: "D. Foster",
                EnumeratorNotes: "Confirmed appointment.",
                ResponseCode: "Complete",
                Mode: "CASI",
                Modes: new List<ModeWindow>
                {
                    new("CASI", new DateTime(2026, 1, 15), new DateTime(2026, 3, 5)),
                    new("CATI", new DateTime(2026, 2, 1), new DateTime(2026, 4, 10))
                },
                OpDomCounts: new List<CountItem>
                {
                    new("Active", "Open cases currently in field", 338),
                    new("Complete", "Final disposition received", 742),
                    new("Refused", "Refusal recorded", 45)
                },
                DcmsCounts: new List<CountItem>
                {
                    new("A1", "Accepted and complete", 615),
                    new("B2", "Partial response", 110),
                    new("C3", "Non-contact", 89)
                },
                TotalReceived: 802,
                TotalDeleted: 18,
                BudgetAllocation: 210_000m,
                RespondentInstancesLast1Year: 3,
                RespondentInstancesLast3Years: 5,
                RespondentInstancesLast5Years: 7,
                ResponseHistoryRate: 71.4m,
                ResponseHistoryBreakdown: new List<ResponseHistoryItem>
                {
                    new("Completed", 5),
                    new("Partial", 1),
                    new("No response", 1)
                }
            ),
            new SurveyInstance(
                SurveyId: 1004,
                SampleId: "SMP-1004-A",
                SampleName: "DIS Monthly Frame",
                ReferenceDate: new DateTime(2026, 1, 15),
                Title: "Dairy Inventory Survey (DIS)",
                SubTitle: "Monthly inventory update",
                SurveyFrequency: "Monthly",
                Version: "v5.2",
                SurveyDate: new DateTime(2026, 1, 15),
                SurveyStartDate: new DateTime(2026, 1, 1),
                SurveyStopDate: new DateTime(2026, 1, 31),
                HqSurveyAdmin: "R. Gomez",
                ProjectCode: "DIS-2026-01",
                Status: "Overdue",
                OmbNumber: "0535-0321",
                OmbExpires: new DateTime(2027, 12, 31),
                State: "WI",
                Region: "GLR",
                StateId: "55",
                StateAlpha: "WI",
                DcmsCodeId: "B2",
                OpDomStatusId: "Active",
                EnumeratorId: "E-105",
                EnumeratorName: "H. Gomez",
                ManagerId: "M-204",
                ManagerName: "R. Lopez",
                CoachId: "C-305",
                CoachName: "B. Chen",
                EnumeratorNotes: "Awaiting callback.",
                ResponseCode: "Pending",
                Mode: "CAPI",
                Modes: new List<ModeWindow>
                {
                    new("CAPI", new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)),
                    new("MAIL", new DateTime(2026, 1, 5), new DateTime(2026, 1, 20))
                },
                OpDomCounts: new List<CountItem>
                {
                    new("Active", "Open cases currently in field", 205),
                    new("Complete", "Final disposition received", 488),
                    new("Refused", "Refusal recorded", 22)
                },
                DcmsCounts: new List<CountItem>
                {
                    new("A1", "Accepted and complete", 421),
                    new("B2", "Partial response", 54),
                    new("C3", "Non-contact", 63)
                },
                TotalReceived: 512,
                TotalDeleted: 11,
                BudgetAllocation: 140_000m,
                RespondentInstancesLast1Year: 2,
                RespondentInstancesLast3Years: 4,
                RespondentInstancesLast5Years: 5,
                ResponseHistoryRate: 60.0m,
                ResponseHistoryBreakdown: new List<ResponseHistoryItem>
                {
                    new("Completed", 3),
                    new("Partial", 1),
                    new("Refused", 1)
                }
            ),
            new SurveyInstance(
                SurveyId: 1004,
                SampleId: "SMP-1004-A",
                SampleName: "DIS Monthly Frame",
                ReferenceDate: new DateTime(2026, 2, 15),
                Title: "Dairy Inventory Survey (DIS)",
                SubTitle: "Monthly inventory update",
                SurveyFrequency: "Monthly",
                Version: "v5.3",
                SurveyDate: new DateTime(2026, 2, 15),
                SurveyStartDate: new DateTime(2026, 2, 1),
                SurveyStopDate: new DateTime(2026, 2, 28),
                HqSurveyAdmin: "R. Gomez",
                ProjectCode: "DIS-2026-02",
                Status: "In progress",
                OmbNumber: "0535-0321",
                OmbExpires: new DateTime(2027, 12, 31),
                State: "WI",
                Region: "GLR",
                StateId: "55",
                StateAlpha: "WI",
                DcmsCodeId: "A1",
                OpDomStatusId: "Active",
                EnumeratorId: "E-105",
                EnumeratorName: "H. Gomez",
                ManagerId: "M-204",
                ManagerName: "R. Lopez",
                CoachId: "C-305",
                CoachName: "B. Chen",
                EnumeratorNotes: "February cycle follow-up.",
                ResponseCode: "Complete",
                Mode: "CAPI",
                Modes: new List<ModeWindow>
                {
                    new("CAPI", new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)),
                    new("MAIL", new DateTime(2026, 2, 3), new DateTime(2026, 2, 19))
                },
                OpDomCounts: new List<CountItem>
                {
                    new("Active", "Open cases currently in field", 214),
                    new("Complete", "Final disposition received", 521),
                    new("Refused", "Refusal recorded", 19)
                },
                DcmsCounts: new List<CountItem>
                {
                    new("A1", "Accepted and complete", 446),
                    new("B2", "Partial response", 62),
                    new("C3", "Non-contact", 58)
                },
                TotalReceived: 544,
                TotalDeleted: 9,
                BudgetAllocation: 145_000m,
                RespondentInstancesLast1Year: 2,
                RespondentInstancesLast3Years: 4,
                RespondentInstancesLast5Years: 5,
                ResponseHistoryRate: 62.5m,
                ResponseHistoryBreakdown: new List<ResponseHistoryItem>
                {
                    new("Completed", 3),
                    new("Partial", 1),
                    new("Refused", 1)
                }
            ),
            new SurveyInstance(
                SurveyId: 1004,
                SampleId: "SMP-1004-A",
                SampleName: "DIS Monthly Frame",
                ReferenceDate: new DateTime(2026, 3, 15),
                Title: "Dairy Inventory Survey (DIS)",
                SubTitle: "Monthly inventory update",
                SurveyFrequency: "Monthly",
                Version: "v5.4",
                SurveyDate: new DateTime(2026, 3, 15),
                SurveyStartDate: new DateTime(2026, 3, 1),
                SurveyStopDate: new DateTime(2026, 3, 31),
                HqSurveyAdmin: "R. Gomez",
                ProjectCode: "DIS-2026-03",
                Status: "Not started",
                OmbNumber: "0535-0321",
                OmbExpires: new DateTime(2027, 12, 31),
                State: "WI",
                Region: "GLR",
                StateId: "55",
                StateAlpha: "WI",
                DcmsCodeId: "B2",
                OpDomStatusId: "Active",
                EnumeratorId: "E-105",
                EnumeratorName: "H. Gomez",
                ManagerId: "M-204",
                ManagerName: "R. Lopez",
                CoachId: "C-305",
                CoachName: "B. Chen",
                EnumeratorNotes: "March cycle pending start.",
                ResponseCode: "Pending",
                Mode: "CAPI",
                Modes: new List<ModeWindow>
                {
                    new("CAPI", new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)),
                    new("MAIL", new DateTime(2026, 3, 4), new DateTime(2026, 3, 22))
                },
                OpDomCounts: new List<CountItem>
                {
                    new("Active", "Open cases currently in field", 233),
                    new("Complete", "Final disposition received", 498),
                    new("Refused", "Refusal recorded", 24)
                },
                DcmsCounts: new List<CountItem>
                {
                    new("A1", "Accepted and complete", 429),
                    new("B2", "Partial response", 69),
                    new("C3", "Non-contact", 57)
                },
                TotalReceived: 529,
                TotalDeleted: 13,
                BudgetAllocation: 149_000m,
                RespondentInstancesLast1Year: 2,
                RespondentInstancesLast3Years: 4,
                RespondentInstancesLast5Years: 5,
                ResponseHistoryRate: 60.0m,
                ResponseHistoryBreakdown: new List<ResponseHistoryItem>
                {
                    new("Completed", 3),
                    new("Partial", 1),
                    new("No response", 1)
                }
            )
        };
        ExpandInstances(instances, 100);
        return instances;
    }

    private static void ExpandInstances(List<SurveyInstance> instances, int targetCount)
    {
        if (instances.Count >= targetCount)
            return;

        var titles = new[]
        {
            "Grain Stocks Survey",
            "Livestock Slaughter Report",
            "Hogs and Pigs Survey",
            "Acreage Report",
            "Cotton Objective Yield Survey",
            "Cattle Inventory Survey",
            "Milk Production Survey",
            "Rice Stocks Survey",
            "Vegetable Production Survey",
            "Fruit Chemical Use Survey"
        };

        var statuses = new[] { "Not started", "In progress", "Blocked", "Overdue", "IN HQ REVIEW" };
        var states = new[] { "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA", "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY", "PR" };
        var modes = new[] { "CASI", "CAPI", "CATI", "MAIL" };
        var rand = new Random(42);

        var nextSurveyId = instances.Max(i => i.SurveyId) + 1;
        while (instances.Count < targetCount)
        {
            var title = titles[rand.Next(titles.Length)];
            var year = rand.Next(2025, 2028);
            var referenceDate = new DateTime(year, rand.Next(1, 13), rand.Next(1, 25));
            var surveyDate = referenceDate;
            var startDate = surveyDate.AddDays(rand.Next(0, 5));
            var stopDate = startDate.AddDays(rand.Next(20, 120));
            var surveyId = nextSurveyId++;
            var sampleId = $"SMP-{surveyId}-{(char)('A' + rand.Next(0, 3))}";
            var sampleName = $"{title.Split(' ')[0]} Frame {year}";
            var status = statuses[rand.Next(statuses.Length)];
            var mode = modes[rand.Next(modes.Length)];
            var stateAlpha = states[rand.Next(states.Length)];
            var dcmsCode = new[] { "A1", "B2", "C3" }[rand.Next(0, 3)];
            var opDomStatus = new[] { "Active", "Complete", "Refused" }[rand.Next(0, 3)];
            var responseHistory = BuildResponseHistory(rand);

            instances.Add(new SurveyInstance(
                SurveyId: surveyId,
                SampleId: sampleId,
                SampleName: sampleName,
                ReferenceDate: referenceDate,
                Title: $"{title} ({year})",
                SubTitle: "Standard cycle",
                SurveyFrequency: "Annual",
                Version: $"v{rand.Next(1, 4)}.{rand.Next(0, 9)}",
                SurveyDate: surveyDate,
                SurveyStartDate: startDate,
                SurveyStopDate: stopDate,
                HqSurveyAdmin: "TBD",
                ProjectCode: $"{title.Split(' ')[0].ToUpperInvariant()}-{year}",
                Status: status,
                OmbNumber: $"0535-{rand.Next(1000, 9999)}",
                OmbExpires: surveyDate.AddYears(rand.Next(2, 4)),
                State: stateAlpha,
                Region: "N/A",
                StateId: rand.Next(1, 57).ToString("D2"),
                StateAlpha: stateAlpha,
                DcmsCodeId: dcmsCode,
                OpDomStatusId: opDomStatus,
                EnumeratorId: $"E-{rand.Next(100, 999)}",
                EnumeratorName: $"Enum {rand.Next(1, 50)}",
                ManagerId: $"M-{rand.Next(100, 999)}",
                ManagerName: $"Mgr {rand.Next(1, 30)}",
                CoachId: $"C-{rand.Next(100, 999)}",
                CoachName: $"Coach {rand.Next(1, 20)}",
                EnumeratorNotes: $"Note {rand.Next(1, 8)}: {title.Split(' ')[0]} follow-up.",
                ResponseCode: new[] { "Complete", "Pending", "Refused" }[rand.Next(0, 3)],
                Mode: mode,
                Modes: new List<ModeWindow>
                {
                    new("CASI", startDate, stopDate),
                    new("CAPI", startDate.AddDays(2), stopDate.AddDays(-5)),
                    new("CATI", startDate.AddDays(5), stopDate),
                    new("MAIL", startDate.AddDays(7), stopDate.AddDays(-7))
                },
                OpDomCounts: new List<CountItem>
                {
                    new("Active", "Open cases currently in field", rand.Next(50, 600)),
                    new("Complete", "Final disposition received", rand.Next(200, 1200)),
                    new("Refused", "Refusal recorded", rand.Next(5, 80))
                },
                DcmsCounts: new List<CountItem>
                {
                    new("A1", "Accepted and complete", rand.Next(150, 1000)),
                    new("B2", "Partial response", rand.Next(20, 200)),
                    new("C3", "Non-contact", rand.Next(10, 150))
                },
                TotalReceived: rand.Next(200, 1500),
                TotalDeleted: rand.Next(0, 60),
                BudgetAllocation: rand.Next(80_000, 500_000),
                RespondentInstancesLast1Year: responseHistory.Last1Year,
                RespondentInstancesLast3Years: responseHistory.Last3Years,
                RespondentInstancesLast5Years: responseHistory.Last5Years,
                ResponseHistoryRate: responseHistory.ResponseRate,
                ResponseHistoryBreakdown: responseHistory.Breakdown
            ));
        }
    }

    private static ResponseHistory BuildResponseHistory(Random rand)
    {
        var last5 = rand.Next(3, 12);
        var last3 = Math.Min(last5, rand.Next(1, last5 + 1));
        var last1 = Math.Min(last3, rand.Next(0, Math.Min(3, last3) + 1));

        var completed = rand.Next(1, last5);
        var partial = rand.Next(0, last5 - completed + 1);
        var refused = rand.Next(0, last5 - completed - partial + 1);
        var noResponse = last5 - completed - partial - refused;

        var breakdown = new List<ResponseHistoryItem>
        {
            new("Completed", completed),
            new("Partial", partial),
            new("Refused", refused),
            new("No response", noResponse)
        }.Where(item => item.Count > 0).ToList();

        var responseRate = Math.Round((decimal)completed / last5 * 100m, 1);

        return new ResponseHistory(
            Last1Year: last1,
            Last3Years: last3,
            Last5Years: last5,
            ResponseRate: responseRate,
            Breakdown: breakdown
        );
    }

    private static List<SurveyRecordIndexItem> BuildInstanceGridRecords(SurveyInstance instance, int count)
    {
        var records = new List<SurveyRecordIndexItem>(count);
        var itemCount = Math.Max(6, count);
        for (var i = 0; i < itemCount; i++)
        {
            var fullRecord = BuildFullRecord(instance, i + 1);
            var poid = fullRecord.FirstOrDefault(f => f.Field == "poid")?.Value ?? string.Empty;
            var targetPoid = fullRecord.FirstOrDefault(f => f.Field == "target_poid")?.Value ?? string.Empty;
            records.Add(new SurveyRecordIndexItem(
                instance.SurveyId,
                instance.SampleId,
                instance.ReferenceDate,
                poid,
                targetPoid,
                instance.Mode,
                instance.StateAlpha,
                instance.StateId,
                $"S-{instance.SurveyId}-{(i + 1):D3}"));
        }

        return records;
    }

    private static string FormatModeDate(DateTime? value)
        => value.HasValue ? value.Value.ToString("yyyy-MM-dd") : "N/A";

    private static List<DetailField> BuildFullRecord(SurveyInstance instance, int variation = 0)
    {
        var rand = CreateStableRandom(instance.SurveyId, instance.SampleId, instance.ReferenceDate, variation);
        var countyId = rand.Next(1, 999).ToString("D3");
        var tract = rand.Next(100000, 999999).ToString();
        var subtract = rand.Next(10, 99).ToString();
        var poid = rand.Next(1000000, 9999999).ToString();
        var zip5 = rand.Next(10000, 99999).ToString();
        var zip4 = rand.Next(1000, 9999).ToString();
        var phone = $"{rand.Next(200, 989)}-{rand.Next(200, 999)}-{rand.Next(1000, 9999)}";
        var addrNumber = rand.Next(100, 9999);
        var placeName = $"{instance.StateAlpha} City {rand.Next(1, 40)}";
        var operName = $"Oper {rand.Next(100, 999)} Farms";
        var wholeName = $"Whole {rand.Next(100, 999)} Holdings";
        var respDate = instance.ReferenceDate.AddDays(rand.Next(1, 20)).ToString("yyyy-MM-dd");
        var pmcFlag = rand.Next(0, 2) == 0 ? "N" : "Y";
        var rfoFlag = rand.Next(0, 2) == 0 ? "N" : "Y";
        var mseqnum = rand.Next(1, 9999).ToString();
        var email = $"{instance.SampleId.ToLowerInvariant()}@demo.usda.gov";
        var opmail = $"PO Box {rand.Next(100, 9999)}";
        var officeNotes = $"Office note {rand.Next(1, 6)}";
        var enumNotes = instance.EnumeratorNotes;
        var sampNo = rand.Next(100000, 999999).ToString();
        var epaId = $"EPA-{rand.Next(1000, 9999)}";
        var label1 = $"L1-{rand.Next(10, 99)}";
        var label2 = $"L2-{rand.Next(10, 99)}";
        var label3 = $"L3-{rand.Next(10, 99)}";
        var label4 = $"L4-{rand.Next(10, 99)}";
        var label5 = $"L5-{rand.Next(10, 99)}";
        var labels = $"{label1},{label2},{label3},{label4},{label5}";
        var otherField = $"Other {rand.Next(10, 99)}";
        var latitude = (30 + rand.NextDouble() * 15).ToString("0.0000", CultureInfo.InvariantCulture);
        var longitude = (-120 + rand.NextDouble() * 15).ToString("0.0000", CultureInfo.InvariantCulture);
        // Use a demo-friendly numeric frame id in range 0..13 so UI redirects that expect 0-13 will work
        var frameId = rand.Next(0, 14).ToString(CultureInfo.InvariantCulture);
        var segmentId = $"SEG-{rand.Next(1000, 9999)}";
        var afTract = $"AF-{rand.Next(10000, 99999)}";
        var surveyWebCode = $"WEB-{rand.Next(100000, 999999)}";
        var tier = $"T{rand.Next(1, 4)}";
        var xstateLink = $"XSL-{rand.Next(1000, 9999)}";
        var coordinationId = $"COORD-{rand.Next(1000, 9999)}";
        var censusVars = $"CVAR-{rand.Next(10, 99)}";
        var availableModes = instance.Modes.Select(m => NormalizeCollectionMode(m.Mode)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (availableModes.Count == 0)
            availableModes.Add(NormalizeCollectionMode(instance.Mode));
        var hrMode1Year = availableModes[rand.Next(availableModes.Count)];
        var hrMode3Year = availableModes[rand.Next(availableModes.Count)];
        var hrMode5Year = availableModes[rand.Next(availableModes.Count)];
        var hrPct1Year = Math.Max(25, Math.Min(100, instance.RespondentInstancesLast1Year * 28 + rand.Next(-8, 9)));
        var hrPct3Year = Math.Max(25, Math.Min(100, instance.RespondentInstancesLast3Years * 18 + rand.Next(-6, 7)));
        var hrPct5Year = Math.Max(25, Math.Min(100, instance.RespondentInstancesLast5Years * 12 + rand.Next(-5, 6)));

        return new List<DetailField>
        {
            new("project_code", instance.ProjectCode),
            new("sample_id", instance.SampleId),
            new("sample_name", instance.SampleName),
            new("survey_id", instance.SurveyId.ToString()),
            new("survey_title", instance.Title),
            new("survey_subtitle", instance.SubTitle),
            new("survey_date", instance.SurveyDate.ToString("yyyy-MM-dd")),
            new("state_id", instance.StateId),
            new("stratum", $"STR-{rand.Next(10, 99)}"),
            new("target_poid", $"TP-{rand.Next(10000, 99999)}"),
            new("tract", tract),
            new("subtract", subtract),
            new("poid", poid),
            new("dcms_code_id", instance.DcmsCodeId),
            new("oper_name", operName),
            new("whole_name", wholeName),
            new("addr_delivery", $"{addrNumber} {placeName} Rd"),
            new("addr_other", $"Suite {rand.Next(100, 999)}"),
            new("place_name", placeName),
            new("state_alpha", instance.StateAlpha),
            new("zip5", zip5),
            new("zip4", zip4),
            new("op_addr_delivery", $"{addrNumber + 10} {placeName} Ave"),
            new("op_address_other", $"Box {rand.Next(10, 99)}"),
            new("op_placename", placeName),
            new("op_state_alpha", instance.StateAlpha),
            new("op_zip5", zip5),
            new("op_zip4", zip4),
            new("phone_char", phone),
            new("enumerator_id", instance.EnumeratorId),
            new("enumerator_name", instance.EnumeratorName),
            new("manager_id", instance.ManagerId),
            new("manager_name", instance.ManagerName),
            new("coach_id", instance.CoachId),
            new("coach_name", instance.CoachName),
            new("respdate", respDate),
            new("pmc_mail_flag", pmcFlag),
            new("rfo_mail_flag", rfoFlag),
            new("casi_flag", instance.Modes.Any(m => m.Mode == "CASI") ? "Y" : "N"),
            new("capi_flag", instance.Modes.Any(m => m.Mode == "CAPI") ? "Y" : "N"),
            new("cati_flag", instance.Modes.Any(m => m.Mode == "CATI") ? "Y" : "N"),
            new("mail_start_date", FormatModeDate(instance.Modes.FirstOrDefault(m => m.Mode == "MAIL")?.StartDate)),
            new("mail_stop_date", FormatModeDate(instance.Modes.FirstOrDefault(m => m.Mode == "MAIL")?.StopDate)),
            new("casi_start_date", FormatModeDate(instance.Modes.FirstOrDefault(m => m.Mode == "CASI")?.StartDate)),
            new("casi_stop_date", FormatModeDate(instance.Modes.FirstOrDefault(m => m.Mode == "CASI")?.StopDate)),
            new("capi_start_date", FormatModeDate(instance.Modes.FirstOrDefault(m => m.Mode == "CAPI")?.StartDate)),
            new("capi_stop_date", FormatModeDate(instance.Modes.FirstOrDefault(m => m.Mode == "CAPI")?.StopDate)),
            new("cati_start_date", FormatModeDate(instance.Modes.FirstOrDefault(m => m.Mode == "CATI")?.StartDate)),
            new("cati_stop_date", FormatModeDate(instance.Modes.FirstOrDefault(m => m.Mode == "CATI")?.StopDate)),
            new("op_county_id", countyId),
            new("opcounty_name", $"{placeName} County"),
            new("op_dom_status_id", instance.OpDomStatusId),
            new("mseqnum", mseqnum),
            new("e_mail", email),
            new("opmail", opmail),
            new("office_notes", officeNotes),
            new("enum_notes", enumNotes),
            new("samp_no", sampNo),
            new("epa_id", epaId),
            new("label1", label1),
            new("label2", label2),
            new("label3", label3),
            new("label4", label4),
            new("label5", label5),
            new("label1-5", labels),
            new("other_field", otherField),
            new("latitude", latitude),
            new("longitude", longitude),
            new("latitude longitude", $"{latitude}, {longitude}"),
            new("frame_id", frameId),
            new("segment_id", segmentId),
            new("af_tract", afTract),
            new("response_code", instance.ResponseCode),
            new("state-abbrev", instance.StateAlpha),
            new("survey code for web access", surveyWebCode),
            new("tier", tier),
            new("xstatelink (casi)", xstateLink),
            new("coordination_id (capi)", coordinationId),
            new("Census variables", censusVars),
            new("hr_surveys_1yr", instance.RespondentInstancesLast1Year.ToString(CultureInfo.InvariantCulture)),
            new("hr_pct_1yr", hrPct1Year.ToString(CultureInfo.InvariantCulture)),
            new("hr_mode_1yr", hrMode1Year),
            new("hr_surveys_3yr", instance.RespondentInstancesLast3Years.ToString(CultureInfo.InvariantCulture)),
            new("hr_pct_3yr", hrPct3Year.ToString(CultureInfo.InvariantCulture)),
            new("hr_mode_3yr", hrMode3Year),
            new("hr_surveys_5yr", instance.RespondentInstancesLast5Years.ToString(CultureInfo.InvariantCulture)),
            new("hr_pct_5yr", hrPct5Year.ToString(CultureInfo.InvariantCulture)),
            new("hr_mode_5yr", hrMode5Year)
        };
    }

    private static Random CreateStableRandom(int surveyId, string sampleId, DateTime referenceDate, int variation = 0)
    {
        var hash = StableHash(sampleId);
        var seed = HashCode.Combine(surveyId, hash, referenceDate.Year, referenceDate.Month, referenceDate.Day, variation);
        return new Random(seed);
    }

    private static List<SurveyDesignerAssociation> BuildSurveyDesignerAssociations(SurveyInstance instance)
    {
        var rand = CreateStableRandom(instance.SurveyId * 17, instance.SampleId, instance.ReferenceDate);
        var count = rand.Next(1, 4);
        var associations = new List<SurveyDesignerAssociation>();
        var modes = instance.Modes.Select(m => m.Mode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (modes.Count == 0)
        {
            modes.Add(instance.Mode);
        }

        for (var i = 0; i < count; i++)
        {
            var sKeyNumber = rand.Next(100, 999);
            var sKey = $"S-{instance.SurveyId}-{sKeyNumber}";
            var questionnaireId = $"QNR-{instance.SurveyId}-{rand.Next(10, 99)}";
            var mode = modes[i % modes.Count];
            var version = $"v{rand.Next(1, 5)}.{rand.Next(0, 10)}";
            var status = rand.Next(0, 3) switch
            {
                0 => "Published",
                1 => "NASS Review",
                _ => "Draft"
            };

            foreach (var format in new[] { "Paper", "Web" })
            {
                var artifactId = $"{questionnaireId}-{format.ToUpperInvariant()}";
                associations.Add(new SurveyDesignerAssociation(
                    sKey,
                    artifactId,
                    mode,
                    version,
                    format,
                    status,
                    $"https://sms-lite.demo/questionnaires/{artifactId}",
                    $"https://sms-lite.demo/specifications/{instance.SurveyId}/{sKey}/{format.ToLowerInvariant()}",
                    $"https://sms-lite.demo/metadata/{instance.SurveyId}/{sKey}",
                    BuildSpecificationItems(rand, instance.SurveyId)));
            }
        }

        return associations
            .OrderBy(a => a.SKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.QuestionnaireFormat, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<QuestionnaireSpecItem> BuildSpecificationItems(Random rand, int surveyId)
    {
        var count = rand.Next(2, 5);
        var items = new List<QuestionnaireSpecItem>();
        for (var i = 0; i < count; i++)
        {
            var code = $"I-{surveyId % 1000:D3}-{rand.Next(10, 99)}";
            items.Add(new QuestionnaireSpecItem(
                code,
                $"Mock item description for {code}"));
        }

        return items
            .DistinctBy(i => i.ItemCode)
            .OrderBy(i => i.ItemCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<CollectionMaterial> BuildCollectionMaterials(SurveyInstance instance)
    {
        var rand = CreateStableRandom(instance.SurveyId * 31, instance.SampleId, instance.ReferenceDate, variation: 53);
        var availableModes = instance.Modes
            .Select(m => NormalizeCollectionMode(m.Mode))
            .Where(m => m is "EDR" or "CAPI" or "CATI" or "Mail")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (availableModes.Count == 0)
            availableModes.Add("Mail");

        var materialTypes = new[]
        {
            "Presurvey Letter",
            "Questionnaire",
            "Pressure Seal",
            "Follow-up Letter",
            "Thank You Letter"
        };

        var materials = new List<CollectionMaterial>(availableModes.Count * materialTypes.Length);
        foreach (var mode in availableModes)
        {
            foreach (var type in materialTypes)
            {
                var uploadDate = instance.ReferenceDate.AddDays(-rand.Next(2, 45));
                var extension = type.Equals("Questionnaire", StringComparison.OrdinalIgnoreCase) ? "pdf" : "docx";
                var fileSizeBytes = type switch
                {
                    "Questionnaire" => rand.NextInt64(1_400_000, 4_800_000),
                    "Pressure Seal" => rand.NextInt64(160_000, 520_000),
                    _ => rand.NextInt64(220_000, 1_100_000)
                };

                materials.Add(new CollectionMaterial(
                    $"{instance.Title} {mode} {type}",
                    type,
                    mode,
                    $"v{rand.Next(1, 5)}.{rand.Next(0, 10)}",
                    uploadDate,
                    fileSizeBytes,
                    $"{Slugify(instance.Title)}-{mode.ToLowerInvariant()}-{Slugify(type)}.{extension}"));
            }
        }

        return materials
            .OrderByDescending(m => m.UploadDate)
            .ThenBy(m => m.CollectionMode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.MaterialType, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeCollectionMode(string mode)
        => mode.ToUpperInvariant() switch
        {
            "CAWI" => "EDR",
            "CASI" => "EDR",
            "MAIL" => "Mail",
            "CAPI" => "CAPI",
            "CATI" => "CATI",
            _ => mode
        };

    private static string Slugify(string value)
        => string.Join("-",
            value.Split(new[] { ' ', '/', '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToLowerInvariant();

    public SurveyRespondentRecord? GetRespondentByPoid(DateTime referenceDate, int surveyId, string sampleId, string poid)
        => GetRespondentsForInstance(referenceDate, surveyId, sampleId, null, 30)
            .FirstOrDefault(r =>
                r.Fields.FirstOrDefault(f => f.Field.Equals("poid", StringComparison.OrdinalIgnoreCase))?.Value
                    ?.Equals(poid, StringComparison.OrdinalIgnoreCase) == true);

    public IReadOnlyList<RespondentTimelineEvent> GetRespondentTimeline(string poid, int surveyId, string sampleId)
    {
        if (string.IsNullOrWhiteSpace(poid))
            return [];

        var seed = StableHash($"{poid}|{surveyId}|{sampleId}|timeline");
        var rand = new Random(seed);
        var relatedInstances = _instances
            .Where(i => i.SampleId.Equals(sampleId, StringComparison.OrdinalIgnoreCase) || i.SurveyId == surveyId)
            .OrderByDescending(i => i.ReferenceDate)
            .Take(8)
            .ToList();

        if (relatedInstances.Count == 0)
            return [];

        var events = new List<RespondentTimelineEvent>();
        foreach (var instance in relatedInstances)
        {
            var eventDate = instance.ReferenceDate.AddDays(rand.Next(0, 18));
            events.Add(new RespondentTimelineEvent(
                eventDate,
                "Survey Iteration",
                "SMS",
                $"{instance.Title} ({instance.SampleId}) iteration opened for respondent {poid}.",
                $"surveys/details?referenceDate={instance.ReferenceDate:yyyy-MM-dd}&surveyId={instance.SurveyId}&sampleId={Uri.EscapeDataString(instance.SampleId)}"));

            events.Add(new RespondentTimelineEvent(
                eventDate.AddDays(2),
                instance.Mode.Equals("MAIL", StringComparison.OrdinalIgnoreCase) ? "Mail Sent" : "Call Attempt",
                "Survey Ops",
                $"Primary contact attempt recorded in {NormalizeCollectionMode(instance.Mode)} mode.",
                $"surveys/record?referenceDate={instance.ReferenceDate:yyyy-MM-dd}&surveyId={instance.SurveyId}&sampleId={Uri.EscapeDataString(instance.SampleId)}"));

            if (rand.Next(0, 2) == 0)
            {
                events.Add(new RespondentTimelineEvent(
                    eventDate.AddDays(4),
                    "Response Received",
                    "SMS",
                    $"Disposition captured as {instance.ResponseCode}.",
                    $"respondents/details?poid={Uri.EscapeDataString(poid)}&surveyId={instance.SurveyId}&sampleId={Uri.EscapeDataString(instance.SampleId)}&referenceDate={instance.ReferenceDate:yyyy-MM-dd}&stateId={Uri.EscapeDataString(instance.StateId)}"));
            }
        }

        return events
            .OrderByDescending(e => e.EventDate)
            .ToList();
    }

    private static int StableHash(string input)
    {
        unchecked
        {
            int hash = 23;
            foreach (var ch in input)
            {
                hash = (hash * 31) + ch;
            }
            return hash;
        }
    }
}

public sealed record SurveyInstance(
    int SurveyId,
    string SampleId,
    string SampleName,
    DateTime ReferenceDate,
    string Title,
    string SubTitle,
    string SurveyFrequency,
    string Version,
    DateTime SurveyDate,
    DateTime SurveyStartDate,
    DateTime SurveyStopDate,
    string HqSurveyAdmin,
    string ProjectCode,
    string Status,
    string OmbNumber,
    DateTime OmbExpires,
    string State,
    string Region,
    string StateId,
    string StateAlpha,
    string DcmsCodeId,
    string OpDomStatusId,
    string EnumeratorId,
    string EnumeratorName,
    string ManagerId,
    string ManagerName,
    string CoachId,
    string CoachName,
    string EnumeratorNotes,
    string ResponseCode,
    string Mode,
    List<ModeWindow> Modes,
    List<CountItem> OpDomCounts,
    List<CountItem> DcmsCounts,
    int TotalReceived,
    int TotalDeleted,
    decimal BudgetAllocation,
    int RespondentInstancesLast1Year,
    int RespondentInstancesLast3Years,
    int RespondentInstancesLast5Years,
    decimal ResponseHistoryRate,
    List<ResponseHistoryItem> ResponseHistoryBreakdown
);

public sealed record SurveyGridRow(
    int? SampleId,
    int? Fips,
    int? SurveyId,
    int? StateId,
    DateTime? SurveyDate,
    int? SKey,
    int? Poid,
    decimal? TargetPoid,
    int? Tract,
    int? Subtract,
    int? FrameId,
    int? OperationId,
    int? PersonId,
    int? ObjectiveYieldId,
    int? CommentFlag,
    string? Comments,
    string? OfficeNotes,
    int? PeriodId,
    int? Stratum,
    int? PartnerNo,
    int? Rep,
    int? KZero,
    int? X1,
    int? X2,
    int? X3,
    int? X4,
    int? X5,
    int? Flag1,
    int? Flag2,
    int? Flag3,
    int? Flag4,
    int? Flag5,
    short? HoldFlag,
    int? SZeroFlag,
    string? Label1,
    string? Label2,
    string? Label3,
    string? Label4,
    string? Label5,
    string? OtherField,
    short? CatiFlag,
    string? CatiGroup,
    short? CapiFlag,
    short? PhoneOnlyFlag,
    short? CawiFlag,
    short? MailFlag,
    short? PmcMailFlag,
    short? RfoMailFlag,
    DateTime? MailStartDate,
    DateTime? MailStopDate,
    DateTime? CawiStartDate,
    DateTime? CawiStopDate,
    DateTime? CapiStartDate,
    DateTime? CapiStopDate,
    DateTime? CatiStartDate,
    DateTime? CatiStopDate,
    short? CatiApp,
    int? ActiveStatusId,
    int? OpDomStatusId,
    int? ManagerFlag,
    string? ManagerFlagChar,
    decimal? StatePoid,
    string? OperationName,
    string? OperationAddress,
    string? OperationOtherAddress,
    string? OperationPlaceName,
    string? OperationStateAbbreviation,
    string? OperationZip5,
    string? OperationZip4,
    string? OperationPhone,
    string? OperationPhoneChar,
    int? OperationCellPhoneFlag,
    string? OperationCellPhoneFlagChar,
    string? OperationFax,
    string? OperationFaxChar,
    int? OperationFarmFlag,
    int? AgriculturalBusinessFlag,
    int? TaggedId,
    int? OperationCountyId,
    int? OperationDistrictId,
    string? OperationEmail,
    int? PersonStatusId,
    string? LastName,
    string? FirstName,
    string? MiddleName,
    string? SuffixId,
    string? WholeName,
    string? Address,
    string? OtherAddress,
    string? PersonPlaceName,
    string? PersonStateAbbreviation,
    string? PersonZip5,
    string? PersonZip4,
    string? PersonPhone,
    string? PersonPhoneChar,
    int? PersonCellPhoneFlag,
    string? PersonCellPhoneFlagChar,
    string? PersonOtherPhone,
    string? PersonOtherPhoneChar,
    int? PersonOtherCellPhoneFlag,
    string? PersonOtherCellPhoneFlagChar,
    string? PersonFaxPhone,
    string? PersonFaxPhoneChar,
    int? CountyId,
    int? DistrictId,
    string? PersonEmail,
    int? LanguageId,
    decimal? Latitude,
    decimal? Longitude,
    int? DataCollectionModeCodeId,
    int? DcmsCodeId,
    int? PermSupervisorId,
    int? PermEnumeratorId,
    string? SurveyCode,
    string? Barcode,
    int? RfoReviewFlag,
    int? MarkedFlag,
    int? Mseqnum,
    string? CountyName,
    string? OperationCountyName,
    DateTime? RespDate,
    DateTime? DataPull,
    int? ResponseCode,
    int? RespondentCode,
    int? ReportingModeCode,
    int? DataCaptureCode,
    int? DataCollectionEnumeratorId,
    DateTime? DataCollectionDate,
    short? ReviewFlag,
    int? StartedFlag,
    int? SavedFlag,
    int? OutOfBusinessId,
    string? XStateLink,
    long? CoordinationId,
    string? SurveyTitle,
    string? SurveySubtitle,
    string? Version,
    string? StateName,
    string? StateAbbreviation,
    string? OperationComments,
    string? PersonComments,
    int? BackupDcmsCode,
    string? BackupEnumeratorId,
    string? SupervisorId,
    string? EnumeratorId,
    string? EnumeratorName,
    string? SupervisorName,
    string? BackupSupervisorId,
    int? SmsCheckIn,
    short? HistoricResponsePercentage1Year,
    short? HistoricResponseSurveys1Year,
    string? HistoricResponseMode1Year,
    short? HistoricResponsePercentage3Years,
    short? HistoricResponseSurveys3Years,
    string? HistoricResponseMode3Years,
    short? HistoricResponsePercentage5Years,
    short? HistoricResponseSurveys5Years,
    string? HistoricResponseMode5Years,
    int? StartDcDcmsCode,
    int? StartDcEnumerator,
    int? StartDcSupervisor,
    short? PartnershipFlag,
    int? AddedRecordFlag,
    string? EnumeratorNotes,
    int? CapiAppointmentFlag,
    int? CapiAttemptedContacts,
    int? Tier,
    int? ReactivatedFlag,
    DateTime? ReactivatedFlagDate,
    string? ManagerId,
    string? ManagerName,
    short? ComputedSkill,
    short? ManualSkill,
    short? CoordinationFlag,
    int? Ruid
)
{
    public string SampleName => "N/A";
    public string ElmoSurveyId => "N/A";
    public string OmbNumber => "N/A";
}

public sealed record FilterOptionsResponse(
    List<string> AvailableReferenceDates,
    List<int> AvailableSurveyIds,
    List<string> AvailableSampleIds
);

public sealed record SurveyInstanceDetailResponse(
    string SampleId,
    string SampleName,
    int SurveyId,
    string Title,
    string SubTitle,
    string SurveyFrequency,
    string Version,
    DateTime SurveyDate,
    DateTime ReferenceDate,
    DateTime SurveyStartDate,
    DateTime SurveyStopDate,
    string HqSurveyAdmin,
    string ProjectCode,
    string OmbDocket,
    string OmbExpiration,
    List<ModeWindow> Modes,
    List<CountItem> OpDomCounts,
    List<CountItem> DcmsCounts,
    List<CountItem> DataCollectionStatusCounts,
    List<CountItem> ReportsReceivedByModeCounts,
    int TotalSample,
    int TotalReceived,
    int TotalDeleted,
    decimal BudgetAllocation,
    int RespondentInstancesLast1Year,
    int RespondentInstancesLast3Years,
    int RespondentInstancesLast5Years,
    decimal ResponseHistoryRate,
    List<ResponseHistoryItem> ResponseHistoryBreakdown,
    List<SurveyDesignerAssociation> SurveyDesignerAssociations,
    List<CollectionMaterial> CollectionMaterials,
    List<SurveyDetailRecordRow> RecordRows,
    List<DetailField> FullRecord
);

public sealed record ModeWindow(string Mode, DateTime? StartDate, DateTime? StopDate);

public sealed record CountItem(string Code, string Definition, int Count);

public sealed record ResponseHistoryItem(string Label, int Count);

public sealed record DetailField(string Field, string Value);

public sealed record SurveyDetailRecordRow(
    string Fips,
    string State,
    string SKey,
    string Status,
    string Poid,
    string TargetPoid,
    string SampleId,
    DateTime ReferenceDate
);

public sealed record SurveyDesignerAssociation(
    string SKey,
    string QuestionnaireId,
    string CollectionMode,
    string QuestionnaireVersion,
    string QuestionnaireFormat,
    string QuestionnaireStatus,
    string QuestionnaireLink,
    string SpecificationsLink,
    string MetadataLink,
    List<QuestionnaireSpecItem> SpecificationItems
);

public sealed record QuestionnaireSpecItem(
    string ItemCode,
    string Description
);

public sealed record CollectionMaterial(
    string MaterialName,
    string MaterialType,
    string CollectionMode,
    string Version,
    DateTime UploadDate,
    long FileSizeBytes,
    string FileName
);

public sealed record SurveyRecordIndexItem(
    int SurveyId,
    string SampleId,
    DateTime ReferenceDate,
    string Poid,
    string TargetPoid,
    string Mode,
    string StateAlpha,
    string StateId,
    string SKey
);

public sealed record SurveyRecordLookupResult(
    DateTime ReferenceDate,
    int SurveyId,
    string SampleId,
    string Poid,
    string TargetPoid
);

public sealed record SurveyRespondentRecord(
    int RespondentId,
    int SurveyId,
    string SampleId,
    DateTime ReferenceDate,
    string StateId,
    string StateAlpha,
    string SKey,
    List<DetailField> Fields
);

public sealed record ResponseHistory(
    int Last1Year,
    int Last3Years,
    int Last5Years,
    decimal ResponseRate,
    List<ResponseHistoryItem> Breakdown
);

public sealed record RespondentTimelineEvent(
    DateTime EventDate,
    string EventType,
    string Actor,
    string Summary,
    string Link
);
