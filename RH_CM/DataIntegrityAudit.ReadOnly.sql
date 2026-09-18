/*
    Trainify / RH - Data integrity audit (READ ONLY)

    Safe for STG and Production:
      - SELECT statements only.
      - No persistent objects, temporary tables, DML or DDL.
      - Run with an account that has only db_datareader when possible.

    Result sets:
      1. Prioritized summary.
      2. Orphan course assignments.
      3. Orphan movements.
      4. Orphan completions.
      5. Exam movements without answers.
      6. CODE_EXAM values shared by multiple employees.
      7. Internal completions without a passed internal movement.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @TopRows int = 500;

/* ================================================================
   1) SUMMARY
   ================================================================ */
;WITH AuditSummary AS
(
    SELECT CAST(1 AS int) AS SortOrder,
           CAST(N'CRITICAL' AS nvarchar(20)) AS Severity,
           CAST(N'EmployeesMissingDepartment' AS nvarchar(100)) AS Finding,
           COUNT_BIG(*) AS FindingCount,
           CAST(N'Headcount references a department that does not exist.' AS nvarchar(500)) AS Interpretation
    FROM dbo.SY_HEADCOUNT h
    LEFT JOIN dbo.CT_DEPARTMENT d ON d.PK_DEPARTMENT = h.FK_DEPARTMENT
    WHERE d.PK_DEPARTMENT IS NULL

    UNION ALL
    SELECT 2, 'CRITICAL', 'EmployeesMissingPosition', COUNT_BIG(*),
           'Headcount references a position that does not exist.'
    FROM dbo.SY_HEADCOUNT h
    LEFT JOIN dbo.CT_POSITION p ON p.PK_POSITION = h.FK_POSITION
    WHERE p.PK_POSITION IS NULL

    UNION ALL
    SELECT 3, 'CRITICAL', 'AssignmentsMissingCourse', COUNT_BIG(*),
           'A course assignment references a deleted/nonexistent course.'
    FROM dbo.CT_COURSEASSIGNMENTS a
    LEFT JOIN dbo.CT_COURSE c ON c.PK_Course = a.FK_Course
    WHERE c.PK_Course IS NULL

    UNION ALL
    SELECT 4, 'CRITICAL', 'AssignmentsMissingPosition', COUNT_BIG(*),
           'A course assignment references a deleted/nonexistent position.'
    FROM dbo.CT_COURSEASSIGNMENTS a
    LEFT JOIN dbo.CT_POSITION p ON p.PK_POSITION = a.FK_Position
    WHERE p.PK_POSITION IS NULL

    UNION ALL
    SELECT 5, 'CRITICAL', 'MovementsMissingAssignment', COUNT_BIG(*),
           'Training history cannot resolve its course assignment.'
    FROM dbo.SY_COURSEMOVEMENTS m
    LEFT JOIN dbo.CT_COURSEASSIGNMENTS a ON a.PK_CourseAssignment = m.FK_CourseAssignment
    WHERE a.PK_CourseAssignment IS NULL

    UNION ALL
    SELECT 6, 'CRITICAL', 'MovementsMissingEmployee', COUNT_BIG(*),
           'Training history cannot resolve its employee.'
    FROM dbo.SY_COURSEMOVEMENTS m
    LEFT JOIN dbo.SY_HEADCOUNT h ON h.PK_HEADCOUNT = m.FK_Headcount
    WHERE h.PK_HEADCOUNT IS NULL

    UNION ALL
    SELECT 7, 'CRITICAL', 'CompletionsMissingAssignment', COUNT_BIG(*),
           'A completion cannot resolve its course assignment.'
    FROM dbo.SY_COURSECOMPLETED c
    LEFT JOIN dbo.CT_COURSEASSIGNMENTS a ON a.PK_CourseAssignment = c.FK_CourseAssignment
    WHERE a.PK_CourseAssignment IS NULL

    UNION ALL
    SELECT 8, 'CRITICAL', 'CompletionsMissingEmployee', COUNT_BIG(*),
           'A completion cannot resolve its employee.'
    FROM dbo.SY_COURSECOMPLETED c
    LEFT JOIN dbo.SY_HEADCOUNT h ON h.PK_HEADCOUNT = c.FK_Headcount
    WHERE h.PK_HEADCOUNT IS NULL

    UNION ALL
    SELECT 9, 'HIGH', 'ActiveEmployeesInactiveDepartment', COUNT_BIG(*),
           'Active employee is linked to an inactive department.'
    FROM dbo.SY_HEADCOUNT h
    JOIN dbo.CT_DEPARTMENT d ON d.PK_DEPARTMENT = h.FK_DEPARTMENT
    WHERE h.AVAILABLE = 1 AND ISNULL(d.AVAILABLE, 0) <> 1

    UNION ALL
    SELECT 10, 'HIGH', 'ActiveEmployeesInactivePosition', COUNT_BIG(*),
           'Active employee is linked to an inactive position.'
    FROM dbo.SY_HEADCOUNT h
    JOIN dbo.CT_POSITION p ON p.PK_POSITION = h.FK_POSITION
    WHERE h.AVAILABLE = 1 AND ISNULL(p.AVAILABLE, 0) <> 1

    UNION ALL
    SELECT 11, 'HIGH', 'InternalExamMovementsWithoutAnswers', COUNT_BIG(*),
           'Internal exam movement has no answer rows. External delivery mode is intentionally excluded.'
    FROM dbo.SY_COURSEMOVEMENTS m
    WHERE m.FK_DeliveryMode = 1
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.SY_USER_ANSWERS a
          WHERE a.CODE_EXAM = m.CODE_EXAM
            AND a.FK_HEADCOUNT = m.FK_Headcount
            AND a.AVAILABLE = 1
      )

    UNION ALL
    SELECT 12, 'HIGH', 'ExamCodesSharedByMultipleEmployees', COUNT_BIG(*),
           'Legacy CODE_EXAM value is used by more than one employee; review/delete must also filter by employee.'
    FROM
    (
        SELECT e.CODE_EXAM
        FROM
        (
            SELECT CODE_EXAM, FK_HEADCOUNT FROM dbo.SY_USER_DIAGNOSTIC
            UNION
            SELECT CODE_EXAM, FK_HEADCOUNT FROM dbo.SY_USER_ANSWERS
            UNION
            SELECT CODE_EXAM, FK_Headcount FROM dbo.SY_COURSEMOVEMENTS
        ) e
        GROUP BY e.CODE_EXAM
        HAVING COUNT(DISTINCT e.FK_HEADCOUNT) > 1
    ) duplicateCodes

    UNION ALL
    SELECT 13, 'REVIEW', 'InternalCompletionsWithoutPassedMovement', COUNT_BIG(*),
           'May be legacy/manual imports. Review before treating as an application error.'
    FROM dbo.SY_COURSECOMPLETED c
    WHERE c.Avaialble = 1
      AND c.FK_DeliveryMode = 1
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.SY_COURSEMOVEMENTS m
          WHERE m.FK_CourseAssignment = c.FK_CourseAssignment
            AND m.FK_Headcount = c.FK_Headcount
            AND m.FK_CourseStatus = 1
            AND m.Avaialble = 1
      )
)
SELECT Severity, Finding, FindingCount, Interpretation
FROM AuditSummary
ORDER BY SortOrder;

