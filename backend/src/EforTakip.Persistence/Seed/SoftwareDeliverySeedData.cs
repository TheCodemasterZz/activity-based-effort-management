namespace EforTakip.Persistence.Seed;

/// <summary>
/// "Software Delivery" değer akışına ait sabit (gerçek) referans veri: aşamalar,
/// L1 aktivite grupları ve L2 aktiviteleri. Bogus ile üretilen rastgele sahte veriden
/// farklı olarak bu veri gerçek/sabit bir referans kataloğu olduğu için hem migration'ın
/// HasData'sına hem de (EnsureCreated üzerinden) Test Mode'a aynı kaynaktan uygulanır.
/// </summary>
public static class SoftwareDeliverySeedData
{
    public const string ValueStreamName = "Software Delivery";

    public static readonly Guid ValueStreamId = Id(1, 0);

    public static readonly (string Name, int Order)[] Stages =
    [
        ("Demand Definition", 1),
        ("Delivery Planning", 2),
        ("Requirements Scoping", 3),
        ("Technical Design", 4),
        ("Development", 5),
        ("Code Quality", 6),
        ("Build & Compilation", 7),
        ("Pre-deployment Testing", 8),
        ("Release Packing", 9),
        ("Deployment", 10),
        ("Post Deployment Testing & Validation", 11),
        ("Handover To Operations", 12),
        ("Go Live", 13),
        ("Maintenance", 14),
    ];

    /// <summary>L1 aktivite grubu adı -> altındaki L2 aktivite adları (tüm aşamalardaki görünümlerin birleşimi, tekrarsız).</summary>
    public static readonly (string L1, string[] L2)[] ActivityCatalog =
    [
        ("Analysis", [
            "Business Requirement Analysis", "Feasibility Study", "Gap Analysis", "Risk Analysis",
            "Stakeholder Identification & Workshops", "Requirements Elicitation, Analysis & Prioritization",
            "Functional / Non-Functional Req. Definition", "Acceptance Criteria & Business Rules Definition",
            "Impact Analysis", "Root Cause Analysis",
        ]),
        ("Enterprise Architecture", ["High-Level Solution Design", "Business Capability Impact Mapping"]),
        ("Solution Architecture", ["High-Level Resource & Budget Estimation", "Roadmapping"]),
        ("Regulatory Compliance", [
            "Standards & Framework Alignment", "Non-Conformance & Gap Risk Analysis",
            "Technical File & Documentation Pre-check",
        ]),
        ("Documentation", [
            "Business Requirements Document (BRD)", "High-Level Solution Design Document",
            "Project Management Plan", "Resource Plan", "Risk Register", "Work Breakdown Structure (WBS)",
            "Technical Requirements Document (TRD)", "Requirements Traceability Matrix (RTM)",
            "High-Level Design Document (HLD)", "Low-Level Design Document (LLD)",
            "Build Configuration Update", "Operational Runbook", "User Documentation",
        ]),
        ("Governance", ["Governance Meetings"]),
        ("Planning", [
            "Project Charter", "Scope Planning", "Schedule Planning", "Resource Planning",
            "Cost Planning", "Risk Planning", "Communication Planning", "Project Baseline Approval",
        ]),
        ("Requirements Quality Control", [
            "SQA Review (Software Quality Assurance Review)", "Stakeholder Review & Scope Baseline Approval",
            "Technical Review (Developer / Tester / Tech Lead)",
        ]),
        ("Traceability", [
            "Biz Req -> Tech Req Traceability", "Tech Req -> Design Spec Traceability",
            "Design -> Source Code Traceability", "Test Case Traceability",
        ]),
        ("Architecture Design", [
            "High-Level Design (System/Global Design)", "Low-Level Design (Component/Detailed Design)",
            "Integration & API / Interface Design", "Database & Data Model Design", "Solution Design",
            "Technical Standards", "Architecture Alignment", "Architecture Review",
        ]),
        ("Design Quality Control", ["SQA Review (Software Quality Assurance Review)", "Technical Team Peer Review"]),
        ("Development", [
            "Backend Development", "Frontend Development", "Database Development",
            "Interface / API Development", "Configuration Development", "Integration Development",
        ]),
        ("Test Automation", ["Automation Development", "Automation Pipeline", "E2E Test Suite"]),
        ("Testing", [
            "Unit Test Execution & Code Coverage Check", "Functional Testing", "Security Testing",
            "Performance Testing", "Regression Testing", "Integration Testing",
            "User Acceptance Testing (UAT)", "Test Closure & Reporting", "Smoke Testing", "Sanity Testing",
        ]),
        ("Code Quality Assurance", ["Code Review & Static Code Analysis", "SonarQube Review", "Secure Coding Review"]),
        ("Quality Assurance", [
            "QA Review", "Build Verification", "Defect Logging & Tracking", "Defect Management & Retesting",
            "Defect Management", "Test Review (SQA)", "Test Case Peer Review", "QA Sign-off",
        ]),
        ("DevOps", [
            "Build Execution", "CI/CD Pipeline", "Dependency Management", "Package Generation",
            "Release Package Preparation", "Artifact Packaging", "Rollback Planning",
            "Deploy to Staging", "Deploy to Production",
        ]),
        ("Infrastructure", ["Environment Configuration", "Environment Setup", "Infra Provisioning", "Cloud Configuration"]),
        ("Test Preparation & Design", [
            "Test Plan & Strategy Document Creation", "Test Case Writing & Test Scenario Design",
            "Test Data Preparation & Environment Readiness Check", "Test Environment Setup",
        ]),
        ("System Ops", ["System Configuration", "Service Registration", "Health Check", "Platform Management"]),
        ("Operations", [
            "Monitoring Setup", "Alerting Configuration", "Operations Handover", "Go-Live Checklist",
            "Incident Management", "Problem Management", "Change Management", "Service Request Management",
            "Event Management", "Service Level Management", "Configuration Management (CMDB)",
        ]),
        ("Knowledge Transfer", ["Knowledge Transfer Session"]),
        ("Training & Enablement", ["Administrator Training", "Technician / Technical Training", "End User Training"]),
    ];

