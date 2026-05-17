using System;

namespace Skillforge.Utility;

public class ComplianceRecordUtility
{
    /// <summary>
    /// Utilities used to send a textual response
    /// </summary>
    public const string UpdateComplianceRecords = "The Compliance Records are Updated Successfully.";
    public const string DivideByZero = "No employees found to calculate compliance summary.";
    public const string FetchComplianceSummaryFailed = "An error occurred while fetching the compliance summary.";
    public const string RefreshComplianceRecordsFailed = "An error occurred while refreshing compliance records.";
    public const string DeleteComplianceRecordsFailed = "An error occurred while deleting existing compliance records.";
    public const string FetchCertificationsFailed = "An error occurred while fetching certifications for compliance refresh.";
}