/* ================================================================
   2) COURSE ASSIGNMENTS WITH MISSING CATALOG REFERENCES
   ================================================================ */
SELECT TOP (@TopRows)
       a.PK_CourseAssignment,
       a.FK_Course,
       c.CourseName,
       a.FK_Position,
       p.NAME_POSITION,
       a.FK_RequiredCourseLevels,
       a.FK_DeliveryMode,
       a.Available,
       CASE
           WHEN c.PK_Course IS NULL THEN 'MISSING COURSE'
           WHEN p.PK_POSITION IS NULL THEN 'MISSING POSITION'
       END IntegrityProblem
FROM dbo.CT_COURSEASSIGNMENTS a
LEFT JOIN dbo.CT_COURSE c ON c.PK_Course = a.FK_Course
LEFT JOIN dbo.CT_POSITION p ON p.PK_POSITION = a.FK_Position
WHERE c.PK_Course IS NULL OR p.PK_POSITION IS NULL
ORDER BY a.PK_CourseAssignment;

/* ================================================================
   3) MOVEMENTS WITH MISSING ASSIGNMENT OR EMPLOYEE
   ================================================================ */
SELECT TOP (@TopRows)
       m.PK_MovementCourse,
       m.CODE_EXAM,
       m.FK_CourseAssignment,
       m.FK_Headcount,
       m.FK_CourseStatus,
       m.FK_DeliveryMode,
       m.Score,
       m.CreateDate,
       CONCAT(
           CASE WHEN a.PK_CourseAssignment IS NULL THEN 'MISSING ASSIGNMENT; ' ELSE '' END,
           CASE WHEN h.PK_HEADCOUNT IS NULL THEN 'MISSING EMPLOYEE; ' ELSE '' END
       ) IntegrityProblem
FROM dbo.SY_COURSEMOVEMENTS m
LEFT JOIN dbo.CT_COURSEASSIGNMENTS a ON a.PK_CourseAssignment = m.FK_CourseAssignment
LEFT JOIN dbo.SY_HEADCOUNT h ON h.PK_HEADCOUNT = m.FK_Headcount
WHERE a.PK_CourseAssignment IS NULL OR h.PK_HEADCOUNT IS NULL
ORDER BY m.CreateDate DESC, m.PK_MovementCourse DESC;