    /// <summary>Aşama adı -> o aşamaya atanan L1 aktivite grubu adları.</summary>
    public static readonly (string Stage, string[] L1s)[] StageActivityMap =
    [
        ("Demand Definition", ["Analysis", "Enterprise Architecture", "Solution Architecture", "Regulatory Compliance", "Documentation", "Governance"]),
        ("Delivery Planning", ["Planning", "Requirements Quality Control", "Documentation", "Governance"]),
        ("Requirements Scoping", ["Analysis", "Traceability", "Requirements Quality Control", "Documentation", "Governance"]),
        ("Technical Design", ["Architecture Design", "Traceability", "Design Quality Control", "Documentation", "Governance"]),
        ("Development", ["Development", "Test Automation", "Testing", "Traceability"]),
        ("Code Quality", ["Code Quality Assurance", "Architecture Design", "Quality Assurance"]),
        ("Build & Compilation", ["DevOps", "Test Automation", "Infrastructure", "Quality Assurance", "Documentation"]),
        ("Pre-deployment Testing", ["Test Preparation & Design", "Testing", "Test Automation", "Quality Assurance", "Analysis", "Traceability"]),
        ("Release Packing", ["DevOps", "Quality Assurance", "Documentation", "Governance"]),
        ("Deployment", ["DevOps", "Infrastructure", "System Ops"]),
        ("Post Deployment Testing & Validation", ["Testing", "Operations", "Quality Assurance", "System Ops"]),
        ("Handover To Operations", ["Operations", "System Ops", "Knowledge Transfer", "Training & Enablement", "Documentation", "Governance"]),
        ("Go Live", ["Operations", "Training & Enablement", "System Ops", "Governance"]),
        ("Maintenance", ["Operations"]),
    ];

    /// <summary>Türkiye resmi tatil günleri (2026 yılı için sabit referans veri).</summary>
    public static readonly (int Month, int Day, string Name)[] Holidays =
    [
        (1, 1, "Yılbaşı"),
        (4, 23, "Ulusal Egemenlik ve Çocuk Bayramı"),
        (5, 1, "Emek ve Dayanışma Günü"),
        (5, 19, "Atatürk'ü Anma, Gençlik ve Spor Bayramı"),
        (7, 15, "Demokrasi ve Millî Birlik Günü"),
        (8, 30, "Zafer Bayramı"),
        (10, 29, "Cumhuriyet Bayramı"),
    ];

    public const int HolidayYear = 2026;

    private static Guid Id(int category, int index) => Guid.Parse($"00000000-0000-0000-{category:D4}-{index:D12}");

    public static Guid StageId(int order) => Id(2, order);

    public static Guid L1Id(int l1Index) => Id(3, l1Index * 1000);

    public static Guid L2Id(int l1Index, int l2Index) => Id(3, l1Index * 1000 + l2Index + 1);

    public static Guid AssignmentId(int n) => Id(4, n);

    public static Guid HolidayId(int n) => Id(5, n);

    /// <summary>L1 adı -> (l1Index, L1 Guid) sözlüğü; StageActivityMap ile L1Id'yi eşlemek için.</summary>
    public static IReadOnlyDictionary<string, (int Index, Guid Id)> L1Lookup { get; } =
        ActivityCatalog
            .Select((entry, index) => (entry.L1, Index: index, Id: L1Id(index)))
            .ToDictionary(x => x.L1, x => (x.Index, x.Id));
}
