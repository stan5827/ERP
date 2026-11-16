$ErrorActionPreference = 'Stop'

$containerName = 'erp-main-wip-postgres'
$databaseName = 'ErpGeneral'
$expectedPortBinding = '127.0.0.1:55434'

$container = docker inspect --format '{{.Name}}' $containerName
if ($LASTEXITCODE -ne 0) {
    throw "Could not inspect the expected WIP database container '$containerName'."
}
if ($container -notmatch [regex]::Escape("/$containerName")) {
    throw "Database container identity mismatch; expected '$containerName'."
}
$portMapping = docker port $containerName 5432/tcp
if ($LASTEXITCODE -ne 0 -or $portMapping.Trim() -ne $expectedPortBinding) {
    throw "Database port binding mismatch; expected '$expectedPortBinding' -> 5432."
}

$targetDatabase = docker exec $containerName psql -U erp -d $databaseName -At -c 'SELECT current_database();'
if ($LASTEXITCODE -ne 0 -or $targetDatabase.Trim() -ne $databaseName) {
    throw "Database target mismatch; expected '$databaseName'."
}

$sql = @'
DO $$
DECLARE
    department_id uuid;
    job_title_id uuid;
    employee_id uuid;
BEGIN
    INSERT INTO public."Departments"
        ("Id", "DeptCode", "DeptName", "DeptType", "IsActive", "IsCostCenter", "DisplayOrder", "CreatedAt", "UpdatedAt")
    VALUES
        (gen_random_uuid(), 'WIP', 'WIP 測試部門', 'admin', TRUE, FALSE, 9000, NOW(), NOW())
    ON CONFLICT ("DeptCode") DO UPDATE
        SET "DeptName" = EXCLUDED."DeptName",
            "IsActive" = TRUE,
            "UpdatedAt" = NOW()
    RETURNING "Id" INTO department_id;

    SELECT "Id" INTO job_title_id
    FROM public."JobTitles"
    WHERE "TitleCode" = 'WIP-TEST'
    ORDER BY "CreatedAt", "Id"
    LIMIT 1;

    IF job_title_id IS NULL THEN
        INSERT INTO public."JobTitles"
            ("Id", "TitleCode", "TitleName", "ApplicableDeptType", "IsSupervisor", "IsActive", "Description", "CreatedAt")
        VALUES
            (gen_random_uuid(), 'WIP-TEST', 'WIP 測試職員', 'admin', FALSE, TRUE, 'WIP synthetic account-provisioning test data', NOW())
        RETURNING "Id" INTO job_title_id;
    ELSE
        UPDATE public."JobTitles"
        SET "TitleName" = 'WIP 測試職員',
            "ApplicableDeptType" = 'admin',
            "IsActive" = TRUE,
            "Description" = 'WIP synthetic account-provisioning test data'
        WHERE "Id" = job_title_id;
    END IF;

    SELECT "Id" INTO employee_id
    FROM public."Employees"
    WHERE "EmployeeNo" = 'WIP-DEMO-001';

    IF employee_id IS NULL THEN
        INSERT INTO public."Employees"
            ("Id", "EmployeeNo", "ChineseName", "EnglishName", "PassportNo", "Email",
             "DepartmentId", "JobTitleId", "PositionTitle", "JobCategory", "HireDate", "Status",
             "BaseSalary", "FoodAllowance", "PositionAllowance", "FullAttendanceBonus",
             "LaborInsuranceGrade", "HealthInsuranceGrade", "PensionContributionRate",
             "TaxDependents", "OvertimePermission", "CreatedAt", "UpdatedAt")
        VALUES
            (gen_random_uuid(), 'WIP-DEMO-001', 'WIP 測試員工', 'WIP Test Employee',
             'WIP-NO-REAL-DOCUMENT', 'wip.employee@example.invalid',
             department_id, job_title_id, 'WIP 測試職員', 'administrative', CURRENT_DATE, 'active',
             0, 0, 0, 0, 0, 0, 0, 0, FALSE, NOW(), NOW());
    ELSE
        UPDATE public."Employees"
        SET "ChineseName" = 'WIP 測試員工',
            "EnglishName" = 'WIP Test Employee',
            "PassportNo" = 'WIP-NO-REAL-DOCUMENT',
            "Email" = 'wip.employee@example.invalid',
            "DepartmentId" = department_id,
            "JobTitleId" = job_title_id,
            "PositionTitle" = 'WIP 測試職員',
            "JobCategory" = 'administrative',
            "HireDate" = COALESCE("HireDate", CURRENT_DATE),
            "Status" = 'active',
            "UpdatedAt" = NOW()
        WHERE "Id" = employee_id;
    END IF;
END $$;
'@

$sql | docker exec -i $containerName psql -v ON_ERROR_STOP=1 -U erp -d $databaseName
if ($LASTEXITCODE -ne 0) {
    throw 'Could not seed the synthetic employee in the isolated WIP database.'
}

$verificationSql = @'
SELECT e."EmployeeNo", e."ChineseName", e."Status",
       e."PassportNo", e."HireDate" IS NOT NULL,
       d."DeptCode", d."IsActive",
       j."TitleCode", j."IsActive",
       NOT EXISTS (SELECT 1 FROM public."Users" u WHERE u."EmployeeId" = e."Id")
FROM public."Employees" e
JOIN public."Departments" d ON d."Id" = e."DepartmentId"
JOIN public."JobTitles" j ON j."Id" = e."JobTitleId"
WHERE e."EmployeeNo" = 'WIP-DEMO-001';
'@
$verification = $verificationSql | docker exec -i $containerName psql -v ON_ERROR_STOP=1 -U erp -d $databaseName -At -F '|'
if ($LASTEXITCODE -ne 0) {
    throw 'Could not verify the synthetic WIP employee.'
}
$fields = $verification.Trim() -split '\|'
if ($fields.Count -ne 10 -or $fields[0] -ne 'WIP-DEMO-001' -or $fields[2] -ne 'active' -or
    $fields[3] -ne 'WIP-NO-REAL-DOCUMENT' -or $fields[4] -ne 't' -or
    $fields[5] -ne 'WIP' -or $fields[6] -ne 't' -or $fields[7] -ne 'WIP-TEST' -or
    $fields[8] -ne 't' -or $fields[9] -ne 't') {
    throw 'The synthetic WIP employee did not pass the account-provisioning eligibility checks.'
}

Write-Output 'Seeded and verified synthetic employee WIP-DEMO-001 in erp-main-wip-postgres / ErpGeneral.'
Write-Output 'This employee uses a reserved .invalid email and a clearly synthetic document identifier.'
