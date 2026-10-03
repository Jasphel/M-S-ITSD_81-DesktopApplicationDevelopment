-- 1. Basic Retrieval: Retrieve all equipment
SELECT * FROM "Equipment";

-- 2. Filtering: Retrieve available equipment
SELECT * FROM "Equipment" WHERE "IsAvailable" = TRUE;

-- 3. Join: Retrieve active borrowings with student and equipment info
SELECT 
    s."FullName" AS Student,
    e."Name" AS Equipment,
    b."BorrowedAt" AS Borrowed,
    b."ExpectedReturnAt" AS Due
FROM "Borrowings" b
JOIN "Students" s ON b."StudentId" = s."Id"
JOIN "Equipment" e ON b."EquipmentId" = e."Id"
WHERE b."Status" = 0;

-- 4. Aggregate: Count active borrowings per student
SELECT "StudentId", COUNT(*) AS ActiveBorrowings
FROM "Borrowings"
WHERE "Status" = 0
GROUP BY "StudentId";

-- 5. Update: Update equipment availability state
UPDATE "Equipment" 
SET "IsAvailable" = FALSE 
WHERE "Id" = 1;