/* ================================================================
   4) COMPLETIONS WITH MISSING ASSIGNMENT OR EMPLOYEE
   ================================================================ */
SELECT TOP (@TopRows)
       c.PK_CourseCompleted,
       c.FK_CourseAssignment,
       c.FK_Headcount,
       c.FK_CourseStatus,
       c.FK_DeliveryMode,
       c.Score,
       c.CreateDate,
       CONCAT(
           CASE WHEN a.PK_CourseAssignment IS NULL THEN 'MISSING ASSIGNMENT; ' ELSE '' END,
           CASE WHEN h.PK_HEADCOUNT IS NULL THEN 'MISSING EMPLOYEE; ' ELSE '' END
       ) IntegrityProblem
FROM dbo.SY_COURSECOMPLETED c
LEFT JOIN dbo.CT_COURSEASSIGNMENTS a ON a.PK_CourseAssignment = c.FK_CourseAssignment
LEFT JOIN dbo.SY_HEADCOUNT h ON h.PK_HEADCOUNT = c.FK_Headcount
WHERE a.PK_CourseAssignment IS NULL OR h.PK_HEADCOUNT IS NULL
ORDER BY c.CreateDate DESC, c.PK_CourseCompleted DESC;

/* ================================================================
   5) INTERNAL EXAM MOVEMENTS WITHOUT ANSWERS
   ================================================================ */
SELECT TOP (@TopRows)
       m.PK_MovementCourse,
       m.CODE_EXAM,
       m.FK_Headcount,
       h.CONTROL_NUMBER,
       m.FK_CourseAssignment,
       c.CourseName,
       m.FK_CourseStatus,
       m.Score,
       m.CreateDate
FROM dbo.SY_COURSEMOVEMENTS m
LEFT JOIN dbo.SY_HEADCOUNT h ON h.PK_HEADCOUNT = m.FK_Headcount
LEFT JOIN dbo.CT_COURSEASSIGNMENTS ca ON ca.PK_CourseAssignment = m.FK_CourseAssignment
LEFT JOIN dbo.CT_COURSE c ON c.PK_Course = ca.FK_Course
WHERE m.FK_DeliveryMode = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.SY_USER_ANSWERS a
      WHERE a.CODE_EXAM = m.CODE_EXAM
        AND a.FK_HEADCOUNT = m.FK_Headcount
        AND a.AVAILABLE = 1
  )
ORDER BY m.CreateDate DESC, m.PK_MovementCourse DESC;

/* ================================================================
   6) CODE_EXAM SHARED BY MULTIPLE EMPLOYEES
   ================================================================ */
SELECT TOP (@TopRows)
       e.CODE_EXAM,
       COUNT(DISTINCT e.FK_HEADCOUNT) EmployeeCount,
       MIN(e.FK_HEADCOUNT) FirstHeadcountId,
       MAX(e.FK_HEADCOUNT) LastHeadcountId
FROM
(
    SELECT CODE_EXAM, FK_HEADCOUNT FROM dbo.SY_USER_DIAGNOSTIC
    UNION
    SELECT CODE_EXAM, FK_HEADCOUNT FROM dbo.SY_USER_ANSWERS
    UNION
    SELECT CODE_EXAM, FK_Headcount FROM dbo.SY_COURSEMOVEMENTS
) e
GROUP BY e.CODE_EXAM
HAVING COUNT(DISTINCT e.FK_HEADCOUNT) > 1
ORDER BY e.CODE_EXAM DESC;

/* ================================================================
   7) INTERNAL COMPLETIONS WITHOUT A PASSED INTERNAL MOVEMENT
      These can be legitimate manual/imported records, so review them.
   ================================================================ */
SELECT TOP (@TopRows)
       c.PK_CourseCompleted,
       c.FK_Headcount,
       h.CONTROL_NUMBER,
       c.FK_CourseAssignment,
       course.CourseName,
       c.Score,
       c.CreateUser,
       c.CreateDate
FROM dbo.SY_COURSECOMPLETED c
LEFT JOIN dbo.SY_HEADCOUNT h ON h.PK_HEADCOUNT = c.FK_Headcount
LEFT JOIN dbo.CT_COURSEASSIGNMENTS ca ON ca.PK_CourseAssignment = c.FK_CourseAssignment
LEFT JOIN dbo.CT_COURSE course ON course.PK_Course = ca.FK_Course
WHERE c.Avaialble = 1
  AND c.FK_DeliveryMode = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.SY_COURSEMOVEMENTS m
      WHERE m.FK_CourseAssignment = c.FK_CourseAssignment
        AND m.FK_Headcount = c.FK_Headcount
        AND m.FK_CourseStatus = 1
        AND m.Avaialble = 1
  )
ORDER BY c.CreateDate DESC, c.PK_CourseCompleted DESC;
